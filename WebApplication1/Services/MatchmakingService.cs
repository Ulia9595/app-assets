using System.Collections.Concurrent;

namespace WebApplication1.Services
{
    public record MatchmakingEntry(
        int UserId,
        int Rating,
        int TopicId,
        string ConnectionId,
        DateTime EnqueuedAt
    );

    public interface IMatchmakingService
    {
        MatchmakingEntry? Enqueue(MatchmakingEntry entry);
        void Remove(int userId);
        void UpdateConnectionId(int userId, string newConnectionId);
        bool IsQueued(int userId);
    }

    public sealed class MatchmakingService : IMatchmakingService
    {
        private readonly ConcurrentDictionary<int, MatchmakingEntry> _queue = new();

        private readonly object _matchLock = new();

        private const int MinRatingDiff = 0;
        private const int MaxRatingDiff = 200;

        public MatchmakingEntry? Enqueue(MatchmakingEntry entry)
        {
            Console.WriteLine("MATCHMAKING QUEUE");
            Console.WriteLine($"ENQUEUE: User {entry.UserId}, Rating={entry.Rating}, Topic={entry.TopicId}");
            Console.WriteLine($"Enqueued at: {entry.EnqueuedAt:HH:mm:ss.fff}");

            lock (_matchLock)
            {
                _queue[entry.UserId] = entry;

                Console.WriteLine($"Queue size after add: {_queue.Count}");

                if (_queue.Count > 1)
                {
                    Console.WriteLine("urrent queue contents:");
                    foreach (var q in _queue.Values)
                        Console.WriteLine($"User {q.UserId}: Rating={q.Rating}, Topic={q.TopicId}, Enqueued={q.EnqueuedAt:HH:mm:ss.fff}");
                }

                Console.WriteLine($"Searching for opponent for User {entry.UserId} (Topic={entry.TopicId}, Rating={entry.Rating})...");
                Console.WriteLine($"Criteria: Same Topic, Rating diff between {MinRatingDiff} and {MaxRatingDiff}");

                var candidates = _queue.Values.Where(e => e.UserId != entry.UserId).ToList();

                if (candidates.Any())
                {
                    Console.WriteLine($"Candidates found: {candidates.Count}");
                    foreach (var c in candidates)
                    {
                        bool sameTopic = c.TopicId == entry.TopicId;
                        int ratingDiff = Math.Abs(c.Rating - entry.Rating);
                        bool inRange = ratingDiff >= MinRatingDiff && ratingDiff <= MaxRatingDiff;
                        Console.WriteLine($"User {c.UserId}: Topic={c.TopicId} (match={sameTopic}), RatingDiff={ratingDiff} (inRange={inRange})");
                    }
                }
                else
                {
                    Console.WriteLine("No other candidates in queue");
                }

                var opponent = _queue.Values
                    .Where(e =>
                        e.UserId != entry.UserId &&
                        e.TopicId == entry.TopicId &&
                        Math.Abs(e.Rating - entry.Rating) >= MinRatingDiff &&
                        Math.Abs(e.Rating - entry.Rating) <= MaxRatingDiff)
                    .OrderBy(e => e.EnqueuedAt)
                    .FirstOrDefault();

                if (opponent == null)
                {
                    Console.WriteLine($"No suitable opponent found for User {entry.UserId}");
                    Console.WriteLine("END QUEUE (WAITING)");
                    return null;
                }

                Console.WriteLine("MATCH FOUND!");
                Console.WriteLine($"Player1: User {entry.UserId} (Rating={entry.Rating}, Topic={entry.TopicId})");
                Console.WriteLine($"Player2: User {opponent.UserId} (Rating={opponent.Rating}, Topic={opponent.TopicId})");
                Console.WriteLine($"Rating difference: {Math.Abs(entry.Rating - opponent.Rating)}");

                _queue.TryRemove(entry.UserId, out _);
                _queue.TryRemove(opponent.UserId, out _);

                Console.WriteLine($"Removed both from queue. New queue size: {_queue.Count}");
                Console.WriteLine("END QUEUE (MATCHED)");

                return opponent;
            }
        }

        public void Remove(int userId) => _queue.TryRemove(userId, out _);

        public void UpdateConnectionId(int userId, string newConnectionId)
        {
            if (_queue.TryGetValue(userId, out var existing))
                _queue[userId] = existing with { ConnectionId = newConnectionId };
        }

        public bool IsQueued(int userId) => _queue.ContainsKey(userId);
    }
}
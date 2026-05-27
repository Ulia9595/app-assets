using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication1.Data;
using WebApplication1.Models.Entities;
using WebApplication1.Services;

namespace WebApplication1.Hubs
{
    [Authorize]
    public class TournamentHub : Hub
    {
        private const int StatusWaiting = 1;
        private const int StatusActive = 2;
        private const int StatusFinished = 3;
        private const int StatusCancelled = 4;
        private const int StatusInactive = 5;

        private const int ParticipationBonus = 5;
        private const int DrawRatingDelta = 15;
        private const int LossRatingDelta = -25;

        private static readonly Dictionary<string, int> DifficultyPoints = new(StringComparer.OrdinalIgnoreCase)
        {
            ["лёгкий"] = 5,
            ["легкий"] = 5,
            ["средний"] = 10,
            ["сложный"] = 15,
        };

        private const int QuestionsPerTournament = 3;

        private const int InviteExpirySeconds = 60;

        private const int TournamentTimeoutSeconds = 150;

        private static readonly System.Collections.Concurrent.ConcurrentDictionary
            <int, PendingInvite> PendingInvites = new();

        private readonly IServiceProvider _services;
        private readonly IMatchmakingService _matchmaking;

        public TournamentHub(IServiceProvider services, IMatchmakingService matchmaking)
        {
            _services = services;
            _matchmaking = matchmaking;
        }

        private int GetUserId() =>
            int.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public override async Task OnConnectedAsync()
        {
            int userId = GetUserId();
            Console.WriteLine($"Client CONNECTED: User {userId}, ConnectionId: {Context.ConnectionId}");

            _matchmaking.UpdateConnectionId(userId, Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            int userId = GetUserId();
            Console.WriteLine($"Client DISCONNECTED: User {userId}, ConnectionId: {Context.ConnectionId}, Exception: {exception?.Message ?? "none"}");

            _matchmaking.Remove(userId);

            CancelPendingInviteFor(userId);

            await CancelActiveTournamentOnDisconnect(userId);

            await base.OnDisconnectedAsync(exception);
        }

        public async Task FindMatch(int topicId)
        {
            int userId = GetUserId();

            Console.WriteLine($"FIND MATCH START");
            Console.WriteLine($"User: {userId}, RequestedTopicId: {topicId}, Time: {DateTime.Now:HH:mm:ss.fff}");
            Console.WriteLine($"ConnectionId: {Context.ConnectionId}");

            if (_matchmaking.IsQueued(userId))
            {
                Console.WriteLine($"User {userId} is already in queue!");
                await Clients.Caller.SendAsync("Error", "Вы уже в очереди поиска.");
                return;
            }

            await using var scope = _services.CreateAsyncScope();
            await using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            int originalTopicId = topicId;

            if (topicId == 0)
            {
                Console.WriteLine($"User {userId}: Resolving topic from user progress...");
                topicId = await ResolveCurrentTopicId(userId, db);
                Console.WriteLine($"User {userId}: Auto-resolved TopicId = {topicId} (was {originalTopicId})");
            }
            else
            {
                Console.WriteLine($"User {userId}: Using explicit TopicId = {topicId}");
            }

            if (topicId == 0)
            {
                Console.WriteLine($"User {userId}: No topic found! User hasn't completed any levels");
                await Clients.Caller.SendAsync("Error", "Не удалось определить тему. Пройдите хотя бы один уровень.");
                return;
            }

            var rating = await db.UserRatings
                .Where(r => r.UserId == userId)
                .Select(r => r.CurrentRating)
                .FirstOrDefaultAsync();

            Console.WriteLine($"User {userId}: Rating = {rating}, Final Topic = {topicId}");

            var myEntry = new MatchmakingEntry(userId, rating, topicId, Context.ConnectionId, DateTime.UtcNow);

            Console.WriteLine($"User {userId}: Adding to matchmaking queue...");
            var opponent = _matchmaking.Enqueue(myEntry);

            if (opponent == null)
            {
                Console.WriteLine($"User {userId}: No opponent found, waiting in queue...");
                Console.WriteLine($"FIND MATCH END (WAITING)");
                await Clients.Caller.SendAsync("Searching", new { message = "Ищем соперника...", topicId });
                return;
            }

            Console.WriteLine($"MATCH FOUND! User {userId} matched with opponent {opponent.UserId}");
            Console.WriteLine($"Opponent Rating: {opponent.Rating}, Topic: {opponent.TopicId}");
            Console.WriteLine($"FIND MATCH END (MATCHED)");

            await SendInvites(db, myEntry, opponent);
        }

        public Task CancelSearch()
        {
            _matchmaking.Remove(GetUserId());
            return Clients.Caller.SendAsync("SearchCancelled");
        }

        [HubMethodName("AcceptInvite")]
        public async Task AcceptInvite(int tournamentId)
        {
            int userId = GetUserId();

            if (!PendingInvites.TryGetValue(tournamentId, out var invite)) return;

            invite.MarkAccepted(userId);

            if (invite.BothAccepted)
            {
                PendingInvites.TryRemove(tournamentId, out _);

                using (var scope = _services.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    await StartTournament(db, invite.TournamentId, invite.Player1, invite.Player2);
                }
            }
            else
            {
                var opponent = invite.Player1.UserId == userId ? invite.Player2 : invite.Player1;

                var myName = Context.User?.FindFirstValue(ClaimTypes.Name) ?? "Соперник";

                await Clients.Client(opponent.ConnectionId).SendAsync("OpponentAccepted", new
                {
                    TournamentId = tournamentId,
                    OpponentName = myName
                });

                await Clients.Caller.SendAsync("InviteAcceptedByMe", tournamentId);
            }
        }

        public async Task DeclineInvite(int tournamentId)
        {
            int userId = GetUserId();

            if (!PendingInvites.TryRemove(tournamentId, out var pending))
                return;

            await using var scope = _services.CreateAsyncScope();
            await using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tournament = await db.Tournaments.FindAsync(tournamentId);
            if (tournament != null)
            {
                tournament.StatusId = StatusCancelled;
                await db.SaveChangesAsync();
            }

            var decliningConnectionId = pending.Player1.UserId == userId
                ? pending.Player1.ConnectionId
                : pending.Player2.ConnectionId;
            var otherConnectionId = pending.Player1.UserId == userId
                ? pending.Player2.ConnectionId
                : pending.Player1.ConnectionId;

            await Clients.Client(otherConnectionId)
                .SendAsync("InviteDeclined", new { tournamentId });

            var otherEntry = pending.Player1.UserId == userId
                ? pending.Player2 : pending.Player1;

            _ = Task.Delay(500).ContinueWith(async _ =>
            {
                await using var scope2 = _services.CreateAsyncScope();
                await using var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
                var otherRating = await db2.UserRatings
                    .Where(r => r.UserId == otherEntry.UserId)
                    .Select(r => r.CurrentRating)
                    .FirstOrDefaultAsync();

                var reQueueEntry = otherEntry with
                {
                    Rating = otherRating,
                    EnqueuedAt = DateTime.UtcNow
                };

                if (_matchmaking.IsQueued(otherEntry.UserId)) return;

                var newOpponent = _matchmaking.Enqueue(reQueueEntry);

                if (newOpponent != null)
                    await SendInvites(db2, reQueueEntry, newOpponent);
                else
                    await Clients.Client(otherEntry.ConnectionId)
                        .SendAsync("Searching", new { message = "Ищем нового соперника...", topicId = otherEntry.TopicId });
            }, TaskScheduler.Default);
        }

        public async Task SubmitAnswer(int tournamentId, int questionId, int answerOptionId)
        {
            int userId = GetUserId();
            await using var scope = _services.CreateAsyncScope();
            await using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var tournament = await db.Tournaments
                .Include(t => t.Status)
                .Include(t => t.TournamentQuestions)
                    .ThenInclude(tq => tq.Question)
                        .ThenInclude(q => q.DifficultyType)
                .Include(t => t.TournamentQuestions)
                    .ThenInclude(tq => tq.Question)
                        .ThenInclude(q => q.AnswerOptions)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            if (tournament == null)
            {
                await Clients.Caller.SendAsync("Error", "Турнир не найден.");
                return;
            }
            if (tournament.StatusId != StatusActive)
            {
                await Clients.Caller.SendAsync("Error", "Турнир не активен.");
                return;
            }

            var tq = tournament.TournamentQuestions.FirstOrDefault(q => q.QuestionId == questionId);
            if (tq == null)
            {
                await Clients.Caller.SendAsync("Error", "Вопрос не относится к этому турниру.");
                return;
            }

            bool alreadyAnswered = await db.PlayerAnswers
                .AnyAsync(pa => pa.UserId == userId && pa.TournamentId == tournamentId && pa.QuestionId == questionId);
            if (alreadyAnswered)
            {
                await Clients.Caller.SendAsync("Error", "Вы уже ответили на этот вопрос.");
                return;
            }

            var answerOption = tq.Question.AnswerOptions.FirstOrDefault(a => a.Id == answerOptionId);
            if (answerOption == null)
            {
                await Clients.Caller.SendAsync("Error", "Ответ на вопрос отсутствует.");
                return;
            }

            db.PlayerAnswers.Add(new PlayerAnswer
            {
                UserId = userId,
                QuestionId = questionId,
                TournamentId = tournamentId,
                AnswerOptionId = answerOptionId,
                AnsweredAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            int pointsForQuestion = answerOption.IsCorrect
                ? GetPointsForDifficulty(tq.Question.DifficultyType.Name)
                : 0;

            await Clients.Caller.SendAsync("AnswerResult", new
            {
                questionId,
                isCorrect = answerOption.IsCorrect,
                correctOptionId = tq.Question.AnswerOptions.FirstOrDefault(a => a.IsCorrect)?.Id,
                points = pointsForQuestion
            });

            int totalQuestions = tournament.TournamentQuestions.Count;
            var allAnswers = await db.PlayerAnswers
                .Where(pa => pa.TournamentId == tournamentId)
                .ToListAsync();

            var playersFinished = allAnswers
                .GroupBy(pa => pa.UserId)
                .Where(g => g.Count() >= totalQuestions)
                .Select(g => g.Key)
                .ToList();

            if (playersFinished.Count >= 2)
                await FinalizeTournament(db, tournament, allAnswers);
        }

        private async Task SendInvites(
            AppDbContext db, MatchmakingEntry player1, MatchmakingEntry player2)
        {
            Console.WriteLine($"SEND INVITES");
            Console.WriteLine($"Tournament: User {player1.UserId} vs User {player2.UserId}");
            Console.WriteLine($"Player1 ConnectionId: {player1.ConnectionId}");
            Console.WriteLine($"Player2 ConnectionId: {player2.ConnectionId}");

            var questions = await SelectQuestions(db, player1.TopicId);

            if (questions.Count < QuestionsPerTournament)
            {
                Console.WriteLine($"Not enough questions! Found {questions.Count}, need {QuestionsPerTournament}");
                await Clients.Client(player1.ConnectionId).SendAsync("Error",
                    "Недостаточно вопросов по данной теме. Попробуйте позже.");
                await Clients.Client(player2.ConnectionId).SendAsync("Error",
                    "Недостаточно вопросов по данной теме. Попробуйте позже.");
                return;
            }

            var userNames = await db.Users
                .Where(u => u.Id == player1.UserId || u.Id == player2.UserId)
                .Select(u => new { u.Id, u.Name })
                .ToDictionaryAsync(u => u.Id, u => u.Name ?? $"Игрок #{u.Id}");

            var topicName = await db.Topics
                .Where(t => t.Id == player1.TopicId)
                .Select(t => t.Name)
                .FirstOrDefaultAsync() ?? "Неизвестная тема";

            var tournament = new Tournament
            {
                TopicId = player1.TopicId,
                MinRating = Math.Min(player1.Rating, player2.Rating),
                MaxRating = Math.Max(player1.Rating, player2.Rating),
                StatusId = StatusWaiting,
                CreatedAt = DateTime.UtcNow
            };
            db.Tournaments.Add(tournament);
            await db.SaveChangesAsync();
            Console.WriteLine($"Tournament created: ID={tournament.Id}");

            for (int i = 0; i < questions.Count; i++)
            {
                db.TournamentQuestions.Add(new TournamentQuestion
                {
                    TournamentId = tournament.Id,
                    QuestionId = questions[i].Id,
                    QuestionNumber = i + 1
                });
            }
            await db.SaveChangesAsync();

            PendingInvites[tournament.Id] = new PendingInvite(tournament.Id, player1, player2);
            Console.WriteLine($"Pending invite registered for tournament {tournament.Id}");

            Console.WriteLine($"Sending InviteReceived to Player {player1.UserId} (ConnectionId: {player1.ConnectionId})...");
            await Clients.Client(player1.ConnectionId).SendAsync("InviteReceived", new
            {
                tournamentId = tournament.Id,
                opponentId = player2.UserId,
                opponentName = userNames.GetValueOrDefault(player2.UserId, $"Игрок #{player2.UserId}"),
                opponentRating = player2.Rating,
                topicName,
                expiresInSeconds = InviteExpirySeconds
            });
            Console.WriteLine($"Sent to Player {player1.UserId}");

            Console.WriteLine($"Sending InviteReceived to Player {player2.UserId} (ConnectionId: {player2.ConnectionId})...");
            await Clients.Client(player2.ConnectionId).SendAsync("InviteReceived", new
            {
                tournamentId = tournament.Id,
                opponentId = player1.UserId,
                opponentName = userNames.GetValueOrDefault(player1.UserId, $"Игрок #{player1.UserId}"),
                opponentRating = player1.Rating,
                topicName,
                expiresInSeconds = InviteExpirySeconds
            });
            Console.WriteLine($"Sent to Player {player2.UserId}");
            Console.WriteLine($"SEND INVITES END");

            _ = Task.Delay(TimeSpan.FromSeconds(InviteExpirySeconds)).ContinueWith(async _ =>
            {
                if (!PendingInvites.TryRemove(tournament.Id, out PendingInvite? _)) return;

                await using var expScope = _services.CreateAsyncScope();
                await using var expDb = expScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var t = await expDb.Tournaments.FindAsync(tournament.Id);
                if (t != null && t.StatusId == StatusWaiting)
                {
                    t.StatusId = StatusCancelled;
                    await expDb.SaveChangesAsync();
                }

                await Clients.Client(player1.ConnectionId)
                    .SendAsync("InviteExpired", new { tournamentId = tournament.Id });
                await Clients.Client(player2.ConnectionId)
                    .SendAsync("InviteExpired", new { tournamentId = tournament.Id });
            });
        }

        private async Task StartTournament(
            AppDbContext db, int tournamentId,
            MatchmakingEntry player1, MatchmakingEntry player2)
        {
            var tournament = await db.Tournaments
                .Include(t => t.TournamentQuestions.OrderBy(tq => tq.QuestionNumber))
                    .ThenInclude(tq => tq.Question)
                        .ThenInclude(q => q.DifficultyType)
                .Include(t => t.TournamentQuestions)
                    .ThenInclude(tq => tq.Question)
                        .ThenInclude(q => q.AnswerOptions)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);

            if (tournament == null) return;

            tournament.StatusId = StatusActive;
            await db.SaveChangesAsync();

            string groupName = TournamentGroupName(tournamentId);
            await Groups.AddToGroupAsync(player1.ConnectionId, groupName);
            await Groups.AddToGroupAsync(player2.ConnectionId, groupName);

            var questionDtos = tournament.TournamentQuestions
                .OrderBy(tq => tq.QuestionNumber)
                .Select(tq => new
                {
                    id = tq.QuestionId,
                    text = tq.Question.QuestionText,
                    difficulty = tq.Question.DifficultyType.Name,
                    points = GetPointsForDifficulty(tq.Question.DifficultyType.Name),
                    options = tq.Question.AnswerOptions
                        .Select(a => new { id = a.Id, text = a.AnswerText }).ToList()
                }).ToList();

            await Clients.Group(groupName).SendAsync("TournamentStarted", new
            {
                tournamentId,
                questions = questionDtos
            });

            var userNames = await db.Users
                .Where(u => u.Id == player1.UserId || u.Id == player2.UserId)
                .Select(u => new { u.Id, u.Name })
                .ToDictionaryAsync(u => u.Id, u => u.Name ?? $"Игрок #{u.Id}");

            await Clients.Client(player1.ConnectionId).SendAsync("OpponentInfo", new
            {
                opponentId = player2.UserId,
                opponentRating = player2.Rating,
                opponentName = userNames.GetValueOrDefault(player2.UserId, "")
            });

            await Clients.Client(player2.ConnectionId).SendAsync("OpponentInfo", new
            {
                opponentId = player1.UserId,
                opponentRating = player1.Rating,
                opponentName = userNames.GetValueOrDefault(player1.UserId, "")
            });

            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(TournamentTimeoutSeconds));

                await using var timeoutScope = _services.CreateAsyncScope();
                await using var timeoutDb = timeoutScope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                var timeoutTournament = await timeoutDb.Tournaments
                    .Include(t => t.TournamentQuestions)
                        .ThenInclude(tq => tq.Question)
                            .ThenInclude(q => q.DifficultyType)
                    .Include(t => t.TournamentQuestions)
                        .ThenInclude(tq => tq.Question)
                            .ThenInclude(q => q.AnswerOptions)
                    .FirstOrDefaultAsync(t => t.Id == tournamentId);

                if (timeoutTournament == null)
                    return;

                if (timeoutTournament.StatusId != StatusActive)
                    return;

                var answers = await timeoutDb.PlayerAnswers
                    .Where(pa => pa.TournamentId == tournamentId)
                    .ToListAsync();

                if (answers.Count == 0)
                    {
                    answers = new List<PlayerAnswer>
                    {
                new()
                    {
                    TournamentId = tournamentId,
                    UserId = player1.UserId,
                    QuestionId = timeoutTournament.TournamentQuestions.First().QuestionId,
                    AnswerOptionId = -1,
                    AnsweredAt = DateTime.UtcNow
                    },
                new()
                    {
                    TournamentId = tournamentId,
                    UserId = player2.UserId,
                    QuestionId = timeoutTournament.TournamentQuestions.First().QuestionId,
                    AnswerOptionId = -1,
                    AnsweredAt = DateTime.UtcNow
                    }
                };
                }

                Console.WriteLine($"Tournament timeout reached: {tournamentId}");

                await FinalizeTournament(timeoutDb, timeoutTournament, answers);
            });
        }

        private static void CancelPendingInviteFor(int userId)
        {
            var keys = PendingInvites
                .Where(kv =>
                    kv.Value.Player1.UserId == userId ||
                    kv.Value.Player2.UserId == userId)
                .Select(kv => kv.Key)
                .ToList();

            foreach (var key in keys)
                PendingInvites.TryRemove(key, out _);
        }

        private async Task FinalizeTournament(
            AppDbContext db, Tournament tournament, List<PlayerAnswer> allAnswers)
        {
            var tqs = tournament.TournamentQuestions.ToList();

            var questionMeta = tqs.ToDictionary(
                tq => tq.QuestionId,
                tq => (
                    difficulty: tq.Question.DifficultyType.Name,
                    correctId: tq.Question.AnswerOptions.FirstOrDefault(a => a.IsCorrect)?.Id ?? 0
                ));

            var playerIds = allAnswers.Select(a => a.UserId).Distinct().ToList();

            var scores = new Dictionary<int, int>();
            foreach (var pid in playerIds)
            {
                int score = ParticipationBonus;
                var answers = allAnswers.Where(a => a.UserId == pid).ToList();
                foreach (var ans in answers)
                {
                    if (!questionMeta.TryGetValue(ans.QuestionId, out var meta)) continue;
                    if (ans.AnswerOptionId == meta.correctId)
                        score += GetPointsForDifficulty(meta.difficulty);
                }
                scores[pid] = score;
            }

            int p1 = playerIds[0], p2 = playerIds[1];
            int s1 = scores.GetValueOrDefault(p1), s2 = scores.GetValueOrDefault(p2);

            string result;
            int ratingDelta1, ratingDelta2;

            if (s1 > s2)
            {
                result = "win_p1";
                ratingDelta1 = s1;
                ratingDelta2 = LossRatingDelta;
            }
            else if (s2 > s1)
            {
                result = "win_p2";
                ratingDelta1 = LossRatingDelta;
                ratingDelta2 = s2;
            }
            else
            {
                result = "draw";
                ratingDelta1 = DrawRatingDelta;
                ratingDelta2 = DrawRatingDelta;
            }

            await ApplyRatingChange(db, p1, ratingDelta1, tournament.Id);
            await ApplyRatingChange(db, p2, ratingDelta2, tournament.Id);

            tournament.StatusId = StatusFinished;
            await db.SaveChangesAsync();

            string groupName = TournamentGroupName(tournament.Id);
            await Clients.Group(groupName).SendAsync("TournamentFinished", new
            {
                tournamentId = tournament.Id,
                result,
                scores = new
                {
                    player1 = new { userId = p1, score = s1, ratingDelta = ratingDelta1 },
                    player2 = new { userId = p2, score = s2, ratingDelta = ratingDelta2 }
                }
            });
        }

        private async Task CancelActiveTournamentOnDisconnect(int userId)
        {
            await using var scope = _services.CreateAsyncScope();
            await using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var activeTournamentId = await db.PlayerAnswers
                .Where(pa => pa.UserId == userId)
                .Join(db.Tournaments.Where(t => t.StatusId == StatusActive),
                    pa => pa.TournamentId,
                    t => t.Id,
                    (pa, t) => t.Id)
                .FirstOrDefaultAsync();

            if (activeTournamentId == 0) return;

            var tournament = await db.Tournaments.FindAsync(activeTournamentId);
            if (tournament == null) return;

            tournament.StatusId = StatusCancelled;
            await db.SaveChangesAsync();

            string groupName = TournamentGroupName(activeTournamentId);
            await Clients.Group(groupName).SendAsync("TournamentCancelled", new
            {
                tournamentId = activeTournamentId,
                reason = "Соперник покинул игру."
            });
        }

        private static async Task<List<Question>> SelectQuestions(AppDbContext db, int topicId)
        {
            var allQuestions = await db.Questions
            .Include(q => q.DifficultyType)
            .Include(q => q.AnswerOptions)
            .Where(q => q.TopicId == topicId && q.AnswerOptions.Any(a => a.IsCorrect))
            .ToListAsync();

            var easy = allQuestions
                .Where(q => IsDifficulty(q, "лёгкий", "легкий"))
                .ToList();

            var medium = allQuestions
                .Where(q => IsDifficulty(q, "средний"))
                .ToList();

            var hard = allQuestions
                .Where(q => IsDifficulty(q, "сложный"))
                .ToList();

            var selected = new List<Question>();
            var rng = Random.Shared;

            if (easy.Count > 0)
                selected.Add(easy[rng.Next(easy.Count)]);

            if (medium.Count > 0)
                selected.Add(medium[rng.Next(medium.Count)]);

            if (hard.Count > 0)
                selected.Add(hard[rng.Next(hard.Count)]);

            if (selected.Count < QuestionsPerTournament)
            {
                var usedIds = selected.Select(q => q.Id).ToHashSet();

                var remaining = allQuestions
                    .Where(q => !usedIds.Contains(q.Id))
                    .OrderBy(_ => rng.Next())
                    .Take(QuestionsPerTournament - selected.Count);

                selected.AddRange(remaining);
            }

            return selected;
        }

        private static bool IsDifficulty(Question q, params string[] names) =>
            names.Any(n => string.Equals(q.DifficultyType.Name, n, StringComparison.OrdinalIgnoreCase));

        private static int GetPointsForDifficulty(string difficultyName) =>
            DifficultyPoints.TryGetValue(difficultyName, out var pts) ? pts : 5;

        private static async Task<int> ResolveCurrentTopicId(int userId, AppDbContext db)
        {
            Console.WriteLine($"Resolving topic for User {userId}...");

            var topicId = await db.Solutions
                .Where(s => s.UserId == userId && s.IsCorrect)
                .Join(db.PracticeTasks,
                    s => s.TaskId,
                    t => t.Id,
                    (s, t) => t.LevelId)
                .Join(db.Levels,
                    levelId => levelId,
                    l => l.Id,
                    (levelId, l) => l.TopicId)
                .OrderByDescending(topicId => topicId)
                .FirstOrDefaultAsync();

            Console.WriteLine($"User {userId} -> Resolved TopicId = {topicId}");

            return topicId;
        }

        private async Task ApplyRatingChange(AppDbContext db, int userId, int delta, int tournamentId)
        {
            var rating = await db.UserRatings.FirstOrDefaultAsync(r => r.UserId == userId);
            if (rating == null)
            {
                rating = new UserRating { UserId = userId, CurrentRating = 500 };
                db.UserRatings.Add(rating);
                await db.SaveChangesAsync();
            }

            int oldRating = rating.CurrentRating;
            rating.CurrentRating = Math.Max(0, oldRating + delta);
            rating.LastUpdated = DateTime.UtcNow;

            var reason = await db.RatingChangeReasons
                .FirstOrDefaultAsync(r => r.Name == "PvP-турнир")
                ?? await db.RatingChangeReasons.FirstAsync();

            db.RatingHistories.Add(new RatingHistory
            {
                UserId = userId,
                OldRating = oldRating,
                NewRating = rating.CurrentRating,
                ReasonId = reason.Id,
                TournamentId = tournamentId,
                CreatedAt = DateTime.UtcNow
            });
        }

        private static string TournamentGroupName(int tournamentId) =>
            $"tournament_{tournamentId}";
    }

    internal sealed class PendingInvite
    {
        public int TournamentId { get; }
        public MatchmakingEntry Player1 { get; }
        public MatchmakingEntry Player2 { get; }

        private readonly HashSet<int> _acceptedUsers = new();

        public PendingInvite(int tournamentId, MatchmakingEntry player1, MatchmakingEntry player2)
        {
            TournamentId = tournamentId;
            Player1 = player1;
            Player2 = player2;
        }

        public void MarkAccepted(int userId) => _acceptedUsers.Add(userId);

        public bool BothAccepted => _acceptedUsers.Count >= 2;

        public bool HasUserAccepted(int userId) => _acceptedUsers.Contains(userId);
    }
}
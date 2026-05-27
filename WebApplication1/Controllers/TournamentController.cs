using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication1.Data;
using WebApplication1.Models.Responses;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/tournaments")]
    [Authorize]
    public class TournamentController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IMatchmakingService _matchmaking;
        private readonly ILogger<TournamentController> _logger;

        public TournamentController(AppDbContext db, IMatchmakingService matchmaking, ILogger<TournamentController> logger)
        {
            _db = db;
            _matchmaking = matchmaking;
            _logger = logger;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<TournamentDetailResponse>>> GetTournament(int id)
        {
            int userId = GetUserId();

            var tournament = await _db.Tournaments
                .Include(t => t.Status)
                .Include(t => t.Topic)
                .Include(t => t.TournamentQuestions.OrderBy(tq => tq.QuestionNumber))
                    .ThenInclude(tq => tq.Question)
                        .ThenInclude(q => q.DifficultyType)
                .Include(t => t.TournamentQuestions)
                    .ThenInclude(tq => tq.Question)
                        .ThenInclude(q => q.AnswerOptions)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tournament == null)
                return NotFound(ApiResponse<TournamentDetailResponse>.Fail("Турнир не найден"));

            bool isParticipant = await _db.PlayerAnswers
                .AnyAsync(pa => pa.TournamentId == id && pa.UserId == userId);

            bool tournamentFinished = tournament.StatusId == 3;

            var myAnswers = await _db.PlayerAnswers
                .Where(pa => pa.TournamentId == id && pa.UserId == userId)
                .Select(pa => new { pa.QuestionId, pa.AnswerOptionId })
                .ToListAsync();

            var questionDtos = tournament.TournamentQuestions.Select(tq =>
            {
                var myAnswer = myAnswers.FirstOrDefault(a => a.QuestionId == tq.QuestionId);
                var correctId = tq.Question.AnswerOptions.FirstOrDefault(a => a.IsCorrect)?.Id;

                return new TournamentQuestionResponse
                {
                    QuestionNumber = tq.QuestionNumber,
                    QuestionId = tq.QuestionId,
                    QuestionText = tq.Question.QuestionText,
                    Difficulty = tq.Question.DifficultyType.Name,
                    Points = GetPointsForDifficulty(tq.Question.DifficultyType.Name),
                    Options = tq.Question.AnswerOptions.Select(a => new AnswerOptionResponse
                    {
                        Id = a.Id,
                        Text = a.AnswerText,
                        IsCorrect = tournamentFinished ? a.IsCorrect : null
                    }).ToList(),
                    MyAnswerOptionId = myAnswer?.AnswerOptionId,
                    IsAnswered = myAnswer != null,
                    IsCorrect = tournamentFinished && myAnswer != null
                        ? myAnswer.AnswerOptionId == correctId
                        : null
                };
            }).ToList();

            TournamentResultResponse? resultResponse = null;
            if (tournamentFinished)
                resultResponse = await BuildResult(id);

            return Ok(ApiResponse<TournamentDetailResponse>.Ok(new TournamentDetailResponse
            {
                TournamentId = tournament.Id,
                TopicName = tournament.Topic.Name,
                Status = tournament.Status.Name,
                IsParticipant = isParticipant,
                Questions = questionDtos,
                Result = resultResponse
            }));
        }

        [HttpGet("history")]
        public async Task<ActionResult<ApiResponse<List<TournamentHistoryResponse>>>> GetHistory()
        {
            int userId = GetUserId();

            var tournamentIds = await _db.PlayerAnswers
                .Where(pa => pa.UserId == userId)
                .Select(pa => pa.TournamentId)
                .Distinct()
                .ToListAsync();

            var tournaments = await _db.Tournaments
                .Include(t => t.Status)
                .Include(t => t.Topic)
                .Include(t => t.RatingHistories.Where(rh => rh.UserId == userId))
                .Where(t => tournamentIds.Contains(t.Id))
                .OrderByDescending(t => t.CreatedAt)
                .Take(20)
                .ToListAsync();

            var result = tournaments.Select(t => new TournamentHistoryResponse
            {
                TournamentId = t.Id,
                TopicName = t.Topic.Name,
                Status = t.Status.Name,
                CreatedAt = t.CreatedAt,
                RatingDelta = t.RatingHistories.FirstOrDefault()?.NewRating -
                               t.RatingHistories.FirstOrDefault()?.OldRating
            }).ToList();

            return Ok(ApiResponse<List<TournamentHistoryResponse>>.Ok(result));
        }

        [HttpGet("queue-status")]
        public ActionResult<ApiResponse<QueueStatusResponse>> GetQueueStatus()
        {
            int userId = GetUserId();
            bool inQueue = _matchmaking.IsQueued(userId);
            return Ok(ApiResponse<QueueStatusResponse>.Ok(new QueueStatusResponse { InQueue = inQueue }));
        }

        private async Task<TournamentResultResponse?> BuildResult(int tournamentId)
        {
            var answers = await _db.PlayerAnswers
                .Include(pa => pa.AnswerOption)
                .Include(pa => pa.Question)
                    .ThenInclude(q => q.DifficultyType)
                .Include(pa => pa.Question)
                    .ThenInclude(q => q.AnswerOptions)
                .Where(pa => pa.TournamentId == tournamentId)
                .ToListAsync();

            var playerIds = answers.Select(a => a.UserId).Distinct().ToList();
            if (playerIds.Count < 2) return null;

            const int participationBonus = 5;
            var scores = new Dictionary<int, int>();
            foreach (var pid in playerIds)
            {
                int score = participationBonus;
                foreach (var ans in answers.Where(a => a.UserId == pid))
                {
                    var correctId = ans.Question.AnswerOptions.FirstOrDefault(a => a.IsCorrect)?.Id;
                    if (ans.AnswerOptionId == correctId)
                        score += GetPointsForDifficulty(ans.Question.DifficultyType.Name);
                }
                scores[pid] = score;
            }

            int p1 = playerIds[0], p2 = playerIds[1];
            int s1 = scores[p1], s2 = scores[p2];

            string outcome = s1 == s2 ? "draw" : s1 > s2 ? "win_p1" : "win_p2";

            var ratingChanges = await _db.RatingHistories
                .Where(rh => rh.TournamentId == tournamentId && playerIds.Contains(rh.UserId!.Value))
                .ToListAsync();

            return new TournamentResultResponse
            {
                Outcome = outcome,
                Players = playerIds.Select(pid => new PlayerResultResponse
                {
                    UserId = pid,
                    Score = scores[pid],
                    RatingDelta = ratingChanges.FirstOrDefault(r => r.UserId == pid)
                        .Let(r => r != null ? r.NewRating - r.OldRating : 0)
                }).ToList()
            };
        }

        private static int GetPointsForDifficulty(string difficultyName) =>
            difficultyName.ToLower() switch
            {
                var d when d.Contains("лёгк") || d.Contains("легк") => 5,
                var d when d.Contains("средн") => 10,
                var d when d.Contains("сложн") => 15,
                _ => 5
            };
    }
    file static class ObjectExtensions
    {
        public static TResult? Let<T, TResult>(this T? obj, Func<T, TResult> block) where T : class =>
            obj == null ? default : block(obj);
    }
}
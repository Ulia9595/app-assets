using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication1.Data;
using WebApplication1.Models.Responses;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/learning")]
    [Authorize]
    public class LearningController : ControllerBase
    {
        private readonly AppDbContext _db;

        public LearningController(AppDbContext db)
        {
            _db = db;
        }

        private int? GetCurrentUserId()
        {
            var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(value, out var id) ? id : null;
        }

        [HttpGet("topics")]
        public async Task<ActionResult<ApiResponse<List<TopicResponse>>>> GetTopics()
        {
            var userId = GetCurrentUserId();

            var topics = await _db.Topics
                .OrderBy(t => t.DisplayOrder)
                .Include(t => t.Levels)
                    .ThenInclude(l => l.PracticeTasks)
                .ToListAsync();

            HashSet<int> solvedTaskIds = new();
            if (userId.HasValue)
            {
                solvedTaskIds = (await _db.Solutions
                    .Where(s => s.UserId == userId.Value && s.IsCorrect)
                    .Select(s => s.TaskId)
                    .Distinct()
                    .ToListAsync())
                    .ToHashSet();
            }

            var topicResults = topics.Select(t =>
            {
                var levelCount = t.Levels.Count;
                var completedLevelCount = t.Levels.Count(l =>
                    l.PracticeTasks.Any() &&
                    l.PracticeTasks.All(task => solvedTaskIds.Contains(task.Id)));

                return new
                {
                    Topic = t,
                    LevelCount = levelCount,
                    CompletedLevelCount = completedLevelCount,
                    IsCompleted = levelCount > 0 && completedLevelCount >= levelCount
                };
            }).ToList();

            var result = new List<TopicResponse>();
            for (int i = 0; i < topicResults.Count; i++)
            {
                var t = topicResults[i];
                bool isLocked = i > 0 && !topicResults[i - 1].IsCompleted;

                result.Add(new TopicResponse
                {
                    Id = t.Topic.Id,
                    Name = t.Topic.Name,
                    Description = t.Topic.Description,
                    DisplayOrder = t.Topic.DisplayOrder,
                    LevelCount = t.LevelCount,
                    CompletedLevelCount = t.CompletedLevelCount,
                    IsCompleted = t.IsCompleted,
                    IsLocked = isLocked
                });
            }

            return Ok(ApiResponse<List<TopicResponse>>.Ok(result));
        }

        [HttpGet("topics/{topicId:int}/levels")]
        public async Task<ActionResult<ApiResponse<List<LevelResponse>>>> GetLevels(int topicId)
        {
            var userId = GetCurrentUserId();

            var topicExists = await _db.Topics.AnyAsync(t => t.Id == topicId);
            if (!topicExists)
                return NotFound(ApiResponse<List<LevelResponse>>.Fail("Тема не найдена"));

            var levels = await _db.Levels
                .Where(l => l.TopicId == topicId)
                .OrderBy(l => l.LevelNumber)
                .Include(l => l.PracticeTasks)
                .Include(l => l.Theory)
                .ToListAsync();

            HashSet<int> solvedTaskIds = new();
            if (userId.HasValue)
            {
                var taskIdsInTopic = levels
                    .SelectMany(l => l.PracticeTasks)
                    .Select(t => t.Id)
                    .ToHashSet();

                solvedTaskIds = (await _db.Solutions
                    .Where(s => s.UserId == userId.Value &&
                                s.IsCorrect &&
                                taskIdsInTopic.Contains(s.TaskId))
                    .Select(s => s.TaskId)
                    .Distinct()
                    .ToListAsync())
                    .ToHashSet();
            }

            var result = new List<LevelResponse>();
            for (int i = 0; i < levels.Count; i++)
            {
                var level = levels[i];
                var totalTasks = level.PracticeTasks.Count;
                var completedTasks = level.PracticeTasks
                    .Count(t => solvedTaskIds.Contains(t.Id));

                bool isCompleted = totalTasks > 0 && completedTasks >= totalTasks;
                bool isLocked = i > 0 && !result[i - 1].IsCompleted;

                result.Add(new LevelResponse
                {
                    Id = level.Id,
                    TopicId = level.TopicId,
                    Name = level.Name,
                    LevelNumber = level.LevelNumber,
                    HasTheory = level.Theory != null,
                    TaskCount = totalTasks,
                    IsCompleted = isCompleted,
                    IsLocked = isLocked,
                    TasksCompleted = completedTasks
                });
            }

            return Ok(ApiResponse<List<LevelResponse>>.Ok(result));
        }

        [HttpGet("levels/{levelId:int}")]
        public async Task<ActionResult<ApiResponse<LevelDetailResponse>>> GetLevelDetail(int levelId)
        {
            var userId = GetCurrentUserId();

            var level = await _db.Levels
                .Include(l => l.Theory)
                .Include(l => l.PracticeTasks)
                    .ThenInclude(t => t.DifficultyType)
                .FirstOrDefaultAsync(l => l.Id == levelId);

            if (level == null)
                return NotFound(ApiResponse<LevelDetailResponse>.Fail("Уровень не найден"));

            HashSet<int> solvedTaskIds = new();
            if (userId.HasValue)
            {
                solvedTaskIds = (await _db.Solutions
                    .Where(s => s.UserId == userId.Value &&
                                s.IsCorrect &&
                                s.Task.LevelId == levelId)
                    .Select(s => s.TaskId)
                    .Distinct()
                    .ToListAsync())
                    .ToHashSet();
            }

            var detail = new LevelDetailResponse
            {
                Id = level.Id,
                TopicId = level.TopicId,
                Name = level.Name,
                LevelNumber = level.LevelNumber,
                Theory = level.Theory == null ? null : new TheoryResponse
                {
                    Id = level.Theory.Id,
                    Title = level.Theory.Title,
                    Content = level.Theory.Content
                },
                Tasks = level.PracticeTasks
                    .OrderBy(t => t.DisplayOrder)
                    .Select(t => new PracticeTaskResponse
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Condition = t.Condition,
                        Difficulty = t.DifficultyType?.Name ?? "Обычный",
                        DisplayOrder = t.DisplayOrder,
                        IsSolved = solvedTaskIds.Contains(t.Id)
                    })
                    .ToList()
            };

            return Ok(ApiResponse<LevelDetailResponse>.Ok(detail));
        }
    }
}

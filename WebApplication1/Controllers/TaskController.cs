using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using WebApplication1.Data;
using WebApplication1.Models.Entities;
using WebApplication1.Models.Requests;
using WebApplication1.Models.Responses;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/tasks")]
    [Authorize]
    public class TaskController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public TaskController(
            AppDbContext db,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _db = db;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        private int? GetCurrentUserId()
        {
            var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(value, out var id) ? id : null;
        }

        [HttpGet("{taskId:int}")]
        public async Task<ActionResult<ApiResponse<TaskDetailResponse>>> GetTask(int taskId)
        {
            var userId = GetCurrentUserId();

            var task = await _db.PracticeTasks
                .Include(t => t.DifficultyType)
                .Include(t => t.CheckType)
                .Include(t => t.Hints.OrderBy(h => h.DisplayOrder))
                .Include(t => t.TestCases)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                return NotFound(ApiResponse<TaskDetailResponse>.Fail("Задание не найдено"));

            bool isSolved = false;
            if (userId.HasValue)
                isSolved = await _db.Solutions
                    .AnyAsync(s => s.UserId == userId.Value && s.TaskId == taskId && s.IsCorrect);

            return Ok(ApiResponse<TaskDetailResponse>.Ok(new TaskDetailResponse
            {
                Id = task.Id,
                LevelId = task.LevelId,
                Name = task.Name,
                Condition = task.Condition,
                Difficulty = task.DifficultyType.Name,
                CheckType = task.CheckType.Name,
                IsSolved = isSolved,
                PublicTestCases = task.TestCases
                    .Where(tc => !tc.IsHidden)
                    .Select(tc => new TestCaseResponse
                    {
                        Id = tc.Id,
                        InputData = tc.InputData,
                        ExpectedOutput = tc.ExpectedOutput
                    }).ToList(),
                Hints = task.Hints
                    .Select(h => new HintResponse
                    {
                        Id = h.Id,
                        HintText = h.HintText,
                        DisplayOrder = h.DisplayOrder
                    }).ToList()
            }));
        }

        [HttpPost("{taskId:int}/run")]
        public async Task<ActionResult<ApiResponse<RunResponse>>> RunCode(
            int taskId, [FromBody] RunCodeRequest request)
        {
            var validation = CodeSafetyValidator.Validate(request.Code);
            if (!validation.IsValid)
                return BadRequest(ApiResponse<RunResponse>.Fail(validation.ErrorMessage!));

            var result = await ExecuteCode(request.Code, request.Stdin);

            if (result == null)
                return StatusCode(503, ApiResponse<RunResponse>.Fail(
                    "Компилятор временно недоступен. Попробуйте через несколько секунд."));

            bool isSuccess = result.Run.Code == 0 && string.IsNullOrEmpty(result.Run.Signal);

            var translatedError = !isSuccess
                ? KotlinErrorTranslator.Translate(result.Run.Stderr, result.Run.Stdout, result.Run.Message, result.Run.Signal)
                : "";

            var translatedCompile = !string.IsNullOrEmpty(result.Compile?.Stderr)
                ? KotlinErrorTranslator.Translate(result.Compile.Stderr, "", null, null)
                : "";

            return Ok(ApiResponse<RunResponse>.Ok(new RunResponse
            {
                Stdout = result.Run.Stdout,
                Stderr = translatedError,
                CompileOutput = translatedCompile,
                ExitCode = result.Run.Code ?? -1,
                IsSuccess = isSuccess
            }));
        }

        [HttpPost("{taskId:int}/submit")]
        public async Task<ActionResult<ApiResponse<SubmitResponse>>> SubmitSolution(
            int taskId, [FromBody] RunCodeRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var validation = CodeSafetyValidator.Validate(request.Code);
            if (!validation.IsValid)
                return BadRequest(ApiResponse<SubmitResponse>.Fail(validation.ErrorMessage!));

            var alreadySolved = await _db.Solutions
                .AnyAsync(s => s.UserId == userId.Value && s.TaskId == taskId && s.IsCorrect);

            if (alreadySolved)
                return Ok(ApiResponse<SubmitResponse>.Ok(new SubmitResponse
                {
                    IsCorrect = true,
                    Message = "Задание уже решено",
                    TestResults = new List<TestResultResponse>(),
                    EloGained = 0
                }));

            var task = await _db.PracticeTasks
                .Include(t => t.DifficultyType)
                .Include(t => t.CheckType)
                .Include(t => t.TestCases)
                .Include(t => t.CustomCheckAlgorithm)
                .Include(t => t.Level).ThenInclude(l => l.Topic)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                return NotFound(ApiResponse<SubmitResponse>.Fail("Задание не найдено"));

            var testResults = new List<TestResultResponse>();
            bool allPassed;

            bool isCustomCheck = task.CheckType.Name
                .Contains("Кастом", StringComparison.OrdinalIgnoreCase);

            if (isCustomCheck)
                allPassed = await RunCustomCheck(task, request.Code, testResults);
            else
                allPassed = await RunTestCaseCheck(task, request.Code, testResults);

            _db.Solutions.Add(new Solution
            {
                UserId = userId.Value,
                TaskId = taskId,
                SolutionCode = request.Code,
                IsCorrect = allPassed,
                CreatedAt = DateTime.UtcNow
            });

            int eloGained = 0;
            if (allPassed)
            {
                eloGained = task.DifficultyType.Name.ToLower() switch
                {
                    var d when d.Contains("лёгк") || d.Contains("легк") => 5,
                    var d when d.Contains("средн") => 15,
                    var d when d.Contains("сложн") => 25,
                    _ => 5
                };
                await AddElo(userId.Value, eloGained, taskId: taskId, reason: "Решение задания");
                await CheckAndRewardTopicCompletion(userId.Value, task);
            }

            await _db.SaveChangesAsync();

            return Ok(ApiResponse<SubmitResponse>.Ok(new SubmitResponse
            {
                IsCorrect = allPassed,
                Message = allPassed ? "Все тесты пройдены!" : "Некоторые тесты не пройдены",
                TestResults = testResults,
                EloGained = eloGained,
                PassedCount = testResults.Count(r => r.Passed),
                TotalCount = testResults.Count
            }));
        }

        private async Task<bool> RunTestCaseCheck(
            PracticeTask task, string code, List<TestResultResponse> testResults)
        {
            if (!task.TestCases.Any())
            {
                testResults.Add(new TestResultResponse
                {
                    TestCaseId = 0,
                    Passed = false,
                    IsHidden = false,
                    ErrorMessage = "Для этого задания не настроены тест-кейсы"
                });
                return false;
            }

            bool allPassed = true;
            bool compileErrorReported = false;

            foreach (var testCase in task.TestCases.OrderBy(tc => tc.Id))
            {
                var result = await ExecuteCode(code, testCase.InputData)
                    ?? throw new Exception("Компилятор недоступен");

                bool hasCompileError = !string.IsNullOrWhiteSpace(result.Compile?.Stderr) &&
                    result.Compile.Stderr.Contains("error:");
                if (hasCompileError && !compileErrorReported)
                {
                    compileErrorReported = true;
                    testResults.Add(new TestResultResponse
                    {
                        TestCaseId = testCase.Id,
                        Passed = false,
                        InputData = testCase.IsHidden ? null : testCase.InputData,
                        ExpectedOutput = testCase.IsHidden ? null : testCase.ExpectedOutput,
                        IsHidden = testCase.IsHidden,
                        ErrorMessage = KotlinErrorTranslator.Translate(
                            result.Compile.Stderr, "", null, null)
                    });
                    allPassed = false;
                    break;
                }

                bool runFailed = result.Run.Code != 0 || !string.IsNullOrEmpty(result.Run.Signal);

                if (runFailed)
                {
                    testResults.Add(new TestResultResponse
                    {
                        TestCaseId = testCase.Id,
                        Passed = false,
                        InputData = testCase.IsHidden ? null : testCase.InputData,
                        ExpectedOutput = testCase.IsHidden ? null : testCase.ExpectedOutput,
                        ActualOutput = testCase.IsHidden ? null : result.Run.Stdout,
                        IsHidden = testCase.IsHidden,
                        ErrorMessage = KotlinErrorTranslator.Translate(
                            result.Run.Stderr, result.Run.Stdout, result.Run.Message)
                    });
                    allPassed = false;
                    continue;
                }

                var expected = testCase.ExpectedOutput.Trim();
                var actual = result.Run.Stdout.Trim();
                var passed = expected == actual;
                if (!passed) allPassed = false;

                testResults.Add(new TestResultResponse
                {
                    TestCaseId = testCase.Id,
                    Passed = passed,
                    InputData = testCase.IsHidden ? null : testCase.InputData,
                    ExpectedOutput = testCase.IsHidden ? null : testCase.ExpectedOutput,
                    ActualOutput = testCase.IsHidden ? null : actual,
                    IsHidden = testCase.IsHidden
                });
            }

            return allPassed;
        }

        private async Task<bool> RunCustomCheck(
            PracticeTask task, string code, List<TestResultResponse> testResults)
        {
            if (task.CustomCheckAlgorithm == null)
            {
                testResults.Add(new TestResultResponse
                {
                    TestCaseId = 0,
                    Passed = false,
                    IsHidden = false,
                    ErrorMessage = "Алгоритм проверки не настроен"
                });
                return false;
            }

            var result = await ExecuteCode(code, stdin: null)
                ?? throw new Exception("Компилятор недоступен");

            bool runFailed = result.Run.Code != 0 || !string.IsNullOrEmpty(result.Run.Signal);

            if (runFailed)
            {
                testResults.Add(new TestResultResponse
                {
                    TestCaseId = 0,
                    Passed = false,
                    IsHidden = false,
                    ErrorMessage = KotlinErrorTranslator.Translate(
                        result.Run.Stderr, result.Run.Stdout, result.Run.Message)
                });
                return false;
            }

            bool passed = EvaluateCustomAlgorithm(task.CustomCheckAlgorithm.AlgorithmCode, result.Run.Stdout);

            testResults.Add(new TestResultResponse
            {
                TestCaseId = 0,
                Passed = passed,
                IsHidden = false,
                ActualOutput = result.Run.Stdout,
                ErrorMessage = passed ? null : "Вывод программы не соответствует ожидаемому результату"
            });

            return passed;
        }

        private static bool EvaluateCustomAlgorithm(string algorithmCode, string stdout)
        {
            try
            {
                var lines = stdout.Split('\n');
                var trimmedStdout = stdout.Trim();

                var m = System.Text.RegularExpressions.Regex.Match(
                    algorithmCode, @"lines\[(\d+)\]\.Trim\(\)\s*==\s*""([^""]*)""");
                if (m.Success)
                {
                    int idx = int.Parse(m.Groups[1].Value);
                    return idx < lines.Length && lines[idx].Trim() == m.Groups[2].Value;
                }

                m = System.Text.RegularExpressions.Regex.Match(
                    algorithmCode, @"return\s+lines\[(\d+)\]\.Trim\(\)\s*==\s*""([^""]*)""");
                if (m.Success)
                {
                    int idx = int.Parse(m.Groups[1].Value);
                    return idx < lines.Length && lines[idx].Trim() == m.Groups[2].Value;
                }

                m = System.Text.RegularExpressions.Regex.Match(
                    algorithmCode, @"stdout\.Trim\(\)\s*==\s*""([^""]*)""");
                if (m.Success) return trimmedStdout == m.Groups[1].Value;

                m = System.Text.RegularExpressions.Regex.Match(
                    algorithmCode, @"stdout\.Contains\(""([^""]*)""\)");
                if (m.Success) return stdout.Contains(m.Groups[1].Value);

                return false;
            }
            catch { return false; }
        }

        private async Task CheckAndRewardTopicCompletion(int userId, PracticeTask task)
        {
            var topic = task.Level.Topic;
            var levels = await _db.Levels
                .Include(l => l.PracticeTasks)
                .Where(l => l.TopicId == topic.Id)
                .ToListAsync();

            if (!levels.Any()) return;

            var topicTaskIds = levels.SelectMany(l => l.PracticeTasks).Select(t => t.Id).ToHashSet();
            var solvedTaskIds = (await _db.Solutions
                .Where(s => s.UserId == userId && s.IsCorrect && topicTaskIds.Contains(s.TaskId))
                .Select(s => s.TaskId).Distinct().ToListAsync()).ToHashSet();

            bool topicCompleted = levels.All(l =>
                l.PracticeTasks.Any() && l.PracticeTasks.All(t => solvedTaskIds.Contains(t.Id)));
            if (!topicCompleted) return;

            var topicBonusReason = await _db.RatingChangeReasons
                .FirstOrDefaultAsync(r => r.Name == "Прохождение темы");
            if (topicBonusReason == null) return;

            bool alreadyRewarded = await _db.RatingHistories
                .AnyAsync(rh => rh.UserId == userId && rh.TopicId == topic.Id && rh.ReasonId == topicBonusReason.Id);
            if (alreadyRewarded) return;

            await AddElo(userId, 100, topicId: topic.Id, reason: "Прохождение темы");
        }

        private async Task AddElo(int userId, int points, int? taskId = null,
            int? topicId = null, string reason = "")
        {
            var userRating = await _db.UserRatings.FirstOrDefaultAsync(r => r.UserId == userId);
            if (userRating == null)
            {
                userRating = new UserRating { UserId = userId, CurrentRating = 500 };
                _db.UserRatings.Add(userRating);
                await _db.SaveChangesAsync();
            }

            var oldRating = userRating.CurrentRating;
            userRating.CurrentRating += points;
            userRating.LastUpdated = DateTime.UtcNow;

            var ratingReason = await _db.RatingChangeReasons
                .FirstOrDefaultAsync(r => r.Name == reason)
                ?? await _db.RatingChangeReasons.FirstAsync();

            _db.RatingHistories.Add(new RatingHistory
            {
                UserId = userId,
                OldRating = oldRating,
                NewRating = userRating.CurrentRating,
                ReasonId = ratingReason.Id,
                TaskId = taskId,
                TopicId = topicId,
                CreatedAt = DateTime.UtcNow
            });
        }

        private async Task<PistonResponse?> ExecuteCode(string code, string? stdin = null)
        {
            try
            {
                var pistonUrl = _configuration["PistonApi:BaseUrl"] ?? "http://localhost:2000";
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(60);

                var payload = new
                {
                    language = "kotlin",
                    version = "1.8.20",
                    files = new[] { new { content = code } },
                    stdin = stdin ?? ""
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{pistonUrl}/api/v2/execute", content);
                if (!response.IsSuccessStatusCode) return null;

                var body = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PistonResponse>(body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch { return null; }
        }
    }

    public class PistonResponse
    {
        public PistonRunResult Compile { get; set; } = new();
        public PistonRunResult Run { get; set; } = new();
    }

    public class PistonRunResult
    {
        public string Stdout { get; set; } = "";
        public string Stderr { get; set; } = "";
        public int? Code { get; set; }
        public string? Signal { get; set; }
        public string? Message { get; set; }
    }
}
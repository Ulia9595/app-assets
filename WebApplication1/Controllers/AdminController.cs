using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SendGrid.Helpers.Mail;
using System.Security.Claims;
using WebApplication1.Data;
using WebApplication1.Models.Entities;
using WebApplication1.Models.Requests;
using WebApplication1.Models.ViewModels;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [Route("admin")]
    public class AdminController : Controller
    {
        private readonly AuthService _authService;
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public AdminController(AuthService authService, AppDbContext context, INotificationService notificationService)
        {
            _authService = authService;
            _context = context;
            _notificationService = notificationService;
        }

        [HttpGet("login")]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View("~/Views/Account/AdminLogin.cshtml");
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            var result = await _authService.LoginAsync(new LoginRequest
            {
                Email = email,
                Password = password
            });

            if (!result.Success || result.Data == null)
            {
                ViewBag.Error = result.Error ?? "Неверный email или пароль";
                return View("~/Views/Account/AdminLogin.cshtml");
            }

            if (result.Data.User.Role != "admin")
            {
                ViewBag.Error = "Доступ разрешён только администраторам";
                return View("~/Views/Account/AdminLogin.cshtml");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, result.Data.User.Uid),
                new Claim(ClaimTypes.Email, result.Data.User.Email),
                new Claim(ClaimTypes.Role, result.Data.User.Role),
                new Claim("uid", result.Data.User.Uid)
            };

            var identity = new ClaimsIdentity(claims, "AdminCookie");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                "AdminCookie",
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });

            return RedirectToAction("Dashboard", "Admin");
        }

        [HttpPost("logout")]
        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("AdminCookie");
            Response.Cookies.Delete("GameAuth.Admin");

            Response.Cookies.Delete("jwt");

            HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            return RedirectToAction("Login", "Admin");
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var playerUsersQuery = _context.Users
                .Include(u => u.Role)
                .Where(u => u.Role.Code == "player");

            var playerUserIds = await playerUsersQuery
                .Select(u => u.Id)
                .ToListAsync();

            var ratings = await _context.UserRatings
                .Where(r => playerUserIds.Contains(r.UserId))
                .Select(r => r.CurrentRating)
                .ToListAsync();

            var model = new AdminDashboardViewModel
            {
                PlayersCount = await playerUsersQuery.CountAsync(),

                RolesCount = await _context.Roles.CountAsync(),
                AvatarsCount = await _context.AvailableAvatars.CountAsync(),

                OtpCodesCount = await _context.OtpCodes.CountAsync(o =>
                    (o.UserId.HasValue && playerUserIds.Contains(o.UserId.Value))
                    || playerUsersQuery.Any(u => u.Email == o.Email)
                ),

                 ActiveOtpCodesCount = await _context.OtpCodes.CountAsync(o =>
                    !o.Used &&
                    o.ExpiresAt > DateTime.UtcNow &&
                    (
                        (o.UserId.HasValue && playerUserIds.Contains(o.UserId.Value))
                        || playerUsersQuery.Any(u => u.Email == o.Email)
                    )
                ),

                PasswordResetTokensCount = await _context.PasswordResetTokens
                    .CountAsync(t => playerUserIds.Contains(t.UserId)),

                PasswordResetAttemptsCount = await _context.PasswordResetAttempts.CountAsync(a =>
                    playerUsersQuery.Any(u => u.Email == a.Email)
                ),

                UserRatingsCount = await _context.UserRatings
                    .CountAsync(r => playerUserIds.Contains(r.UserId)),

                RatingHistoryCount = await _context.RatingHistories
                    .CountAsync(h => h.UserId.HasValue && playerUserIds.Contains(h.UserId.Value)),

                AverageRating = ratings.Any() ? Math.Round(ratings.Average(), 1) : 0,
                MaxRating = ratings.Any() ? ratings.Max() : 0,
                MinRating = ratings.Any() ? ratings.Min() : 0
            };

            model.LearningContent = await BuildLearningModelAsync();
            model.TopicsCount = model.LearningContent.Topics.Count;
            model.LevelsCount = model.LearningContent.Topics.Sum(t => t.Levels.Count);

            model.TasksContent = await BuildTasksModelAsync();
            model.TasksCount = model.TasksContent.Topics.Sum(t =>
                                     t.Levels.Sum(l => l.Tasks.Count));

            model.PvpContent = await BuildPvpModelAsync();
            model.TournamentsCount = model.PvpContent.Tournaments.Count;

            model.UsersContent = await BuildUsersModelAsync();
            model.AnalyticsContent = await BuildAnalyticsModelAsync();

            return View("~/Views/Admin/Dashboard.cshtml", model);
        }

        private async Task<LearningContentViewModel> BuildLearningModelAsync()
        {
            var topics = await _context.Topics
                .Include(t => t.Levels)
                    .ThenInclude(l => l.Theory)
                .OrderBy(t => t.DisplayOrder)
                .ToListAsync();

            return new LearningContentViewModel
            {
                Topics = topics.Select(t => new TopicWithLevelsDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description,
                    DisplayOrder = t.DisplayOrder,
                    Levels = t.Levels.OrderBy(l => l.LevelNumber).Select(l => new LevelWithTheoryDto
                    {
                        Id = l.Id,
                        TopicId = l.TopicId,
                        Name = l.Name,
                        LevelNumber = l.LevelNumber,
                        Theory = l.Theory == null ? null : new TheoryDto
                        {
                            Id = l.Theory.Id,
                            LevelId = l.Theory.LevelId,
                            Title = l.Theory.Title,
                            Content = l.Theory.Content
                        }
                    }).ToList()
                }).ToList()
            };
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpGet("learning")]
        public async Task<IActionResult> Learning()
        {
            var model = await BuildLearningModelAsync();
            return PartialView("~/Views/Admin/Sections/_LearningPanel.cshtml", model);
        }

        private async Task<TasksContentViewModel> BuildTasksModelAsync()
        {
            var topics = await _context.Topics
                .Include(t => t.Levels)
                    .ThenInclude(l => l.PracticeTasks)
                        .ThenInclude(pt => pt.DifficultyType)
                .Include(t => t.Levels)
                    .ThenInclude(l => l.PracticeTasks)
                        .ThenInclude(pt => pt.CheckType)
                .Include(t => t.Levels)
                    .ThenInclude(l => l.PracticeTasks)
                        .ThenInclude(pt => pt.TestCases)
                .Include(t => t.Levels)
                    .ThenInclude(l => l.PracticeTasks)
                        .ThenInclude(pt => pt.Hints)
                .Include(t => t.Levels)
                    .ThenInclude(l => l.PracticeTasks)
                        .ThenInclude(pt => pt.CustomCheckAlgorithm)
                .OrderBy(t => t.DisplayOrder)
                .ToListAsync();

            var difficultyTypes = await _context.DifficultyTypes
                .OrderBy(d => d.Id)
                .Select(d => new DifficultyTypeDto { Id = d.Id, Name = d.Name })
                .ToListAsync();

            var checkTypes = await _context.CheckTypes
                .OrderBy(c => c.Id)
                .Select(c => new CheckTypeDto { Id = c.Id, Name = c.Name })
                .ToListAsync();

            return new TasksContentViewModel
            {
                DifficultyTypes = difficultyTypes,
                CheckTypes = checkTypes,
                Topics = topics.Select(t => new TaskTopicDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Levels = t.Levels.OrderBy(l => l.LevelNumber).Select(l => new TaskLevelDto
                    {
                        Id = l.Id,
                        Name = l.Name,
                        LevelNumber = l.LevelNumber,
                        TopicId = t.Id,
                        Tasks = l.PracticeTasks.OrderBy(pt => pt.DisplayOrder).Select(pt => new PracticeTaskDto
                        {
                            Id = pt.Id,
                            LevelId = pt.LevelId,
                            Name = pt.Name,
                            Condition = pt.Condition,
                            DifficultyTypeId = pt.DifficultyTypeId,
                            DifficultyTypeName = pt.DifficultyType.Name,
                            CheckTypeId = pt.CheckTypeId,
                            CheckTypeName = pt.CheckType.Name,
                            DisplayOrder = pt.DisplayOrder,
                            TestCases = pt.TestCases.Select(tc => new TestCaseDto
                            {
                                Id = tc.Id,
                                TaskId = tc.TaskId,
                                InputData = tc.InputData,
                                ExpectedOutput = tc.ExpectedOutput,
                                IsHidden = tc.IsHidden
                            }).ToList(),
                            Hints = pt.Hints.OrderBy(h => h.DisplayOrder).Select(h => new HintDto
                            {
                                Id = h.Id,
                                TaskId = h.TaskId,
                                HintText = h.HintText,
                                DisplayOrder = h.DisplayOrder
                            }).ToList(),
                            CustomCheckAlgorithm = pt.CustomCheckAlgorithm == null ? null : new CustomCheckAlgorithmDto
                            {
                                Id = pt.CustomCheckAlgorithm.Id,
                                TaskId = pt.CustomCheckAlgorithm.TaskId,
                                AlgorithmCode = pt.CustomCheckAlgorithm.AlgorithmCode
                            }
                        }).ToList()
                    }).ToList()
                }).ToList()
            };
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpGet("tasks")]
        public async Task<IActionResult> Tasks()
        {
            var model = await BuildTasksModelAsync();
            return PartialView("~/Views/Admin/Sections/_TasksPanel.cshtml", model);
        }

        private async Task<PvpContentViewModel> BuildPvpModelAsync()
        {
            var topicsDict = await _context.Topics
                .ToDictionaryAsync(t => t.Id, t => t.Name);

            var statuses = await _context.TournamentStatuses
                .OrderBy(s => s.Id)
                .Select(s => new PvpStatusDto { Id = s.Id, Name = s.Name })
                .ToListAsync();

            var topics = await _context.Topics
                .OrderBy(t => t.DisplayOrder)
                .Select(t => new PvpTopicDto { Id = t.Id, Name = t.Name })
                .ToListAsync();

            var difficulties = await _context.DifficultyTypes
                .OrderBy(d => d.Id)
                .Select(d => new PvpDifficultyDto { Id = d.Id, Name = d.Name })
                .ToListAsync();

            var tournaments = await _context.Tournaments
                .Include(t => t.Status)
                .Include(t => t.TournamentQuestions)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            var tournamentQuestionIds = tournaments
                .SelectMany(t => t.TournamentQuestions.Select(tq => tq.QuestionId))
                .Distinct()
                .ToList();

            var tournamentQuestionsDict = tournamentQuestionIds.Any()
                ? await _context.Questions
                    .Include(q => q.DifficultyType)
                    .Where(q => tournamentQuestionIds.Contains(q.Id))
                    .ToDictionaryAsync(q => q.Id)
                : new Dictionary<int, Question>();

            var questions = await _context.Questions
                .Include(q => q.DifficultyType)
                .Include(q => q.AnswerOptions)
                .OrderBy(q => q.TopicId)
                .ThenBy(q => q.Id)
                .ToListAsync();

            return new PvpContentViewModel
            {
                Topics = topics,
                Statuses = statuses,
                DifficultyTypes = difficulties,
                Tournaments = tournaments.Select(t => new PvpTournamentDto
                {
                    Id = t.Id,
                    TopicId = t.TopicId,
                    TopicName = topicsDict.TryGetValue(t.TopicId, out var tt) ? tt : "",
                    StatusId = t.StatusId,
                    StatusName = t.Status.Name,
                    MinRating = t.MinRating,
                    MaxRating = t.MaxRating,
                    CreatedAt = t.CreatedAt,
                    Questions = t.TournamentQuestions
                        .OrderBy(tq => tq.QuestionNumber)
                        .Select(tq => new PvpTournamentQuestionDto
                        {
                            TournamentId = tq.TournamentId,
                            QuestionId = tq.QuestionId,
                            QuestionNumber = tq.QuestionNumber,
                            QuestionText = tournamentQuestionsDict.TryGetValue(tq.QuestionId, out var q)
                                ? q.QuestionText : "",
                            DifficultyTypeName = tournamentQuestionsDict.TryGetValue(tq.QuestionId, out var q2)
                                ? q2.DifficultyType.Name : ""
                        }).ToList()
                }).ToList(),
                Questions = questions.Select(q => new PvpQuestionDto
                {
                    Id = q.Id,
                    TopicId = q.TopicId,
                    TopicName = topicsDict.TryGetValue(q.TopicId, out var tn) ? tn : "",
                    DifficultyTypeId = q.DifficultyTypeId,
                    DifficultyTypeName = q.DifficultyType.Name,
                    QuestionText = q.QuestionText,
                    AnswerOptions = q.AnswerOptions.Select(a => new PvpAnswerOptionDto
                    {
                        Id = a.Id,
                        QuestionId = a.QuestionId,
                        AnswerText = a.AnswerText,
                        IsCorrect = a.IsCorrect
                    }).ToList()
                }).ToList()
            };
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpGet("pvp")]
        public async Task<IActionResult> Pvp()
        {
            var model = await BuildPvpModelAsync();
            return PartialView("~/Views/Admin/Sections/_PvpPanel.cshtml", model);
        }

        private const int MinTasksPerLevel = 3;
        private const int MaxTasksPerLevel = 5;
        private const int MaxEasyTasks = 3;
        private const int MinEasyTasks = 1;
        private const string EasyDifficulty = "Лёгкий";
        private const string MediumDifficulty = "Средний";
        private const string HardDifficulty = "Сложный";
        private const string CustomCheckTypeName = "Кастомная проверка";
        private const string TemplateCheckTypeName = "Шаблонная проверка";

        private static string NormalizeTestText(string text)
        {
            return text
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .TrimEnd();
        }

        private static string? NormalizeTestInput(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            return NormalizeTestText(text);
        }

        private async Task<string?> ValidateLevelTasksCompositionAsync(int levelId, int? excludeTaskId, string? newDifficultyName)
        {
            var existingTasks = await _context.PracticeTasks
                .Include(t => t.DifficultyType)
                .Where(t => t.LevelId == levelId && (excludeTaskId == null || t.Id != excludeTaskId))
                .ToListAsync();

            var allDifficulties = existingTasks
                .Select(t => t.DifficultyType.Name)
                .ToList();

            if (newDifficultyName != null)
                allDifficulties.Add(newDifficultyName);

            int total = allDifficulties.Count;
            int easy = allDifficulties.Count(d => d == EasyDifficulty);
            int medium = allDifficulties.Count(d => d == MediumDifficulty);
            int hard = allDifficulties.Count(d => d == HardDifficulty);

            if (total > MaxTasksPerLevel)
                return $"Уровень не может содержать более {MaxTasksPerLevel} заданий (сейчас будет {total})";

            if (easy > MaxEasyTasks)
                return $"Лёгких заданий не может быть более {MaxEasyTasks} (сейчас будет {easy})";

            if (medium > 1)
                return "Заданий средней сложности должно быть ровно 1";

            if (hard > 1)
                return "Заданий повышенной сложности должно быть ровно 1";

            if (newDifficultyName == null && total < MinTasksPerLevel && total > 0)
                return $"Уровень должен содержать не менее {MinTasksPerLevel} заданий";

            return null;
        }

        private async Task<UsersContentViewModel> BuildUsersModelAsync()
        {
            var totalTopics = await _context.Topics.CountAsync();
            var totalLevels = await _context.Levels.CountAsync();

            var users = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Rating)
                .Where(u => u.Role.Code == "player")
                .OrderByDescending(u => u.Rating != null ? u.Rating.CurrentRating : 0)
                .ToListAsync();

            var userIds = users.Select(u => u.Id).ToList();

            var solvedDict = await _context.Solutions
                .Where(s => userIds.Contains(s.UserId) && s.IsCorrect)
                .GroupBy(s => s.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            var topicCompletions = await _context.RatingHistories
                .Where(h => h.UserId.HasValue
                    && userIds.Contains(h.UserId.Value)
                    && h.TopicId.HasValue
                    && h.TaskId == null)
                .GroupBy(h => h.UserId!.Value)
                .Select(g => new { UserId = g.Key, Count = g.Select(x => x.TopicId).Distinct().Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            var levelCompletions = await _context.RatingHistories
                .Where(h => h.UserId.HasValue
                    && userIds.Contains(h.UserId.Value)
                    && h.TaskId.HasValue)
                .GroupBy(h => h.UserId!.Value)
                .Select(g => new { UserId = g.Key, Count = g.Select(x => x.TaskId).Distinct().Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            var tournamentsDict = await _context.PlayerAnswers
                .Where(pa => userIds.Contains(pa.UserId))
                .GroupBy(pa => pa.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Select(x => x.TournamentId).Distinct().Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            return new UsersContentViewModel
            {
                TotalTopics = totalTopics,
                TotalLevels = totalLevels,
                Users = users.Select(u => new UserRowDto
                {
                    Id = u.Id,
                    DisplayName = !string.IsNullOrWhiteSpace(u.Name) ? u.Name : $"Игрок #{u.Id}",
                    CurrentRating = u.Rating?.CurrentRating ?? 500,
                    CreatedAt = u.CreatedAt,
                    SolvedTasksCount = solvedDict.TryGetValue(u.Id, out var s) ? s : 0,
                    CompletedTopicsCount = topicCompletions.TryGetValue(u.Id, out var t) ? t : 0,
                    CompletedLevelsCount = levelCompletions.TryGetValue(u.Id, out var l) ? l : 0,
                    TournamentsPlayedCount = tournamentsDict.TryGetValue(u.Id, out var tr) ? tr : 0
                }).ToList()
            };
        }

        private async Task<AnalyticsViewModel> BuildAnalyticsModelAsync()
        {
            var ratings = await _context.UserRatings
                .Select(r => r.CurrentRating)
                .ToListAsync();

            var eloRanges = new[]
            {
                (label: "0–200",   min: 0,    max: 200),
                (label: "200–400", min: 200,  max: 400),
                (label: "400–600", min: 400,  max: 600),
                (label: "600–800", min: 600,  max: 800),
                (label: "800–1000",min: 800,  max: 1000),
                (label: "1000+",   min: 1000, max: int.MaxValue),
            };

            var eloDistribution = eloRanges.Select(r => new EloRangeDto
            {
                Label = r.label,
                Count = ratings.Count(x => x >= r.min && x < r.max)
            }).ToList();

            var progressData = await _context.RatingHistories
                .Where(h => h.UserId.HasValue && (h.TopicId.HasValue || h.TaskId.HasValue))
                .Select(h => new
                {
                    Month = h.CreatedAt.ToString("yyyy-MM"),
                    IsTopicCompletion = h.TopicId.HasValue && h.TaskId == null,
                    IsLevelCompletion = h.TaskId.HasValue
                })
                .ToListAsync();

            var progressGrouped = progressData
                .GroupBy(x => x.Month)
                .OrderBy(g => g.Key)
                .Select(g => new ProgressPointDto
                {
                    Month = g.Key,
                    TopicsCompleted = g.Count(x => x.IsTopicCompletion),
                    LevelsCompleted = g.Count(x => x.IsLevelCompletion)
                })
                .ToList();

            var solutionsCorrect = await _context.Solutions.CountAsync(s => s.IsCorrect);
            var solutionsWrong = await _context.Solutions.CountAsync(s => !s.IsCorrect);

            var topicsDict = await _context.Topics
                .ToDictionaryAsync(t => t.Id, t => t.Name);

            var pvpData = await _context.PlayerAnswers
                .Include(pa => pa.AnswerOption)
                .Include(pa => pa.Tournament)
                .GroupBy(pa => pa.TournamentId)
                .Select(g => new
                {
                    TournamentId = g.Key,
                    TopicId = g.First().Tournament.TopicId,
                    Correct = g.Count(x => x.AnswerOption.IsCorrect),
                    Wrong = g.Count(x => !x.AnswerOption.IsCorrect)
                })
                .ToListAsync();

            var pvpStats = pvpData.Select(p => new PvpStatsDto
            {
                TournamentId = p.TournamentId,
                TopicName = topicsDict.TryGetValue(p.TopicId, out var tn) ? tn : $"Турнир #{p.TournamentId}",
                CorrectAnswers = p.Correct,
                WrongAnswers = p.Wrong
            }).ToList();

            var playerUsers = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Role.Code == "player")
                .Select(u => new { u.Id, u.Name })
                .ToListAsync();

            var playerIds = playerUsers.Select(u => u.Id).ToList();

            var allProgress = await _context.RatingHistories
                .Where(h => h.UserId.HasValue
                    && playerIds.Contains(h.UserId.Value)
                    && (h.TopicId.HasValue || h.TaskId.HasValue))
                .Select(h => new
                {
                    UserId = h.UserId!.Value,
                    Month = h.CreatedAt.ToString("yyyy-MM"),
                    IsTopic = h.TopicId.HasValue && h.TaskId == null,
                    IsLevel = h.TaskId.HasValue
                })
                .ToListAsync();

            var allSolutions = await _context.Solutions
                .Where(s => playerIds.Contains(s.UserId))
                .Select(s => new { s.UserId, s.IsCorrect })
                .ToListAsync();

            var allAnswers = await _context.PlayerAnswers
                .Include(pa => pa.AnswerOption)
                .Include(pa => pa.Tournament)
                .Where(pa => playerIds.Contains(pa.UserId))
                .Select(pa => new
                {
                    pa.UserId,
                    pa.TournamentId,
                    TopicId = pa.Tournament.TopicId,
                    IsCorrect = pa.AnswerOption.IsCorrect
                })
                .ToListAsync();

            var perUserAnalytics = playerUsers.Select(u =>
            {
                var displayName = !string.IsNullOrWhiteSpace(u.Name) ? u.Name : $"Игрок #{u.Id}";

                var progress = allProgress
                    .Where(p => p.UserId == u.Id)
                    .GroupBy(p => p.Month)
                    .OrderBy(g => g.Key)
                    .Select(g => new ProgressPointDto
                    {
                        Month = g.Key,
                        TopicsCompleted = g.Count(x => x.IsTopic),
                        LevelsCompleted = g.Count(x => x.IsLevel)
                    }).ToList();

                var pvp = allAnswers
                    .Where(pa => pa.UserId == u.Id)
                    .GroupBy(pa => pa.TournamentId)
                    .Select(g => new PvpStatsDto
                    {
                        TournamentId = g.Key,
                        TopicName = topicsDict.TryGetValue(g.First().TopicId, out var tn) ? tn : $"Турнир #{g.Key}",
                        CorrectAnswers = g.Count(x => x.IsCorrect),
                        WrongAnswers = g.Count(x => !x.IsCorrect)
                    }).ToList();

                return new UserAnalyticsDto
                {
                    UserId = u.Id,
                    DisplayName = displayName,
                    Progress = progress,
                    SolutionsCorrect = allSolutions.Count(s => s.UserId == u.Id && s.IsCorrect),
                    SolutionsWrong = allSolutions.Count(s => s.UserId == u.Id && !s.IsCorrect),
                    PvpStats = pvp
                };
            }).ToList();

            return new AnalyticsViewModel
            {
                EloDistribution = eloDistribution,
                LearningProgress = progressGrouped,
                SolutionsCorrect = solutionsCorrect,
                SolutionsWrong = solutionsWrong,
                PvpStats = pvpStats,
                PerUserAnalytics = perUserAnalytics,
                PlayerNames = perUserAnalytics.Select(u => u.DisplayName).ToList()
            };
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpGet("users")]
        public async Task<IActionResult> Users()
        {
            var model = await BuildUsersModelAsync();
            return PartialView("~/Views/Admin/Sections/_UsersPanel.cshtml", model);
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpGet("analytics")]
        public async Task<IActionResult> Analytics()
        {
            var model = await BuildAnalyticsModelAsync();
            return PartialView("~/Views/Admin/Sections/_AnalyticsPanel.cshtml", model);
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpPost("topics/create")]
        public async Task<IActionResult> CreateTopic([FromForm] CreateTopicRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return BadRequest(new { error = "Название темы обязательно" });

            var toShift = await _context.Topics
                .Where(t => t.DisplayOrder >= req.DisplayOrder)
                .ToListAsync();
            foreach (var t in toShift) t.DisplayOrder++;

            var topic = new Topic
            {
                Name = req.Name.Trim(),
                Description = req.Description?.Trim(),
                DisplayOrder = req.DisplayOrder
            };

            _context.Topics.Add(topic);
            await _context.SaveChangesAsync();
            await _notificationService.SendNotificationToAllAsync(
                "Новая тема",
                $"Добавлена новая тема: {topic.Name}",
                NotificationType.TopicAdded,
                topic.Id
            );
            return Ok(new { id = topic.Id, message = "Тема успешно создана" });
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpPost("topics/update")]
        public async Task<IActionResult> UpdateTopic([FromForm] UpdateTopicRequest req)
        {
            var topic = await _context.Topics.FindAsync(req.Id);
            if (topic == null) return NotFound(new { error = "Тема не найдена" });
            if (string.IsNullOrWhiteSpace(req.Name))
                return BadRequest(new { error = "Название темы обязательно" });

            if (topic.DisplayOrder != req.DisplayOrder)
            {
                var conflict = await _context.Topics
                    .AnyAsync(t => t.DisplayOrder == req.DisplayOrder && t.Id != req.Id);

                if (conflict)
                {
                    var toShift = await _context.Topics
                        .Where(t => t.DisplayOrder >= req.DisplayOrder && t.Id != req.Id)
                        .ToListAsync();
                    foreach (var t in toShift) t.DisplayOrder++;
                }
            }

            topic.Name = req.Name.Trim();
            topic.Description = req.Description?.Trim();
            topic.DisplayOrder = req.DisplayOrder;

            await _context.SaveChangesAsync();
            await _notificationService.SendNotificationToAllAsync(
                "Тема обновлена",
                $"Обновлена тема: {topic.Name}",
                NotificationType.TopicUpdated,
                topic.Id
            );
            return Ok(new { message = "Тема успешно обновлена" });
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpPost("topics/delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTopic(int id)
        {
            var topic = await _context.Topics
                .Include(t => t.Levels)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (topic == null) return NotFound(new { error = "Тема не найдена" });
            if (topic.Levels.Any())
                return BadRequest(new { error = "Нельзя удалить тему, в которой есть уровни" });

            var deletedOrder = topic.DisplayOrder;

            var toShift = await _context.Topics
                .Where(t => t.DisplayOrder > deletedOrder && t.Id != id)
                .ToListAsync();

            _context.Topics.Remove(topic);
            foreach (var t in toShift) t.DisplayOrder--;

            await _context.SaveChangesAsync();
            await _notificationService.SendNotificationToAllAsync(
                "Тема удалена",
                $"Удалена тема: {topic.Name}",
                NotificationType.TopicDeleted,
                id
            );
            return Ok(new { message = "Тема успешно удалена" });
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpPost("levels/create")]
        public async Task<IActionResult> CreateLevel([FromForm] CreateLevelRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return BadRequest(new { error = "Название уровня обязательно" });

            var topicExists = await _context.Topics.AnyAsync(t => t.Id == req.TopicId);
            if (!topicExists) return NotFound(new { error = "Тема не найдена" });

            var toShift = await _context.Levels
                .Where(l => l.TopicId == req.TopicId && l.LevelNumber >= req.LevelNumber)
                .ToListAsync();
            foreach (var l in toShift) l.LevelNumber++;

            var level = new Level
            {
                TopicId = req.TopicId,
                Name = req.Name.Trim(),
                LevelNumber = req.LevelNumber
            };

            _context.Levels.Add(level);
            await _context.SaveChangesAsync();
            var topicName = await _context.Topics
                .Where(t => t.Id == req.TopicId)
                .Select(t => t.Name)
                .FirstOrDefaultAsync() ?? "неизвестная тема";

            await _notificationService.SendNotificationToAllAsync(
                "Новый уровень",
                $"Добавлен новый уровень: {level.Name} в теме \"{topicName}\"",
                NotificationType.LevelAdded,
                level.Id
            );
            return Ok(new { id = level.Id, message = "Уровень успешно создан" });
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpPost("levels/update")]
        public async Task<IActionResult> UpdateLevel([FromForm] UpdateLevelRequest req)
        {
            var level = await _context.Levels.FindAsync(req.Id);
            if (level == null) return NotFound(new { error = "Уровень не найден" });
            if (string.IsNullOrWhiteSpace(req.Name))
                return BadRequest(new { error = "Название уровня обязательно" });

            var topicExists = await _context.Topics.AnyAsync(t => t.Id == req.TopicId);
            if (!topicExists) return NotFound(new { error = "Тема не найдена" });

            if (level.LevelNumber != req.LevelNumber || level.TopicId != req.TopicId)
            {
                var conflict = await _context.Levels
                    .AnyAsync(l => l.TopicId == req.TopicId && l.LevelNumber == req.LevelNumber && l.Id != req.Id);

                if (conflict)
                {
                    var toShift = await _context.Levels
                        .Where(l => l.TopicId == req.TopicId && l.LevelNumber >= req.LevelNumber && l.Id != req.Id)
                        .ToListAsync();
                    foreach (var l in toShift) l.LevelNumber++;
                }
            }

            level.TopicId = req.TopicId;
            level.Name = req.Name.Trim();
            level.LevelNumber = req.LevelNumber;

            await _context.SaveChangesAsync();
            await _notificationService.SendNotificationToAllAsync(
                "Уровень обновлён",
                $"Обновлён уровень: {level.Name}",
                NotificationType.LevelUpdated,
                level.Id
            );
            return Ok(new { message = "Уровень успешно обновлён" });
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpPost("levels/delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLevel(int id)
        {
            var level = await _context.Levels
                .Include(l => l.Theory)
                .Include(l => l.PracticeTasks)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (level == null) return NotFound(new { error = "Уровень не найден" });
            if (level.PracticeTasks.Any())
                return BadRequest(new { error = "Нельзя удалить уровень, в котором есть задания" });

            var deletedNumber = level.LevelNumber;
            var topicId = level.TopicId;

            if (level.Theory != null)
                _context.Theories.Remove(level.Theory);

            _context.Levels.Remove(level);

            var toShift = await _context.Levels
                .Where(l => l.TopicId == topicId && l.LevelNumber > deletedNumber)
                .ToListAsync();
            foreach (var l in toShift) l.LevelNumber--;

            await _context.SaveChangesAsync();
            await _notificationService.SendNotificationToAllAsync(
                "Уровень удалён",
                $"Удалён уровень: {level.Name}",
                NotificationType.LevelDeleted,
                id
            );
            return Ok(new { message = "Уровень успешно удалён" });
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpPost("theory/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTheory([FromForm] CreateTheoryRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Title))
                return BadRequest(new { error = "Заголовок теории обязателен" });

            if (string.IsNullOrWhiteSpace(req.Content))
                return BadRequest(new { error = "Содержимое теории обязательно" });

            var level = await _context.Levels
                .Include(l => l.Theory)
                .FirstOrDefaultAsync(l => l.Id == req.LevelId);

            if (level == null) return NotFound(new { error = "Уровень не найден" });

            if (level.Theory != null)
                return BadRequest(new { error = "У этого уровня уже есть теория. Используйте редактирование." });

            var theory = new Theory
            {
                LevelId = req.LevelId,
                Title = req.Title.Trim(),
                Content = req.Content.Trim()
            };

            _context.Theories.Add(theory);
            await _context.SaveChangesAsync();
            return Ok(new { id = theory.Id, message = "Теория успешно создана" });
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpPost("theory/update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTheory([FromForm] UpdateTheoryRequest req)
        {
            var theory = await _context.Theories.FindAsync(req.Id);
            if (theory == null) return NotFound(new { error = "Теория не найдена" });

            if (string.IsNullOrWhiteSpace(req.Title))
                return BadRequest(new { error = "Заголовок теории обязателен" });

            if (string.IsNullOrWhiteSpace(req.Content))
                return BadRequest(new { error = "Содержимое теории обязательно" });

            theory.Title = req.Title.Trim();
            theory.Content = req.Content.Trim();

            await _context.SaveChangesAsync();
            return Ok(new { message = "Теория успешно обновлена" });
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpPost("theory/delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTheory(int id)
        {
            var theory = await _context.Theories.FindAsync(id);
            if (theory == null) return NotFound(new { error = "Теория не найдена" });

            _context.Theories.Remove(theory);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Теория успешно удалена" });
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpPost("tasks/create")]
        public async Task<IActionResult> CreateTask([FromForm] CreatePracticeTaskRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { error = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)) });

            var level = await _context.Levels.FindAsync(req.LevelId);
            if (level == null) return NotFound(new { error = "Уровень не найден" });

            var difficultyType = await _context.DifficultyTypes.FindAsync(req.DifficultyTypeId);
            if (difficultyType == null) return NotFound(new { error = "Тип сложности не найден" });

            var compositionError = await ValidateLevelTasksCompositionAsync(req.LevelId, excludeTaskId: null, newDifficultyName: difficultyType.Name);
            if (compositionError != null) return BadRequest(new { error = compositionError });

            var toShift = await _context.PracticeTasks
                .Where(t => t.LevelId == req.LevelId && t.DisplayOrder >= req.DisplayOrder)
                .ToListAsync();
            foreach (var t in toShift) t.DisplayOrder++;

            var task = new PracticeTask
            {
                LevelId = req.LevelId,
                Name = req.Name.Trim(),
                Condition = req.Condition.Trim(),
                DifficultyTypeId = req.DifficultyTypeId,
                CheckTypeId = req.CheckTypeId,
                DisplayOrder = req.DisplayOrder
            };

            _context.PracticeTasks.Add(task);
            await _context.SaveChangesAsync();
            await _notificationService.SendNotificationToAllAsync(
                "Новое задание",
                $"Добавлено новое задание: {task.Name}",
                NotificationType.TaskAdded,
                task.Id
            );
            return Ok(new { id = task.Id, message = "Задание успешно создано" });
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpPost("tasks/update")]
        public async Task<IActionResult> UpdateTask([FromForm] UpdatePracticeTaskRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { error = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)) });

            var task = await _context.PracticeTasks
                .Include(t => t.CheckType)
                .Include(t => t.CustomCheckAlgorithm)
                .Include(t => t.TestCases)
                .FirstOrDefaultAsync(t => t.Id == req.Id);
            if (task == null) return NotFound(new { error = "Задание не найдено" });

            var levelExists = await _context.Levels.AnyAsync(l => l.Id == req.LevelId);
            if (!levelExists) return NotFound(new { error = "Уровень не найден" });

            var difficultyType = await _context.DifficultyTypes.FindAsync(req.DifficultyTypeId);
            if (difficultyType == null) return NotFound(new { error = "Тип сложности не найден" });

            var newCheckType = await _context.CheckTypes.FindAsync(req.CheckTypeId);
            if (newCheckType == null) return NotFound(new { error = "Тип проверки не найден" });

            var compositionError = await ValidateLevelTasksCompositionAsync(req.LevelId, excludeTaskId: req.Id, newDifficultyName: difficultyType.Name);
            if (compositionError != null) return BadRequest(new { error = compositionError });

            if (task.DisplayOrder != req.DisplayOrder || task.LevelId != req.LevelId)
            {
                var conflict = await _context.PracticeTasks
                    .AnyAsync(t => t.LevelId == req.LevelId && t.DisplayOrder == req.DisplayOrder && t.Id != req.Id);

                if (conflict)
                {
                    var toShift = await _context.PracticeTasks
                        .Where(t => t.LevelId == req.LevelId && t.DisplayOrder >= req.DisplayOrder && t.Id != req.Id)
                        .ToListAsync();
                    foreach (var t in toShift) t.DisplayOrder++;
                }
            }

            if (task.CheckTypeId != req.CheckTypeId)
            {
                bool newIsCustom = newCheckType.Name == CustomCheckTypeName;
                if (newIsCustom)
                    _context.TestCases.RemoveRange(task.TestCases);
                else if (task.CustomCheckAlgorithm != null)
                    _context.CustomCheckAlgorithms.Remove(task.CustomCheckAlgorithm);
            }

            task.LevelId = req.LevelId;
            task.Name = req.Name.Trim();
            task.Condition = req.Condition.Trim();
            task.DifficultyTypeId = req.DifficultyTypeId;
            task.CheckTypeId = req.CheckTypeId;
            task.DisplayOrder = req.DisplayOrder;

            await _context.SaveChangesAsync();
            await _notificationService.SendNotificationToAllAsync(
                "Задание обновлено",
                $"Обновлено задание: {task.Name}",
                NotificationType.TaskUpdated,
                task.Id
            );
            return Ok(new { message = "Задание успешно обновлено" });
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpPost("tasks/delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var task = await _context.PracticeTasks
                .Include(t => t.TestCases)
                .Include(t => t.Hints)
                .Include(t => t.CustomCheckAlgorithm)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) return NotFound(new { error = "Задание не найдено" });

            var deletedOrder = task.DisplayOrder;
            var levelId = task.LevelId;

            var toShift = await _context.PracticeTasks
                .Where(t => t.LevelId == levelId && t.DisplayOrder > deletedOrder && t.Id != id)
                .ToListAsync();

            if (task.CustomCheckAlgorithm != null)
                _context.CustomCheckAlgorithms.Remove(task.CustomCheckAlgorithm);

            _context.TestCases.RemoveRange(task.TestCases);
            _context.Hints.RemoveRange(task.Hints);
            _context.PracticeTasks.Remove(task);

            foreach (var t in toShift) t.DisplayOrder--;

            await _context.SaveChangesAsync();
            await _notificationService.SendNotificationToAllAsync(
                "Задание удалено",
                $"Удалено задание: {task.Name}",
                NotificationType.TaskDeleted,
                id
            );
            return Ok(new { message = "Задание успешно удалено" });
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpPost("testcases/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTestCase([FromForm] CreateTestCaseRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    error = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors).Select(e => e.ErrorMessage))
                });

            var task = await _context.PracticeTasks
                .Include(t => t.CheckType)
                .FirstOrDefaultAsync(t => t.Id == req.TaskId);

            if (task == null) return NotFound(new { error = "Задание не найдено" });

            if (task.CheckType.Name == CustomCheckTypeName)
                return BadRequest(new { error = "Это задание использует кастомную проверку — тест-кейсы не поддерживаются" });

            var testCase = new TestCase
            {
                TaskId = req.TaskId,
                InputData = NormalizeTestInput(req.InputData),
                ExpectedOutput = NormalizeTestText(req.ExpectedOutput),
                IsHidden = req.IsHidden
            };

            _context.TestCases.Add(testCase);
            await _context.SaveChangesAsync();
            return Ok(new { id = testCase.Id, message = "Тест-кейс успешно создан" });
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpPost("testcases/update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTestCase([FromForm] UpdateTestCaseRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    error = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors).Select(e => e.ErrorMessage))
                });

            var testCase = await _context.TestCases
                .Include(tc => tc.PracticeTask)
                    .ThenInclude(pt => pt.CheckType)
                .FirstOrDefaultAsync(tc => tc.Id == req.Id);

            if (testCase == null) return NotFound(new { error = "Тест-кейс не найден" });

            if (testCase.PracticeTask.CheckType.Name == CustomCheckTypeName)
                return BadRequest(new { error = "Это задание использует кастомную проверку — тест-кейсы не поддерживаются" });

            testCase.InputData = NormalizeTestInput(req.InputData);
            testCase.ExpectedOutput = NormalizeTestText(req.ExpectedOutput);
            testCase.IsHidden = req.IsHidden;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Тест-кейс успешно обновлён" });
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpPost("testcases/delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTestCase(int id)
        {
            var testCase = await _context.TestCases.FindAsync(id);
            if (testCase == null) return NotFound(new { error = "Тест-кейс не найден" });

            _context.TestCases.Remove(testCase);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Тест-кейс успешно удалён" });
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpPost("hints/create")]
        public async Task<IActionResult> CreateHint([FromForm] CreateHintRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { error = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)) });

            var taskExists = await _context.PracticeTasks.AnyAsync(t => t.Id == req.TaskId);
            if (!taskExists) return NotFound(new { error = "Задание не найдено" });

            var toShift = await _context.Hints
                .Where(h => h.TaskId == req.TaskId && h.DisplayOrder >= req.DisplayOrder)
                .ToListAsync();
            foreach (var h in toShift) h.DisplayOrder++;

            var hint = new Hint
            {
                TaskId = req.TaskId,
                HintText = req.HintText.Trim(),
                DisplayOrder = req.DisplayOrder
            };

            _context.Hints.Add(hint);
            await _context.SaveChangesAsync();
            return Ok(new { id = hint.Id, message = "Подсказка успешно создана" });
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpPost("hints/update")]
        public async Task<IActionResult> UpdateHint([FromForm] UpdateHintRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { error = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)) });

            var hint = await _context.Hints.FindAsync(req.Id);
            if (hint == null) return NotFound(new { error = "Подсказка не найдена" });

            if (hint.DisplayOrder != req.DisplayOrder)
            {
                var conflict = await _context.Hints
                    .AnyAsync(h => h.TaskId == hint.TaskId && h.DisplayOrder == req.DisplayOrder && h.Id != req.Id);

                if (conflict)
                {
                    var toShift = await _context.Hints
                        .Where(h => h.TaskId == hint.TaskId && h.DisplayOrder >= req.DisplayOrder && h.Id != req.Id)
                        .ToListAsync();
                    foreach (var h in toShift) h.DisplayOrder++;
                }
            }

            hint.HintText = req.HintText.Trim();
            hint.DisplayOrder = req.DisplayOrder;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Подсказка успешно обновлена" });
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpPost("hints/delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteHint(int id)
        {
            var hint = await _context.Hints.FindAsync(id);
            if (hint == null) return NotFound(new { error = "Подсказка не найдена" });

            var deletedOrder = hint.DisplayOrder;
            var taskId = hint.TaskId;

            _context.Hints.Remove(hint);

            var toShift = await _context.Hints
                .Where(h => h.TaskId == taskId && h.DisplayOrder > deletedOrder)
                .ToListAsync();
            foreach (var h in toShift) h.DisplayOrder--;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Подсказка успешно удалена" });
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpPost("algorithms/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAlgorithm([FromForm] CreateCustomAlgorithmRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    error = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors).Select(e => e.ErrorMessage))
                });

            var task = await _context.PracticeTasks
                .Include(t => t.CheckType)
                .Include(t => t.CustomCheckAlgorithm)
                .FirstOrDefaultAsync(t => t.Id == req.TaskId);

            if (task == null) return NotFound(new { error = "Задание не найдено" });

            if (task.CheckType.Name != CustomCheckTypeName)
                return BadRequest(new { error = "Это задание использует шаблонную проверку — алгоритм не поддерживается" });

            if (task.CustomCheckAlgorithm != null)
                return BadRequest(new { error = "У этого задания уже есть алгоритм проверки. Используйте редактирование." });

            var algorithm = new CustomCheckAlgorithm
            {
                TaskId = req.TaskId,
                AlgorithmCode = req.AlgorithmCode.Trim()
            };

            _context.CustomCheckAlgorithms.Add(algorithm);
            await _context.SaveChangesAsync();
            return Ok(new { id = algorithm.Id, message = "Алгоритм проверки успешно создан" });
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpPost("algorithms/update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAlgorithm([FromForm] UpdateCustomAlgorithmRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { error = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)) });

            var algorithm = await _context.CustomCheckAlgorithms.FindAsync(req.Id);
            if (algorithm == null) return NotFound(new { error = "Алгоритм проверки не найден" });

            algorithm.AlgorithmCode = req.AlgorithmCode.Trim();

            await _context.SaveChangesAsync();
            return Ok(new { message = "Алгоритм проверки успешно обновлён" });
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpPost("algorithms/delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAlgorithm(int id)
        {
            var algorithm = await _context.CustomCheckAlgorithms.FindAsync(id);
            if (algorithm == null) return NotFound(new { error = "Алгоритм проверки не найден" });

            _context.CustomCheckAlgorithms.Remove(algorithm);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Алгоритм проверки успешно удалён" });
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpPost("tournaments/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTournament([FromForm] CreateTournamentRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { error = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)) });

            if (req.MinRating > req.MaxRating)
                return BadRequest(new { error = "Минимальный рейтинг не может быть больше максимального" });

            var topicExists = await _context.Topics.AnyAsync(t => t.Id == req.TopicId);
            if (!topicExists) return NotFound(new { error = "Тема не найдена" });

            var statusExists = await _context.TournamentStatuses.AnyAsync(s => s.Id == req.StatusId);
            if (!statusExists) return NotFound(new { error = "Статус не найден" });

            var tournament = new Tournament
            {
                TopicId = req.TopicId,
                StatusId = req.StatusId,
                MinRating = req.MinRating,
                MaxRating = req.MaxRating,
                CreatedAt = DateTime.UtcNow
            };

            _context.Tournaments.Add(tournament);
            await _context.SaveChangesAsync();
            var topicName = await _context.Topics
            .Where(t => t.Id == req.TopicId)
            .Select(t => t.Name)
            .FirstOrDefaultAsync() ?? "неизвестная тема";

            await _notificationService.SendNotificationToAllAsync(
                "Новый PvP-турнир",
                $"Открыт новый турнир по теме: \"{topicName}\"",
                NotificationType.TournamentCreated,
                tournament.Id
            );
            return Ok(new { id = tournament.Id, message = "Турнир успешно создан" });
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpPost("tournaments/update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTournament([FromForm] UpdateTournamentRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { error = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)) });

            if (req.MinRating > req.MaxRating)
                return BadRequest(new { error = "Минимальный рейтинг не может быть больше максимального" });

            var tournament = await _context.Tournaments
                .Include(t => t.TournamentQuestions)
                    .ThenInclude(tq => tq.Question)
                .FirstOrDefaultAsync(t => t.Id == req.Id);

            if (tournament == null) return NotFound(new { error = "Турнир не найден" });

            var topicExists = await _context.Topics.AnyAsync(t => t.Id == req.TopicId);
            if (!topicExists) return NotFound(new { error = "Тема не найдена" });

            var statusExists = await _context.TournamentStatuses.AnyAsync(s => s.Id == req.StatusId);
            if (!statusExists) return NotFound(new { error = "Статус не найден" });

            var removedCount = 0;

            if (tournament.TopicId != req.TopicId && tournament.TournamentQuestions.Any())
            {
                var wrongQuestions = tournament.TournamentQuestions
                    .Where(tq => tq.Question.TopicId != req.TopicId)
                    .ToList();

                if (wrongQuestions.Any())
                {
                    _context.TournamentQuestions.RemoveRange(wrongQuestions);
                    removedCount = wrongQuestions.Count;
                }
            }

            tournament.TopicId = req.TopicId;
            tournament.StatusId = req.StatusId;
            tournament.MinRating = req.MinRating;
            tournament.MaxRating = req.MaxRating;

            await _context.SaveChangesAsync();

            var topicName = await _context.Topics
                .Where(t => t.Id == req.TopicId)
                .Select(t => t.Name)
                .FirstOrDefaultAsync() ?? "неизвестная тема";

            await _notificationService.SendNotificationToAllAsync(
                "Турнир обновлён",
                $"Обновлён турнир по теме: \"{topicName}\"",
                NotificationType.TournamentUpdated,
                tournament.Id
            );

            var message = removedCount > 0
                ? $"Турнир обновлён. Автоматически откреплено вопросов несовместимой темы: {removedCount}"
                : "Турнир успешно обновлён";

            return Ok(new { message });
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpPost("tournaments/delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTournament(int id)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.TournamentQuestions)
                .Include(t => t.PlayerAnswers)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tournament == null) return NotFound(new { error = "Турнир не найден" });

            if (tournament.PlayerAnswers.Any())
                return BadRequest(new { error = "Нельзя удалить турнир с ответами игроков" });

            _context.TournamentQuestions.RemoveRange(tournament.TournamentQuestions);
            _context.Tournaments.Remove(tournament);
            await _context.SaveChangesAsync();
            await _notificationService.SendNotificationToAllAsync(
                "Турнир удалён",
                $"Удалён турнир по теме",
                NotificationType.TournamentDeleted,
                id
            );
            return Ok(new { message = "Турнир успешно удалён" });
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpPost("tournaments/questions/add")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTournamentQuestion([FromForm] AddTournamentQuestionRequest req)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.TournamentQuestions)
                .FirstOrDefaultAsync(t => t.Id == req.TournamentId);

            if (tournament == null) return NotFound(new { error = "Турнир не найден" });

            var questionExists = await _context.Questions.AnyAsync(q => q.Id == req.QuestionId);
            if (!questionExists) return NotFound(new { error = "Вопрос не найден" });

            var alreadyAdded = tournament.TournamentQuestions.Any(tq => tq.QuestionId == req.QuestionId);
            if (alreadyAdded)
                return BadRequest(new { error = "Этот вопрос уже добавлен в турнир" });

            var nextNumber = tournament.TournamentQuestions.Any()
                ? tournament.TournamentQuestions.Max(tq => tq.QuestionNumber) + 1
                : 1;

            var tq = new TournamentQuestion
            {
                TournamentId = req.TournamentId,
                QuestionId = req.QuestionId,
                QuestionNumber = nextNumber
            };

            _context.TournamentQuestions.Add(tq);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Вопрос добавлен в турнир" });
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpPost("tournaments/questions/remove")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveTournamentQuestion([FromForm] RemoveTournamentQuestionRequest req)
        {
            var tq = await _context.TournamentQuestions
                .FirstOrDefaultAsync(x => x.TournamentId == req.TournamentId && x.QuestionId == req.QuestionId);

            if (tq == null) return NotFound(new { error = "Вопрос не найден в турнире" });

            _context.TournamentQuestions.Remove(tq);
            await _context.SaveChangesAsync();

            var remaining = await _context.TournamentQuestions
                .Where(x => x.TournamentId == req.TournamentId)
                .OrderBy(x => x.QuestionNumber)
                .ToListAsync();

            for (int i = 0; i < remaining.Count; i++)
                remaining[i].QuestionNumber = i + 1;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Вопрос удалён из турнира" });
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpPost("questions/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQuestion([FromForm] CreateQuestionRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { error = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)) });

            var topicExists = await _context.Topics.AnyAsync(t => t.Id == req.TopicId);
            if (!topicExists) return NotFound(new { error = "Тема не найдена" });

            var diffExists = await _context.DifficultyTypes.AnyAsync(d => d.Id == req.DifficultyTypeId);
            if (!diffExists) return NotFound(new { error = "Тип сложности не найден" });

            var question = new Question
            {
                TopicId = req.TopicId,
                DifficultyTypeId = req.DifficultyTypeId,
                QuestionText = req.QuestionText.Trim()
            };

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();
            return Ok(new { id = question.Id, message = "Вопрос успешно создан" });
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpPost("questions/update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuestion([FromForm] UpdateQuestionRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { error = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)) });

            var question = await _context.Questions.FindAsync(req.Id);
            if (question == null) return NotFound(new { error = "Вопрос не найден" });

            var topicExists = await _context.Topics.AnyAsync(t => t.Id == req.TopicId);
            if (!topicExists) return NotFound(new { error = "Тема не найдена" });

            var diffExists = await _context.DifficultyTypes.AnyAsync(d => d.Id == req.DifficultyTypeId);
            if (!diffExists) return NotFound(new { error = "Тип сложности не найден" });

            question.TopicId = req.TopicId;
            question.DifficultyTypeId = req.DifficultyTypeId;
            question.QuestionText = req.QuestionText.Trim();

            await _context.SaveChangesAsync();
            return Ok(new { message = "Вопрос успешно обновлён" });
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpPost("questions/delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var question = await _context.Questions
                .Include(q => q.AnswerOptions)
                .Include(q => q.TournamentQuestions)
                .Include(q => q.PlayerAnswers)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null) return NotFound(new { error = "Вопрос не найден" });

            if (question.PlayerAnswers.Any())
                return BadRequest(new { error = "Нельзя удалить вопрос с ответами игроков" });

            _context.TournamentQuestions.RemoveRange(question.TournamentQuestions);
            _context.AnswerOptions.RemoveRange(question.AnswerOptions);
            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Вопрос успешно удалён" });
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpPost("answers/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAnswerOption([FromForm] CreateAnswerOptionRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { error = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)) });

            var question = await _context.Questions
                .Include(q => q.AnswerOptions)
                .FirstOrDefaultAsync(q => q.Id == req.QuestionId);

            if (question == null) return NotFound(new { error = "Вопрос не найден" });

            if (question.AnswerOptions.Count >= 4)
                return BadRequest(new { error = "У вопроса не может быть более 4 вариантов ответа" });

            if (req.IsCorrect && question.AnswerOptions.Any(a => a.IsCorrect))
                return BadRequest(new { error = "У вопроса уже есть правильный ответ" });

            var answer = new AnswerOption
            {
                QuestionId = req.QuestionId,
                AnswerText = req.AnswerText.Trim(),
                IsCorrect = req.IsCorrect
            };

            _context.AnswerOptions.Add(answer);
            await _context.SaveChangesAsync();
            return Ok(new { id = answer.Id, message = "Вариант ответа успешно создан" });
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpPost("answers/update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAnswerOption([FromForm] UpdateAnswerOptionRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { error = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)) });

            var answer = await _context.AnswerOptions
                .Include(a => a.Question)
                    .ThenInclude(q => q.AnswerOptions)
                .FirstOrDefaultAsync(a => a.Id == req.Id);

            if (answer == null) return NotFound(new { error = "Вариант ответа не найден" });

            if (req.IsCorrect && !answer.IsCorrect &&
                answer.Question.AnswerOptions.Any(a => a.Id != req.Id && a.IsCorrect))
                return BadRequest(new { error = "У вопроса уже есть правильный ответ" });

            answer.AnswerText = req.AnswerText.Trim();
            answer.IsCorrect = req.IsCorrect;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Вариант ответа успешно обновлён" });
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpPost("answers/delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAnswerOption(int id)
        {
            var answer = await _context.AnswerOptions
                .Include(a => a.PlayerAnswers)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (answer == null) return NotFound(new { error = "Вариант ответа не найден" });

            if (answer.PlayerAnswers.Any())
                return BadRequest(new { error = "Нельзя удалить вариант ответа, на который уже отвечали игроки" });

            _context.AnswerOptions.Remove(answer);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Вариант ответа успешно удалён" });
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpGet("export/xlsx")]
        public async Task<IActionResult> ExportXlsx()
        {
            using var wb = new ClosedXML.Excel.XLWorkbook();

            void WriteSheet(string name, string[] headers, List<object?[]> rows)
            {
                static string CleanSheetName(string raw)
                {
                    if (string.IsNullOrWhiteSpace(raw))
                        return "Sheet";

                    var invalidChars = new[] { '\\', '/', '*', '?', ':', '[', ']' };
                    var cleaned = raw;

                    foreach (var ch in invalidChars)
                        cleaned = cleaned.Replace(ch, '-');

                    if (cleaned.Length > 31)
                        cleaned = cleaned.Substring(0, 31);

                    cleaned = cleaned.Trim();

                    return string.IsNullOrWhiteSpace(cleaned) ? "Sheet" : cleaned;
                }

                var ws = wb.Worksheets.Add(CleanSheetName(name));

                for (int i = 0; i < headers.Length; i++)
                    ws.Cell(1, i + 1).Value = headers[i];

                if (rows.Count == 0)
                {
                    ws.Cell(2, 1).Value = "No data / Данных нет";
                    ws.Cell(2, 1).Style.Font.Italic = true;
                    ws.Cell(2, 1).Style.Font.FontColor = ClosedXML.Excel.XLColor.Gray;
                }
                else
                {
                    for (int r = 0; r < rows.Count; r++)
                        for (int c = 0; c < rows[r].Length; c++)
                        {
                            var cell = ws.Cell(r + 2, c + 1);
                            var val = rows[r][c];
                            if (val is int iv) cell.Value = iv;
                            else if (val is bool bv) cell.Value = bv ? "Yes / Да" : "No / Нет";
                            else cell.Value = val?.ToString() ?? "";
                        }
                }

                ws.Row(1).Style.Font.Bold = true;
                ws.Row(1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#EFF6FF");
                ws.Columns().AdjustToContents();
            }

            var users = await _context.Users
                .Include(u => u.Role).Include(u => u.Rating).Include(u => u.Avatar)
                .OrderBy(u => u.Id).ToListAsync();
            var userIds = users.Select(u => u.Id).ToList();

            var solvedDict = await _context.Solutions
                .Where(s => userIds.Contains(s.UserId) && s.IsCorrect)
                .GroupBy(s => s.UserId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);

            var topicDict = await _context.RatingHistories
                .Where(h => h.UserId.HasValue && userIds.Contains(h.UserId.Value)
                    && h.TopicId.HasValue && h.TaskId == null)
                .GroupBy(h => h.UserId!.Value)
                .Select(g => new { g.Key, Count = g.Select(x => x.TopicId).Distinct().Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);

            var levelDict = await _context.RatingHistories
                .Where(h => h.UserId.HasValue && userIds.Contains(h.UserId.Value) && h.TaskId.HasValue)
                .GroupBy(h => h.UserId!.Value)
                .Select(g => new { g.Key, Count = g.Select(x => x.TaskId).Distinct().Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);

            var tournamentDict = await _context.PlayerAnswers
                .Where(pa => userIds.Contains(pa.UserId))
                .GroupBy(pa => pa.UserId)
                .Select(g => new { g.Key, Count = g.Select(x => x.TournamentId).Distinct().Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);

            WriteSheet("Users / Пользователи",
                new[] { "ID", "UID", "Email", "Name / Ник", "Role / Роль",
                "Avatar ID / Аватар ID", "Avatar URL / URL аватара",
                "ELO Rating / ELO-рейтинг", "Registered At / Дата регистрации",
                "Solved Tasks / Решено заданий", "Completed Topics / Завершено тем",
                "Completed Levels / Завершено уровней", "Tournaments Played / Турниров сыграно" },
                users.Select(u => new object?[] {
                u.Id, u.Uid ?? "", u.Email,
                u.Name ?? $"Игрок #{u.Id}",
                u.Role?.Code ?? "",
                u.AvatarId?.ToString() ?? "",
                u.Avatar?.Url ?? "",
                u.Rating?.CurrentRating ?? 500,
                u.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                solvedDict.TryGetValue(u.Id, out var s) ? s : 0,
                topicDict.TryGetValue(u.Id, out var t) ? t : 0,
                levelDict.TryGetValue(u.Id, out var l) ? l : 0,
                tournamentDict.TryGetValue(u.Id, out var tr) ? tr : 0
                }).ToList());

            var roles = await _context.Roles.OrderBy(r => r.Id).ToListAsync();
            WriteSheet("Roles / Роли",
                new[] { "ID", "Code / Код" },
                roles.Select(r => new object?[] { r.Id, r.Code }).ToList());

            var avatars = await _context.AvailableAvatars.OrderBy(a => a.DisplayOrder).ToListAsync();
            WriteSheet("Avatars / Аватары",
                new[] { "ID", "URL", "Display Order / Порядок" },
                avatars.Select(a => new object?[] { a.Id, a.Url, a.DisplayOrder }).ToList());

            var ratings = await _context.UserRatings.OrderBy(r => r.UserId).ToListAsync();
            WriteSheet("User Ratings / Рейтинги",
                new[] { "ID", "User ID", "Current Rating / Текущий рейтинг", "Last Updated / Обновлено" },
                ratings.Select(r => new object?[] {
                r.Id, r.UserId, r.CurrentRating,
                r.LastUpdated.ToString("yyyy-MM-dd HH:mm")
                }).ToList());

            var history = await _context.RatingHistories
                .Include(h => h.Reason).OrderBy(h => h.CreatedAt).ToListAsync();
            WriteSheet("Rating History / Рейтинг. история",
                new[] { "ID", "User ID", "Old Rating / Старый рейтинг", "New Rating / Новый рейтинг",
                "Reason / Причина", "Task ID", "Topic ID", "Tournament ID", "Created At / Дата" },
                history.Select(h => new object?[] {
                h.Id, h.UserId?.ToString() ?? "",
                h.OldRating, h.NewRating,
                h.Reason?.Name ?? "",
                h.TaskId?.ToString() ?? "",
                h.TopicId?.ToString() ?? "",
                h.TournamentId?.ToString() ?? "",
                h.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                }).ToList());

            var reasons = await _context.RatingChangeReasons.OrderBy(r => r.Id).ToListAsync();
            WriteSheet("Rating Reasons / Причины рейтинга",
                new[] { "ID", "Name / Название" },
                reasons.Select(r => new object?[] { r.Id, r.Name }).ToList());

            var topics = await _context.Topics.OrderBy(t => t.DisplayOrder).ToListAsync();
            WriteSheet("Topics / Темы",
                new[] { "ID", "Name / Название", "Description / Описание", "Display Order / Порядок" },
                topics.Select(t => new object?[] {
            t.Id, t.Name, t.Description ?? "", t.DisplayOrder
                }).ToList());

            var levels = await _context.Levels.OrderBy(l => l.TopicId).ThenBy(l => l.LevelNumber).ToListAsync();
            WriteSheet("Levels / Уровни",
                new[] { "ID", "Topic ID", "Name / Название", "Level Number / Номер уровня" },
                levels.Select(l => new object?[] {
            l.Id, l.TopicId, l.Name, l.LevelNumber
                }).ToList());

            var theories = await _context.Theories.OrderBy(t => t.LevelId).ToListAsync();
            WriteSheet("Theories / Теории",
                new[] { "ID", "Level ID", "Title / Заголовок", "Content / Содержание" },
                theories.Select(t => new object?[] {
            t.Id, t.LevelId, t.Title, t.Content
                }).ToList());

            var diffs = await _context.DifficultyTypes.OrderBy(d => d.Id).ToListAsync();
            WriteSheet("Difficulty Types / Сложности",
                new[] { "ID", "Name / Название" },
                diffs.Select(d => new object?[] { d.Id, d.Name }).ToList());

            var checks = await _context.CheckTypes.OrderBy(c => c.Id).ToListAsync();
            WriteSheet("Check Types / Типы проверки",
                new[] { "ID", "Name / Название" },
                checks.Select(c => new object?[] { c.Id, c.Name }).ToList());

            var tasks = await _context.PracticeTasks
                .Include(t => t.DifficultyType).Include(t => t.CheckType)
                .Include(t => t.Level).ThenInclude(l => l.Topic)
                .OrderBy(t => t.LevelId).ThenBy(t => t.DisplayOrder).ToListAsync();
            WriteSheet("Tasks / Задания",
                new[] { "ID", "Level ID", "Topic / Тема", "Name / Название",
                "Condition / Условие", "Difficulty / Сложность",
                "Check Type / Тип проверки", "Display Order / Порядок" },
                tasks.Select(t => new object?[] {
                t.Id, t.LevelId,
                t.Level?.Topic?.Name ?? "",
                t.Name, t.Condition,
                t.DifficultyType?.Name ?? "",
                t.CheckType?.Name ?? "",
                t.DisplayOrder
                }).ToList());

            var testCases = await _context.TestCases.OrderBy(tc => tc.TaskId).ToListAsync();
            WriteSheet("Test Cases / Тест-кейсы",
                new[] { "ID", "Task ID", "Input Data / Входные данные",
                "Expected Output / Ожидаемый вывод", "Is Hidden / Скрыт" },
                testCases.Select(tc => new object?[] {
                tc.Id, tc.TaskId,
                tc.InputData ?? "",
                tc.ExpectedOutput,
                tc.IsHidden
                }).ToList());

            var hints = await _context.Hints.OrderBy(h => h.TaskId).ThenBy(h => h.DisplayOrder).ToListAsync();
            WriteSheet("Hints / Подсказки",
                new[] { "ID", "Task ID", "Hint Text / Текст подсказки", "Display Order / Порядок" },
                hints.Select(h => new object?[] {
                h.Id, h.TaskId, h.HintText, h.DisplayOrder
                }).ToList());

            var algos = await _context.CustomCheckAlgorithms.OrderBy(a => a.TaskId).ToListAsync();
            WriteSheet("Custom Algorithms / Алгоритмы",
                new[] { "ID", "Task ID", "Algorithm Code / Код алгоритма" },
                algos.Select(a => new object?[] {
                a.Id, a.TaskId, a.AlgorithmCode
                }).ToList());

            var solutions = await _context.Solutions.OrderBy(s => s.CreatedAt).ToListAsync();
            WriteSheet("Solutions / Решения заданий",
                new[] { "ID", "User ID", "Task ID", "Solution Code / Код решения",
                "Is Correct / Правильно", "Created At / Дата" },
                solutions.Select(s => new object?[] {
                s.Id, s.UserId, s.TaskId,
                s.SolutionCode,
                s.IsCorrect,
                s.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                }).ToList());

            var tStatuses = await _context.TournamentStatuses.OrderBy(s => s.Id).ToListAsync();
            WriteSheet("Tournament Statuses / Статусы турниров",
                new[] { "ID", "Name / Название" },
                tStatuses.Select(s => new object?[] { s.Id, s.Name }).ToList());

            var tournaments = await _context.Tournaments
                .Include(t => t.Topic).Include(t => t.Status)
                .OrderBy(t => t.CreatedAt).ToListAsync();
            WriteSheet("Tournaments / Турниры",
                new[] { "ID", "Topic ID", "Topic / Тема", "Status / Статус",
                "Min ELO / Мин. ELO", "Max ELO / Макс. ELO", "Created At / Дата создания" },
                tournaments.Select(t => new object?[] {
                t.Id, t.TopicId,
                t.Topic?.Name ?? "",
                t.Status?.Name ?? "",
                t.MinRating, t.MaxRating,
                t.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                }).ToList());

            var questions = await _context.Questions
                .Include(q => q.DifficultyType)
                .OrderBy(q => q.TopicId).ThenBy(q => q.Id).ToListAsync();
            WriteSheet("Questions / Вопросы",
                new[] { "ID", "Topic ID", "Difficulty / Сложность", "Question Text / Текст вопроса" },
                questions.Select(q => new object?[] {
                q.Id, q.TopicId,
                q.DifficultyType?.Name ?? "",
                q.QuestionText
                }).ToList());

            var answers = await _context.AnswerOptions.OrderBy(a => a.QuestionId).ThenBy(a => a.Id).ToListAsync();
            WriteSheet("Answer Options / Варианты ответов",
                new[] { "ID", "Question ID", "Answer Text / Текст ответа", "Is Correct / Правильный" },
                answers.Select(a => new object?[] {
                a.Id, a.QuestionId, a.AnswerText, a.IsCorrect
                }).ToList());

            var tQuestions = await _context.TournamentQuestions
                .OrderBy(tq => tq.TournamentId).ThenBy(tq => tq.QuestionNumber).ToListAsync();
            WriteSheet("Tournament Questions / Вопросы турниров",
                new[] { "Tournament ID", "Question ID", "Question Number / Номер вопроса" },
                tQuestions.Select(tq => new object?[] {
                tq.TournamentId, tq.QuestionId, tq.QuestionNumber
                }).ToList());

            var playerAnswers = await _context.PlayerAnswers
                .Include(pa => pa.AnswerOption)
                .OrderBy(pa => pa.AnsweredAt).ToListAsync();
                WriteSheet("Player Answers / Ответы игроков",
                new[] { "ID", "User ID", "Tournament ID", "Question ID",
                "Answer Option ID", "Is Correct / Правильно", "Answered At / Дата ответа" },
                playerAnswers.Select(pa => new object?[] {
                pa.Id, pa.UserId, pa.TournamentId, pa.QuestionId,
                pa.AnswerOptionId,
                pa.AnswerOption?.IsCorrect ?? false,
                pa.AnsweredAt.ToString("yyyy-MM-dd HH:mm")
                }).ToList());

            var otpCodes = await _context.OtpCodes.OrderBy(o => o.CreatedAt).ToListAsync();
            WriteSheet("OTP Codes / OTP-коды",
                new[] { "ID", "User ID", "Email", "Code / Код", "Purpose ID",
                "Expires At / Истекает", "Used / Использован", "Created At / Создан" },
                            otpCodes.Select(o => new object?[] {
                o.Id,
                o.UserId?.ToString() ?? "",
                o.Email,
                o.Code,
                o.PurposeId,
                o.ExpiresAt.ToString("yyyy-MM-dd HH:mm"),
                o.Used,
                o.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                }).ToList());

            var otpPurposes = await _context.OtpPurposes.OrderBy(p => p.Id).ToListAsync();
            WriteSheet("OTP Purposes / Назначения OTP",
                new[] { "ID", "Name / Название" },
                otpPurposes.Select(p => new object?[] { p.Id, p.Name }).ToList());

            var prtokens = await _context.PasswordResetTokens.OrderBy(t => t.CreatedAt).ToListAsync();
            WriteSheet("Password Reset Tokens / Токены сброса",
                new[] { "ID", "User ID", "Token / Токен", "Expires At / Истекает",
                "Used / Использован", "Created At / Создан" },
                prtokens.Select(t => new object?[] {
                t.Id, t.UserId.ToString(), t.Token,
                t.ExpiresAt.ToString("yyyy-MM-dd HH:mm"),
                t.Used,
                t.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                }).ToList());

            var prAttempts = await _context.PasswordResetAttempts.OrderBy(a => a.CreatedAt).ToListAsync();
            WriteSheet("Password Reset Attempts / Попытки сброса",
                new[] { "ID", "Email", "IP Address / IP-адрес", "Created At / Дата" },
                prAttempts.Select(a => new object?[] {
                a.Id,
                a.Email,
                a.IpAddress ?? "",
                a.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                }).ToList());

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            var fileName = $"export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        [Authorize(AuthenticationSchemes = "AdminCookie", Roles = "admin")]
        [HttpGet("export/sql")]
        public async Task<IActionResult> ExportSql()
        {
            var sb = new System.Text.StringBuilder();
            var ts = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            static string Esc(string? s) =>
                s == null ? "NULL" : "'" + s.Replace("'", "''") + "'";
            static string EscN(int? n) => n?.ToString() ?? "NULL";
            static string EscB(bool b) => b ? "TRUE" : "FALSE";
            static string EscDt(DateTime? d) => d == null ? "NULL" : $"'{d.Value:yyyy-MM-dd HH:mm:ss}'";

            sb.AppendLine($"-- Database Backup / Резервная копия БД");
            sb.AppendLine($"-- Generated at / Сгенерировано: {ts} UTC");
            sb.AppendLine("-- Generated by Admin Panel / Сгенерировано административной панелью");
            sb.AppendLine("-- PostgreSQL");
            sb.AppendLine();
            sb.AppendLine("SET statement_timeout = 0;");
            sb.AppendLine("SET lock_timeout = 0;");
            sb.AppendLine("SET client_encoding = 'UTF8';");
            sb.AppendLine("SET standard_conforming_strings = on;");
            sb.AppendLine("SET check_function_bodies = false;");
            sb.AppendLine("SET row_security = off;");
            sb.AppendLine();

            sb.AppendLine("-- roles / роли");
            sb.AppendLine("CREATE TABLE IF NOT EXISTS public.roles (");
            sb.AppendLine("    id   SERIAL PRIMARY KEY,");
            sb.AppendLine("    code VARCHAR(50) NOT NULL,");
            sb.AppendLine("    CONSTRAINT uq_roles_code UNIQUE (code)");
            sb.AppendLine(");");
            sb.AppendLine("TRUNCATE TABLE public.roles RESTART IDENTITY CASCADE;");
            var roles = await _context.Roles.OrderBy(r => r.Id).ToListAsync();
            foreach (var r in roles)
                sb.AppendLine($"INSERT INTO public.roles (id, code) VALUES ({r.Id}, {Esc(r.Code)});");
            sb.AppendLine();

            sb.AppendLine("-- available_avatars / доступные аватары");
            sb.AppendLine("CREATE TABLE IF NOT EXISTS public.available_avatars (");
            sb.AppendLine("    id            SERIAL PRIMARY KEY,");
            sb.AppendLine("    url           TEXT    NOT NULL,");
            sb.AppendLine("    display_order INTEGER NOT NULL DEFAULT 0");
            sb.AppendLine(");");
            sb.AppendLine("TRUNCATE TABLE public.available_avatars RESTART IDENTITY CASCADE;");
            var avatars = await _context.AvailableAvatars.OrderBy(a => a.Id).ToListAsync();
            foreach (var a in avatars)
                sb.AppendLine($"INSERT INTO public.available_avatars (id, url, display_order) VALUES ({a.Id}, {Esc(a.Url)}, {a.DisplayOrder});");
            sb.AppendLine();

            sb.AppendLine("-- otp_purposes / назначения OTP");
            sb.AppendLine("CREATE TABLE IF NOT EXISTS public.otp_purposes (");
            sb.AppendLine("    id   SERIAL PRIMARY KEY,");
            sb.AppendLine("    name VARCHAR(100) NOT NULL,");
            sb.AppendLine("    CONSTRAINT uq_otp_purposes_name UNIQUE (name)");
            sb.AppendLine(");");
            sb.AppendLine("TRUNCATE TABLE public.otp_purposes RESTART IDENTITY CASCADE;");
            var otpPurposes = await _context.OtpPurposes.OrderBy(p => p.Id).ToListAsync();
            foreach (var p in otpPurposes)
                sb.AppendLine($"INSERT INTO public.otp_purposes (id, name) VALUES ({p.Id}, {Esc(p.Name)});");
            sb.AppendLine();

            sb.AppendLine("-- rating_change_reasons / причины изменения рейтинга");
            sb.AppendLine("CREATE TABLE IF NOT EXISTS public.rating_change_reasons (");
            sb.AppendLine("    id   SERIAL PRIMARY KEY,");
            sb.AppendLine("    name VARCHAR(100) NOT NULL,");
            sb.AppendLine("    CONSTRAINT uq_rating_change_reasons_name UNIQUE (name)");
            sb.AppendLine(");");
            sb.AppendLine("TRUNCATE TABLE public.rating_change_reasons RESTART IDENTITY CASCADE;");
            var reasons = await _context.RatingChangeReasons.OrderBy(r => r.Id).ToListAsync();
            foreach (var r in reasons)
                sb.AppendLine($"INSERT INTO public.rating_change_reasons (id, name) VALUES ({r.Id}, {Esc(r.Name)});");
            sb.AppendLine();

            sb.AppendLine("-- users / пользователи");
            sb.AppendLine("CREATE TABLE IF NOT EXISTS public.users (");
            sb.AppendLine("    id            SERIAL PRIMARY KEY,");
            sb.AppendLine("    uid           VARCHAR(255),");
            sb.AppendLine("    email         VARCHAR(255) NOT NULL,");
            sb.AppendLine("    password_hash VARCHAR(255) NOT NULL,");
            sb.AppendLine("    role_id       INTEGER      NOT NULL REFERENCES public.roles(id) ON DELETE RESTRICT,");
            sb.AppendLine("    name          VARCHAR(30),");
            sb.AppendLine("    avatar_id     INTEGER      REFERENCES public.available_avatars(id) ON DELETE SET NULL,");
            sb.AppendLine("    created_at    TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,");
            sb.AppendLine("    CONSTRAINT uq_users_email UNIQUE (email),");
            sb.AppendLine("    CONSTRAINT uq_users_uid   UNIQUE (uid)");
            sb.AppendLine(");");
            sb.AppendLine("CREATE INDEX IF NOT EXISTS idx_users_email   ON public.users (email);");
            sb.AppendLine("CREATE INDEX IF NOT EXISTS idx_users_uid     ON public.users (uid);");
            sb.AppendLine("CREATE INDEX IF NOT EXISTS idx_users_role_id ON public.users (role_id);");
            sb.AppendLine("TRUNCATE TABLE public.users RESTART IDENTITY CASCADE;");
            var allUsers = await _context.Users.OrderBy(u => u.Id).ToListAsync();
            foreach (var u in allUsers)
                sb.AppendLine($"INSERT INTO public.users (id, uid, email, password_hash, role_id, name, avatar_id, created_at) " +
                    $"VALUES ({u.Id}, {Esc(u.Uid)}, {Esc(u.Email)}, {Esc(u.PasswordHash)}, " +
                    $"{u.RoleId}, {Esc(u.Name)}, {EscN(u.AvatarId)}, '{u.CreatedAt:yyyy-MM-dd HH:mm:ss}');");
            sb.AppendLine();

            sb.AppendLine("-- user_ratings / рейтинги пользователей");
            sb.AppendLine("CREATE TABLE IF NOT EXISTS public.user_ratings (");
            sb.AppendLine("    id             SERIAL PRIMARY KEY,");
            sb.AppendLine("    user_id        INTEGER   REFERENCES public.users(id) ON DELETE CASCADE,");
            sb.AppendLine("    current_rating INTEGER   NOT NULL DEFAULT 500,");
            sb.AppendLine("    last_updated   TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,");
            sb.AppendLine("    CONSTRAINT uq_user_ratings_user_id UNIQUE (user_id)");
            sb.AppendLine(");");
            sb.AppendLine("TRUNCATE TABLE public.user_ratings RESTART IDENTITY CASCADE;");
            var ratings = await _context.UserRatings.OrderBy(r => r.Id).ToListAsync();
            foreach (var r in ratings)
                sb.AppendLine($"INSERT INTO public.user_ratings (id, user_id, current_rating, last_updated) " +
                    $"VALUES ({r.Id}, {r.UserId}, {r.CurrentRating}, '{r.LastUpdated:yyyy-MM-dd HH:mm:ss}');");
            sb.AppendLine();

            sb.AppendLine("-- otp_codes / OTP-коды");
            sb.AppendLine("CREATE TABLE IF NOT EXISTS public.otp_codes (");
            sb.AppendLine("    id         SERIAL PRIMARY KEY,");
            sb.AppendLine("    user_id    INTEGER      REFERENCES public.users(id) ON DELETE CASCADE,");
            sb.AppendLine("    email      VARCHAR(255) NOT NULL,");
            sb.AppendLine("    code       VARCHAR(6)   NOT NULL,");
            sb.AppendLine("    purpose_id INTEGER      NOT NULL REFERENCES public.otp_purposes(id) ON DELETE RESTRICT,");
            sb.AppendLine("    expires_at TIMESTAMP    NOT NULL DEFAULT (CURRENT_TIMESTAMP + INTERVAL '15 minutes'),");
            sb.AppendLine("    used       BOOLEAN      NOT NULL DEFAULT FALSE,");
            sb.AppendLine("    created_at TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP");
            sb.AppendLine(");");
            sb.AppendLine("TRUNCATE TABLE public.otp_codes RESTART IDENTITY CASCADE;");
            var otpCodes = await _context.OtpCodes.OrderBy(o => o.Id).ToListAsync();
            foreach (var o in otpCodes)
                sb.AppendLine($"INSERT INTO public.otp_codes (id, user_id, email, code, purpose_id, expires_at, used, created_at) " +
                    $"VALUES ({o.Id}, {EscN(o.UserId)}, {Esc(o.Email)}, {Esc(o.Code)}, {o.PurposeId}, " +
                    $"'{o.ExpiresAt:yyyy-MM-dd HH:mm:ss}', {EscB(o.Used)}, {EscDt(o.CreatedAt)});");
            sb.AppendLine();

            sb.AppendLine("-- password_reset_tokens / токены сброса пароля");
            sb.AppendLine("CREATE TABLE IF NOT EXISTS public.password_reset_tokens (");
            sb.AppendLine("    id         SERIAL PRIMARY KEY,");
            sb.AppendLine("    user_id    INTEGER      REFERENCES public.users(id) ON DELETE CASCADE,");
            sb.AppendLine("    token      VARCHAR(255) NOT NULL,");
            sb.AppendLine("    expires_at TIMESTAMP    NOT NULL DEFAULT (CURRENT_TIMESTAMP + INTERVAL '1 hour'),");
            sb.AppendLine("    used       BOOLEAN      NOT NULL DEFAULT FALSE,");
            sb.AppendLine("    created_at TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,");
            sb.AppendLine("    CONSTRAINT uq_password_reset_tokens_token UNIQUE (token)");
            sb.AppendLine(");");
            sb.AppendLine("TRUNCATE TABLE public.password_reset_tokens RESTART IDENTITY CASCADE;");
            var prtokens = await _context.PasswordResetTokens.OrderBy(t => t.Id).ToListAsync();
            foreach (var t in prtokens)
                sb.AppendLine($"INSERT INTO public.password_reset_tokens (id, user_id, token, expires_at, used, created_at) " +
                    $"VALUES ({t.Id}, {EscN(t.UserId)}, {Esc(t.Token)}, '{t.ExpiresAt:yyyy-MM-dd HH:mm:ss}', " +
                    $"{EscB(t.Used)}, {EscDt(t.CreatedAt)});");
            sb.AppendLine();

            sb.AppendLine("-- password_reset_attempts / попытки сброса пароля");
            sb.AppendLine("CREATE TABLE IF NOT EXISTS public.password_reset_attempts (");
            sb.AppendLine("    id         SERIAL PRIMARY KEY,");
            sb.AppendLine("    email      VARCHAR(255) NOT NULL,");
            sb.AppendLine("    ip_address VARCHAR(45),");
            sb.AppendLine("    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP");
            sb.AppendLine(");");
            sb.AppendLine("CREATE INDEX IF NOT EXISTS idx_password_reset_attempts_email ON public.password_reset_attempts (email, created_at);");
            sb.AppendLine("TRUNCATE TABLE public.password_reset_attempts RESTART IDENTITY CASCADE;");
            var prAttempts = await _context.PasswordResetAttempts.OrderBy(a => a.Id).ToListAsync();
            foreach (var a in prAttempts)
                sb.AppendLine($"INSERT INTO public.password_reset_attempts (id, email, ip_address, created_at) " +
                    $"VALUES ({a.Id}, {Esc(a.Email)}, {Esc(a.IpAddress)}, {EscDt(a.CreatedAt)});");
            sb.AppendLine();

            sb.AppendLine("-- rating_history / рейтинговая история");
            sb.AppendLine("CREATE TABLE IF NOT EXISTS public.rating_history (");
            sb.AppendLine("    id            SERIAL PRIMARY KEY,");
            sb.AppendLine("    user_id       INTEGER   REFERENCES public.users(id) ON DELETE CASCADE,");
            sb.AppendLine("    old_rating    INTEGER   NOT NULL,");
            sb.AppendLine("    new_rating    INTEGER   NOT NULL,");
            sb.AppendLine("    reason_id     INTEGER   NOT NULL REFERENCES public.rating_change_reasons(id) ON DELETE RESTRICT,");
            sb.AppendLine("    task_id       INTEGER,");
            sb.AppendLine("    topic_id      INTEGER,");
            sb.AppendLine("    tournament_id INTEGER,");
            sb.AppendLine("    created_at    TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP");
            sb.AppendLine(");");
            sb.AppendLine("TRUNCATE TABLE public.rating_history RESTART IDENTITY CASCADE;");
            var ratingHistory = await _context.RatingHistories.OrderBy(h => h.Id).ToListAsync();
            foreach (var h in ratingHistory)
                sb.AppendLine($"INSERT INTO public.rating_history (id, user_id, old_rating, new_rating, reason_id, task_id, topic_id, tournament_id, created_at) " +
                    $"VALUES ({h.Id}, {EscN(h.UserId)}, {h.OldRating}, {h.NewRating}, " +
                    $"{EscN(h.ReasonId)}, {EscN(h.TaskId)}, {EscN(h.TopicId)}, {EscN(h.TournamentId)}, '{h.CreatedAt:yyyy-MM-dd HH:mm:ss}');");
            sb.AppendLine();

            sb.AppendLine("-- topics / темы");
            sb.AppendLine("CREATE TABLE IF NOT EXISTS public.topics (");
            sb.AppendLine("    id            SERIAL PRIMARY KEY,");
            sb.AppendLine("    name          VARCHAR(255) NOT NULL,");
            sb.AppendLine("    description   TEXT,");
            sb.AppendLine("    display_order INTEGER NOT NULL DEFAULT 0");
            sb.AppendLine(");");
            sb.AppendLine("TRUNCATE TABLE public.topics RESTART IDENTITY CASCADE;");
            var topicsSql = await _context.Topics.OrderBy(t => t.Id).ToListAsync();
            foreach (var t in topicsSql)
                sb.AppendLine($"INSERT INTO public.topics (id, name, description, display_order) " +
                    $"VALUES ({t.Id}, {Esc(t.Name)}, {Esc(t.Description)}, {t.DisplayOrder});");
            sb.AppendLine();

            sb.AppendLine("-- levels / уровни");
            sb.AppendLine("CREATE TABLE IF NOT EXISTS public.levels (");
            sb.AppendLine("    id           SERIAL PRIMARY KEY,");
            sb.AppendLine("    topic_id     INTEGER      NOT NULL REFERENCES public.topics(id) ON DELETE CASCADE,");
            sb.AppendLine("    name         VARCHAR(255) NOT NULL,");
            sb.AppendLine("    level_number INTEGER      NOT NULL,");
            sb.AppendLine("    CONSTRAINT uq_levels_topic_level UNIQUE (topic_id, level_number)");
            sb.AppendLine(");");
            sb.AppendLine("CREATE INDEX IF NOT EXISTS idx_levels_topic_id ON public.levels (topic_id);");
            sb.AppendLine("TRUNCATE TABLE public.levels RESTART IDENTITY CASCADE;");
            var levels = await _context.Levels.OrderBy(l => l.Id).ToListAsync();
            foreach (var l in levels)
                sb.AppendLine($"INSERT INTO public.levels (id, topic_id, name, level_number) " +
                    $"VALUES ({l.Id}, {l.TopicId}, {Esc(l.Name)}, {l.LevelNumber});");
            sb.AppendLine();

            sb.AppendLine("-- theories / теории");
            sb.AppendLine("CREATE TABLE IF NOT EXISTS public.theories (");
            sb.AppendLine("    id       SERIAL PRIMARY KEY,");
            sb.AppendLine("    level_id INTEGER      NOT NULL REFERENCES public.levels(id) ON DELETE CASCADE,");
            sb.AppendLine("    title    VARCHAR(500) NOT NULL,");
            sb.AppendLine("    content  TEXT         NOT NULL,");
            sb.AppendLine("    CONSTRAINT uq_theories_level_id UNIQUE (level_id)");
            sb.AppendLine(");");
            sb.AppendLine("TRUNCATE TABLE public.theories RESTART IDENTITY CASCADE;");
            var theories = await _context.Theories.OrderBy(t => t.Id).ToListAsync();
            foreach (var t in theories)
                sb.AppendLine($"INSERT INTO public.theories (id, level_id, title, content) " +
                    $"VALUES ({t.Id}, {t.LevelId}, {Esc(t.Title)}, {Esc(t.Content)});");
            sb.AppendLine();

            sb.AppendLine("-- difficulty_types / типы сложности");
            sb.AppendLine("CREATE TABLE IF NOT EXISTS public.difficulty_types (");
            sb.AppendLine("    id   SERIAL PRIMARY KEY,");
            sb.AppendLine("    name VARCHAR(100) NOT NULL,");
            sb.AppendLine("    CONSTRAINT uq_difficulty_types_name UNIQUE (name)");
            sb.AppendLine(");");
            sb.AppendLine("TRUNCATE TABLE public.difficulty_types RESTART IDENTITY CASCADE;");
            var diffs = await _context.DifficultyTypes.OrderBy(d => d.Id).ToListAsync();
            foreach (var d in diffs)
                sb.AppendLine($"INSERT INTO public.difficulty_types (id, name) VALUES ({d.Id}, {Esc(d.Name)});");
            sb.AppendLine();

            sb.AppendLine("-- check_types / типы проверки");
            sb.AppendLine("CREATE TABLE IF NOT EXISTS public.check_types (");
            sb.AppendLine("    id   SERIAL PRIMARY KEY,");
            sb.AppendLine("    name VARCHAR(100) NOT NULL,");
            sb.AppendLine("    CONSTRAINT uq_check_types_name UNIQUE (name)");
            sb.AppendLine(");");
            sb.AppendLine("TRUNCATE TABLE public.check_types RESTART IDENTITY CASCADE;");
            var checks = await _context.CheckTypes.OrderBy(c => c.Id).ToListAsync();
            foreach (var c in checks)
                sb.AppendLine($"INSERT INTO public.check_types (id, name) VALUES ({c.Id}, {Esc(c.Name)});");
            sb.AppendLine();

            sb.AppendLine("-- practice_tasks / задания");
            sb.AppendLine("CREATE TABLE IF NOT EXISTS public.practice_tasks (");
            sb.AppendLine("    id                 SERIAL PRIMARY KEY,");
            sb.AppendLine("    level_id           INTEGER      NOT NULL REFERENCES public.levels(id) ON DELETE CASCADE,");
            sb.AppendLine("    name               VARCHAR(500) NOT NULL,");
            sb.AppendLine("    condition          TEXT         NOT NULL,");
            sb.AppendLine("    difficulty_type_id INTEGER      NOT NULL REFERENCES public.difficulty_types(id) ON DELETE RESTRICT,");
            sb.AppendLine("    check_type_id      INTEGER      NOT NULL REFERENCES public.check_types(id) ON DELETE RESTRICT,");
            sb.AppendLine("    display_order      INTEGER      NOT NULL DEFAULT 0");
            sb.AppendLine(");");
            sb.AppendLine("TRUNCATE TABLE public.practice_tasks RESTART IDENTITY CASCADE;");
            var tasksSql = await _context.PracticeTasks.OrderBy(t => t.Id).ToListAsync();
            foreach (var t in tasksSql)
                sb.AppendLine($"INSERT INTO public.practice_tasks (id, level_id, name, condition, difficulty_type_id, check_type_id, display_order) " +
                    $"VALUES ({t.Id}, {t.LevelId}, {Esc(t.Name)}, {Esc(t.Condition)}, " +
                    $"{t.DifficultyTypeId}, {t.CheckTypeId}, {t.DisplayOrder});");
            sb.AppendLine();

            sb.AppendLine("-- custom_check_algorithms / алгоритмы проверки");
            sb.AppendLine("CREATE TABLE IF NOT EXISTS public.custom_check_algorithms (");
            sb.AppendLine("    id             SERIAL PRIMARY KEY,");
            sb.AppendLine("    task_id        INTEGER NOT NULL REFERENCES public.practice_tasks(id) ON DELETE CASCADE,");
            sb.AppendLine("    algorithm_code TEXT    NOT NULL,");
            sb.AppendLine("    CONSTRAINT uq_custom_check_algorithms_task_id UNIQUE (task_id)");
            sb.AppendLine(");");
            sb.AppendLine("TRUNCATE TABLE public.custom_check_algorithms RESTART IDENTITY CASCADE;");
            var algos = await _context.CustomCheckAlgorithms.OrderBy(a => a.Id).ToListAsync();
            foreach (var a in algos)
                sb.AppendLine($"INSERT INTO public.custom_check_algorithms (id, task_id, algorithm_code) " +
                    $"VALUES ({a.Id}, {a.TaskId}, {Esc(a.AlgorithmCode)});");
            sb.AppendLine();

            sb.AppendLine("-- test_cases / тест-кейсы");
            sb.AppendLine("CREATE TABLE IF NOT EXISTS public.test_cases (");
            sb.AppendLine("    id              SERIAL PRIMARY KEY,");
            sb.AppendLine("    task_id         INTEGER NOT NULL REFERENCES public.practice_tasks(id) ON DELETE CASCADE,");
            sb.AppendLine("    input_data      TEXT,");
            sb.AppendLine("    expected_output TEXT    NOT NULL,");
            sb.AppendLine("    is_hidden       BOOLEAN NOT NULL DEFAULT FALSE");
            sb.AppendLine(");");
            sb.AppendLine("CREATE INDEX IF NOT EXISTS idx_test_cases_task_id ON public.test_cases (task_id);");
            sb.AppendLine("TRUNCATE TABLE public.test_cases RESTART IDENTITY CASCADE;");
            var testCases = await _context.TestCases.OrderBy(tc => tc.Id).ToListAsync();
            foreach (var tc in testCases)
                sb.AppendLine($"INSERT INTO public.test_cases (id, task_id, input_data, expected_output, is_hidden) " +
                    $"VALUES ({tc.Id}, {tc.TaskId}, {Esc(tc.InputData)}, {Esc(tc.ExpectedOutput)}, {EscB(tc.IsHidden)});");
            sb.AppendLine();

            sb.AppendLine("-- hints / подсказки");
            sb.AppendLine("CREATE TABLE IF NOT EXISTS public.hints (");
            sb.AppendLine("    id            SERIAL PRIMARY KEY,");
            sb.AppendLine("    task_id       INTEGER NOT NULL REFERENCES public.practice_tasks(id) ON DELETE CASCADE,");
            sb.AppendLine("    hint_text     TEXT    NOT NULL,");
            sb.AppendLine("    display_order INTEGER NOT NULL DEFAULT 0");
            sb.AppendLine(");");
            sb.AppendLine("CREATE INDEX IF NOT EXISTS idx_hints_task_id ON public.hints (task_id);");
            sb.AppendLine("TRUNCATE TABLE public.hints RESTART IDENTITY CASCADE;");
            var hints = await _context.Hints.OrderBy(h => h.TaskId).ThenBy(h => h.DisplayOrder).ToListAsync();
            foreach (var h in hints)
                sb.AppendLine($"INSERT INTO public.hints (id, task_id, hint_text, display_order) " +
                    $"VALUES ({h.Id}, {h.TaskId}, {Esc(h.HintText)}, {h.DisplayOrder});");
            sb.AppendLine();

            sb.AppendLine("-- solutions / решения заданий");
            sb.AppendLine("CREATE TABLE IF NOT EXISTS public.solutions (");
            sb.AppendLine("    id            SERIAL PRIMARY KEY,");
            sb.AppendLine("    user_id       INTEGER   NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,");
            sb.AppendLine("    task_id       INTEGER   NOT NULL REFERENCES public.practice_tasks(id) ON DELETE CASCADE,");
            sb.AppendLine("    solution_code TEXT      NOT NULL,");
            sb.AppendLine("    is_correct    BOOLEAN   NOT NULL DEFAULT FALSE,");
            sb.AppendLine("    created_at    TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP");
            sb.AppendLine(");");
            sb.AppendLine("CREATE INDEX IF NOT EXISTS idx_solutions_user_id   ON public.solutions (user_id);");
            sb.AppendLine("CREATE INDEX IF NOT EXISTS idx_solutions_task_id   ON public.solutions (task_id);");
            sb.AppendLine("CREATE INDEX IF NOT EXISTS idx_solutions_user_task ON public.solutions (user_id, task_id);");
            sb.AppendLine("TRUNCATE TABLE public.solutions RESTART IDENTITY CASCADE;");
            var solutionsSql = await _context.Solutions.OrderBy(s => s.Id).ToListAsync();
            foreach (var s in solutionsSql)
                sb.AppendLine($"INSERT INTO public.solutions (id, user_id, task_id, solution_code, is_correct, created_at) " +
                    $"VALUES ({s.Id}, {s.UserId}, {s.TaskId}, {Esc(s.SolutionCode)}, " +
                    $"{EscB(s.IsCorrect)}, '{s.CreatedAt:yyyy-MM-dd HH:mm:ss}');");
            sb.AppendLine();

            sb.AppendLine("-- tournament_statuses / статусы турниров");
            sb.AppendLine("CREATE TABLE IF NOT EXISTS public.tournament_statuses (");
            sb.AppendLine("    id   SERIAL PRIMARY KEY,");
            sb.AppendLine("    name VARCHAR(100) NOT NULL,");
            sb.AppendLine("    CONSTRAINT uq_tournament_statuses_name UNIQUE (name)");
            sb.AppendLine(");");
            sb.AppendLine("TRUNCATE TABLE public.tournament_statuses RESTART IDENTITY CASCADE;");
            var tStatuses = await _context.TournamentStatuses.OrderBy(s => s.Id).ToListAsync();
            foreach (var s in tStatuses)
                sb.AppendLine($"INSERT INTO public.tournament_statuses (id, name) VALUES ({s.Id}, {Esc(s.Name)});");
            sb.AppendLine();

            sb.AppendLine("-- tournaments / турниры");
            sb.AppendLine("CREATE TABLE IF NOT EXISTS public.tournaments (");
            sb.AppendLine("    id         SERIAL PRIMARY KEY,");
            sb.AppendLine("    topic_id   INTEGER   NOT NULL REFERENCES public.topics(id) ON DELETE CASCADE,");
            sb.AppendLine("    min_rating INTEGER   NOT NULL DEFAULT 0,");
            sb.AppendLine("    max_rating INTEGER   NOT NULL DEFAULT 9999,");
            sb.AppendLine("    status_id  INTEGER   NOT NULL REFERENCES public.tournament_statuses(id) ON DELETE RESTRICT,");
            sb.AppendLine("    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,");
            sb.AppendLine("    CONSTRAINT chk_tournaments_rating CHECK (min_rating <= max_rating)");
            sb.AppendLine(");");
            sb.AppendLine("CREATE INDEX IF NOT EXISTS idx_tournaments_topic_id  ON public.tournaments (topic_id);");
            sb.AppendLine("CREATE INDEX IF NOT EXISTS idx_tournaments_status_id ON public.tournaments (status_id);");
            sb.AppendLine("TRUNCATE TABLE public.tournaments RESTART IDENTITY CASCADE;");
            var tournamentsSql = await _context.Tournaments.OrderBy(t => t.Id).ToListAsync();
            foreach (var t in tournamentsSql)
                sb.AppendLine($"INSERT INTO public.tournaments (id, topic_id, min_rating, max_rating, status_id, created_at) " +
                    $"VALUES ({t.Id}, {t.TopicId}, {t.MinRating}, {t.MaxRating}, " +
                    $"{t.StatusId}, '{t.CreatedAt:yyyy-MM-dd HH:mm:ss}');");
            sb.AppendLine();

            sb.AppendLine("-- questions / вопросы");
            sb.AppendLine("CREATE TABLE IF NOT EXISTS public.questions (");
            sb.AppendLine("    id                 SERIAL PRIMARY KEY,");
            sb.AppendLine("    topic_id           INTEGER NOT NULL REFERENCES public.topics(id) ON DELETE CASCADE,");
            sb.AppendLine("    difficulty_type_id INTEGER NOT NULL REFERENCES public.difficulty_types(id) ON DELETE RESTRICT,");
            sb.AppendLine("    question_text      TEXT    NOT NULL");
            sb.AppendLine(");");
            sb.AppendLine("CREATE INDEX IF NOT EXISTS idx_questions_topic_id           ON public.questions (topic_id);");
            sb.AppendLine("CREATE INDEX IF NOT EXISTS idx_questions_difficulty_type_id ON public.questions (difficulty_type_id);");
            sb.AppendLine("TRUNCATE TABLE public.questions RESTART IDENTITY CASCADE;");
            var questionsSql = await _context.Questions.OrderBy(q => q.Id).ToListAsync();
            foreach (var q in questionsSql)
                sb.AppendLine($"INSERT INTO public.questions (id, topic_id, difficulty_type_id, question_text) " +
                    $"VALUES ({q.Id}, {q.TopicId}, {q.DifficultyTypeId}, {Esc(q.QuestionText)});");
            sb.AppendLine();

            sb.AppendLine("-- answer_options / варианты ответов");
            sb.AppendLine("CREATE TABLE IF NOT EXISTS public.answer_options (");
            sb.AppendLine("    id          SERIAL PRIMARY KEY,");
            sb.AppendLine("    question_id INTEGER NOT NULL REFERENCES public.questions(id) ON DELETE CASCADE,");
            sb.AppendLine("    answer_text TEXT    NOT NULL,");
            sb.AppendLine("    is_correct  BOOLEAN NOT NULL DEFAULT FALSE");
            sb.AppendLine(");");
            sb.AppendLine("CREATE INDEX IF NOT EXISTS idx_answer_options_question_id ON public.answer_options (question_id);");
            sb.AppendLine("TRUNCATE TABLE public.answer_options RESTART IDENTITY CASCADE;");
            var answersSql = await _context.AnswerOptions.OrderBy(a => a.Id).ToListAsync();
            foreach (var a in answersSql)
                sb.AppendLine($"INSERT INTO public.answer_options (id, question_id, answer_text, is_correct) " +
                    $"VALUES ({a.Id}, {a.QuestionId}, {Esc(a.AnswerText)}, {EscB(a.IsCorrect)});");
            sb.AppendLine();

            sb.AppendLine("-- tournament_questions / вопросы турниров");
            sb.AppendLine("CREATE TABLE IF NOT EXISTS public.tournament_questions (");
            sb.AppendLine("    tournament_id   INTEGER NOT NULL REFERENCES public.tournaments(id) ON DELETE CASCADE,");
            sb.AppendLine("    question_id     INTEGER NOT NULL REFERENCES public.questions(id)   ON DELETE CASCADE,");
            sb.AppendLine("    question_number INTEGER NOT NULL,");
            sb.AppendLine("    PRIMARY KEY (tournament_id, question_id),");
            sb.AppendLine("    CONSTRAINT uq_tournament_question_number UNIQUE (tournament_id, question_number)");
            sb.AppendLine(");");
            sb.AppendLine("CREATE INDEX IF NOT EXISTS idx_tournament_questions_question_id ON public.tournament_questions (question_id);");
            sb.AppendLine("TRUNCATE TABLE public.tournament_questions;");
            var tqSql = await _context.TournamentQuestions.OrderBy(x => x.TournamentId).ThenBy(x => x.QuestionNumber).ToListAsync();
            foreach (var tq in tqSql)
                sb.AppendLine($"INSERT INTO public.tournament_questions (tournament_id, question_id, question_number) " +
                    $"VALUES ({tq.TournamentId}, {tq.QuestionId}, {tq.QuestionNumber});");
            sb.AppendLine();

            sb.AppendLine("-- player_answers / ответы игроков");
            sb.AppendLine("CREATE TABLE IF NOT EXISTS public.player_answers (");
            sb.AppendLine("    id               SERIAL PRIMARY KEY,");
            sb.AppendLine("    user_id          INTEGER   NOT NULL REFERENCES public.users(id)          ON DELETE CASCADE,");
            sb.AppendLine("    question_id      INTEGER   NOT NULL REFERENCES public.questions(id)      ON DELETE CASCADE,");
            sb.AppendLine("    tournament_id    INTEGER   NOT NULL REFERENCES public.tournaments(id)    ON DELETE CASCADE,");
            sb.AppendLine("    answer_option_id INTEGER   NOT NULL REFERENCES public.answer_options(id) ON DELETE RESTRICT,");
            sb.AppendLine("    answered_at      TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,");
            sb.AppendLine("    CONSTRAINT uq_player_answers_user_question_tournament UNIQUE (user_id, question_id, tournament_id)");
            sb.AppendLine(");");
            sb.AppendLine("CREATE INDEX IF NOT EXISTS idx_player_answers_user_id       ON public.player_answers (user_id);");
            sb.AppendLine("CREATE INDEX IF NOT EXISTS idx_player_answers_tournament_id ON public.player_answers (tournament_id);");
            sb.AppendLine("CREATE INDEX IF NOT EXISTS idx_player_answers_question_id   ON public.player_answers (question_id);");
            sb.AppendLine("TRUNCATE TABLE public.player_answers RESTART IDENTITY CASCADE;");
            var paSql = await _context.PlayerAnswers.OrderBy(pa => pa.Id).ToListAsync();
            foreach (var pa in paSql)
                sb.AppendLine($"INSERT INTO public.player_answers (id, user_id, question_id, tournament_id, answer_option_id, answered_at) " +
                    $"VALUES ({pa.Id}, {pa.UserId}, {pa.QuestionId}, {pa.TournamentId}, " +
                    $"{pa.AnswerOptionId}, '{pa.AnsweredAt:yyyy-MM-dd HH:mm:ss}');");
            sb.AppendLine();

            sb.AppendLine("-- End of backup / Конец резервной копии");

            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.sql";
            return File(bytes, "application/sql", fileName);
        }

        [Route("admin/error")]
        [HttpGet]
        public IActionResult Error(int? statusCode)
        {
            if (statusCode.HasValue)
                Response.StatusCode = statusCode.Value;
            return View("~/Views/Admin/Error.cshtml");
        }
    }
}
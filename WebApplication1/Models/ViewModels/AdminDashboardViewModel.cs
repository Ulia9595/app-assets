namespace WebApplication1.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int PlayersCount { get; set; }
        public int RolesCount { get; set; }
        public int AvatarsCount { get; set; }

        public int OtpCodesCount { get; set; }
        public int ActiveOtpCodesCount { get; set; }

        public int PasswordResetTokensCount { get; set; }
        public int PasswordResetAttemptsCount { get; set; }

        public int UserRatingsCount { get; set; }
        public int RatingHistoryCount { get; set; }

        public double AverageRating { get; set; }
        public int MaxRating { get; set; }
        public int MinRating { get; set; }

        public int TopicsCount { get; set; } = 0;
        public int LevelsCount { get; set; } = 0;
        public int TasksCount { get; set; } = 0;
        public int TournamentsCount { get; set; } = 0;
        public LearningContentViewModel LearningContent { get; set; } = new();
        public TasksContentViewModel TasksContent { get; set; } = new();
        public PvpContentViewModel PvpContent { get; set; } = new();
        public UsersContentViewModel UsersContent { get; set; } = new();
        public AnalyticsViewModel AnalyticsContent { get; set; } = new();
    }

    public class LearningContentViewModel
    {
        public List<TopicWithLevelsDto> Topics { get; set; } = new();
    }

    public class TopicWithLevelsDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
        public List<LevelWithTheoryDto> Levels { get; set; } = new();
    }

    public class LevelWithTheoryDto
    {
        public int Id { get; set; }
        public int TopicId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int LevelNumber { get; set; }
        public TheoryDto? Theory { get; set; }
    }

    public class TheoryDto
    {
        public int Id { get; set; }
        public int LevelId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }

    public class TasksContentViewModel
    {
        public List<TaskTopicDto> Topics { get; set; } = new();
        public List<DifficultyTypeDto> DifficultyTypes { get; set; } = new();
        public List<CheckTypeDto> CheckTypes { get; set; } = new();
    }

    public class TaskTopicDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<TaskLevelDto> Levels { get; set; } = new();
    }

    public class TaskLevelDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int LevelNumber { get; set; }
        public int TopicId { get; set; }
        public List<PracticeTaskDto> Tasks { get; set; } = new();
    }

    public class PracticeTaskDto
    {
        public int Id { get; set; }
        public int LevelId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public int DifficultyTypeId { get; set; }
        public string DifficultyTypeName { get; set; } = string.Empty;
        public int CheckTypeId { get; set; }
        public string CheckTypeName { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public List<TestCaseDto> TestCases { get; set; } = new();
        public List<HintDto> Hints { get; set; } = new();
        public CustomCheckAlgorithmDto? CustomCheckAlgorithm { get; set; }
    }

    public class TestCaseDto
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public string? InputData { get; set; }
        public string ExpectedOutput { get; set; } = string.Empty;
        public bool IsHidden { get; set; }
    }

    public class HintDto
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public string HintText { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }

    public class CustomCheckAlgorithmDto
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public string AlgorithmCode { get; set; } = string.Empty;
    }

    public class DifficultyTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class CheckTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class PvpContentViewModel
    {
        public List<PvpTournamentDto> Tournaments { get; set; } = new();
        public List<PvpQuestionDto> Questions { get; set; } = new();
        public List<PvpTopicDto> Topics { get; set; } = new();
        public List<PvpStatusDto> Statuses { get; set; } = new();
        public List<PvpDifficultyDto> DifficultyTypes { get; set; } = new();
    }

    public class PvpTournamentDto
    {
        public int Id { get; set; }
        public int TopicId { get; set; }
        public string TopicName { get; set; } = string.Empty;
        public int StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public int MinRating { get; set; }
        public int MaxRating { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<PvpTournamentQuestionDto> Questions { get; set; } = new();
    }

    public class PvpTournamentQuestionDto
    {
        public int TournamentId { get; set; }
        public int QuestionId { get; set; }
        public int QuestionNumber { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string DifficultyTypeName { get; set; } = string.Empty;
    }

    public class PvpQuestionDto
    {
        public int Id { get; set; }
        public int TopicId { get; set; }
        public string TopicName { get; set; } = string.Empty;
        public int DifficultyTypeId { get; set; }
        public string DifficultyTypeName { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
        public List<PvpAnswerOptionDto> AnswerOptions { get; set; } = new();
    }

    public class PvpAnswerOptionDto
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public string AnswerText { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }

    public class PvpTopicDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class PvpStatusDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class PvpDifficultyDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class UsersContentViewModel
    {
        public List<UserRowDto> Users { get; set; } = new();
        public int TotalTopics { get; set; }
        public int TotalLevels { get; set; }
    }

    public class UserRowDto
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public int CurrentRating { get; set; }
        public DateTime CreatedAt { get; set; }
        public int SolvedTasksCount { get; set; }
        public int CompletedTopicsCount { get; set; }
        public int CompletedLevelsCount { get; set; }
        public int TournamentsPlayedCount { get; set; }
    }

    public class AnalyticsViewModel
    {
        public List<EloRangeDto> EloDistribution { get; set; } = new();

        public List<ProgressPointDto> LearningProgress { get; set; } = new();

        public int SolutionsCorrect { get; set; }
        public int SolutionsWrong { get; set; }

        public List<PvpStatsDto> PvpStats { get; set; } = new();

        public List<UserAnalyticsDto> PerUserAnalytics { get; set; } = new();
        public List<string> PlayerNames { get; set; } = new();
    }

    public class EloRangeDto
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class ProgressPointDto
    {
        public string Month { get; set; } = string.Empty;
        public int TopicsCompleted { get; set; }
        public int LevelsCompleted { get; set; }
    }

    public class PvpStatsDto
    {
        public int TournamentId { get; set; }
        public string TopicName { get; set; } = string.Empty;
        public int CorrectAnswers { get; set; }
        public int WrongAnswers { get; set; }
    }

    public class UserAnalyticsDto
    {
        public int UserId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public List<ProgressPointDto> Progress { get; set; } = new();
        public int SolutionsCorrect { get; set; }
        public int SolutionsWrong { get; set; }
        public List<PvpStatsDto> PvpStats { get; set; } = new();
    }
}
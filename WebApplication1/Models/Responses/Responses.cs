namespace WebApplication1.Models.Responses
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Error { get; set; }

        public static ApiResponse<T> Ok(T data) => new() { Success = true, Data = data };
        public static ApiResponse<T> Fail(string error) => new() { Success = false, Error = error };
    }

    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public UserResponse User { get; set; } = new();
    }

    public class UserResponse
    {
        public string Uid { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "player";
        public string? Name { get; set; }

        public int? AvatarId { get; set; }
        public string? AvatarUrl { get; set; }

        public int EloPoints { get; set; } = 500;

        public int Level => EloPoints / 1000;
        public int PointsInCurrentLevel => EloPoints % 1000;
        public float LevelProgress => PointsInCurrentLevel / 1000f;
        public int PointsToNextLevel => 1000 - PointsInCurrentLevel;
    }

    public class AvatarResponse
    {
        public int Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }

    public class OtpResponse
    {
        public bool Sent { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class TopicResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
        public int LevelCount { get; set; }
        public int CompletedLevelCount { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsLocked { get; set; }

        public float TopicProgress => LevelCount > 0
            ? (float)CompletedLevelCount / LevelCount
            : 0f;
    }

    public class LevelResponse
    {
        public int Id { get; set; }
        public int TopicId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int LevelNumber { get; set; }
        public bool HasTheory { get; set; }
        public int TaskCount { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsLocked { get; set; }
        public int TasksCompleted { get; set; }
    }

    public class LevelDetailResponse
    {
        public int Id { get; set; }
        public int TopicId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int LevelNumber { get; set; }
        public TheoryResponse? Theory { get; set; }
        public List<PracticeTaskResponse> Tasks { get; set; } = new();
    }

    public class TheoryResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class PracticeTaskResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsSolved { get; set; }
    }

    public class TaskDetailResponse
    {
        public int Id { get; set; }
        public int LevelId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string CheckType { get; set; } = string.Empty;
        public bool IsSolved { get; set; }
        public List<TestCaseResponse> PublicTestCases { get; set; } = new();
        public List<HintResponse> Hints { get; set; } = new();
    }

    public class TestCaseResponse
    {
        public int Id { get; set; }
        public string? InputData { get; set; }
        public string ExpectedOutput { get; set; } = string.Empty;
    }

    public class HintResponse
    {
        public int Id { get; set; }
        public string HintText { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }

    public class RunResponse
    {
        public string Stdout { get; set; } = string.Empty;
        public string Stderr { get; set; } = string.Empty;
        public string CompileOutput { get; set; } = string.Empty;
        public int ExitCode { get; set; }
        public bool IsSuccess { get; set; }
    }

    public class SubmitResponse
    {
        public bool IsCorrect { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<TestResultResponse> TestResults { get; set; } = new();
        public int EloGained { get; set; }
        public int PassedCount { get; set; }
        public int TotalCount { get; set; }
    }

    public class TestResultResponse
    {
        public int TestCaseId { get; set; }
        public bool Passed { get; set; }
        public string? InputData { get; set; }
        public string? ExpectedOutput { get; set; }
        public string? ActualOutput { get; set; }
        public bool IsHidden { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class TournamentDetailResponse
    {
        public int TournamentId { get; set; }
        public string TopicName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsParticipant { get; set; }
        public List<TournamentQuestionResponse> Questions { get; set; } = new();
        public TournamentResultResponse? Result { get; set; }
    }

    public class TournamentQuestionResponse
    {
        public int QuestionNumber { get; set; }
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public int Points { get; set; }
        public List<AnswerOptionResponse> Options { get; set; } = new();
        public int? MyAnswerOptionId { get; set; }
        public bool IsAnswered { get; set; }
        public bool? IsCorrect { get; set; }
    }

    public class AnswerOptionResponse
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool? IsCorrect { get; set; }
    }

    public class TournamentResultResponse
    {
        public string Outcome { get; set; } = string.Empty;
        public List<PlayerResultResponse> Players { get; set; } = new();
    }

    public class PlayerResultResponse
    {
        public int UserId { get; set; }
        public int Score { get; set; }
        public int RatingDelta { get; set; }
    }

    public class TournamentHistoryResponse
    {
        public int TournamentId { get; set; }
        public string TopicName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int? RatingDelta { get; set; }
    }

    public class QueueStatusResponse
    {
        public bool InQueue { get; set; }
    }

    public class RatingHistoryResponse
    {
        public int Id { get; set; }
        public int OldRating { get; set; }
        public int NewRating { get; set; }
        public int Delta { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? TaskName { get; set; }
        public string? TopicName { get; set; }
        public int? TournamentId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class LeaderboardEntryResponse
    {
        public int UserId { get; set; }
        public int Rank { get; set; }
        public string Name { get; set; } = string.Empty;
        public int EloPoints { get; set; }
        public string? AvatarUrl { get; set; }
    }

    public class LeaderboardResponse
    {
        public List<LeaderboardEntryResponse> Entries { get; set; } = new();
        public int CurrentUserRank { get; set; }
    }
}
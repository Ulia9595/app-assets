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
        public string? Name { get; set; }
        public string? AvatarUrl { get; set; }
        public int EloPoints { get; set; } = 500;
        public bool IsEmailVerified { get; set; }

        public int Level => EloPoints / 1000;
        public int PointsInCurrentLevel => EloPoints % 1000;
        public float LevelProgress => PointsInCurrentLevel / 1000f;
        public int PointsToNextLevel => 1000 - PointsInCurrentLevel;
    }

    public class OtpResponse
    {
        public bool Sent { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

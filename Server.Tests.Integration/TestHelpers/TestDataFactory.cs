using System;
using System.Security.Claims;
using WebApplication1.Models.Entities;
using WebApplication1.Models.Requests;
using WebApplication1.Models.Responses;
using BCrypt.Net;

namespace Server.Tests.Integration.TestHelpers;

public static class TestDataFactory
{

    public static User CreateTestUser(
        int id = 1,
        string email = "test@example.com",
        string name = "Test User",
        string uid = null,
        int eloPoints = 500,
        bool isEmailVerified = true,
        string avatarUrl = "https://example.com/avatar.jpg",
        string password = "ValidPass123!")
    {
        return new User
        {
            Id = id,
            Uid = uid ?? $"test-uid-{id}",
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Name = name,
            AvatarUrl = avatarUrl,
            EloPoints = eloPoints,
            IsEmailVerified = isEmailVerified,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static User CreateUserForRegistration(
        string email = null,
        string name = "New User",
        string password = "ValidPass123!")
    {
        return CreateTestUser(
            id: 100,
            email: email ?? GenerateTestEmail(),
            name: name,
            uid: "new-user-uid",
            isEmailVerified: false,
            password: password
        );
    }

    public static User CreateUserForPasswordReset(
        string email = null,
        string name = "Reset User",
        string password = "OldPass123!")
    {
        return CreateTestUser(
            id: 200,
            email: email ?? "reset@example.com",
            name: name,
            uid: "reset-user-uid",
            password: password
        );
    }

    public static RegisterRequest CreateRegisterRequest(
        string email = null,
        string name = "New User",
        string password = "ValidPass123!",
        string avatarUrl = "https://example.com/avatar.jpg")
    {
        return new RegisterRequest
        {
            Email = email ?? GenerateTestEmail(),
            Password = password,
            PasswordRepeat = password,
            Name = name,
            AvatarUrl = avatarUrl
        };
    }

    public static LoginRequest CreateLoginRequest(
        string email = "test@example.com",
        string password = "ValidPass123!")
    {
        return new LoginRequest
        {
            Email = email,
            Password = password
        };
    }

    public static UpdateProfileRequest CreateUpdateProfileRequest(
        string name = "Updated Name",
        string avatarUrl = "https://example.com/new-avatar.jpg")
    {
        return new UpdateProfileRequest
        {
            Name = name,
            AvatarUrl = avatarUrl
        };
    }

    public static ForgotPasswordRequest CreateForgotPasswordRequest(
        string email = "user@example.com")
    {
        return new ForgotPasswordRequest
        {
            Email = email
        };
    }

    public static ResetPasswordRequest CreateResetPasswordRequest(
        string token = "reset-token-123",
        string newPassword = "NewPass123!")
    {
        return new ResetPasswordRequest
        {
            Token = token,
            NewPassword = newPassword,
            ConfirmPassword = newPassword
        };
    }

    public static ChangeEmailRequest CreateChangeEmailRequest(
        string newEmail = null)
    {
        return new ChangeEmailRequest
        {
            NewEmail = newEmail ?? $"newemail.{Guid.NewGuid():N}@example.com"
        };
    }

    public static AuthResponse CreateAuthResponse(
        User user = null,
        string token = "test-jwt-token-123")
    {
        user ??= CreateTestUser();

        return new AuthResponse
        {
            Token = token,
            User = new UserResponse
            {
                Uid = user.Uid!,
                Email = user.Email,
                Name = user.Name,
                AvatarUrl = user.AvatarUrl,
                EloPoints = user.EloPoints,
                IsEmailVerified = user.IsEmailVerified
            }
        };
    }

    public static ApiResponse<AuthResponse> CreateAuthApiResponse(
        User user = null,
        string token = "test-jwt-token-123")
    {
        return ApiResponse<AuthResponse>.Ok(CreateAuthResponse(user, token));
    }

    public static ApiResponse<UserResponse> CreateUserResponse(
        User user = null)
    {
        user ??= CreateTestUser();

        var userResponse = new UserResponse
        {
            Uid = user.Uid!,
            Email = user.Email,
            Name = user.Name,
            AvatarUrl = user.AvatarUrl,
            EloPoints = user.EloPoints,
            IsEmailVerified = user.IsEmailVerified
        };

        return ApiResponse<UserResponse>.Ok(userResponse);
    }

    public static ApiResponse<string> CreateSuccessStringResponse(
        string message = "Операция выполнена успешно")
    {
        return ApiResponse<string>.Ok(message);
    }

    public static ApiResponse<bool> CreateSuccessBoolResponse(bool data = true)
    {
        return ApiResponse<bool>.Ok(data);
    }

    public static OtpCode CreateOtpCode(
        string email = "test@example.com",
        string code = null,
        string purpose = "registration",
        bool used = false,
        int? userId = null)
    {
        return new OtpCode
        {
            UserId = userId,
            Email = email,
            Code = code ?? new Random().Next(100000, 999999).ToString(),
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            Used = used,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static PasswordResetToken CreatePasswordResetToken(
        int userId = 1,
        string token = null,
        bool used = false,
        DateTime? expiresAt = null)
    {
        return new PasswordResetToken
        {
            UserId = userId,
            Token = token ?? $"reset-token-{Guid.NewGuid():N}",
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddHours(1),
            Used = used,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static UserRating CreateUserRating(
        int userId = 1,
        int currentRating = 500)
    {
        return new UserRating
        {
            UserId = userId,
            CurrentRating = currentRating,
            Level = currentRating / 1000,
            GamesPlayed = 0,
            GamesWon = 0,
            GamesLost = 0,
            WinStreak = 0,
            BestRating = currentRating,
            LastUpdated = DateTime.UtcNow
        };
    }

    public static AvailableAvatar CreateAvailableAvatar(
        int id = 1,
        string url = "https://example.com/avatar1.jpg")
    {
        return new AvailableAvatar
        {
            Id = id,
            Category = "cats",
            Url = url,
            DisplayOrder = id,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static List<User> CreateUserList(int count = 3)
    {
        var users = new List<User>();
        for (int i = 1; i <= count; i++)
        {
            users.Add(CreateTestUser(
                id: i,
                email: $"user{i}@example.com",
                name: $"User {i}",
                uid: $"uid-{i}"
            ));
        }
        return users;
    }

    public static List<AvailableAvatar> CreateAvatarList(int count = 6)
    {
        var avatars = new List<AvailableAvatar>();
        for (int i = 1; i <= count; i++)
        {
            avatars.Add(CreateAvailableAvatar(
                id: i,
                url: $"https://example.com/avatar{i}.jpg"
            ));
        }
        return avatars;
    }

    public static List<Claim> CreateUserClaims(
        int userId = 1,
        string email = "test@example.com",
        string uid = "test-uid")
    {
        return new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim("uid", uid)
        };
    }

    public static string GenerateTestToken()
    {
        return $"test-jwt-{Guid.NewGuid():N}";
    }

    public static string GenerateTestUid()
    {
        return $"uid-{Guid.NewGuid():N}";
    }

    public static string GenerateTestEmail()
    {
        return $"test-{Guid.NewGuid():N}@example.com";
    }

    public static string GenerateValidPassword()
    {
        return "ValidPass123!";
    }
}
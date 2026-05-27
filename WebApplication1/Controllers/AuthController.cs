using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication1.Data;
using WebApplication1.Models.Entities;
using WebApplication1.Models.Requests;
using WebApplication1.Models.Responses;
using WebApplication1.Services;
using WebApplication1.Utils;
using WebApplication1.Filters;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly JwtService _jwtService;
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly UsernameService _usernameService;

        public AuthController(
            AuthService authService,
            JwtService jwtService,
            AppDbContext context,
            IConfiguration configuration,
            UsernameService usernameService)
        {
            _authService = authService;
            _jwtService = jwtService;
            _context = context;
            _configuration = configuration;
            _usernameService = usernameService;
        }

        [HttpGet("public")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicInfo()
        {
            try
            {
                var dbConnected = await _context.Database.CanConnectAsync();
                var userCount = await _context.Users.CountAsync();
                var avatarCount = 0;

                try
                {
                    await _context.Database.OpenConnectionAsync();
                    using var command = _context.Database.GetDbConnection().CreateCommand();
                    command.CommandText = "SELECT COUNT(*) FROM available_avatars";
                    var count = await command.ExecuteScalarAsync();
                    avatarCount = Convert.ToInt32(count);
                    _context.Database.CloseConnection();
                }
                catch
                {
                    avatarCount = 0;
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        serverStatus = "running",
                        databaseConnected = dbConnected,
                        totalUsers = userCount,
                        totalAvatars = avatarCount,
                        serverTime = DateTime.UtcNow,
                        version = "1.0.0",
                        features = new[]
                        {
                            "Authentication",
                            "Profile Management",
                            "Avatar Selection",
                            "Elo Rating System"
                        }
                    },
                    error = (string?)null
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = false,
                    data = (object?)null,
                    error = ex.Message
                });
            }
        }

        [HttpPost("create-test-user")]
        public async Task<IActionResult> CreateTestUser()
        {
            try
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == "test@example.com");

                if (existingUser != null)
                {
                    _context.Users.Remove(existingUser);
                    await _context.SaveChangesAsync();
                }

                var playerRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Code == "player");

                if (playerRole == null)
                    return StatusCode(500, new { error = "Роль player не найдена" });

                var firstAvatar = await _context.AvailableAvatars
                    .OrderBy(a => a.DisplayOrder)
                    .FirstOrDefaultAsync();

                var user = new User
                {
                    Uid = "test-" + Guid.NewGuid(),
                    Email = "test@example.com",
                    PasswordHash = PasswordHelper.HashPassword("123456"),
                    RoleId = playerRole.Id,
                    Name = "Test User",
                    AvatarId = firstAvatar?.Id,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var userRating = await _context.UserRatings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.UserId == user.Id);

                if (userRating == null)
                {
                    return StatusCode(500, new { error = "Рейтинг пользователя не создан" });
                }

                _context.UserRatings.Add(userRating);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Test user created",
                    email = user.Email,
                    password = "123456",
                    role = playerRole.Code,
                    userId = user.Id,
                    avatarId = user.AvatarId,
                    avatarUrl = firstAvatar?.Url,
                    eloPoints = userRating.CurrentRating
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("send-otp")]
        [AllowAnonymous]
        [RateLimit(maxRequests: 5, timeWindowMinutes: 5, limitType: RateLimitType.Ip)]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
        {
            var result = await _authService.SendOtpAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("verify-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            var result = await _authService.VerifyOtpAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("check-username/{username}")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckUsername(string username)
        {
            var result = await _usernameService.ValidateAndCheckUsernameAsync(username);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        [HttpGet("username-suggestions")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUsernameSuggestions()
        {
            var result = await _usernameService.GenerateSuggestionsAsync(3);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        [HttpPost("register")]
        [AllowAnonymous]
        [RateLimit(maxRequests: 5, timeWindowMinutes: 1, limitType: RateLimitType.Ip)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var validator = new PasswordValidator();
            var validation = validator.GetDetailedValidation(request.Password);
            if (!validation.IsValid)
            {
                var failed = validation.Requirements.Where(r => !r.Value).Select(r => r.Key);
                return BadRequest(new ApiResponse<bool> { Success = false, Error = $"Пароль не соответствует требованиям: {string.Join(", ", failed)}" });
            }

            var result = await _authService.RegisterAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [RateLimit(maxRequests: 10, timeWindowMinutes: 1, limitType: RateLimitType.Ip)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);
            return result.Success ? Ok(result) : Unauthorized(result);
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [RateLimit(maxRequests: 3, timeWindowMinutes: 10, limitType: RateLimitType.Ip)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString()
                           ?? HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                           ?? HttpContext.Request.Headers["X-Real-IP"].FirstOrDefault()
                           ?? "127.0.0.1";

            var result = await _authService.ForgotPasswordAsync(request, clientIp);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var result = await _authService.ResetPasswordAsync(request);
            if (result.Success)
            {
                var token = await _context.PasswordResetTokens.FirstOrDefaultAsync(t => t.Token == request.Token);
                if (token != null)
                {
                    token.Used = true;
                    await _context.SaveChangesAsync();
                }

                return Ok(new { success = true, message = "Пароль успешно изменен", redirectUrl = "/static/reset-success.html" });
            }

            return BadRequest(new { success = false, error = result.Error, redirectUrl = $"/static/reset-invalid.html?message={Uri.EscapeDataString(result.Error)}" });
        }

        [Authorize]
        [HttpPost("send-email-change-otp")]
        [RateLimit(maxRequests: 3, timeWindowMinutes: 10, limitType: RateLimitType.User)]
        public async Task<IActionResult> SendEmailChangeOtp([FromBody] ChangeEmailRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email)) return Unauthorized();

            var result = await _authService.SendEmailChangeOtpAsync(email, request.NewEmail);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [Authorize]
        [HttpPost("change-email")]
        [RateLimit(maxRequests: 5, timeWindowMinutes: 1, limitType: RateLimitType.User)]
        public async Task<IActionResult> ChangeEmail([FromBody] VerifyOtpRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var result = await _authService.ChangeEmailAsync(userId.Value, request.Email, request.Code);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [Authorize]
        [HttpPut("elo/{points}")]
        [RateLimit(maxRequests: 10, timeWindowMinutes: 1, limitType: RateLimitType.User)]
        public async Task<IActionResult> UpdateEloPoints(int points)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var result = await _authService.UpdateEloPointsAsync(userId.Value, points);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [Authorize]
        [HttpPost("logout")]
        public IActionResult Logout() => Ok(new { message = "Успешный выход" });

        [Authorize]
        [HttpGet("validate")]
        public IActionResult Validate()
        {
            var userId = GetCurrentUserId();
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var uid = User.FindFirst("uid")?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            return Ok(new { isValid = true, userId, email, uid, role });
        }

        [Authorize]
        [HttpGet("debug-claims")]
        public IActionResult DebugClaims()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();

            return Ok(new
            {
                claims,
                hasUid = claims.Any(c => c.Type == "uid"),
                hasEmail = claims.Any(c => c.Type == ClaimTypes.Email),
                hasUserId = claims.Any(c => c.Type == ClaimTypes.NameIdentifier),
                hasRole = claims.Any(c => c.Type == ClaimTypes.Role)
            });
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
                return userId;

            var uidClaim = User.FindFirst("uid")?.Value;
            if (string.IsNullOrEmpty(uidClaim)) return null;

            var user = _context.Users.AsNoTracking().FirstOrDefault(u => u.Uid == uidClaim);
            return user?.Id;
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using WebApplication1.Models.Requests;
using WebApplication1.Models.Responses;
using WebApplication1.Services;
using WebApplication1.Data;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly ProfileService _profileService;
        private readonly AppDbContext _context;
        private readonly RatingHistoryService _ratingHistoryService;
        private readonly LeaderboardService _leaderboardService;

        public ProfileController(ProfileService profileService, AppDbContext context, RatingHistoryService ratingHistoryService, LeaderboardService leaderboardService)
        {
            _profileService = profileService;
            _context = context;
            _ratingHistoryService = ratingHistoryService;
            _leaderboardService = leaderboardService;
            _leaderboardService = leaderboardService;
        }

        private string? GetCurrentUid()
        {
            var uid = User.FindFirst("uid")?.Value;

            if (string.IsNullOrEmpty(uid))
                uid = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

            if (string.IsNullOrEmpty(uid))
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
                {
                    var user = _context.Users.AsNoTracking().FirstOrDefault(u => u.Id == userId);
                    if (user != null)
                        uid = user.Uid;
                }
            }
            return uid;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var uid = GetCurrentUid();
            if (string.IsNullOrEmpty(uid)) return Unauthorized();

            var result = await _profileService.GetProfileAsync(uid);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var uid = GetCurrentUid();
            if (string.IsNullOrEmpty(uid)) return Unauthorized();

            var result = await _profileService.UpdateProfileAsync(uid, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("avatars")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAvailableAvatars()
        {
            var result = await _profileService.GetAvailableAvatarsAsync();
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("complete-registration")]
        public async Task<IActionResult> CompleteRegistration([FromBody] CompleteRegistrationRequest request)
        {
            var uid = GetCurrentUid();
            if (string.IsNullOrEmpty(uid)) return Unauthorized();

            var result = await _profileService.CompleteRegistrationAsync(uid, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("check-username/{username}")]
        public async Task<IActionResult> CheckUsername(string username)
        {
            var uid = GetCurrentUid();

            if (string.IsNullOrEmpty(uid))
                return Unauthorized(ApiResponse<bool>.Fail("Пользователь не авторизован"));

            var result = await _profileService.CheckUsernameAsync(username, uid);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        [HttpGet("elo")]
        public async Task<IActionResult> GetEloRating()
        {
            var uid = GetCurrentUid();
            if (string.IsNullOrEmpty(uid)) return Unauthorized();

            var profileResult = await _profileService.GetProfileAsync(uid);
            if (!profileResult.Success || profileResult.Data == null)
                return BadRequest(profileResult);

            var eloInfo = new
            {
                profileResult.Data.EloPoints,
                profileResult.Data.Level,
                profileResult.Data.PointsInCurrentLevel,
                profileResult.Data.LevelProgress,
                profileResult.Data.PointsToNextLevel
            };

            return Ok(ApiResponse<object>.Ok(eloInfo));
        }

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
                hasRole = claims.Any(c => c.Type == ClaimTypes.Role),
                role = User.FindFirst(ClaimTypes.Role)?.Value
            });
        }

        [HttpGet("rating-history")]
        public async Task<IActionResult> GetRatingHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var uid = GetCurrentUid();
            if (string.IsNullOrEmpty(uid)) return Unauthorized();

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var result = await _ratingHistoryService.GetHistoryAsync(uid, page, pageSize);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpGet("leaderboard")]
        public async Task<IActionResult> GetLeaderboard()
        {
            var uid = GetCurrentUid();
            if (string.IsNullOrEmpty(uid)) return Unauthorized();

            var result = await _leaderboardService.GetLeaderboardAsync(uid);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}

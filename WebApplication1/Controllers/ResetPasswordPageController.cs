using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using WebApplication1.Data;
using WebApplication1.Models.Entities;
using WebApplication1.Models.Requests;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [Route("account")]
    public class ResetPasswordPageController : Controller
    {
        private readonly AppDbContext _context;
        private readonly AuthService _authService;
        private readonly ILogger<ResetPasswordPageController> _logger;
        private readonly IConfiguration _configuration;

        public ResetPasswordPageController(
            AppDbContext context,
            AuthService authService,
            ILogger<ResetPasswordPageController> logger,
            IConfiguration configuration)
        {
            _context = context;
            _authService = authService;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPasswordPage([FromQuery] string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    return Redirect($"/static/reset-invalid.html?message={Uri.EscapeDataString("Токен не предоставлен")}");
                }

                var resetToken = await _context.PasswordResetTokens
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Token == token &&
                                             t.Used == false &&
                                             t.ExpiresAt > DateTime.UtcNow);

                if (resetToken == null)
                {
                    _logger.LogWarning($"Невалидный токен сброса пароля: {token}");
                    return Redirect($"/static/reset-invalid.html?message={Uri.EscapeDataString("Токен недействителен, просрочен или уже использован")}");
                }

                var frontendUrl = _configuration["EmailSettings:FrontendUrl"] ??
                                 $"{Request.Scheme}://{Request.Host}";

                return Redirect($"{frontendUrl}/static/reset-password.html?token={Uri.EscapeDataString(token)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при проверке токена: {token}");
                return Redirect($"/static/reset-invalid.html?message={Uri.EscapeDataString("Произошла ошибка при обработке запроса")}");
            }
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPasswordPage(
            [FromForm] string token,
            [FromForm, MinLength(6)] string newPassword,
            [FromForm] string confirmPassword)
        {
            try
            {
                if (newPassword != confirmPassword)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Пароли не совпадают",
                        redirectUrl = $"/static/reset-password.html?token={Uri.EscapeDataString(token)}&error=Пароли не совпадают"
                    });
                }

                var resetToken = await _context.PasswordResetTokens
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Token == token &&
                                             t.Used == false &&
                                             t.ExpiresAt > DateTime.UtcNow);

                if (resetToken == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Токен недействителен, просрочен или уже использован",
                        redirectUrl = $"/static/reset-invalid.html?message={Uri.EscapeDataString("Токен недействителен, просрочен или уже использован")}"
                    });
                }

                var request = new ResetPasswordRequest
                {
                    Token = token,
                    NewPassword = newPassword,
                    ConfirmPassword = confirmPassword
                };

                var result = await _authService.ResetPasswordAsync(request);

                if (result.Success)
                {
                    _logger.LogInformation($"Пароль успешно сброшен по токену: {token}");

                    resetToken.Used = true;
                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        success = true,
                        message = "Пароль успешно изменен",
                        redirectUrl = "/static/reset-success.html"
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = result.Error,
                        redirectUrl = $"/static/reset-password.html?token={Uri.EscapeDataString(token)}&error={Uri.EscapeDataString(result.Error)}"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка сброса пароля для токена: {token}");
                return BadRequest(new
                {
                    success = false,
                    error = "Произошла ошибка при сбросе пароля",
                    redirectUrl = $"/static/reset-password.html?token={Uri.EscapeDataString(token)}&error={Uri.EscapeDataString("Произошла ошибка при сбросе пароля")}"
                });
            }
        }

        [HttpGet("reset-success")]
        [AllowAnonymous]
        public IActionResult ResetSuccess()
        {
            return Redirect("/static/reset-success.html");
        }
    }
}
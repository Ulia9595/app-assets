using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models.Entities;
using WebApplication1.Models.Requests;
using WebApplication1.Models.Responses;
using WebApplication1.Utils;

namespace WebApplication1.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly JwtService _jwtService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            AppDbContext context,
            IEmailService emailService,
            IConfiguration configuration,
            JwtService jwtService,
            ILogger<AuthService> logger)
        {
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<ApiResponse<string>> SendOtpAsync(SendOtpRequest request)
        {
            try
            {
                if (request.Purpose == "registration")
                {
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == request.Email);

                    if (existingUser != null)
                        return ApiResponse<string>.Fail("Пользователь с таким email уже существует");
                }

                var otpCode = new Random().Next(100000, 999999).ToString();

                var oldOtps = await _context.OtpCodes
                    .Where(o => o.Email == request.Email && o.Purpose == request.Purpose)
                    .ToListAsync();

                if (oldOtps.Any())
                {
                    _context.OtpCodes.RemoveRange(oldOtps);
                }

                var otp = new OtpCode
                {
                    Email = request.Email,
                    Code = otpCode,
                    Purpose = request.Purpose,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15)
                };

                _context.OtpCodes.Add(otp);
                await _context.SaveChangesAsync();

                var isSent = await _emailService.SendOtpEmail(request.Email, otpCode, request.Purpose);

                if (!isSent)
                    return ApiResponse<string>.Fail("Ошибка отправки кода на email");

                return ApiResponse<string>.Ok("Код отправлен на email");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки OTP для {Email}", request.Email);
                return ApiResponse<string>.Fail($"Ошибка: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> VerifyOtpAsync(VerifyOtpRequest request)
        {
            try
            {
                var otp = await _context.OtpCodes
                    .Where(o => o.Email == request.Email &&
                               o.Code == request.Code &&
                               o.Used == false &&
                               o.ExpiresAt > DateTime.UtcNow)
                    .FirstOrDefaultAsync();

                if (otp == null)
                    return ApiResponse<bool>.Fail("Неверный или просроченный код");

                otp.Used = true;
                await _context.SaveChangesAsync();

                return ApiResponse<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка верификации OTP для {Email}", request.Email);
                return ApiResponse<bool>.Fail($"Ошибка: {ex.Message}");
            }
        }

        public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
        {
            try
            {
                _logger.LogInformation($"RegisterAsync вызван: Email={request.Email}, Name='{request.Name}', AvatarUrl='{request.AvatarUrl}'");

                if (request.Password != request.PasswordRepeat)
                    return ApiResponse<AuthResponse>.Fail("Пароли не совпадают");

                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (existingUser != null)
                    return ApiResponse<AuthResponse>.Fail("Пользователь с таким email уже существует");

                var user = new User
                {
                    Uid = Guid.NewGuid().ToString(),
                    Email = request.Email,
                    PasswordHash = PasswordHelper.HashPassword(request.Password),
                    Name = request.Name,
                    AvatarUrl = request.AvatarUrl,
                    EloPoints = 500,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var token = _jwtService.GenerateToken(user.Id, user.Email, user.Uid);

                var response = new AuthResponse
                {
                    Token = token,
                    User = new UserResponse
                    {
                        Uid = user.Uid!,
                        Email = user.Email,
                        Name = user.Name,
                        AvatarUrl = user.AvatarUrl,
                        EloPoints = user.EloPoints,
                        IsEmailVerified = false
                    }
                };

                return ApiResponse<AuthResponse>.Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка регистрации для {Email}", request.Email);
                return ApiResponse<AuthResponse>.Fail($"Ошибка регистрации: {ex.Message}");
            }
        }

        public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (user == null || !PasswordHelper.VerifyPassword(request.Password, user.PasswordHash))
                    return ApiResponse<AuthResponse>.Fail("Неверный email или пароль");

                var token = _jwtService.GenerateToken(user.Id, user.Email, user.Uid);

                var response = new AuthResponse
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

                return ApiResponse<AuthResponse>.Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка авторизации для {Email}", request.Email);
                return ApiResponse<AuthResponse>.Fail($"Ошибка авторизации: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> ForgotPasswordAsync(ForgotPasswordRequest request, string? clientIp = null)
        {
            try
            {
                _logger.LogInformation($"Запрос сброса пароля для {request.Email} с IP: {clientIp}");

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (user == null)
                {
                    _logger.LogInformation($"Email {request.Email} не найден, возвращаем общий ответ");
                    return ApiResponse<string>.Ok("Если email существует, инструкция отправлена");
                }

                var maxAttempts = _configuration.GetValue<int>("AppSettings:MaxPasswordResetAttempts", 3);
                var blockMinutes = _configuration.GetValue<int>("AppSettings:PasswordResetBlockMinutes", 15);

                var recentAttempts = await _context.PasswordResetAttempts
                    .CountAsync(a => a.Email == request.Email &&
                                    a.CreatedAt > DateTime.UtcNow.AddHours(-1));

                if (recentAttempts >= maxAttempts)
                {
                    _logger.LogWarning($"Превышен лимит попыток для {request.Email}. Попыток: {recentAttempts}");
                    return ApiResponse<string>.Fail($"Превышен лимит попыток. Попробуйте через {blockMinutes} минут.");
                }

                var requireEmailConfirmation = _configuration.GetValue<bool>("SecuritySettings:RequireEmailConfirmation", false);
                if (requireEmailConfirmation && !user.IsEmailVerified)
                {
                    return ApiResponse<string>.Fail("Подтвердите email перед сбросом пароля");
                }

                var token = Guid.NewGuid().ToString();
                var resetToken = new PasswordResetToken
                {
                    UserId = user.Id,
                    Token = token,
                    ExpiresAt = DateTime.UtcNow.AddHours(1)
                };

                var attempt = new PasswordResetAttempt
                {
                    Email = request.Email,
                    IpAddress = clientIp,
                    CreatedAt = DateTime.UtcNow
                };

                _context.PasswordResetTokens.Add(resetToken);
                _context.PasswordResetAttempts.Add(attempt);
                await _context.SaveChangesAsync();

                var isSent = await _emailService.SendPasswordResetEmail(user.Email, token);

                if (!isSent)
                {
                    _logger.LogError($"Ошибка отправки email для {request.Email}");
                    return ApiResponse<string>.Fail("Ошибка отправки email");
                }

                _logger.LogInformation($"Письмо сброса пароля отправлено на {request.Email}");
                return ApiResponse<string>.Ok("Инструкция отправлена на email");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в ForgotPassword для {Email}", request.Email);
                return ApiResponse<string>.Fail($"Ошибка: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> ResetPasswordAsync(ResetPasswordRequest request)
        {
            try
            {
                if (request.NewPassword != request.ConfirmPassword)
                    return ApiResponse<bool>.Fail("Пароли не совпадают");

                var token = await _context.PasswordResetTokens
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Token == request.Token &&
                                             t.Used == false &&
                                             t.ExpiresAt > DateTime.UtcNow);

                if (token == null)
                    return ApiResponse<bool>.Fail("Неверный или просроченный токен");

                token.User.PasswordHash = PasswordHelper.HashPassword(request.NewPassword);
                token.User.UpdatedAt = DateTime.UtcNow;
                token.Used = true;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Пароль успешно сброшен для пользователя {token.User.Email}");
                return ApiResponse<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка сброса пароля с токеном {Token}", request.Token);
                return ApiResponse<bool>.Fail($"Ошибка: {ex.Message}");
            }
        }

        public async Task<ApiResponse<string>> SendEmailChangeOtpAsync(string currentEmail, string newEmail)
        {
            try
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == newEmail);

                if (existingUser != null)
                    return ApiResponse<string>.Fail("Новый email уже используется");

                var otpCode = new Random().Next(100000, 999999).ToString();

                var otp = new OtpCode
                {
                    Email = newEmail,
                    Code = otpCode,
                    Purpose = "email_change",
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15)
                };

                _context.OtpCodes.Add(otp);
                await _context.SaveChangesAsync();

                var isSent = await _emailService.SendOtpEmail(newEmail, otpCode, "email_change");

                if (!isSent)
                    return ApiResponse<string>.Fail("Ошибка отправки кода на email");

                return ApiResponse<string>.Ok("Код отправлен на новый email");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки OTP для смены email с {Current} на {New}",
                    currentEmail, newEmail);
                return ApiResponse<string>.Fail($"Ошибка: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> CheckUsernameAsync(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                    return ApiResponse<bool>.Fail("Имя пользователя не может быть пустым");

                if (username.Length < 2 || username.Length > 30)
                    return ApiResponse<bool>.Fail("Имя должно быть от 2 до 30 символов");

                if (!System.Text.RegularExpressions.Regex.IsMatch(username, @"[a-zA-Zа-яА-Я]"))
                    return ApiResponse<bool>.Fail("Имя должно содержать хотя бы одну букву");

                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Name != null && u.Name.ToLower() == username.ToLower());

                if (existingUser != null)
                    return ApiResponse<bool>.Ok(false);

                return ApiResponse<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка проверки имени пользователя: {ex.Message}");
                return ApiResponse<bool>.Fail($"Ошибка проверки имени: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> ChangeEmailAsync(int userId, string newEmail, string otpCode)
        {
            try
            {
                _logger.LogInformation($"Вызов для UserId={userId}, NewEmail={newEmail}, OTP={otpCode}");

                var otp = await _context.OtpCodes
                    .Where(o => o.Email == newEmail &&
                                o.Code == otpCode &&
                                o.Purpose == "email_change" &&
                                o.Used == false &&
                                o.ExpiresAt > DateTime.UtcNow)
                    .FirstOrDefaultAsync();

                if (otp == null)
                {
                    _logger.LogWarning($"OTP не найден или просрочен для {newEmail}");
                    return ApiResponse<bool>.Fail("Неверный или просроченный код");
                }


                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"Пользователь с ID {userId} не найден");
                    return ApiResponse<bool>.Fail("Пользователь не найден");
                }

                _logger.LogInformation($"Старый email: {user.Email}, Новый email: {newEmail}");

                user.Email = newEmail;
                user.IsEmailVerified = true;
                user.UpdatedAt = DateTime.UtcNow;

                otp.Used = true;

                _logger.LogInformation($"Сохраняем изменения в БД...");
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Email успешно изменен для пользователя ID {userId}");

                return ApiResponse<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка смены email для пользователя {UserId}", userId);
                return ApiResponse<bool>.Fail($"Ошибка: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> UpdateEloPointsAsync(int userId, int points)
        {
            try
            {
                _logger.LogInformation($"Вызов для UserId={userId}, NewPoints={points}");

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"Пользователь с ID {userId} не найден");
                    return ApiResponse<bool>.Fail("Пользователь не найден");
                }

                var oldPoints = user.EloPoints;
                _logger.LogInformation($"Старый рейтинг: {oldPoints}, Новый рейтинг: {points}");

                user.EloPoints = points;
                user.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation($"Сохраняем изменения в БД...");
                await _context.SaveChangesAsync();
                _logger.LogInformation($"ELO рейтинг обновлен для пользователя {userId}: {oldPoints} -> {points}");

                return ApiResponse<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обновления ELO для пользователя {UserId}", userId);
                return ApiResponse<bool>.Fail($"Ошибка обновления рейтинга: {ex.Message}");
            }
        }

    }
}
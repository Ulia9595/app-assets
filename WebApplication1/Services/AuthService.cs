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
        private readonly PasswordValidator _passwordValidator;
        private readonly UsernameService _usernameService;

        public AuthService(
            AppDbContext context,
            IEmailService emailService,
            IConfiguration configuration,
            JwtService jwtService,
            ILogger<AuthService> logger,
            PasswordValidator passwordValidator,
            UsernameService usernameService)
        {
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
            _jwtService = jwtService;
            _logger = logger;
            _passwordValidator = passwordValidator;
            _usernameService = usernameService;
        }

        private async Task<OtpPurpose?> GetOtpPurposeAsync(string purposeName)
        {
            return await _context.OtpPurposes
                .FirstOrDefaultAsync(p => p.Name == purposeName);
        }

        public async Task<ApiResponse<string>> SendOtpAsync(SendOtpRequest request)
        {
            try
            {
                var purpose = await GetOtpPurposeAsync(request.Purpose);

                if (purpose == null)
                    return ApiResponse<string>.Fail("Некорректное назначение OTP-кода");

                if (request.Purpose == "registration")
                {
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == request.Email);

                    if (existingUser != null)
                        return ApiResponse<string>.Fail("Пользователь с таким email уже существует");
                }

                var otpCode = new Random().Next(100000, 999999).ToString();

                var oldOtps = await _context.OtpCodes
                    .Where(o => o.Email == request.Email && o.PurposeId == purpose.Id)
                    .ToListAsync();

                if (oldOtps.Any())
                {
                    _context.OtpCodes.RemoveRange(oldOtps);
                }

                var otp = new OtpCode
                {
                    Email = request.Email,
                    Code = otpCode,
                    PurposeId = purpose.Id,
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
                _logger.LogInformation(
                    $"RegisterAsync вызван: Email={request.Email}, Name='{request.Name}', AvatarId='{request.AvatarId}'"
                );

                if (request.Password != request.PasswordRepeat)
                    return ApiResponse<AuthResponse>.Fail("Пароли не совпадают");

                var usernameCheck = await _usernameService.ValidateAndCheckUsernameAsync(request.Name);

                if (!usernameCheck.Success)
                    return ApiResponse<AuthResponse>.Fail(usernameCheck.Error ?? "Некорректное имя пользователя");

                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (existingUser != null)
                    return ApiResponse<AuthResponse>.Fail("Пользователь с таким email уже существует");

                var playerRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Code == "player");

                if (playerRole == null)
                    return ApiResponse<AuthResponse>.Fail("Роль игрока не найдена");

                AvailableAvatar? avatar = null;

                if (request.AvatarId.HasValue)
                {
                    avatar = await _context.AvailableAvatars
                        .FirstOrDefaultAsync(a => a.Id == request.AvatarId.Value);

                    if (avatar == null)
                        return ApiResponse<AuthResponse>.Fail("Выбранный аватар не найден");
                }

                var user = new User
                {
                    Uid = Guid.NewGuid().ToString(),
                    Email = request.Email,
                    PasswordHash = PasswordHelper.HashPassword(request.Password),
                    RoleId = playerRole.Id,
                    Name = request.Name.Trim(),
                    AvatarId = avatar?.Id,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var userRating = await _context.UserRatings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.UserId == user.Id);

                if (userRating == null)
                    return ApiResponse<AuthResponse>.Fail("Ошибка создания рейтинга пользователя");

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendWelcomeEmail(user.Email, user.Name ?? "Игрок");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ошибка фоновой отправки welcome email для {Email}", user.Email);
                    }
                });

                var token = _jwtService.GenerateToken(user.Id, user.Email, user.Uid!, playerRole.Code);

                var response = new AuthResponse
                {
                    Token = token,
                    User = new UserResponse
                    {
                        Uid = user.Uid!,
                        Email = user.Email,
                        Role = playerRole.Code,
                        Name = user.Name,
                        AvatarId = user.AvatarId,
                        AvatarUrl = avatar?.Url,
                        EloPoints = userRating.CurrentRating
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
                    .Include(u => u.Role)
                    .Include(u => u.Avatar)
                    .Include(u => u.Rating)
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (user == null || !PasswordHelper.VerifyPassword(request.Password, user.PasswordHash))
                    return ApiResponse<AuthResponse>.Fail("Неверный email или пароль");

                var rating = user.Rating;

                if (rating == null)
                {
                    rating = new UserRating
                    {
                        UserId = user.Id,
                        CurrentRating = 500,
                        LastUpdated = DateTime.UtcNow
                    };

                    _context.UserRatings.Add(rating);
                    await _context.SaveChangesAsync();
                }

                var token = _jwtService.GenerateToken(user.Id, user.Email, user.Uid!, user.Role.Code);

                var response = new AuthResponse
                {
                    Token = token,
                    User = new UserResponse
                    {
                        Uid = user.Uid!,
                        Email = user.Email,
                        Role = user.Role.Code,
                        Name = user.Name,
                        AvatarId = user.AvatarId,
                        AvatarUrl = user.Avatar?.Url,
                        EloPoints = rating.CurrentRating
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

                var passwordValidation = _passwordValidator.GetDetailedValidation(request.NewPassword);

                if (!passwordValidation.IsValid)
                {
                    var missingRequirements = passwordValidation.Requirements
                        .Where(r => !r.Value)
                        .Select(r => r.Key);

                    return ApiResponse<bool>.Fail(
                        "Пароль не соответствует требованиям: " + string.Join(", ", missingRequirements)
                    );
                }

                var token = await _context.PasswordResetTokens
                    .Include(t => t.User)
                    .ThenInclude(u => u.Role)
                    .FirstOrDefaultAsync(t => t.Token == request.Token &&
                                             t.Used == false &&
                                             t.ExpiresAt > DateTime.UtcNow);

                if (token == null)
                    return ApiResponse<bool>.Fail("Неверный или просроченный токен");

                if (token.User.Role.Code == "admin")
                    return ApiResponse<bool>.Fail("Администратор не может менять пароль");

                token.User.PasswordHash = PasswordHelper.HashPassword(request.NewPassword);
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

                var purpose = await GetOtpPurposeAsync("email_change");

                if (purpose == null)
                    return ApiResponse<string>.Fail("Назначение OTP-кода для смены email не найдено");

                var otpCode = new Random().Next(100000, 999999).ToString();

                var otp = new OtpCode
                {
                    Email = newEmail,
                    Code = otpCode,
                    PurposeId = purpose.Id,
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
            return await _usernameService.ValidateAndCheckUsernameAsync(username);
        }

        public async Task<ApiResponse<bool>> ChangeEmailAsync(int userId, string newEmail, string otpCode)
        {
            try
            {
                _logger.LogInformation($"Вызов для UserId={userId}, NewEmail={newEmail}, OTP={otpCode}");

                var purpose = await GetOtpPurposeAsync("email_change");

                if (purpose == null)
                    return ApiResponse<bool>.Fail("Назначение OTP-кода для смены email не найдено");

                var otp = await _context.OtpCodes
                    .Where(o => o.Email == newEmail &&
                                o.Code == otpCode &&
                                o.PurposeId == purpose.Id &&
                                o.Used == false &&
                                o.ExpiresAt > DateTime.UtcNow)
                    .FirstOrDefaultAsync();

                if (otp == null)
                {
                    _logger.LogWarning($"OTP не найден или просрочен для {newEmail}");
                    return ApiResponse<bool>.Fail("Неверный или просроченный код");
                }

                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    _logger.LogWarning($"Пользователь с ID {userId} не найден");
                    return ApiResponse<bool>.Fail("Пользователь не найден");
                }

                if (user.Role.Code == "admin")
                    return ApiResponse<bool>.Fail("Администратор не может менять email");

                user.Email = newEmail;
                otp.Used = true;

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

                var rating = await _context.UserRatings
                    .FirstOrDefaultAsync(r => r.UserId == userId);

                if (rating == null)
                {
                    var userExists = await _context.Users.AnyAsync(u => u.Id == userId);

                    if (!userExists)
                    {
                        _logger.LogWarning($"Пользователь с ID {userId} не найден");
                        return ApiResponse<bool>.Fail("Пользователь не найден");
                    }

                    rating = new UserRating
                    {
                        UserId = userId,
                        CurrentRating = 500,
                        LastUpdated = DateTime.UtcNow
                    };

                    _context.UserRatings.Add(rating);
                }

                var oldPoints = rating.CurrentRating;

                rating.CurrentRating = points;
                rating.LastUpdated = DateTime.UtcNow;

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
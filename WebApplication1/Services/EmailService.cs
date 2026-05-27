using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace WebApplication1.Services
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = "smtp.yandex.ru";
        public int SmtpPort { get; set; } = 587;
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderPassword { get; set; } = string.Empty;
        public string SenderName { get; set; } = "Game Auth System";
        public bool EnableSsl { get; set; } = true;
        public bool TestMode { get; set; } = false;
        public string FrontendUrl { get; set; } = "http://localhost:5141";
    }

    public interface IEmailService
    {
        Task<bool> SendOtpEmail(string toEmail, string otpCode, string purpose);
        Task<bool> SendPasswordResetEmail(string toEmail, string resetToken);
        Task<bool> SendWelcomeEmail(string toEmail, string userName);
    }

    public class SmartEmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<SmartEmailService> _logger;
        private readonly IConfiguration _configuration;

        public SmartEmailService(
            IOptions<EmailSettings> settings,
            ILogger<SmartEmailService> logger,
            IConfiguration configuration)
        {
            _settings = settings.Value;
            _logger = logger;
            _configuration = configuration;

            var frontendUrl = _configuration["EmailSettings:FrontendUrl"] ?? _settings.FrontendUrl;
            _settings.FrontendUrl = frontendUrl;

            _logger.LogInformation($"EmailService инициализирован. Отправитель: {_settings.SenderEmail}");
            _logger.LogInformation($"Frontend URL: {_settings.FrontendUrl}");

            if (_settings.TestMode)
            {
                _logger.LogWarning("EMAIL ТЕСТОВЫЙ РЕЖИМ ВКЛЮЧЕН! Письма не отправляются реально.");
            }
        }

        public async Task<bool> SendOtpEmail(string toEmail, string otpCode, string purpose)
        {
            try
            {
                _logger.LogInformation($"Отправка OTP ({purpose}) на {toEmail}");

                if (_settings.TestMode)
                {
                    _logger.LogInformation($"TEST MODE: OTP для {toEmail}: {otpCode}");
                    Console.WriteLine($"TEST: OTP для {toEmail}: {otpCode}");
                    return true;
                }

                return await SendViaSmtp(toEmail, otpCode, purpose);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки OTP на {Email}", toEmail);
                return false;
            }
        }

        private async Task<bool> SendViaSmtp(string toEmail, string otpCode, string purpose)
        {
            try
            {
                _logger.LogInformation($"Подключение к SMTP: {_settings.SmtpServer}:{_settings.SmtpPort}");

                using var message = new MailMessage();
                message.From = new MailAddress(_settings.SenderEmail, _settings.SenderName);
                message.To.Add(toEmail);

                var (subject, body) = GetEmailContent(toEmail, otpCode, purpose);
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = true;

                using var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
                {
                    EnableSsl = _settings.EnableSsl,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_settings.SenderEmail, _settings.SenderPassword),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 10000
                };

                await client.SendMailAsync(message);

                _logger.LogInformation($"Email успешно отправлен на {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка отправки на {toEmail}: {ex.Message}");
                return false;
            }
        }

        private (string subject, string body) GetEmailContent(string toEmail, string otpCode, string purpose)
        {
            var subject = purpose switch
            {
                "registration" => "Код подтверждения регистрации",
                "email_change" => "Код подтверждения смены email",
                "password_reset" => "Код для сброса пароля",
                _ => "Код подтверждения"
            };

            var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='UTF-8'>
                    <style>
                        body {{ font-family: Arial, sans-serif; color: #333; line-height: 1.6; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .code {{ font-size: 32px; font-weight: bold; color: #4CAF50; 
                                 padding: 15px; background: #f9f9f9; border-radius: 8px; 
                                 display: inline-block; margin: 20px 0; text-align: center; 
                                 letter-spacing: 5px; }}
                        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; 
                                  color: #777; font-size: 12px; }}
                        .warning {{ background: #fff3cd; color: #856404; padding: 10px; 
                                    border-radius: 5px; margin: 15px 0; }}
                        .token-box {{ background: #f8f9fa; padding: 15px; border-radius: 5px; 
                                     border-left: 4px solid #4CAF50; margin: 15px 0; 
                                     font-family: monospace; word-break: break-all; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <h3>Здравствуйте!</h3>
                        <p>Ваш проверочный код:</p>
                        <div class='code'>{otpCode}</div>
                        <p>Введите этот код в приложении для завершения операции.</p>
                        <div class='warning'>
                            <p><strong>Код действителен в течение 15 минут.</strong></p>
                        </div>
                        <div class='footer'>
                            <p>Если вы не запрашивали этот код, проигнорируйте это письмо.</p>
                            <p>С уважением,<br><strong>{_settings.SenderName}</strong></p>
                        </div>
                    </div>
                </body>
                </html>";

            return (subject, body);
        }

        public async Task<bool> SendPasswordResetEmail(string toEmail, string resetToken)
        {
            try
            {
                _logger.LogInformation($"Отправка reset token на {toEmail}");

                var resetLink = GenerateResetPasswordLink(resetToken);

                if (_settings.TestMode)
                {
                    _logger.LogInformation($"TEST MODE: Reset token для {toEmail}: {resetToken}");
                    Console.WriteLine($"TEST: Reset token для {toEmail}: {resetToken}");
                    Console.WriteLine($"TEST: Reset link: {resetLink}");

                    return true;
                }

                var subject = "Сброс пароля";
                var body = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset='UTF-8'>
                        <style>
                            body {{ font-family: Arial, sans-serif; color: #333; line-height: 1.6; }}
                            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                            .button {{ display: inline-block; padding: 12px 24px; background: #4CAF50; 
                                      color: white; text-decoration: none; border-radius: 5px; 
                                      margin: 10px 0; }}
                            .token-box {{ background: #f8f9fa; padding: 15px; border-radius: 5px; 
                                          margin: 15px 0; 
                                         font-family: monospace; word-break: break-all; }}
                            .warning {{ background: #fff3cd; color: #856404; padding: 10px; 
                                        border-radius: 5px; margin: 15px 0; }}
                            .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; 
                                      color: #777; font-size: 12px; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <h3>Сброс пароля</h3>
                            <p>Для сброса пароля перейдите по ссылке:</p>
                            <p><a href='{resetLink}' class='button'>Сбросить пароль</a></p>
                            
                            <p><strong>Или скопируйте ссылку:</strong></p>
                            <div class='token-box'>{resetLink}</div>
                            
                            <p><strong>Или используйте токен в приложении:</strong></p>
                            <div class='token-box'>{resetToken}</div>
                            
                            <div class='warning'>
                                <p><strong>Внимание!</strong></p>
                                <p>• Ссылка действительна <strong>1 час</strong></p>
                                <p>• Никому не передавайте эту ссылку или токен</p>
                                <p>• Если вы не запрашивали сброс пароля, проигнорируйте это письмо</p>
                            </div>
                            
                            <div class='footer'>
                                <p>С уважением,<br><strong>{_settings.SenderName}</strong></p>
                            </div>
                        </div>
                    </body>
                    </html>";

                using var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
                {
                    EnableSsl = _settings.EnableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_settings.SenderEmail, _settings.SenderPassword),
                    Timeout = 10000
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                message.To.Add(toEmail);
                await client.SendMailAsync(message);

                _logger.LogInformation($"Reset email отправлен на {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки email сброса пароля на {Email}", toEmail);
                return false;
            }
        }

        private string GenerateResetPasswordLink(string token)
        {
            var frontendUrl = _settings.FrontendUrl ?? "http://localhost:5141";
            return $"{frontendUrl}/account/reset-password?token={Uri.EscapeDataString(token)}";
        }

        public async Task<bool> SendWelcomeEmail(string toEmail, string userName)
        {
            try
            {
                _logger.LogInformation($"Отправка welcome email на {toEmail}");

                if (_settings.TestMode)
                {
                    _logger.LogInformation($"TEST MODE: Welcome email для {toEmail}");
                    return true;
                }

                var subject = "Добро пожаловать!";
                var body = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset='UTF-8'>
                        <style>
                            body {{ font-family: Arial, sans-serif; color: #333; line-height: 1.6; }}
                            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <h3>Здравствуйте, {userName}!</h3>
                            <p>Добро пожаловать в наше игровое приложение!</p>
                            <p>Мы рады, что вы присоединились к нашему сообществу.</p>
                            <p>Желаем удачи в играх и высоких рейтингов!</p>
                            <br>
                            <p>С уважением,<br><strong>{_settings.SenderName}</strong></p>
                        </div>
                    </body>
                    </html>";

                using var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
                {
                    EnableSsl = _settings.EnableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_settings.SenderEmail, _settings.SenderPassword),
                    Timeout = 10000
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                message.To.Add(toEmail);
                await client.SendMailAsync(message);

                _logger.LogInformation($"Welcome email отправлен на {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки welcome email на {Email}", toEmail);
                return false;
            }
        }
    }
}
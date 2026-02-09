using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace WebApplication1.Models.Requests
{
    public class PasswordValidatorAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var password = value as string;

            if (string.IsNullOrEmpty(password))
                return new ValidationResult("Пароль обязателен");

            if (password.Length < 8)
                return new ValidationResult("Пароль должен содержать минимум 8 символов");

            if (!Regex.IsMatch(password, "[A-ZА-Я]"))
                return new ValidationResult("Пароль должен содержать хотя бы одну заглавную букву");

            if (!Regex.IsMatch(password, "[a-zа-я]"))
                return new ValidationResult("Пароль должен содержать хотя бы одну строчную букву");

            if (!Regex.IsMatch(password, @"\d"))
                return new ValidationResult("Пароль должен содержать хотя бы одну цифру");

            if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]"))
                return new ValidationResult("Пароль должен содержать хотя бы один специальный символ (!@#$%^&* и т.д.)");

            return ValidationResult.Success;
        }
    }

    public class RegisterRequest
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Неверный формат email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль обязателен")]
        [PasswordValidator]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Повтор пароля обязателен")]
        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        public string PasswordRepeat { get; set; } = string.Empty;

        [Required(ErrorMessage = "Имя обязательно")]
        [MinLength(2, ErrorMessage = "Имя должно содержать минимум 2 символа")]
        [MaxLength(50, ErrorMessage = "Имя не должно превышать 50 символов")]
        public string Name { get; set; } = string.Empty;

        [Url(ErrorMessage = "Неверный формат URL для аватара")]
        public string? AvatarUrl { get; set; }
    }

    public class LoginRequest
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Неверный формат email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль обязателен")]
        public string Password { get; set; } = string.Empty;
    }

    public class SendOtpRequest
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Неверный формат email")]
        public string Email { get; set; } = string.Empty;

        public string Purpose { get; set; } = "registration";
    }

    public class VerifyOtpRequest
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Неверный формат email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Код обязателен")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Код должен содержать 6 символов")]
        public string Code { get; set; } = string.Empty;
    }

    public class ForgotPasswordRequest
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Неверный формат email")]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        [Required(ErrorMessage = "Токен обязателен")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Новый пароль обязателен")]
        [PasswordValidator]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Подтверждение пароля обязательно")]
        [Compare("NewPassword", ErrorMessage = "Пароли не совпадают")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ChangeEmailRequest
    {
        [Required(ErrorMessage = "Новый Email обязателен")]
        [EmailAddress(ErrorMessage = "Неверный формат email")]
        public string NewEmail { get; set; } = string.Empty;
    }

    public class UpdateProfileRequest
    {
        [MinLength(2, ErrorMessage = "Имя должно содержать минимум 2 символа")]
        [MaxLength(50, ErrorMessage = "Имя не должно превышать 50 символов")]
        public string? Name { get; set; }

        [Url(ErrorMessage = "Неверный формат URL для аватара")]
        public string? AvatarUrl { get; set; }
    }

    public class CompleteRegistrationRequest
    {
        [Required(ErrorMessage = "Имя обязательно")]
        [MinLength(2, ErrorMessage = "Имя должно содержать минимум 2 символа")]
        [MaxLength(50, ErrorMessage = "Имя не должно превышать 50 символов")]
        public string Name { get; set; } = string.Empty;

        [Url(ErrorMessage = "Неверный формат URL для аватара")]
        public string? AvatarUrl { get; set; }
    }

    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "Текущий пароль обязателен")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Новый пароль обязателен")]
        [PasswordValidator]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Подтверждение пароля обязательно")]
        [Compare("NewPassword", ErrorMessage = "Пароли не совпадают")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
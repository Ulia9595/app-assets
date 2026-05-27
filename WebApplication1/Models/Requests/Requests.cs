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

            return ValidationResult.Success!;
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
        [MaxLength(30, ErrorMessage = "Имя не должно превышать 30 символов")]
        public string Name { get; set; } = string.Empty;

        public int? AvatarId { get; set; }
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

        [Required(ErrorMessage = "Назначение OTP-кода обязательно")]
        [RegularExpression(
            "^(registration|email_change|password_reset)$",
            ErrorMessage = "Некорректное назначение OTP-кода"
        )]
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
        [MaxLength(30, ErrorMessage = "Имя не должно превышать 30 символов")]
        public string? Name { get; set; }

        public int? AvatarId { get; set; }
    }

    public class CompleteRegistrationRequest
    {
        [Required(ErrorMessage = "Имя обязательно")]
        [MinLength(2, ErrorMessage = "Имя должно содержать минимум 2 символа")]
        [MaxLength(30, ErrorMessage = "Имя не должно превышать 30 символов")]
        public string Name { get; set; } = string.Empty;

        public int? AvatarId { get; set; }
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

    public class CreateTopicRequest
    {
        [Required(ErrorMessage = "Название темы обязательно")]
        [MaxLength(60, ErrorMessage = "Название не должно превышать 60 символов")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Порядок отображения должен быть неотрицательным")]
        public int DisplayOrder { get; set; }
    }

    public class UpdateTopicRequest
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Название темы обязательно")]
        [MaxLength(60, ErrorMessage = "Название не должно превышать 60 символов")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Порядок отображения должен быть неотрицательным")]
        public int DisplayOrder { get; set; }
    }

    public class CreateLevelRequest
    {
        [Required(ErrorMessage = "Тема обязательна")]
        public int TopicId { get; set; }

        [Required(ErrorMessage = "Название уровня обязательно")]
        [MaxLength(60, ErrorMessage = "Название не должно превышать 60 символов")]
        public string Name { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Номер уровня должен быть больше нуля")]
        public int LevelNumber { get; set; }
    }

    public class UpdateLevelRequest
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Тема обязательна")]
        public int TopicId { get; set; }

        [Required(ErrorMessage = "Название уровня обязательно")]
        [MaxLength(60, ErrorMessage = "Название не должно превышать 60 символов")]
        public string Name { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Номер уровня должен быть больше нуля")]
        public int LevelNumber { get; set; }
    }

    public class CreateTheoryRequest
    {
        [Required(ErrorMessage = "Уровень обязателен")]
        public int LevelId { get; set; }

        [Required(ErrorMessage = "Заголовок теории обязателен")]
        [MaxLength(50, ErrorMessage = "Заголовок не должен превышать 50 символов")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Содержимое теории обязательно")]
        public string Content { get; set; } = string.Empty;
    }

    public class UpdateTheoryRequest
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Заголовок теории обязателен")]
        [MaxLength(50, ErrorMessage = "Заголовок не должен превышать 50 символов")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Содержимое теории обязательно")]
        public string Content { get; set; } = string.Empty;
    }

    public class CreatePracticeTaskRequest
    {
        [Required(ErrorMessage = "Уровень обязателен")]
        public int LevelId { get; set; }

        [Required(ErrorMessage = "Название задания обязательно")]
        [MaxLength(60, ErrorMessage = "Название не должно превышать 60 символов")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Условие задания обязательно")]
        public string Condition { get; set; } = string.Empty;

        [Required(ErrorMessage = "Тип сложности обязателен")]
        public int DifficultyTypeId { get; set; }

        [Required(ErrorMessage = "Тип проверки обязателен")]
        public int CheckTypeId { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Порядок отображения должен быть неотрицательным")]
        public int DisplayOrder { get; set; }
    }

    public class UpdatePracticeTaskRequest
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Уровень обязателен")]
        public int LevelId { get; set; }

        [Required(ErrorMessage = "Название задания обязательно")]
        [MaxLength(60, ErrorMessage = "Название не должно превышать 60 символов")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Условие задания обязательно")]
        public string Condition { get; set; } = string.Empty;

        [Required(ErrorMessage = "Тип сложности обязателен")]
        public int DifficultyTypeId { get; set; }

        [Required(ErrorMessage = "Тип проверки обязателен")]
        public int CheckTypeId { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Порядок отображения должен быть неотрицательным")]
        public int DisplayOrder { get; set; }
    }

    public class CreateTestCaseRequest
    {
        [Required(ErrorMessage = "Задание обязательно")]
        public int TaskId { get; set; }

        public string? InputData { get; set; }

        [Required(ErrorMessage = "Ожидаемый вывод обязателен")]
        public string ExpectedOutput { get; set; } = string.Empty;

        public bool IsHidden { get; set; } = false;
    }

    public class UpdateTestCaseRequest
    {
        [Required]
        public int Id { get; set; }

        public string? InputData { get; set; }

        [Required(ErrorMessage = "Ожидаемый вывод обязателен")]
        public string ExpectedOutput { get; set; } = string.Empty;

        public bool IsHidden { get; set; }
    }

    public class CreateHintRequest
    {
        [Required(ErrorMessage = "Задание обязательно")]
        public int TaskId { get; set; }

        [Required(ErrorMessage = "Текст подсказки обязателен")]
        public string HintText { get; set; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "Порядок отображения должен быть неотрицательным")]
        public int DisplayOrder { get; set; }
    }

    public class UpdateHintRequest
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Текст подсказки обязателен")]
        public string HintText { get; set; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "Порядок отображения должен быть неотрицательным")]
        public int DisplayOrder { get; set; }
    }

    public class CreateCustomAlgorithmRequest
    {
        [Required(ErrorMessage = "Задание обязательно")]
        public int TaskId { get; set; }

        [Required(ErrorMessage = "Код алгоритма обязателен")]
        public string AlgorithmCode { get; set; } = string.Empty;
    }

    public class UpdateCustomAlgorithmRequest
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Код алгоритма обязателен")]
        public string AlgorithmCode { get; set; } = string.Empty;
    }

    public class CreateTournamentRequest
    {
        [Required(ErrorMessage = "Тема обязательна")]
        public int TopicId { get; set; }

        [Required(ErrorMessage = "Статус обязателен")]
        public int StatusId { get; set; }

        [Range(0, 9999, ErrorMessage = "Минимальный рейтинг от 0 до 9999")]
        public int MinRating { get; set; } = 0;

        [Range(0, 9999, ErrorMessage = "Максимальный рейтинг от 0 до 9999")]
        public int MaxRating { get; set; } = 9999;
    }

    public class UpdateTournamentRequest
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Тема обязательна")]
        public int TopicId { get; set; }

        [Required(ErrorMessage = "Статус обязателен")]
        public int StatusId { get; set; }

        [Range(0, 9999, ErrorMessage = "Минимальный рейтинг от 0 до 9999")]
        public int MinRating { get; set; } = 0;

        [Range(0, 9999, ErrorMessage = "Максимальный рейтинг от 0 до 9999")]
        public int MaxRating { get; set; } = 9999;
    }

    public class AddTournamentQuestionRequest
    {
        [Required(ErrorMessage = "Турнир обязателен")]
        public int TournamentId { get; set; }

        [Required(ErrorMessage = "Вопрос обязателен")]
        public int QuestionId { get; set; }
    }

    public class RemoveTournamentQuestionRequest
    {
        [Required]
        public int TournamentId { get; set; }

        [Required]
        public int QuestionId { get; set; }
    }

    public class CreateQuestionRequest
    {
        [Required(ErrorMessage = "Тема обязательна")]
        public int TopicId { get; set; }

        [Required(ErrorMessage = "Тип сложности обязателен")]
        public int DifficultyTypeId { get; set; }

        [Required(ErrorMessage = "Текст вопроса обязателен")]
        [MaxLength(1000, ErrorMessage = "Текст вопроса не должен превышать 1000 символов")]
        public string QuestionText { get; set; } = string.Empty;
    }

    public class UpdateQuestionRequest
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Тема обязательна")]
        public int TopicId { get; set; }

        [Required(ErrorMessage = "Тип сложности обязателен")]
        public int DifficultyTypeId { get; set; }

        [Required(ErrorMessage = "Текст вопроса обязателен")]
        [MaxLength(1000, ErrorMessage = "Текст вопроса не должен превышать 1000 символов")]
        public string QuestionText { get; set; } = string.Empty;
    }

    public class CreateAnswerOptionRequest
    {
        [Required(ErrorMessage = "Вопрос обязателен")]
        public int QuestionId { get; set; }

        [Required(ErrorMessage = "Текст ответа обязателен")]
        [MaxLength(500, ErrorMessage = "Текст ответа не должен превышать 500 символов")]
        public string AnswerText { get; set; } = string.Empty;

        public bool IsCorrect { get; set; } = false;
    }

    public class UpdateAnswerOptionRequest
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Текст ответа обязателен")]
        [MaxLength(500, ErrorMessage = "Текст ответа не должен превышать 500 символов")]
        public string AnswerText { get; set; } = string.Empty;

        public bool IsCorrect { get; set; } = false;
    }

    public class RunCodeRequest
    {
        public string Code { get; set; } = string.Empty;
        public string? Stdin { get; set; }
    }
}
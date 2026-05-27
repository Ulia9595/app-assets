using System.Collections.Generic;
using System.Linq;

namespace WebApplication1.Services
{
    public enum PasswordStrength
    {
        Empty,
        Weak,
        Medium,
        Strong
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public Dictionary<string, bool> Requirements { get; set; }
        public PasswordStrength Strength { get; set; }
        public string StrengthText => Strength.ToString();
        public int MetCount => Requirements?.Values.Count(v => v) ?? 0;
    }

    public class PasswordValidator
    {
        private const string SpecialChars = "!@#$%^&*+=";

        public ValidationResult GetDetailedValidation(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Requirements = new Dictionary<string, bool>
                    {
                        ["Минимум 8 символов"] = false,
                        ["Заглавная буква"] = false,
                        ["Цифра"] = false,
                        ["Спецсимвол"] = false
                    },
                    Strength = PasswordStrength.Empty
                };
            }

            var requirements = new Dictionary<string, bool>
            {
                ["Минимум 8 символов"] = password.Length >= 8,
                ["Заглавная буква"] = password.Any(char.IsUpper),
                ["Цифра"] = password.Any(char.IsDigit),
                ["Спецсимвол"] = password.Any(c => SpecialChars.Contains(c))
            };

            var isValid = requirements.Values.All(v => v);
            var strength = CalculateStrength(password, requirements);

            return new ValidationResult
            {
                IsValid = isValid,
                Requirements = requirements,
                Strength = strength
            };
        }

        public PasswordStrength CalculateStrength(string password, Dictionary<string, bool> requirements = null)
        {
            if (string.IsNullOrEmpty(password))
                return PasswordStrength.Empty;

            requirements ??= GetDetailedValidation(password).Requirements;
            var metCount = requirements.Values.Count(v => v);

            if (metCount <= 1)
                return PasswordStrength.Weak;

            if (metCount == 2 || metCount == 3)
                return PasswordStrength.Medium;

            return PasswordStrength.Strong;
        }

        public bool IsValid(string password)
        {
            return GetDetailedValidation(password).IsValid;
        }

        public string GetStrengthTextRu(PasswordStrength strength)
        {
            return strength switch
            {
                PasswordStrength.Empty => "Пустой",
                PasswordStrength.Weak => "Слабый",
                PasswordStrength.Medium => "Средний",
                PasswordStrength.Strong => "Сильный",
                _ => "Неизвестно"
            };
        }

        public string GetStrengthColor(PasswordStrength strength)
        {
            return strength switch
            {
                PasswordStrength.Empty => "#6c757d",
                PasswordStrength.Weak => "#dc3545",
                PasswordStrength.Medium => "#ffc107",
                PasswordStrength.Strong => "#198754",
                _ => "#6c757d"
            };
        }

        public int GetStrengthPercentage(PasswordStrength strength)
        {
            return strength switch
            {
                PasswordStrength.Empty => 0,
                PasswordStrength.Weak => 33,
                PasswordStrength.Medium => 66,
                PasswordStrength.Strong => 100,
                _ => 0
            };
        }
    }
}
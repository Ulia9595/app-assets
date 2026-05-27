using Bogus;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using WebApplication1.Data;
using WebApplication1.Models.Responses;

namespace WebApplication1.Services
{
    public class UsernameService
    {
        private readonly AppDbContext _context;
        private readonly Faker _faker;
        private readonly Random _random = new();
        private readonly ProfanityService _profanityService;

        private const int MinLength = 2;
        private const int MaxLength = 30;

        private static readonly Regex UsernameRegex = new(
            @"^(?=(.*[A-Za-z]){2,})[A-Za-z0-9_]+( [A-Za-z0-9_]+)?$",
            RegexOptions.Compiled
        );

        private static readonly Regex InvalidCharsRegex = new(
            @"[^A-Za-z0-9_]",
            RegexOptions.Compiled
        );

        public UsernameService(
            AppDbContext context,
            ProfanityService profanityService)
        {
            _context = context;
            _profanityService = profanityService;
            _faker = new Faker("en");
        }

        public ApiResponse<bool> ValidateUsername(string? username)
        {
            var trimmed = username?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(trimmed))
                return ApiResponse<bool>.Fail("Введите имя");

            if (trimmed.Length < MinLength)
                return ApiResponse<bool>.Fail("Имя должно содержать минимум 2 символа");

            if (trimmed.Length > MaxLength)
                return ApiResponse<bool>.Fail("Имя должно быть не длиннее 30 символов");

            if (!UsernameRegex.IsMatch(trimmed))
                return ApiResponse<bool>.Fail("Имя должно содержать минимум 2 латинские буквы. Разрешены латинские буквы, цифры, _ и один пробел");

            var profanityError = _profanityService.GetErrorIfProfanity(trimmed);
            if (profanityError != null)
                return ApiResponse<bool>.Fail(profanityError);

            return ApiResponse<bool>.Ok(true);
        }

        public async Task<ApiResponse<bool>> IsUsernameUniqueAsync(
            string username,
            string? currentUserId = null)
        {
            var validation = ValidateUsername(username);
            if (!validation.Success)
                return validation;

            var normalizedName = username.Trim().ToLower();

            var exists = await _context.Users.AnyAsync(u =>
                u.Name != null &&
                u.Name.ToLower() == normalizedName &&
                (currentUserId == null || u.Uid != currentUserId)
            );

            return ApiResponse<bool>.Ok(!exists);
        }

        public async Task<ApiResponse<bool>> ValidateAndCheckUsernameAsync(
            string username,
            string? currentUserId = null)
        {
            var validation = ValidateUsername(username);
            if (!validation.Success)
                return validation;

            var uniqueResult = await IsUsernameUniqueAsync(username, currentUserId);
            if (!uniqueResult.Success)
                return uniqueResult;

            if (!uniqueResult.Data)
                return ApiResponse<bool>.Fail("Это имя уже занято");

            return ApiResponse<bool>.Ok(true);
        }

        public async Task<ApiResponse<List<string>>> GenerateSuggestionsAsync(int count = 3)
        {
            var suggestions = new List<string>();
            var attempts = 0;
            const int maxAttempts = 200;

            while (suggestions.Count < count && attempts < maxAttempts)
            {
                attempts++;

                var username = GenerateUsernameCandidate();

                var validation = ValidateUsername(username);
                if (!validation.Success)
                    continue;

                var uniqueResult = await IsUsernameUniqueAsync(username);
                if (!uniqueResult.Success || !uniqueResult.Data)
                    continue;

                if (suggestions.Any(x => x.Equals(username, StringComparison.OrdinalIgnoreCase)))
                    continue;

                suggestions.Add(username);
            }

            if (suggestions.Count == 0)
                return ApiResponse<List<string>>.Fail("Не удалось сгенерировать свободное имя");

            return ApiResponse<List<string>>.Ok(suggestions);
        }

        private string GenerateUsernameCandidate()
        {
            var first = GetCleanWord();
            var second = GetCleanWord();
            var number = _random.Next(1, 100);

            var username = _random.Next(2) == 0
                ? $"{first}{number} {second}"
                : $"{first} {second}{number}";

            username = NormalizeSpaces(username);
            username = CapitalizeWords(username);

            if (username.Length <= MaxLength)
                return username;

            return TrimToValidUsername(username);
        }

        private string GetCleanWord()
        {
            for (var i = 0; i < 20; i++)
            {
                var raw = _random.Next(4) switch
                {
                    0 => _faker.Hacker.Adjective(),
                    1 => _faker.Hacker.Noun(),
                    2 => _faker.Commerce.Color(),
                    _ => _faker.Hacker.Verb()
                };

                var clean = CleanWord(raw);

                if (clean.Length >= 2)
                    return clean;
            }

            return "Player";
        }

        private static string CleanWord(string input)
        {
            var withoutInvalidChars = InvalidCharsRegex.Replace(input, "");

            if (string.IsNullOrWhiteSpace(withoutInvalidChars))
                return "";

            return withoutInvalidChars;
        }

        private static string NormalizeSpaces(string input)
        {
            return string.Join(
                " ",
                input.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            );
        }

        private static string CapitalizeWords(string input)
        {
            return string.Join(" ", input.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(word =>
                {
                    if (string.IsNullOrEmpty(word))
                        return word;

                    return char.ToUpperInvariant(word[0]) +
                           word[1..].ToLowerInvariant();
                }));
        }

        private static string TrimToValidUsername(string username)
        {
            var parts = username.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 1)
                return parts[0].Length > MaxLength ? parts[0][..MaxLength] : parts[0];

            var first = parts[0];
            var second = parts[1];

            var availableForSecond = MaxLength - first.Length - 1;

            if (availableForSecond < 2)
            {
                first = first[..Math.Min(first.Length, MaxLength)];
                return first;
            }

            if (second.Length > availableForSecond)
                second = second[..availableForSecond];

            return $"{first} {second}".Trim();
        }
    }
}
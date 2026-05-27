using System.Text.RegularExpressions;

namespace WebApplication1.Services
{
    public class ProfanityService
    {
        private readonly ProfanityFilter.ProfanityFilter _englishFilter = new();

        private static readonly Regex NonLatinRegex = new(@"[^a-z]", RegexOptions.Compiled);

        private static readonly Dictionary<string, string> LeetMap = new()
        {
            ["0"] = "o",
            ["1"] = "i",
            ["3"] = "e",
            ["4"] = "a",
            ["5"] = "s",
            ["7"] = "t",
            ["8"] = "b",
            ["@"] = "a",
            ["$"] = "s"
        };

        private static readonly string[] RussianTranslitRoots =
        {
            "hui", "huy", "xui", "xuy", "xyi", "xyu", "xu",
            "pizd", "pizdec", "pisd", "pidor", "pidr",
            "blya", "blat", "suka", "sucka",
            "eban", "ebat", "ebal", "eblan", "yeban",
            "mudak", "mudil", "zalup", "govn",
            "zhopa", "jopa"
        };

        public bool ContainsProfanity(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            if (_englishFilter.ContainsProfanity(text))
                return true;

            var directNormalized = NormalizeLeetOnly(text);

            if (ContainsRussianTranslitProfanity(directNormalized))
                return true;

            var normalized = NormalizeTranslit(text);

            return ContainsRussianTranslitProfanity(normalized);
        }

        public string? GetErrorIfProfanity(string? text)
        {
            return ContainsProfanity(text)
                ? "Имя содержит недопустимые слова"
                : null;
        }

        private static bool ContainsRussianTranslitProfanity(string normalizedText)
        {
            return RussianTranslitRoots.Any(root =>
                normalizedText.Contains(root, StringComparison.OrdinalIgnoreCase)
            );
        }

        private static string NormalizeLeetOnly(string text)
        {
            var normalized = text.ToLowerInvariant();

            foreach (var pair in LeetMap)
            {
                normalized = normalized.Replace(pair.Key, pair.Value);
            }

            return NonLatinRegex.Replace(normalized, "");
        }

        private static string NormalizeTranslit(string text)
        {
            var normalized = NormalizeLeetOnly(text);

            normalized = normalized
                .Replace("sch", "s")
                .Replace("kh", "h")
                .Replace("ch", "c")
                .Replace("sh", "s")
                .Replace("ya", "a")
                .Replace("yo", "o")
                .Replace("yu", "u")
                .Replace("iy", "i")
                .Replace("ij", "i");

            return NonLatinRegex.Replace(normalized, "");
        }
    }
}
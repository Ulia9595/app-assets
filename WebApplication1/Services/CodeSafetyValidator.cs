namespace WebApplication1.Services
{
    public static class CodeSafetyValidator
    {
        private static readonly string[] DangerousPatterns =
        {
            "Runtime.getRuntime()",
            "ProcessBuilder",
            "Process.exec",
            "System.exit",

            "java.io.File",
            "FileInputStream",
            "FileOutputStream",
            "FileReader",
            "FileWriter",
            "Files.read",
            "Files.write",
            "Path.of",

            "java.net.Socket",
            "java.net.URL(",
            "HttpURLConnection",
            "URLConnection",

            "ClassLoader",
            "Class.forName",
            "getDeclaredMethod",
            "setAccessible",

            "Thread.sleep",
            "java.lang.Thread(",
            "Executors.",
            "ThreadPoolExecutor",

            "sun.misc.Unsafe",
            "System.getenv",
            "System.getProperty",
        };

        public static CodeValidationResult Validate(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return CodeValidationResult.Fail("Код не может быть пустым.");

            if (code.Length > 10_000)
                return CodeValidationResult.Fail("Код слишком длинный (максимум 10 000 символов).");

            foreach (var pattern in DangerousPatterns)
            {
                if (code.Contains(pattern, StringComparison.Ordinal))
                    return CodeValidationResult.Fail(
                        $"Использование `{pattern}` запрещено в заданиях.");
            }

            if (!code.Contains("fun main"))
                return CodeValidationResult.Fail(
                    "Код должен содержать функцию `fun main()`.");

            return CodeValidationResult.Ok();
        }
    }

    public class CodeValidationResult
    {
        public bool IsValid { get; private set; }
        public string? ErrorMessage { get; private set; }

        private CodeValidationResult() { }

        public static CodeValidationResult Ok() =>
            new() { IsValid = true };

        public static CodeValidationResult Fail(string message) =>
            new() { IsValid = false, ErrorMessage = message };
    }
}
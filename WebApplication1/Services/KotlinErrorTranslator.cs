using System.Text.RegularExpressions;

namespace WebApplication1.Services
{
    public static class KotlinErrorTranslator
    {
        public static string Translate(string stderr, string stdout, string? pistonMessage, string? signal = null)
        {
            if (pistonMessage?.Contains("Time limit exceeded") == true ||
                pistonMessage?.Contains("TO") == true ||
                signal == "SIGKILL" ||
                signal == "SIGTERM" ||
                stderr.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                stdout.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                return "Превышено время выполнения. Проверьте наличие бесконечного цикла.";

            if (string.IsNullOrWhiteSpace(stderr) && string.IsNullOrWhiteSpace(stdout))
                return "Компилятор не вернул результат. Попробуйте ещё раз.";

            if (string.IsNullOrWhiteSpace(stderr)) return "";

            return TranslateCompilationError(stderr)
                ?? TranslateRuntimeError(stderr)
                ?? SmartFallback(stderr);
        }

        private static string? TranslateCompilationError(string stderr)
        {
            var line = ExtractLineNumberFromError(stderr);
            var prefix = line != null ? $"Строка {line}: " : "";

            var m = Regex.Match(stderr, @"error: unresolved reference:?\s*'?(\w+)'?", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                var name = m.Groups[1].Value;
                var fix = SuggestCorrection(name);
                return $"{prefix}Неизвестное имя `{name}`." +
                       (fix != null ? $" Возможно, вы имели в виду `{fix}`?" : " Проверьте написание.");
            }

            m = Regex.Match(stderr, @"error: type mismatch[:\s]+inferred type is (\S+) but (\S+) was expected", RegexOptions.IgnoreCase);
            if (m.Success)
                return $"{prefix}Несоответствие типов: ожидался `{m.Groups[2].Value}`, получен `{m.Groups[1].Value}`.";

            if (stderr.Contains("error: expecting an expression") ||
                stderr.Contains("error: expecting ')'") ||
                stderr.Contains("error: expecting '}'") ||
                stderr.Contains("error: expecting '('"))
                return $"{prefix}Синтаксическая ошибка. Проверьте расстановку скобок.";

            if (stderr.Contains("overload resolution ambiguity"))
                return $"{prefix}Функция вызвана без аргумента или с неверным типом аргумента.";

            if (stderr.Contains("val cannot be reassigned"))
                return $"{prefix}Нельзя изменить переменную `val`. Используйте `var`.";

            if (stderr.Contains("return type mismatch"))
                return $"{prefix}Функция возвращает значение не того типа, что указан в объявлении.";

            if (stderr.Contains("none of the following candidates is applicable"))
                return $"{prefix}Неверные аргументы функции. Проверьте типы передаваемых значений.";

            m = Regex.Match(stderr, @"variable '(\w+)' must be initialized");
            if (m.Success)
                return $"{prefix} Переменная `";
            if (stderr.Contains("must be initialized"))
                return $"{prefix}Переменная не инициализирована. Присвойте ей начальное значение.";

            if (stderr.Contains("only safe") || stderr.Contains("non-null asserted"))
                return $"{prefix}Возможное обращение к `null`. Используйте `?.` или проверьте значение.";

            m = Regex.Match(stderr, @"error: incompatible types:\s*(\S+)\s*and\s*(\S+)", RegexOptions.IgnoreCase);
            if (m.Success)
                return $"{prefix} Несовместимые типы: `";

            m = Regex.Match(stderr, @"error: no value passed for parameter '(\w+)'");
            if (m.Success)
                return $"{prefix} Не передан аргумент `";

            if (stderr.Contains("error: too many arguments"))
                return $"{prefix}Слишком много аргументов. Проверьте сигнатуру функции.";

            if (stderr.Contains("w:") && !stderr.Contains("error:"))
                return null;

            if (stderr.Contains("function must have a body"))
                return $"{prefix}У функции нет тела. Добавьте блок `{{}}` с реализацией.";

            if (stderr.Contains("a 'return' expression required"))
                return $"{prefix}Функция должна возвращать значение. Добавьте `return`.";

            m = Regex.Match(stderr, @"error: modifier '(\w+)' is not applicable");
            if (m.Success)
                return $"{prefix} Модификатор `";

            m = Regex.Match(stderr, @"error: .* is already defined");
            if (m.Success)
                return $"{prefix}Имя уже объявлено в этой области видимости. Используйте другое имя.";

            if (stderr.Contains("error: expression is unused"))
                return $"{prefix}Выражение не используется. Возможно, пропущен оператор или вызов.";

            if (stderr.Contains("'break' or 'continue' jumps across a function") ||
                stderr.Contains("'break' is not allowed outside a loop"))
                return $"{prefix}`break` или `continue` используется вне цикла.";

            return null;
        }

        private static string? TranslateRuntimeError(string stderr)
        {
            if (stderr.Contains("StackOverflowError"))
                return "Переполнение стека: бесконечная рекурсия. Убедитесь, что есть условие выхода.";

            var m = Regex.Match(stderr, @"NumberFormatException.*?""([^""]+)""", RegexOptions.IgnoreCase);
            if (m.Success)
                return $"Не удалось преобразовать `{m.Groups[1].Value}` в число. Проверьте входные данные.";
            if (stderr.Contains("NumberFormatException"))
                return "Ошибка преобразования в число. Входные данные имеют неверный формат.";

            if (stderr.Contains("ArrayIndexOutOfBoundsException") ||
                stderr.Contains("IndexOutOfBoundsException"))
                return "Выход за границы массива или списка. Проверьте индексы.";

            if (stderr.Contains("StringIndexOutOfBoundsException"))
                return "Выход за границы строки. Проверьте длину строки перед обращением к символам.";

            if (stderr.Contains("NullPointerException"))
                return "Обращение к `null`. Проверьте, что переменная инициализирована.";

            m = Regex.Match(stderr, @"ClassCastException.*?(\w+) cannot be cast to.*?(\w+)", RegexOptions.IgnoreCase);
            if (m.Success)
                return $"Невозможно преобразовать `{m.Groups[1].Value}` к типу `{m.Groups[2].Value}`.";

            if (stderr.Contains("ArithmeticException") && stderr.Contains("zero"))
                return "Деление на ноль.";

            if (stderr.Contains("OutOfMemoryError"))
                return "Недостаточно памяти. Проверьте, нет ли бесконечного накопления данных.";

            if (stderr.Contains("NoSuchElementException"))
                return "Попытка получить элемент из пустой коллекции.";

            if (stderr.Contains("ConcurrentModificationException"))
                return "Изменение коллекции во время итерации по ней.";

            if (stderr.Contains("IllegalArgumentException"))
            {
                m = Regex.Match(stderr, @"IllegalArgumentException:\s*(.+)");
                var detail = m.Success ? $": {m.Groups[1].Value.Trim()}" : "";
                return $"Неверный аргумент{detail}.";
            }

            if (stderr.Contains("IllegalStateException"))
                return "Некорректное состояние программы. Проверьте порядок операций.";

            if (stderr.Contains("UnsupportedOperationException"))
                return "Операция не поддерживается для данного типа данных.";

            if (stderr.Contains("NullPointerException") ||
                (stderr.Contains("readLine") && stderr.Contains("null")))
                return "Программа ожидает ввод данных, но он не был предоставлен.";

            return null;
        }

        private static string SmartFallback(string stderr)
        {
            var lines = stderr.Split('\n');
            var meaningful = new List<string>();

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line)) continue;
                if (line.StartsWith("at ")) continue;
                if (line == "^") continue;
                if (line.StartsWith("^\t")) continue;
                if (line.StartsWith("w: ")) continue;
                if (line.StartsWith("    ")) continue;
                if (line.StartsWith("public ")) continue;
                if (line.Length < 3) continue;

                if (Regex.IsMatch(line, @"^\s*(val|var|fun|println|print|readLine|if|for|while)"))
                    continue;

                var cleaned = Regex.Replace(line,
                    @"^file0\.code\.kt:(\d+):\d+:\s*(error|warning):\s*",
                    m => $"Строка {m.Groups[1].Value}: ");

                cleaned = Regex.Replace(cleaned, @"^w:\s*file0\.code\.kt:\d+:\d+:\s*", "");

                if (!string.IsNullOrEmpty(cleaned))
                    meaningful.Add(cleaned);

                if (meaningful.Count >= 2) break;
            }

            if (!meaningful.Any())
                return "Ошибка выполнения. Проверьте код.";

            return "" + string.Join(" ", meaningful);
        }

        private static string? SuggestCorrection(string name)
        {
            var corrections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["prntln"] = "println",
                ["printl"] = "println",
                ["prinln"] = "println",
                ["prtinln"] = "println",
                ["prinltn"] = "println",
                ["prtinl"] = "println",
                ["readlin"] = "readLine",
                ["readline"] = "readLine",
                ["mian"] = "main",
                ["maIn"] = "main",
                ["Sting"] = "String",
                ["stirng"] = "String",
                ["interger"] = "Int",
                ["intger"] = "Int",
                ["lenght"] = "length",
                ["legnth"] = "length",
                ["toSting"] = "toString",
                ["tostring"] = "toString",
                ["forech"] = "forEach",
                ["forEeach"] = "forEach",
                ["whlie"] = "while",
                ["wihle"] = "while",
                ["retrun"] = "return",
                ["retrn"] = "return",
                ["prinf"] = "printf",
                ["listOf_"] = "listOf",
                ["arrayOf_"] = "arrayOf",
                ["mutableListof"] = "mutableListOf",
            };

            return corrections.TryGetValue(name, out var s) ? s : null;
        }

        private static string? ExtractLineNumberFromError(string stderr)
        {
            var m = Regex.Match(stderr, @"file0\.code\.kt:(\d+):");
            return m.Success ? m.Groups[1].Value : null;
        }
    }
}
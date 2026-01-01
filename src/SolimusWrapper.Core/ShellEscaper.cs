using System.Text;

namespace SolimusWrapper.Core;

/// <summary>
/// Утилиты для безопасного экранирования аргументов командной строки
/// </summary>
public static class ShellEscaper
{
    private static readonly char[] WindowsDangerousChars =
        ['&', '|', '<', '>', '^', '%', '!', '"', ' ', '\t', ';', '(', ')', '@'];

    private static readonly char[] UnixDangerousChars =
    [
        '&', '|', '<', '>', ';', '$', '`', '\\', '"', '\'', ' ', '\t', '\n', '(', ')', '{', '}', '[', ']', '*', '?',
        '#', '~'
    ];

    // Общий список для ContainsDangerousCharacters (кросс-платформенный)
    private static readonly char[] AllDangerousChars =
    [
        '&', '|', '<', '>', ';', '$', '`', '\\', '"', '\'', ' ', '\t', '\n', '(', ')', '{', '}', '[', ']', '*', '?',
        '#', '~', '^', '%', '!', '@'
    ];

    /// <summary>
    /// Экранирует аргумент для текущей платформы
    /// </summary>
    public static string Escape(string argument)
    {
        if (string.IsNullOrEmpty(argument))
            return OperatingSystem.IsWindows() ? "\"\"" : "''";

        return OperatingSystem.IsWindows()
            ? EscapeWindows(argument)
            : EscapeUnix(argument);
    }

    /// <summary>
    /// Экранирует несколько аргументов
    /// </summary>
    public static IEnumerable<string> Escape(IEnumerable<string> arguments)
        => arguments.Select(Escape);

    /// <summary>
    /// Экранирует аргумент для Windows (cmd.exe)
    /// </summary>
    public static string EscapeWindows(string argument)
    {
        if (string.IsNullOrEmpty(argument))
            return "\"\"";

        if (!argument.Any(c => WindowsDangerousChars.Contains(c)))
            return argument;

        var escaped = new StringBuilder();
        escaped.Append('"');

        foreach (var c in argument)
        {
            switch (c)
            {
                case '"':
                    escaped.Append("\\\"");
                    break;
                case '\\':
                    escaped.Append("\\\\");
                    break;
                case '%':
                    escaped.Append("%%");
                    break;
                default:
                    escaped.Append(c);
                    break;
            }
        }

        escaped.Append('"');
        return escaped.ToString();
    }

    /// <summary>
    /// Экранирует аргумент для Unix (bash/sh)
    /// </summary>
    public static string EscapeUnix(string argument)
    {
        if (string.IsNullOrEmpty(argument))
            return "''";

        if (!argument.Any(c => UnixDangerousChars.Contains(c)))
            return argument;

        var escaped = new StringBuilder();
        escaped.Append('\'');

        foreach (var c in argument)
        {
            if (c == '\'')
            {
                escaped.Append("'\\''");
            }
            else
            {
                escaped.Append(c);
            }
        }

        escaped.Append('\'');
        return escaped.ToString();
    }

    /// <summary>
    /// Экранирует аргумент для PowerShell
    /// </summary>
    public static string EscapePowerShell(string argument)
    {
        if (string.IsNullOrEmpty(argument))
            return "''";

        if (!argument.Any(c => c == '\'' || c == '"' || c == '$' || c == '`' || char.IsWhiteSpace(c)))
            return argument;

        var escaped = new StringBuilder();
        escaped.Append('\'');

        foreach (var c in argument)
        {
            if (c == '\'')
            {
                escaped.Append("''");
            }
            else
            {
                escaped.Append(c);
            }
        }

        escaped.Append('\'');
        return escaped.ToString();
    }

    /// <summary>
    /// Проверяет, содержит ли строка потенциально опасные символы (кросс-платформенно)
    /// </summary>
    public static bool ContainsDangerousCharacters(string? argument)
    {
        if (string.IsNullOrEmpty(argument))
            return false;

        return argument.Any(c => AllDangerousChars.Contains(c));
    }

    /// <summary>
    /// Проверяет, похож ли аргумент на попытку инъекции команды
    /// </summary>
    public static bool LooksLikeInjection(string? argument)
    {
        if (string.IsNullOrEmpty(argument))
            return false;

        var injectionPatterns = new[]
        {
            "&&", "||", "|", ";", "$(",
            "$(", "`", ">", "<", ">>",
            "<<", "&>", "2>", "2>&1"
        };

        return injectionPatterns.Any(p => argument.Contains(p));
    }

    /// <summary>
    /// Собирает команду в строку с правильным экранированием
    /// </summary>
    public static string BuildCommandLine(string executable, params string[] arguments)
    {
        var escaped = arguments.Select(Escape);
        return $"{Escape(executable)} {string.Join(" ", escaped)}";
    }

    /// <summary>
    /// Собирает команду в строку с правильным экранированием
    /// </summary>
    public static string BuildCommandLine(string executable, IEnumerable<string> arguments)
    {
        var escaped = arguments.Select(Escape);
        return $"{Escape(executable)} {string.Join(" ", escaped)}";
    }
}
using System.Text.RegularExpressions;

namespace SolimusWrapper.Core.Logging;

/// <summary>
/// Логгер команд в консоль
/// </summary>
public class ConsoleCommandLogger : ICommandLogger
{
    private readonly LoggingOptions _options;
    private readonly object _lock = new();

    public ConsoleCommandLogger() : this(new LoggingOptions()) { }

    public ConsoleCommandLogger(LoggingOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public void LogCommandStart(CommandStartInfo info)
    {
        if (!_options.LogCommandStart)
            return;

        var commandLine = MaskSensitiveData(info.CommandLine);
        var workingDir = info.WorkingDirectory ?? Environment.CurrentDirectory;

        WriteLog(_options.CommandLevel, "CMD", $"Starting: {commandLine}");
        WriteLog(CommandLogLevel.Debug, "CMD", $"Working directory: {workingDir}");
    }

    public void LogStandardOutput(string line)
    {
        if (_options.StandardOutputLevel == CommandLogLevel.None)
            return;

        var maskedLine = MaskSensitiveData(TruncateLine(line));
        WriteLog(_options.StandardOutputLevel, "OUT", maskedLine);
    }

    public void LogStandardError(string line)
    {
        if (_options.StandardErrorLevel == CommandLogLevel.None)
            return;

        var maskedLine = MaskSensitiveData(TruncateLine(line));
        WriteLog(_options.StandardErrorLevel, "ERR", maskedLine);
    }

    public void LogCommandEnd(CommandEndInfo info)
    {
        if (!_options.LogCommandEnd)
            return;

        var status = info.IsSuccess ? "completed successfully" : $"failed with exit code {info.ExitCode}";
        var level = info.IsSuccess ? _options.CommandLevel : CommandLogLevel.Error;

        WriteLog(level, "CMD", $"Command {status} in {info.Duration.TotalMilliseconds:F0}ms");
    }

    public void LogError(Exception exception)
    {
        WriteLog(CommandLogLevel.Error, "ERR", $"Exception: {exception.Message}");

        if (exception.StackTrace != null)
        {
            WriteLog(CommandLogLevel.Debug, "ERR", exception.StackTrace);
        }
    }

    public void LogRetry(RetryAttempt attempt)
    {
        var reason = attempt.LastException?.Message ?? $"Exit code: {attempt.LastExitCode}";
        WriteLog(
            CommandLogLevel.Warning,
            "RETRY",
            $"Attempt {attempt.AttemptNumber}/{attempt.MaxAttempts} failed: {reason}. " +
            $"Retrying in {attempt.NextDelay.TotalMilliseconds:F0}ms...");
    }

    private void WriteLog(CommandLogLevel level, string category, string message)
    {
        lock (_lock)
        {
            var originalColor = Console.ForegroundColor;

            try
            {
                Console.ForegroundColor = GetColorForLevel(level);

                var timestamp = _options.IncludeTimestamp
                    ? $"[{DateTime.Now.ToString(_options.TimestampFormat)}] "
                    : "";

                var levelStr = GetLevelString(level);
                Console.WriteLine($"{timestamp}[{levelStr}] [{category}] {message}");
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }
    }

    private string MaskSensitiveData(string input)
    {
        if (!_options.MaskSensitiveData || string.IsNullOrEmpty(input))
            return input;

        var result = input;

        foreach (var pattern in _options.SensitivePatterns)
        {
            try
            {
                result = Regex.Replace(result, pattern, match =>
                {
                    var parts = match.Value.Split([':', '=', ' '], 2);
                    return parts.Length > 1 ? $"{parts[0]}=****" : "****";
                }, RegexOptions.IgnoreCase);
            }
            catch
            {
                // Ignore invalid patterns
            }
        }

        return result;
    }

    private string TruncateLine(string line)
    {
        if (line.Length <= _options.MaxLineLength)
            return line;

        return line[.._options.MaxLineLength] + "... (truncated)";
    }

    private static ConsoleColor GetColorForLevel(CommandLogLevel level) => level switch
    {
        CommandLogLevel.Trace => ConsoleColor.DarkGray,
        CommandLogLevel.Debug => ConsoleColor.Gray,
        CommandLogLevel.Information => ConsoleColor.White,
        CommandLogLevel.Warning => ConsoleColor.Yellow,
        CommandLogLevel.Error => ConsoleColor.Red,
        _ => ConsoleColor.White
    };

    private static string GetLevelString(CommandLogLevel level) => level switch
    {
        CommandLogLevel.Trace => "TRC",
        CommandLogLevel.Debug => "DBG",
        CommandLogLevel.Information => "INF",
        CommandLogLevel.Warning => "WRN",
        CommandLogLevel.Error => "ERR",
        _ => "???"
    };
}
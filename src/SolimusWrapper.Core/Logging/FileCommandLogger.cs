using System.Text;
using System.Text.RegularExpressions;

namespace SolimusWrapper.Core.Logging;

/// <summary>
/// Логгер команд в файл
/// </summary>
public sealed class FileCommandLogger : ICommandLogger, IDisposable
{
    private readonly LoggingOptions _options;
    private readonly StreamWriter _writer;
    private readonly object _lock = new();
    private bool _disposed;

    public FileCommandLogger(string filePath) : this(filePath, new LoggingOptions()) { }

    public FileCommandLogger(string filePath, LoggingOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        _writer = new StreamWriter(filePath, append: true, Encoding.UTF8)
        {
            AutoFlush = true
        };
    }

    public void LogCommandStart(CommandStartInfo info)
    {
        if (!_options.LogCommandStart)
            return;

        var commandLine = MaskSensitiveData(info.CommandLine);
        WriteLine($"[{GetTimestamp()}] [CMD] [START] {commandLine}");
        WriteLine($"[{GetTimestamp()}] [CMD] [INFO] Working directory: {info.WorkingDirectory ?? "default"}");
    }

    public void LogStandardOutput(string line)
    {
        if (_options.StandardOutputLevel == CommandLogLevel.None)
            return;

        var maskedLine = MaskSensitiveData(line);
        WriteLine($"[{GetTimestamp()}] [OUT] {maskedLine}");
    }

    public void LogStandardError(string line)
    {
        if (_options.StandardErrorLevel == CommandLogLevel.None)
            return;

        var maskedLine = MaskSensitiveData(line);
        WriteLine($"[{GetTimestamp()}] [ERR] {maskedLine}");
    }

    public void LogCommandEnd(CommandEndInfo info)
    {
        if (!_options.LogCommandEnd)
            return;

        var status = info.IsSuccess ? "SUCCESS" : $"FAILED ({info.ExitCode})";
        WriteLine($"[{GetTimestamp()}] [CMD] [END] {status} Duration: {info.Duration.TotalMilliseconds:F0}ms");
    }

    public void LogError(Exception exception)
    {
        WriteLine($"[{GetTimestamp()}] [ERR] Exception: {exception.Message}");
        if (exception.StackTrace != null)
        {
            WriteLine($"[{GetTimestamp()}] [ERR] {exception.StackTrace}");
        }
    }

    public void LogRetry(RetryAttempt attempt)
    {
        var reason = attempt.LastException?.Message ?? $"Exit code: {attempt.LastExitCode}";
        WriteLine($"[{GetTimestamp()}] [RETRY] Attempt {attempt.AttemptNumber}/{attempt.MaxAttempts}: {reason}");
    }

    private void WriteLine(string message)
    {
        lock (_lock)
        {
            if (!_disposed)
            {
                _writer.WriteLine(message);
            }
        }
    }

    private string GetTimestamp() => DateTime.Now.ToString(_options.TimestampFormat);

    private string MaskSensitiveData(string input)
    {
        if (!_options.MaskSensitiveData || string.IsNullOrEmpty(input))
            return input;

        var result = input;

        foreach (var pattern in _options.SensitivePatterns)
        {
            try
            {
                result = Regex.Replace(result, pattern, "****", RegexOptions.IgnoreCase);
            }
            catch
            {
                // Ignore invalid patterns
            }
        }

        return result;
    }

    public void Dispose()
    {
        if (_disposed) return;

        lock (_lock)
        {
            _disposed = true;
            _writer.Dispose();
        }
    }
}
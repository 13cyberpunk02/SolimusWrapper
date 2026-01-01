namespace SolimusWrapper.Core.Logging;

/// <summary>
/// Уровень логирования
/// </summary>
public enum CommandLogLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    None
}

/// <summary>
/// Интерфейс логгера для команд
/// </summary>
public interface ICommandLogger
{
    /// <summary>
    /// Логирует запуск команды
    /// </summary>
    void LogCommandStart(CommandStartInfo info);

    /// <summary>
    /// Логирует строку stdout
    /// </summary>
    void LogStandardOutput(string line);

    /// <summary>
    /// Логирует строку stderr
    /// </summary>
    void LogStandardError(string line);

    /// <summary>
    /// Логирует завершение команды
    /// </summary>
    void LogCommandEnd(CommandEndInfo info);

    /// <summary>
    /// Логирует ошибку
    /// </summary>
    void LogError(Exception exception);

    /// <summary>
    /// Логирует повторную попытку
    /// </summary>
    void LogRetry(RetryAttempt attempt);
}

/// <summary>
/// Информация о запуске команды
/// </summary>
public readonly record struct CommandStartInfo(
    string TargetFilePath,
    IReadOnlyList<string> Arguments,
    string? WorkingDirectory,
    DateTimeOffset StartTime)
{
    public string CommandLine => string.IsNullOrEmpty(string.Join(" ", Arguments))
        ? TargetFilePath
        : $"{TargetFilePath} {string.Join(" ", Arguments)}";
}

/// <summary>
/// Информация о завершении команды
/// </summary>
public readonly record struct CommandEndInfo(
    string TargetFilePath,
    int ExitCode,
    bool IsSuccess,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    TimeSpan Duration);
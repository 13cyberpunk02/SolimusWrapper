namespace SolimusWrapper.Core.Logging;

/// <summary>
/// Логгер, который пишет в несколько логгеров одновременно
/// </summary>
public sealed class CompositeCommandLogger : ICommandLogger
{
    private readonly ICommandLogger[] _loggers;

    public CompositeCommandLogger(params ICommandLogger[] loggers)
    {
        _loggers = loggers ?? throw new ArgumentNullException(nameof(loggers));
    }

    public void LogCommandStart(CommandStartInfo info)
    {
        foreach (var logger in _loggers)
            logger.LogCommandStart(info);
    }

    public void LogStandardOutput(string line)
    {
        foreach (var logger in _loggers)
            logger.LogStandardOutput(line);
    }

    public void LogStandardError(string line)
    {
        foreach (var logger in _loggers)
            logger.LogStandardError(line);
    }

    public void LogCommandEnd(CommandEndInfo info)
    {
        foreach (var logger in _loggers)
            logger.LogCommandEnd(info);
    }

    public void LogError(Exception exception)
    {
        foreach (var logger in _loggers)
            logger.LogError(exception);
    }

    public void LogRetry(RetryAttempt attempt)
    {
        foreach (var logger in _loggers)
            logger.LogRetry(attempt);
    }
}
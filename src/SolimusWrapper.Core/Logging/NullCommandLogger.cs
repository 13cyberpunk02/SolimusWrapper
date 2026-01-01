namespace SolimusWrapper.Core.Logging;

/// <summary>
/// Логгер, который ничего не делает
/// </summary>
public sealed class NullCommandLogger : ICommandLogger
{
    public static NullCommandLogger Instance { get; } = new();

    private NullCommandLogger() { }

    public void LogCommandStart(CommandStartInfo info) { }
    public void LogStandardOutput(string line) { }
    public void LogStandardError(string line) { }
    public void LogCommandEnd(CommandEndInfo info) { }
    public void LogError(Exception exception) { }
    public void LogRetry(RetryAttempt attempt) { }
}
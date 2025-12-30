namespace SolimusWrapper.Core;

/// <summary>
/// Результат выполнения команды
/// </summary>
public readonly record struct CommandResult(
    int ExitCode,
    DateTimeOffset StartTime,
    DateTimeOffset ExitTime)
{
    public TimeSpan RunTime => ExitTime - StartTime;
    public bool IsSuccess => ExitCode == 0;
    
    public void EnsureSuccess()
    {
        if (!IsSuccess)
            throw new CommandExecutionException(ExitCode);
    }
}

public class CommandExecutionException(int exitCode) 
    : Exception($"Command failed with exit code {exitCode}")
{
    public int ExitCode { get; } = exitCode;
}
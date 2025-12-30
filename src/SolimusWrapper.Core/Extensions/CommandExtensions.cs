namespace SolimusWrapper.Core.Extensions;

public static class CommandExtensions
{
    /// <summary>
    /// Быстрый запуск команды с аргументами
    /// </summary>
    public static ValueTask<CommandResult> RunAsync(
        string command, 
        params string[] args) =>
        Command.Run(command).WithArguments(args).ExecuteAsync();

    /// <summary>
    /// Выполняет shell команду
    /// </summary>
    public static Command Shell(string command)
    {
        return OperatingSystem.IsWindows() ? 
            Command.Run("cmd").WithArguments("/c", command) : 
            Command.Run("/bin/sh").WithArguments("-c", command);
    }

    /// <summary>
    /// Конвейер команд (pipe)
    /// </summary>
    public static async ValueTask<string> PipeAsync(
        this Command source,
        Command target,
        CancellationToken ct = default)
    {
        var output = await source.ExecuteAndReadOutputAsync(ct);
        return await target.ExecuteAndReadOutputAsync(ct);
    }
}
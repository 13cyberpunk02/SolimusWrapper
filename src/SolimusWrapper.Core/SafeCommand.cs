using SolimusWrapper.Core.Extensions;

namespace SolimusWrapper.Core;

/// <summary>
/// Обёртка для безопасного выполнения команд с автоматическим экранированием
/// </summary>
public static class SafeCommand
{
    /// <summary>
    /// Создаёт команду с автоматическим экранированием аргументов
    /// </summary>
    public static Command Run(string targetFilePath)
    {
        // Валидация пути к исполняемому файлу
        ValidateExecutablePath(targetFilePath);
        return Command.Run(targetFilePath);
    }

    /// <summary>
    /// Создаёт команду с безопасными аргументами
    /// </summary>
    public static Command RunWithSafeArgs(string targetFilePath, params string[] arguments)
    {
        ValidateExecutablePath(targetFilePath);
        ValidateArguments(arguments);

        return Command.Run(targetFilePath)
            .WithArguments(arguments);
    }

    /// <summary>
    /// Создаёт shell команду с безопасным экранированием
    /// </summary>
    public static Command Shell(string command)
    {
        if (ShellEscaper.LooksLikeInjection(command))
        {
            throw new ArgumentException(
                $"Command contains potentially dangerous patterns: {command}",
                nameof(command));
        }

        return CommandExtensions.Shell(command);
    }

    private static void ValidateExecutablePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Executable path cannot be empty", nameof(path));

        if (path.Contains(".."))
            throw new ArgumentException("Executable path cannot contain '..'", nameof(path));

        if (ShellEscaper.LooksLikeInjection(path))
            throw new ArgumentException($"Executable path contains dangerous characters: {path}", nameof(path));
    }

    private static void ValidateArguments(string[] arguments)
    {
        foreach (var arg in arguments)
        {
            if (ShellEscaper.LooksLikeInjection(arg))
            {
                throw new ArgumentException(
                    $"Argument contains potentially dangerous patterns: {arg}",
                    nameof(arguments));
            }
        }
    }
}
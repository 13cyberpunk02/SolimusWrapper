namespace SolimusWrapper.Core.Extensions;

public static class CommandExtensions
{
    /// <summary>
    /// Выполняет shell команду
    /// </summary>
    public static Command Shell(string command) =>
        OperatingSystem.IsWindows()
            ? Command.Run("cmd").WithArguments("/c", command)
            : Command.Run("/bin/sh").WithArguments("-c", command);

    /// <summary>
    /// Список файлов в директории
    /// </summary>
    public static Command ListFiles(string? directory = null)
    {
        if (OperatingSystem.IsWindows())
        {
            var args = string.IsNullOrEmpty(directory)
                ? (List<string>)["/c", "dir"]
                : ["/c", "dir", directory];
            return Command.Run("cmd").WithArguments(args);
        }

        var lsArgs = string.IsNullOrEmpty(directory)
            ? (List<string>)["-c", "ls -la"]
            : ["-c", $"ls -la {directory}"];
        return Command.Run("/bin/sh").WithArguments(lsArgs);
    }

    /// <summary>
    /// Получить текущую директорию
    /// </summary>
    public static Command GetCurrentDirectory() =>
        OperatingSystem.IsWindows() ? Command.Run("cmd").WithArguments("/c", "cd") : Command.Run("pwd");

    /// <summary>
    /// Вывести текст
    /// </summary>
    public static Command Echo(string text) =>
        OperatingSystem.IsWindows() ?
            Command.Run("cmd").WithArguments("/c", $"echo {text}") :
            Command.Run("echo").WithArguments(text);

    /// <summary>
    /// Ожидание (sleep)
    /// </summary>
    public static Command Sleep(int seconds) =>
        OperatingSystem.IsWindows() ?
            Command.Run("powershell")
                .WithArguments("-Command", $"Start-Sleep -Seconds {seconds}") :
            Command.Run("sleep").WithArguments(seconds.ToString());

    /// <summary>
    /// Ping хоста
    /// </summary>
    public static Command Ping(string host, int count = 4) =>
        OperatingSystem.IsWindows() ? 
            Command.Run("ping").WithArguments("-n", count.ToString(), host) :
            Command.Run("ping").WithArguments("-c", count.ToString(), host);

    /// <summary>
    /// Очистить консоль
    /// </summary>
    public static Command Clear() =>
        OperatingSystem.IsWindows() ? 
        Command.Run("cmd").WithArguments("/c", "cls") : 
        Command.Run("clear");

    /// <summary>
    /// Получить переменную окружения
    /// </summary>
    public static Command GetEnvironmentVariable(string name) =>
        OperatingSystem.IsWindows() ? 
            Command.Run("cmd").WithArguments("/c", $"echo %{name}%") : 
            Command.Run("/bin/sh").WithArguments("-c", $"echo ${name}");

    /// <summary>
    /// Найти файлы по шаблону
    /// </summary>
    public static Command FindFiles(string pattern, string? directory = null)
    {
        var dir = directory ?? ".";

        return OperatingSystem.IsWindows() ? 
            Command.Run("cmd").WithArguments("/c", $"dir /s /b {dir}\\{pattern}") : 
            Command.Run("find").WithArguments(dir, "-name", pattern);
    }

    /// <summary>
    /// Проверить существование файла
    /// </summary>
    public static Command FileExists(string path) =>
        OperatingSystem.IsWindows() ? 
            Command.Run("cmd").WithArguments("/c", $"if exist \"{path}\" (echo true) else (echo false)") : 
            Command.Run("/bin/sh").WithArguments("-c", $"[ -f \"{path}\" ] && echo true || echo false");
}
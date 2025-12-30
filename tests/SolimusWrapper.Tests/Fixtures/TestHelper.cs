using System.Runtime.InteropServices;

namespace SolimusWrapper.Tests.Fixtures;

public static class TestHelper
{
    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    public static bool IsUnix => IsLinux || IsMacOS;

    /// <summary>
    /// Возвращает команду echo для текущей платформы
    /// </summary>
    public static (string Command, string[] Args) GetEchoCommand(string text)
    {
        if (IsWindows)
            return ("cmd", ["/c", $"echo {text}"]);

        return ("echo", [text]);
    }

    /// <summary>
    /// Возвращает команду sleep для текущей платформы
    /// </summary>
    public static (string Command, string[] Args) GetSleepCommand(int seconds) =>
         IsWindows ? ("ping", ["-n", (seconds + 1).ToString(), "127.0.0.1"]) :
            ("sleep", [seconds.ToString()]);

    /// <summary>
    /// Возвращает команду cat/type для текущей платформы
    /// </summary>
    public static (string Command, string[] Args) GetCatCommand()
    {
        if (IsWindows)
            return ("cmd", ["/c", "findstr", ".*"]);

        return ("cat", []);
    }

    /// <summary>
    /// Команда которая возвращает exit code
    /// </summary>
    public static (string Command, string[] Args) GetExitCodeCommand(int exitCode)
    {
        if (IsWindows)
            return ("cmd", ["/c", $"exit {exitCode}"]);

        return ("sh", ["-c", $"exit {exitCode}"]);
    }

    /// <summary>
    /// Команда которая пишет в stderr
    /// </summary>
    public static (string Command, string[] Args) GetStdErrCommand(string text)
    {
        if (IsWindows)
            return ("cmd", ["/c", $"echo {text} 1>&2"]);

        return ("sh", ["-c", $"echo '{text}' >&2"]);
    }

    /// <summary>
    /// Создаёт временный файл и возвращает путь
    /// </summary>
    public static string CreateTempFile(string content = "")
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>
    /// Создаёт временную директорию
    /// </summary>
    public static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(path);
        return path;
    }
    
    /// <summary>
    /// Возвращает команду для получения текущей директории
    /// </summary>
    public static (string Command, string[] Args) GetPwdCommand()
    {
        if (IsWindows)
            return ("cmd", ["/c", "cd"]);
    
        return ("pwd", []);
    }
}
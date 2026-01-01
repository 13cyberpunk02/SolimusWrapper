using FluentAssertions;
using SolimusWrapper.Core;
using SolimusWrapper.Core.Logging;
using SolimusWrapper.Tests.Fixtures;

namespace SolimusWrapper.Tests;

public class LoggingTests
{
    #region LoggingOptions Tests

    [Fact]
    public void LoggingOptions_Default_HasCorrectValues()
    {
        // Act
        var options = new LoggingOptions();

        // Assert
        options.LogCommandStart.Should().BeTrue();
        options.LogCommandEnd.Should().BeTrue();
        options.StandardOutputLevel.Should().Be(CommandLogLevel.Debug);
        options.StandardErrorLevel.Should().Be(CommandLogLevel.Warning);
        options.MaskSensitiveData.Should().BeTrue();
        options.IncludeTimestamp.Should().BeTrue();
        options.MaxLineLength.Should().Be(1000);
    }

    #endregion

    #region NullCommandLogger Tests

    [Fact]
    public void NullCommandLogger_Instance_IsSingleton()
    {
        // Act
        var instance1 = NullCommandLogger.Instance;
        var instance2 = NullCommandLogger.Instance;

        // Assert
        instance1.Should().BeSameAs(instance2);
    }

    [Fact]
    public void NullCommandLogger_AllMethods_DoNotThrow()
    {
        // Arrange
        var logger = NullCommandLogger.Instance;

        // Act & Assert - должны не выбрасывать исключения
        var act1 = () => logger.LogCommandStart(default);
        var act2 = () => logger.LogStandardOutput("test");
        var act3 = () => logger.LogStandardError("test");
        var act4 = () => logger.LogCommandEnd(default);
        var act5 = () => logger.LogError(new Exception());
        var act6 = () => logger.LogRetry(default);

        act1.Should().NotThrow();
        act2.Should().NotThrow();
        act3.Should().NotThrow();
        act4.Should().NotThrow();
        act5.Should().NotThrow();
        act6.Should().NotThrow();
    }

    #endregion

    #region ConsoleCommandLogger Tests

    [Fact]
    public void ConsoleCommandLogger_DefaultConstructor_CreatesLogger()
    {
        // Act
        var logger = new ConsoleCommandLogger();

        // Assert
        logger.Should().NotBeNull();
    }

    [Fact]
    public void ConsoleCommandLogger_WithOptions_CreatesLogger()
    {
        // Arrange
        var options = new LoggingOptions
        {
            LogCommandStart = false,
            LogCommandEnd = false
        };

        // Act
        var logger = new ConsoleCommandLogger(options);

        // Assert
        logger.Should().NotBeNull();
    }

    [Fact]
    public void ConsoleCommandLogger_NullOptions_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ConsoleCommandLogger(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region FileCommandLogger Tests

    [Fact]
    public void FileCommandLogger_CreatesLogFile()
    {
        // Arrange
        var logPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.log");

        try
        {
            // Act
            using (var logger = new FileCommandLogger(logPath))
            {
                logger.LogStandardOutput("test line");
            }

            // Assert
            File.Exists(logPath).Should().BeTrue();
            File.ReadAllText(logPath).Should().Contain("test line");
        }
        finally
        {
            if (File.Exists(logPath))
                File.Delete(logPath);
        }
    }

    [Fact]
    public void FileCommandLogger_LogsAllEvents()
    {
        // Arrange
        var logPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.log");

        try
        {
            using (var logger = new FileCommandLogger(logPath))
            {
                logger.LogCommandStart(new CommandStartInfo("test", ["arg1"], "/tmp", DateTimeOffset.Now));
                logger.LogStandardOutput("stdout line");
                logger.LogStandardError("stderr line");
                logger.LogCommandEnd(new CommandEndInfo("test", 0, true, DateTimeOffset.Now, DateTimeOffset.Now,
                    TimeSpan.FromSeconds(1)));
            }

            var content = File.ReadAllText(logPath);
            content.Should().Contain("[CMD]");
            content.Should().Contain("[START]");
            content.Should().Contain("[OUT]");
            content.Should().Contain("[ERR]");
            content.Should().Contain("[END]");
        }
        finally
        {
            CleanupFile(logPath);
        }
    }

    [Fact]
    public void FileCommandLogger_CreatesDirectory_IfNotExists()
    {
        // Arrange
        var dirPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var logPath = Path.Combine(dirPath, "test.log");

        try
        {
            // Act
            using (var logger = new FileCommandLogger(logPath))
            {
                logger.LogStandardOutput("test");
            }

            // Assert
            Directory.Exists(dirPath).Should().BeTrue();
            File.Exists(logPath).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(dirPath))
                Directory.Delete(dirPath, true);
        }
    }

    #endregion

    #region CompositeCommandLogger Tests

    [Fact]
    public void CompositeCommandLogger_CallsAllLoggers()
    {
        // Arrange
        var logger1Called = false;
        var logger2Called = false;

        var mockLogger1 = new TestCommandLogger(() => logger1Called = true);
        var mockLogger2 = new TestCommandLogger(() => logger2Called = true);

        var composite = new CompositeCommandLogger(mockLogger1, mockLogger2);

        // Act
        composite.LogStandardOutput("test");

        // Assert
        logger1Called.Should().BeTrue();
        logger2Called.Should().BeTrue();
    }

    [Fact]
    public void CompositeCommandLogger_NullLoggers_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new CompositeCommandLogger(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Command With Logging Integration Tests

    [Fact]
    public async Task Command_WithConsoleLogging_ExecutesSuccessfully()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetEchoCommand("logged");

        // Act
        var result = await Command.Run(cmd)
            .WithArguments(args)
            .WithConsoleLogging()
            .ExecuteAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Command_WithFileLogging_WritesToFile()
    {
        // Arrange
        var logPath = Path.Combine(Path.GetTempPath(), $"cmd_{Guid.NewGuid()}.log");
        var (cmd, args) = TestHelper.GetEchoCommand("file logged");

        try
        {
            // Создаём логгер явно для контроля жизненного цикла
            using (var logger = new FileCommandLogger(logPath))
            {
                var result = await Command.Run(cmd)
                    .WithArguments(args)
                    .WithLogger(logger)
                    .ExecuteAsync();

                result.IsSuccess.Should().BeTrue();
            }

            // Файл освобождён, можно читать
            File.Exists(logPath).Should().BeTrue();

            var content = await File.ReadAllTextAsync(logPath);
            content.Should().Contain("[CMD]");
        }
        finally
        {
            await CleanupFileAsync(logPath);
        }
    }

    [Fact]
    public async Task Command_WithLogger_PassesLoggerToExecution()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetEchoCommand("custom logger");
        var loggerCalled = false;
        var testLogger = new TestCommandLogger(() => loggerCalled = true);

        // Act
        await Command.Run(cmd)
            .WithArguments(args)
            .WithLogger(testLogger)
            .ExecuteAsync();

        // Assert
        loggerCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Command_WithoutLogging_RemovesLogger()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetEchoCommand("no logging");

        // Act - should not throw
        var result = await Command.Run(cmd)
            .WithArguments(args)
            .WithConsoleLogging()
            .WithoutLogging()
            .ExecuteAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Command_WithConsoleLogging_ConfigureOptions_Works()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetEchoCommand("configured");

        // Act
        var result = await Command.Run(cmd)
            .WithArguments(args)
            .WithConsoleLogging(options =>
            {
                options.LogCommandStart = false;
                options.LogCommandEnd = true;
                options.StandardOutputLevel = CommandLogLevel.Information;
            })
            .ExecuteAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Sensitive Data Masking Tests

    [Fact]
    public void FileCommandLogger_MasksSensitiveData()
    {
        // Arrange
        var logPath = Path.Combine(Path.GetTempPath(), $"mask_{Guid.NewGuid()}.log");
        var options = new LoggingOptions { MaskSensitiveData = true };

        try
        {
            using (var logger = new FileCommandLogger(logPath, options))
            {
                logger.LogStandardOutput("password=secret123");
                logger.LogStandardOutput("api_key: my-secret-key");
                logger.LogStandardOutput("Bearer token123abc");
            }

            var content = File.ReadAllText(logPath);
            content.Should().NotContain("secret123");
            content.Should().NotContain("my-secret-key");
            content.Should().NotContain("token123abc");
            content.Should().Contain("****");
        }
        finally
        {
            CleanupFile(logPath);
        }
    }

    [Fact]
    public void FileCommandLogger_NoMasking_WhenDisabled()
    {
        // Arrange
        var logPath = Path.Combine(Path.GetTempPath(), $"nomask_{Guid.NewGuid()}.log");
        var options = new LoggingOptions { MaskSensitiveData = false };

        try
        {
            using (var logger = new FileCommandLogger(logPath, options))
            {
                logger.LogStandardOutput("password=secret123");
            }

            var content = File.ReadAllText(logPath);
            content.Should().Contain("secret123");
        }
        finally
        {
            CleanupFile(logPath);
        }
    }

    #endregion

    #region CommandStartInfo and CommandEndInfo Tests

    [Fact]
    public void CommandStartInfo_CommandLine_FormatsCorrectly()
    {
        // Arrange
        var info = new CommandStartInfo("dotnet", ["build", "-c", "Release"], "/work", DateTimeOffset.Now);

        // Assert
        info.CommandLine.Should().Be("dotnet build -c Release");
    }

    [Fact]
    public void CommandStartInfo_CommandLine_NoArgs_ReturnsOnlyTarget()
    {
        // Arrange
        var info = new CommandStartInfo("dotnet", [], null, DateTimeOffset.Now);

        // Assert
        info.CommandLine.Should().Be("dotnet");
    }

    [Fact]
    public void CommandEndInfo_Properties_AreCorrect()
    {
        // Arrange
        var start = DateTimeOffset.Now;
        var end = start.AddSeconds(5);
        var info = new CommandEndInfo("test", 0, true, start, end, TimeSpan.FromSeconds(5));

        // Assert
        info.TargetFilePath.Should().Be("test");
        info.ExitCode.Should().Be(0);
        info.IsSuccess.Should().BeTrue();
        info.Duration.Should().Be(TimeSpan.FromSeconds(5));
    }

    #endregion
    
    private static void CleanupFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Ignore
        }
    }

    private static async Task CleanupFileAsync(string path)
    {
        await Task.Delay(50);
        CleanupFile(path);
    }
}

/// <summary>
/// Тестовый логгер для проверки вызовов
/// </summary>
file class TestCommandLogger : ICommandLogger
{
    private readonly Action _onLog;

    public TestCommandLogger(Action onLog)
    {
        _onLog = onLog;
    }

    public void LogCommandStart(CommandStartInfo info) => _onLog();
    public void LogStandardOutput(string line) => _onLog();
    public void LogStandardError(string line) => _onLog();
    public void LogCommandEnd(CommandEndInfo info) => _onLog();
    public void LogError(Exception exception) => _onLog();
    public void LogRetry(RetryAttempt attempt) => _onLog();
}
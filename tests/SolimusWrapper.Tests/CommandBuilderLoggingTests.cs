using FluentAssertions;
using SolimusWrapper.Core;
using SolimusWrapper.Core.Builders;
using SolimusWrapper.Core.Logging;
using SolimusWrapper.Tests.Fixtures;

namespace SolimusWrapper.Tests;

public class CommandBuilderLoggingTests
{
    [Fact]
    public async Task CommandBuilder_UseConsoleLogging_Works()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetEchoCommand("builder logging");

        // Act
        var result = await new CommandBuilder(cmd)
            .AddArguments(args)
            .UseConsoleLogging()
            .ExecuteAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CommandBuilder_UseFileLogging_CreatesFile()
    {
        // Arrange
        var logPath = Path.Combine(Path.GetTempPath(), $"builder_{Guid.NewGuid()}.log");
        var (cmd, args) = TestHelper.GetEchoCommand("builder file");

        try
        {
            // Создаём логгер явно
            using (var logger = new FileCommandLogger(logPath))
            {
                await new CommandBuilder(cmd)
                    .AddArguments(args)
                    .SetLogger(logger)
                    .ExecuteAsync();
            }

            // Assert
            File.Exists(logPath).Should().BeTrue();
        }
        finally
        {
            await Task.Delay(50);
            if (File.Exists(logPath))
            {
                try
                {
                    File.Delete(logPath);
                }
                catch
                {
                }
            }
        }
    }


    [Fact]
    public async Task CommandBuilder_SetLogger_UsesCustomLogger()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetEchoCommand("custom");
        var loggerUsed = false;
        var testLogger = new TestLogger(() => loggerUsed = true);

        // Act
        await new CommandBuilder(cmd)
            .AddArguments(args)
            .SetLogger(testLogger)
            .ExecuteAsync();

        // Assert
        loggerUsed.Should().BeTrue();
    }

    [Fact]
    public async Task CommandBuilder_UseConsoleLogging_WithOptions_Works()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetEchoCommand("options");

        // Act
        var result = await new CommandBuilder(cmd)
            .AddArguments(args)
            .UseConsoleLogging(options =>
            {
                options.LogCommandStart = false;
                options.StandardOutputLevel = CommandLogLevel.Information;
            })
            .ExecuteAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CommandBuilder_AddSafeArgument_EscapesCorrectly()
    {
        // Arrange & Act
        var result = await new CommandBuilder("dotnet")
            .AddSafeArgument("--version")
            .ExecuteAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CommandBuilder_ExecuteWithRetry_Works()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetEchoCommand("retry");

        // Act
        var result = await new CommandBuilder(cmd)
            .AddArguments(args)
            .ExecuteWithRetryAsync(RetryOptions.Immediate(2));

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CommandBuilder_ExecuteWithRetry_ConfigureAction_Works()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetEchoCommand("retry configure");

        // Act
        var result = await new CommandBuilder(cmd)
            .AddArguments(args)
            .ExecuteWithRetryAsync(options =>
            {
                options.MaxAttempts = 2;
                options.Delay = TimeSpan.Zero;
            });

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}

file class TestLogger : ICommandLogger
{
    private readonly Action _onLog;

    public TestLogger(Action onLog) => _onLog = onLog;

    public void LogCommandStart(CommandStartInfo info) => _onLog();
    public void LogStandardOutput(string line) => _onLog();
    public void LogStandardError(string line) => _onLog();
    public void LogCommandEnd(CommandEndInfo info) => _onLog();
    public void LogError(Exception exception) => _onLog();
    public void LogRetry(RetryAttempt attempt) => _onLog();
}
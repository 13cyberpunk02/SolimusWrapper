using FluentAssertions;
using SolimusWrapper.Core;
using SolimusWrapper.Tests.Fixtures;

namespace SolimusWrapper.Tests;

public class CommandTests
{
    #region Basic Execution

    [Fact]
    public async Task ExecuteAsync_SimpleCommand_ReturnsSuccessResult()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetEchoCommand("hello");

        // Act
        var result = await Command.Run(cmd)
            .WithArguments(args)
            .ExecuteAsync();

        // Assert
        result.ExitCode.Should().Be(0);
        result.IsSuccess.Should().BeTrue();
        result.RunTime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task ExecuteAndReadOutputAsync_EchoCommand_ReturnsOutput()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetEchoCommand("hello world");

        // Act
        var output = await Command.Run(cmd)
            .WithArguments(args)
            .ExecuteAndReadOutputAsync();

        // Assert
        output.Should().Contain("hello world");
    }

    [Fact]
    public async Task ExecuteAndReadAllAsync_ReturnsStdOutAndStdErr()
    {
        // Arrange
        string cmd, cmdArgs;
        if (TestHelper.IsWindows)
        {
            cmd = "cmd";
            cmdArgs = "/c echo stdout && echo stderr 1>&2";
        }
        else
        {
            cmd = "sh";
            cmdArgs = "-c";
        }

        // Act
        var (stdOut, stdErr) = await Command.Run(cmd)
            .WithArguments(TestHelper.IsWindows
                ? [cmdArgs]
                : [cmdArgs, "echo stdout && echo stderr >&2"])
            .WithValidation(false)
            .ExecuteAndReadAllAsync();

        // Assert
        stdOut.Should().Contain("stdout");
        stdErr.Should().Contain("stderr");
    }

    #endregion

    #region Exit Codes

    [Fact]
    public async Task ExecuteAsync_NonZeroExitCode_ThrowsException()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetExitCodeCommand(1);

        // Act
        var act = () => Command.Run(cmd)
            .WithArguments(args)
            .ExecuteAsync()
            .AsTask();

        // Assert
        await act.Should().ThrowAsync<CommandExecutionException>()
            .Where(e => e.ExitCode == 1);
    }

    [Fact]
    public async Task ExecuteAsync_NonZeroExitCode_WithValidationDisabled_DoesNotThrow()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetExitCodeCommand(42);

        // Act
        var result = await Command.Run(cmd)
            .WithArguments(args)
            .WithValidation(false)
            .ExecuteAsync();

        // Assert
        result.ExitCode.Should().Be(42);
        result.IsSuccess.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(255)]
    public async Task ExecuteAsync_VariousExitCodes_ReturnsCorrectCode(int expectedCode)
    {
        // Arrange
        var (cmd, args) = TestHelper.GetExitCodeCommand(expectedCode);

        // Act
        var result = await Command.Run(cmd)
            .WithArguments(args)
            .WithValidation(false)
            .ExecuteAsync();

        // Assert
        result.ExitCode.Should().Be(expectedCode);
    }

    #endregion

    #region Timeout

    [Fact]
    public async Task ExecuteAsync_WithTimeout_ThrowsTimeoutException()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetSleepCommand(10);

        // Act
        var act = () => Command.Run(cmd)
            .WithArguments(args)
            .WithTimeout(TimeSpan.FromMilliseconds(100))
            .ExecuteAsync()
            .AsTask();

        // Assert
        await act.Should().ThrowAsync<TimeoutException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithSufficientTimeout_Completes()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetEchoCommand("quick");

        // Act
        var result = await Command.Run(cmd)
            .WithArguments(args)
            .WithTimeout(TimeSpan.FromSeconds(30))
            .ExecuteAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Cancellation

    [Fact]
    public async Task ExecuteAsync_WithCancellation_ThrowsOperationCanceled()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetSleepCommand(10);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        var act = () => Command.Run(cmd)
            .WithArguments(args)
            .ExecuteAsync(cts.Token)
            .AsTask();

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region Working Directory

    [Fact]
    public async Task ExecuteAsync_WithWorkingDirectory_UsesCorrectDirectory()
    {
        // Arrange
        var tempDir = TestHelper.CreateTempDirectory();

        try
        {
            var (cmd, args) = TestHelper.GetPwdCommand();

            // Act
            var output = await Command.Run(cmd)
                .WithArguments(args)
                .WithWorkingDirectory(tempDir)
                .ExecuteAndReadOutputAsync();

            // Assert
            output.Should().Contain(Path.GetFileName(tempDir));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    #endregion

    #region Environment Variables

    [Fact]
    public async Task ExecuteAsync_WithEnvironmentVariable_PassesVariable()
    {
        // Arrange
        var varName = "MY_TEST_VAR";
        var varValue = "test_value_123";

        string cmd;
        string[] args;

        if (TestHelper.IsWindows)
        {
            cmd = "cmd";
            args = ["/c", $"echo %{varName}%"];
        }
        else
        {
            cmd = "sh";
            args = ["-c", $"echo ${varName}"];
        }

        // Act
        var output = await Command.Run(cmd)
            .WithArguments(args)
            .WithEnvironmentVariable(varName, varValue)
            .ExecuteAndReadOutputAsync();

        // Assert
        output.Should().Contain(varValue);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleEnvironmentVariables_PassesAllVariables()
    {
        // Arrange
        var vars = new Dictionary<string, string?>
        {
            ["VAR1"] = "value1",
            ["VAR2"] = "value2"
        };

        string cmd;
        string[] args;

        if (TestHelper.IsWindows)
        {
            cmd = "cmd";
            args = ["/c", "echo %VAR1% %VAR2%"];
        }
        else
        {
            cmd = "sh";
            args = ["-c", "echo $VAR1 $VAR2"];
        }

        // Act
        var output = await Command.Run(cmd)
            .WithArguments(args)
            .WithEnvironmentVariables(vars)
            .ExecuteAndReadOutputAsync();

        // Assert
        output.Should().Contain("value1");
        output.Should().Contain("value2");
    }

    #endregion

    #region Fluent API Immutability

    [Fact]
    public void FluentMethods_ReturnNewInstance()
    {
        // Arrange
        var original = Command.Run("test");

        // Act
        var withArgs = original.WithArguments("arg1");
        var withDir = original.WithWorkingDirectory("/tmp");
        var withTimeout = original.WithTimeout(TimeSpan.FromSeconds(10));

        // Assert
        withArgs.Should().NotBeSameAs(original);
        withDir.Should().NotBeSameAs(original);
        withTimeout.Should().NotBeSameAs(original);
    }

    [Fact]
    public void FluentMethods_ChainCorrectly()
    {
        // Arrange & Act
        var command = Command.Run("test")
            .WithArguments("arg1", "arg2")
            .WithWorkingDirectory("/tmp")
            .WithTimeout(TimeSpan.FromSeconds(30))
            .WithValidation(false);

        // Assert
        command.Should().NotBeNull();
        command.ToString().Should().Contain("test");
        command.ToString().Should().Contain("arg1");
    }

    #endregion

    #region OnExit Callback

    [Fact]
    public async Task ExecuteAsync_OnExitCallback_IsCalled()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetExitCodeCommand(42);
        int? capturedExitCode = null;

        // Act
        await Command.Run(cmd)
            .WithArguments(args)
            .WithValidation(false)
            .OnExit(code => capturedExitCode = code)
            .ExecuteAsync();

        // Assert
        capturedExitCode.Should().Be(42);
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ReturnsFormattedCommand()
    {
        // Arrange
        var command = Command.Run("dotnet")
            .WithArguments("build", "-c", "Release");

        // Act
        var result = command.ToString();

        // Assert
        result.Should().Be("dotnet build -c Release");
    }

    [Fact]
    public void ToString_EscapesArgumentsWithSpaces()
    {
        // Arrange
        var command = Command.Run("echo")
            .WithArguments("hello world", "test");

        // Act
        var result = command.ToString();

        // Assert
        result.Should().Contain("\"hello world\"");
    }

    #endregion
}
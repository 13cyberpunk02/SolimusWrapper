using System.Text;
using FluentAssertions;
using SolimusWrapper.Core;
using SolimusWrapper.Core.Builders;
using SolimusWrapper.Tests.Fixtures;

namespace SolimusWrapper.Tests;

public class CommandBuilderTests
{
    #region Basic Building

    [Fact]
    public void Build_WithTarget_CreatesCommand()
    {
        // Arrange & Act
        var command = new CommandBuilder("dotnet")
            .AddArgument("--version")
            .Build();

        // Assert
        command.Should().NotBeNull();
        command.ToString().Should().Contain("dotnet");
    }

    [Fact]
    public void Build_WithoutTarget_ThrowsException()
    {
        // Arrange
        var builder = new CommandBuilder();

        // Act
        var act = () => builder.Build();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Target file path*");
    }

    #endregion

    #region Arguments

    [Fact]
    public void AddArgument_SingleArgument_AddsToCommand()
    {
        // Act
        var command = new CommandBuilder("git")
            .AddArgument("status")
            .Build();

        // Assert
        command.ToString().Should().Be("git status");
    }

    [Fact]
    public void AddArguments_MultipleArguments_AddsAllToCommand()
    {
        // Act
        var command = new CommandBuilder("git")
            .AddArguments("commit", "-m", "message")
            .Build();

        // Assert
        command.ToString().Should().Be("git commit -m message");
    }

    [Fact]
    public void AddArgumentIf_ConditionTrue_AddsArgument()
    {
        // Act
        var command = new CommandBuilder("dotnet")
            .AddArgument("build")
            .AddArgumentIf(true, "--verbose")
            .Build();

        // Assert
        command.ToString().Should().Contain("--verbose");
    }

    [Fact]
    public void AddArgumentIf_ConditionFalse_DoesNotAddArgument()
    {
        // Act
        var command = new CommandBuilder("dotnet")
            .AddArgument("build")
            .AddArgumentIf(false, "--verbose")
            .Build();

        // Assert
        command.ToString().Should().NotContain("--verbose");
    }

    [Fact]
    public void AddArgumentIfNotEmpty_WithValue_AddsArgument()
    {
        // Act
        var command = new CommandBuilder("dotnet")
            .AddArgument("build")
            .AddArgumentIfNotEmpty("-c", "Release")
            .Build();

        // Assert
        command.ToString().Should().Contain("-c Release");
    }

    [Fact]
    public void AddArgumentIfNotEmpty_WithNullValue_DoesNotAddArgument()
    {
        // Act
        var command = new CommandBuilder("dotnet")
            .AddArgument("build")
            .AddArgumentIfNotEmpty("-c", null)
            .Build();

        // Assert
        command.ToString().Should().NotContain("-c");
    }

    [Fact]
    public void AddFlag_Enabled_AddsFlag()
    {
        // Act
        var command = new CommandBuilder("ls")
            .AddFlag("-la")
            .Build();

        // Assert
        command.ToString().Should().Contain("-la");
    }

    [Fact]
    public void AddFlag_Disabled_DoesNotAddFlag()
    {
        // Act
        var command = new CommandBuilder("ls")
            .AddFlag("-la", enabled: false)
            .Build();

        // Assert
        command.ToString().Should().NotContain("-la");
    }

    [Fact]
    public void ClearArguments_RemovesAllArguments()
    {
        // Act
        var command = new CommandBuilder("git")
            .AddArguments("arg1", "arg2")
            .ClearArguments()
            .AddArgument("arg3")
            .Build();

        // Assert
        command.ToString().Should().Be("git arg3");
    }

    #endregion

    #region Execution Shortcuts

    [Fact]
    public async Task ExecuteAsync_DirectExecution_Works()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetEchoCommand("test");

        // Act
        var result = await new CommandBuilder(cmd)
            .AddArguments(args)
            .ExecuteAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAndReadOutputAsync_DirectExecution_ReturnsOutput()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetEchoCommand("builder test");

        // Act
        var output = await new CommandBuilder(cmd)
            .AddArguments(args)
            .ExecuteAndReadOutputAsync();

        // Assert
        output.Should().Contain("builder test");
    }

    #endregion

    #region Configuration

    [Fact]
    public async Task SetTimeout_AppliesTimeout()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetSleepCommand(10);

        // Act
        var act = () => new CommandBuilder(cmd)
            .AddArguments(args)
            .SetTimeout(TimeSpan.FromMilliseconds(100))
            .SetValidation(false) 
            .ExecuteAsync()
            .AsTask();

        // Assert
        await act.Should().ThrowAsync<TimeoutException>();
    }

    [Fact]
    public void SetTimeout_NegativeValue_ThrowsOnBuild()
    {
        // Act
        var act = () => new CommandBuilder("test")
            .SetTimeout(TimeSpan.FromSeconds(-1))
            .Build();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Timeout must be positive*");
    }

    [Fact]
    public async Task SetValidation_False_DoesNotThrowOnNonZeroExit()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetExitCodeCommand(1);

        // Act
        var result = await new CommandBuilder(cmd)
            .AddArguments(args)
            .SetValidation(false)
            .ExecuteAsync();

        // Assert
        result.ExitCode.Should().Be(1);
    }

    #endregion

    #region Pipes

    [Fact]
    public async Task SetStandardOutput_StringBuilder_CapturesOutput()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetEchoCommand("captured");
        var sb = new StringBuilder();

        // Act
        await new CommandBuilder(cmd)
            .AddArguments(args)
            .SetStandardOutput(sb)
            .ExecuteAsync();

        // Assert
        sb.ToString().Should().Contain("captured");
    }

    [Fact]
    public async Task SetStandardOutput_Delegate_CallsHandler()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetEchoCommand("delegated");
        var lines = new List<string>();

        // Act
        await new CommandBuilder(cmd)
            .AddArguments(args)
            .SetStandardOutput(line => lines.Add(line))
            .ExecuteAsync();

        // Assert
        lines.Should().ContainSingle(l => l.Contains("delegated"));
    }

    [Fact]
    public async Task MergeStandardOutputAndError_CapturesBoth()
    {
        // Arrange
        var sb = new StringBuilder();
        string cmd;
        string[] args;

        if (TestHelper.IsWindows)
        {
            cmd = "cmd";
            args = ["/c", "echo out && echo err 1>&2"];
        }
        else
        {
            cmd = "sh";
            args = ["-c", "echo out && echo err >&2"];
        }

        // Act
        await new CommandBuilder(cmd)
            .AddArguments(args)
            .MergeStandardOutputAndError(PipeTarget.ToStringBuilder(sb))
            .ExecuteAsync();

        // Assert
        var output = sb.ToString();
        output.Should().Contain("out");
        output.Should().Contain("err");
    }

    #endregion
}
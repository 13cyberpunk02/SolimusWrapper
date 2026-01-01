using FluentAssertions;
using SolimusWrapper.Core;
using SolimusWrapper.Tests.Fixtures;

namespace SolimusWrapper.Tests;

public class SafeCommandTests
{
    [Fact]
    public void Run_ValidPath_CreatesCommand()
    {
        // Act
        var command = SafeCommand.Run("dotnet");

        // Assert
        command.Should().NotBeNull();
    }

    [Fact]
    public void Run_EmptyPath_ThrowsArgumentException()
    {
        // Act
        var act = () => SafeCommand.Run("");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Run_WhitespacePath_ThrowsArgumentException()
    {
        // Act
        var act = () => SafeCommand.Run("   ");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Run_PathTraversal_ThrowsArgumentException()
    {
        // Act
        var act = () => SafeCommand.Run("../../../etc/passwd");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*'..'*");
    }

    [Fact]
    public void Run_InjectionInPath_ThrowsArgumentException()
    {
        // Act
        var act = () => SafeCommand.Run("cmd; rm -rf /");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*dangerous*");
    }

    [Fact]
    public void RunWithSafeArgs_ValidArgs_CreatesCommand()
    {
        // Act
        var command = SafeCommand.RunWithSafeArgs("echo", "hello", "world");

        // Assert
        command.Should().NotBeNull();
    }

    [Fact]
    public void RunWithSafeArgs_InjectionInArgs_ThrowsArgumentException()
    {
        // Act
        var act = () => SafeCommand.RunWithSafeArgs("echo", "hello; rm -rf /");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*dangerous*");
    }

    [Fact]
    public void Shell_ValidCommand_CreatesCommand()
    {
        // Act
        var command = SafeCommand.Shell("echo hello");

        // Assert
        command.Should().NotBeNull();
    }

    [Fact]
    public void Shell_InjectionPattern_ThrowsArgumentException()
    {
        // Act
        var act = () => SafeCommand.Shell("echo hello && rm -rf /");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*dangerous*");
    }

    [Fact]
    public async Task RunWithSafeArgs_ExecutesSuccessfully()
    {
        // Arrange
        var (cmd, _) = TestHelper.GetEchoCommand("safe");
        
        // Act
        var result = await SafeCommand.RunWithSafeArgs("dotnet", "--version")
            .ExecuteAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
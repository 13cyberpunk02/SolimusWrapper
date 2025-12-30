using FluentAssertions;
using SolimusWrapper.Core;

namespace SolimusWrapper.Tests;

public class CommandResultTests
{
    [Fact]
    public void IsSuccess_ExitCodeZero_ReturnsTrue()
    {
        // Arrange
        var result = new CommandResult(0, DateTimeOffset.Now, DateTimeOffset.Now);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(255)]
    public void IsSuccess_NonZeroExitCode_ReturnsFalse(int exitCode)
    {
        // Arrange
        var result = new CommandResult(exitCode, DateTimeOffset.Now, DateTimeOffset.Now);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void RunTime_CalculatesCorrectDuration()
    {
        // Arrange
        var start = DateTimeOffset.Now;
        var end = start.AddSeconds(5);
        var result = new CommandResult(0, start, end);

        // Assert
        result.RunTime.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void EnsureSuccess_ExitCodeZero_DoesNotThrow()
    {
        // Arrange
        var result = new CommandResult(0, DateTimeOffset.Now, DateTimeOffset.Now);

        // Act
        var act = () => result.EnsureSuccess();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureSuccess_NonZeroExitCode_ThrowsException()
    {
        // Arrange
        var result = new CommandResult(42, DateTimeOffset.Now, DateTimeOffset.Now);

        // Act
        var act = () => result.EnsureSuccess();

        // Assert
        act.Should().Throw<CommandExecutionException>()
            .Where(e => e.ExitCode == 42);
    }
}
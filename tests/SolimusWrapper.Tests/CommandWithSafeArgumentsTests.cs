using FluentAssertions;
using SolimusWrapper.Core;
using SolimusWrapper.Tests.Fixtures;

namespace SolimusWrapper.Tests;

public class CommandWithSafeArgumentsTests
{
    [Fact]
    public async Task WithSafeArguments_SimpleArgs_ExecutesSuccessfully()
    {
        // Arrange & Act
        var result = await Command.Run("dotnet")
            .WithSafeArguments("--version")
            .ExecuteAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task WithSafeArguments_ArgsWithSpaces_ExecutesSuccessfully()
    {
        // Arrange
        var (cmd, _) = TestHelper.GetEchoCommand("test");
        
        // Act
        var output = await Command.Run("dotnet")
            .WithSafeArguments("--info")
            .ExecuteAndReadOutputAsync();

        // Assert
        output.Should().NotBeEmpty();
    }

    [Fact]
    public void WithSafeArguments_ReturnsNewInstance()
    {
        // Arrange
        var original = Command.Run("test");

        // Act
        var withSafeArgs = original.WithSafeArguments("arg1", "arg2");

        // Assert
        withSafeArgs.Should().NotBeSameAs(original);
    }

    [Fact]
    public void WithSafeArguments_Collection_Works()
    {
        // Arrange
        var args = new List<string> { "arg1", "arg2", "arg3" };

        // Act
        var command = Command.Run("test")
            .WithSafeArguments(args);

        // Assert
        command.Should().NotBeNull();
        command.ToString().Should().Contain("arg1");
    }
}
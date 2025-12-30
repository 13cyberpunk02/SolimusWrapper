using System.Text;
using FluentAssertions;
using SolimusWrapper.Core;
using SolimusWrapper.Tests.Fixtures;

namespace SolimusWrapper.Tests;

public class PipeSourceTests
{
    [SkippableFact]
    public async Task FromString_SendsInputToProcess()
    {
        Skip.If(TestHelper.IsWindows, "cat command not available on Windows");

        // Arrange
        var input = "hello from stdin";

        // Act
        var output = await Command.Run("cat")
            .WithStandardInputPipe(PipeSource.FromString(input))
            .ExecuteAndReadOutputAsync();

        // Assert
        output.Should().Be(input);
    }

    [SkippableFact]
    public async Task FromString_MultilineInput_SendsAllLines()
    {
        Skip.If(TestHelper.IsWindows, "cat command not available on Windows");

        // Arrange
        var input = "line1\nline2\nline3";

        // Act
        var output = await Command.Run("cat")
            .WithStandardInputPipe(PipeSource.FromString(input))
            .ExecuteAndReadOutputAsync();

        // Assert
        output.Should().Contain("line1");
        output.Should().Contain("line2");
        output.Should().Contain("line3");
    }

    [SkippableFact]
    public async Task FromStream_SendsStreamContents()
    {
        Skip.If(TestHelper.IsWindows, "cat command not available on Windows");

        // Arrange
        var content = "stream content";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var output = await Command.Run("cat")
            .WithStandardInputPipe(PipeSource.FromStream(ms))
            .ExecuteAndReadOutputAsync();

        // Assert
        output.Should().Be(content);
    }

    [SkippableFact]
    public async Task FromFile_SendsFileContents()
    {
        Skip.If(TestHelper.IsWindows, "cat command not available on Windows");

        // Arrange
        var content = "file content for stdin";
        var tempFile = TestHelper.CreateTempFile(content);

        try
        {
            // Act
            var output = await Command.Run("cat")
                .WithStandardInputPipe(PipeSource.FromFile(tempFile))
                .ExecuteAndReadOutputAsync();

            // Assert
            output.Should().Be(content);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [SkippableFact]
    public async Task FromBytes_SendsBytesToProcess()
    {
        Skip.If(TestHelper.IsWindows, "cat command not available on Windows");

        // Arrange
        var bytes = Encoding.UTF8.GetBytes("bytes input");

        // Act
        var output = await Command.Run("cat")
            .WithStandardInputPipe(PipeSource.FromBytes(bytes))
            .ExecuteAndReadOutputAsync();

        // Assert
        output.Should().Be("bytes input");
    }

    [Fact]
    public async Task Grep_WithStdin_FiltersInput()
    {
        // Arrange
        string cmd;
        string[] args;
        var input = "apple\nbanana\napricot\ncherry";

        if (TestHelper.IsWindows)
        {
            cmd = "findstr";
            args = ["ap"];
        }
        else
        {
            cmd = "grep";
            args = ["ap"];
        }

        // Act
        var output = await Command.Run(cmd)
            .WithArguments(args)
            .WithStandardInputPipe(PipeSource.FromString(input))
            .ExecuteAndReadOutputAsync();

        // Assert
        output.Should().Contain("apple");
        output.Should().Contain("apricot");
        output.Should().NotContain("banana");
        output.Should().NotContain("cherry");
    }
}
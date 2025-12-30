using System.Text;
using FluentAssertions;
using SolimusWrapper.Core;
using SolimusWrapper.Tests.Fixtures;

namespace SolimusWrapper.Tests;

public class PipeTargetTests
{
    [Fact]
    public async Task ToStringBuilder_CapturesOutput()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetEchoCommand("pipe test");
        var sb = new StringBuilder();

        // Act
        await Command.Run(cmd)
            .WithArguments(args)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(sb))
            .ExecuteAsync();

        // Assert
        sb.ToString().Should().Contain("pipe test");
    }

    [Fact]
    public async Task ToDelegate_CallsHandlerForEachLine()
    {
        // Arrange
        string cmd;
        string[] args;
        
        if (TestHelper.IsWindows)
        {
            cmd = "cmd";
            args = ["/c", "echo line1 && echo line2 && echo line3"];
        }
        else
        {
            cmd = "sh";
            args = ["-c", "echo line1; echo line2; echo line3"];
        }

        var lines = new List<string>();

        // Act
        await Command.Run(cmd)
            .WithArguments(args)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(line => lines.Add(line)))
            .ExecuteAsync();

        // Assert
        lines.Should().HaveCountGreaterThanOrEqualTo(3);
        lines.Should().Contain(l => l.Contains("line1"));
        lines.Should().Contain(l => l.Contains("line2"));
        lines.Should().Contain(l => l.Contains("line3"));
    }

    [Fact]
    public async Task ToFile_WritesOutputToFile()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetEchoCommand("file output");
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            await Command.Run(cmd)
                .WithArguments(args)
                .WithStandardOutputPipe(PipeTarget.ToFile(tempFile))
                .ExecuteAsync();

            // Assert
            var content = await File.ReadAllTextAsync(tempFile);
            content.Should().Contain("file output");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ToStream_WritesToStream()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetEchoCommand("stream output");
        using var ms = new MemoryStream();

        // Act
        await Command.Run(cmd)
            .WithArguments(args)
            .WithStandardOutputPipe(PipeTarget.ToStream(ms))
            .ExecuteAsync();

        // Assert
        ms.Position = 0;
        using var reader = new StreamReader(ms);
        var content = await reader.ReadToEndAsync();
        content.Should().Contain("stream output");
    }

    [Fact]
    public async Task Null_DiscardsOutput()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetEchoCommand("discarded");

        // Act & Assert (should not throw)
        var result = await Command.Run(cmd)
            .WithArguments(args)
            .WithStandardOutputPipe(PipeTarget.Null)
            .ExecuteAsync();

        result.IsSuccess.Should().BeTrue();
    }
}
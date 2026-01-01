using FluentAssertions;
using SolimusWrapper.Core;

namespace SolimusWrapper.Tests;

public class ShellEscaperTests
{
    #region Escape Tests

    [Fact]
    public void Escape_EmptyString_ReturnsEmptyQuoted()
    {
        // Act
        var result = ShellEscaper.Escape("");

        // Assert
        result.Should().BeOneOf("\"\"", "''");
    }

    [Fact]
    public void Escape_SimpleString_ReturnsUnchanged()
    {
        // Act
        var result = ShellEscaper.Escape("simple");

        // Assert
        result.Should().Be("simple");
    }

    [Fact]
    public void Escape_StringWithSpaces_ReturnsQuoted()
    {
        // Act
        var result = ShellEscaper.Escape("hello world");

        // Assert
        result.Should().Contain("hello world");
        (result.StartsWith("\"") || result.StartsWith("'")).Should().BeTrue();
    }

    [Fact]
    public void Escape_StringWithSpecialChars_ReturnsEscaped()
    {
        // Act
        var result = ShellEscaper.Escape("file;rm -rf /");

        // Assert
        result.Should().NotBe("file;rm -rf /");
        (result.StartsWith("\"") || result.StartsWith("'")).Should().BeTrue();
    }

    [Theory]
    [InlineData("normal")]
    [InlineData("file.txt")]
    [InlineData("path/to/file")]
    [InlineData("123")]
    public void Escape_SafeStrings_ReturnsUnchanged(string input)
    {
        // Act
        var result = ShellEscaper.Escape(input);

        // Assert
        result.Should().Be(input);
    }

    #endregion

    #region EscapeWindows Tests

    [Fact]
    public void EscapeWindows_StringWithQuotes_EscapesQuotes()
    {
        // Act
        var result = ShellEscaper.EscapeWindows("say \"hello\"");

        // Assert
        result.Should().Contain("\\\"");
    }

    [Fact]
    public void EscapeWindows_StringWithPercent_EscapesPercent()
    {
        // Act
        var result = ShellEscaper.EscapeWindows("100%done");

        // Assert
        result.Should().Contain("%%");
    }

    [Fact]
    public void EscapeWindows_EmptyString_ReturnsEmptyQuoted()
    {
        // Act
        var result = ShellEscaper.EscapeWindows("");

        // Assert
        result.Should().Be("\"\"");
    }

    #endregion

    #region EscapeUnix Tests

    [Fact]
    public void EscapeUnix_StringWithSingleQuote_EscapesCorrectly()
    {
        // Act
        var result = ShellEscaper.EscapeUnix("it's a test");

        // Assert
        result.Should().Contain("'\\''");
    }

    [Fact]
    public void EscapeUnix_StringWithDollar_EscapesCorrectly()
    {
        // Act
        var result = ShellEscaper.EscapeUnix("$HOME");

        // Assert
        result.Should().StartWith("'");
        result.Should().EndWith("'");
    }

    [Fact]
    public void EscapeUnix_EmptyString_ReturnsEmptyQuoted()
    {
        // Act
        var result = ShellEscaper.EscapeUnix("");

        // Assert
        result.Should().Be("''");
    }

    #endregion

    #region EscapePowerShell Tests

    [Fact]
    public void EscapePowerShell_StringWithSingleQuote_DoublesQuote()
    {
        // Act
        var result = ShellEscaper.EscapePowerShell("it's test");

        // Assert
        result.Should().Contain("''");
    }

    [Fact]
    public void EscapePowerShell_SimpleString_ReturnsUnchanged()
    {
        // Act
        var result = ShellEscaper.EscapePowerShell("simple");

        // Assert
        result.Should().Be("simple");
    }

    #endregion

    #region ContainsDangerousCharacters Tests

    [Theory]
    [InlineData("normal", false)]
    [InlineData("hello world", true)]
    [InlineData("cmd;ls", true)]
    [InlineData("a&&b", true)]
    [InlineData("a|b", true)]
    [InlineData("$(cmd)", true)]
    [InlineData("`cmd`", true)]
    [InlineData("file.txt", false)]
    public void ContainsDangerousCharacters_ReturnsExpected(string input, bool expected)
    {
        // Act
        var result = ShellEscaper.ContainsDangerousCharacters(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ContainsDangerousCharacters_EmptyString_ReturnsFalse()
    {
        // Act
        var result = ShellEscaper.ContainsDangerousCharacters("");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ContainsDangerousCharacters_NullString_ReturnsFalse()
    {
        // Act
        var result = ShellEscaper.ContainsDangerousCharacters(null!);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region LooksLikeInjection Tests

    [Theory]
    [InlineData("normal", false)]
    [InlineData("file;rm -rf /", true)]
    [InlineData("a && b", true)]
    [InlineData("a || b", true)]
    [InlineData("$(whoami)", true)]
    [InlineData("`id`", true)]
    [InlineData("cat > file", true)]
    [InlineData("cat < file", true)]
    [InlineData("cmd 2>&1", true)]
    public void LooksLikeInjection_ReturnsExpected(string input, bool expected)
    {
        // Act
        var result = ShellEscaper.LooksLikeInjection(input);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region BuildCommandLine Tests

    [Fact]
    public void BuildCommandLine_SimpleCommand_ReturnsCorrect()
    {
        // Act
        var result = ShellEscaper.BuildCommandLine("echo", "hello", "world");

        // Assert
        result.Should().Contain("echo");
        result.Should().Contain("hello");
        result.Should().Contain("world");
    }

    [Fact]
    public void BuildCommandLine_WithSpaces_EscapesArguments()
    {
        // Act
        var result = ShellEscaper.BuildCommandLine("echo", "hello world");

        // Assert
        result.Should().Contain("echo");
        (result.Contains("\"hello world\"") || result.Contains("'hello world'")).Should().BeTrue();
    }

    #endregion

    #region Escape Collection Tests

    [Fact]
    public void Escape_Collection_EscapesAll()
    {
        // Arrange
        var args = new[] { "simple", "with space", "special;char" };

        // Act
        var result = ShellEscaper.Escape(args).ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].Should().Be("simple");
        
        result[1].Should().NotBe("with space"); 
        result[2].Should().NotBe("special;char");

        result[1].Should().Contain("with space");
        result[2].Should().Contain("special;char");
    }

    #endregion
}
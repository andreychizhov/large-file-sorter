using System.Buffers;
using FluentAssertions;

namespace Sorter.Tests;

public class LineParserTests
{
    [Fact]
    public void Parse_ValidLine_ReturnsCorrectNumberAndText()
    {
        // Arrange
        ReadOnlySpan<char> line = "123. Zebra shines yellow";
        var expectedNumber = 123;
        var expectedText = "Zebra shines yellow";

        // Act
        var result = LineParser.Parse(line);

        // Assert
        result.Number.Should().Be(expectedNumber);
        result.Text.Span.ToString().Should().Be(expectedText);

        // Cleanup
        ArrayPool<char>.Shared.Return(result.Buffer);
    }

    [Fact]
    public void Parse_DuplicateNumber_ReturnsCorrectNumberAndText()
    {
        // Arrange
        ReadOnlySpan<char> line = "42. Apple is sweet";
        var expectedNumber = 42;
        var expectedText = "Apple is sweet";

        // Act
        var result = LineParser.Parse(line);

        // Assert
        result.Number.Should().Be(expectedNumber);
        result.Text.Span.ToString().Should().Be(expectedText);

        // Cleanup
        ArrayPool<char>.Shared.Return(result.Buffer);
    }

    [Fact]
    public void Parse_EmptyText_ReturnsCorrectNumberAndText()
    {
        // Act
        var result = LineParser.Parse("1. ");

        // Assert
        result.Number.Should().Be(1);
        result.Text.Span.ToString().Should().Be("");
    }

    [Fact]
    public void Parse_MissingSeparator_ThrowsFormatException()
    {
        // Act
        Action act = () => LineParser.Parse("1 Apple shines yellow");

        // Assert
        act.Should().Throw<FormatException>()
           .WithMessage("Invalid line format.");
    }

    [Fact]
    public void Parse_InvalidNumber_ThrowsFormatException()
    {
        // Act
        Action act = () => LineParser.Parse("abc. Apple shines yellow");

        // Assert
        act.Should().Throw<FormatException>()
           .WithMessage("Invalid number format.");
    }

    [Fact]
    public void Parse_MaxInt_ReturnsCorrectNumberAndText()
    {
        // Arrange
        ReadOnlySpan<char> line = "2147483647. Apple shines yellow";
        var expectedNumber = 2147483647;
        var expectedText = "Apple shines yellow";

        // Act
        var result = LineParser.Parse(line);

        // Assert
        result.Number.Should().Be(expectedNumber);
        result.Text.Span.ToString().Should().Be(expectedText);

        // Cleanup
        ArrayPool<char>.Shared.Return(result.Buffer);
    }

    [Fact]
    public void Parse_MinInt_ReturnsCorrectNumberAndText()
    {
        // Arrange
        ReadOnlySpan<char> line = "-2147483648. Apple shines yellow";
        var expectedNumber = -2147483648;
        var expectedText = "Apple shines yellow";

        // Act
        var result = LineParser.Parse(line);

        // Assert
        result.Number.Should().Be(expectedNumber);
        result.Text.Span.ToString().Should().Be(expectedText);

        // Cleanup
        ArrayPool<char>.Shared.Return(result.Buffer);
    }

    [Fact]
    public void Parse_LongText_ReturnsCorrectNumberAndText()
    {
        // Arrange
        var longText = new string('A', 998) + " shines yellow";
        ReadOnlySpan<char> line = $"1. {longText}";
        var expectedNumber = 1;
        var expectedText = longText;

        // Act
        var result = LineParser.Parse(line);

        // Assert
        result.Number.Should().Be(expectedNumber);
        result.Text.Span.ToString().Should().Be(expectedText);

        // Cleanup
        ArrayPool<char>.Shared.Return(result.Buffer);
    }

    [Fact]
    public void Parse_MixedCaseText_ReturnsCorrectNumberAndText()
    {
        // Arrange
        ReadOnlySpan<char> line = "1. ApPlE shines YeLLoW";
        var expectedNumber = 1;
        var expectedText = "ApPlE shines YeLLoW";

        // Act
        var result = LineParser.Parse(line);

        // Assert
        result.Number.Should().Be(expectedNumber);
        result.Text.Span.ToString().Should().Be(expectedText);

        // Cleanup
        ArrayPool<char>.Shared.Return(result.Buffer);
    }

    [Fact]
    public void Parse_OnlyDot_ReturnsCorrectNumberAndText()
    {
        // Arrange
        ReadOnlySpan<char> line = "1. .";
        var expectedNumber = 1;
        var expectedText = ".";

        // Act
        var result = LineParser.Parse(line);

        // Assert
        result.Number.Should().Be(expectedNumber);
        result.Text.Span.ToString().Should().Be(expectedText);

        // Cleanup
        ArrayPool<char>.Shared.Return(result.Buffer);
    }
}
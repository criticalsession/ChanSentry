using ChanSentry.Cli.Utils;

namespace ChanSentry.Tests.Utils;

[TestFixture]
public class FileNameSanitizerTests
{
    #region Sanitize Tests

    [Test]
    public void Sanitize_WithValidFileName_ReturnsUnchanged()
    {
        // Arrange
        var fileName = "valid_filename_123";

        // Act
        var result = FileNameSanitizer.Sanitize(fileName);

        // Assert
        Assert.That(result, Is.EqualTo("valid_filename_123"));
    }

    [Test]
    public void Sanitize_WithInvalidCharacters_ReplacesWithUnderscore()
    {
        // Arrange
        var fileName = "file<name>:with|invalid?chars*";

        // Act
        var result = FileNameSanitizer.Sanitize(fileName);

        // Assert
        Assert.That(result, Does.Not.Contain("<"));
        Assert.That(result, Does.Not.Contain(">"));
        Assert.That(result, Does.Not.Contain(":"));
        Assert.That(result, Does.Not.Contain("|"));
        Assert.That(result, Does.Not.Contain("?"));
        Assert.That(result, Does.Not.Contain("*"));
        Assert.That(result, Does.Contain("_"));
    }

    [Test]
    public void Sanitize_WithSpectreMarkupBrackets_ReplacesWithUnderscore()
    {
        // Arrange
        var fileName = "file[bold]name[/bold]";

        // Act
        var result = FileNameSanitizer.Sanitize(fileName);

        // Assert
        Assert.That(result, Does.Not.Contain("["));
        Assert.That(result, Does.Not.Contain("]"));
        Assert.That(result, Is.EqualTo("file_bold_name__bold_"));
    }

    [Test]
    public void Sanitize_WithNullInput_ReturnsEmptyString()
    {
        // Arrange
        string? fileName = null;

        // Act
        var result = FileNameSanitizer.Sanitize(fileName);

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void Sanitize_WithEmptyString_ReturnsEmptyString()
    {
        // Arrange
        var fileName = "";

        // Act
        var result = FileNameSanitizer.Sanitize(fileName);

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void Sanitize_WithWhitespaceOnly_ReturnsEmptyString()
    {
        // Arrange
        var fileName = "   ";

        // Act
        var result = FileNameSanitizer.Sanitize(fileName);

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void Sanitize_WithLeadingTrailingWhitespace_TrimsWhitespace()
    {
        // Arrange
        var fileName = "  valid_filename  ";

        // Act
        var result = FileNameSanitizer.Sanitize(fileName);

        // Assert
        Assert.That(result, Is.EqualTo("valid_filename"));
    }

    [Test]
    public void Sanitize_WithLongFileName_TruncatesTo200Characters()
    {
        // Arrange
        var fileName = new string('a', 300);

        // Act
        var result = FileNameSanitizer.Sanitize(fileName);

        // Assert
        Assert.That(result.Length, Is.EqualTo(200));
    }

    [Test]
    public void Sanitize_WithExactly200Characters_RemainsUnchanged()
    {
        // Arrange
        var fileName = new string('a', 200);

        // Act
        var result = FileNameSanitizer.Sanitize(fileName);

        // Assert
        Assert.That(result.Length, Is.EqualTo(200));
    }

    [Test]
    public void Sanitize_WithBackslashAndForwardSlash_ReplacesWithUnderscore()
    {
        // Arrange
        var fileName = "path/to\\file";

        // Act
        var result = FileNameSanitizer.Sanitize(fileName);

        // Assert
        Assert.That(result, Does.Not.Contain("/"));
        Assert.That(result, Does.Not.Contain("\\"));
        Assert.That(result, Does.Contain("_"));
    }

    [Test]
    public void Sanitize_WithQuotes_ReplacesWithUnderscore()
    {
        // Arrange
        var fileName = "file\"name'with\"quotes";

        // Act
        var result = FileNameSanitizer.Sanitize(fileName);

        // Assert
        Assert.That(result, Does.Not.Contain("\""));
    }

    [Test]
    public void Sanitize_WithMultipleConsecutiveInvalidChars_ReplacesAllWithUnderscores()
    {
        // Arrange
        var fileName = "file<>:|name";

        // Act
        var result = FileNameSanitizer.Sanitize(fileName);

        // Assert
        Assert.That(result, Does.StartWith("file____"));
    }

    #endregion

    #region EscapeMarkup Tests

    [Test]
    public void EscapeMarkup_WithNoMarkup_ReturnsUnchanged()
    {
        // Arrange
        var text = "plain text without markup";

        // Act
        var result = FileNameSanitizer.EscapeMarkup(text);

        // Assert
        Assert.That(result, Is.EqualTo("plain text without markup"));
    }

    [Test]
    public void EscapeMarkup_WithBrackets_EscapesBrackets()
    {
        // Arrange
        var text = "text [bold]with[/bold] markup";

        // Act
        var result = FileNameSanitizer.EscapeMarkup(text);

        // Assert
        Assert.That(result, Is.EqualTo("text [[bold]]with[[/bold]] markup"));
    }

    [Test]
    public void EscapeMarkup_WithOnlyOpenBracket_EscapesOpenBracket()
    {
        // Arrange
        var text = "text with [ bracket";

        // Act
        var result = FileNameSanitizer.EscapeMarkup(text);

        // Assert
        Assert.That(result, Is.EqualTo("text with [[ bracket"));
    }

    [Test]
    public void EscapeMarkup_WithOnlyCloseBracket_EscapesCloseBracket()
    {
        // Arrange
        var text = "text with ] bracket";

        // Act
        var result = FileNameSanitizer.EscapeMarkup(text);

        // Assert
        Assert.That(result, Is.EqualTo("text with ]] bracket"));
    }

    [Test]
    public void EscapeMarkup_WithNullInput_ReturnsEmptyString()
    {
        // Arrange
        string? text = null;

        // Act
        var result = FileNameSanitizer.EscapeMarkup(text);

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void EscapeMarkup_WithEmptyString_ReturnsEmptyString()
    {
        // Arrange
        var text = "";

        // Act
        var result = FileNameSanitizer.EscapeMarkup(text);

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void EscapeMarkup_WithMultipleBrackets_EscapesAll()
    {
        // Arrange
        var text = "[red]Error[/red] [yellow]Warning[/yellow]";

        // Act
        var result = FileNameSanitizer.EscapeMarkup(text);

        // Assert
        Assert.That(result, Is.EqualTo("[[red]]Error[[/red]] [[yellow]]Warning[[/yellow]]"));
    }

    [Test]
    public void EscapeMarkup_WithNestedBrackets_EscapesAll()
    {
        // Arrange
        var text = "[[already escaped]]";

        // Act
        var result = FileNameSanitizer.EscapeMarkup(text);

        // Assert
        Assert.That(result, Is.EqualTo("[[[[already escaped]]]]"));
    }

    #endregion

    #region Integration Tests

    [Test]
    public void Sanitize_WithRealWorldFileName_HandlesCorrectly()
    {
        // Arrange - Example from 4chan: filenames can have various characters
        var fileName = "sticky btfo [WINNING]";

        // Act
        var result = FileNameSanitizer.Sanitize(fileName);

        // Assert
        Assert.That(result, Does.Not.Contain("["));
        Assert.That(result, Does.Not.Contain("]"));
        Assert.That(result, Is.EqualTo("sticky btfo _WINNING_"));
    }

    [Test]
    public void Sanitize_AndEscapeMarkup_WorkTogether()
    {
        // Arrange
        var fileName = "file<with>invalid[chars]";

        // Act
        var sanitized = FileNameSanitizer.Sanitize(fileName);
        var escaped = FileNameSanitizer.EscapeMarkup(sanitized);

        // Assert
        Assert.That(sanitized, Does.Not.Contain("<"));
        Assert.That(sanitized, Does.Not.Contain(">"));
        Assert.That(sanitized, Does.Not.Contain("["));
        Assert.That(sanitized, Does.Not.Contain("]"));
        // After sanitization, no brackets remain, so escaping doesn't change it
        Assert.That(escaped, Is.EqualTo(sanitized));
    }

    [Test]
    public void Sanitize_WithCommonUnicodeCharacters_HandlesCorrectly()
    {
        // Arrange - Common Unicode characters that are typically valid in filenames
        var fileName = "test_ñ_ü_é_file";

        // Act
        var result = FileNameSanitizer.Sanitize(fileName);

        // Assert - These characters should be preserved on most systems
        Assert.That(result, Is.EqualTo("test_ñ_ü_é_file"));
    }

    [Test]
    public void Sanitize_WithWindowsReservedNames_PreservesInput()
    {
        // Arrange - Windows reserved names (CON, PRN, AUX, etc.)
        // Note: This test verifies the sanitizer doesn't specifically handle these,
        // as they're already invalid at the OS level
        var fileName = "CON_test";

        // Act
        var result = FileNameSanitizer.Sanitize(fileName);

        // Assert - The sanitizer focuses on character-level validation
        Assert.That(result, Is.EqualTo("CON_test"));
    }

    #endregion
}

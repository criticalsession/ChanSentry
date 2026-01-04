using System.Text;

namespace ChanSentry.Cli.Utils;

/// <summary>
/// Provides methods for sanitizing file names to ensure they are valid for the operating system
/// and safe for use with Spectre.Console.
/// </summary>
public static class FileNameSanitizer
{
    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();
    private static readonly char[] AdditionalInvalidChars = { '"', '\'', '[', ']' };
    
    /// <summary>
    /// Sanitizes a filename by removing or replacing invalid characters for the operating system
    /// and escaping special characters used by Spectre.Console markup.
    /// </summary>
    /// <param name="fileName">The filename to sanitize.</param>
    /// <returns>A sanitized filename safe for filesystem operations and Spectre.Console display.</returns>
    public static string Sanitize(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return string.Empty;
        }

        var sanitized = new StringBuilder(fileName.Length);

        foreach (var c in fileName)
        {
            if (InvalidFileNameChars.Contains(c) || AdditionalInvalidChars.Contains(c))
            {
                sanitized.Append('_');
            }
            else
            {
                sanitized.Append(c);
            }
        }

        var result = sanitized.ToString().Trim();
        
        if (string.IsNullOrWhiteSpace(result))
        {
            return string.Empty;
        }

        if (result.Length > 200)
        {
            result = result.Substring(0, 200);
        }

        return result;
    }

    /// <summary>
    /// Escapes special Spectre.Console markup characters in a string for safe display.
    /// </summary>
    /// <param name="text">The text to escape.</param>
    /// <returns>A string with Spectre.Console markup characters escaped.</returns>
    public static string EscapeMarkup(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return text.Replace("[", "[[").Replace("]", "]]");
    }
}

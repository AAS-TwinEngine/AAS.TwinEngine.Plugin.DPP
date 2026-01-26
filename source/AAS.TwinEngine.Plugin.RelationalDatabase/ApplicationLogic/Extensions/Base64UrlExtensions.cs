using System.Text;

using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;

using Microsoft.AspNetCore.WebUtilities;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Extensions;

public static class Base64UrlExtensions
{
    private const int MaxBase64UrlLength = 256;

    /// <summary>
    /// Decodes a Base64 URL encoded string to its original UTF-8 representation.
    /// </summary>
    /// <param name="encoded">The Base64 URL encoded string.</param>
    /// <param name="logger"></param>
    /// <returns>The decoded UTF-8 string, or empty if input is null or whitespace.</returns>
    /// <exception cref="InvalidUserInputException">
    /// Thrown when the input is invalid, too large, or cannot be decoded.
    /// </exception>
    public static string DecodeBase64(this string encoded, ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(encoded))
        {
            logger?.LogError("Identifier cannot be null or empty.");
            throw new InvalidUserInputException();
        }

        if (encoded.Length > MaxBase64UrlLength)
        {
            logger?.LogError("Base64 URL input exceeds maximum allowed length ({MaxLength}). Actual length: {ActualLength}", MaxBase64UrlLength, encoded.Length);
            throw new InvalidUserInputException();
        }

        try
        {
            var bytes = WebEncoders.Base64UrlDecode(encoded);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to decode Base64 URL string: {Encoded}", encoded);
            throw new InvalidUserInputException();
        }
    }

    /// <summary>
    /// Encodes a UTF-8 string to Base64 URL format.
    /// </summary>
    /// <param name="plainText">The plain UTF-8 string to encode.</param>
    /// <param name="logger"></param>
    /// <returns>The Base64 URL encoded string, or empty if input is null or whitespace.</returns>
    /// <exception cref="InternalDataProcessingException">Thrown when the string cannot be encoded.</exception>
    public static string EncodeBase64(this string plainText, ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(plainText))
        {
            return string.Empty;
        }

        try
        {
            var bytes = Encoding.UTF8.GetBytes(plainText);
            return WebEncoders.Base64UrlEncode(bytes);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to encode string to Base64 URL format: {PlainText}", plainText);
            throw new InvalidUserInputException();
        }
    }
}

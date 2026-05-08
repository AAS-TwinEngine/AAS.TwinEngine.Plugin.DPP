using System.Text;

using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Extensions;

using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.UnitTests.ApplicationLogic.Extensions;

public class Base64UrlExtensionsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DecodeBase64_ShouldThrow_OnNullOrWhitespace(string? input)
    {
        var logger = Substitute.For<ILogger>();

        string act() => input!.DecodeBase64(logger);

        Assert.Throws<InvalidUserInputException>((Func<string>)act);
        AssertLogErrorCalled(logger, expectedMessageContains: "Identifier cannot be null or empty.");
    }

    [Fact]
    public void DecodeBase64_ShouldReturnDecodedText_OnValidBase64Url()
    {
        const string Plain = "Hello-World_2025!";
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(Plain));
        var logger = Substitute.For<ILogger>();

        var result = encoded.DecodeBase64(logger);

        Assert.Equal(Plain, result);
        AssertLogErrorNotCalled(logger);
    }

    [Theory]
    [InlineData("%%%invalid%%%")]
    [InlineData("====")]
    [InlineData("abc$")]
    public void DecodeBase64_ShouldThrowAndLog_OnInvalidBase64Url(string encoded)
    {
        var logger = Substitute.For<ILogger>();

        string Act() => encoded.DecodeBase64(logger);

        Assert.Throws<InvalidUserInputException>((Func<string>)Act);
        AssertLogErrorCalled(logger, expectedMessageContains: "Failed to decode input Base64 URL string");
    }

    [Fact]
    public void DecodeBase64_ShouldThrow_OnExceedingMaxLength()
    {
        var longStringForBase64 = new string('A', 193); 
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(longStringForBase64));
        var logger = Substitute.For<ILogger>();

        string Act() => encoded.DecodeBase64(logger);

        Assert.Throws<InvalidUserInputException>((Func<string>)Act);
        AssertLogErrorCalled(logger, expectedMessageContains: "Base64 URL input exceeds maximum allowed length");
    }
    
    [Fact]
    public void DecodeBase64_ShouldThrow_OnDecodedIdentifierExceedingMaxLength()
    {
        var longString = new string('A', 2049);
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(longString));
        var logger = Substitute.For<ILogger>();

        string Act() => encoded.DecodeBase64(logger);

        Assert.Throws<InvalidUserInputException>((Func<string>)Act);
        AssertLogErrorCalled(logger, expectedMessageContains: "Base64 URL input exceeds maximum allowed length");
    }

    [Fact]
    public void DecodeBase64_ShouldSucceed_AtBase64UrlMaxLength()
    {
        var plainText = new string('A', 192); 
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(plainText));
        var logger = Substitute.For<ILogger>();
        Assert.Equal(256, encoded.Length);

        var result = encoded.DecodeBase64(logger);

        Assert.Equal(plainText, result);
        AssertLogErrorNotCalled(logger);
    }

    [Theory]
    [InlineData(255)] 
    [InlineData(256)] 
    public void DecodeBase64_ShouldSucceed_AtOrBelowBase64UrlMaxLength(int base64Length)
    {
        var plainTextLength = (base64Length * 3) / 4;
        var plainText = new string('A', plainTextLength);
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(plainText));
        var logger = Substitute.For<ILogger>();

        if (encoded.Length > base64Length)
        {
            encoded = encoded[..base64Length];
        }

        var result = encoded.DecodeBase64(logger);

        Assert.NotNull(result);
        AssertLogErrorNotCalled(logger);
    }

    [Theory]
    [InlineData(257)]
    [InlineData(300)]
    [InlineData(512)]
    public void DecodeBase64_ShouldThrow_OverBase64UrlMaxLength(int base64Length)
    {
        var longBase64String = new string('A', base64Length);
        var logger = Substitute.For<ILogger>();

        string Act() => longBase64String.DecodeBase64(logger);

        Assert.Throws<InvalidUserInputException>((Func<string>)Act);
        AssertLogErrorCalled(logger, expectedMessageContains: "Base64 URL input exceeds maximum allowed length");
    }

    [Fact]
    public void DecodeBase64_ShouldValidateLengthBeforeDecoding()
    {
        var longBase64String = new string('A', 300);
        var logger = Substitute.For<ILogger>();

        string Act() => longBase64String.DecodeBase64(logger);

        var exception = Assert.Throws<InvalidUserInputException>((Func<string>)Act);
        Assert.NotNull(exception);
        logger.Received().Log(
                              LogLevel.Error,
                              Arg.Any<EventId>(),
                              Arg.Is<object>(o => ToStringState(o).Contains("Base64 URL input exceeds maximum allowed length", StringComparison.OrdinalIgnoreCase)),
                              Arg.Any<Exception>(),
                              Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void DecodeBase64_ShouldLogCorrectLengths_OnBase64UrlLengthExceeded()
    {
        var longBase64String = new string('A', 300);
        var logger = Substitute.For<ILogger>();

        string Act() => longBase64String.DecodeBase64(logger);

        Assert.Throws<InvalidUserInputException>((Func<string>)Act);
    
        logger.Received().Log(
                              LogLevel.Error,
                              Arg.Any<EventId>(),
                              Arg.Is<object>(o => 
                                                                     ToStringState(o).Contains("256", StringComparison.Ordinal) &&
                                                                     ToStringState(o).Contains("300", StringComparison.Ordinal)),
                              Arg.Any<Exception>(),
                              Arg.Any<Func<object, Exception?, string>>());
    }

    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("'; DROP TABLE Users; --")]
    [InlineData("../../etc/passwd")]
    [InlineData("javascript:alert(1)")]
    [InlineData("SELECT * FROM Users")]
    [InlineData("<img onerror='alert(1)'>")]
    [InlineData("UNION SELECT password FROM users")]
    public void DecodeBase64_ShouldThrow_OnMaliciousPatterns(string maliciousInput)
    {
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(maliciousInput));
        var logger = Substitute.For<ILogger>();

        string Act() => encoded.DecodeBase64(logger);

        Assert.Throws<InvalidUserInputException>((Func<string>)Act);
        AssertLogErrorCalled(logger, expectedMessageContains: "Decoded identifier contains malicious patterns");
    }

    [Fact]
    public void DecodeBase64_ShouldHandleValidComplexIdentifiers()
    {
        var validIdentifiers = new[]
        {
            "https://example.com/semantic/id/12345",
            "urn:company:product:version:1.0",
            "my-valid-identifier_2025",
            "Property123"
        };

        var logger = Substitute.For<ILogger>();

        foreach (var identifier in validIdentifiers)
        {
            var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(identifier));

            var result = encoded.DecodeBase64(logger);

            Assert.Equal(identifier, result);
        }

        AssertLogErrorNotCalled(logger);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EncodeBase64_ShouldReturnEmpty_OnNullOrWhitespace(string? input)
    {
        var logger = Substitute.For<ILogger>();

        var result = input!.EncodeBase64(logger);

        Assert.Equal(string.Empty, result);
        AssertLogErrorNotCalled(logger);
    }

    [Theory]
    [InlineData("Hello", "SGVsbG8")]
    [InlineData("Hello-World_2025!", null)]
    public void EncodeBase64_ShouldReturnEncoded_OnValidText(string plainText, string? expectedPrefix)
    {
        var logger = Substitute.For<ILogger>();

        var encoded = plainText.EncodeBase64(logger);

        Assert.False(string.IsNullOrWhiteSpace(encoded));
        if (!string.IsNullOrEmpty(expectedPrefix))
        {
            Assert.StartsWith(expectedPrefix, encoded, StringComparison.Ordinal);
        }

        var decoded = WebEncoders.Base64UrlDecode(encoded);
        var rt = Encoding.UTF8.GetString(decoded);
        Assert.Equal(plainText, rt);

        AssertLogErrorNotCalled(logger);
    }

    [Fact]
    public void EncodeBase64_ShouldHandleSpecialCharacters()
    {
        var specialTexts = new[]
        {
            "Hello/World",
            "Test+Value",
            "Key=Value",
            "Path\\To\\Resource",
            "User@Domain.com"
        };

        var logger = Substitute.For<ILogger>();

        foreach (var text in specialTexts)
        {
            var encoded = text.EncodeBase64(logger);

            Assert.False(string.IsNullOrWhiteSpace(encoded));

            var decoded = WebEncoders.Base64UrlDecode(encoded);
            var roundTrip = Encoding.UTF8.GetString(decoded);
            Assert.Equal(text, roundTrip);
        }

        AssertLogErrorNotCalled(logger);
    }

    [Fact]
    public void DecodeBase64_ShouldWork_WithoutLogger()
    {
        const string Plain = "TestWithoutLogger";
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(Plain));

        var result = encoded.DecodeBase64();

        Assert.Equal(Plain, result);
    }

    [Fact]
    public void EncodeBase64_ShouldWork_WithoutLogger()
    {
        const string Plain = "TestWithoutLogger";

        var encoded = Plain.EncodeBase64();

        Assert.False(string.IsNullOrWhiteSpace(encoded));
        var decoded = WebEncoders.Base64UrlDecode(encoded);
        var roundTrip = Encoding.UTF8.GetString(decoded);
        Assert.Equal(Plain, roundTrip);
    }

    private static void AssertLogErrorCalled(ILogger logger, string? expectedMessageContains = null)
    {
        logger.ReceivedWithAnyArgs().Log(
                                         Arg.Is<LogLevel>(l => l == LogLevel.Error),
                                         Arg.Any<EventId>(),
                                         Arg.Any<object>(),
                                         Arg.Any<Exception>(),
                                         Arg.Any<Func<object, Exception?, string>>());

        if (!string.IsNullOrWhiteSpace(expectedMessageContains))
        {
            logger.Received().Log(
                                  LogLevel.Error,
                                  Arg.Any<EventId>(),
                                  Arg.Is<object>(o => ToStringState(o).Contains(expectedMessageContains, StringComparison.OrdinalIgnoreCase)),
                                  Arg.Any<Exception>(),
                                  Arg.Any<Func<object, Exception?, string>>());
        }
    }

    private static void AssertLogErrorNotCalled(ILogger logger)
    {
        logger.DidNotReceive().Log(
                                   LogLevel.Error,
                                   Arg.Any<EventId>(),
                                   Arg.Any<object>(),
                                   Arg.Any<Exception>(),
                                   Arg.Any<Func<object, Exception?, string>>());
    }

    private static string ToStringState(object state) => state?.ToString() ?? string.Empty;
}

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

        string act() => Base64UrlExtensions.DecodeBase64(input!, logger);

        Assert.Throws<InvalidUserInputException>((Func<string>)act);
      AssertLogErrorCalled(logger, expectedMessageContains: "Identifier cannot be null or empty.");
    }

    [Fact]
    public void DecodeBase64_ShouldReturnDecodedText_OnValidBase64Url()
    {
        const string Plain = "Hello-World_2025!";
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(Plain));
        var logger = Substitute.For<ILogger>();

        var result = Base64UrlExtensions.DecodeBase64(encoded, logger);

        Assert.Equal(Plain, result);
        AssertLogErrorNotCalled(logger);
    }

    [Theory]
    [InlineData("%%%invalid%%%")]
    [InlineData("====")]
    [InlineData("a b c")]
    public void DecodeBase64_ShouldThrowAndLog_OnInvalidBase64Url(string encoded)
    {
        var logger = Substitute.For<ILogger>();

        Assert.Throws<InvalidUserInputException>((Func<string>)Act);
        AssertLogErrorCalled(logger, expectedMessageContains: "Failed to decode input Base64 URL string");
        return;

        string Act() => Base64UrlExtensions.DecodeBase64(encoded, logger);
    }

    [Fact]
    public void DecodeBase64_ShouldThrow_OnExceedingMaxLength()
    {
        var longString = new string('A', 2049);
        var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(longString));
        var logger = Substitute.For<ILogger>();

        Assert.Throws<InvalidUserInputException>((Func<string>)Act);
        AssertLogErrorCalled(logger, expectedMessageContains: "Decoded identifier exceeds maximum length");
        return;

        string Act() => Base64UrlExtensions.DecodeBase64(encoded, logger);
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

        Assert.Throws<InvalidUserInputException>((Func<string>)Act);
        AssertLogErrorCalled(logger, expectedMessageContains: "Decoded identifier contains malicious patterns");
        return;

        string Act() => Base64UrlExtensions.DecodeBase64(encoded, logger);
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

            var result = Base64UrlExtensions.DecodeBase64(encoded, logger);

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

        var result = Base64UrlExtensions.EncodeBase64(input!, logger);

        Assert.Equal(string.Empty, result);
        AssertLogErrorNotCalled(logger);
    }

    [Theory]
    [InlineData("Hello", "SGVsbG8")]
    [InlineData("Hello-World_2025!", null)]
    public void EncodeBase64_ShouldReturnEncoded_OnValidText(string plainText, string? expectedPrefix)
    {
        var logger = Substitute.For<ILogger>();

        var encoded = Base64UrlExtensions.EncodeBase64(plainText, logger);

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
            var encoded = Base64UrlExtensions.EncodeBase64(text, logger);

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

        var result = Base64UrlExtensions.DecodeBase64(encoded);

        Assert.Equal(Plain, result);
    }

    [Fact]
    public void EncodeBase64_ShouldWork_WithoutLogger()
    {
        const string Plain = "TestWithoutLogger";

        var encoded = Base64UrlExtensions.EncodeBase64(Plain);

        Assert.False(string.IsNullOrWhiteSpace(encoded));
        var decoded = WebEncoders.Base64UrlDecode(encoded);
        var roundTrip = Encoding.UTF8.GetString(decoded);
        Assert.Equal(Plain, roundTrip);
    }

    private static void AssertLogErrorCalled(ILogger logger, string? expectedMessageContains = null)
    {
        logger.ReceivedWithAnyArgs().Log(Arg.Is<LogLevel>(l => l == LogLevel.Error),
                                         Arg.Any<EventId>(),
                                         Arg.Any<object>(),
                                         Arg.Any<Exception>(),
                                         Arg.Any<Func<object, Exception?, string>>());

        if (!string.IsNullOrWhiteSpace(expectedMessageContains))
        {
            logger.Received().Log(LogLevel.Error,
                                  Arg.Any<EventId>(),
                                  Arg.Is<object>(o => ToStringState(o).Contains(expectedMessageContains, StringComparison.OrdinalIgnoreCase)),
                                  Arg.Any<Exception>(),
                                  Arg.Any<Func<object, Exception?, string>>());
        }
    }

    private static void AssertLogErrorNotCalled(ILogger logger)
    {
        logger.DidNotReceive().Log(LogLevel.Error,
                                   Arg.Any<EventId>(),
                                   Arg.Any<object>(),
                                   Arg.Any<Exception>(),
                                   Arg.Any<Func<object, Exception?, string>>());
    }

    private static string ToStringState(object state) => state?.ToString() ?? string.Empty;
}

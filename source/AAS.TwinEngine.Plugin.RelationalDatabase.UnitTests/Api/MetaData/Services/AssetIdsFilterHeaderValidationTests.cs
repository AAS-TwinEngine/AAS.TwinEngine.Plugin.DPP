using System.Text.Json;

using AAS.TwinEngine.Plugin.RelationalDatabase.Api.MetaData.Services;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;

using Microsoft.Extensions.Logging;

using NSubstitute;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.UnitTests.Api.MetaData.Services;

public class AssetIdsFilterHeaderValidationTests
{
    private readonly ILogger<AssetIdsFilterHeaderValidation> _logger = Substitute.For<ILogger<AssetIdsFilterHeaderValidation>>();
    private readonly AssetIdsFilterHeaderValidation _sut;

    public AssetIdsFilterHeaderValidationTests() => _sut = new AssetIdsFilterHeaderValidation(_logger);

    [Fact]
    public void ParseToDomainModel_WhenHeaderIsNull_ReturnsNull()
    {
        var result = _sut.ParseToDomainModel(null);

        Assert.Null(result);
    }

    [Fact]
    public void ParseToDomainModel_WhenHeaderIsWhitespace_ReturnsNull()
    {
        var result = _sut.ParseToDomainModel("   ");

        Assert.Null(result);
    }

    [Fact]
    public void ParseToDomainModel_WhenValidSpecificAssetIdsHeader_ReturnsFilter()
    {
        var header = "[{\"name\":\"serialNumber\",\"value\":\"SN-4711\"}]";

        var result = _sut.ParseToDomainModel(header);

        Assert.NotNull(result);
        var identifier = Assert.Single(result!.Identifiers);
        Assert.Equal("serialNumber", identifier.Name);
        Assert.Equal("SN-4711", identifier.Value);
    }

    [Fact]
    public void ParseToDomainModel_WhenValidGlobalAssetIdHeader_ReturnsFilter()
    {
        var header = "[{\"name\":\"globalAssetId\",\"value\":\"https://mm-software.com/ids/assets/000-002\"}]";

        var result = _sut.ParseToDomainModel(header);

        Assert.NotNull(result);
        var identifier = Assert.Single(result!.Identifiers);
        Assert.Equal("globalAssetId", identifier.Name);
        Assert.Equal("https://mm-software.com/ids/assets/000-002", identifier.Value);
    }

    [Fact]
    public void ParseToDomainModel_WhenMultipleIdentifiers_ReturnsAllIdentifiers()
    {
        var header = "[{\"name\":\"serialNumber\",\"value\":\"SN-4711\"},{\"name\":\"batchId\",\"value\":\"B-2026-03\"}]";

        var result = _sut.ParseToDomainModel(header);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Identifiers.Count);
    }

    [Fact]
    public void ParseToDomainModel_WhenHeaderIsMalformedJson_ThrowsInvalidUserInput()
    {
        var malformedHeader = "[{\"name\":\"serialNumber\",\"value\":\"SN-4711\"";

        var ex = Assert.Throws<InvalidUserInputException>(() => _sut.ParseToDomainModel(malformedHeader));

        Assert.Contains("Invalid aastwinengine-assetids header", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseToDomainModel_WhenHeaderIsNotArray_ThrowsInvalidUserInput()
    {
        var objectHeader = "{\"name\":\"serialNumber\",\"value\":\"SN-4711\"}";

        var ex = Assert.Throws<InvalidUserInputException>(() => _sut.ParseToDomainModel(objectHeader));

        Assert.Contains("JSON array", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseToDomainModel_WhenHeaderContainsUnsupportedProperty_ThrowsInvalidUserInput()
    {
        var header = "[{\"name\":\"serialNumber\",\"value\":\"SN-4711\",\"externalSubjectId\":{}}]";

        var ex = Assert.Throws<InvalidUserInputException>(() => _sut.ParseToDomainModel(header));

        Assert.Contains("Unsupported property", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseToDomainModel_WhenNameMissing_ThrowsInvalidUserInput()
    {
        var header = "[{\"value\":\"SN-4711\"}]";

        var ex = Assert.Throws<InvalidUserInputException>(() => _sut.ParseToDomainModel(header));

        Assert.Contains("'name' property", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseToDomainModel_WhenValueMissing_ThrowsInvalidUserInput()
    {
        var header = "[{\"name\":\"serialNumber\"}]";

        var ex = Assert.Throws<InvalidUserInputException>(() => _sut.ParseToDomainModel(header));

        Assert.Contains("'value' property", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseToDomainModel_WhenNameEmpty_ThrowsInvalidUserInput()
    {
        var header = "[{\"name\":\"\",\"value\":\"SN-4711\"}]";

        var ex = Assert.Throws<InvalidUserInputException>(() => _sut.ParseToDomainModel(header));

        Assert.Contains("'name' must not be empty", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseToDomainModel_WhenValueEmpty_ThrowsInvalidUserInput()
    {
        var header = "[{\"name\":\"serialNumber\",\"value\":\"\"}]";

        var ex = Assert.Throws<InvalidUserInputException>(() => _sut.ParseToDomainModel(header));

        Assert.Contains("'value' must not be empty", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseToDomainModel_WhenIdentifierCountExceedsMaximum_ThrowsInvalidUserInputException()
    {
        var logger = Substitute.For<ILogger<AssetIdsFilterHeaderValidation>>();
        var sut = new AssetIdsFilterHeaderValidation(logger);
        var identifiers = Enumerable.Range(1, 11)
            .Select(index => new
            {
                name = $"name-{index}",
                value = $"value-{index}"
            });
        var headerValue = JsonSerializer.Serialize(identifiers);

        var exception = Assert.Throws<InvalidUserInputException>(() => sut.ParseToDomainModel(headerValue));

        Assert.Contains("maximum of 10 asset identifiers", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseToDomainModel_WhenIdentifierCountIsAtMaximum_ReturnsFilter()
    {
        var logger = Substitute.For<ILogger<AssetIdsFilterHeaderValidation>>();
        var sut = new AssetIdsFilterHeaderValidation(logger);
        var identifiers = Enumerable.Range(1, 10)
            .Select(index => new
            {
                name = $"name-{index}",
                value = $"value-{index}"
            });
        var headerValue = JsonSerializer.Serialize(identifiers);

        var result = sut.ParseToDomainModel(headerValue);

        Assert.NotNull(result);
        Assert.Equal(10, result!.Identifiers.Count);
    }

    [Fact]
    public void ParseToDomainModel_WhenArrayIsEmpty_ReturnsEmptyFilter()
    {
        var logger = Substitute.For<ILogger<AssetIdsFilterHeaderValidation>>();
        var sut = new AssetIdsFilterHeaderValidation(logger);

        var result = sut.ParseToDomainModel("[]");

        Assert.NotNull(result);
        Assert.Empty(result!.Identifiers);
    }

    [Fact]
    public void ParseToDomainModel_WhenNameExceedsMaxLength_ThrowsInvalidUserInput()
    {
        var longName = new string('a', AssetIdsFilterHeaderValidation.NameMaxLength + 1);
        var header = $"[{{\"name\":\"{longName}\",\"value\":\"SN-4711\"}}]";

        var ex = Assert.Throws<InvalidUserInputException>(() => _sut.ParseToDomainModel(header));

        Assert.Contains($"'name' must not exceed {AssetIdsFilterHeaderValidation.NameMaxLength} characters", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseToDomainModel_WhenNameIsAtMaxLength_ReturnsFilter()
    {
        var exactName = new string('a', AssetIdsFilterHeaderValidation.NameMaxLength);
        var header = $"[{{\"name\":\"{exactName}\",\"value\":\"SN-4711\"}}]";

        var result = _sut.ParseToDomainModel(header);

        Assert.NotNull(result);
        Assert.Single(result!.Identifiers);
    }

    [Fact]
    public void ParseToDomainModel_WhenValueExceedsMaxLength_ThrowsInvalidUserInput()
    {
        var longValue = new string('a', AssetIdsFilterHeaderValidation.ValueMaxLength + 1);
        var header = $"[{{\"name\":\"serialNumber\",\"value\":\"{longValue}\"}}]";

        var ex = Assert.Throws<InvalidUserInputException>(() => _sut.ParseToDomainModel(header));

        Assert.Contains($"'value' must not exceed {AssetIdsFilterHeaderValidation.ValueMaxLength} characters", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseToDomainModel_WhenValueIsAtMaxLength_ReturnsFilter()
    {
        var exactValue = new string('a', AssetIdsFilterHeaderValidation.ValueMaxLength);
        var header = $"[{{\"name\":\"serialNumber\",\"value\":\"{exactValue}\"}}]";

        var result = _sut.ParseToDomainModel(header);

        Assert.NotNull(result);
        Assert.Single(result!.Identifiers);
    }

    [Theory]
    [InlineData("\u0000")]
    [InlineData("\u0001")]
    [InlineData("\u001F")]
    [InlineData("\uFFFE")]
    [InlineData("\uFFFF")]
    public void ParseToDomainModel_WhenNameContainsInvalidXmlCharacter_ThrowsInvalidUserInput(string invalidChar)
    {
        var header = JsonSerializer.Serialize(new[] { new { name = $"serial{invalidChar}Number", value = "SN-4711" } });

        var ex = Assert.Throws<InvalidUserInputException>(() => _sut.ParseToDomainModel(header));

        Assert.Contains("'name' contains invalid characters", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("\u0000")]
    [InlineData("\u0001")]
    [InlineData("\u001F")]
    [InlineData("\uFFFE")]
    [InlineData("\uFFFF")]
    public void ParseToDomainModel_WhenValueContainsInvalidXmlCharacter_ThrowsInvalidUserInput(string invalidChar)
    {
        var header = JsonSerializer.Serialize(new[] { new { name = "serialNumber", value = $"SN{invalidChar}4711" } });

        var ex = Assert.Throws<InvalidUserInputException>(() => _sut.ParseToDomainModel(header));

        Assert.Contains("'value' contains invalid characters", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Hello World")]
    [InlineData("serial\u0009Number")]
    [InlineData("value\u000AHere")]
    [InlineData("\u20AC-price")]
    public void ParseToDomainModel_WhenNameContainsValidXmlCharacters_ReturnsFilter(string validName)
    {
        var header = JsonSerializer.Serialize(new[] { new { name = validName, value = "SN-4711" } });

        var result = _sut.ParseToDomainModel(header);

        Assert.NotNull(result);
        Assert.Single(result!.Identifiers);
    }

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("javascript:alert(1)")]
    [InlineData("../../etc/passwd")]
    [InlineData("'; DROP TABLE assets; --")]
    public void ParseToDomainModel_WhenNameContainsInjectionPattern_ThrowsInvalidUserInput(string maliciousName)
    {
        var header = JsonSerializer.Serialize(new[] { new { name = maliciousName, value = "SN-4711" } });

        var ex = Assert.Throws<InvalidUserInputException>(() => _sut.ParseToDomainModel(header));

        Assert.Contains("potentially malicious patterns", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("javascript:alert(1)")]
    [InlineData("../../etc/passwd")]
    [InlineData("'; DROP TABLE assets; --")]
    public void ParseToDomainModel_WhenValueContainsInjectionPattern_ThrowsInvalidUserInput(string maliciousValue)
    {
        var header = JsonSerializer.Serialize(new[] { new { name = "serialNumber", value = maliciousValue } });

        var ex = Assert.Throws<InvalidUserInputException>(() => _sut.ParseToDomainModel(header));

        Assert.Contains("potentially malicious patterns", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseToDomainModel_WhenNameIsNumericJsonValue_CoercesToStringAndReturnsFilter()
    {
        const string header = "[{\"name\":42,\"value\":\"SN-4711\"}]";

        var result = _sut.ParseToDomainModel(header);

        Assert.NotNull(result);
        var identifier = Assert.Single(result!.Identifiers);
        Assert.Equal("42", identifier.Name);
        Assert.Equal("SN-4711", identifier.Value);
    }

    [Fact]
    public void ParseToDomainModel_WhenValueIsNumericJsonValue_CoercesToStringAndReturnsFilter()
    {
        const string header = "[{\"name\":\"serialNumber\",\"value\":4711}]";

        var result = _sut.ParseToDomainModel(header);

        Assert.NotNull(result);
        var identifier = Assert.Single(result!.Identifiers);
        Assert.Equal("serialNumber", identifier.Name);
        Assert.Equal("4711", identifier.Value);
    }
}

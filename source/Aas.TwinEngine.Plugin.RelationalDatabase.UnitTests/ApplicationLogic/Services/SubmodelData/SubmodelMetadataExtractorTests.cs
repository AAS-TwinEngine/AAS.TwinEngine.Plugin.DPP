using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Helper;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.UnitTests.ApplicationLogic.Services.SubmodelData;

public class SubmodelMetadataExtractorTests
{
    private readonly ILogger<SubmodelMetadataExtractor> _logger;
    private readonly IOptions<ExtractionRules> _extractionRulesOptions;
    private SubmodelMetadataExtractor _sut;

    public SubmodelMetadataExtractorTests()
    {
        _logger = Substitute.For<ILogger<SubmodelMetadataExtractor>>();
        _extractionRulesOptions = Substitute.For<IOptions<ExtractionRules>>();

        var defaultRules = CreateDefaultExtractionRules();
        _extractionRulesOptions.Value.Returns(defaultRules);

        _sut = new SubmodelMetadataExtractor(_extractionRulesOptions, _logger);
    }

    [Fact]
    public void ExtractSubmodelMetadata_ValidSubmodelId_ReturnsExtractionResult()
    {
        const string submodelId = "product123/Nameplate/data";

        var result = _sut.ExtractSubmodelMetadata(submodelId);

        Assert.NotNull(result);
        Assert.Equal("product123", result.ProductId);
        Assert.Equal(SubmodelName.NamePlate, result.SubmodelName);
    }

    [Fact]
    public void ExtractSubmodelMetadata_ContactInformationSubmodel_ReturnsCorrectSubmodelName()
    {
        const string submodelId = "product456/ContactInformation/info";

        var result = _sut.ExtractSubmodelMetadata(submodelId);

        Assert.Equal(SubmodelName.ContactInformation, result.SubmodelName);
    }

    [Fact]
    public void ExtractSubmodelMetadata_CaseInsensitiveSubmodelName_ReturnsCorrectResult()
    {
        const string submodelId = "product789/NAMEPLATE/data";

        var result = _sut.ExtractSubmodelMetadata(submodelId);

        Assert.Equal(SubmodelName.NamePlate, result.SubmodelName);
    }

    [Fact]
    public void ExtractSubmodelMetadata_ProductIdAtDifferentIndex_ExtractsCorrectly()
    {
        var rules = new ExtractionRules
        {
            ProductIdExtractionRules =
            [
                new() { Separator = "/", Index = 2, Pattern = string.Empty }
            ],
            SubmodelNameExtractionRules = CreateDefaultSubmodelNameRules()
        };
        _extractionRulesOptions.Value.Returns(rules);
        _sut = new SubmodelMetadataExtractor(_extractionRulesOptions, _logger);
        const string submodelId = "prefix/product999/Nameplate/data";

        var result = _sut.ExtractSubmodelMetadata(submodelId);

        Assert.Equal("product999", result.ProductId);
    }

    [Fact]
    public void ExtractSubmodelMetadata_DifferentSeparator_ExtractsCorrectly()
    {
        var rules = new ExtractionRules
        {
            ProductIdExtractionRules =
            [
                new() { Separator = "-", Index = 1, Pattern = string.Empty }
            ],
            SubmodelNameExtractionRules =
            [
                new() { SubmodelName = "Nameplate", Pattern = [".*Nameplate.*"] }
            ]
        };
        _extractionRulesOptions.Value.Returns(rules);
        _sut = new SubmodelMetadataExtractor(_extractionRulesOptions, _logger);
        const string submodelId = "productABC-Nameplate-data";

        var result = _sut.ExtractSubmodelMetadata(submodelId);

        Assert.Equal("productABC", result.ProductId);
    }

    [Fact]
    public void ExtractSubmodelMetadata_NoMatchingProductIdRule_ThrowsInvalidUserInputException()
    {
        var rules = new ExtractionRules
        {
            ProductIdExtractionRules =
            [
                new() { Separator = "|", Index = 1, Pattern = string.Empty }
            ],
            SubmodelNameExtractionRules = CreateDefaultSubmodelNameRules()
        };
        _extractionRulesOptions.Value.Returns(rules);
        _sut = new SubmodelMetadataExtractor(_extractionRulesOptions, _logger);
        const string submodelId = "product/Nameplate/data";

        Assert.Throws<InvalidUserInputException>(() => _sut.ExtractSubmodelMetadata(submodelId));

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("ProductId could not be extracted")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void ExtractSubmodelMetadata_IndexOutOfRange_ThrowsInvalidUserInputException()
    {
        var rules = new ExtractionRules
        {
            ProductIdExtractionRules =
            [
                new() { Separator = "/", Index = 10, Pattern = string.Empty }
            ],
            SubmodelNameExtractionRules = CreateDefaultSubmodelNameRules()
        };
        _extractionRulesOptions.Value.Returns(rules);
        _sut = new SubmodelMetadataExtractor(_extractionRulesOptions, _logger);
        const string submodelId = "product/Nameplate";

        Assert.Throws<InvalidUserInputException>(() => _sut.ExtractSubmodelMetadata(submodelId));
    }

    [Fact]
    public void ExtractSubmodelMetadata_EmptyProductIdRules_ThrowsInvalidUserInputException()
    {
        var rules = new ExtractionRules
        {
            ProductIdExtractionRules = new List<ProductIdExtractionRules>(),
            SubmodelNameExtractionRules = CreateDefaultSubmodelNameRules()
        };
        _extractionRulesOptions.Value.Returns(rules);
        _sut = new SubmodelMetadataExtractor(_extractionRulesOptions, _logger);
        const string submodelId = "product/Nameplate/data";

        Assert.Throws<InvalidUserInputException>(() => _sut.ExtractSubmodelMetadata(submodelId));
    }

    [Fact]
    public void ExtractSubmodelMetadata_NoMatchingSubmodelNamePattern_ThrowsInvalidUserInputException()
    {
        var rules = new ExtractionRules
        {
            ProductIdExtractionRules = CreateDefaultProductIdRules(),
            SubmodelNameExtractionRules = new List<SubmodelNameExtractionRules>
            {
                new() { SubmodelName = "UnknownSubmodel", Pattern = new List<string> { ".*UnknownPattern.*" } }
            }
        };
        _extractionRulesOptions.Value.Returns(rules);
        _sut = new SubmodelMetadataExtractor(_extractionRulesOptions, _logger);
        const string submodelId = "product/Nameplate/data";

        Assert.Throws<InvalidUserInputException>(() => _sut.ExtractSubmodelMetadata(submodelId));
        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Submodel Name could not be extracted")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void ExtractSubmodelMetadata_UnrecognizedSubmodelName_ThrowsInvalidUserInputException()
    {
        var rules = new ExtractionRules
        {
            ProductIdExtractionRules = CreateDefaultProductIdRules(),
            SubmodelNameExtractionRules = new List<SubmodelNameExtractionRules>
            {
                new() { SubmodelName = "InvalidSubmodelName", Pattern = new List<string> { ".*Invalid.*" } }
            }
        };
        _extractionRulesOptions.Value.Returns(rules);
        _sut = new SubmodelMetadataExtractor(_extractionRulesOptions, _logger);
        const string submodelId = "product/Invalid/data";

        var exception = Assert.Throws<InvalidUserInputException>(() => _sut.ExtractSubmodelMetadata(submodelId));
        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("is not recognized")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void ExtractSubmodelMetadata_EmptySubmodelNameRules_ThrowsInvalidUserInputException()
    {
        var rules = new ExtractionRules
        {
            ProductIdExtractionRules = CreateDefaultProductIdRules(),
            SubmodelNameExtractionRules = new List<SubmodelNameExtractionRules>()
        };
        _extractionRulesOptions.Value.Returns(rules);
        _sut = new SubmodelMetadataExtractor(_extractionRulesOptions, _logger);
        const string submodelId = "product/Nameplate/data";

        Assert.Throws<InvalidUserInputException>(() => _sut.ExtractSubmodelMetadata(submodelId));
    }

    [Fact]
    public void ExtractSubmodelMetadata_MultiplePatterns_MatchesFirstPattern()
    {
        var rules = new ExtractionRules
        {
            ProductIdExtractionRules = CreateDefaultProductIdRules(),
            SubmodelNameExtractionRules = new List<SubmodelNameExtractionRules>
            {
                new()
                {
                    SubmodelName = "Nameplate",
                    Pattern = [".*plate.*", ".*NAMEPLATE.*", ".*Nameplate.*"]
                }
            }
        };
        _extractionRulesOptions.Value.Returns(rules);
        _sut = new SubmodelMetadataExtractor(_extractionRulesOptions, _logger);
        const string submodelId = "product/someplate/data";

        var result = _sut.ExtractSubmodelMetadata(submodelId);

        Assert.Equal(SubmodelName.NamePlate, result.SubmodelName);
    }

    [Fact]
    public void ExtractSubmodelMetadata_MultipleProductIdRules_UsesFirstMatch()
    {
        var rules = new ExtractionRules
        {
            ProductIdExtractionRules =
            [
                new() { Separator = "|", Index = 1, Pattern = "Regex" },
                new() { Separator = "/", Index = 1, Pattern = "Regex" }
            ],
            SubmodelNameExtractionRules = CreateDefaultSubmodelNameRules()
        };
        _extractionRulesOptions.Value.Returns(rules);
        _sut = new SubmodelMetadataExtractor(_extractionRulesOptions, _logger);
        const string submodelId = "product123/Nameplate/data";

        var result = _sut.ExtractSubmodelMetadata(submodelId);

        Assert.Equal("product123", result.ProductId);
    }

    [Fact]
    public void ExtractSubmodelMetadata_MultipleSubmodelNameRules_UsesFirstMatch()
    {
        var rules = new ExtractionRules
        {
            ProductIdExtractionRules = CreateDefaultProductIdRules(),
            SubmodelNameExtractionRules = new List<SubmodelNameExtractionRules>
            {
                new() { SubmodelName = "ContactInformation", Pattern = new List<string> { ".*Contact.*" } },
                new() { SubmodelName = "Nameplate", Pattern = new List<string> { ".*Nameplate.*" } }
            }
        };
        _extractionRulesOptions.Value.Returns(rules);
        _sut = new SubmodelMetadataExtractor(_extractionRulesOptions, _logger);
        const string submodelId = "product/ContactInfo/data";

        var result = _sut.ExtractSubmodelMetadata(submodelId);

        Assert.Equal(SubmodelName.ContactInformation, result.SubmodelName);
    }

    [Fact]
    public void ExtractSubmodelMetadata_NullSubmodelId_ThrowsInvalidUserInputException() => Assert.Throws<InvalidUserInputException>(() => _sut.ExtractSubmodelMetadata(null!));

    [Fact]
    public void ExtractSubmodelMetadata_EmptySubmodelId_ThrowsInvalidUserInputException() => Assert.Throws<InvalidUserInputException>(() => _sut.ExtractSubmodelMetadata(string.Empty));

    [Fact]
    public void ExtractSubmodelMetadata_WhitespaceSubmodelId_ThrowsInvalidUserInputException() => Assert.Throws<InvalidUserInputException>(() => _sut.ExtractSubmodelMetadata("   "));

    [Fact]
    public void ExtractSubmodelMetadata_ZeroIndex_ThrowsInvalidUserInputException()
    {
        var rules = new ExtractionRules
        {
            ProductIdExtractionRules = new List<ProductIdExtractionRules>
            {
                new() { Separator = "/", Index = 0, Pattern = string.Empty }
            },
            SubmodelNameExtractionRules = CreateDefaultSubmodelNameRules()
        };
        _extractionRulesOptions.Value.Returns(rules);
        _sut = new SubmodelMetadataExtractor(_extractionRulesOptions, _logger);
        const string submodelId = "product/Nameplate/data";

        Assert.Throws<InvalidUserInputException>(() => _sut.ExtractSubmodelMetadata(submodelId));
    }

    [Fact]
    public void ExtractSubmodelMetadata_NegativeIndex_ThrowsInvalidUserInputException()
    {
        var rules = new ExtractionRules
        {
            ProductIdExtractionRules = new List<ProductIdExtractionRules>
            {
                new() { Separator = "/", Index = -1, Pattern = string.Empty }
            },
            SubmodelNameExtractionRules = CreateDefaultSubmodelNameRules()
        };
        _extractionRulesOptions.Value.Returns(rules);
        _sut = new SubmodelMetadataExtractor(_extractionRulesOptions, _logger);
        const string submodelId = "product/Nameplate/data";

        Assert.Throws<InvalidUserInputException>(() => _sut.ExtractSubmodelMetadata(submodelId));
    }

    private static ExtractionRules CreateDefaultExtractionRules()
    {
        return new ExtractionRules
        {
            ProductIdExtractionRules = CreateDefaultProductIdRules(),
            SubmodelNameExtractionRules = CreateDefaultSubmodelNameRules()
        };
    }

    private static List<ProductIdExtractionRules> CreateDefaultProductIdRules()
    {
        return
        [
            new() { Separator = "/", Index = 1, Pattern = string.Empty }
        ];
    }

    private static List<SubmodelNameExtractionRules> CreateDefaultSubmodelNameRules()
    {
        return
        [
            new() { SubmodelName = "Nameplate", Pattern = [".*Nameplate.*"] },
            new() { SubmodelName = "ContactInformation", Pattern = [".*ContactInformation.*"] }
        ];
    }
}

using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Helper;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.UnitTests.ApplicationLogic.Services.SubmodelData;

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
    public void ExtractSubmodelMetadata_SplitStrategy_ReturnsExtractionResult()
    {
        const string submodelId = "product123/Nameplate/data";

        var result = _sut.ExtractSubmodelMetadata(submodelId);

        Assert.NotNull(result);
        Assert.Equal("product123", result.ProductId);
        Assert.Equal(SubmodelName.Nameplate, result.SubmodelName);
    }

    [Fact]
    public void ExtractSubmodelMetadata_SplitStrategy_DifferentIndex_ExtractsCorrectly()
    {
        var rules = new ExtractionRules
        {
            ProductIdExtractionRules =
            [
                new() { Strategy = ExtractionStrategy.Split, Pattern = "/", Index = 2 }
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
    public void ExtractSubmodelMetadata_SplitStrategy_DifferentSeparator_ExtractsCorrectly()
    {
        var rules = new ExtractionRules
        {
            ProductIdExtractionRules =
            [
                new() { Strategy = ExtractionStrategy.Split, Pattern = "-", Index = 1 }
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
    public void ExtractSubmodelMetadata_SplitStrategy_RangeEndIndex_JoinsSegments()
    {
        var rules = new ExtractionRules
        {
            ProductIdExtractionRules =
            [
                new() { Strategy = ExtractionStrategy.Split, Pattern = "/", Index = 2, EndIndex = 3 }
            ],
            SubmodelNameExtractionRules = CreateDefaultSubmodelNameRules()
        };
        _extractionRulesOptions.Value.Returns(rules);
        _sut = new SubmodelMetadataExtractor(_extractionRulesOptions, _logger);
        const string submodelId = "prefix/2000-2201/353-000/Nameplate/data";

        var result = _sut.ExtractSubmodelMetadata(submodelId);

        Assert.Equal("2000-2201/353-000", result.ProductId);
    }

    [Fact]
    public void ExtractSubmodelMetadata_SplitStrategy_EndIndexOutOfBounds_ThrowsInvalidUserInputException()
    {
        var rules = new ExtractionRules
        {
            ProductIdExtractionRules =
            [
                new() { Strategy = ExtractionStrategy.Split, Pattern = "/", Index = 2, EndIndex = 10 }
            ],
            SubmodelNameExtractionRules = CreateDefaultSubmodelNameRules()
        };
        _extractionRulesOptions.Value.Returns(rules);
        _sut = new SubmodelMetadataExtractor(_extractionRulesOptions, _logger);
        const string submodelId = "prefix/product/Nameplate";

        Assert.Throws<InvalidUserInputException>(() => _sut.ExtractSubmodelMetadata(submodelId));
    }

    [Fact]
    public void ExtractSubmodelMetadata_RegexStrategy_SingleSegment_ExtractsCorrectly()
    {
        var rules = new ExtractionRules
        {
            ProductIdExtractionRules =
            [
                new()
                {
                    Strategy = ExtractionStrategy.Regex,
                    Pattern = @"^[^/]+/([^/]+)/",
                    Index = 1
                }
            ],
            SubmodelNameExtractionRules = CreateDefaultSubmodelNameRules()
        };
        _extractionRulesOptions.Value.Returns(rules);
        _sut = new SubmodelMetadataExtractor(_extractionRulesOptions, _logger);
        const string submodelId = "prefix/product123/Nameplate/data";

        var result = _sut.ExtractSubmodelMetadata(submodelId);

        Assert.Equal("product123", result.ProductId);
    }

    [Fact]
    public void ExtractSubmodelMetadata_RegexStrategy_MultiSegment_ExtractsCorrectly()
    {
        var rules = new ExtractionRules
        {
            ProductIdExtractionRules =
            [
                new()
                {
                    Strategy = ExtractionStrategy.Regex,
                    Pattern = @"^https?://[^/]+/ids/submodel/([^/]+/[^/]+)(?:/|$)",
                    Index = 1
                }
            ],
            SubmodelNameExtractionRules = CreateDefaultSubmodelNameRules()
        };
        _extractionRulesOptions.Value.Returns(rules);
        _sut = new SubmodelMetadataExtractor(_extractionRulesOptions, _logger);
        const string submodelId = "https://wago.com/ids/submodel/2000-2201/353-000/Nameplate";

        var result = _sut.ExtractSubmodelMetadata(submodelId);

        Assert.Equal("2000-2201/353-000", result.ProductId);
    }

    [Fact]
    public void ExtractSubmodelMetadata_RegexStrategy_NoMatch_ThrowsInvalidUserInputException()
    {
        var rules = new ExtractionRules
        {
            ProductIdExtractionRules =
            [
                new()
                {
                    Strategy = ExtractionStrategy.Regex,
                    Pattern = @"^NOMATCH(\d+)$",
                    Index = 1
                }
            ],
            SubmodelNameExtractionRules = CreateDefaultSubmodelNameRules()
        };
        _extractionRulesOptions.Value.Returns(rules);
        _sut = new SubmodelMetadataExtractor(_extractionRulesOptions, _logger);
        const string submodelId = "product/Nameplate/data";

        Assert.Throws<InvalidUserInputException>(() => _sut.ExtractSubmodelMetadata(submodelId));
    }

    [Fact]
    public void ExtractSubmodelMetadata_RegexStrategy_GroupIndexOutOfBounds_ThrowsInvalidUserInputException()
    {
        var rules = new ExtractionRules
        {
            ProductIdExtractionRules =
            [
                new()
                {
                    Strategy = ExtractionStrategy.Regex,
                    Pattern = @"^([^/]+)/",
                    Index = 5
                }
            ],
            SubmodelNameExtractionRules = CreateDefaultSubmodelNameRules()
        };
        _extractionRulesOptions.Value.Returns(rules);
        _sut = new SubmodelMetadataExtractor(_extractionRulesOptions, _logger);
        const string submodelId = "product/Nameplate/data";

        Assert.Throws<InvalidUserInputException>(() => _sut.ExtractSubmodelMetadata(submodelId));
    }

    [Fact]
    public void ExtractSubmodelMetadata_ValidationPatternMatches_ReturnsExtractedValue()
    {
        var rules = new ExtractionRules
        {
            ProductIdExtractionRules =
            [
                new()
                {
                    Strategy = ExtractionStrategy.Split,
                    Pattern = "/",
                    Index = 1,
                    ValidationPattern = @"^[a-zA-Z0-9]+$"
                }
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
    public void ExtractSubmodelMetadata_ValidationPatternFails_SkipsToNextRule()
    {
        var rules = new ExtractionRules
        {
            ProductIdExtractionRules =
            [
                new()
                {
                    Strategy = ExtractionStrategy.Split,
                    Pattern = "/",
                    Index = 1,
                    ValidationPattern = @"^\d+$"
                },
                new()
                {
                    Strategy = ExtractionStrategy.Split,
                    Pattern = "/",
                    Index = 2,
                    ValidationPattern = @"^[a-zA-Z0-9]+$"
                }
            ],
            SubmodelNameExtractionRules = CreateDefaultSubmodelNameRules()
        };
        _extractionRulesOptions.Value.Returns(rules);
        _sut = new SubmodelMetadataExtractor(_extractionRulesOptions, _logger);
        const string submodelId = "notdigits/product456/Nameplate/data";

        var result = _sut.ExtractSubmodelMetadata(submodelId);

        Assert.Equal("product456", result.ProductId);
    }

    [Fact]
    public void ExtractSubmodelMetadata_MultipleRules_FirstMatchWins()
    {
        var rules = new ExtractionRules
        {
            ProductIdExtractionRules =
            [
                new() { Strategy = ExtractionStrategy.Split, Pattern = "|", Index = 1, ValidationPattern = @"^\d+$" },
                new() { Strategy = ExtractionStrategy.Split, Pattern = "/", Index = 1, ValidationPattern = @"^[a-zA-Z0-9]+$" }
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
    public void ExtractSubmodelMetadata_MixedStrategies_RegexFallsBackToSplit()
    {
        var rules = new ExtractionRules
        {
            ProductIdExtractionRules =
            [
                new()
                {
                    Strategy = ExtractionStrategy.Regex,
                    Pattern = @"^NOMATCH(\d+)$",
                    Index = 1,
                    ValidationPattern = @"^\d+$"
                },
                new()
                {
                    Strategy = ExtractionStrategy.Split,
                    Pattern = "/",
                    Index = 1,
                    ValidationPattern = @"^[a-zA-Z0-9]+$"
                }
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
    public void ExtractSubmodelMetadata_ThreeRuleFallbackChain_ThirdRuleWins()
    {
        var rules = new ExtractionRules
        {
            ProductIdExtractionRules =
            [
                new()
                {
                    Strategy = ExtractionStrategy.Regex,
                    Pattern = @"^NOMATCH1(\w+)$",
                    Index = 1,
                    ValidationPattern = @"^\d+$"
                },
                new()
                {
                    Strategy = ExtractionStrategy.Regex,
                    Pattern = @"^NOMATCH2(\w+)$",
                    Index = 1,
                    ValidationPattern = @"^\d+$"
                },
                new()
                {
                    Strategy = ExtractionStrategy.Split,
                    Pattern = "/",
                    Index = 1,
                    ValidationPattern = @"^[a-zA-Z0-9]+$"
                }
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
    public void ExtractSubmodelMetadata_NoMatchingProductIdRule_ThrowsInvalidUserInputException()
    {
        var rules = new ExtractionRules
        {
            ProductIdExtractionRules =
            [
                new() { Strategy = ExtractionStrategy.Split, Pattern = "|", Index = 3 }
            ],
            SubmodelNameExtractionRules = CreateDefaultSubmodelNameRules()
        };
        _extractionRulesOptions.Value.Returns(rules);
        _sut = new SubmodelMetadataExtractor(_extractionRulesOptions, _logger);
        const string submodelId = "noseparator";

        Assert.Throws<InvalidUserInputException>(() => _sut.ExtractSubmodelMetadata(submodelId));

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("ProductId could not be extracted")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void ExtractSubmodelMetadata_SplitIndexOutOfRange_ThrowsInvalidUserInputException()
    {
        var rules = new ExtractionRules
        {
            ProductIdExtractionRules =
            [
                new() { Strategy = ExtractionStrategy.Split, Pattern = "/", Index = 10 }
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
            ProductIdExtractionRules = new List<ProductIdExtractionRule>(),
            SubmodelNameExtractionRules = CreateDefaultSubmodelNameRules()
        };
        _extractionRulesOptions.Value.Returns(rules);
        _sut = new SubmodelMetadataExtractor(_extractionRulesOptions, _logger);
        const string submodelId = "product/Nameplate/data";

        Assert.Throws<InvalidUserInputException>(() => _sut.ExtractSubmodelMetadata(submodelId));
    }

    [Fact]
    public void ExtractSubmodelMetadata_NullSubmodelId_ThrowsInvalidUserInputException() =>
        Assert.Throws<InvalidUserInputException>(() => _sut.ExtractSubmodelMetadata(null!));

    [Fact]
    public void ExtractSubmodelMetadata_EmptySubmodelId_ThrowsInvalidUserInputException() =>
        Assert.Throws<InvalidUserInputException>(() => _sut.ExtractSubmodelMetadata(string.Empty));

    [Fact]
    public void ExtractSubmodelMetadata_WhitespaceSubmodelId_ThrowsInvalidUserInputException() =>
        Assert.Throws<InvalidUserInputException>(() => _sut.ExtractSubmodelMetadata("   "));

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

        Assert.Equal(SubmodelName.Nameplate, result.SubmodelName);
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

        Assert.Throws<InvalidUserInputException>(() => _sut.ExtractSubmodelMetadata(submodelId));
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

        Assert.Equal(SubmodelName.Nameplate, result.SubmodelName);
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

    private static ExtractionRules CreateDefaultExtractionRules()
    {
        return new ExtractionRules
        {
            ProductIdExtractionRules = CreateDefaultProductIdRules(),
            SubmodelNameExtractionRules = CreateDefaultSubmodelNameRules()
        };
    }

    private static List<ProductIdExtractionRule> CreateDefaultProductIdRules()
    {
        return
        [
            new() { Strategy = ExtractionStrategy.Split, Pattern = "/", Index = 1 }
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

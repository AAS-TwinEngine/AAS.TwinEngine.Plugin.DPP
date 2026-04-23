using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.UnitTests.ApplicationLogic.Services.SubmodelData;

public class ExtractionRulesValidatorTests
{
    private readonly ILogger<ExtractionRulesValidator> _logger;
    private readonly ExtractionRulesValidator _sut;

    private const string GenericError = "Invalid extraction rules configuration.";

    public ExtractionRulesValidatorTests()
    {
        _logger = Substitute.For<ILogger<ExtractionRulesValidator>>();
        _sut = new ExtractionRulesValidator(_logger);
    }

    [Theory]
    [InlineData(ExtractionStrategy.Split, "/", 1)]
    [InlineData(ExtractionStrategy.Regex, @"^(\w+)/", 1)]
    [InlineData(ExtractionStrategy.Regex, @"^(\w+)/", 0)]
    public void Validate_ValidRules_Succeeds(ExtractionStrategy strategy, string pattern, int index)
    {
        var rule = new ProductIdExtractionRule
        {
            Strategy = strategy,
            Pattern = pattern,
            Index = index,
            ValidationPattern = strategy == ExtractionStrategy.Regex ? @"^\w+$" : null
        };

        var options = CreateValidOptions(rule);

        var result = _sut.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_RegexIndexZero_Succeeds()
    {
        var options = CreateValidOptions(
            new ProductIdExtractionRule
            {
                Strategy = ExtractionStrategy.Regex,
                Pattern = @"^(\w+)/",
                Index = 0,
                ValidationPattern = @"^\w+$"
            });

        var result = _sut.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("", 1, "Pattern must not be empty")]
    [InlineData("/", -1, "Index must be >= 0")]
    public void Validate_InvalidBasicProductRules_Fails(string pattern, int index, string expectedLog)
    {
        var options = CreateValidOptions(
            new ProductIdExtractionRule { Strategy = ExtractionStrategy.Split, Pattern = pattern, Index = index });

        var result = _sut.Validate(null, options);

        AssertFailure(result);
        VerifyLog(expectedLog);
    }

    [Theory]
    [InlineData(5, 3)]
    [InlineData(10, 2)]
    public void Validate_EndIndexLessThanIndex_Fails(int index, int endIndex)
    {
        var options = CreateValidOptions(
            new ProductIdExtractionRule { Strategy = ExtractionStrategy.Split, Pattern = "/", Index = index, EndIndex = endIndex });

        var result = _sut.Validate(null, options);

        AssertFailure(result);
        VerifyLog("EndIndex must be >= Index");
    }

    [Theory]
    [InlineData("[invalid(", 1)]
    [InlineData("(", 1)]
    [InlineData("[invalid(", 0)]
    public void Validate_InvalidRegexPattern_Fails(string pattern, int index)
    {
        var options = CreateValidOptions(
            new ProductIdExtractionRule
            {
                Strategy = ExtractionStrategy.Regex,
                Pattern = pattern,
                Index = index,
                ValidationPattern = @"^\w+$"
            });

        var result = _sut.Validate(null, options);

        AssertFailure(result);
        VerifyLog("Pattern is not a valid regex");
    }

    [Theory]
    [InlineData("[invalid(")]
    [InlineData("(")]
    public void Validate_InvalidValidationPattern_Fails(string validationPattern)
    {
        var options = CreateValidOptions(
            new ProductIdExtractionRule
            {
                Strategy = ExtractionStrategy.Regex,
                Pattern = @"^(\w+)",
                Index = 1,
                ValidationPattern = validationPattern
            });

        var result = _sut.Validate(null, options);

        AssertFailure(result);
        VerifyLog("ValidationPattern is not a valid regex");
    }

    [Fact]
    public void Validate_MultipleRegexRules_MissingValidationPattern_Fails()
    {
        var options = CreateValidOptions(
            new ProductIdExtractionRule { Strategy = ExtractionStrategy.Regex, Pattern = @"^(\w+)/", Index = 1 },
            new ProductIdExtractionRule { Strategy = ExtractionStrategy.Regex, Pattern = @"^(\d+)/", Index = 1 });

        var result = _sut.Validate(null, options);

        AssertFailure(result);
        VerifyLog("ValidationPattern is required");
    }

    [Fact]
    public void Validate_NoProductRules_Fails()
    {
        var options = new ExtractionRules
        {
            ProductIdExtractionRules = [],
            SubmodelNameExtractionRules = CreateValidSubmodel()
        };

        var result = _sut.Validate(null, options);

        AssertFailure(result);
        VerifyLog("At least one ProductIdExtractionRule is required");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_EmptySubmodelName_Fails(string? name)
    {
        var options = CreateOptionsWithSubmodel(new SubmodelNameExtractionRules
        {
            SubmodelName = name,
            Pattern = [".*"]
        });

        var result = _sut.Validate(null, options);

        AssertFailure(result);
        VerifyLog("SubmodelName must not be empty");
    }

    [Fact]
    public void Validate_SubmodelWithoutPatterns_Fails()
    {
        var options = CreateOptionsWithSubmodel(new SubmodelNameExtractionRules
        {
            SubmodelName = "Nameplate",
            Pattern = []
        });

        var result = _sut.Validate(null, options);

        AssertFailure(result);
        VerifyLog("At least one pattern is required");
    }

    [Theory]
    [InlineData("[invalid(")]
    [InlineData("(")]
    public void Validate_SubmodelInvalidRegex_Fails(string pattern)
    {
        var options = CreateOptionsWithSubmodel(new SubmodelNameExtractionRules
        {
            SubmodelName = "Nameplate",
            Pattern = [pattern]
        });

        var result = _sut.Validate(null, options);

        AssertFailure(result);
        VerifyLog("is not a valid regex");
    }

    private static ExtractionRules CreateValidOptions(params ProductIdExtractionRule[] rules)
    {
        return new ExtractionRules
        {
            ProductIdExtractionRules = rules.ToList(),
            SubmodelNameExtractionRules = CreateValidSubmodel()
        };
    }

    private static ExtractionRules CreateOptionsWithSubmodel(SubmodelNameExtractionRules submodel)
    {
        return new ExtractionRules
        {
            ProductIdExtractionRules =
            [
                new ProductIdExtractionRule { Strategy = ExtractionStrategy.Split, Pattern = "/", Index = 1 }
            ],
            SubmodelNameExtractionRules = [submodel]
        };
    }

    private static List<SubmodelNameExtractionRules> CreateValidSubmodel()
    => [
        new SubmodelNameExtractionRules
        {
            SubmodelName = "Nameplate",
            Pattern = [".*Nameplate.*"]
        }
    ];

    private static void AssertFailure(ValidateOptionsResult result)
    {
        Assert.True(result.Failed);
        Assert.Equal(GenericError, result.FailureMessage);
    }

    private void VerifyLog(string expected)
    {
        _logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains(expected)),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}

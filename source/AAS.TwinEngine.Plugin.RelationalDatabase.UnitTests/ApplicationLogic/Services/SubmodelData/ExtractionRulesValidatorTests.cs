using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Config;

using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.UnitTests.ApplicationLogic.Services.SubmodelData;

public class ExtractionRulesValidatorTests
{
    private readonly ILogger<ExtractionRulesValidator> _logger;
    private readonly ExtractionRulesValidator _sut;

    public ExtractionRulesValidatorTests()
    {
        _logger = Substitute.For<ILogger<ExtractionRulesValidator>>();
        _sut = new ExtractionRulesValidator(_logger);
    }

    [Fact]
    public void Validate_NoProductIdRules_Fails()
    {
        var options = new ExtractionRules
        {
            ProductIdExtractionRules = [],
            SubmodelNameExtractionRules = [new() { SubmodelName = "Nameplate", Pattern = [".*Nameplate.*"] }]
        };

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("At least one ProductIdExtractionRule is required", result.FailureMessage);
    }

    [Fact]
    public void Validate_SingleValidSplitRule_Succeeds()
    {
        var options = new ExtractionRules
        {
            ProductIdExtractionRules =
            [
                new() { Strategy = ExtractionStrategy.Split, Pattern = "/", Index = 1 }
            ],
            SubmodelNameExtractionRules = [new() { SubmodelName = "Nameplate", Pattern = [".*Nameplate.*"] }]
        };

        var result = _sut.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_SingleRule_ValidationPatternOptional()
    {
        var options = new ExtractionRules
        {
            ProductIdExtractionRules =
            [
                new() { Strategy = ExtractionStrategy.Split, Pattern = "/", Index = 1, ValidationPattern = null }
            ],
            SubmodelNameExtractionRules = [new() { SubmodelName = "Nameplate", Pattern = [".*Nameplate.*"] }]
        };

        var result = _sut.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_MultipleRules_AllHaveValidationPattern_Succeeds()
    {
        var options = new ExtractionRules
        {
            ProductIdExtractionRules =
            [
                new() { Strategy = ExtractionStrategy.Regex, Pattern = @"^(\w+)/", Index = 1, ValidationPattern = @"^\w+$" },
                new() { Strategy = ExtractionStrategy.Split, Pattern = "/", Index = 1, ValidationPattern = @"^\w+$" }
            ],
            SubmodelNameExtractionRules = [new() { SubmodelName = "Nameplate", Pattern = [".*Nameplate.*"] }]
        };

        var result = _sut.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_EmptyPattern_Fails()
    {
        var options = new ExtractionRules
        {
            ProductIdExtractionRules =
            [
                new() { Strategy = ExtractionStrategy.Split, Pattern = "", Index = 1 }
            ],
            SubmodelNameExtractionRules = [new() { SubmodelName = "Nameplate", Pattern = [".*Nameplate.*"] }]
        };

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("Pattern must not be empty", result.FailureMessage);
    }

    [Fact]
    public void Validate_IndexLessThanZero_Fails()
    {
        var options = new ExtractionRules
        {
            ProductIdExtractionRules =
            [
                new() { Strategy = ExtractionStrategy.Split, Pattern = "/", Index = -1 }
            ],
            SubmodelNameExtractionRules = [new() { SubmodelName = "Nameplate", Pattern = [".*Nameplate.*"] }]
        };

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("Index must be >= 0", result.FailureMessage);
    }

    [Fact]
    public void Validate_EndIndexLessThanIndex_Fails()
    {
        var options = new ExtractionRules
        {
            ProductIdExtractionRules =
            [
                new() { Strategy = ExtractionStrategy.Split, Pattern = "/", Index = 5, EndIndex = 3 }
            ],
            SubmodelNameExtractionRules = [new() { SubmodelName = "Nameplate", Pattern = [".*Nameplate.*"] }]
        };

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("EndIndex must be >= Index", result.FailureMessage);
    }

    [Fact]
    public void Validate_InvalidRegexPattern_Fails()
    {
        var options = new ExtractionRules
        {
            ProductIdExtractionRules =
            [
                new() { Strategy = ExtractionStrategy.Regex, Pattern = "[invalid(", Index = 1 }
            ],
            SubmodelNameExtractionRules = [new() { SubmodelName = "Nameplate", Pattern = [".*Nameplate.*"] }]
        };

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("Pattern is not a valid regex", result.FailureMessage);
    }

    [Fact]
    public void Validate_DescriptionUsedInErrorMessage()
    {
        var options = new ExtractionRules
        {
            ProductIdExtractionRules =
            [
                new() { Strategy = ExtractionStrategy.Split, Pattern = "", Index = 1 }
            ],
            SubmodelNameExtractionRules = [new() { SubmodelName = "Nameplate", Pattern = [".*Nameplate.*"] }]
        };

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("ProductIdExtractionRule[0]: Pattern must", result.FailureMessage);
    }

    [Fact]
    public void Validate_EmptySubmodelName_Fails()
    {
        var options = new ExtractionRules
        {
            ProductIdExtractionRules =
            [
                new() { Strategy = ExtractionStrategy.Split, Pattern = "/", Index = 1 }
            ],
            SubmodelNameExtractionRules = [new() { SubmodelName = "", Pattern = [".*Nameplate.*"] }]
        };

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("SubmodelName must not be empty", result.FailureMessage);
    }

    [Fact]
    public void Validate_SubmodelNameRuleWithNoPatterns_Fails()
    {
        var options = new ExtractionRules
        {
            ProductIdExtractionRules =
            [
                new() { Strategy = ExtractionStrategy.Split, Pattern = "/", Index = 1 }
            ],
            SubmodelNameExtractionRules = [new() { SubmodelName = "Nameplate", Pattern = [] }]
        };

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("At least one pattern is required", result.FailureMessage);
    }

    [Fact]
    public void Validate_SubmodelNameRuleWithInvalidRegex_Fails()
    {
        var options = new ExtractionRules
        {
            ProductIdExtractionRules =
            [
                new() { Strategy = ExtractionStrategy.Split, Pattern = "/", Index = 1 }
            ],
            SubmodelNameExtractionRules = [new() { SubmodelName = "Nameplate", Pattern = ["[invalid("] }]
        };

        var result = _sut.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("is not a valid regex", result.FailureMessage);
    }
}

using System.Data;
using System.Data.Common;

using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.Validators;

using Microsoft.Extensions.Logging;

using Npgsql;

using NSubstitute;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.UnitTests.Infrastructure.DataAccess.Validators;

public class DbParameterValidatorTests
{
    private readonly ILogger _logger = Substitute.For<ILogger>();

    #region Helper Methods

    private static void AssertLoggedAtLevel(ILogger logger, LogLevel logLevel, int expectedCallCount = 1)
    {
        logger.Received(expectedCallCount).Log(
            logLevel,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    private static void AssertLoggedError(ILogger logger, int expectedCallCount = 1)
    {
        AssertLoggedAtLevel(logger, LogLevel.Error, expectedCallCount);
    }

    #endregion

    #region ValidateParameters Tests

    [Fact]
    public void ValidateParameters_WithNullParameters_ShouldNotThrow()
    {
        var exception = Record.Exception(() =>
            DbParameterValidator.ValidateParameters(null, _logger));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateParameters_WithEmptyParameters_ShouldNotThrow()
    {
        var parameters = new List<DbParameter>();

        var exception = Record.Exception(() =>
            DbParameterValidator.ValidateParameters(parameters, _logger));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateParameters_WithValidParameters_ShouldNotThrow()
    {
        var parameters = new List<DbParameter>
        {
            new NpgsqlParameter("@param1", "value1") { Direction = ParameterDirection.Input },
            new NpgsqlParameter("@param2", 123) { Direction = ParameterDirection.Input }
        };

        var exception = Record.Exception(() =>
            DbParameterValidator.ValidateParameters(parameters, _logger));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateParameters_WithTooManyParameters_ShouldThrow()
    {
        var parameters = Enumerable.Range(0, 101)
            .Select(i => new NpgsqlParameter($"@param{i}", i)
            {
                Direction = ParameterDirection.Input
            })
            .Cast<DbParameter>()
            .ToList();

        var exception = Assert.Throws<ResourceNotValidException>(() =>
            DbParameterValidator.ValidateParameters(parameters, _logger));

        AssertLoggedError(_logger);
    }

    [Fact]
    public void ValidateParameters_WithMaxAllowedParameters_ShouldNotThrow()
    {
        var parameters = Enumerable.Range(0, 100)
            .Select(i => new NpgsqlParameter($"@param{i}", i)
            {
                Direction = ParameterDirection.Input
            })
            .Cast<DbParameter>()
            .ToList();

        var exception = Record.Exception(() =>
            DbParameterValidator.ValidateParameters(parameters, _logger));

        Assert.Null(exception);
    }

    #endregion

    #region ValidateParameter Tests

    [Fact]
    public void ValidateParameter_WithNullParameter_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            DbParameterValidator.ValidateParameter(null!, _logger));
    }

    [Fact]
    public void ValidateParameter_WithValidParameter_ShouldNotThrow()
    {
        var parameter = new NpgsqlParameter("@param", "valid-value")
        {
            Direction = ParameterDirection.Input
        };

        var exception = Record.Exception(() =>
            DbParameterValidator.ValidateParameter(parameter, _logger));

        Assert.Null(exception);
    }

    #endregion

    #region Parameter Name Validation Tests

    [Fact]
    public void ValidateParameter_WithInvalidParameterName_ShouldThrow()
    {
        var parameter = new NpgsqlParameter("invalid_name", "value")
        {
            Direction = ParameterDirection.Input
        };

        Assert.Throws<ResourceNotValidException>(() =>
            DbParameterValidator.ValidateParameter(parameter, _logger));
        AssertLoggedError(_logger);
    }

    [Fact]
    public void ValidateParameter_WithEmptyParameterName_ShouldThrow()
    {
        var parameter = new NpgsqlParameter(string.Empty, "value")
        {
            Direction = ParameterDirection.Input
        };

        Assert.Throws<ResourceNotValidException>(() =>
            DbParameterValidator.ValidateParameter(parameter, _logger));
        AssertLoggedError(_logger);
    }

    [Fact]
    public void ValidateParameter_WithWhitespaceParameterName_ShouldThrow()
    {
        var parameter = new NpgsqlParameter("   ", "value")
        {
            Direction = ParameterDirection.Input
        };

        Assert.Throws<ResourceNotValidException>(() =>
            DbParameterValidator.ValidateParameter(parameter, _logger));

        AssertLoggedError(_logger);
    }

    [Fact]
    public void ValidateParameter_WithTooLongParameterName_ShouldThrow()
    {
        var longName = "@" + new string('a', 130);
        var parameter = new NpgsqlParameter(longName, "value")
        {
            Direction = ParameterDirection.Input
        };

        var exception = Assert.Throws<ResourceNotValidException>(() =>
            DbParameterValidator.ValidateParameter(parameter, _logger));

        Assert.Equal("Parameter name is too long.", exception.Message);
        AssertLoggedError(_logger);
    }

    [Fact]
    public void ValidateParameter_WithMaxLengthParameterName_ShouldNotThrow()
    {
        var maxName = "@" + new string('a', 127);
        var parameter = new NpgsqlParameter(maxName, "value")
        {
            Direction = ParameterDirection.Input
        };

        var exception = Record.Exception(() =>
            DbParameterValidator.ValidateParameter(parameter, _logger));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("@param-name")] // Contains hyphen
    [InlineData("@param.name")] // Contains dot
    [InlineData("@param name")] // Contains space
    [InlineData("@123param")] // Starts with number after @
    [InlineData("param")] // Missing @
    [InlineData("@@param")] // Double @
    public void ValidateParameter_WithInvalidParameterNameFormat_ShouldThrow(string invalidName)
    {
        var parameter = new NpgsqlParameter(invalidName, "value")
        {
            Direction = ParameterDirection.Input
        };

        Assert.Throws<ResourceNotValidException>(() =>
            DbParameterValidator.ValidateParameter(parameter, _logger));
        AssertLoggedError(_logger);
    }

    [Theory]
    [InlineData("@param")]
    [InlineData("@_param")]
    [InlineData("@param_1")]
    [InlineData("@Param123")]
    [InlineData("@_123")]
    public void ValidateParameter_WithValidParameterNameFormat_ShouldNotThrow(string validName)
    {
        var parameter = new NpgsqlParameter(validName, "value")
        {
            Direction = ParameterDirection.Input
        };

        var exception = Record.Exception(() =>
            DbParameterValidator.ValidateParameter(parameter, _logger));

        Assert.Null(exception);
    }

    #endregion

    #region Parameter Direction Validation Tests

    [Fact]
    public void ValidateParameter_WithInputDirection_ShouldNotThrow()
    {
        var parameter = new NpgsqlParameter("@param", "value")
        {
            Direction = ParameterDirection.Input
        };

        var exception = Record.Exception(() =>
            DbParameterValidator.ValidateParameter(parameter, _logger));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateParameter_WithInputOutputDirection_ShouldNotThrow()
    {
        var parameter = new NpgsqlParameter("@param", "value")
        {
            Direction = ParameterDirection.InputOutput
        };

        var exception = Record.Exception(() =>
            DbParameterValidator.ValidateParameter(parameter, _logger));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateParameter_WithOutputDirection_ShouldThrow()
    {
        var parameter = new NpgsqlParameter("@param", "value")
        {
            Direction = ParameterDirection.Output
        };

        Assert.Throws<ResourceNotValidException>(() =>
            DbParameterValidator.ValidateParameter(parameter, _logger));

        AssertLoggedError(_logger);
    }

    [Fact]
    public void ValidateParameter_WithReturnValueDirection_ShouldThrow()
    {
        var parameter = new NpgsqlParameter("@param", "value")
        {
            Direction = ParameterDirection.ReturnValue
        };

        Assert.Throws<ResourceNotValidException>(() =>
            DbParameterValidator.ValidateParameter(parameter, _logger));
        AssertLoggedError(_logger);
    }

    #endregion

    #region Parameter Value Type Tests

    [Fact]
    public void ValidateParameter_WithValidStringValue_ShouldNotThrow()
    {
        var parameter = new NpgsqlParameter("@param", "valid-value_123")
        {
            Direction = ParameterDirection.Input
        };

        var exception = Record.Exception(() =>
            DbParameterValidator.ValidateParameter(parameter, _logger));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateParameter_WithNumericValue_ShouldNotThrow()
    {
        var parameter = new NpgsqlParameter("@param", 12345)
        {
            Direction = ParameterDirection.Input
        };

        var exception = Record.Exception(() =>
            DbParameterValidator.ValidateParameter(parameter, _logger));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(typeof(byte))]
    [InlineData(typeof(sbyte))]
    [InlineData(typeof(short))]
    [InlineData(typeof(ushort))]
    [InlineData(typeof(int))]
    [InlineData(typeof(uint))]
    [InlineData(typeof(long))]
    [InlineData(typeof(ulong))]
    [InlineData(typeof(float))]
    [InlineData(typeof(double))]
    [InlineData(typeof(decimal))]
    public void ValidateParameter_WithNumericTypes_ShouldNotThrow(Type numericType)
    {
        var value = Convert.ChangeType(123, numericType);
        var parameter = new NpgsqlParameter("@param", value)
        {
            Direction = ParameterDirection.Input
        };

        var exception = Record.Exception(() =>
            DbParameterValidator.ValidateParameter(parameter, _logger));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateParameter_WithDateTimeValue_ShouldNotThrow()
    {
        var parameter = new NpgsqlParameter("@param", DateTime.UtcNow)
        {
            Direction = ParameterDirection.Input
        };

        var exception = Record.Exception(() =>
            DbParameterValidator.ValidateParameter(parameter, _logger));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateParameter_WithDateTimeOffsetValue_ShouldNotThrow()
    {
        var parameter = new NpgsqlParameter("@param", DateTimeOffset.UtcNow)
        {
            Direction = ParameterDirection.Input
        };

        var exception = Record.Exception(() =>
            DbParameterValidator.ValidateParameter(parameter, _logger));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateParameter_WithBoolValue_ShouldNotThrow()
    {
        var parameter = new NpgsqlParameter("@param", true)
        {
            Direction = ParameterDirection.Input
        };

        var exception = Record.Exception(() =>
            DbParameterValidator.ValidateParameter(parameter, _logger));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateParameter_WithGuidValue_ShouldNotThrow()
    {
        var parameter = new NpgsqlParameter("@param", Guid.NewGuid())
        {
            Direction = ParameterDirection.Input
        };

        var exception = Record.Exception(() => DbParameterValidator.ValidateParameter(parameter, _logger));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateParameter_WithByteArrayValue_ShouldNotThrow()
    {
        var parameter = new NpgsqlParameter("@param", new byte[] { 1, 2, 3, 4, 5 })
        {
            Direction = ParameterDirection.Input
        };

        var exception = Record.Exception(() =>
            DbParameterValidator.ValidateParameter(parameter, _logger));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateParameter_WithNullValue_ShouldNotThrow()
    {
        var parameter = new NpgsqlParameter("@param", null)
        {
            Direction = ParameterDirection.Input
        };

        var exception = Record.Exception(() =>
            DbParameterValidator.ValidateParameter(parameter, _logger));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateParameter_WithDBNullValue_ShouldNotThrow()
    {
        var parameter = new NpgsqlParameter("@param", DBNull.Value)
        {
            Direction = ParameterDirection.Input
        };

        var exception = Record.Exception(() =>
            DbParameterValidator.ValidateParameter(parameter, _logger));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateParameter_WithUnsupportedType_ShouldThrow()
    {
        var parameter = new NpgsqlParameter("@param", new { Name = "Test" })
        {
            Direction = ParameterDirection.Input
        };

        Assert.Throws<ResourceNotValidException>(() =>
            DbParameterValidator.ValidateParameter(parameter, _logger));
        AssertLoggedError(_logger);
    }

    #endregion

    #region SQL Injection Tests

    [Fact]
    public void ValidateParameter_WithSqlInjectionAttempt_ShouldThrow()
    {
        var parameter = new NpgsqlParameter("@param", "'; DROP TABLE Users; --")
        {
            Direction = ParameterDirection.Input
        };

        Assert.Throws<ResourceNotValidException>(() =>
            DbParameterValidator.ValidateParameter(parameter, _logger));
        AssertLoggedError(_logger);
    }

    [Theory]
    [InlineData("'; DROP TABLE Users; --")]
    [InlineData("1' OR '1'='1")]
    [InlineData("admin'--")]
    [InlineData("1; DELETE FROM Users")]
    [InlineData("EXEC sp_executesql")]
    [InlineData("UNION SELECT * FROM Users")]
    [InlineData("INSERT INTO Users VALUES")]
    [InlineData("UPDATE Users SET")]
    [InlineData("CREATE TABLE Test")]
    [InlineData("ALTER TABLE Users")]
    [InlineData("MERGE INTO Users")]
    [InlineData("/* comment */ SELECT")]
    [InlineData("DECLARE @var")]
    [InlineData("CAST(value AS int)")]
    [InlineData("CONVERT(int, value)")]
    public void ValidateParameter_WithCommonSqlInjectionPatterns_ShouldThrow(string maliciousInput)
    {
        var parameter = new NpgsqlParameter("@param", maliciousInput)
        {
            Direction = ParameterDirection.Input
        };

        Assert.Throws<ResourceNotValidException>(() =>
            DbParameterValidator.ValidateParameter(parameter, _logger));
        AssertLoggedError(_logger);
    }

    [Theory]
    [InlineData("valid-value")]
    [InlineData("user@example.com")]
    [InlineData("John Doe")]
    [InlineData("123-456-7890")]
    [InlineData("Product Name (2024)")]
    [InlineData("O'Reilly Media")]
    [InlineData("https://mm-software.com/ids/aas/000-002")]
    public void ValidateParameter_WithSafeStringValues_ShouldNotThrow(string safeInput)
    {
        var parameter = new NpgsqlParameter("@param", safeInput)
        {
            Direction = ParameterDirection.Input
        };

        var exception = Record.Exception(() =>
            DbParameterValidator.ValidateParameter(parameter, _logger));

        Assert.Null(exception);
    }

    #endregion

    #region String Length Validation Tests

    [Fact]
    public void ValidateParameter_WithTooLongString_ShouldThrow()
    {
        var longString = new string('a', 4001);
        var parameter = new NpgsqlParameter("@param", longString)
        {
            Direction = ParameterDirection.Input
        };

        var exception = Assert.Throws<ResourceNotValidException>(() =>
            DbParameterValidator.ValidateParameter(parameter, _logger));

        Assert.Equal("Parameter value is too long.", exception.Message);
        AssertLoggedError(_logger);
    }

    [Fact]
    public void ValidateParameter_WithMaxLengthString_ShouldNotThrow()
    {
        var maxString = new string('a', 4000);
        var parameter = new NpgsqlParameter("@param", maxString)
        {
            Direction = ParameterDirection.Input
        };

        var exception = Record.Exception(() =>
            DbParameterValidator.ValidateParameter(parameter, _logger));

        Assert.Null(exception);
    }

    #endregion

    #region Suspicious Characters Tests

    [Fact]
    public void ValidateParameter_WithControlCharacters_ShouldThrow()
    {
        var parameter = new NpgsqlParameter("@param", "test\0value")
        {
            Direction = ParameterDirection.Input
        };

        Assert.Throws<ResourceNotValidException>(() =>
            DbParameterValidator.ValidateParameter(parameter, _logger));

        AssertLoggedError(_logger);
    }

    [Fact]
    public void ValidateParameter_WithAllowedControlCharacters_ShouldNotThrow()
    {
        var parameter = new NpgsqlParameter("@param", "line1\r\nline2\ttab")
        {
            Direction = ParameterDirection.Input
        };

        var exception = Record.Exception(() =>
            DbParameterValidator.ValidateParameter(parameter, _logger));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("\0")] // Null character
    [InlineData("\x01")] // Start of heading
    [InlineData("\x1F")] // Unit separator
    [InlineData("\x7F")] // Delete character
    public void ValidateParameter_WithDisallowedControlCharacters_ShouldThrow(string controlChar)
    {
        var parameter = new NpgsqlParameter("@param", $"test{controlChar}value")
        {
            Direction = ParameterDirection.Input
        };

        Assert.Throws<ResourceNotValidException>(() =>
            DbParameterValidator.ValidateParameter(parameter, _logger));
        AssertLoggedError(_logger);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void ValidateParameters_WithMixedValidAndInvalidParameters_ShouldThrowOnFirstInvalid()
    {
        var parameters = new List<DbParameter>
        {
            new NpgsqlParameter("@valid1", "value1") { Direction = ParameterDirection.Input },
            new NpgsqlParameter("invalid", "value2") { Direction = ParameterDirection.Input }, // Invalid name
            new NpgsqlParameter("@valid3", "value3") { Direction = ParameterDirection.Input }
        };

        Assert.Throws<ResourceNotValidException>(() =>
            DbParameterValidator.ValidateParameters(parameters, _logger));

        AssertLoggedError(_logger);
    }

    [Fact]
    public void ValidateParameter_WithAllValidations_ShouldNotThrow()
    {
        var parameter = new NpgsqlParameter("@valid_param_123", "Safe value with allowed chars: @#$%")
        {
            Direction = ParameterDirection.Input
        };

        var exception = Record.Exception(() =>
            DbParameterValidator.ValidateParameter(parameter, _logger));

        Assert.Null(exception);
    }

    #endregion
}

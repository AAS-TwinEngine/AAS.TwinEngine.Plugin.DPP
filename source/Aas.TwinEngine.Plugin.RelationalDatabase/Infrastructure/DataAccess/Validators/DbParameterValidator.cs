using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;

using ResourceNotValidException = Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure.ResourceNotValidException;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.DataAccess.Validators;

public static partial class DbParameterValidator
{
    private const int MaxParameterNameLength = 128;
    private const int MaxStringParameterLength = 4000;
    private const int MaxParameterCount = 100;

    [GeneratedRegex(
        @"(\b(ALTER|CREATE|DELETE|DROP|EXEC(UTE)?|INSERT( +INTO)?|MERGE|SELECT|UPDATE|UNION( +ALL)?)\b)|" +
        @"(\bOR\b.*=)|(\bAND\b.*=)|(--)|(/\*)|(\*/)|(\bxp_)|(\bsp_)|(\bDECLARE\b)|(\bCAST\b)|(\bCONVERT\b)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        matchTimeoutMilliseconds: 100)]
    private static partial Regex SqlInjectionPattern();

    [GeneratedRegex(
        @"^@[a-zA-Z_][a-zA-Z0-9_]*$",
        RegexOptions.Compiled,
        matchTimeoutMilliseconds: 100)]
    private static partial Regex ValidParameterName();

    public static void ValidateParameters(IEnumerable<DbParameter>? parameters, ILogger? logger = null)
    {
        if (parameters is null)
        {
            return;
        }

        var parameterList = parameters.ToList();
        ValidateParameterCount(parameterList.Count, logger);

        foreach (var parameter in parameterList)
        {
            ValidateParameter(parameter, logger);
        }
    }

    public static void ValidateParameter(DbParameter parameter, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        ValidateParameterName(parameter.ParameterName, logger);
        ValidateParameterValue(parameter, logger);
        ValidateParameterDirection(parameter, logger);
    }

    private static void ValidateParameterCount(int count, ILogger? logger)
    {
        if (count <= MaxParameterCount)
        {
            return;
        }

        logger?.LogError("Parameter count {Count} exceeds maximum allowed {Max}", count, MaxParameterCount);
        throw new ResourceNotValidException();
    }

    private static void ValidateParameterName(string parameterName, ILogger? logger)
    {
        ThrowIfNullOrWhiteSpace(parameterName, logger);
        ThrowIfNameTooLong(parameterName, logger);
        ThrowIfInvalidFormat(parameterName, logger);
    }

    private static void ValidateParameterDirection(DbParameter parameter, ILogger? logger)
    {
        if (parameter.Direction is ParameterDirection.Input or ParameterDirection.InputOutput)
        {
            return;
        }

        logger?.LogError("Suspicious parameter direction: {Direction} for parameter {Name}", parameter.Direction, parameter.ParameterName);
        throw new ResourceNotValidException();
    }

    private static void ValidateParameterValue(DbParameter parameter, ILogger? logger)
    {
        if (IsNullValue(parameter.Value))
        {
            return;
        }

        var value = parameter.Value;
        var valueType = value!.GetType();

        switch (value)
        {
            case string stringValue:
                ValidateStringParameter(parameter.ParameterName, stringValue, logger);
                break;

            case DateTime:
            case DateTimeOffset:
            case bool:
            case Guid:
            case byte[]:
                break;

            default:
                ValidateValueType(valueType, parameter.ParameterName, logger);
                break;
        }
    }

    private static void ValidateStringParameter(string parameterName, string value, ILogger? logger)
    {
        ThrowIfStringTooLong(parameterName, value, logger);
        ThrowIfSqlInjectionDetected(parameterName, value, logger);
        ThrowIfSuspiciousCharacters(parameterName, value, logger);
    }

    private static void ValidateValueType(Type valueType, string parameterName, ILogger? logger)
    {
        if (IsNumericType(valueType))
        {
            return;
        }

        logger?.LogError("Unsupported parameter type: {Type} for parameter {Name}", valueType.Name, parameterName);
        throw new ResourceNotValidException();
    }

    #region Validation Helpers

    private static void ThrowIfNullOrWhiteSpace(string parameterName, ILogger? logger)
    {
        if (!string.IsNullOrWhiteSpace(parameterName))
        {
            return;
        }

        logger?.LogError("Parameter name is null or empty");
        throw new ResourceNotValidException();
    }

    private static void ThrowIfNameTooLong(string parameterName, ILogger? logger)
    {
        if (parameterName.Length <= MaxParameterNameLength)
        {
            return;
        }

        logger?.LogError(
            "Parameter name exceeds maximum length: {Length}",
            parameterName.Length);

        throw new ResourceNotValidException("Parameter name is too long.");
    }

    private static void ThrowIfInvalidFormat(string parameterName, ILogger? logger)
    {
        if (ValidParameterName().IsMatch(parameterName))
        {
            return;
        }

        logger?.LogError("Invalid parameter name format: {Name}", parameterName);
        throw new ResourceNotValidException();
    }

    private static void ThrowIfStringTooLong(string parameterName, string value, ILogger? logger)
    {
        if (value.Length <= MaxStringParameterLength)
        {
            return;
        }

        logger?.LogError("String parameter {Name} exceeds maximum length: {Length}", parameterName, value.Length);

        throw new ResourceNotValidException("Parameter value is too long.");
    }

    private static void ThrowIfSqlInjectionDetected(string parameterName, string value, ILogger? logger)
    {
        if (!SqlInjectionPattern().IsMatch(value))
        {
            return;
        }

        logger?.LogError("Potential SQL injection detected in parameter {Name}. Value: {Value}", parameterName, value);
        throw new ResourceNotValidException();
    }

    private static void ThrowIfSuspiciousCharacters(string parameterName, string value, ILogger? logger)
    {
        if (!ContainsSuspiciousCharacters(value))
        {
            return;
        }

        logger?.LogError("Suspicious characters detected in parameter {Name}", parameterName);
        throw new ResourceNotValidException();
    }

    #endregion

    #region Type Checking

    private static bool IsNullValue(object? value) =>
        value is null or DBNull;

    private static bool ContainsSuspiciousCharacters(string value) =>
        value.Any(c => char.IsControl(c) && c is not ('\r' or '\n' or '\t'));

    private static bool IsNumericType(Type type) =>
        type == typeof(byte) ||
        type == typeof(sbyte) ||
        type == typeof(short) ||
        type == typeof(ushort) ||
        type == typeof(int) ||
        type == typeof(uint) ||
        type == typeof(long) ||
        type == typeof(ulong) ||
        type == typeof(float) ||
        type == typeof(double) ||
        type == typeof(decimal);

    #endregion
}

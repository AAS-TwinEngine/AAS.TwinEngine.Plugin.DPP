using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Extensions;

public static class PaginationValidationExtensions
{
    public static void ValidateLimit(this int? limit, ILogger? logger = null)
    {
        if (limit is null or > 0)
        {
            return;
        }

        logger?.LogError("Invalid pagination limit provided: {Limit}", limit);
        throw new InvalidUserInputException();
    }

    public static void ValidateCursor(this string cursor, ILogger? logger = null)
    {
        if (!string.IsNullOrWhiteSpace(cursor))
        {
            _ = cursor.DecodeBase64(logger);
        }
    }
}

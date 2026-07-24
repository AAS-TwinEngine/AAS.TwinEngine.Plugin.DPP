using System.Data.Common;

using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Extensions;
using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.MetaData;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.MetaData.Helper;

public static class ShellsFilterQueryBuilder
{
    private const string FilterMarker = "{{__ASSET_FILTER__}}";
    private const string PaginationMarker = "{{__PAGINATION__}}";
    private const string GlobalAssetId = "globalAssetId";

    public static (string Query, List<DbParameter> Parameters) Build(
        string baseQuery,
        AssetIdFilterHeader? filter,
        string? idShort,
        Func<string, object?, DbParameter> parameterFactory,
        string? cursor = null,
        int? limit = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseQuery);
        ArgumentNullException.ThrowIfNull(parameterFactory);

        var hasIdShort = !string.IsNullOrEmpty(idShort);
        var hasCursor = !string.IsNullOrEmpty(cursor);

        var whereClauses = new List<string>();
        var parameters = new List<DbParameter>();

        if (filter?.Identifiers is { Count: > 0 } identifiers)
        {
            for (var index = 0; index < identifiers.Count; index++)
            {
                var identifier = identifiers[index];

                whereClauses.Add(
                    IsGlobalAssetId(identifier.Name)
                        ? BuildGlobalAssetIdClause(index, identifier, parameterFactory, parameters)
                        : BuildSpecificAssetIdClause(index, identifier, parameterFactory, parameters));
            }
        }

        if (hasIdShort)
        {
            whereClauses.Add($"A.\"IdShort\" = @idShort");
            parameters.Add(parameterFactory("@idShort", idShort));
        }

        if (hasCursor)
        {
            var decodedCursor = cursor.DecodeBase64();
            parameters.Add(parameterFactory("@p_cursor", decodedCursor));
            whereClauses.Add("A.\"AasId\" > @p_cursor");
        }

        var whereClause = whereClauses.Count > 0
            ? "WHERE " + string.Join(" AND ", whereClauses)
            : string.Empty;

        var query = ReplaceMarker(baseQuery, whereClause);

        query = BuildPaginationClause(query, limit, parameterFactory, parameters);

        return (query, parameters);
    }

    private static string BuildPaginationClause(
        string query,
        int? limit,
        Func<string, object?, DbParameter> parameterFactory,
        ICollection<DbParameter> parameters)
    {
        var pageSize = (limit ?? 100) + 1; // Fetch +1 to detect whether a next page exists
        parameters.Add(parameterFactory("@p_page_size", pageSize));

        return query.Replace(PaginationMarker, "ORDER BY A.\"AasId\" LIMIT @p_page_size", StringComparison.Ordinal);
    }

    private static string BuildGlobalAssetIdClause(
        int index,
        SpecificAssetIdsData identifier,
        Func<string, object?, DbParameter> parameterFactory,
        ICollection<DbParameter> parameters)
    {
        var parameterName = CreateValueParameterName(index);

        parameters.Add(parameterFactory(parameterName, identifier.Value));

        return $"A.\"GlobalAssetId\" = {parameterName}";
    }
    private static string BuildSpecificAssetIdClause(
        int index,
        SpecificAssetIdsData identifier,
        Func<string, object?, DbParameter> parameterFactory,
        ICollection<DbParameter> parameters)
    {
        var nameParameter = CreateNameParameterName(index);
        var valueParameter = CreateValueParameterName(index);

        parameters.Add(parameterFactory(nameParameter, identifier.Name));
        parameters.Add(parameterFactory(valueParameter, identifier.Value));

        return
            """
            EXISTS (
                SELECT 1
                FROM "SpecificAssetIds" sai
                WHERE sai."AssetId" = A."Id"
                AND sai."Name" =
            """
            + $"{nameParameter} "
            + $"AND sai.\"Value\" = {valueParameter}"
            + ")";
    }

    private static string CreateNameParameterName(int index)
        => $"@f_name_{index}";

    private static string CreateValueParameterName(int index)
        => $"@f_value_{index}";

    private static string ReplaceMarker(string baseQuery, string whereClause)
    {
        if (baseQuery.Contains(FilterMarker, StringComparison.Ordinal))
        {
            return baseQuery.Replace(FilterMarker, whereClause, StringComparison.Ordinal);
        }

        if (string.IsNullOrWhiteSpace(whereClause))
        {
            return baseQuery;
        }

        var trimmedQuery = TrimTrailingSemicolon(baseQuery);

        return $"{trimmedQuery}{Environment.NewLine}{whereClause};";
    }

    private static string TrimTrailingSemicolon(string query)
    {
        var trimmed = query.TrimEnd();

        return trimmed.EndsWith(';')
            ? trimmed[..^1].TrimEnd()
            : trimmed;
    }
    private static bool IsGlobalAssetId(string? identifierName)
        => string.Equals(identifierName, GlobalAssetId, StringComparison.Ordinal);
}

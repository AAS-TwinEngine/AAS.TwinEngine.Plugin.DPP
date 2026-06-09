using System.Data.Common;

using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.MetaData;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.MetaData.Helper;

public static class ShellsFilterQueryBuilder
{
    private const string FilterMarker = "{{__ASSET_FILTER__}}";
    private const string GlobalAssetId = "globalAssetId";

    public static (string Query, List<DbParameter> Parameters) Build(
        string baseQuery,
        AssetIdFilterHeader? filter,
        Func<string, object?, DbParameter> parameterFactory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseQuery);
        ArgumentNullException.ThrowIfNull(parameterFactory);

        if (filter == null || filter.Identifiers.Count == 0)
        {
            return (ReplaceMarker(baseQuery, string.Empty), []);
        }

        var whereClauses = new List<string>(filter.Identifiers.Count);
        var parameters = new List<DbParameter>(filter.Identifiers.Count * 2);

        for (var index = 0; index < filter.Identifiers.Count; index++)
        {
            var identifier = filter.Identifiers[index];

            whereClauses.Add(
                IsGlobalAssetId(identifier.Name)
                    ? BuildGlobalAssetIdClause(index, identifier, parameterFactory, parameters)
                    : BuildSpecificAssetIdClause(index, identifier, parameterFactory, parameters));
        }

        var whereClause = "WHERE " + string.Join(" AND ", whereClauses);
        return (ReplaceMarker(baseQuery, whereClause), parameters);
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
    private static bool IsGlobalAssetId(string identifierName)
        => string.Equals(identifierName, GlobalAssetId, StringComparison.Ordinal);
}

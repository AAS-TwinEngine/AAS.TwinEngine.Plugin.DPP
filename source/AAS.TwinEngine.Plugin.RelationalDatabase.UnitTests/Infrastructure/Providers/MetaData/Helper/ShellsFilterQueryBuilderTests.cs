using System.Data.Common;

using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Extensions;
using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.MetaData;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.MetaData.Helper;

using Npgsql;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.UnitTests.Infrastructure.Providers.MetaData.Helper;

public class ShellsFilterQueryBuilderTests
{
    private const string QueryWithFilterAndPaginationMarker = "SELECT * FROM \"Asset\" A\n{{__ASSET_FILTER__}}\n{{__PAGINATION__}};";

    [Fact]
    public void Build_WhenBaseQueryIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ShellsFilterQueryBuilder.Build(null!, null, null, CreateParameter));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Build_WhenBaseQueryIsEmptyOrWhitespace_ThrowsArgumentException(string baseQuery)
    {
        Assert.Throws<ArgumentException>(() =>
            ShellsFilterQueryBuilder.Build(baseQuery, null, null, CreateParameter));
    }

    [Fact]
    public void Build_WhenParameterFactoryIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ShellsFilterQueryBuilder.Build("SELECT 1;", null, null, null!));
    }

    [Fact]
    public void Build_WhenFilterIsNull_ReplacesMarkerWithEmptyClause()
    {
        const string query = QueryWithFilterAndPaginationMarker;

        var (resultQuery, parameters) = ShellsFilterQueryBuilder.Build(query, null, null, CreateParameter);

        Assert.DoesNotContain("{{__ASSET_FILTER__}}", resultQuery, StringComparison.Ordinal);
        Assert.DoesNotContain("WHERE", resultQuery, StringComparison.Ordinal);
        Assert.Single(parameters);
        AssertParameter(parameters[0], "@p_page_size", 101);
    }

    [Fact]
    public void Build_WhenFilterIdentifiersAreEmpty_ReplacesMarkerWithEmptyClause()
    {
        const string query = QueryWithFilterAndPaginationMarker;
        var filter = new AssetIdFilterHeader { Identifiers = [] };

        var (resultQuery, parameters) = ShellsFilterQueryBuilder.Build(query, filter, null, CreateParameter);

        Assert.DoesNotContain("{{__ASSET_FILTER__}}", resultQuery, StringComparison.Ordinal);
        Assert.DoesNotContain("WHERE", resultQuery, StringComparison.Ordinal);
        Assert.Single(parameters);
        AssertParameter(parameters[0], "@p_page_size", 101);
    }

    [Fact]
    public void Build_WhenFilterHasSpecificAssetId_BuildsExistsClauseAndTwoParameters()
    {
        const string query = QueryWithFilterAndPaginationMarker;
        var filter = new AssetIdFilterHeader
        {
            Identifiers =
            [
                new SpecificAssetIdsData { Name = "serialNumber", Value = "SN-4711" }
            ]
        };

        var (resultQuery, parameters) = ShellsFilterQueryBuilder.Build(query, filter, null, CreateParameter);

        Assert.Contains("WHERE", resultQuery, StringComparison.Ordinal);
        Assert.Contains("EXISTS", resultQuery, StringComparison.Ordinal);
        Assert.Contains("sai.\"Name\" =@f_name_0", resultQuery, StringComparison.Ordinal);
        Assert.Contains("sai.\"Value\" = @f_value_0", resultQuery, StringComparison.Ordinal);

        Assert.Equal(3, parameters.Count);
        AssertParameter(parameters[0], "@f_name_0", "serialNumber");
        AssertParameter(parameters[1], "@f_value_0", "SN-4711");
        AssertParameter(parameters[2], "@p_page_size", 101);
    }

    [Fact]
    public void Build_WhenFilterHasGlobalAssetId_BuildsGlobalClauseAndSingleParameter()
    {
        const string query = QueryWithFilterAndPaginationMarker;
        var filter = new AssetIdFilterHeader
        {
            Identifiers =
            [
                new SpecificAssetIdsData { Name = "globalAssetId", Value = "asset-001" }
            ]
        };

        var (resultQuery, parameters) = ShellsFilterQueryBuilder.Build(query, filter, null, CreateParameter);

        Assert.Contains("A.\"GlobalAssetId\" = @f_value_0", resultQuery, StringComparison.Ordinal);
        Assert.DoesNotContain("EXISTS", resultQuery, StringComparison.Ordinal);

        Assert.Equal(2, parameters.Count);
        AssertParameter(parameters[0], "@f_value_0", "asset-001");
        AssertParameter(parameters[1], "@p_page_size", 101);
    }

    [Fact]
    public void Build_WhenFilterHasMixedIdentifiers_CombinesClausesUsingAnd()
    {
        const string query = QueryWithFilterAndPaginationMarker;
        var filter = new AssetIdFilterHeader
        {
            Identifiers =
            [
                new SpecificAssetIdsData { Name = "globalAssetId", Value = "asset-001" },
                new SpecificAssetIdsData { Name = "serialNumber", Value = "SN-4711" }
            ]
        };

        var (resultQuery, parameters) = ShellsFilterQueryBuilder.Build(query, filter, null, CreateParameter);

        Assert.Contains("A.\"GlobalAssetId\" = @f_value_0", resultQuery, StringComparison.Ordinal);
        Assert.Contains("EXISTS", resultQuery, StringComparison.Ordinal);
        Assert.Contains("AND", resultQuery, StringComparison.Ordinal);

        Assert.Equal(4, parameters.Count);
        AssertParameter(parameters[0], "@f_value_0", "asset-001");
        AssertParameter(parameters[1], "@f_name_1", "serialNumber");
        AssertParameter(parameters[2], "@f_value_1", "SN-4711");
        AssertParameter(parameters[3], "@p_page_size", 101);
    }

    [Fact]
    public void Build_WhenIdentifierNameCaseDoesNotMatchGlobalAssetId_TreatsItAsSpecificAssetId()
    {
        const string query = QueryWithFilterAndPaginationMarker;
        var filter = new AssetIdFilterHeader
        {
            Identifiers =
            [
                new SpecificAssetIdsData { Name = "GlobalAssetId", Value = "asset-001" }
            ]
        };

        var (resultQuery, parameters) = ShellsFilterQueryBuilder.Build(query, filter, null, CreateParameter);

        Assert.Contains("EXISTS", resultQuery, StringComparison.Ordinal);
        Assert.DoesNotContain("A.\"GlobalAssetId\"", resultQuery, StringComparison.Ordinal);

        Assert.Equal(3, parameters.Count);
        AssertParameter(parameters[0], "@f_name_0", "GlobalAssetId");
        AssertParameter(parameters[1], "@f_value_0", "asset-001");
        AssertParameter(parameters[2], "@p_page_size", 101);
    }

    [Fact]
    public void Build_WhenQueryHasNoMarkerAndFilterIsEmpty_ReturnsOriginalQueryUnchanged()
    {
        const string query = "SELECT * FROM \"Asset\" A;";
        var filter = new AssetIdFilterHeader { Identifiers = [] };

        var (resultQuery, parameters) = ShellsFilterQueryBuilder.Build(query, filter, null, CreateParameter);

        Assert.Equal(query, resultQuery);
        Assert.Single(parameters);
        AssertParameter(parameters[0], "@p_page_size", 101);
    }

    [Fact]
    public void Build_WhenQueryHasNoMarkerAndFilterExists_AppendsWhereBeforeTrailingSemicolon()
    {
        const string query = "SELECT * FROM \"Asset\" A;";
        var filter = new AssetIdFilterHeader
        {
            Identifiers =
            [
                new SpecificAssetIdsData { Name = "globalAssetId", Value = "asset-001" }
            ]
        };

        var (resultQuery, parameters) = ShellsFilterQueryBuilder.Build(query, filter, null, CreateParameter);

        Assert.StartsWith("SELECT * FROM \"Asset\" A", resultQuery, StringComparison.Ordinal);
        Assert.EndsWith(";", resultQuery, StringComparison.Ordinal);
        Assert.Contains("WHERE A.\"GlobalAssetId\" = @f_value_0", resultQuery, StringComparison.Ordinal);

        Assert.Equal(2, parameters.Count);
        AssertParameter(parameters[0], "@f_value_0", "asset-001");
        AssertParameter(parameters[1], "@p_page_size", 101);
    }

    [Fact]
    public void Build_WhenQueryHasNoMarkerAndNoTrailingSemicolon_AppendsWhereAndSemicolon()
    {
        const string query = "SELECT * FROM \"Asset\" A";
        var filter = new AssetIdFilterHeader
        {
            Identifiers =
            [
                new SpecificAssetIdsData { Name = "globalAssetId", Value = "asset-001" }
            ]
        };

        var (resultQuery, parameters) = ShellsFilterQueryBuilder.Build(query, filter, null, CreateParameter);

        Assert.Contains("WHERE A.\"GlobalAssetId\" = @f_value_0", resultQuery, StringComparison.Ordinal);
        Assert.EndsWith(";", resultQuery, StringComparison.Ordinal);

        Assert.Equal(2, parameters.Count);
        AssertParameter(parameters[0], "@f_value_0", "asset-001");
        AssertParameter(parameters[1], "@p_page_size", 101);
    }

    private static DbParameter CreateParameter(string name, object? value)
        => new NpgsqlParameter(name, value ?? DBNull.Value);

    private static void AssertParameter(DbParameter parameter, string expectedName, object expectedValue)
    {
        Assert.Equal(expectedName, parameter.ParameterName);
        Assert.Equal(expectedValue, parameter.Value);
    }

    [Fact]
    public void Build_WhenIdShortIsProvided_AppendsIdShortClause()
    {
        const string query = QueryWithFilterAndPaginationMarker;
        const string idShort = "M&M03";

        var (resultQuery, parameters) = ShellsFilterQueryBuilder.Build(query, null, idShort, CreateParameter);

        Assert.Contains("WHERE", resultQuery, StringComparison.Ordinal);
        Assert.Contains("A.\"IdShort\" = @idShort", resultQuery, StringComparison.Ordinal);
        Assert.DoesNotContain("EXISTS", resultQuery, StringComparison.Ordinal);

        Assert.Equal(2, parameters.Count);
        AssertParameter(parameters[0], "@idShort", idShort);
        AssertParameter(parameters[1], "@p_page_size", 101);
    }

    [Fact]
    public void Build_WhenIdShortAndAssetFilter_CombinesBothClauses()
    {
        const string query = QueryWithFilterAndPaginationMarker;
        const string idShort = "M&M03";
        var filter = new AssetIdFilterHeader
        {
            Identifiers =
            [
                new SpecificAssetIdsData { Name = "serialNumber", Value = "SN-4711" }
            ]
        };

        var (resultQuery, parameters) = ShellsFilterQueryBuilder.Build(query, filter, idShort, CreateParameter);

        Assert.Contains("WHERE", resultQuery, StringComparison.Ordinal);
        Assert.Contains("EXISTS", resultQuery, StringComparison.Ordinal);
        Assert.Contains("AND", resultQuery, StringComparison.Ordinal);
        Assert.Contains("A.\"IdShort\" = @idShort", resultQuery, StringComparison.Ordinal);

        Assert.Equal(4, parameters.Count);
        AssertParameter(parameters[0], "@f_name_0", "serialNumber");
        AssertParameter(parameters[1], "@f_value_0", "SN-4711");
        AssertParameter(parameters[2], "@idShort", idShort);
        AssertParameter(parameters[3], "@p_page_size", 101);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Build_WhenIdShortIsNullOrEmpty_NoIdShortClause(string? idShort)
    {
        const string query = QueryWithFilterAndPaginationMarker;

        var (resultQuery, parameters) = ShellsFilterQueryBuilder.Build(query, null, idShort, CreateParameter);

        Assert.DoesNotContain("IdShort", resultQuery, StringComparison.Ordinal);
        Assert.DoesNotContain("WHERE", resultQuery, StringComparison.Ordinal);
        Assert.Single(parameters);
        AssertParameter(parameters[0], "@p_page_size", 101);
    }

    [Fact]
    public void Build_WhenPaginationMarkerExistsAndLimitProvided_AppendsOrderAndLimitPlusOne()
    {
        const string query = "SELECT * FROM \"Asset\" A\n{{__ASSET_FILTER__}}\n{{__PAGINATION__}};";

        var (resultQuery, parameters) = ShellsFilterQueryBuilder.Build(query, null, null, CreateParameter, null, 10);

        Assert.Contains("ORDER BY A.\"AasId\" LIMIT @p_page_size", resultQuery, StringComparison.Ordinal);
        Assert.DoesNotContain("{{__PAGINATION__}}", resultQuery, StringComparison.Ordinal);

        Assert.Single(parameters);
        AssertParameter(parameters[0], "@p_page_size", 11);
    }

    [Fact]
    public void Build_WhenPaginationMarkerExistsAndLimitMissing_UsesDefaultPageSizePlusOne()
    {
        const string query = "SELECT * FROM \"Asset\" A\n{{__ASSET_FILTER__}}\n{{__PAGINATION__}};";

        var (resultQuery, parameters) = ShellsFilterQueryBuilder.Build(query, null, null, CreateParameter);

        Assert.Contains("ORDER BY A.\"AasId\" LIMIT @p_page_size", resultQuery, StringComparison.Ordinal);

        Assert.Single(parameters);
        AssertParameter(parameters[0], "@p_page_size", 101);
    }

    [Fact]
    public void Build_WhenCursorProvided_AddsKeysetCursorClauseAndDecodedParameter()
    {
        const string query = "SELECT * FROM \"Asset\" A\n{{__ASSET_FILTER__}}\n{{__PAGINATION__}};";
        var cursor = "aas-002".EncodeBase64();

        var (resultQuery, parameters) = ShellsFilterQueryBuilder.Build(query, null, null, CreateParameter, cursor, 5);

        Assert.Contains("WHERE A.\"AasId\" > @p_cursor", resultQuery, StringComparison.Ordinal);
        Assert.Contains("ORDER BY A.\"AasId\" LIMIT @p_page_size", resultQuery, StringComparison.Ordinal);

        Assert.Equal(2, parameters.Count);
        AssertParameter(parameters[0], "@p_cursor", "aas-002");
        AssertParameter(parameters[1], "@p_page_size", 6);
    }
}

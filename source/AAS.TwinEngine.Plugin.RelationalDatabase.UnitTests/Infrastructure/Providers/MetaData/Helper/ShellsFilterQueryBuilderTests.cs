using System.Data.Common;

using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.MetaData;
using AAS.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.MetaData.Helper;

using Npgsql;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.UnitTests.Infrastructure.Providers.MetaData.Helper;

public class ShellsFilterQueryBuilderTests
{
    [Fact]
    public void Build_WhenBaseQueryIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ShellsFilterQueryBuilder.Build(null!, null, CreateParameter));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Build_WhenBaseQueryIsEmptyOrWhitespace_ThrowsArgumentException(string baseQuery)
    {
        Assert.Throws<ArgumentException>(() =>
            ShellsFilterQueryBuilder.Build(baseQuery, null, CreateParameter));
    }

    [Fact]
    public void Build_WhenParameterFactoryIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ShellsFilterQueryBuilder.Build("SELECT 1;", null, null!));
    }

    [Fact]
    public void Build_WhenFilterIsNull_ReplacesMarkerWithEmptyClause()
    {
        const string query = "SELECT * FROM \"Asset\" A\n{{__ASSET_FILTER__}};";

        var (resultQuery, parameters) = ShellsFilterQueryBuilder.Build(query, null, CreateParameter);

        Assert.DoesNotContain("{{__ASSET_FILTER__}}", resultQuery, StringComparison.Ordinal);
        Assert.DoesNotContain("WHERE", resultQuery, StringComparison.Ordinal);
        Assert.Empty(parameters);
    }

    [Fact]
    public void Build_WhenFilterIdentifiersAreEmpty_ReplacesMarkerWithEmptyClause()
    {
        const string query = "SELECT * FROM \"Asset\" A\n{{__ASSET_FILTER__}};";
        var filter = new AssetIdFilterHeader { Identifiers = [] };

        var (resultQuery, parameters) = ShellsFilterQueryBuilder.Build(query, filter, CreateParameter);

        Assert.DoesNotContain("{{__ASSET_FILTER__}}", resultQuery, StringComparison.Ordinal);
        Assert.DoesNotContain("WHERE", resultQuery, StringComparison.Ordinal);
        Assert.Empty(parameters);
    }

    [Fact]
    public void Build_WhenFilterHasSpecificAssetId_BuildsExistsClauseAndTwoParameters()
    {
        const string query = "SELECT * FROM \"Asset\" A\n{{__ASSET_FILTER__}};";
        var filter = new AssetIdFilterHeader
        {
            Identifiers =
            [
                new SpecificAssetIdsData { Name = "serialNumber", Value = "SN-4711" }
            ]
        };

        var (resultQuery, parameters) = ShellsFilterQueryBuilder.Build(query, filter, CreateParameter);

        Assert.Contains("WHERE", resultQuery, StringComparison.Ordinal);
        Assert.Contains("EXISTS", resultQuery, StringComparison.Ordinal);
        Assert.Contains("sai.\"Name\" =@f_name_0", resultQuery, StringComparison.Ordinal);
        Assert.Contains("sai.\"Value\" = @f_value_0", resultQuery, StringComparison.Ordinal);

        Assert.Equal(2, parameters.Count);
        AssertParameter(parameters[0], "@f_name_0", "serialNumber");
        AssertParameter(parameters[1], "@f_value_0", "SN-4711");
    }

    [Fact]
    public void Build_WhenFilterHasGlobalAssetId_BuildsGlobalClauseAndSingleParameter()
    {
        const string query = "SELECT * FROM \"Asset\" A\n{{__ASSET_FILTER__}};";
        var filter = new AssetIdFilterHeader
        {
            Identifiers =
            [
                new SpecificAssetIdsData { Name = "globalAssetId", Value = "asset-001" }
            ]
        };

        var (resultQuery, parameters) = ShellsFilterQueryBuilder.Build(query, filter, CreateParameter);

        Assert.Contains("A.\"GlobalAssetId\" = @f_value_0", resultQuery, StringComparison.Ordinal);
        Assert.DoesNotContain("EXISTS", resultQuery, StringComparison.Ordinal);

        Assert.Single(parameters);
        AssertParameter(parameters[0], "@f_value_0", "asset-001");
    }

    [Fact]
    public void Build_WhenFilterHasMixedIdentifiers_CombinesClausesUsingAnd()
    {
        const string query = "SELECT * FROM \"Asset\" A\n{{__ASSET_FILTER__}};";
        var filter = new AssetIdFilterHeader
        {
            Identifiers =
            [
                new SpecificAssetIdsData { Name = "globalAssetId", Value = "asset-001" },
                new SpecificAssetIdsData { Name = "serialNumber", Value = "SN-4711" }
            ]
        };

        var (resultQuery, parameters) = ShellsFilterQueryBuilder.Build(query, filter, CreateParameter);

        Assert.Contains("A.\"GlobalAssetId\" = @f_value_0", resultQuery, StringComparison.Ordinal);
        Assert.Contains("EXISTS", resultQuery, StringComparison.Ordinal);
        Assert.Contains("AND", resultQuery, StringComparison.Ordinal);

        Assert.Equal(3, parameters.Count);
        AssertParameter(parameters[0], "@f_value_0", "asset-001");
        AssertParameter(parameters[1], "@f_name_1", "serialNumber");
        AssertParameter(parameters[2], "@f_value_1", "SN-4711");
    }

    [Fact]
    public void Build_WhenIdentifierNameCaseDoesNotMatchGlobalAssetId_TreatsItAsSpecificAssetId()
    {
        const string query = "SELECT * FROM \"Asset\" A\n{{__ASSET_FILTER__}};";
        var filter = new AssetIdFilterHeader
        {
            Identifiers =
            [
                new SpecificAssetIdsData { Name = "GlobalAssetId", Value = "asset-001" }
            ]
        };

        var (resultQuery, parameters) = ShellsFilterQueryBuilder.Build(query, filter, CreateParameter);

        Assert.Contains("EXISTS", resultQuery, StringComparison.Ordinal);
        Assert.DoesNotContain("A.\"GlobalAssetId\"", resultQuery, StringComparison.Ordinal);

        Assert.Equal(2, parameters.Count);
        AssertParameter(parameters[0], "@f_name_0", "GlobalAssetId");
        AssertParameter(parameters[1], "@f_value_0", "asset-001");
    }

    [Fact]
    public void Build_WhenQueryHasNoMarkerAndFilterIsEmpty_ReturnsOriginalQueryUnchanged()
    {
        const string query = "SELECT * FROM \"Asset\" A;";
        var filter = new AssetIdFilterHeader { Identifiers = [] };

        var (resultQuery, parameters) = ShellsFilterQueryBuilder.Build(query, filter, CreateParameter);

        Assert.Equal(query, resultQuery);
        Assert.Empty(parameters);
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

        var (resultQuery, parameters) = ShellsFilterQueryBuilder.Build(query, filter, CreateParameter);

        Assert.StartsWith("SELECT * FROM \"Asset\" A", resultQuery, StringComparison.Ordinal);
        Assert.EndsWith(";", resultQuery, StringComparison.Ordinal);
        Assert.Contains("WHERE A.\"GlobalAssetId\" = @f_value_0", resultQuery, StringComparison.Ordinal);

        Assert.Single(parameters);
        AssertParameter(parameters[0], "@f_value_0", "asset-001");
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

        var (resultQuery, parameters) = ShellsFilterQueryBuilder.Build(query, filter, CreateParameter);

        Assert.Contains("WHERE A.\"GlobalAssetId\" = @f_value_0", resultQuery, StringComparison.Ordinal);
        Assert.EndsWith(";", resultQuery, StringComparison.Ordinal);

        Assert.Single(parameters);
        AssertParameter(parameters[0], "@f_value_0", "asset-001");
    }

    private static DbParameter CreateParameter(string name, object? value)
        => new NpgsqlParameter(name, value ?? DBNull.Value);

    private static void AssertParameter(DbParameter parameter, string expectedName, object expectedValue)
    {
        Assert.Equal(expectedName, parameter.ParameterName);
        Assert.Equal(expectedValue, parameter.Value);
    }
}

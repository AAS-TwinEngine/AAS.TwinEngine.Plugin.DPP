using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Helper;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Providers;
using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

using Json.Schema;

using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using IQueryProvider = AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Shared.IQueryProvider;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.UnitTests.ApplicationLogic.Services.SubmodelData;

public class SubmodelDataServiceTests
{
    private readonly ISubmodelMetadataExtractor _submodelMetadataExtractor;
    private readonly ISemanticIdToColumnMapper _semanticIdToColumnMapper;
    private readonly ISemanticTreeResponseBuilder _semanticTreeResponseBuilder;
    private readonly IQueryProvider _queryProvider;
    private readonly ISubmodelDataProvider _submodelDataProvider;
    private readonly ILogger<SubmodelDataService> _logger;
    private readonly SubmodelDataService _sut;

    public SubmodelDataServiceTests()
    {
        _submodelMetadataExtractor = Substitute.For<ISubmodelMetadataExtractor>();
        _semanticIdToColumnMapper = Substitute.For<ISemanticIdToColumnMapper>();
        _semanticTreeResponseBuilder = Substitute.For<ISemanticTreeResponseBuilder>();
        _queryProvider = Substitute.For<IQueryProvider>();
        _submodelDataProvider = Substitute.For<ISubmodelDataProvider>();
        _logger = Substitute.For<ILogger<SubmodelDataService>>();

        _sut = new SubmodelDataService(_submodelMetadataExtractor, _semanticIdToColumnMapper, _semanticTreeResponseBuilder, _queryProvider, _submodelDataProvider, _logger);
    }

    [Fact]
    public async Task GetValuesBySemanticIds_ValidInputs_ReturnsExpectedResult()
    {
        var jsonSchema = CreateValidJsonSchema();
        const string submodelId = "test-submodel-id";
        const string productId = "product-123";
        const string sqlQuery = "SELECT * FROM TestTable";
        var extractionResult = new SubmodelIdExtractionResult(productId, SubmodelName.Nameplate);
        var responseNode = new SemanticLeafNode("responseSemanticId", DataType.String, "responseValue");
        var expectedResult = new SemanticLeafNode("resultSemanticId", DataType.String, "finalValue");
        var columnMapping = new Dictionary<string, string> { ["requestSemanticId"] = "columnName" };
        _submodelMetadataExtractor.ExtractSubmodelMetadata(submodelId).Returns(extractionResult);
        _semanticIdToColumnMapper.GetSemanticIdToColumnMapping(Arg.Any<SemanticTreeNode>()).Returns(columnMapping);
        _queryProvider.GetQuery(SubmodelName.Nameplate.ToString()).Returns(sqlQuery);
        _submodelDataProvider.GetSubmodelValuesAsync(sqlQuery, productId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SemanticTreeNode>(responseNode));
        _semanticTreeResponseBuilder.BuildResponse(Arg.Any<SemanticTreeNode>(), responseNode, columnMapping)
            .Returns(expectedResult);

        var result = await _sut.GetValuesBySemanticIds(jsonSchema, submodelId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("resultSemanticId", result.SemanticId);
    }

    [Fact]
    public async Task GetValuesBySemanticIds_CallsAllDependenciesInCorrectOrder()
    {
        var jsonSchema = CreateValidJsonSchema();
        const string SubmodelId = "test-submodel-id";
        const string ProductId = "product-123";
        const string SqlQuery = "SELECT * FROM TestTable";
        var extractionResult = new SubmodelIdExtractionResult(ProductId, SubmodelName.Nameplate);
        var responseNode = new SemanticLeafNode("response", DataType.String, "value");
        var resultNode = new SemanticLeafNode("result", DataType.String, "finalValue");
        var columnMapping = new Dictionary<string, string>();
        _submodelMetadataExtractor.ExtractSubmodelMetadata(SubmodelId).Returns(extractionResult);
        _semanticIdToColumnMapper.GetSemanticIdToColumnMapping(Arg.Any<SemanticTreeNode>()).Returns(columnMapping);
        _queryProvider.GetQuery(Arg.Any<string>()).Returns(SqlQuery);
        _submodelDataProvider.GetSubmodelValuesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SemanticTreeNode>(responseNode));
        _semanticTreeResponseBuilder.BuildResponse(Arg.Any<SemanticTreeNode>(), Arg.Any<SemanticTreeNode>(), Arg.Any<Dictionary<string, string>>())
            .Returns(resultNode);

        await _sut.GetValuesBySemanticIds(jsonSchema, SubmodelId, CancellationToken.None);

        _submodelMetadataExtractor.Received(1).ExtractSubmodelMetadata(SubmodelId);
        _semanticIdToColumnMapper.Received(1).GetSemanticIdToColumnMapping(Arg.Any<SemanticTreeNode>());
        _queryProvider.Received(1).GetQuery(nameof(SubmodelName.Nameplate));
        await _submodelDataProvider.Received(1).GetSubmodelValuesAsync(SqlQuery, ProductId, Arg.Any<CancellationToken>());
        _semanticTreeResponseBuilder.Received(1).BuildResponse(Arg.Any<SemanticTreeNode>(), responseNode, columnMapping);
    }

    [Fact]
    public async Task GetValuesBySemanticIds_SqlQueryNotFound_ThrowsQueryNotAvailableException()
    {
        var jsonSchema = CreateValidJsonSchema();
        const string SubmodelId = "test-submodel-id";
        var extractionResult = new SubmodelIdExtractionResult("productId", SubmodelName.Nameplate);
        _submodelMetadataExtractor.ExtractSubmodelMetadata(SubmodelId).Returns(extractionResult);
        _semanticIdToColumnMapper.GetSemanticIdToColumnMapping(Arg.Any<SemanticTreeNode>())
            .Returns([]);
        _queryProvider.GetQuery(Arg.Any<string>()).Returns((string?)null);

        var exception = await Assert.ThrowsAsync<QueryNotAvailableException>(() => _sut.GetValuesBySemanticIds(jsonSchema, SubmodelId, CancellationToken.None));

        Assert.Contains("Internal Server Error.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetValuesBySemanticIds_SqlQueryEmpty_ThrowsQueryNotAvailableException()
    {
        var jsonSchema = CreateValidJsonSchema();
        const string SubmodelId = "test-submodel-id";
        var extractionResult = new SubmodelIdExtractionResult("productId", SubmodelName.Nameplate);
        _submodelMetadataExtractor.ExtractSubmodelMetadata(SubmodelId).Returns(extractionResult);
        _semanticIdToColumnMapper.GetSemanticIdToColumnMapping(Arg.Any<SemanticTreeNode>())
            .Returns([]);
        _queryProvider.GetQuery(Arg.Any<string>()).Returns(string.Empty);

        await Assert.ThrowsAsync<QueryNotAvailableException>(() => _sut.GetValuesBySemanticIds(jsonSchema, SubmodelId, CancellationToken.None));
    }

    [Fact]
    public async Task GetValuesBySemanticIds_SqlQueryWhitespace_ThrowsQueryNotAvailableException()
    {
        var jsonSchema = CreateValidJsonSchema();
        const string SubmodelId = "test-submodel-id";
        var extractionResult = new SubmodelIdExtractionResult("productId", SubmodelName.Nameplate);
        _submodelMetadataExtractor.ExtractSubmodelMetadata(SubmodelId).Returns(extractionResult);
        _semanticIdToColumnMapper.GetSemanticIdToColumnMapping(Arg.Any<SemanticTreeNode>())
            .Returns([]);
        _queryProvider.GetQuery(Arg.Any<string>()).Returns("   ");

        await Assert.ThrowsAsync<QueryNotAvailableException>(() => _sut.GetValuesBySemanticIds(jsonSchema, SubmodelId, CancellationToken.None));
    }

    [Fact]
    public async Task GetValuesBySemanticIds_MetadataExtractorThrows_PropagatesException()
    {
        var jsonSchema = CreateValidJsonSchema();
        const string SubmodelId = "test-submodel-id";
        _submodelMetadataExtractor.ExtractSubmodelMetadata(SubmodelId)
            .Throws(new InvalidOperationException("Extraction failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.GetValuesBySemanticIds(jsonSchema, SubmodelId, CancellationToken.None));
    }

    [Fact]
    public async Task GetValuesBySemanticIds_ColumnMapperThrows_PropagatesException()
    {
        var jsonSchema = CreateValidJsonSchema();
        const string SubmodelId = "test-submodel-id";
        var extractionResult = new SubmodelIdExtractionResult("productId", SubmodelName.Nameplate);
        _submodelMetadataExtractor.ExtractSubmodelMetadata(SubmodelId).Returns(extractionResult);
        _semanticIdToColumnMapper.GetSemanticIdToColumnMapping(Arg.Any<SemanticTreeNode>())
            .Throws(new InvalidOperationException("Mapping failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.GetValuesBySemanticIds(jsonSchema, SubmodelId, CancellationToken.None));
    }

    [Fact]
    public async Task GetValuesBySemanticIds_DataProviderThrows_PropagatesException()
    {
        var jsonSchema = CreateValidJsonSchema();
        const string SubmodelId = "test-submodel-id";
        const string SqlQuery = "SELECT * FROM TestTable";
        var extractionResult = new SubmodelIdExtractionResult("productId", SubmodelName.Nameplate);
        _submodelMetadataExtractor.ExtractSubmodelMetadata(SubmodelId).Returns(extractionResult);
        _semanticIdToColumnMapper.GetSemanticIdToColumnMapping(Arg.Any<SemanticTreeNode>())
            .Returns([]);
        _queryProvider.GetQuery(Arg.Any<string>()).Returns(SqlQuery);
        _submodelDataProvider.GetSubmodelValuesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Data provider failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.GetValuesBySemanticIds(jsonSchema, SubmodelId, CancellationToken.None));
    }

    [Fact]
    public async Task GetValuesBySemanticIds_ResponseBuilderThrows_PropagatesException()
    {
        var jsonSchema = CreateValidJsonSchema();
        const string SubmodelId = "test-submodel-id";
        const string SqlQuery = "SELECT * FROM TestTable";
        var extractionResult = new SubmodelIdExtractionResult("productId", SubmodelName.Nameplate);
        var responseNode = new SemanticLeafNode("response", DataType.String, "value");
        _submodelMetadataExtractor.ExtractSubmodelMetadata(SubmodelId).Returns(extractionResult);
        _semanticIdToColumnMapper.GetSemanticIdToColumnMapping(Arg.Any<SemanticTreeNode>())
            .Returns([]);
        _queryProvider.GetQuery(Arg.Any<string>()).Returns(SqlQuery);
        _submodelDataProvider.GetSubmodelValuesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SemanticTreeNode>(responseNode));
        _semanticTreeResponseBuilder.BuildResponse(Arg.Any<SemanticTreeNode>(), Arg.Any<SemanticTreeNode>(), Arg.Any<Dictionary<string, string>>())
            .Throws(new InvalidOperationException("Response builder failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.GetValuesBySemanticIds(jsonSchema, SubmodelId, CancellationToken.None));
    }

    [Fact]
    public async Task GetValuesBySemanticIds_CancellationRequested_PropagatesCancellation()
    {
        var jsonSchema = CreateValidJsonSchema();
        const string SubmodelId = "test-submodel-id";
        const string SqlQuery = "SELECT * FROM TestTable";
        var extractionResult = new SubmodelIdExtractionResult("productId", SubmodelName.Nameplate);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        _submodelMetadataExtractor.ExtractSubmodelMetadata(SubmodelId).Returns(extractionResult);
        _semanticIdToColumnMapper.GetSemanticIdToColumnMapping(Arg.Any<SemanticTreeNode>())
            .Returns([]);
        _queryProvider.GetQuery(Arg.Any<string>()).Returns(SqlQuery);
        _submodelDataProvider.GetSubmodelValuesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(() => _sut.GetValuesBySemanticIds(jsonSchema, SubmodelId, cts.Token));
    }

    [Fact]
    public async Task GetValuesBySemanticIds_PassesCancellationTokenToProvider()
    {
        var jsonSchema = CreateValidJsonSchema();
        const string SubmodelId = "test-submodel-id";
        const string SqlQuery = "SELECT * FROM TestTable";
        var extractionResult = new SubmodelIdExtractionResult("productId", SubmodelName.Nameplate);
        var responseNode = new SemanticLeafNode("response", DataType.String, "value");
        var resultNode = new SemanticLeafNode("result", DataType.String, "finalValue");
        using var cts = new CancellationTokenSource();
        _submodelMetadataExtractor.ExtractSubmodelMetadata(SubmodelId).Returns(extractionResult);
        _semanticIdToColumnMapper.GetSemanticIdToColumnMapping(Arg.Any<SemanticTreeNode>())
            .Returns([]);
        _queryProvider.GetQuery(Arg.Any<string>()).Returns(SqlQuery);
        _submodelDataProvider.GetSubmodelValuesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SemanticTreeNode>(responseNode));
        _semanticTreeResponseBuilder.BuildResponse(Arg.Any<SemanticTreeNode>(), Arg.Any<SemanticTreeNode>(), Arg.Any<Dictionary<string, string>>())
            .Returns(resultNode);

        await _sut.GetValuesBySemanticIds(jsonSchema, SubmodelId, cts.Token);

        await _submodelDataProvider.Received(1).GetSubmodelValuesAsync(SqlQuery, "productId", cts.Token);
    }

    [Theory]
    [InlineData(SubmodelName.Nameplate)]
    [InlineData(SubmodelName.ContactInformation)]
    public async Task GetValuesBySemanticIds_DifferentSubmodelNames_QueriesCorrectSubmodel(SubmodelName submodelName)
    {
        var jsonSchema = CreateValidJsonSchema();
        const string SubmodelId = "test-submodel-id";
        const string SqlQuery = "SELECT * FROM TestTable";
        var extractionResult = new SubmodelIdExtractionResult("productId", submodelName);
        var responseNode = new SemanticLeafNode("response", DataType.String, "value");
        var resultNode = new SemanticLeafNode("result", DataType.String, "finalValue");
        _submodelMetadataExtractor.ExtractSubmodelMetadata(SubmodelId).Returns(extractionResult);
        _semanticIdToColumnMapper.GetSemanticIdToColumnMapping(Arg.Any<SemanticTreeNode>())
            .Returns([]);
        _queryProvider.GetQuery(Arg.Any<string>()).Returns(SqlQuery);
        _submodelDataProvider.GetSubmodelValuesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SemanticTreeNode>(responseNode));
        _semanticTreeResponseBuilder.BuildResponse(Arg.Any<SemanticTreeNode>(), Arg.Any<SemanticTreeNode>(), Arg.Any<Dictionary<string, string>>())
            .Returns(resultNode);

        await _sut.GetValuesBySemanticIds(jsonSchema, SubmodelId, CancellationToken.None);

        _queryProvider.Received(1).GetQuery(submodelName.ToString());
    }

    private static JsonSchema CreateValidJsonSchema()
    {
        return new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["testProperty"] = new JsonSchemaBuilder()
                    .Type(SchemaValueType.String)
                    .Build()
            })
            .Build();
    }
}

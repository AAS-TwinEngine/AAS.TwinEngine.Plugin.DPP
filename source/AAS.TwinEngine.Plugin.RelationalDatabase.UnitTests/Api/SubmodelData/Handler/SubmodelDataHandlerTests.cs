using System.Text;
using System.Text.Json.Nodes;

using AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Handler;
using AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Requests;
using AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Services;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;
using AAS.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

using Json.Schema;

using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.UnitTests.Api.SubmodelData.Handler;

public class SubmodelDataHandlerTests
{
    private readonly ILogger<SubmodelDataHandler> _logger = Substitute.For<ILogger<SubmodelDataHandler>>();
    private readonly ISubmodelDataService _submodelDataService = Substitute.For<ISubmodelDataService>();
    private readonly IJsonSchemaValidator _jsonSchemaValidator = Substitute.For<IJsonSchemaValidator>();
    private readonly ISemanticTreeHandler _semanticTreeHandler = Substitute.For<ISemanticTreeHandler>();
    private readonly SubmodelDataHandler _sut;

    public SubmodelDataHandlerTests() => _sut = new SubmodelDataHandler(_logger, _submodelDataService, _jsonSchemaValidator, _semanticTreeHandler);

    [Fact]
    public async Task GetSubmodelData_ShouldReturnJsonObject_WhenRequestIsValid()
    {
        var encodedSubmodelId = Convert.ToBase64String(Encoding.UTF8.GetBytes("test-submodel-id"));
        var dataQuery = new JsonSchemaBuilder().Type(SchemaValueType.Object).Build();
        var request = new GetSubmodelDataRequest(encodedSubmodelId, dataQuery);
        var semanticTreeNode = new SemanticLeafNode("testId", DataType.String, "testValue");
        var expectedJsonObject = new JsonObject { ["result"] = "testValue" };

        _submodelDataService.GetValuesBySemanticIds(dataQuery, "test-submodel-id", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SemanticTreeNode>(semanticTreeNode));

        _semanticTreeHandler.GetJson(semanticTreeNode, dataQuery)
            .Returns(expectedJsonObject);

        var result = await _sut.GetSubmodelData(request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(expectedJsonObject.ToString(), result.ToString());
        _jsonSchemaValidator.Received(1).ValidateRequestSchema(dataQuery);
        await _submodelDataService.Received(1).GetValuesBySemanticIds(dataQuery, "test-submodel-id", Arg.Any<CancellationToken>());
        _semanticTreeHandler.Received(1).GetJson(semanticTreeNode, dataQuery);
    }

    [Fact]
    public async Task GetSubmodelData_ShouldDecodeSubmodelId_BeforeProcessing()
    {
        var decodedSubmodelId = "decoded-submodel-id";
        var encodedSubmodelId = Convert.ToBase64String(Encoding.UTF8.GetBytes(decodedSubmodelId));
        var dataQuery = new JsonSchemaBuilder().Type(SchemaValueType.Object).Build();
        var request = new GetSubmodelDataRequest(encodedSubmodelId, dataQuery);
        var semanticTreeNode = new SemanticLeafNode("testId", DataType.String, "testValue");
        var expectedJsonObject = new JsonObject { ["result"] = "testValue" };

        _submodelDataService.GetValuesBySemanticIds(dataQuery, decodedSubmodelId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SemanticTreeNode>(semanticTreeNode));

        _semanticTreeHandler.GetJson(semanticTreeNode, dataQuery)
            .Returns(expectedJsonObject);

        await _sut.GetSubmodelData(request, CancellationToken.None);

        await _submodelDataService.Received(1).GetValuesBySemanticIds(dataQuery, decodedSubmodelId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSubmodelData_ShouldValidateRequestSchema_BeforeProcessing()
    {
        var encodedSubmodelId = Convert.ToBase64String(Encoding.UTF8.GetBytes("test-submodel-id"));
        var dataQuery = new JsonSchemaBuilder().Type(SchemaValueType.Object).Build();
        var request = new GetSubmodelDataRequest(encodedSubmodelId, dataQuery);
        var semanticTreeNode = new SemanticLeafNode("testId", DataType.String, "testValue");
        var expectedJsonObject = new JsonObject { ["result"] = "testValue" };

        _submodelDataService.GetValuesBySemanticIds(dataQuery, "test-submodel-id", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SemanticTreeNode>(semanticTreeNode));

        _semanticTreeHandler.GetJson(semanticTreeNode, dataQuery)
            .Returns(expectedJsonObject);

        await _sut.GetSubmodelData(request, CancellationToken.None);

        _jsonSchemaValidator.Received(1).ValidateRequestSchema(dataQuery);
    }

    [Fact]
    public async Task GetSubmodelData_ShouldThrowException_WhenValidationFails()
    {
        var encodedSubmodelId = Convert.ToBase64String(Encoding.UTF8.GetBytes("test-submodel-id"));
        var dataQuery = new JsonSchemaBuilder().Type(SchemaValueType.Object).Build();
        var request = new GetSubmodelDataRequest(encodedSubmodelId, dataQuery);

        _jsonSchemaValidator.When(x => x.ValidateRequestSchema(dataQuery))
            .Do(_ => throw new InvalidOperationException("Schema validation failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.GetSubmodelData(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelData_ShouldThrowException_WhenServiceThrows()
    {
        var encodedSubmodelId = Convert.ToBase64String(Encoding.UTF8.GetBytes("test-submodel-id"));
        var dataQuery = new JsonSchemaBuilder().Type(SchemaValueType.Object).Build();
        var request = new GetSubmodelDataRequest(encodedSubmodelId, dataQuery);

        _submodelDataService.GetValuesBySemanticIds(dataQuery, "test-submodel-id", Arg.Any<CancellationToken>())
            .Throws(new Exception("Service failure"));

        await Assert.ThrowsAsync<Exception>(() => _sut.GetSubmodelData(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelData_ShouldThrowException_WhenSemanticTreeHandlerThrows()
    {
        var encodedSubmodelId = Convert.ToBase64String(Encoding.UTF8.GetBytes("test-submodel-id"));
        var dataQuery = new JsonSchemaBuilder().Type(SchemaValueType.Object).Build();
        var request = new GetSubmodelDataRequest(encodedSubmodelId, dataQuery);
        var semanticTreeNode = new SemanticLeafNode("testId", DataType.String, "testValue");

        _submodelDataService.GetValuesBySemanticIds(dataQuery, "test-submodel-id", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SemanticTreeNode>(semanticTreeNode));

        _semanticTreeHandler.GetJson(semanticTreeNode, dataQuery)
            .Throws(new InvalidOperationException("Conversion failed"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.GetSubmodelData(request, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelData_ShouldLogInformation_WhenProcessingRequest()
    {
        var encodedSubmodelId = Convert.ToBase64String(Encoding.UTF8.GetBytes("test-submodel-id"));
        var dataQuery = new JsonSchemaBuilder().Type(SchemaValueType.Object).Build();
        var request = new GetSubmodelDataRequest(encodedSubmodelId, dataQuery);
        var semanticTreeNode = new SemanticLeafNode("testId", DataType.String, "testValue");
        var expectedJsonObject = new JsonObject { ["result"] = "testValue" };

        _submodelDataService.GetValuesBySemanticIds(dataQuery, "test-submodel-id", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SemanticTreeNode>(semanticTreeNode));

        _semanticTreeHandler.GetJson(semanticTreeNode, dataQuery)
            .Returns(expectedJsonObject);

        await _sut.GetSubmodelData(request, CancellationToken.None);
    }

    [Fact]
    public async Task GetSubmodelData_ShouldHandleCancellation_WhenTokenIsCancelled()
    {
        var encodedSubmodelId = Convert.ToBase64String(Encoding.UTF8.GetBytes("test-submodel-id"));
        var dataQuery = new JsonSchemaBuilder().Type(SchemaValueType.Object).Build();
        var request = new GetSubmodelDataRequest(encodedSubmodelId, dataQuery);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        _submodelDataService.GetValuesBySemanticIds(dataQuery, "test-submodel-id", Arg.Any<CancellationToken>())
            .Throws(new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(() => _sut.GetSubmodelData(request, cts.Token));
    }

    [Fact]
    public async Task GetSubmodelData_ShouldThrowNotFoundException_WhenSubmodelIdIsNull()
    {
        var dataQuery = new JsonSchemaBuilder().Type(SchemaValueType.Object).Build();
        var request = new GetSubmodelDataRequest(null!, dataQuery);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetSubmodelData(request, CancellationToken.None));
    }

    private static string Encode(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

    [Fact]
    public async Task GetSubmodelDataAsync_ShouldReturnResponses_ForEachDecodedSubmodelId()
    {
        var dataQuery = new JsonSchemaBuilder().Type(SchemaValueType.Object).Build();
        var encodedId1 = Encode("submodel-1");
        var encodedId2 = Encode("submodel-2");
        var batchRequest = new GetSubmodelDataBatchRequest([encodedId1, encodedId2], dataQuery);
        var tree1 = new SemanticLeafNode("id1", DataType.String, "value1");
        var tree2 = new SemanticLeafNode("id2", DataType.String, "value2");
        var json1 = new JsonObject { ["result"] = "value1" };
        var json2 = new JsonObject { ["result"] = "value2" };

        _submodelDataService
            .GetValuesBySemanticIds(dataQuery, Arg.Is<IReadOnlyList<string>>(ids => ids.SequenceEqual(new[] { "submodel-1", "submodel-2" })), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyDictionary<string, SemanticTreeNode>>(new Dictionary<string, SemanticTreeNode>
            {
                ["submodel-1"] = tree1,
                ["submodel-2"] = tree2
            }));

        _semanticTreeHandler.GetJson(tree1, dataQuery, validateResponse: false).Returns(json1);
        _semanticTreeHandler.GetJson(tree2, dataQuery, validateResponse: false).Returns(json2);

        var result = await _sut.GetSubmodelDataAsync([batchRequest], CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.SubmodelId == "submodel-1" && r.Result == json1);
        Assert.Contains(result, r => r.SubmodelId == "submodel-2" && r.Result == json2);
    }

    [Fact]
    public async Task GetSubmodelDataAsync_ShouldSkipValidation_WhenBuildingBatchResponses()
    {
        var dataQuery = new JsonSchemaBuilder().Type(SchemaValueType.Object).Build();
        var encodedId = Encode("submodel-1");
        var batchRequest = new GetSubmodelDataBatchRequest([encodedId], dataQuery);
        var tree = new SemanticLeafNode("id1", DataType.String, "value1");

        _submodelDataService
            .GetValuesBySemanticIds(dataQuery, Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyDictionary<string, SemanticTreeNode>>(new Dictionary<string, SemanticTreeNode>
            {
                ["submodel-1"] = tree
            }));

        await _sut.GetSubmodelDataAsync([batchRequest], CancellationToken.None);

        _semanticTreeHandler.Received(1).GetJson(tree, dataQuery, validateResponse: false);
    }

    [Fact]
    public async Task GetSubmodelDataAsync_ShouldDeduplicateRepeatedSubmodelIdsWithinAGroup()
    {
        var dataQuery = new JsonSchemaBuilder().Type(SchemaValueType.Object).Build();
        var encodedId = Encode("submodel-1");
        var batchRequest = new GetSubmodelDataBatchRequest([encodedId, encodedId], dataQuery);
        var tree = new SemanticLeafNode("id1", DataType.String, "value1");
        var json = new JsonObject { ["result"] = "value1" };

        _submodelDataService
            .GetValuesBySemanticIds(dataQuery, Arg.Is<IReadOnlyList<string>>(ids => ids.Count == 1 && ids[0] == "submodel-1"), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyDictionary<string, SemanticTreeNode>>(new Dictionary<string, SemanticTreeNode>
            {
                ["submodel-1"] = tree
            }));
        _semanticTreeHandler.GetJson(tree, dataQuery, validateResponse: false).Returns(json);

        var result = await _sut.GetSubmodelDataAsync([batchRequest], CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetSubmodelDataAsync_ShouldThrowSubmodelDataNotFoundException_WhenServiceOmitsRequestedId()
    {
        var dataQuery = new JsonSchemaBuilder().Type(SchemaValueType.Object).Build();
        var encodedId = Encode("submodel-missing");
        var batchRequest = new GetSubmodelDataBatchRequest([encodedId], dataQuery);

        _submodelDataService
            .GetValuesBySemanticIds(dataQuery, Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyDictionary<string, SemanticTreeNode>>(new Dictionary<string, SemanticTreeNode>()));

        await Assert.ThrowsAsync<SubmodelDataNotFoundException>(() => _sut.GetSubmodelDataAsync([batchRequest], CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelDataAsync_ShouldThrowInvalidUserInputException_WhenRequestsCollectionIsEmpty()
    {
        await Assert.ThrowsAsync<InvalidUserInputException>(() =>
            _sut.GetSubmodelDataAsync([], CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelDataAsync_ShouldThrowArgumentNullException_WhenRequestsIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.GetSubmodelDataAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetSubmodelDataAsync_ShouldProcessMultipleGroupsIndependently()
    {
        var dataQuery1 = new JsonSchemaBuilder().Type(SchemaValueType.Object).Build();
        var dataQuery2 = new JsonSchemaBuilder().Type(SchemaValueType.String).Build();
        var group1 = new GetSubmodelDataBatchRequest([Encode("group1-id")], dataQuery1);
        var group2 = new GetSubmodelDataBatchRequest([Encode("group2-id")], dataQuery2);
        var tree1 = new SemanticLeafNode("id1", DataType.String, "value1");
        var tree2 = new SemanticLeafNode("id2", DataType.String, "value2");
        var json1 = new JsonObject { ["result"] = "value1" };
        var json2 = new JsonObject { ["result"] = "value2" };

        _submodelDataService
            .GetValuesBySemanticIds(dataQuery1, Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyDictionary<string, SemanticTreeNode>>(new Dictionary<string, SemanticTreeNode> { ["group1-id"] = tree1 }));
        _submodelDataService
            .GetValuesBySemanticIds(dataQuery2, Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyDictionary<string, SemanticTreeNode>>(new Dictionary<string, SemanticTreeNode> { ["group2-id"] = tree2 }));
        _semanticTreeHandler.GetJson(tree1, dataQuery1, validateResponse: false).Returns(json1);
        _semanticTreeHandler.GetJson(tree2, dataQuery2, validateResponse: false).Returns(json2);

        var result = await _sut.GetSubmodelDataAsync([group1, group2], CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.SubmodelId == "group1-id" && r.Result == json1);
        Assert.Contains(result, r => r.SubmodelId == "group2-id" && r.Result == json2);
    }

    [Fact]
    public async Task GetSubmodelDataAsync_ShouldThrowException_WhenServiceThrows()
    {
        var dataQuery = new JsonSchemaBuilder().Type(SchemaValueType.Object).Build();
        var batchRequest = new GetSubmodelDataBatchRequest([Encode("submodel-1")], dataQuery);

        _submodelDataService
            .GetValuesBySemanticIds(dataQuery, Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Service failure"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.GetSubmodelDataAsync([batchRequest], CancellationToken.None));
    }
}

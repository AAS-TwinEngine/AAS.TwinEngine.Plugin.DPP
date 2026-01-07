using System.Text;
using System.Text.Json.Nodes;

using Aas.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Handler;
using Aas.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Requests;
using Aas.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Services;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;
using Aas.TwinEngine.Plugin.RelationalDatabase.DomainModel.SubmodelData;

using Json.Schema;

using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.UnitTests.Api.SubmodelData.Handler;

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
}

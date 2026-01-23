using System.Text.Json.Nodes;

using AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData;
using AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Handler;
using AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Requests;

using Json.Schema;

using Microsoft.AspNetCore.Mvc;

using NSubstitute;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.UnitTests.Api.SubmodelData;

public class SubmodelDataControllerTests
{
    private readonly ISubmodelDataHandler _submodelDataHandler = Substitute.For<ISubmodelDataHandler>();
    private readonly SubmodelDataController _sut;
    private readonly JsonObject _expectedJsonObject = new() { ["name"] = "testValue" };
    private readonly JsonSchema _testSchema;

    public SubmodelDataControllerTests()
    {
        _sut = new SubmodelDataController(_submodelDataHandler);
        _testSchema = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchema>
            {
                ["name"] = new JsonSchemaBuilder().Type(SchemaValueType.String).Build()
            })
            .Build();
    }

    [Fact]
    public async Task RetrieveDataAsync_ShouldReturnOk_WhenDataIsAvailable()
    {
        const string submodelId = "test-submodel-id";
        _submodelDataHandler.GetSubmodelData(Arg.Any<GetSubmodelDataRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(_expectedJsonObject));

        var result = await _sut.RetrieveDataAsync(_testSchema, submodelId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(_expectedJsonObject, okResult.Value);
    }

    [Fact]
    public async Task RetrieveDataAsync_ShouldCallHandlerWithCorrectRequest()
    {
        const string submodelId = "test-submodel-id";
        _submodelDataHandler.GetSubmodelData(Arg.Any<GetSubmodelDataRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(_expectedJsonObject));

        await _sut.RetrieveDataAsync(_testSchema, submodelId, CancellationToken.None);

        await _submodelDataHandler.Received(1).GetSubmodelData(
            Arg.Is<GetSubmodelDataRequest>(req => req.submodelId == submodelId && req.dataQuery == _testSchema),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RetrieveDataAsync_ShouldThrowException_WhenHandlerThrows()
    {
        const string submodelId = "test-submodel-id";
        _submodelDataHandler.GetSubmodelData(Arg.Any<GetSubmodelDataRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<JsonObject>>(_ => throw new InvalidOperationException("Handler error"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.RetrieveDataAsync(_testSchema, submodelId, CancellationToken.None));
    }

    [Fact]
    public async Task RetrieveDataAsync_ShouldHandleCancellation_WhenTokenIsCancelled()
    {
        const string submodelId = "test-submodel-id";
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _submodelDataHandler.GetSubmodelData(Arg.Any<GetSubmodelDataRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<JsonObject>>(_ => throw new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.RetrieveDataAsync(_testSchema, submodelId, cts.Token));
    }
}

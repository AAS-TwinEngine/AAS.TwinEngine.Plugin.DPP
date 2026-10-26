using System.Text.Json.Nodes;

using AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData;
using AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Handler;
using AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Requests;
using AAS.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Responses;

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

    [Fact]
    public async Task RetrieveBatchDataAsync_ShouldReturnOk_WhenDataIsAvailable()
    {
        var requests = new List<GetSubmodelDataBatchRequest>
        {
            new(["encoded-id-1", "encoded-id-2"], _testSchema)
        };
        var expectedResults = new List<GetSubmodelDataBatchResponse>
        {
            new("encoded-id-1", _expectedJsonObject),
            new("encoded-id-2", _expectedJsonObject)
        };
        _submodelDataHandler
            .GetSubmodelDataAsync(Arg.Any<IReadOnlyCollection<GetSubmodelDataBatchRequest>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<GetSubmodelDataBatchResponse>>(expectedResults));

        var result = await _sut.RetrieveBatchDataAsync(requests, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expectedResults, okResult.Value);
    }

    [Fact]
    public async Task RetrieveBatchDataAsync_ShouldCallHandlerWithProvidedRequests()
    {
        var requests = new List<GetSubmodelDataBatchRequest>
        {
            new(["encoded-id-1"], _testSchema)
        };
        _submodelDataHandler
            .GetSubmodelDataAsync(Arg.Any<IReadOnlyCollection<GetSubmodelDataBatchRequest>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<GetSubmodelDataBatchResponse>>([]));

        await _sut.RetrieveBatchDataAsync(requests, CancellationToken.None);

        await _submodelDataHandler.Received(1).GetSubmodelDataAsync(requests, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RetrieveBatchDataAsync_ShouldThrowException_WhenHandlerThrows()
    {
        var requests = new List<GetSubmodelDataBatchRequest>
        {
            new(["encoded-id-1"], _testSchema)
        };
        _submodelDataHandler
            .GetSubmodelDataAsync(Arg.Any<IReadOnlyCollection<GetSubmodelDataBatchRequest>>(), Arg.Any<CancellationToken>())
            .Returns<Task<IReadOnlyList<GetSubmodelDataBatchResponse>>>(_ => throw new InvalidOperationException("Handler error"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.RetrieveBatchDataAsync(requests, CancellationToken.None));
    }

    [Fact]
    public async Task RetrieveBatchDataAsync_ShouldHandleCancellation_WhenTokenIsCancelled()
    {
        var requests = new List<GetSubmodelDataBatchRequest>
        {
            new(["encoded-id-1"], _testSchema)
        };
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _submodelDataHandler
            .GetSubmodelDataAsync(Arg.Any<IReadOnlyCollection<GetSubmodelDataBatchRequest>>(), Arg.Any<CancellationToken>())
            .Returns<Task<IReadOnlyList<GetSubmodelDataBatchResponse>>>(_ => throw new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.RetrieveBatchDataAsync(requests, cts.Token));
    }
}

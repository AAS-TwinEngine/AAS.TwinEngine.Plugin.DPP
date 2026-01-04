using System.Text;
using System.Text.Json;

using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.Manifest;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure.Providers.Shared;

using Microsoft.Extensions.Logging;

using NSubstitute;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.UnitTests.Infrastructure.Providers.Manifest;

public class ManifestProviderTests
{
    private readonly ILogger<ManifestProvider> _logger;
    private ManifestProvider _sut;

    public ManifestProviderTests()
    {
        _logger = Substitute.For<ILogger<ManifestProvider>>();
        MappingData.MappingJson = CreateJsonDocument("[]");
        _sut = new ManifestProvider(_logger);
    }

    [Fact]
    public void GetSupportedSemanticIds_ReturnsDistinctTrimmedLeafSemanticIds()
    {
        MappingData.MappingJson = CreateJsonDocument("""
                                                             [
                                                                 { "Column": "dbo.Products.Name", "SemanticId": [ "  sid:1  " , "sid:1.0"]},
                                                                 { "Column": "dbo.Products.Price", "SemanticId": [ "sid:2" ]},
                                                                 { "Column": "dbo.Products.Price", "SemanticId": [ "sid:2"] },
                                                                 { "Column": "dbo.Products", "SemanticId": ["sid:ignored"] },
                                                                 { "Column": "dbo.Catalog.Items.Count", "SemanticId": ["sid:ignored-too" ]},
                                                                 { "Column": "dbo.A.B.C.D", "SemanticId": ["sid:ignored-three"] },
                                                                 { "Column": "dbo.X.Y", "SemanticId": ["sid:3" ]},
                                                                 { "Column": "dbo.Z.T.V", "SemanticId": [ "   " ]},
                                                                 { "Column": "dbo.W.Q.E", "SemanticId": null }
                                                             ]
                                                     """);
        _sut = new ManifestProvider(_logger);

        var result = _sut.GetSupportedSemanticIds();

        Assert.NotNull(result);
        Assert.Equal(4, result.Count);
        Assert.Contains("sid:1.0", result);
        Assert.Contains("sid:1", result);
        Assert.Contains("sid:2", result);
        Assert.Contains("sid:3", result);
    }

    [Fact]
    public void GetSupportedSemanticIds_WhenMappingEmpty_ReturnsEmpty()
    {
        MappingData.MappingJson = CreateJsonDocument("[]");
        _sut = new ManifestProvider(_logger);

        var result = _sut.GetSupportedSemanticIds();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetSupportedSemanticIds_WhenJsonIsNotFormated_ThrowsResponseParsingException_AndLogsError()
    {
        MappingData.MappingJson = CreateInvalidJsonDocumentForDeserialization();
        _sut = new ManifestProvider(_logger);

        Assert.Throws<ResponseParsingException>(() => _sut.GetSupportedSemanticIds());
        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Failed to de-serialize mapping.json")),
            Arg.Any<JsonException>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Theory]
    [InlineData("dbo.Table.Column", true)]
    [InlineData("dbo.Table", false)]
    [InlineData("dbo.Table.Column.More", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    [InlineData("no.dots", false)]
    [InlineData("one.two.three", true)]
    public void IsLeafColumnIdentifier_Method_Gives_ResponseCorrectly(string value, bool expected)
    {
        var method = typeof(ManifestProvider).GetMethod("IsLeafColumnIdentifier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var actual = (bool)method!.Invoke(null, [value])!;

        Assert.Equal(expected, actual);
    }

    private static JsonDocument CreateJsonDocument(string json)
        => JsonDocument.Parse(Encoding.UTF8.GetBytes(json));

    private static JsonDocument CreateInvalidJsonDocumentForDeserialization()
    {
        var bytes = Encoding.UTF8.GetBytes("{}");
        return JsonDocument.Parse(bytes);
    }
}

using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Application;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Infrastructure;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Manifest;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Manifest.Config;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Manifest.Providers;

using Microsoft.Extensions.Options;

using NSubstitute;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.UnitTests.ApplicationLogic.Services.Manifest;
public class ManifestServiceTests
{
    private readonly IManifestProvider _manifestProvider;
    private readonly IOptions<Capabilities> _options;
    private ManifestService _sut;
    private static readonly string[] supportedSemanticIds = ["id-1", "id-2"];

    public ManifestServiceTests()
    {
        _manifestProvider = Substitute.For<IManifestProvider>();
        _options = Substitute.For<IOptions<Capabilities>>();
        _options.Value.Returns(new Capabilities { HasShellDescriptor = true, HasAssetInformation = true });
        _manifestProvider.GetSupportedSemanticIds().Returns(supportedSemanticIds);
        _sut = new ManifestService(_manifestProvider, _options);
    }

    [Fact]
    public void GetManifestData_ReturnsSemanticIds_And_MappedCapabilities()
    {
        var result = _sut.GetManifestData();

        Assert.NotNull(result);
        Assert.NotNull(result.SupportedSemanticIds);
        Assert.Collection(result.SupportedSemanticIds,
            x => Assert.Equal("id-1", x),
            x => Assert.Equal("id-2", x));
        Assert.NotNull(result.Capabilities);
        Assert.True(result.Capabilities.HasShellDescriptor);
        Assert.True(result.Capabilities.HasAssetInformation);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void GetManifestData_ReflectsCapabilities_FromOptions(bool hasShellDescriptor, bool hasAssetInformation)
    {
        _manifestProvider.GetSupportedSemanticIds().Returns(Array.Empty<string>());
        _options.Value.Returns(new Capabilities { HasShellDescriptor = hasShellDescriptor, HasAssetInformation = hasAssetInformation });
        _sut = new ManifestService(_manifestProvider, _options);

        var result = _sut.GetManifestData();

        Assert.NotNull(result);
        Assert.NotNull(result.Capabilities);
        Assert.Equal(hasShellDescriptor, result.Capabilities.HasShellDescriptor);
        Assert.Equal(hasAssetInformation, result.Capabilities.HasAssetInformation);
        Assert.Empty(result.SupportedSemanticIds);
    }

    [Fact]
    public void GetManifestData_WhenProviderReturnsEmpty_ReturnsEmptySemanticIds()
    {
        _manifestProvider.GetSupportedSemanticIds().Returns(Array.Empty<string>());
        _options.Value.Returns(new Capabilities { HasShellDescriptor = false, HasAssetInformation = false });
        _sut = new ManifestService(_manifestProvider, _options);

        var result = _sut.GetManifestData();

        Assert.NotNull(result);
        Assert.Empty(result.SupportedSemanticIds);
        Assert.False(result.Capabilities.HasShellDescriptor);
        Assert.False(result.Capabilities.HasAssetInformation);
    }

    [Fact]
    public void GetManifestData_WhenProviderThrows_ResponseParsingException_IsWrappedAsInternalDataProcessingException()
    {
        _manifestProvider.GetSupportedSemanticIds().Returns(_ => throw new ResponseParsingException());
        _options.Value.Returns(new Capabilities { HasShellDescriptor = true, HasAssetInformation = false });
        _sut = new ManifestService(_manifestProvider, _options);

        var ex = Assert.Throws<InternalDataProcessingException>(() => _sut.GetManifestData());
        Assert.IsType<ResponseParsingException>(ex.InnerException);
    }

    [Fact]
    public void Capabilities_AreCapturedAtConstructionTime()
    {
        _options.Value.Returns(new Capabilities { HasShellDescriptor = true, HasAssetInformation = true });
        _sut = new ManifestService(_manifestProvider, _options);

        _options.Value.Returns(new Capabilities { HasShellDescriptor = false, HasAssetInformation = false });

        var result = _sut.GetManifestData();

        Assert.True(result.Capabilities.HasShellDescriptor);
        Assert.True(result.Capabilities.HasAssetInformation);
    }
}

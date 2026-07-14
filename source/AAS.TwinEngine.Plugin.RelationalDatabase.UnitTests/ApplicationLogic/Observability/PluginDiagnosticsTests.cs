using System.Diagnostics;

using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Observability;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.UnitTests.ApplicationLogic.Observability;

public class PluginDiagnosticsTests
{
    [Fact]
    public void SourceName_ReturnsExpectedValue() => Assert.Equal("RelationalDatabasePlugin", PluginTracing.SourceName);

    [Fact]
    public void Source_IsNotNull() => Assert.NotNull(PluginTracing.Source);

    [Fact]
    public void Source_HasCorrectName() => Assert.Equal(PluginTracing.SourceName, PluginTracing.Source.Name);

    private ActivityListenerFixture CreateFixture() => new();

    [Fact]
    public void SpanNames_AreCorrect()
    {
        Assert.Equal("Validating request", PluginTracing.Spans.ValidatingRequest);
        Assert.Equal("Query execution", PluginTracing.Spans.QueryExecution);
        Assert.Equal("Filling data from database", PluginTracing.Spans.FillingDataFromDatabase);
        Assert.Equal("Validating response", PluginTracing.Spans.ValidatingResponse);
        Assert.Equal("Create mapping form request", PluginTracing.Spans.CreateMappingFormRequest);
        Assert.Equal("Fetching shell metadata", PluginTracing.Spans.FetchingShellMetadata);
        Assert.Equal("Fetching asset metadata", PluginTracing.Spans.FetchingAssetMetadata);
    }

    [Fact]
    public void AttributeNames_AreCorrect()
    {
        Assert.Equal("aas.submodel_id", PluginTracing.Attributes.SubmodelId);
        Assert.Equal("aas.shell_id", PluginTracing.Attributes.ShellId);
    }

    [Fact]
    public void StartValidatingRequest_CreatesActivity()
    {
        using var fixture = CreateFixture();
        using var activity = PluginTracing.StartValidatingRequest();

        Assert.Single(fixture.Activities);
    }

    [Fact]
    public void StartQueryExecution_CreatesActivity()
    {
        using var fixture = CreateFixture();
        using var activity = PluginTracing.StartQueryExecution();

        Assert.Single(fixture.Activities);
    }

    [Fact]
    public void StartFillingDataFromDatabase_CreatesActivityWithCorrectName()
    {
        using var fixture = CreateFixture();
        using var activity = PluginTracing.StartFillingDataFromDatabase("submodel-001");

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(PluginTracing.Spans.FillingDataFromDatabase, capturedActivity.OperationName);
    }

    [Fact]
    public void StartFillingDataFromDatabase_SetsSubmodelIdTag()
    {
        using var fixture = CreateFixture();
        const string SubmodelId = "submodel-db-123";
        using var activity = PluginTracing.StartFillingDataFromDatabase(SubmodelId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(SubmodelId, capturedActivity.GetTagItem(PluginTracing.Attributes.SubmodelId));
    }

    [Fact]
    public void StartMappingExecution_CreatesActivityWithCorrectName()
    {
        using var fixture = CreateFixture();
        using var activity = PluginTracing.StartMappingExecution("Nameplate");

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(PluginTracing.Spans.CreateMappingFormRequest, capturedActivity.OperationName);
    }

    [Fact]
    public void StartMappingExecution_SetsSubmodelIdTag()
    {
        using var fixture = CreateFixture();
        const string submodelId = "submodel-001";
        using var activity = PluginTracing.StartMappingExecution(submodelId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(submodelId, capturedActivity.GetTagItem(PluginTracing.Attributes.SubmodelId));
    }

    [Fact]
    public void StartValidatingResponse_CreatesActivity()
    {
        using var fixture = CreateFixture();
        using var activity = PluginTracing.StartValidatingResponse();

        Assert.Single(fixture.Activities);
    }

    [Fact]
    public void StartFetchingShellMetadata_CreatesActivityWithShellIdTag()
    {
        using var fixture = CreateFixture();
        const string shellId = "shell-meta-001";
        using var activity = PluginTracing.StartFetchingShellMetadata(shellId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(PluginTracing.Spans.FetchingShellMetadata, capturedActivity.OperationName);
        Assert.Equal(shellId, capturedActivity.GetTagItem(PluginTracing.Attributes.ShellId));
    }

    [Fact]
    public void StartFetchingAssetMetadata_CreatesActivityWithShellIdTag()
    {
        using var fixture = CreateFixture();
        const string assetId = "asset-meta-001";
        using var activity = PluginTracing.StartFetchingAssetMetadata(assetId);

        var capturedActivity = Assert.Single(fixture.Activities);
        Assert.Equal(PluginTracing.Spans.FetchingAssetMetadata, capturedActivity.OperationName);
        Assert.Equal(assetId, capturedActivity.GetTagItem(PluginTracing.Attributes.ShellId));
    }

    [Fact]
    public void RecordError_WithException_SetsErrorStatusWithExceptionMessage()
    {
        using var fixture = CreateFixture();
        using var activity = PluginTracing.Source.StartActivity("test-error");
        var ex = new ArgumentException("Invalid argument provided");

        activity.RecordError(ex);

        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal("Invalid argument provided", activity.StatusDescription);
    }

    [Fact]
    public void RecordError_WithException_WhenActivityIsNull_DoesNotThrow()
    {
        Activity? activity = null;
        var ex = new InvalidOperationException("Operation failed");

        // Should not throw
        activity.RecordError(ex);
        Assert.True(true);
    }

    [Fact]
    public void RecordError_WithDescription_SetsErrorStatusWithDescription()
    {
        using var fixture = CreateFixture();
        using var activity = PluginTracing.Source.StartActivity("test-error-desc");
        const string ErrorDescription = "Custom error occurred";

        activity.RecordError(ErrorDescription);

        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal(ErrorDescription, activity.StatusDescription);
    }

    [Fact]
    public void RecordError_WithDescription_WhenActivityIsNull_DoesNotThrow()
    {
        Activity? activity = null;
        const string ErrorDescription = "Custom error occurred";

        // Should not throw
        activity.RecordError(ErrorDescription);
        Assert.True(true);
    }
}

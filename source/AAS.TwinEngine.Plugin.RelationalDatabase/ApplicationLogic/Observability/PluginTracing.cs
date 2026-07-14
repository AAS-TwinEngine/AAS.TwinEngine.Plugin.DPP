using System.Diagnostics;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Observability;

public static class PluginTracing
{
    public const string SourceName = "RelationalDatabasePlugin";
    public static readonly ActivitySource Source = new(SourceName);

    public static class Spans
    {
        public const string ValidatingRequest = "Validating request";
        public const string QueryExecution = "Query execution";
        public const string FillingDataFromDatabase = "Filling data from database";
        public const string ValidatingResponse = "Validating response";
        public const string CreateMappingFormRequest = "Create mapping form request";
        public const string FetchingShellMetadata = "Fetching shell metadata";
        public const string FetchingAssetMetadata = "Fetching asset metadata";
    }

    public static class Attributes
    {
        public const string SubmodelId = "aas.submodel_id";
        public const string ShellId = "aas.shell_id";
    }

    public static Activity? StartValidatingRequest() => Source.StartActivity(Spans.ValidatingRequest);

    public static Activity? StartQueryExecution() => Source.StartActivity(Spans.QueryExecution);

    public static Activity? StartFillingDataFromDatabase(string submodelId)
    {
        var activity = Source.StartActivity(Spans.FillingDataFromDatabase);
        _ = activity?.SetTag(Attributes.SubmodelId, submodelId);
        return activity;
    }

    public static Activity? StartMappingExecution(string submodelId)
    {
        var activity = Source.StartActivity(Spans.CreateMappingFormRequest);
        _ = activity?.SetTag(Attributes.SubmodelId, submodelId);
        return activity;
    }

    public static Activity? StartValidatingResponse() => Source.StartActivity(Spans.ValidatingResponse);

    public static Activity? StartFetchingShellMetadata(string shellId)
    {
        var activity = Source.StartActivity(Spans.FetchingShellMetadata);
        _ = activity?.SetTag(Attributes.ShellId, shellId);
        return activity;
    }

    public static Activity? StartFetchingAssetMetadata(string assetId)
    {
        var activity = Source.StartActivity(Spans.FetchingAssetMetadata);
        _ = activity?.SetTag(Attributes.ShellId, assetId);
        return activity;
    }

    public static void RecordError(this Activity? activity, Exception ex)
    {
        if (activity == null)
        {
            return;
        }

        _ = activity.SetStatus(ActivityStatusCode.Error, ex.Message);
    }

    public static void RecordError(this Activity? activity, string description)
    {
        if (activity == null)
        {
            return;
        }

        _ = activity.SetStatus(ActivityStatusCode.Error, description);
    }
}

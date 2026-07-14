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

    public static Activity? StartSpan(string spanName)
        => Source.StartActivity(spanName);

    public static Activity? StartSpan(string spanName, string tagName, object? tagValue)
    {
        var activity = Source.StartActivity(spanName);
        _ = activity?.SetTag(tagName, tagValue);
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

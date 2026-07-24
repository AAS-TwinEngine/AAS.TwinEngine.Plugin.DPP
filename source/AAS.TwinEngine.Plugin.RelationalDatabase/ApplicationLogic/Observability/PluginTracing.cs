using System.Diagnostics;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Observability;

public static class PluginTracing
{
    public const string SourceName = "Plugin.RelationalDatabase";
    public static readonly ActivitySource Source = new(SourceName);

    public static class Spans
    {
        public const string ValidatingRequest = "Validating request";
        public const string QueryExecution = "Query execution";
        public const string FillingDataFromDatabase = "Filling data from database";
        public const string ValidatingResponse = "Validating response";
        public const string CreateMappingFromRequest = "Create mapping from request";
        public const string FetchingShellMetadata = "Fetching shell metadata";
        public const string FetchingAssetMetadata = "Fetching asset metadata";
        public const string FetchingData = "Fetching submodel data";
        public const string ExtractingValuesFromRequest = "Extracting values from request";
        public const string GetQuery = "Get query";
        public const string CollectingSupportedSemanticIds = "Collecting supported semantic IDs";
        public const string ExtractingTreeNodeFromRequest = "Extracting tree node from request";
    }

    public static class Attributes
    {
        public const string SubmodelId = "aas.submodel_id";
        public const string ShellId = "aas.shell_id";
        public const string ProductId = "aas.product_id";
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

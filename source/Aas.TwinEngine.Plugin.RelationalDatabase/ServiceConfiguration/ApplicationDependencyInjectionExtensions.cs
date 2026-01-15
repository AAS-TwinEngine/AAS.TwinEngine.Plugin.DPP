using System.Diagnostics.CodeAnalysis;

using Aas.TwinEngine.Plugin.RelationalDatabase.Api.Manifest.Handler;
using Aas.TwinEngine.Plugin.RelationalDatabase.Api.MetaData.Handler;
using Aas.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Handler;
using Aas.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Services;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Manifest;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.MetaData;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.ResponseBuilder;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ServiceConfiguration;

[ExcludeFromCodeCoverage]
public static class ApplicationDependencyInjectionExtensions
{
    public static void ConfigureApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _ = services.AddExceptionHandler<GlobalExceptionHandler>();
        _ = services.AddProblemDetails();

        _ = services.AddScoped<IManifestService, ManifestService>();
        _ = services.AddScoped<IManifestHandler, ManifestHandler>();

        _ = services.AddScoped<IMetaDataHandler, MetaDataHandler>();
        _ = services.AddScoped<IMetaDataService, MetaDataService>();

        _ = services.AddScoped<IResponseSemanticTreeNodeResolver, ResponseSemanticTreeNodeResolver>();
        _ = services.AddScoped<IResponseBranchNodeProcessor, ResponseBranchNodeProcessor>();
        _ = services.AddScoped<IResponseLeafNodeProcessor, ResponseLeafNodeProcessor>();
        _ = services.AddScoped<ISemanticIdToColumnMapper, SemanticIdToColumnMapper>();
        _ = services.AddScoped<ISemanticTreeResponseBuilder, SemanticTreeResponseBuilder>();
        _ = services.AddScoped<ISubmodelMetadataExtractor, SubmodelMetadataExtractor>();
        _ = services.AddScoped<ISubmodelDataHandler, SubmodelDataHandler>();
        _ = services.AddScoped<ISemanticTreeHandler, SemanticTreeHandler>();
        _ = services.AddScoped<IJsonSchemaValidator, JsonSchemaValidator>();
        _ = services.AddScoped<ISubmodelDataService, SubmodelDataService>();
    }
}

using System.Diagnostics.CodeAnalysis;

using Aas.TwinEngine.Plugin.RelationalDatabase.Api.Manifest.Handler;
using Aas.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Handler;
using Aas.TwinEngine.Plugin.RelationalDatabase.Api.SubmodelData.Services;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.Manifest;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData;
using Aas.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Services.SubmodelData.Helper;

namespace Aas.TwinEngine.Plugin.RelationalDatabase.ServiceConfiguration;

[ExcludeFromCodeCoverage]
public static class ApplicationDependencyInjectionExtensions
{
    public static void ConfigureApplication(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.AddExceptionHandler<GlobalExceptionHandler>();
        _ = services.AddProblemDetails();

        _ = services.AddScoped<ISemanticIdToColumnMapper, SemanticIdToColumnMapper>();
        _ = services.AddScoped<ISemanticTreeResponseBuilder, SemanticTreeResponseBuilder>();
        _ = services.AddScoped<ISubmodelMetadataExtractor, SubmodelMetadataExtractor>();
        _ = services.AddScoped<ISubmodelDataHandler, SubmodelDataHandler>();

        _ = services.AddScoped<ISemanticTreeHandler, SemanticTreeHandler>();
        _ = services.AddScoped<IJsonSchemaValidator, JsonSchemaValidator>();
        _ = services.AddScoped<ISubmodelDataService, SubmodelDataService>();

        _ = services.AddScoped<IManifestService, ManifestService>();
        _ = services.AddScoped<IManifestHandler, ManifestHandler>();
    }
}

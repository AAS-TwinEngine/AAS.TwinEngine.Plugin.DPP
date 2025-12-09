using Aas.TwinEngine.Plugin.RelationalDatabase.ServiceConfiguration;
using Aas.TwinEngine.Plugin.RelationalDatabase.Infrastructure;

using Asp.Versioning;

using Serilog;

namespace Aas.TwinEngine.Plugin.RelationalDatabase;

public class Program
{
    private static readonly Version ApiVersion = new(1, 0);
    private const string ApiTitle = "RelationalDatabase API";

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        _ = builder.Host.UseSerilog();
        builder.ConfigureLogging(builder.Configuration);
        builder.ConfigureCorsServices();

        _ = builder.Services.AddHttpContextAccessor();
        builder.Services.ConfigureInfrastructure(builder.Configuration);
        builder.Services.ConfigureApplication(builder.Configuration);

        _ = builder.Services.AddAuthorization();

        _ = builder.Services.AddControllers();

        _ = builder.Services.AddEndpointsApiExplorer();
        _ = builder.Services.AddOpenApiDocument(settings =>
        {
            settings.DocumentName = ApiVersion.ToString();
            settings.Title = ApiTitle;
        });

        _ = builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(ApiVersion.Major, ApiVersion.Minor);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new HeaderApiVersionReader("api-version");
        })
        .AddMvc();

        var app = builder.Build();

        _ = app.UseExceptionHandler();
        _ = app.UseHttpsRedirection();
        _ = app.UseAuthorization();

        app.UseCorsServices();
        _ = app.UseOpenApi(c => c.PostProcess = (d, _) => d.Servers.Clear());
        _ = app.UseSwaggerUI(c => c.SwaggerEndpoint($"/swagger/{ApiVersion}/swagger.json", ApiTitle));
        _ = app.MapControllers();

        app.Run();
    }
}

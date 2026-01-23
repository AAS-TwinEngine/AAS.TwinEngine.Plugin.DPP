using System.Net;

using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base;
using AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Responses;

using Microsoft.AspNetCore.Diagnostics;

using UnauthorizedAccessException = AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions.Base.UnauthorizedAccessException;

namespace AAS.TwinEngine.Plugin.RelationalDatabase.ApplicationLogic.Exceptions;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext,
                                                Exception exception,
                                                CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        logger.LogError(exception, "An unhandled exception occurred.");

        var statusCode = exception switch
        {
            BadRequestException => StatusCodes.Status400BadRequest,
            ForbiddenException => StatusCodes.Status403Forbidden,
            NotFoundException => StatusCodes.Status404NotFound,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            TimeoutException => StatusCodes.Status408RequestTimeout,
            ServiceUnavailableException => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status500InternalServerError
        };

        var traceId = httpContext.TraceIdentifier;

        var response = new ServiceErrorResponse().Create((HttpStatusCode)statusCode,
                                                         title: exception.Message,
                                                         traceId: traceId);

        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken).ConfigureAwait(false);

        return true;
    }
}

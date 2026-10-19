using KycAggregation.Api.Http;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace KycAggregation.Api.Error;

/// <summary>Turns any unhandled exception into a generic 500 response and logs the detail server-side.</summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled error processing {Method} {Path}",
            httpContext.Request.Method, httpContext.Request.Path);

        await ErrorResults.Unexpected().ExecuteAsync(httpContext);
        return true;
    }
}

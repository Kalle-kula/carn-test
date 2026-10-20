namespace KycAggregation.Api.Security;

/// <summary>Rejects requests that do not present a valid API key in the `X-Api-Key` header.</summary>
public sealed class ApiKeyMiddleware(RequestDelegate next, IReadOnlySet<string> apiKeys)
{
    public const string HeaderName = "X-Api-Key";

    public async Task InvokeAsync(HttpContext context)
    {
        var provided = context.Request.Headers[HeaderName].ToString();
        if (string.IsNullOrEmpty(provided) || !apiKeys.Contains(provided))
        {
            await Results.Json(new { error = "Unauthorized." }, statusCode: StatusCodes.Status401Unauthorized)
                .ExecuteAsync(context);
            return;
        }

        await next(context);
    }
}

public static class ApiKeyMiddlewareExtensions
{
    public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder app, IEnumerable<string> apiKeys)
    {
        var keys = apiKeys.Where(k => !string.IsNullOrWhiteSpace(k)).ToHashSet(StringComparer.Ordinal);
        if (keys.Count == 0)
        {
            throw new InvalidOperationException("At least one API key must be configured (Security:ApiKeys).");
        }

        return app.UseMiddleware<ApiKeyMiddleware>((IReadOnlySet<string>)keys);
    }
}

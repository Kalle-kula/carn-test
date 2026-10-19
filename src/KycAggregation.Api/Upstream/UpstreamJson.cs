using System.Text.Json;

namespace KycAggregation.Api.Upstream;

/// <summary>Shared JSON options for the camelCase upstream payloads.</summary>
public static class UpstreamJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };
}

namespace KycAggregation.Api.Upstream;

/// <summary>Base address for the upstream Customer Data API, bound from configuration.</summary>
public sealed class CustomerDataApiOptions
{
    public const string SectionName = "CustomerDataApi";

    public string BaseUrl { get; set; } = string.Empty;
}

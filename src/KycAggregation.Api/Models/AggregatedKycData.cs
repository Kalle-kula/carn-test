using System.Text.Json.Serialization;

namespace KycAggregation.Api.Models;

/// <summary>Aggregated KYC record returned by the public API (snake_case contract).</summary>
public sealed class AggregatedKycData
{
    [JsonPropertyName("ssn")]
    public required string Ssn { get; init; }

    [JsonPropertyName("first_name")]
    public required string FirstName { get; init; }

    [JsonPropertyName("last_name")]
    public required string LastName { get; init; }

    [JsonPropertyName("address")]
    public required string Address { get; init; }

    [JsonPropertyName("tax_country")]
    public required string TaxCountry { get; init; }

    [JsonPropertyName("phone_number")]
    public string? PhoneNumber { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("income")]
    public int? Income { get; init; }
}

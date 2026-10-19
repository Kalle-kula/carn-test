namespace KycAggregation.Api.Persistence;

/// <summary>Cached KYC record. Keyed by a one-way SSN hash; all PII columns are encrypted at rest.</summary>
public sealed class KycRecordEntity
{
    public required string SsnHash { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Address { get; set; }
    public required string TaxCountry { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public int? Income { get; set; }
}

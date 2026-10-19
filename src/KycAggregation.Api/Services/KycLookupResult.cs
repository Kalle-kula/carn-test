using KycAggregation.Api.Models;

namespace KycAggregation.Api.Services;

/// <summary>Outcome of a KYC lookup, mapped to HTTP status by the endpoint.</summary>
public abstract record KycLookupResult
{
    public sealed record Found(AggregatedKycData Data) : KycLookupResult;

    public sealed record NotFound : KycLookupResult;

    public sealed record Incomplete(IReadOnlyList<string> MissingFields) : KycLookupResult;
}

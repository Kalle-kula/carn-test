using KycAggregation.Api.Models;

namespace KycAggregation.Api.Mapping;

/// <summary>Outcome of mapping upstream data: either a complete record or the list of missing required fields.</summary>
public abstract record KycMappingResult
{
    public sealed record Complete(AggregatedKycData Data) : KycMappingResult;

    public sealed record Incomplete(IReadOnlyList<string> MissingFields) : KycMappingResult;
}

using KycAggregation.Api.Mapping;
using KycAggregation.Api.Persistence;
using KycAggregation.Api.Upstream;

namespace KycAggregation.Api.Services;

public interface IKycAggregationService
{
    Task<KycLookupResult> GetAsync(string ssn, CancellationToken ct);
}

/// <summary>Read-through aggregation: serve from cache, otherwise fetch, map, validate, and cache.</summary>
public sealed class KycAggregationService(
    IKycRepository repository,
    ICustomerDataClient client,
    TimeProvider clock) : IKycAggregationService
{
    public async Task<KycLookupResult> GetAsync(string ssn, CancellationToken ct)
    {
        var cached = await repository.GetAsync(ssn, ct);
        if (cached is not null)
        {
            return new KycLookupResult.Found(cached);
        }

        var asOf = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var bundle = await client.GetAsync(ssn, asOf, ct);
        if (bundle is null)
        {
            return new KycLookupResult.NotFound();
        }

        return KycMapper.Map(ssn, bundle) switch
        {
            KycMappingResult.Incomplete incomplete => new KycLookupResult.Incomplete(incomplete.MissingFields),
            KycMappingResult.Complete complete => await Persist(complete.Data, ct),
            _ => throw new InvalidOperationException("Unknown mapping result."),
        };
    }

    private async Task<KycLookupResult> Persist(Models.AggregatedKycData data, CancellationToken ct)
    {
        await repository.SaveAsync(data, ct);
        return new KycLookupResult.Found(data);
    }
}

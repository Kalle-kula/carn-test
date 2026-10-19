using KycAggregation.Api.Http;
using KycAggregation.Api.Persistence;
using KycAggregation.Api.Services;
using KycAggregation.Api.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace KycAggregation.Api.Api;

public static class KycEndpoints
{
    public static void MapKycEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/kyc-data/{ssn}", GetKycData);
    }

    private static async Task<IResult> GetKycData(
        string ssn,
        IKycAggregationService service,
        SsnHasher hasher,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("KycAggregation.Api.Kyc");

        if (!SsnValidator.IsValid(ssn))
        {
            return ErrorResults.InvalidSsn();
        }

        // Pseudonymous, non-reversible correlation id - keeps the SSN out of the logs.
        var subject = hasher.Hash(ssn)[..12];
        var result = await service.GetAsync(ssn, ct);

        switch (result)
        {
            case KycLookupResult.Found found:
                logger.LogInformation("KYC lookup served for subject {Subject}", subject);
                return Results.Ok(found.Data);

            case KycLookupResult.NotFound:
                logger.LogInformation("KYC lookup not found for subject {Subject}", subject);
                return ErrorResults.NotFound();

            case KycLookupResult.Incomplete incomplete:
                logger.LogError(
                    "Incomplete KYC for subject {Subject}: missing required field(s) {MissingFields}",
                    subject, string.Join(", ", incomplete.MissingFields));
                return ErrorResults.Incomplete(incomplete.MissingFields);

            default:
                return ErrorResults.Unexpected();
        }
    }
}

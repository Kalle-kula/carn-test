using Microsoft.AspNetCore.Http;

namespace KycAggregation.Api.Http;

/// <summary>Single place for the client error contract: `{ "error": "..." }` with the taxonomy's status codes.</summary>
public static class ErrorResults
{
    public static IResult InvalidSsn() =>
        Error(StatusCodes.Status400BadRequest, "Invalid SSN format.");

    public static IResult NotFound() =>
        Error(StatusCodes.Status404NotFound, "Customer data not found for the provided SSN.");

    public static IResult Incomplete(IReadOnlyList<string> missingFields) =>
        Error(StatusCodes.Status422UnprocessableEntity,
            $"KYC data is incomplete: missing required field(s): {string.Join(", ", missingFields)}.");

    public static IResult Unexpected() =>
        Error(StatusCodes.Status500InternalServerError,
            "An unexpected error occurred while processing the request.");

    private static IResult Error(int statusCode, string message) =>
        Results.Json(new { error = message }, statusCode: statusCode);
}

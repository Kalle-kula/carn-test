using System.Text.RegularExpressions;

namespace KycAggregation.Api.Validation;

/// <summary>Validates the Swedish SSN format (YYYYMMDD-NNNN) before any upstream call.</summary>
public static partial class SsnValidator
{
    [GeneratedRegex(@"^\d{8}-\d{4}$")]
    private static partial Regex SsnPattern();

    public static bool IsValid(string? ssn) => !string.IsNullOrEmpty(ssn) && SsnPattern().IsMatch(ssn);
}

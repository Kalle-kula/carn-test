using System.Globalization;
using KycAggregation.Api.Models;
using KycAggregation.Api.Upstream;

namespace KycAggregation.Api.Mapping;

/// <summary>Pure mapping from the three upstream payloads to the aggregated KYC contract.</summary>
public static class KycMapper
{
    public static KycMappingResult Map(string ssn, CustomerDataBundle bundle)
    {
        var form = KycFormLookup(bundle.KycForm);

        var firstName = NullIfBlank(bundle.Personal.FirstName);
        var lastName = NullIfBlank(bundle.Personal.SurName);
        var address = NullIfBlank(bundle.Personal.Address);
        var taxCountry = NullIfBlank(form.GetValueOrDefault("tax_country"));
        var phone = PreferredOrFirst(bundle.Contact?.PhoneNumbers, p => p.Preferred)?.Number;
        var email = PreferredOrFirst(bundle.Contact?.Emails, e => e.Preferred)?.EmailAddress;
        var income = ParseIncome(form.GetValueOrDefault("annual_income"));

        var missing = new List<string>();
        AddIfMissing(missing, "ssn", NullIfBlank(ssn));
        AddIfMissing(missing, "first_name", firstName);
        AddIfMissing(missing, "last_name", lastName);
        AddIfMissing(missing, "address", address);
        AddIfMissing(missing, "tax_country", taxCountry);

        if (missing.Count > 0)
        {
            return new KycMappingResult.Incomplete(missing);
        }

        return new KycMappingResult.Complete(new AggregatedKycData
        {
            Ssn = ssn,
            FirstName = firstName!,
            LastName = lastName!,
            Address = address!,
            TaxCountry = taxCountry!,
            PhoneNumber = phone,
            Email = email,
            Income = income,
        });
    }

    // KYC form keys arrive with inconsistent casing; match them case-insensitively.
    private static Dictionary<string, string?> KycFormLookup(KycForm? form)
    {
        var lookup = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in form?.Items ?? [])
        {
            if (!string.IsNullOrWhiteSpace(item.Key))
            {
                lookup[item.Key] = item.Value;
            }
        }

        return lookup;
    }

    private static T? PreferredOrFirst<T>(IReadOnlyList<T>? items, Func<T, bool> preferred)
        where T : class
        => items is null ? null : items.FirstOrDefault(preferred) ?? items.FirstOrDefault();

    private static int? ParseIncome(string? value)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;

    private static void AddIfMissing(List<string> missing, string field, string? value)
    {
        if (value is null)
        {
            missing.Add(field);
        }
    }
}

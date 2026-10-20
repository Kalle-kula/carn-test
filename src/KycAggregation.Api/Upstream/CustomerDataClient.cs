using System.Net;

namespace KycAggregation.Api.Upstream;

/// <summary>The three upstream payloads for a customer. Null sub-parts mean that source had no data.</summary>
public sealed record CustomerDataBundle(PersonalDetails Personal, ContactDetails? Contact, KycForm? KycForm);

public interface ICustomerDataClient
{
    /// <summary>Returns the customer's upstream data, or null when the customer does not exist.</summary>
    Task<CustomerDataBundle?> GetAsync(string ssn, DateOnly asOf, CancellationToken ct);
}

public sealed class CustomerDataClient(HttpClient http) : ICustomerDataClient
{
    public async Task<CustomerDataBundle?> GetAsync(string ssn, DateOnly asOf, CancellationToken ct)
    {
        var encoded = Uri.EscapeDataString(ssn);

        // Personal details is the anchor: its absence means the customer does not exist.
        var personal = await GetOrNullAsync<PersonalDetails>($"/personal-details/{encoded}", ct);
        if (personal is null)
        {
            return null;
        }

        var contact = await GetOrNullAsync<ContactDetails>($"/contact-details/{encoded}", ct);
        var form = await GetOrNullAsync<KycForm>($"/kyc-form/{encoded}/{asOf:yyyy-MM-dd}", ct);

        return new CustomerDataBundle(personal, contact, form);
    }

    private async Task<T?> GetOrNullAsync<T>(string path, CancellationToken ct) where T : class
    {
        using var response = await http.GetAsync(path, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(UpstreamJson.Options, ct);
    }
}

using System.Net;
using System.Text.Json;
using KycAggregation.Api.Upstream;

namespace KycAggregation.Api.Tests;

public sealed class IntegrationVerificationTests
{
    // Mirrors the grounded upstream data for the five test SSNs.
    private static CustomerDataBundle? Bundle(string ssn) => ssn switch
    {
        "19800115-1234" => Complete("Erik", "Svensson", taxCountry: null, income: "550000"),   // tax_country missing
        "19900220-5678" => Complete("Anna", "Johansson", taxCountry: "SE", income: "420000"),
        "19751230-9101" => Complete("Lars", "Nielsen", taxCountry: "DK", income: "680000"),
        "19850505-4321" => Complete("Maria", "Andersson", taxCountry: "SE", income: "510000"),
        "19951212-3456" => Complete("Johan", "Karlsson", taxCountry: "SE", income: null),        // income missing (optional)
        _ => null,
    };

    private static CustomerDataBundle Complete(string first, string last, string? taxCountry, string? income)
    {
        var items = new List<KycItem>();
        if (taxCountry is not null) items.Add(new KycItem("tax_country", taxCountry));
        if (income is not null) items.Add(new KycItem("annual_income", income));
        return new CustomerDataBundle(
            new PersonalDetails(first, last, "Storgatan 1, 111 22 Stockholm"),
            new ContactDetails(null, [new EmailContact(true, "person@example.com")], [new PhoneContact(true, "+46700000000")]),
            new KycForm(items));
    }

    [Theory]
    [InlineData("19900220-5678", "SE")]
    [InlineData("19751230-9101", "DK")]
    [InlineData("19850505-4321", "SE")]
    [InlineData("19951212-3456", "SE")]
    public async Task Complete_customers_return_200_with_expected_tax_country(string ssn, string expectedTaxCountry)
    {
        using var factory = new KycApiFactory { Responder = (s, _) => Bundle(s) };
        using var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/kyc-data/{ssn}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(expectedTaxCountry, doc.RootElement.GetProperty("tax_country").GetString());
    }

    [Fact]
    public async Task Customer_missing_tax_country_returns_422_and_a_serious_log()
    {
        using var factory = new KycApiFactory { Responder = (s, _) => Bundle(s) };
        using var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/kyc-data/19800115-1234");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains(factory.Logs, line => line.StartsWith("Error|") && line.Contains("Incomplete KYC"));
    }

    [Fact]
    public async Task Record_is_served_from_the_store_after_a_restart_without_an_upstream_call()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"kyc-restart-{Guid.NewGuid():N}.db");
        var keysPath = Path.Combine(Path.GetTempPath(), $"kyc-restart-keys-{Guid.NewGuid():N}");
        const string ssn = "19900220-5678";

        try
        {
            // First "run": fetch from upstream and persist.
            using (var first = new KycApiFactory
            {
                DbPath = dbPath, KeysPath = keysPath, KeepFiles = true,
                Responder = (s, _) => Bundle(s),
            })
            using (var client = first.CreateAuthenticatedClient())
            {
                var response = await client.GetAsync($"/kyc-data/{ssn}");
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }

            // Second "run": same database and keys, upstream throws if contacted.
            using (var second = new KycApiFactory
            {
                DbPath = dbPath, KeysPath = keysPath, KeepFiles = true,
                ThrowOnFetch = true,
            })
            using (var client = second.CreateAuthenticatedClient())
            {
                var response = await client.GetAsync($"/kyc-data/{ssn}");
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                Assert.Equal("Anna", doc.RootElement.GetProperty("first_name").GetString());
            }
        }
        finally
        {
            if (File.Exists(dbPath)) File.Delete(dbPath);
            if (Directory.Exists(keysPath)) Directory.Delete(keysPath, recursive: true);
        }
    }
}

using System.Net;
using System.Text.Json;
using KycAggregation.Api.Upstream;

namespace KycAggregation.Api.Tests;

public sealed class KycEndpointTests
{
    private static CustomerDataBundle CompleteBundle() => new(
        new PersonalDetails("Ada", "Lovelace", "Storgatan 1, 111 22 Stockholm"),
        new ContactDetails(
            Addresses: null,
            Emails: [new EmailContact(Preferred: true, "ada@example.com")],
            PhoneNumbers: [new PhoneContact(Preferred: true, "+46701234567")]),
        new KycForm([new KycItem("tax_country", "SE"), new KycItem("annual_income", "550000")]));

    private static CustomerDataBundle MissingTaxCountryBundle() => new(
        new PersonalDetails("Ada", "Lovelace", "Storgatan 1, 111 22 Stockholm"),
        Contact: null,
        new KycForm([new KycItem("annual_income", "550000")]));

    [Fact]
    public async Task Returns_200_with_aggregated_data_for_a_complete_customer()
    {
        using var factory = new KycApiFactory { Responder = (_, _) => CompleteBundle() };
        using var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/kyc-data/19900220-5678");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.Equal("19900220-5678", root.GetProperty("ssn").GetString());
        Assert.Equal("Ada", root.GetProperty("first_name").GetString());
        Assert.Equal("Lovelace", root.GetProperty("last_name").GetString());
        Assert.Equal("SE", root.GetProperty("tax_country").GetString());
        Assert.Equal("ada@example.com", root.GetProperty("email").GetString());
        Assert.Equal(550000, root.GetProperty("income").GetInt32());
    }

    [Fact]
    public async Task Returns_400_for_malformed_ssn_without_calling_upstream()
    {
        var upstreamCalled = false;
        using var factory = new KycApiFactory { Responder = (_, _) => { upstreamCalled = true; return null; } };
        using var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/kyc-data/not-an-ssn");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(upstreamCalled);
    }

    [Fact]
    public async Task Returns_404_when_the_customer_does_not_exist()
    {
        using var factory = new KycApiFactory { Responder = (_, _) => null };
        using var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/kyc-data/19751230-9101");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Returns_422_when_a_required_field_is_missing()
    {
        using var factory = new KycApiFactory { Responder = (_, _) => MissingTaxCountryBundle() };
        using var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/kyc-data/19800115-1234");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("tax_country", body);
    }

    [Fact]
    public async Task Returns_500_generic_error_on_unexpected_failure()
    {
        using var factory = new KycApiFactory { ThrowOnFetch = true };
        using var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/kyc-data/19850505-4321");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("upstream failure", body);
    }
}

using System.Net;
using KycAggregation.Api.Upstream;

namespace KycAggregation.Api.Tests;

public sealed class SecurityTests
{
    private static CustomerDataBundle CompleteBundle() => new(
        new PersonalDetails("Ada", "Lovelace", "Storgatan 1, 111 22 Stockholm"),
        new ContactDetails(null,
            [new EmailContact(true, "ada@example.com")],
            [new PhoneContact(true, "+46701234567")]),
        new KycForm([new KycItem("tax_country", "SE"), new KycItem("annual_income", "550000")]));

    [Fact]
    public async Task Rejects_request_without_api_key_as_unauthorized()
    {
        using var factory = new KycApiFactory { Responder = (_, _) => CompleteBundle() };
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/kyc-data/19900220-5678");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Rejects_request_with_invalid_api_key()
    {
        using var factory = new KycApiFactory { Responder = (_, _) => CompleteBundle() };
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");

        var response = await client.GetAsync("/kyc-data/19900220-5678");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Throttles_requests_that_exceed_the_configured_rate()
    {
        using var factory = new KycApiFactory { PermitLimit = 1, Responder = (_, _) => CompleteBundle() };
        using var client = factory.CreateAuthenticatedClient();

        var first = await client.GetAsync("/kyc-data/19900220-5678");
        var second = await client.GetAsync("/kyc-data/19851212-3456");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
        Assert.Contains("error", await second.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Logs_never_contain_the_plaintext_ssn_and_an_access_audit_is_produced()
    {
        const string ssn = "19900220-5678";
        using var factory = new KycApiFactory { Responder = (_, _) => CompleteBundle() };
        using var client = factory.CreateAuthenticatedClient();

        await client.GetAsync($"/kyc-data/{ssn}");

        Assert.All(factory.Logs, line => Assert.DoesNotContain(ssn, line));
        Assert.Contains(factory.Logs, line => line.Contains("KYC lookup served for subject"));
    }

    [Fact]
    public async Task Upstream_request_logs_never_contain_the_plaintext_ssn()
    {
        const string ssn = "19900220-5678";
        var upstream = new StubHttpMessageHandler(request => request.RequestUri!.AbsolutePath switch
        {
            var p when p.StartsWith("/personal-details/") =>
                StubHttpMessageHandler.Json("""{"firstName":"Ada","surName":"Lovelace","address":"Storgatan 1, 111 22 Stockholm"}"""),
            var p when p.StartsWith("/contact-details/") =>
                StubHttpMessageHandler.Json("""{"emails":[],"phoneNumbers":[],"addresses":[]}"""),
            var p when p.StartsWith("/kyc-form/") =>
                StubHttpMessageHandler.Json("""{"items":[{"key":"tax_country","value":"SE"}]}"""),
            _ => StubHttpMessageHandler.NotFound(),
        });
        using var factory = new KycApiFactory { UpstreamHandler = upstream };
        using var client = factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/kyc-data/{ssn}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.All(factory.Logs, line => Assert.DoesNotContain(ssn, line));
    }
}

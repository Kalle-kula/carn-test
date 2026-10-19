using KycAggregation.Api.Upstream;

namespace KycAggregation.Api.Tests;

public class CustomerDataClientTests
{
    private static CustomerDataClient ClientReturning(Func<HttpRequestMessage, HttpResponseMessage> responder)
        => new(new HttpClient(new StubHttpMessageHandler(responder))
        {
            BaseAddress = new Uri("https://upstream.example"),
        });

    [Fact]
    public async Task Returns_null_when_customer_not_found()
    {
        var client = ClientReturning(_ => StubHttpMessageHandler.NotFound());

        var result = await client.GetAsync("19800115-1234", new DateOnly(2026, 10, 19), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Returns_bundle_when_personal_details_exist()
    {
        var client = ClientReturning(request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            return path switch
            {
                var p when p.StartsWith("/personal-details/") =>
                    StubHttpMessageHandler.Json("""{"firstName":"Erik","surName":"Johansson","address":"Storgatan 1, 111 22 Stockholm"}"""),
                var p when p.StartsWith("/contact-details/") =>
                    StubHttpMessageHandler.Json("""{"emails":[{"preferred":true,"emailAddress":"erik@example.se"}],"phoneNumbers":[],"addresses":[]}"""),
                var p when p.StartsWith("/kyc-form/") =>
                    StubHttpMessageHandler.Json("""{"items":[{"key":"tax_country","value":"SE"},{"key":"annual_income","value":"550000"}]}"""),
                _ => StubHttpMessageHandler.NotFound(),
            };
        });

        var result = await client.GetAsync("19800115-1234", new DateOnly(2026, 10, 19), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Erik", result!.Personal.FirstName);
        Assert.Equal("SE", result.KycForm!.Items!.Single(i => i.Key == "tax_country").Value);
    }
}

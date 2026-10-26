using System.Net;
using KycAggregation.Api.Upstream;

namespace KycAggregation.Api.Tests;

public sealed class WebClientHostingTests
{
    private const string ClientMissingWarning = "Warning|Web client not built";

    private static CustomerDataBundle CompleteBundle() => new(
        new PersonalDetails("Ada", "Lovelace", "Storgatan 1, 111 22 Stockholm"),
        new ContactDetails(null, [new EmailContact(true, "ada@example.com")], [new PhoneContact(true, "+46701234567")]),
        new KycForm([new KycItem("tax_country", "SE"), new KycItem("annual_income", "550000")]));

    [Fact]
    public async Task Without_a_built_client_the_api_works_and_warns_once_at_startup()
    {
        var webRoot = Directory.CreateTempSubdirectory("kyc-webroot-").FullName;
        try
        {
            using var factory = new KycApiFactory { WebRoot = webRoot, Responder = (_, _) => CompleteBundle() };
            using var client = factory.CreateAuthenticatedClient();

            var root = await client.GetAsync("/");
            var kyc = await client.GetAsync("/kyc-data/19900220-5678");

            Assert.Equal(HttpStatusCode.NotFound, root.StatusCode);
            Assert.Equal(HttpStatusCode.OK, kyc.StatusCode);
            Assert.Single(factory.Logs, line => line.StartsWith(ClientMissingWarning));
        }
        finally
        {
            Directory.Delete(webRoot, recursive: true);
        }
    }

    [Fact]
    public async Task With_a_built_client_the_root_serves_it_without_a_warning()
    {
        var webRoot = Directory.CreateTempSubdirectory("kyc-webroot-").FullName;
        await File.WriteAllTextAsync(Path.Combine(webRoot, "index.html"), "<html>kyc-client</html>");
        try
        {
            using var factory = new KycApiFactory { WebRoot = webRoot };
            using var client = factory.CreateClient();

            var root = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.OK, root.StatusCode);
            Assert.Contains("kyc-client", await root.Content.ReadAsStringAsync());
            Assert.DoesNotContain(factory.Logs, line => line.StartsWith(ClientMissingWarning));
        }
        finally
        {
            Directory.Delete(webRoot, recursive: true);
        }
    }
}

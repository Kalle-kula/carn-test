using System.Text.Json;
using KycAggregation.Api.Upstream;

namespace KycAggregation.Api.Tests;

public class UpstreamDeserializationTests
{
    [Fact]
    public void PersonalDetails_deserializes_camelCase()
    {
        const string json = """
            {"firstName":"Erik","surName":"Johansson","address":"Storgatan 1, 111 22 Stockholm"}
            """;

        var result = JsonSerializer.Deserialize<PersonalDetails>(json, UpstreamJson.Options);

        Assert.NotNull(result);
        Assert.Equal("Erik", result!.FirstName);
        Assert.Equal("Johansson", result.SurName);
        Assert.Equal("Storgatan 1, 111 22 Stockholm", result.Address);
    }

    [Fact]
    public void ContactDetails_deserializes_nested_collections()
    {
        const string json = """
            {
              "addresses":[{"street":"Storgatan 1","city":"Stockholm","state":"Stockholm","postalCode":"111 22","country":"Sweden"}],
              "emails":[{"preferred":true,"emailAddress":"erik.johansson@example.se"}],
              "phoneNumbers":[{"preferred":true,"number":"070-123 45 67"}]
            }
            """;

        var result = JsonSerializer.Deserialize<ContactDetails>(json, UpstreamJson.Options);

        Assert.NotNull(result);
        Assert.Equal("erik.johansson@example.se", result!.Emails!.Single().EmailAddress);
        Assert.True(result.Emails!.Single().Preferred);
        Assert.Equal("070-123 45 67", result.PhoneNumbers!.Single().Number);
    }

    [Fact]
    public void KycForm_deserializes_items()
    {
        const string json = """
            {"items":[{"key":"annual_income","value":"550000"},{"key":"tax_country","value":"SE"}]}
            """;

        var result = JsonSerializer.Deserialize<KycForm>(json, UpstreamJson.Options);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Items!.Count);
        Assert.Equal("annual_income", result.Items![0].Key);
        Assert.Equal("550000", result.Items[0].Value);
    }
}

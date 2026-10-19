using KycAggregation.Api.Mapping;
using KycAggregation.Api.Upstream;

namespace KycAggregation.Api.Tests;

public class KycMapperTests
{
    private static PersonalDetails Personal(string? first = "Erik", string? sur = "Johansson",
        string? address = "Storgatan 1, 111 22 Stockholm") => new(first, sur, address);

    private static KycForm Form(params (string Key, string? Value)[] items)
        => new(items.Select(i => new KycItem(i.Key, i.Value)).ToList());

    [Fact]
    public void Maps_all_fields_for_complete_data()
    {
        var bundle = new CustomerDataBundle(
            Personal(),
            new ContactDetails(
                Addresses: [],
                Emails: [new EmailContact(false, "secondary@example.se"), new EmailContact(true, "preferred@example.se")],
                PhoneNumbers: [new PhoneContact(true, "070-111 11 11")]),
            Form(("tax_country", "SE"), ("annual_income", "550000")));

        var result = Assert.IsType<KycMappingResult.Complete>(KycMapper.Map("19800115-1234", bundle));
        var data = result.Data;

        Assert.Equal("19800115-1234", data.Ssn);
        Assert.Equal("Erik", data.FirstName);
        Assert.Equal("Johansson", data.LastName);
        Assert.Equal("Storgatan 1, 111 22 Stockholm", data.Address);
        Assert.Equal("SE", data.TaxCountry);
        Assert.Equal("preferred@example.se", data.Email); // preferred chosen over first
        Assert.Equal("070-111 11 11", data.PhoneNumber);
        Assert.Equal(550000, data.Income);
    }

    [Fact]
    public void Falls_back_to_first_contact_when_none_preferred()
    {
        var bundle = new CustomerDataBundle(
            Personal(),
            new ContactDetails(
                Addresses: [],
                Emails: [new EmailContact(false, "first@example.se"), new EmailContact(false, "second@example.se")],
                PhoneNumbers: []),
            Form(("tax_country", "SE")));

        var result = Assert.IsType<KycMappingResult.Complete>(KycMapper.Map("19800115-1234", bundle));

        Assert.Equal("first@example.se", result.Data.Email);
        Assert.Null(result.Data.PhoneNumber);
    }

    [Fact]
    public void Matches_kyc_keys_case_insensitively()
    {
        var bundle = new CustomerDataBundle(Personal(), Contact: null,
            Form(("Tax_Country", "DK"), ("Annual_Income", "680000")));

        var result = Assert.IsType<KycMappingResult.Complete>(KycMapper.Map("19751230-9101", bundle));

        Assert.Equal("DK", result.Data.TaxCountry);
        Assert.Equal(680000, result.Data.Income);
    }

    [Fact]
    public void Flags_incomplete_when_tax_country_missing()
    {
        var bundle = new CustomerDataBundle(Personal(), Contact: null,
            Form(("annual_income", "550000")));

        var result = Assert.IsType<KycMappingResult.Incomplete>(KycMapper.Map("19800115-1234", bundle));

        Assert.Contains("tax_country", result.MissingFields);
    }

    [Fact]
    public void Missing_optional_income_still_complete_with_null()
    {
        var bundle = new CustomerDataBundle(Personal(), Contact: null, Form(("tax_country", "SE")));

        var result = Assert.IsType<KycMappingResult.Complete>(KycMapper.Map("19951212-3456", bundle));

        Assert.Null(result.Data.Income);
        Assert.Equal("SE", result.Data.TaxCountry);
    }
}

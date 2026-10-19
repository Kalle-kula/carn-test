namespace KycAggregation.Api.Upstream;

// Shapes observed from the live Customer Data API (camelCase, root-level routes).
// Deserialized with case-insensitive matching, so property names need not carry attributes.

public sealed record PersonalDetails(string? FirstName, string? SurName, string? Address);

public sealed record ContactDetails(
    IReadOnlyList<UpstreamAddress>? Addresses,
    IReadOnlyList<EmailContact>? Emails,
    IReadOnlyList<PhoneContact>? PhoneNumbers);

public sealed record UpstreamAddress(
    string? Street, string? City, string? State, string? PostalCode, string? Country);

public sealed record EmailContact(bool Preferred, string? EmailAddress);

public sealed record PhoneContact(bool Preferred, string? Number);

public sealed record KycForm(IReadOnlyList<KycItem>? Items);

public sealed record KycItem(string? Key, string? Value);

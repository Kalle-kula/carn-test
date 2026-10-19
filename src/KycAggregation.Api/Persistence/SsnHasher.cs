using System.Security.Cryptography;
using System.Text;

namespace KycAggregation.Api.Persistence;

/// <summary>Derives a deterministic, non-reversible lookup key from an SSN using a keyed HMAC.</summary>
public sealed class SsnHasher(string key)
{
    private readonly byte[] _key = Encoding.UTF8.GetBytes(key);

    public string Hash(string ssn)
    {
        using var hmac = new HMACSHA256(_key);
        var digest = hmac.ComputeHash(Encoding.UTF8.GetBytes(ssn));
        return Convert.ToHexString(digest);
    }
}

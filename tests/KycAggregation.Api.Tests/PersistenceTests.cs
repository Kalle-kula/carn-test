using KycAggregation.Api.Models;
using KycAggregation.Api.Persistence;
using KycAggregation.Api.Services;
using KycAggregation.Api.Upstream;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KycAggregation.Api.Tests;

public class PersistenceTests
{
    private static readonly SsnHasher Hasher = new("unit-test-hash-key");

    private static (KycDbContext Db, SqliteConnection Conn) NewDb()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<KycDbContext>().UseSqlite(conn).Options;
        var db = new KycDbContext(options, new EphemeralDataProtectionProvider());
        db.Database.EnsureCreated();
        return (db, conn);
    }

    private static AggregatedKycData Sample(string ssn = "19900220-5678") => new()
    {
        Ssn = ssn,
        FirstName = "Anna",
        LastName = "Svensson",
        Address = "Storgatan 1, 111 22 Stockholm",
        TaxCountry = "SE",
        PhoneNumber = "070-123 45 67",
        Email = "anna@example.se",
        Income = 420000,
    };

    [Fact]
    public async Task Record_round_trips_through_the_store()
    {
        var (db, conn) = NewDb();
        using (conn)
        {
            var repo = new EfKycRepository(db, Hasher);
            await repo.SaveAsync(Sample(), CancellationToken.None);

            var loaded = await repo.GetAsync("19900220-5678", CancellationToken.None);

            Assert.NotNull(loaded);
            Assert.Equal("Anna", loaded!.FirstName);
            Assert.Equal("SE", loaded.TaxCountry);
            Assert.Equal(420000, loaded.Income);
            Assert.Equal("19900220-5678", loaded.Ssn);
        }
    }

    [Fact]
    public async Task Pii_columns_are_encrypted_at_rest_but_decrypt_back()
    {
        var (db, conn) = NewDb();
        using (conn)
        {
            var repo = new EfKycRepository(db, Hasher);
            await repo.SaveAsync(Sample(), CancellationToken.None);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT SsnHash, FirstName, Address FROM Records";
            using var reader = cmd.ExecuteReader();
            Assert.True(reader.Read());
            var storedHash = reader.GetString(0);
            var storedFirstName = reader.GetString(1);
            var storedAddress = reader.GetString(2);

            // SSN value is never stored, only its one-way hash.
            Assert.NotEqual("19900220-5678", storedHash);
            // PII is ciphertext at rest.
            Assert.NotEqual("Anna", storedFirstName);
            Assert.DoesNotContain("Storgatan", storedAddress);

            // But a read through the repository decrypts correctly.
            var loaded = await repo.GetAsync("19900220-5678", CancellationToken.None);
            Assert.Equal("Anna", loaded!.FirstName);
            Assert.Equal("Storgatan 1, 111 22 Stockholm", loaded.Address);
        }
    }

    [Fact]
    public async Task Second_request_is_served_from_store_without_calling_upstream()
    {
        var (db, conn) = NewDb();
        using (conn)
        {
            var client = new CountingClient(BundleFor());
            var service = new KycAggregationService(new EfKycRepository(db, Hasher), client, TimeProvider.System);

            var first = await service.GetAsync("19900220-5678", CancellationToken.None);
            var second = await service.GetAsync("19900220-5678", CancellationToken.None);

            Assert.IsType<KycLookupResult.Found>(first);
            Assert.IsType<KycLookupResult.Found>(second);
            Assert.Equal(1, client.Calls); // upstream hit once, second served from cache
        }
    }

    private static CustomerDataBundle BundleFor() => new(
        new PersonalDetails("Anna", "Svensson", "Storgatan 1, 111 22 Stockholm"),
        new ContactDetails([], [new EmailContact(true, "anna@example.se")], [new PhoneContact(true, "070-123 45 67")]),
        new KycForm([new KycItem("tax_country", "SE"), new KycItem("annual_income", "420000")]));

    private sealed class CountingClient(CustomerDataBundle? bundle) : ICustomerDataClient
    {
        public int Calls { get; private set; }

        public Task<CustomerDataBundle?> GetAsync(string ssn, DateOnly asOf, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(bundle);
        }
    }
}

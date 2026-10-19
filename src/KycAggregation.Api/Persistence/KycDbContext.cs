using System.Globalization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace KycAggregation.Api.Persistence;

public sealed class KycDbContext(DbContextOptions<KycDbContext> options, IDataProtectionProvider protection)
    : DbContext(options)
{
    private readonly IDataProtector _protector = protection.CreateProtector("KycRecord.Pii.v1");

    public DbSet<KycRecordEntity> Records => Set<KycRecordEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Converters run only for non-null values, so nullable columns stay null in the database.
        var stringCrypto = new ValueConverter<string, string>(
            plain => _protector.Protect(plain),
            cipher => _protector.Unprotect(cipher));

        var intCrypto = new ValueConverter<int, string>(
            value => _protector.Protect(value.ToString(CultureInfo.InvariantCulture)),
            cipher => int.Parse(_protector.Unprotect(cipher), CultureInfo.InvariantCulture));

        var record = modelBuilder.Entity<KycRecordEntity>();
        record.HasKey(e => e.SsnHash);
        record.Property(e => e.FirstName).HasConversion(stringCrypto);
        record.Property(e => e.LastName).HasConversion(stringCrypto);
        record.Property(e => e.Address).HasConversion(stringCrypto);
        record.Property(e => e.TaxCountry).HasConversion(stringCrypto);
#pragma warning disable CS8620 // converter runs only for non-null values; nullable columns stay null
        record.Property(e => e.PhoneNumber).HasConversion(stringCrypto);
        record.Property(e => e.Email).HasConversion(stringCrypto);
#pragma warning restore CS8620
        record.Property(e => e.Income).HasConversion(intCrypto);
    }
}

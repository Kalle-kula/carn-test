using KycAggregation.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace KycAggregation.Api.Persistence;

public interface IKycRepository
{
    Task<AggregatedKycData?> GetAsync(string ssn, CancellationToken ct);
    Task SaveAsync(AggregatedKycData data, CancellationToken ct);
}

public sealed class EfKycRepository(KycDbContext db, SsnHasher hasher) : IKycRepository
{
    public async Task<AggregatedKycData?> GetAsync(string ssn, CancellationToken ct)
    {
        var hash = hasher.Hash(ssn);
        var entity = await db.Records.FirstOrDefaultAsync(e => e.SsnHash == hash, ct);
        if (entity is null)
        {
            return null;
        }

        // The SSN value is never persisted; it comes from the caller's request.
        return new AggregatedKycData
        {
            Ssn = ssn,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            Address = entity.Address,
            TaxCountry = entity.TaxCountry,
            PhoneNumber = entity.PhoneNumber,
            Email = entity.Email,
            Income = entity.Income,
        };
    }

    public async Task SaveAsync(AggregatedKycData data, CancellationToken ct)
    {
        var hash = hasher.Hash(data.Ssn);
        var entity = await db.Records.FirstOrDefaultAsync(e => e.SsnHash == hash, ct);
        if (entity is null)
        {
            db.Records.Add(new KycRecordEntity
            {
                SsnHash = hash,
                FirstName = data.FirstName,
                LastName = data.LastName,
                Address = data.Address,
                TaxCountry = data.TaxCountry,
                PhoneNumber = data.PhoneNumber,
                Email = data.Email,
                Income = data.Income,
            });
        }
        else
        {
            entity.FirstName = data.FirstName;
            entity.LastName = data.LastName;
            entity.Address = data.Address;
            entity.TaxCountry = data.TaxCountry;
            entity.PhoneNumber = data.PhoneNumber;
            entity.Email = data.Email;
            entity.Income = data.Income;
        }

        await db.SaveChangesAsync(ct);
    }
}

using ElectricalStore.Application.Abstractions;
using ElectricalStore.Domain.Customers;
using ElectricalStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ElectricalStore.Infrastructure.Persistence.Repositories;

public sealed class CustomerProfileRepository : ICustomerProfileRepository
{
    private readonly AppDbContext _db;

    public CustomerProfileRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<CustomerProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _db.CustomerProfiles.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    public async Task AddAsync(CustomerProfile profile, CancellationToken cancellationToken = default)
    {
        await _db.CustomerProfiles.AddAsync(profile, cancellationToken);
    }
}

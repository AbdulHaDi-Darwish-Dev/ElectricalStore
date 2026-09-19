using ElectricalStore.Application.Abstractions;
using ElectricalStore.Domain.Inventory;
using ElectricalStore.Domain.Ordering;
using ElectricalStore.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ElectricalStore.Infrastructure.Persistence.Repositories;

public sealed class AppUnitOfWork : IAppUnitOfWork
{
    private readonly AppDbContext _db;

    public AppUnitOfWork(AppDbContext db)
    {
        _db = db;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex) when (ex.Entries.Any(e => e.Entity is InventoryItem))
        {
            throw new InventoryConcurrencyConflictException(
                "Inventory was modified concurrently.",
                ex);
        }
        catch (DbUpdateConcurrencyException ex) when (ex.Entries.Any(e => e.Entity is Order))
        {
            throw new OrderConcurrencyConflictException(
                "Order was modified concurrently.",
                ex);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new UniqueConstraintViolationException(
                "A unique constraint was violated.",
                ex);
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException sql && (sql.Number == 2601 || sql.Number == 2627))
                return true;
        }

        return false;
    }
}

public sealed class SystemAppClock : IAppClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

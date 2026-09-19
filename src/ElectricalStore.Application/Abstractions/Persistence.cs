namespace ElectricalStore.Application.Abstractions;

public interface IAppUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IAppClock
{
    DateTime UtcNow { get; }
}

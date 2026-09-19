using ElectricalStore.Domain.Catalog.Categories;

namespace ElectricalStore.Application.Abstractions;

public interface ICategoryRepository
{
    Task AddAsync(Category category, CancellationToken cancellationToken = default);

    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Category?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNormalizedNameAsync(
        string normalizedName,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Category>> ListAsync(
        bool activeOnly,
        bool requireImage = false,
        CancellationToken cancellationToken = default);
}

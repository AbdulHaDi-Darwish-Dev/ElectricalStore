using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Common;
using ElectricalStore.Domain.Ordering;

namespace ElectricalStore.Application.Ordering;

public sealed record OrderingSettingsDto(decimal MinimumMerchandiseSubtotal);

public sealed record UpdateOrderingSettingsRequest(decimal MinimumMerchandiseSubtotal);

public sealed class GetOrderingSettingsUseCase
{
    private readonly IOrderingSettingsRepository _settings;

    public GetOrderingSettingsUseCase(IOrderingSettingsRepository settings)
    {
        _settings = settings;
    }

    public async Task<OrderingSettingsDto> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var row = await _settings.GetOrCreateAsync(cancellationToken);
        return new OrderingSettingsDto(row.MinimumMerchandiseSubtotal);
    }
}

public sealed class UpdateOrderingSettingsUseCase
{
    private readonly IOrderingSettingsRepository _settings;
    private readonly IAppUnitOfWork _unitOfWork;

    public UpdateOrderingSettingsUseCase(IOrderingSettingsRepository settings, IAppUnitOfWork unitOfWork)
    {
        _settings = settings;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<OrderingSettingsDto>> ExecuteAsync(
        UpdateOrderingSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.MinimumMerchandiseSubtotal < 0m)
            return Result.Failure<OrderingSettingsDto>(OrderingErrors.InvalidMinimumOrderAmount);

        var row = await _settings.GetTrackedOrCreateAsync(cancellationToken);
        try
        {
            row.SetMinimumMerchandiseSubtotal(request.MinimumMerchandiseSubtotal);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Result.Failure<OrderingSettingsDto>(OrderingErrors.InvalidMinimumOrderAmount);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(new OrderingSettingsDto(row.MinimumMerchandiseSubtotal));
    }
}

using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Common;
using ElectricalStore.Domain.Shipping;

namespace ElectricalStore.Application.Shipping;

public sealed class CreateDeliveryZoneUseCase
{
    private readonly IDeliveryZoneRepository _repository;
    private readonly IAppUnitOfWork _unitOfWork;

    public CreateDeliveryZoneUseCase(IDeliveryZoneRepository repository, IAppUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DeliveryZoneDto>> ExecuteAsync(
        CreateDeliveryZoneRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = ValidateNameAndFee(request.Name, request.Fee);
        if (validation.IsFailure)
            return Result.Failure<DeliveryZoneDto>(validation.Error!);

        var normalized = DeliveryZone.NormalizeNameKey(request.Name);
        if (await _repository.ExistsByNormalizedNameAsync(normalized, excludeId: null, cancellationToken))
            return Result.Failure<DeliveryZoneDto>(ShippingErrors.NameAlreadyExists);

        DeliveryZone zone;
        try
        {
            zone = DeliveryZone.Create(request.Name, request.Fee, request.IsActive);
        }
        catch (ArgumentException ex) when (ex.ParamName == "name" && ex.Message.Contains("required", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<DeliveryZoneDto>(ShippingErrors.NameRequired);
        }
        catch (ArgumentException ex) when (ex.ParamName == "name")
        {
            return Result.Failure<DeliveryZoneDto>(ShippingErrors.NameTooLong);
        }
        catch (ArgumentOutOfRangeException ex) when (ex.ParamName == "fee")
        {
            return Result.Failure<DeliveryZoneDto>(ShippingErrors.NegativeFee);
        }

        await _repository.AddAsync(zone, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result.Failure<DeliveryZoneDto>(ShippingErrors.NameAlreadyExists);
        }

        return Result.Success(DeliveryZoneDto.From(zone));
    }

    internal static Result ValidateNameAndFee(string? name, decimal fee)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(ShippingErrors.NameRequired);

        if (name.Trim().Length > DeliveryZone.NameMaxLength)
            return Result.Failure(ShippingErrors.NameTooLong);

        if (fee < 0m)
            return Result.Failure(ShippingErrors.NegativeFee);

        return Result.Success();
    }
}

public sealed class UpdateDeliveryZoneUseCase
{
    private readonly IDeliveryZoneRepository _repository;
    private readonly IAppUnitOfWork _unitOfWork;

    public UpdateDeliveryZoneUseCase(IDeliveryZoneRepository repository, IAppUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DeliveryZoneDto>> ExecuteAsync(
        Guid id,
        UpdateDeliveryZoneRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = CreateDeliveryZoneUseCase.ValidateNameAndFee(request.Name, request.Fee);
        if (validation.IsFailure)
            return Result.Failure<DeliveryZoneDto>(validation.Error!);

        var zone = await _repository.GetTrackedByIdAsync(id, cancellationToken);
        if (zone is null)
            return Result.Failure<DeliveryZoneDto>(ShippingErrors.NotFound);

        var normalized = DeliveryZone.NormalizeNameKey(request.Name);
        if (await _repository.ExistsByNormalizedNameAsync(normalized, excludeId: id, cancellationToken))
            return Result.Failure<DeliveryZoneDto>(ShippingErrors.NameAlreadyExists);

        try
        {
            zone.Update(request.Name, request.Fee);
        }
        catch (ArgumentException ex) when (ex.ParamName == "name" && ex.Message.Contains("required", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<DeliveryZoneDto>(ShippingErrors.NameRequired);
        }
        catch (ArgumentException ex) when (ex.ParamName == "name")
        {
            return Result.Failure<DeliveryZoneDto>(ShippingErrors.NameTooLong);
        }
        catch (ArgumentOutOfRangeException ex) when (ex.ParamName == "fee")
        {
            return Result.Failure<DeliveryZoneDto>(ShippingErrors.NegativeFee);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result.Failure<DeliveryZoneDto>(ShippingErrors.NameAlreadyExists);
        }

        return Result.Success(DeliveryZoneDto.From(zone));
    }
}

public sealed class ActivateDeliveryZoneUseCase
{
    private readonly IDeliveryZoneRepository _repository;
    private readonly IAppUnitOfWork _unitOfWork;

    public ActivateDeliveryZoneUseCase(IDeliveryZoneRepository repository, IAppUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DeliveryZoneDto>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var zone = await _repository.GetTrackedByIdAsync(id, cancellationToken);
        if (zone is null)
            return Result.Failure<DeliveryZoneDto>(ShippingErrors.NotFound);

        zone.Activate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(DeliveryZoneDto.From(zone));
    }
}

public sealed class DeactivateDeliveryZoneUseCase
{
    private readonly IDeliveryZoneRepository _repository;
    private readonly IAppUnitOfWork _unitOfWork;

    public DeactivateDeliveryZoneUseCase(IDeliveryZoneRepository repository, IAppUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DeliveryZoneDto>> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var zone = await _repository.GetTrackedByIdAsync(id, cancellationToken);
        if (zone is null)
            return Result.Failure<DeliveryZoneDto>(ShippingErrors.NotFound);

        zone.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(DeliveryZoneDto.From(zone));
    }
}

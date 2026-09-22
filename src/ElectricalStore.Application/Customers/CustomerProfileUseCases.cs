using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Common;
using ElectricalStore.Domain.Customers;

namespace ElectricalStore.Application.Customers;

public sealed class GetCustomerProfileUseCase
{
    private readonly ICustomerProfileRepository _profiles;
    private readonly ICustomerIdentityLookup _identity;

    public GetCustomerProfileUseCase(
        ICustomerProfileRepository profiles,
        ICustomerIdentityLookup identity)
    {
        _profiles = profiles;
        _identity = identity;
    }

    public async Task<Result<CustomerProfileDto>> ExecuteAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            return Result.Failure<CustomerProfileDto>(CustomerErrors.ActorRequired);

        var identity = await _identity.GetByUserIdAsync(userId, cancellationToken);
        if (identity is null)
            return Result.Failure<CustomerProfileDto>(CustomerErrors.IdentityNotFound);

        var profile = await _profiles.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
        {
            // Legacy users (pre-CustomerProfile): read-only fallback — do NOT persist from GET.
            // Provisional display name from email local-part until the user saves via PUT.
            var provisional = DeriveProvisionalFullName(identity.Email, identity.UserName);
            return Result.Success(new CustomerProfileDto(
                userId,
                provisional,
                identity.Email,
                identity.EmailConfirmed));
        }

        return Result.Success(new CustomerProfileDto(
            profile.UserId,
            profile.FullName,
            identity.Email,
            identity.EmailConfirmed));
    }

    public static string DeriveProvisionalFullName(string email, string userName)
    {
        var source = !string.IsNullOrWhiteSpace(email) ? email : userName;
        var local = source.Trim();
        var at = local.IndexOf('@');
        if (at > 0)
            local = local[..at];

        local = local.Trim();
        if (string.IsNullOrWhiteSpace(local))
            local = "Customer";

        if (local.Length > CustomerProfile.FullNameMaxLength)
            local = local[..CustomerProfile.FullNameMaxLength];

        return local;
    }
}

public sealed class UpdateCustomerProfileUseCase
{
    private readonly ICustomerProfileRepository _profiles;
    private readonly ICustomerIdentityLookup _identity;
    private readonly IAppUnitOfWork _unitOfWork;
    private readonly IAppClock _clock;

    public UpdateCustomerProfileUseCase(
        ICustomerProfileRepository profiles,
        ICustomerIdentityLookup identity,
        IAppUnitOfWork unitOfWork,
        IAppClock clock)
    {
        _profiles = profiles;
        _identity = identity;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<CustomerProfileDto>> ExecuteAsync(
        Guid userId,
        UpdateCustomerProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (userId == Guid.Empty)
            return Result.Failure<CustomerProfileDto>(CustomerErrors.ActorRequired);

        var identity = await _identity.GetByUserIdAsync(userId, cancellationToken);
        if (identity is null)
            return Result.Failure<CustomerProfileDto>(CustomerErrors.IdentityNotFound);

        var validation = ValidateFullName(request.FullName);
        if (validation.IsFailure)
            return Result.Failure<CustomerProfileDto>(validation.Error!);

        var profile = await _profiles.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null)
        {
            // First save for legacy users creates the CustomerProfile row.
            profile = CustomerProfile.Create(userId, request.FullName, _clock.UtcNow);
            await _profiles.AddAsync(profile, cancellationToken);
        }
        else
        {
            try
            {
                profile.UpdateFullName(request.FullName, _clock.UtcNow);
            }
            catch (ArgumentException ex) when (ex.ParamName == "fullName" && ex.Message.Contains("required", StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<CustomerProfileDto>(CustomerErrors.FullNameRequired);
            }
            catch (ArgumentException)
            {
                return Result.Failure<CustomerProfileDto>(CustomerErrors.FullNameTooLong);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new CustomerProfileDto(
            profile.UserId,
            profile.FullName,
            identity.Email,
            identity.EmailConfirmed));
    }

    public static Result ValidateFullName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return Result.Failure(CustomerErrors.FullNameRequired);

        if (fullName.Trim().Length > CustomerProfile.FullNameMaxLength)
            return Result.Failure(CustomerErrors.FullNameTooLong);

        return Result.Success();
    }
}

public sealed class CreateCustomerProfileForUserUseCase
{
    private readonly ICustomerProfileRepository _profiles;
    private readonly IAppUnitOfWork _unitOfWork;
    private readonly IAppClock _clock;

    public CreateCustomerProfileForUserUseCase(
        ICustomerProfileRepository profiles,
        IAppUnitOfWork unitOfWork,
        IAppClock clock)
    {
        _profiles = profiles;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    /// <summary>Creates a profile for a newly registered identity user. Idempotent if already present.</summary>
    public async Task<Result<CustomerProfile>> ExecuteAsync(
        Guid userId,
        string fullName,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            return Result.Failure<CustomerProfile>(CustomerErrors.ActorRequired);

        var validation = UpdateCustomerProfileUseCase.ValidateFullName(fullName);
        if (validation.IsFailure)
            return Result.Failure<CustomerProfile>(validation.Error!);

        var existing = await _profiles.GetByUserIdAsync(userId, cancellationToken);
        if (existing is not null)
            return Result.Success(existing);

        CustomerProfile profile;
        try
        {
            profile = CustomerProfile.Create(userId, fullName, _clock.UtcNow);
        }
        catch (ArgumentException ex) when (ex.ParamName == "fullName" && ex.Message.Contains("required", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<CustomerProfile>(CustomerErrors.FullNameRequired);
        }
        catch (ArgumentException)
        {
            return Result.Failure<CustomerProfile>(CustomerErrors.FullNameTooLong);
        }

        await _profiles.AddAsync(profile, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            return Result.Failure<CustomerProfile>(CustomerErrors.ProfileCreateFailed);
        }

        return Result.Success(profile);
    }
}

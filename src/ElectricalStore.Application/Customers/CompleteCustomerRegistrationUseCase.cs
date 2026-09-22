using ElectricalStore.Application.Abstractions;
using ElectricalStore.Application.Common;

namespace ElectricalStore.Application.Customers;

/// <summary>
/// After Permixa identity registration succeeds, create CustomerProfile.
/// On profile failure, compensate by deleting the identity user (separate DbContexts — not a distributed TX).
/// </summary>
public sealed class CompleteCustomerRegistrationUseCase
{
    private readonly CreateCustomerProfileForUserUseCase _createProfile;
    private readonly ICustomerIdentityCompensation _compensation;

    public CompleteCustomerRegistrationUseCase(
        CreateCustomerProfileForUserUseCase createProfile,
        ICustomerIdentityCompensation compensation)
    {
        _createProfile = createProfile;
        _compensation = compensation;
    }

    public async Task<Result<CustomerRegistrationResult>> ExecuteAsync(
        Guid userId,
        string email,
        string fullName,
        CancellationToken cancellationToken = default)
    {
        var profileResult = await _createProfile.ExecuteAsync(userId, fullName, cancellationToken);
        if (profileResult.IsSuccess)
        {
            return Result.Success(new CustomerRegistrationResult(
                userId,
                profileResult.Value.FullName,
                email));
        }

        var cleaned = await _compensation.TryDeleteUserAsync(userId, cancellationToken);
        if (!cleaned)
            return Result.Failure<CustomerRegistrationResult>(CustomerErrors.RegistrationCompensationFailed);

        return Result.Failure<CustomerRegistrationResult>(
            profileResult.Error ?? CustomerErrors.ProfileCreateFailed);
    }
}

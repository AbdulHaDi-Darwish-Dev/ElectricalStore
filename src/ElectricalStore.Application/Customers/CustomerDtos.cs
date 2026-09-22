namespace ElectricalStore.Application.Customers;

public sealed record CustomerProfileDto(
    Guid UserId,
    string FullName,
    string Email,
    bool EmailConfirmed);

public sealed record CustomerRegistrationRequest(
    string FullName,
    string Email,
    string Password);

public sealed record CustomerRegistrationResult(
    Guid UserId,
    string FullName,
    string Email,
    bool EmailVerificationRequired = true,
    bool VerificationEmailSent = false);

public sealed record UpdateCustomerProfileRequest(string FullName);

public sealed record ChangeCustomerPasswordRequest(
    string CurrentPassword,
    string NewPassword);

public sealed record ConfirmCustomerEmailRequest(
    Guid ChallengeId,
    string Token);

public sealed record CustomerEmailVerificationResendRequest(string Email);

public sealed record CustomerEmailVerificationResendResult(string Message);

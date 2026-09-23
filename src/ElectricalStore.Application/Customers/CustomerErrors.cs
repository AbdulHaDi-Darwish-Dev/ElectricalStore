using ElectricalStore.Application.Common;

namespace ElectricalStore.Application.Customers;

public static class CustomerErrors
{
    public static readonly Error FullNameRequired =
        new("Customer.FullNameRequired", "Full name is required.");

    public static readonly Error FullNameTooLong =
        new("Customer.FullNameTooLong", $"Full name must be {Domain.Customers.CustomerProfile.FullNameMaxLength} characters or fewer.");

    public static readonly Error EmailRequired =
        new("Customer.EmailRequired", "Email is required.");

    public static readonly Error PasswordRequired =
        new("Customer.PasswordRequired", "Password is required.");

    public static readonly Error ActorRequired =
        new("Customer.ActorRequired", "Authentication is required.");

    public static readonly Error IdentityNotFound =
        new("Customer.IdentityNotFound", "Identity user was not found.");

    public static readonly Error ProfileCreateFailed =
        new("Customer.ProfileCreateFailed", "Customer profile could not be created.");

    public static readonly Error RegistrationCompensationFailed =
        new("Customer.RegistrationCompensationFailed",
            "Customer profile could not be created and identity cleanup failed. Contact support.");

    public static readonly Error PasswordResetLinkInvalid =
        new("Customer.PasswordResetLinkInvalid", "The password reset link is invalid.");

    public static readonly Error PasswordResetLinkExpired =
        new("Customer.PasswordResetLinkExpired", "The password reset link has expired.");

    public static readonly Error PasswordResetLinkUsed =
        new("Customer.PasswordResetLinkUsed", "The password reset link is no longer valid.");

    public static readonly Error PasswordPolicyFailed =
        new("Customer.PasswordPolicyFailed", "The new password does not meet security requirements.");

    public static readonly Error PasswordResetFailed =
        new("Customer.PasswordResetFailed", "Password reset could not be completed.");

    /// <summary>
    /// Password may already have changed; renewable sessions could not be revoked.
    /// Clients must not treat this as a normal reset success.
    /// </summary>
    public static readonly Error PasswordResetSessionRevocationFailed =
        new(
            "Customer.PasswordResetSessionRevocationFailed",
            "Could not finish securing sessions after password reset. Sign in again or contact support.");

    public static readonly Error CurrentPasswordInvalid =
        new("Customer.CurrentPasswordInvalid", "The current password is incorrect.");

    public static readonly Error EmailAlreadyInUse =
        new("Customer.EmailAlreadyInUse", "The email address is already in use.");

    public static readonly Error EmailUnchanged =
        new("Customer.EmailUnchanged", "The new email is the same as the current email.");

    public static readonly Error EmailChangeDeliveryFailed =
        new("Customer.EmailChangeDeliveryFailed", "The confirmation email could not be sent. Try again later.");

    public static readonly Error EmailChangeCooldownActive =
        new("Customer.EmailChangeCooldownActive", "Please wait before requesting another email change.");

    public static readonly Error EmailChangeRequestFailed =
        new("Customer.EmailChangeRequestFailed", "Email change could not be requested.");

    public static readonly Error EmailChangeUnavailable =
        new("Customer.EmailChangeUnavailable", "Email change is not available for this account.");

    public static readonly Error EmailChangeLinkInvalid =
        new("Customer.EmailChangeLinkInvalid", "The email change link is invalid.");

    public static readonly Error EmailChangeLinkExpired =
        new("Customer.EmailChangeLinkExpired", "The email change link has expired.");

    public static readonly Error EmailChangeLinkUsed =
        new("Customer.EmailChangeLinkUsed", "The email change link is no longer valid.");

    public static readonly Error EmailChangeConfirmFailed =
        new("Customer.EmailChangeConfirmFailed", "Email change could not be completed.");

    /// <summary>
    /// Email may already have changed; renewable sessions could not be revoked.
    /// Clients must not treat this as a normal confirm success.
    /// </summary>
    public static readonly Error EmailChangeSessionRevocationFailed =
        new(
            "Customer.EmailChangeSessionRevocationFailed",
            "Could not finish securing sessions after email change. Sign in again or contact support.");
}

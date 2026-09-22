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
}

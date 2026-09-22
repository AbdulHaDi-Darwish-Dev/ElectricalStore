using ElectricalStore.Application.Abstractions;
using Microsoft.AspNetCore.Identity;
using Permixa.Infrastructure.Identity;

namespace ElectricalStore.Api.Hosting;

public sealed class PermixaCustomerIdentityLookup : ICustomerIdentityLookup
{
    private readonly UserManager<ApplicationUser> _users;

    public PermixaCustomerIdentityLookup(UserManager<ApplicationUser> users)
    {
        _users = users;
    }

    public async Task<CustomerIdentityInfo?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByIdAsync(userId.ToString());
        if (user is null)
            return null;

        return new CustomerIdentityInfo(
            user.Id,
            user.Email ?? string.Empty,
            user.EmailConfirmed,
            user.UserName ?? string.Empty);
    }
}

public sealed class PermixaCustomerIdentityCompensation : ICustomerIdentityCompensation
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly ILogger<PermixaCustomerIdentityCompensation> _logger;

    public PermixaCustomerIdentityCompensation(
        UserManager<ApplicationUser> users,
        ILogger<PermixaCustomerIdentityCompensation> logger)
    {
        _users = users;
        _logger = logger;
    }

    public async Task<bool> TryDeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByIdAsync(userId.ToString());
        if (user is null)
            return true;

        var result = await _users.DeleteAsync(user);
        if (result.Succeeded)
            return true;

        _logger.LogError(
            "Failed to compensate identity user {UserId} after CustomerProfile failure: {Errors}",
            userId,
            string.Join("; ", result.Errors.Select(e => $"{e.Code}:{e.Description}")));
        return false;
    }
}

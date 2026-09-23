using Permixa.Application.Authentication.Abstractions;
using Permixa.Domain.Authentication;

namespace ElectricalStore.IntegrationTests.Support;

/// <summary>
/// Test double: delegates to the real refresh-token repository except when
/// <see cref="RevokeFailureGate.FailRevokeAll"/> is true, then <see cref="RevokeAllForUserAsync"/> throws.
/// </summary>
public sealed class RevokeFailingRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IRefreshTokenRepository _inner;
    private readonly RevokeFailureGate _gate;

    public RevokeFailingRefreshTokenRepository(
        IRefreshTokenRepository inner,
        RevokeFailureGate gate)
    {
        _inner = inner;
        _gate = gate;
    }

    public Task<RefreshToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default) =>
        _inner.GetByTokenHashAsync(tokenHash, cancellationToken);

    public Task<RefreshToken?> GetByIdAsync(
        Guid refreshTokenId,
        CancellationToken cancellationToken = default) =>
        _inner.GetByIdAsync(refreshTokenId, cancellationToken);

    public Task<IReadOnlyList<RefreshToken>> GetActiveByFamilyIdAsync(
        Guid familyId,
        CancellationToken cancellationToken = default) =>
        _inner.GetActiveByFamilyIdAsync(familyId, cancellationToken);

    public Task AddAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default) =>
        _inner.AddAsync(refreshToken, cancellationToken);

    public Task<int> RevokeAllForUserAsync(
        Guid userId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default)
    {
        if (_gate.FailRevokeAll)
            throw new InvalidOperationException("Simulated refresh-token revocation failure.");

        return _inner.RevokeAllForUserAsync(userId, revokedAtUtc, cancellationToken);
    }

    public Task<bool> FamilyExistsForUserAsync(
        Guid userId,
        Guid familyId,
        CancellationToken cancellationToken = default) =>
        _inner.FamilyExistsForUserAsync(userId, familyId, cancellationToken);

    public Task<int> RevokeFamilyForUserAsync(
        Guid userId,
        Guid familyId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default) =>
        _inner.RevokeFamilyForUserAsync(userId, familyId, revokedAtUtc, cancellationToken);

    public Task<bool> HasActiveFamilyForUserAsync(
        Guid userId,
        Guid familyId,
        DateTime utcNow,
        CancellationToken cancellationToken = default) =>
        _inner.HasActiveFamilyForUserAsync(userId, familyId, utcNow, cancellationToken);

    public Task<int> RevokeAllForUserExceptFamilyAsync(
        Guid userId,
        Guid currentFamilyId,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken = default) =>
        _inner.RevokeAllForUserExceptFamilyAsync(
            userId, currentFamilyId, revokedAtUtc, cancellationToken);
}

public sealed class RevokeFailureGate
{
    public bool FailRevokeAll { get; set; }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Shared.CerbosAuth;

/// <summary>
/// Parses policy name strings of the form "Cerbos:resource:action" into a CerbosRequirement.
/// Anything that doesn't start with "Cerbos:" falls through to the default provider,
/// so standard [Authorize] and [Authorize(Roles="...")] attributes still work normally.
/// </summary>
public class CerbosPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public CerbosPolicyProvider(IOptions<AuthorizationOptions> options)
        => _fallback = new DefaultAuthorizationPolicyProvider(options);

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith("Cerbos:", StringComparison.OrdinalIgnoreCase))
            return _fallback.GetPolicyAsync(policyName);

        var parts = policyName.Split(':', 3);
        if (parts.Length != 3)
            throw new InvalidOperationException(
                $"Invalid Cerbos policy '{policyName}'. Expected: 'Cerbos:resource:action'");

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new CerbosRequirement(parts[1], parts[2]))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()  => _fallback.GetDefaultPolicyAsync();
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();
}

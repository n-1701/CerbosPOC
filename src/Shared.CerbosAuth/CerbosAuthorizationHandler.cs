using System.Security.Claims;
using Cerbos.Api.V1.Engine;
using Cerbos.Sdk.Builder;
using Cerbos.Sdk.Utility;
using Microsoft.AspNetCore.Authorization;

namespace Shared.CerbosAuth;

/// <summary>
/// Plugs into ASP.NET Core's authorization pipeline.
/// Called whenever you do: await _auth.AuthorizeAsync(User, resource, "Cerbos:order:approve")
///
/// It builds the Cerbos request from the ClaimsPrincipal + ICerbosResource,
/// calls the PDP, and marks the requirement as succeeded if ALLOW is returned.
/// </summary>
public class CerbosAuthorizationHandler
    : AuthorizationHandler<CerbosRequirement, ICerbosResource>
{
    private readonly CerbosClient _client;

    public CerbosAuthorizationHandler(CerbosClient client) => _client = client;

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CerbosRequirement requirement,
        ICerbosResource resource)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return Task.CompletedTask;

        // --- Principal: who is asking ---
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? context.User.FindFirstValue("sub")
                     ?? throw new InvalidOperationException("No user ID claim.");

        var roles = context.User
            .FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToArray();

        var principal = Principal.NewInstance(userId, roles);

        // Forward any extra claims as principal attributes (e.g. department)
        // Policy can reference these as principal.attr.department etc.
        var department = context.User.FindFirstValue("department");
        if (department is not null)
            principal.WithAttribute("department", AttributeValue.StringValue(department));

        // --- Resource: what is being accessed ---
        var resourceEntry = ResourceEntry
            .NewInstance(requirement.ResourceKind, resource.ResourceId)
            .WithAttribute("ownerId",    AttributeValue.StringValue(resource.OwnerId))
            .WithAttribute("department", AttributeValue.StringValue(resource.Department))
            .WithActions(requirement.Action);

        foreach (var (key, value) in resource.ExtraAttributes)
        {
            var attr = value switch
            {
                bool b   => AttributeValue.BoolValue(b),
                int i    => AttributeValue.IntValue(i),
                long l   => AttributeValue.IntValue(l),
                double d => AttributeValue.FloatValue(d),
                _        => AttributeValue.StringValue(value.ToString()!)
            };
            resourceEntry.WithAttribute(key, attr);
        }

        // --- Ask Cerbos ---
        var request = CheckResourcesRequest
            .NewInstance()
            .WithRequestId(RequestId.Generate())
            .WithPrincipal(principal)
            .WithResourceEntries(resourceEntry);

        var allowed = _client
            .CheckResources(request)
            .Find(resource.ResourceId)
            ?.IsAllowed(requirement.Action) ?? false;

        if (allowed)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Carries "what resource type" and "what action" through the pipeline.
/// Parsed from policy name strings like "Cerbos:order:approve".
/// </summary>
public record CerbosRequirement(string ResourceKind, string Action)
    : IAuthorizationRequirement;

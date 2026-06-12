using Cerbos.Sdk.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.CerbosAuth;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Call this once in Program.cs: services.AddCerbos(configuration)
    /// Reads "Cerbos:PdpAddress" and "Cerbos:UseTls" from appsettings.json.
    /// </summary>
    public static IServiceCollection AddCerbos(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var address = configuration["Cerbos:PdpAddress"] ?? "http://localhost:3593";
        var useTls  = configuration.GetValue<bool>("Cerbos:UseTls");

        var builder = CerbosClientBuilder.ForTarget(address);
        var client  = useTls ? builder.Build() : builder.WithPlaintext().Build();

        services.AddSingleton(client);
        services.AddSingleton<IAuthorizationHandler, CerbosAuthorizationHandler>();

        // Enables "Cerbos:order:approve" style policy names on [Authorize] attributes
        services.AddSingleton<IAuthorizationPolicyProvider, CerbosPolicyProvider>();

        return services;
    }
}

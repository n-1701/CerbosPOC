using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

/// <summary>
/// Tells ASP.NET Core's authentication pipeline to accept the identity
/// that FakeAuthMiddleware already set on context.User.
/// Without this, UseAuthentication() would overwrite context.User with an anonymous identity.
/// </summary>
public class FakeAuthSchemeHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public FakeAuthSchemeHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // If FakeAuthMiddleware already populated User, honour it
        if (Context.User.Identity?.IsAuthenticated == true)
        {
            var ticket = new AuthenticationTicket(Context.User, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        return Task.FromResult(AuthenticateResult.NoResult());
    }
}

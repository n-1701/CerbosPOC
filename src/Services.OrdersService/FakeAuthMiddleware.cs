using System.Security.Claims;

/// <summary>
/// DEV ONLY — lets you test every authorization scenario without a real identity provider.
/// Pass user identity as query string params on any request.
///
/// Test scenarios:
///
///   Regular user views their own order (ALLOW):
///     GET /api/orders/aaaaaaaa-0000-0000-0000-000000000001?userId=user-alice&roles=user&department=engineering
///
///   Regular user tries to view someone else's order (DENY):
///     GET /api/orders/bbbbbbbb-0000-0000-0000-000000000002?userId=user-alice&roles=user&department=engineering
///
///   Owner edits their draft order (ALLOW):
///     PATCH /api/orders/aaaaaaaa-0000-0000-0000-000000000001/edit?userId=user-alice&roles=user&department=engineering
///
///   Owner tries to edit an already-approved order (DENY — wrong status):
///     PATCH /api/orders/cccccccc-0000-0000-0000-000000000003/edit?userId=user-bob&roles=user&department=engineering
///
///   Dept manager approves an order in their dept (ALLOW):
///     PATCH /api/orders/bbbbbbbb-0000-0000-0000-000000000002/approve?userId=mgr-dave&roles=manager&department=engineering
///
///   Dept manager tries to approve an order in a different dept (DENY):
///     PATCH /api/orders/dddddddd-0000-0000-0000-000000000004/approve?userId=mgr-dave&roles=manager&department=engineering
///
///   Admin approves anything (ALLOW):
///     PATCH /api/orders/dddddddd-0000-0000-0000-000000000004/approve?userId=admin-eve&roles=admin&department=any
/// </summary>
public class FakeAuthMiddleware
{
    private readonly RequestDelegate _next;
    public FakeAuthMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var q = context.Request.Query;

        if (q.ContainsKey("userId"))
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, q["userId"].ToString()),
                new("sub",                     q["userId"].ToString()),
                new("department",              q["department"].ToString())
            };

            foreach (var role in q["roles"].ToArray())
                claims.Add(new Claim(ClaimTypes.Role, role));

            context.User = new ClaimsPrincipal(
                new ClaimsIdentity(claims, authenticationType: "FakeAuth"));
        }

        await _next(context);
    }
}

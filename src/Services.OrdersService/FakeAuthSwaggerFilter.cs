using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

/// <summary>
/// Adds userId, roles, department query params to every operation in Swagger UI
/// so you can test different user identities without leaving the browser.
/// </summary>
public class FakeAuthSwaggerFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<OpenApiParameter>();

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "userId", In = ParameterLocation.Query, Required = false,
            Schema = new OpenApiSchema { Type = "string" },
            Description = "e.g. user-alice, user-bob, mgr-dave, admin-eve"
        });

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "roles", In = ParameterLocation.Query, Required = false,
            Schema = new OpenApiSchema { Type = "string" },
            Description = "user | manager | admin"
        });

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "department", In = ParameterLocation.Query, Required = false,
            Schema = new OpenApiSchema { Type = "string" },
            Description = "engineering | marketing"
        });
    }
}

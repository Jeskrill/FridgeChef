using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FridgeChef.Api.Extensions;

internal sealed class RequestExamplesOperationFilter : IOperationFilter
{
    private static readonly Dictionary<string, OpenApiObject> Examples = new()
    {
        ["/auth/registration|POST"] = new OpenApiObject
        {
            ["displayName"] = new OpenApiString("Иван Петров"),
            ["email"] = new OpenApiString("ivan@example.com"),
            ["password"] = new OpenApiString("MyPass1234!")
        },
        ["/auth/sessions|POST"] = new OpenApiObject
        {
            ["email"] = new OpenApiString("ivan@example.com"),
            ["password"] = new OpenApiString("MyPass1234!")
        },
        ["/auth/tokens|POST"] = new OpenApiObject
        {
            ["refreshToken"] = new OpenApiString("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...")
        },
        ["/pantry|POST"] = new OpenApiObject
        {
            ["foodNodeId"] = new OpenApiInteger(1),
            ["quantity"] = new OpenApiDouble(500),
            ["unitId"] = new OpenApiInteger(1)
        },
        ["/pantry/{id}|PATCH"] = new OpenApiObject
        {
            ["quantity"] = new OpenApiDouble(300),
            ["unitId"] = new OpenApiInteger(1)
        },
        ["/users/me|PATCH"] = new OpenApiObject
        {
            ["displayName"] = new OpenApiString("Иван Петров"),
            ["email"] = new OpenApiString("ivan@example.com")
        },
        ["/users/me/password|PUT"] = new OpenApiObject
        {
            ["oldPassword"] = new OpenApiString("MyPass1234!"),
            ["newPassword"] = new OpenApiString("NewPass5678!")
        },
        ["/settings/cuisines|PUT"] = new OpenApiObject
        {
            ["taxonIds"] = new OpenApiArray { new OpenApiInteger(20), new OpenApiInteger(21) }
        },
        ["/settings/allergens|POST"] = new OpenApiObject
        {
            ["foodNodeId"] = new OpenApiInteger(42),
            ["severity"] = new OpenApiString("strict")
        },
        ["/settings/favorite-foods|POST"] = new OpenApiObject
        {
            ["foodNodeId"] = new OpenApiInteger(42)
        },
        ["/settings/excluded-foods|POST"] = new OpenApiObject
        {
            ["foodNodeId"] = new OpenApiInteger(42)
        },
        ["/settings/diets|PUT"] = new OpenApiObject
        {
            ["taxonIds"] = new OpenApiArray { new OpenApiInteger(1), new OpenApiInteger(2) }
        },
        ["/recipes/matches|POST"] = new OpenApiObject
        {
            ["dietFilterIds"] = new OpenApiArray(),
            ["maxResults"] = new OpenApiInteger(20)
        },
        ["/admin/users/{userId}/blocked|PATCH"] = new OpenApiObject
        {
            ["isBlocked"] = new OpenApiBoolean(true)
        },
        ["/admin/pricing/test-queries|POST"] = new OpenApiObject
        {
            ["query"] = new OpenApiString("Молоко 3.2%")
        }
    };

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath ?? "";
        var method = context.ApiDescription.HttpMethod?.ToUpperInvariant() ?? "";
        var key = $"/{path}|{method}";

        if (Examples.TryGetValue(key, out var example)
            && operation.RequestBody?.Content.ContainsKey("application/json") == true)
        {
            operation.RequestBody.Content["application/json"].Example = example;
        }
    }
}

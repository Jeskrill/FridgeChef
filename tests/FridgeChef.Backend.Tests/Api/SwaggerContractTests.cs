using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace FridgeChef.Backend.Tests.Api;

public sealed class SwaggerContractTests
{
    [Fact]
    public async Task SwaggerDocument_ShouldExposeCurrentEndpointRoutes()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Jwt:Secret"] = "integration-test-secret-key-32-bytes-minimum",
                        ["ConnectionStrings:DefaultConnection"] =
                            "Host=localhost;Database=fridgechef_contract_tests;Username=test;Password=test"
                    });
                });
            });
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json", CancellationToken.None);
        var document = await response.Content.ReadAsStringAsync(CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        document.Should().Contain("\"/auth/registration\"");
        document.Should().Contain("\"/auth/sessions\"");
        document.Should().Contain("\"/auth/tokens\"");
        document.Should().Contain("\"/users/me/password\"");
        document.Should().Contain("\"/recipes/matches\"");
        document.Should().Contain("\"/admin/pricing/test-queries\"");

        document.Should().NotContain("\"/auth/register\"");
        document.Should().NotContain("\"/auth/login\"");
        document.Should().NotContain("\"/auth/refresh\"");
        document.Should().NotContain("\"/recipes/search\"");
        document.Should().NotContain("\"/admin/pricing/search-test\"");
    }
}

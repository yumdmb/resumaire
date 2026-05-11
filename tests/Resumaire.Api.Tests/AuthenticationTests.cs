using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Resumaire.Api.Tests.Infrastructure;

namespace Resumaire.Api.Tests;

public sealed class AuthenticationTests(ResumaireApiFactory factory) : IClassFixture<ResumaireApiFactory>
{
    [Fact]
    public async Task GetCurrentUser_WithoutAuthenticatedUser_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_WithAuthenticatedUser_ReturnsUserId()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeaderName, "user-123");

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("user-123", payload.GetProperty("data").GetProperty("userId").GetString());
    }
}

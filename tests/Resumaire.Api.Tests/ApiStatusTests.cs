using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Resumaire.Api.Tests.Infrastructure;

namespace Resumaire.Api.Tests;

public sealed class ApiStatusTests(ResumaireApiFactory factory) : IClassFixture<ResumaireApiFactory>
{
    [Fact]
    public async Task GetRoot_ReturnsApiStatus()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Resumaire API", payload.GetProperty("data").GetProperty("name").GetString());
    }
}

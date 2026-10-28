using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;

namespace MovieApp.Api.Tests;

public sealed class ConfigurationTests
{
    [Fact]
    public async Task Missing_admin_key_disables_editing()
    {
        await using var app = new ApiFactory();
        await using var configured = app.WithWebHostBuilder(b => b.UseSetting("Admin:ApiKey", ""));
        using var client = configured.CreateClient();
        var response = await client.PatchAsync("/api/movies/9001", new StringContent("{\"title\":\"Changed\"}", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(0, app.Notifier.Calls);
    }

    [Fact]
    public async Task Production_does_not_publish_documentation_or_admin_page()
    {
        await using var app = new ApiFactory();
        await using var configured = app.WithWebHostBuilder(b => b.UseEnvironment("Production"));
        using var client = configured.CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/swagger/index.html")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/swagger/v1/swagger.json")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/admin")).StatusCode);
    }


    [Fact]
    public async Task Malformed_json_has_no_dispatch_or_client_stack_trace()
    {
        await using var app = new ApiFactory(); using var client = app.Client(true);
        var response = await client.PatchAsync("/api/movies/9001", new StringContent("{invalid", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain("StackTrace", await response.Content.ReadAsStringAsync());
        Assert.Equal(0, app.Notifier.Calls);
    }

}

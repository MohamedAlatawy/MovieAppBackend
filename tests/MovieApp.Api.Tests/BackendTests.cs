using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using MovieApp.Api.Contracts;
using MovieApp.Api.Data;
using MovieApp.Api.Services;
namespace MovieApp.Api.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string TestKey = "test-only-not-a-real-admin-key-000000000000";
    public string DatabasePath { get; } = Path.Combine(Path.GetTempPath(), "MovieAppBackendTests", Guid.NewGuid().ToString("N"), "test.db");
    public RecordingNotifier Notifier { get; } = new();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development"); builder.UseSetting("Database:Path", DatabasePath); builder.UseSetting("Admin:ApiKey", TestKey); builder.UseSetting("Firebase:Mode", "Local");
        builder.ConfigureServices(services => { services.RemoveAll<IMovieNotifier>(); services.AddSingleton<IMovieNotifier>(sp => { Notifier.Scopes = sp.GetRequiredService<IServiceScopeFactory>(); return Notifier; }); });
    }
    public HttpClient Client(bool admin = false) { var client = CreateClient(); if (admin) client.DefaultRequestHeaders.Add("X-Admin-Key", TestKey); return client; }
}
public sealed class RecordingNotifier : IMovieNotifier
{
    public IServiceScopeFactory Scopes { get; set; } = null!;
    public int Calls { get; private set; }
    public bool CommittedAtDispatch { get; private set; }
    public bool Throw { get; set; }
    public async Task<NotificationResult> MovieUpdatedAsync(int movieId, DateTime updatedAt, CancellationToken ct)
    {
        Calls++; await using var scope = Scopes.CreateAsyncScope();
        var movie = await scope.ServiceProvider.GetRequiredService<MovieDbContext>().Movies.AsNoTracking().SingleAsync(m => m.Id == movieId, ct);
        CommittedAtDispatch = movie.LastModified == updatedAt;
        if (Throw) throw new InvalidOperationException("Fake delivery failure"); return new("test_fake");
    }
}
public sealed class BackendTests
{
    private static StringContent Json(string value) => new(value, Encoding.UTF8, "application/json");
    [Fact]
    public async Task Pagination_metadata_and_order_are_correct()
    {
        await using var app = new ApiFactory(); using var client = app.Client();
        var p = await client.GetFromJsonAsync<MoviePage>("/api/movies?page=1&pageSize=10");
        Assert.NotNull(p); Assert.Equal(10, p.Items.Count); Assert.Equal(31, p.TotalItems); Assert.Equal(4, p.TotalPages); Assert.True(p.HasNextPage); Assert.Equal(Enumerable.Range(1001, 10), p.Items.Select(m => m.Id));
        var last = await client.GetFromJsonAsync<MoviePage>("/api/movies?page=4&pageSize=10"); Assert.Single(last!.Items); Assert.False(last.HasNextPage);
        Assert.Empty((await client.GetFromJsonAsync<MoviePage>("/api/movies?page=5&pageSize=10"))!.Items);
    }
    [Theory]
    [InlineData("page=0")]
    [InlineData("page=-1")]
    [InlineData("pageSize=101")]
    [InlineData("pageSize=0")]
    [InlineData("page=2147483647")]
    public async Task Invalid_pagination_is_problem_details(string query)
    {
        await using var app = new ApiFactory(); using var client = app.Client(); var response = await client.GetAsync("/api/movies?" + query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }
    [Fact]
    public async Task Search_is_case_insensitive_and_matches_list_query()
    {
        await using var app = new ApiFactory(); using var client = app.Client();
        var search = await client.GetFromJsonAsync<MoviePage>("/api/movies/search?query=TOy&pageSize=1"); Assert.Equal(2, search!.TotalItems); Assert.Single(search.Items); Assert.Equal(2, search.TotalPages);
        Assert.Equal(2, (await client.GetFromJsonAsync<MoviePage>("/api/movies?query=toy"))!.TotalItems);
        Assert.Empty((await client.GetFromJsonAsync<MoviePage>("/api/movies/search?query=ZZZnotfound"))!.Items);
    }
    [Theory]
    [InlineData("/api/movies/search?query=%20%20")]
    [InlineData("/api/movies/search")]
    public async Task Empty_search_is_rejected(string url)
    { await using var app = new ApiFactory(); using var client = app.Client(); Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(url)).StatusCode); }
    [Fact]
    public async Task Details_and_unknown_movie()
    {
        await using var app = new ApiFactory(); using var client = app.Client(); var movie = await client.GetFromJsonAsync<MovieDto>("/api/movies/1001");
        Assert.Equal("Toy Harbor", movie!.Title); Assert.True(movie.IsDemo); Assert.Equal("unverified", movie.EligibilityEvidence); Assert.Null(movie.UsCertification);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/movies/999999")).StatusCode);
    }
    [Fact]
    public async Task Patch_persists_fields_and_dispatches_after_commit()
    {
        await using var app = new ApiFactory(); using var client = app.Client(true);
        var response = await client.PatchAsync("/api/movies/9001", Json("""{"title":"A changed title","overview":"A revised demo synopsis.","releaseDate":"2026-05-06","voteAverage":8.4,"genres":["Drama","Family"],"posterUrl":"https://example.com/poster.jpg"}"""));
        response.EnsureSuccessStatusCode(); var movie = await response.Content.ReadFromJsonAsync<MovieDto>(); Assert.Equal(9001, movie!.Id); Assert.Equal("A changed title", movie.Title); Assert.Equal(8.4, movie.VoteAverage); Assert.Equal(new DateOnly(2026, 5, 6), movie.ReleaseDate);
        Assert.Equal(1, app.Notifier.Calls); Assert.True(app.Notifier.CommittedAtDispatch); Assert.Equal("test_fake", response.Headers.GetValues("X-Notification-Status").Single()); Assert.True(movie.LastModified > new DateTime(2026, 1, 1));
        await using var db = new MovieDbContext(new DbContextOptionsBuilder<MovieDbContext>().UseSqlite($"Data Source={app.DatabasePath}").Options);
        Assert.Equal("A changed title", (await db.Movies.SingleAsync(m => m.Id == 9001)).Title);
        Assert.Equal(1, (await client.GetFromJsonAsync<MoviePage>("/api/movies/search?query=changed"))!.TotalItems);
        var clear = await client.PatchAsync("/api/movies/9001", Json("""{"posterUrl":null}""")); Assert.Null((await clear.Content.ReadFromJsonAsync<MovieDto>())!.PosterUrl);
    }
    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("{\"title\":\" \"}")]
    [InlineData("{\"voteAverage\":11}")]
    [InlineData("{\"genres\":[]}")]
    [InlineData("{\"releaseDate\":\"invalid\"}")]
    [InlineData("{\"adult\":false}")]
    [InlineData("{\"usCertification\":\"G\"}")]
    [InlineData("{\"id\":1001}")]
    [InlineData("{\"title\":null}")]
    [InlineData("{\"posterUrl\":\"javascript:alert(1)\"}")]
    [InlineData("{\"title\":\"first\",\"title\":\"second\"}")]
    [InlineData("{\"title\":\"not saved\",\"voteAverage\":-1}")]
    public async Task Invalid_patch_does_not_save_or_dispatch(string body)
    {
        await using var app = new ApiFactory(); using var client = app.Client(true); Assert.Equal(HttpStatusCode.BadRequest, (await client.PatchAsync("/api/movies/9001", Json(body))).StatusCode); Assert.Equal(0, app.Notifier.Calls); Assert.Equal("Demo Edit Sandbox", (await client.GetFromJsonAsync<MovieDto>("/api/movies/9001"))!.Title);
    }
    [Fact]
    public async Task Unauthorized_and_unknown_patches_do_not_dispatch()
    {
        await using var app = new ApiFactory(); using var client = app.Client(); Assert.Equal(HttpStatusCode.Unauthorized, (await client.PatchAsync("/api/movies/9001", Json("{\"title\":\"Changed\"}"))).StatusCode);
        client.DefaultRequestHeaders.Add("X-Admin-Key", "wrong"); Assert.Equal(HttpStatusCode.Unauthorized, (await client.PatchAsync("/api/movies/9001", Json("{\"title\":\"Changed\"}"))).StatusCode);
        using var admin = app.Client(true); Assert.Equal(HttpStatusCode.NotFound, (await admin.PatchAsync("/api/movies/999999", Json("{\"title\":\"Changed\"}"))).StatusCode); Assert.Equal(0, app.Notifier.Calls);
    }
    [Fact]
    public async Task Notification_failure_does_not_rollback_edit()
    {
        await using var app = new ApiFactory(); app.Notifier.Throw = true; using var client = app.Client(true); var response = await client.PatchAsync("/api/movies/9001", Json("{\"title\":\"Saved despite failure\"}")); response.EnsureSuccessStatusCode(); Assert.Equal("failed", response.Headers.GetValues("X-Notification-Status").Single()); Assert.Equal("Saved despite failure", (await client.GetFromJsonAsync<MovieDto>("/api/movies/9001"))!.Title);
    }
    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("invalid token with spaces for testing")]
    public async Task Invalid_tokens_are_rejected(string token)
    { await using var app = new ApiFactory(); using var client = app.Client(); Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/devices/register", new { token })).StatusCode); }
    [Fact]
    public async Task Device_registration_is_idempotent_and_removal_is_private()
    {
        await using var app = new ApiFactory(); using var client = app.Client(); const string token = "test-token-not-a-real-FCM-device-1234567890";
        for (var i = 0; i < 2; i++) { var response = await client.PostAsJsonAsync("/api/devices/register", new { token }); response.EnsureSuccessStatusCode(); Assert.DoesNotContain(token, await response.Content.ReadAsStringAsync()); }
        using var scope = app.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<MovieDbContext>(); Assert.Equal(1, await db.DeviceTokens.CountAsync()); Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/devices/unregister", new { token })).StatusCode); Assert.Equal(0, await db.DeviceTokens.CountAsync()); Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/devices")).StatusCode);
    }
    [Fact]
    public async Task Health_swagger_and_admin_are_available_locally()
    {
        await using var app = new ApiFactory(); using var client = app.Client(); (await client.GetAsync("/health")).EnsureSuccessStatusCode(); Assert.Contains("Swagger UI", await client.GetStringAsync("/swagger/index.html")); var schema = await client.GetStringAsync("/swagger/v1/swagger.json"); Assert.Contains("X-Admin-Key", schema); Assert.Contains("/api/devices/register", schema); Assert.Contains("MovieApp demo editor", await client.GetStringAsync("/admin"));
    }
    [Fact]
    public async Task Missing_credentials_never_claim_delivery()
    {
        await using var app = new ApiFactory(); using var client = app.Client(); using var scope = app.Services.CreateScope(); var transport = scope.ServiceProvider.GetRequiredService<IFcmTransport>(); Assert.False(transport.Configured); var result = await new MovieNotifier(transport, NullLogger<MovieNotifier>.Instance).MovieUpdatedAsync(9001, DateTime.UtcNow, default); Assert.Equal("not_configured", result.Status); Assert.Equal(0, result.Accepted);
    }
}

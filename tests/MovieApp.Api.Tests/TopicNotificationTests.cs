using System.Net;
using System.Net.Http.Json;
using System.Text;
using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MovieApp.Api.Contracts;
using MovieApp.Api.Data;
using MovieApp.Api.Entities;
using MovieApp.Api.Services;

namespace MovieApp.Api.Tests;

public sealed class TopicNotificationTests
{
    private static WebApplicationFactory<Program> WithTransport(ApiFactory app, RecordingTopicTransport transport)
        => app.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IMovieNotifier>();
            services.AddScoped<IMovieNotifier, MovieNotifier>();
            services.RemoveAll<IFcmTransport>();
            services.AddSingleton<IFcmTransport>(sp =>
            {
                transport.Scopes = sp.GetRequiredService<IServiceScopeFactory>();
                return transport;
            });
        }));

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public async Task Patch_sends_one_data_only_topic_message_after_commit_regardless_of_devices(int deviceCount)
    {
        await using var app = new ApiFactory();
        var transport = new RecordingTopicTransport();
        await using var configured = WithTransport(app, transport);
        using var client = configured.CreateClient();
        client.DefaultRequestHeaders.Add("X-Admin-Key", ApiFactory.TestKey);
        using (var scope = configured.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MovieDbContext>();
            for (var i = 0; i < deviceCount; i++) db.DeviceTokens.Add(new DeviceToken { Token = $"unused-topic-test-device-{i}" });
            await db.SaveChangesAsync();
            // Prove dispatch does not even query the compatibility table.
            if (deviceCount == 0) await db.Database.ExecuteSqlRawAsync("DROP TABLE DeviceTokens");
        }
        var response = await client.PatchAsync("/api/movies/9001", new StringContent("{\"title\":\"Topic updated movie\"}", Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        var movie = (await response.Content.ReadFromJsonAsync<MovieDto>())!;
        var message = Assert.Single(transport.Messages);
        Assert.True(transport.CommittedAtSend);
        Assert.Equal("movies", message.Topic);
        Assert.Null(message.Notification);
        Assert.Null(message.Condition);
        Assert.Null(message.Android);
        Assert.Equal(3, message.Data.Count);
        Assert.Equal("movie_updated", message.Data["eventType"]);
        Assert.Equal(movie.Id.ToString(), message.Data["movieId"]);
        Assert.Equal(movie.LastModified.ToUniversalTime().ToString("O"), message.Data["updatedAt"]);
        Assert.EndsWith("Z", message.Data["updatedAt"]);
        Assert.Equal("accepted", response.Headers.GetValues("X-Notification-Status").Single());
        Assert.Equal("1", response.Headers.GetValues("X-Notification-Accepted").Single());
        if (deviceCount > 0)
        {
            using var scope = configured.Services.CreateScope();
            Assert.Equal(deviceCount, await scope.ServiceProvider.GetRequiredService<MovieDbContext>().DeviceTokens.CountAsync());
        }
    }

    [Theory]
    [InlineData(false, "/api/movies/9001", "{\"title\":\"Unauthorized\"}", 401)]
    [InlineData(true, "/api/movies/9001", "{}", 400)]
    [InlineData(true, "/api/movies/999999", "{\"title\":\"Missing\"}", 404)]
    public async Task Rejected_patch_never_reaches_topic_transport(bool admin, string url, string body, int status)
    {
        await using var app = new ApiFactory();
        var transport = new RecordingTopicTransport();
        await using var configured = WithTransport(app, transport);
        using var client = configured.CreateClient();
        if (admin) client.DefaultRequestHeaders.Add("X-Admin-Key", ApiFactory.TestKey);
        var response = await client.PatchAsync(url, new StringContent(body, Encoding.UTF8, "application/json"));
        Assert.Equal((HttpStatusCode)status, response.StatusCode);
        Assert.Empty(transport.Messages);
    }

    [Fact]
    public async Task Topic_transport_failure_leaves_edit_persisted()
    {
        await using var app = new ApiFactory();
        var transport = new RecordingTopicTransport { Fail = true };
        await using var configured = WithTransport(app, transport);
        using var client = configured.CreateClient();
        client.DefaultRequestHeaders.Add("X-Admin-Key", ApiFactory.TestKey);
        var response = await client.PatchAsync("/api/movies/9001", new StringContent("{\"title\":\"Saved despite topic failure\"}", Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        Assert.Single(transport.Messages);
        Assert.True(transport.CommittedAtSend);
        Assert.Equal("failed", response.Headers.GetValues("X-Notification-Status").Single());
        Assert.Equal("0", response.Headers.GetValues("X-Notification-Accepted").Single());
        Assert.Equal("Saved despite topic failure", (await client.GetFromJsonAsync<MovieDto>("/api/movies/9001"))!.Title);
    }

    [Fact]
    public async Task Missing_credentials_skip_transport_without_claiming_success()
    {
        await using var app = new ApiFactory();
        var transport = new RecordingTopicTransport { Configured = false };
        await using var configured = WithTransport(app, transport);
        using var client = configured.CreateClient();
        client.DefaultRequestHeaders.Add("X-Admin-Key", ApiFactory.TestKey);
        var response = await client.PatchAsync("/api/movies/9001", new StringContent("{\"title\":\"Saved locally\"}", Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        Assert.Empty(transport.Messages);
        Assert.Equal("not_configured", response.Headers.GetValues("X-Notification-Status").Single());
        Assert.Equal("0", response.Headers.GetValues("X-Notification-Accepted").Single());
    }

    [Theory]
    [InlineData("Auto", "")]
    [InlineData("Local", "not-a-real-credential-path")]
    public async Task Disabled_real_transport_does_not_open_credentials_or_send(string mode, string path)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["Firebase:Mode"] = mode, ["Firebase:CredentialsPath"] = path }).Build();
        using var transport = new FirebaseTransport(configuration);
        Assert.False(transport.Configured);
        await Assert.ThrowsAsync<InvalidOperationException>(() => transport.SendAsync(new Message { Topic = "movies" }, default));
    }

    private sealed class RecordingTopicTransport : IFcmTransport
    {
        public bool Configured { get; init; } = true;
        public bool Fail { get; init; }
        public IServiceScopeFactory Scopes { get; set; } = null!;
        public List<Message> Messages { get; } = [];
        public bool CommittedAtSend { get; private set; }
        public async Task<string> SendAsync(Message message, CancellationToken ct)
        {
            Messages.Add(message);
            using var scope = Scopes.CreateScope();
            var movie = await scope.ServiceProvider.GetRequiredService<MovieDbContext>().Movies.AsNoTracking()
                .SingleAsync(m => m.Id == int.Parse(message.Data["movieId"]), ct);
            CommittedAtSend = movie.LastModified.ToString("O") == message.Data["updatedAt"];
            if (Fail) throw new InvalidOperationException("Fake topic transport failure");
            return "fake-topic-message-id";
        }
    }
}

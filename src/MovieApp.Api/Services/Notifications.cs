using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
namespace MovieApp.Api.Services;

public sealed record NotificationResult(string Status, int Accepted = 0, int Failed = 0);
public interface IFcmTransport
{
    bool Configured { get; }
    Task<string> SendAsync(Message message, CancellationToken ct);
}
public interface IMovieNotifier { Task<NotificationResult> MovieUpdatedAsync(int movieId, DateTime updatedAt, CancellationToken ct); }
// Lazy initialization keeps bad credentials from preventing reads or committed edits.
public sealed class FirebaseTransport(IConfiguration configuration) : IFcmTransport, IDisposable
{
    private FirebaseApp? app;
    private readonly object gate = new();
    private string? CredentialPath => configuration["Firebase:CredentialsPath"] ?? Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
    public bool Configured => !string.Equals(configuration["Firebase:Mode"], "Local", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(CredentialPath);
    public async Task<string> SendAsync(Message message, CancellationToken ct)
    {
        if (!Configured) throw new InvalidOperationException("Firebase delivery is not configured.");
        lock (gate)
        {
            app ??= FirebaseApp.Create(new AppOptions { Credential = CredentialFactory.FromFile<ServiceAccountCredential>(CredentialPath!).ToGoogleCredential(), ProjectId = configuration["Firebase:ProjectId"] }, "MovieApp-" + Guid.NewGuid().ToString("N"));
        }
        return await FirebaseMessaging.GetMessaging(app).SendAsync(message, ct);
    }
    public void Dispose() => app?.Delete();
}
public sealed class MovieNotifier(IFcmTransport transport, ILogger<MovieNotifier> logger) : IMovieNotifier
{
    public async Task<NotificationResult> MovieUpdatedAsync(int movieId, DateTime updatedAt, CancellationToken ct)
    {
        if (!transport.Configured)
        {
            logger.LogInformation("Movie {MovieId}: local mode/no Firebase credentials; no push sent", movieId);
            return new("not_configured");
        }
        var message = new Message
        {
            Topic = "movies",
            Data = new Dictionary<string, string>
            {
                ["eventType"] = "movie_updated",
                ["movieId"] = movieId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["updatedAt"] = updatedAt.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture)
            }
        };
        // Exactly one application-level send, independent of the DeviceTokens table.
        var messageId = await transport.SendAsync(message, ct);
        logger.LogInformation("FCM accepted movie {MovieId} for topic movies; message ID {MessageId}. Subscriber receipt is unverified", movieId, messageId);
        return new("accepted", Accepted: 1);
    }
}

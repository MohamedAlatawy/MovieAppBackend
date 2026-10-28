using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using MovieApp.Api.Entities;

namespace MovieApp.Api.Contracts;

public sealed record MovieDto(int Id, string Title, string Overview, DateOnly ReleaseDate, string? PosterUrl,
    double VoteAverage, string[] Genres, bool Adult, string? UsCertification, DateTime LastModified,
    string DataSource = "synthetic-demo", string EligibilityEvidence = "unverified", bool IsDemo = true)
{
    public static MovieDto From(Movie m) => new(m.Id, m.Title, m.Overview, m.ReleaseDate, m.PosterUrl,
        m.VoteAverage, JsonSerializer.Deserialize<string[]>(m.GenresJson)!, m.Adult, m.UsCertification, m.LastModified);
}
public sealed record MoviePage(IReadOnlyList<MovieDto> Items, int Page, int PageSize, int TotalItems, int TotalPages, bool HasNextPage);
public sealed class DeviceRequest
{
    /// <summary>FCM registration token. Not an administrator API key.</summary>
    [Required, StringLength(4096, MinimumLength = 20), RegularExpression(@"^[A-Za-z0-9_:\-]+$")]
    public string Token { get; set; } = "";
}

/// <summary>Partial object, not RFC 6902. Omit fields to preserve them. Only posterUrl accepts null.</summary>
public sealed class MoviePatchExample
{
    public string? Title { get; set; }
    public string? Overview { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    public double? VoteAverage { get; set; }
    public string[]? Genres { get; set; }
    public string? PosterUrl { get; set; }
}

public static class MoviePatch
{
    public static Dictionary<string, string[]> Apply(JsonElement body, Movie movie)
    {
        var errors = new Dictionary<string, string[]>();
        void Error(string name, string message) => errors[name] = [message];
        if (body.ValueKind != JsonValueKind.Object || !body.EnumerateObject().Any())
        { Error("body", "Supply a nonempty partial JSON object."); return errors; }
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var p in body.EnumerateObject())
        {
            if (!seen.Add(p.Name)) { Error(p.Name, "Duplicate properties are not allowed."); continue; }
            var v = p.Value;
            switch (p.Name)
            {
                case "title":
                    if (v.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(v.GetString()) || v.GetString()!.Trim().Length > 200) Error(p.Name, "Title must contain 1-200 characters.");
                    else { movie.Title = v.GetString()!.Trim(); movie.SearchTitle = movie.Title.ToUpperInvariant(); }
                    break;
                case "overview":
                    if (v.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(v.GetString()) || v.GetString()!.Length > 5000) Error(p.Name, "Overview must contain 1-5000 characters.");
                    else movie.Overview = v.GetString()!.Trim();
                    break;
                case "releaseDate":
                    if (v.ValueKind != JsonValueKind.String || !DateOnly.TryParseExact(v.GetString(), "yyyy-MM-dd", out var date) || date.Year < 1888 || date.Year > 2100) Error(p.Name, "Use a date between 1888 and 2100 in yyyy-MM-dd format.");
                    else movie.ReleaseDate = date;
                    break;
                case "voteAverage":
                    if (v.ValueKind != JsonValueKind.Number || !v.TryGetDouble(out var vote) || !double.IsFinite(vote) || vote < 0 || vote > 10) Error(p.Name, "Vote average must be between 0 and 10.");
                    else movie.VoteAverage = vote;
                    break;
                case "genres":
                    if (v.ValueKind != JsonValueKind.Array || v.GetArrayLength() < 1 || v.GetArrayLength() > 10 || v.EnumerateArray().Any(g => g.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(g.GetString()) || g.GetString()!.Length > 40)) Error(p.Name, "Provide 1-10 nonempty genres of at most 40 characters each.");
                    else movie.GenresJson = JsonSerializer.Serialize(v.EnumerateArray().Select(g => g.GetString()!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase));
                    break;
                case "posterUrl":
                    if (v.ValueKind == JsonValueKind.Null) movie.PosterUrl = null;
                    else if (v.ValueKind != JsonValueKind.String || v.GetString()!.Length > 2048 || !Uri.TryCreate(v.GetString(), UriKind.Absolute, out var url) || url.Scheme != "https" || !string.IsNullOrEmpty(url.UserInfo)) Error(p.Name, "Use an absolute HTTPS URL without credentials, or null.");
                    else movie.PosterUrl = v.GetString();
                    break;
                default: Error(p.Name, "Unknown or immutable field."); break;
            }
        }
        return errors;
    }
}

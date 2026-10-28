namespace MovieApp.Api.Entities;

public sealed class Movie
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string SearchTitle { get; set; } = "";
    public string Overview { get; set; } = "";
    public DateOnly ReleaseDate { get; set; }
    public string? PosterUrl { get; set; }
    public double VoteAverage { get; set; }
    public string GenresJson { get; set; } = "[]";
    public bool Adult { get; set; }
    public string? UsCertification { get; set; }
    public DateTime LastModified { get; set; }
}

public sealed class DeviceToken
{
    public long Id { get; set; }
    public string Token { get; set; } = "";
    public DateTime UpdatedAt { get; set; }
}

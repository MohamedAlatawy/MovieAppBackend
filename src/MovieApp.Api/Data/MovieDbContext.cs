using Microsoft.EntityFrameworkCore;
using MovieApp.Api.Entities;

namespace MovieApp.Api.Data;

public sealed class MovieDbContext(DbContextOptions<MovieDbContext> options) : DbContext(options)
{
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        var movie = model.Entity<Movie>();
        movie.Property(x => x.Title).HasMaxLength(200);
        movie.Property(x => x.SearchTitle).HasMaxLength(200);
        movie.Property(x => x.Overview).HasMaxLength(5000);
        movie.Property(x => x.PosterUrl).HasMaxLength(2048);
        movie.HasIndex(x => x.SearchTitle);
        movie.HasIndex(x => x.ReleaseDate);
        movie.Property(x => x.LastModified).IsConcurrencyToken();
        // SQLite reads DateTime without Kind; preserve the API's UTC timestamp contract.
        movie.Property(x => x.LastModified).HasConversion(x => x, x => DateTime.SpecifyKind(x, DateTimeKind.Utc));
        model.Entity<DeviceToken>().Property(x => x.Token).HasMaxLength(4096);
        model.Entity<DeviceToken>().HasIndex(x => x.Token).IsUnique();
        movie.HasData(DemoSeed.Movies());
    }
}

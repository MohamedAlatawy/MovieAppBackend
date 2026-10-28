using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieApp.Api.Configuration;
using MovieApp.Api.Contracts;
using MovieApp.Api.Data;
using MovieApp.Api.Services;
namespace MovieApp.Api.Controllers;

[ApiController, Route("api/movies")]
public sealed class MoviesController(MovieDbContext db, IMovieNotifier notifier, ILogger<MoviesController> logger) : ControllerBase
{
    /// <summary>List synthetic demo movies in stable ID order. Optional case-insensitive title query.</summary>
    [HttpGet]
    public Task<ActionResult<MoviePage>> List([Range(1, 1000000)] int page = 1, [Range(1, 100)] int pageSize = 20, [StringLength(200)] string? query = null, CancellationToken ct = default) => Page(page, pageSize, query, ct);
    /// <summary>Search titles. A blank query is rejected.</summary>
    [HttpGet("search")]
    public Task<ActionResult<MoviePage>> Search([Required, StringLength(200)] string query, [Range(1, 1000000)] int page = 1, [Range(1, 100)] int pageSize = 20, CancellationToken ct = default) => Page(page, pageSize, query, ct);
    private async Task<ActionResult<MoviePage>> Page(int page, int pageSize, string? query, CancellationToken ct)
    {
        if (query is not null && string.IsNullOrWhiteSpace(query)) { ModelState.AddModelError("query", "Query must not be blank."); return ValidationProblem(ModelState); }
        var rows = db.Movies.AsNoTracking();
        if (query is not null) { var normalized = query.Trim().ToUpperInvariant(); rows = rows.Where(m => m.SearchTitle.Contains(normalized)); }
        var total = await rows.CountAsync(ct);
        var items = await rows.OrderBy(m => m.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var pages = (int)Math.Ceiling(total / (double)pageSize);
        return new MoviePage(items.Select(MovieDto.From).ToArray(), page, pageSize, total, pages, page < pages);
    }
    /// <summary>Get details. Demo certifications are not authoritative eligibility evidence.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<MovieDto>(200), ProducesResponseType<ProblemDetails>(404)]
    public async Task<ActionResult<MovieDto>> Get(int id, CancellationToken ct)
    {
        var movie = await db.Movies.AsNoTracking().SingleOrDefaultAsync(m => m.Id == id, ct);
        return movie is null ? Problem(statusCode: 404, title: "Movie not found.") : MovieDto.From(movie);
    }
    /// <summary>Edit allowed fields. Requires X-Admin-Key. X-Notification-Status reports post-commit dispatch.</summary>
    /// <remarks>Example: {"title":"Updated demo title","voteAverage":8.2,"posterUrl":null}. IDs and eligibility fields are immutable. Notification failure never rolls back a movie.</remarks>
    [HttpPatch("{id:int}"), RequireAdmin]
    [ProducesResponseType<MovieDto>(200), ProducesResponseType<ValidationProblemDetails>(400), ProducesResponseType<ProblemDetails>(401), ProducesResponseType<ProblemDetails>(404), ProducesResponseType<ProblemDetails>(409), ProducesResponseType<ProblemDetails>(503)]
    public async Task<ActionResult<MovieDto>> Patch(int id, [FromBody] JsonElement patch, CancellationToken ct)
    {
        var movie = await db.Movies.SingleOrDefaultAsync(m => m.Id == id, ct);
        if (movie is null) return Problem(statusCode: 404, title: "Movie not found.");
        var errors = MoviePatch.Apply(patch, movie);
        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));
        movie.LastModified = DateTime.UtcNow > movie.LastModified ? DateTime.UtcNow : movie.LastModified.AddTicks(1);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Problem(statusCode: 409, title: "Movie changed concurrently; fetch it again and retry."); }
        // DB transaction committed. Dispatch independently of client disconnects.
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        NotificationResult result;
        try { result = await notifier.MovieUpdatedAsync(movie.Id, movie.LastModified, timeout.Token); }
        catch (Exception ex)
        {
            logger.LogWarning("Movie {MovieId} saved; notification failed with {FailureType}", movie.Id, ex.GetType().Name);
            result = new("failed");
        }
        Response.Headers["X-Notification-Status"] = result.Status;
        Response.Headers["X-Notification-Accepted"] = result.Accepted.ToString();
        Response.Headers["X-Notification-Failed"] = result.Failed.ToString();
        return MovieDto.From(movie);
    }
}

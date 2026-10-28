using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieApp.Api.Configuration;
using MovieApp.Api.Data;
using MovieApp.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 32 * 1024);
builder.Services.AddProblemDetails();
builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow);
var databasePath = Path.GetFullPath(builder.Configuration["Database:Path"] ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "movies.db"));
Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
builder.Services.AddDbContext<MovieDbContext>(o => o.UseSqlite(new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder { DataSource = databasePath, DefaultTimeout = 10 }.ToString()));
builder.Services.AddSingleton<IFcmTransport, FirebaseTransport>();
builder.Services.AddScoped<IMovieNotifier, MovieNotifier>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new() { Title = "MovieApp Demo Backend", Version = "v1", Description = "Synthetic data; eligibility is unverified. PATCH requires X-Admin-Key. Dispatch outcomes are response headers, not proof of device delivery." });
    o.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "MovieApp.Api.xml"));
    o.OperationFilter<SwaggerExamples>();
});
builder.Services.AddRateLimiter(o =>
{
    o.AddPolicy("devices", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new() { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    o.OnRejected = async (context, ct) => await Results.Problem(statusCode: 429, title: "Too many device requests; retry later.").ExecuteAsync(context.HttpContext);
});
var app = builder.Build();
app.UseExceptionHandler(error => error.Run(async context =>
{
    var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    app.Logger.LogError("Request failed with {FailureType}; trace {TraceId}", ex?.GetType().Name, context.TraceIdentifier);
    var status = ex is BadHttpRequestException badRequest ? badRequest.StatusCode : 500;
    await Results.Problem(statusCode: status, title: status == 500 ? "An unexpected server error occurred." : "Invalid request.", extensions: new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }).ExecuteAsync(context);
}));
app.UseStatusCodePages(async context => await Results.Problem(statusCode: context.HttpContext.Response.StatusCode, title: "Request could not be completed.").ExecuteAsync(context.HttpContext));
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); app.UseSwaggerUI();
    app.MapGet("/admin", async (IWebHostEnvironment env) => Results.Content(await File.ReadAllTextAsync(Path.Combine(env.ContentRootPath, "Admin", "index.html")), "text/html")).ExcludeFromDescription();
}
else { app.UseHsts(); app.UseHttpsRedirection(); }
app.UseRateLimiter();
app.MapControllers();
app.MapGet("/health", async (MovieDbContext db, CancellationToken ct) => await db.Database.CanConnectAsync(ct) ? Results.Ok(new { status = "healthy", dataSource = "synthetic-demo" }) : Results.Problem(statusCode: 503, title: "Database unavailable."));
await using (var scope = app.Services.CreateAsyncScope()) { await scope.ServiceProvider.GetRequiredService<MovieDbContext>().Database.MigrateAsync(); }
app.Run();
public partial class Program;

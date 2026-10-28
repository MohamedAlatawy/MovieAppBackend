using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MovieApp.Api.Contracts;
using MovieApp.Api.Data;
namespace MovieApp.Api.Controllers;

[ApiController, Route("api/devices"), EnableRateLimiting("devices")]
public sealed class DevicesController(MovieDbContext db) : ControllerBase
{
    /// <summary>Register or refresh an opaque FCM token. Syntax checks do not prove Firebase validity.</summary>
    /// <remarks>Example: {"token":"REPLACE_WITH_REAL_DEVICE_FCM_TOKEN"}. Possession of the token is the local-demo registration/removal credential. Never embed the admin key in Android.</remarks>
    [HttpPost("register")]
    public async Task<IActionResult> Register(DeviceRequest request, CancellationToken ct)
    {
        var existing = await db.DeviceTokens.SingleOrDefaultAsync(t => t.Token == request.Token, ct);
        if (existing is null && await db.DeviceTokens.CountAsync(ct) >= 1000) return Problem(statusCode: 503, title: "Local demo device capacity reached.");
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO DeviceTokens (Token, UpdatedAt) VALUES ({request.Token}, {DateTime.UtcNow}) ON CONFLICT(Token) DO UPDATE SET UpdatedAt = excluded.UpdatedAt", ct);
        return Ok(new { status = "registered" });
    }
    /// <summary>Idempotently unregister a token by possession. No tokens are returned.</summary>
    [HttpPost("unregister")]
    public async Task<IActionResult> Unregister(DeviceRequest request, CancellationToken ct)
    {
        await db.DeviceTokens.Where(d => d.Token == request.Token).ExecuteDeleteAsync(ct);
        return NoContent();
    }
}

using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace MovieApp.Api.Configuration;

[AttributeUsage(AttributeTargets.Method)]
public sealed class RequireAdminAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var expected = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Admin:ApiKey"];
        var supplied = context.HttpContext.Request.Headers["X-Admin-Key"];
        if (string.IsNullOrEmpty(expected) || expected.Length < 32)
        {
            context.Result = new ObjectResult(new ProblemDetails { Status = 503, Title = "Administrator editing is not configured." }) { StatusCode = 503 };
            return;
        }
        if (supplied.Count != 1 || supplied[0]!.Length > 512 || !CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(expected)), SHA256.HashData(Encoding.UTF8.GetBytes(supplied[0]!))))
            context.Result = new ObjectResult(new ProblemDetails { Status = 401, Title = "A valid X-Admin-Key is required." }) { StatusCode = 401 };
    }
}

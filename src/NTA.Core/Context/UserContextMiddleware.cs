using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using NTA.Core.ValueObjects;

namespace NTA.Core.Context;

/// <summary>
/// Populates the scoped user context from the authenticated request principal.
/// </summary>
public sealed class UserContextMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Processes the request after JWT authentication has populated the principal.
    /// </summary>
    public async Task InvokeAsync(HttpContext httpContext, UserContext userContext)
    {
        var userIdValue = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdValue, CultureInfo.InvariantCulture, out var userId))
        {
            userContext.Set(
                new UserId(userId),
                httpContext.User.FindAll(ClaimTypes.Role).Select(claim => claim.Value));
        }

        await next(httpContext);
    }
}

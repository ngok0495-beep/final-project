using System.Security.Claims;
using UserService.Services;

namespace UserService.Middleware;

/// <summary>
/// Xác thực JWT (cùng secret với AuthService).
/// </summary>
public class AuthValidationMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly string[] _publicRoutes =
    [
        "/api/users/health",
        "/swagger",
        "/favicon.ico"
    ];

    public AuthValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IJwtTokenValidator jwtValidator)
    {
        var path = context.Request.Path.Value ?? "";

        if (_publicRoutes.Any(r => path.StartsWith(r, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();

        if (string.IsNullOrEmpty(authHeader))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { message = "Thiếu token xác thực." });
            return;
        }

        var validation = jwtValidator.Validate(authHeader);

        if (validation is null || !validation.IsValid)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { message = "Token không hợp lệ hoặc đã hết hạn." });
            return;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, validation.UserId ?? ""),
            new(ClaimTypes.Name, validation.Username ?? ""),
            new(ClaimTypes.Email, validation.Email ?? ""),
            new(ClaimTypes.Role, validation.Role ?? "User")
        };

        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        await _next(context);
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using UserService.Configuration;
using UserService.DTOs;

namespace UserService.Services;

public interface IJwtTokenValidator
{
    AuthValidationResponse? Validate(string bearerHeader);
}

public class JwtTokenValidator : IJwtTokenValidator
{
    private readonly JwtSettings _settings;

    public JwtTokenValidator(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public AuthValidationResponse? Validate(string bearerHeader)
    {
        if (string.IsNullOrWhiteSpace(bearerHeader) || !bearerHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;

        var token = bearerHeader["Bearer ".Length..].Trim();
        if (string.IsNullOrEmpty(token))
            return null;

        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
            var handler = new JwtSecurityTokenHandler();
            handler.MapInboundClaims = false;

            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _settings.Issuer,
                ValidateAudience = true,
                ValidAudience = _settings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            }, out _);

            var userId = GetClaim(principal, JwtRegisteredClaimNames.Sub, ClaimTypes.NameIdentifier, "sub");
            var email = GetClaim(principal, JwtRegisteredClaimNames.Email, ClaimTypes.Email, "email");
            var username = GetClaim(principal, ClaimTypes.Name, "unique_name", "name");
            var role = GetClaim(principal, ClaimTypes.Role, "role");

            if (string.IsNullOrEmpty(userId))
                return null;

            return new AuthValidationResponse(true, userId, username, email, role ?? "User");
        }
        catch
        {
            return null;
        }
    }

    private static string? GetClaim(ClaimsPrincipal principal, params string[] types)
    {
        foreach (var type in types)
        {
            var value = principal.FindFirst(type)?.Value;
            if (!string.IsNullOrEmpty(value))
                return value;
        }

        return null;
    }
}

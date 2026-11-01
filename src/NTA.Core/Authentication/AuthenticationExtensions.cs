using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NTA.Core.Context;

namespace NTA.Core.Authentication;

/// <summary>
/// Registers and configures Core JWT authentication services.
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Adds JWT authentication, authorization, token issuance, and request user context services.
    /// </summary>
    public static IServiceCollection AddCoreAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration is required.");
        options.Validate();

        services.AddSingleton<IOptions<JwtOptions>>(Options.Create(options));
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<UserContext>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwtBearerOptions =>
            {
                jwtBearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = options.Issuer,
                    ValidateAudience = true,
                    ValidAudience = options.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });
        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// Populates the scoped user context from the authenticated JWT principal.
    /// </summary>
    public static IApplicationBuilder UseUserContext(this IApplicationBuilder applicationBuilder) =>
        applicationBuilder.UseMiddleware<UserContextMiddleware>();
}

using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace EventParking.Api.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection
        AddJwtAuthentication(
            this IServiceCollection services,
            IConfiguration configuration)
    {
        var jwtSection =
            configuration.GetSection("Jwt");

        var key =
            jwtSection["Key"];

        var issuer =
            jwtSection["Issuer"];

        var audience =
            jwtSection["Audience"];

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException(
                "JWT Key is not configured.");
        }

        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new InvalidOperationException(
                "JWT Issuer is not configured.");
        }

        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException(
                "JWT Audience is not configured.");
        }

        services
            .AddAuthentication(
                options =>
                {
                    options.DefaultAuthenticateScheme =
                        JwtBearerDefaults
                            .AuthenticationScheme;

                    options.DefaultChallengeScheme =
                        JwtBearerDefaults
                            .AuthenticationScheme;
                })
            .AddJwtBearer(
                options =>
                {
                    options.RequireHttpsMetadata =
                        false;

                    options.SaveToken =
                        true;

                    options.TokenValidationParameters =
                        new TokenValidationParameters
                        {
                            ValidateIssuer =
                                true,

                            ValidateAudience =
                                true,

                            ValidateLifetime =
                                true,

                            ValidateIssuerSigningKey =
                                true,

                            ValidIssuer =
                                issuer,

                            ValidAudience =
                                audience,

                            IssuerSigningKey =
                                new SymmetricSecurityKey(
                                    Encoding.UTF8
                                        .GetBytes(key)),

                            ClockSkew =
                                TimeSpan.Zero
                        };
                });

        services.AddAuthorization();

        return services;
    }
}
namespace EventParking.Api.Extensions;

public static class CorsExtensions
{
    public const string AngularCorsPolicy =
        "AngularCorsPolicy";

    public static IServiceCollection
        AddAngularCors(
            this IServiceCollection services,
            IConfiguration configuration)
    {
        var configuredOrigins =
            configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>();

        var allowedOrigins =
            configuredOrigins != null &&
            configuredOrigins.Length > 0
                ? configuredOrigins
                : new[]
                {
                    "http://localhost:4200",
                    "https://localhost:4200"
                };

        services.AddCors(
            options =>
            {
                options.AddPolicy(
                    AngularCorsPolicy,
                    policy =>
                    {
                        policy
                            .WithOrigins(
                                allowedOrigins)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    });
            });

        return services;
    }
}
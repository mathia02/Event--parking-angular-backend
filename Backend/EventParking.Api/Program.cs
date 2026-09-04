using EventParking.Api.Extensions;
using EventParking.Api.Middleware;
using EventParking.DataAccess.Context;
using EventParking.DataAccess.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder =
    WebApplication.CreateBuilder(args);

// ---------------------------------------------------------
// CONTROLLERS
// ---------------------------------------------------------

builder.Services
    .AddControllers();

// ---------------------------------------------------------
// DATABASE
// ---------------------------------------------------------

var connectionString =
    builder.Configuration
        .GetConnectionString(
            "DefaultConnection");

if (string.IsNullOrWhiteSpace(
        connectionString))
{
    throw new InvalidOperationException(
        "DefaultConnection is not configured.");
}

builder.Services
    .AddDbContext<ApplicationDbContext>(
        options =>
            options.UseSqlServer(
                connectionString,
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(
                        typeof(ApplicationDbContext)
                            .Assembly
                            .FullName);
                }));

// ---------------------------------------------------------
// APPLICATION SERVICES
// ---------------------------------------------------------

builder.Services
    .AddApplicationServices();

// ---------------------------------------------------------
// JWT AUTHENTICATION
// ---------------------------------------------------------

builder.Services
    .AddJwtAuthentication(
        builder.Configuration);

// ---------------------------------------------------------
// CORS
// ---------------------------------------------------------

builder.Services
    .AddAngularCors(
        builder.Configuration);

// ---------------------------------------------------------
// SWAGGER
// ---------------------------------------------------------

builder.Services
    .AddEndpointsApiExplorer();

builder.Services
    .AddSwaggerGen(
        options =>
        {
            options.SwaggerDoc(
                "v1",
                new OpenApiInfo
                {
                    Title =
                        "Event & Parking Reservation API",

                    Version =
                        "v1",

                    Description =
                        "Backend API for Event & Parking Reservation System."
                });

            options.AddSecurityDefinition(
                "Bearer",
                new OpenApiSecurityScheme
                {
                    Name =
                        "Authorization",

                    Type =
                        SecuritySchemeType.Http,

                    Scheme =
                        "bearer",

                    BearerFormat =
                        "JWT",

                    In =
                        ParameterLocation.Header,

                    Description =
                        "Enter your JWT token."
                });

            options.AddSecurityRequirement(
                new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference =
                                new OpenApiReference
                                {
                                    Type =
                                        ReferenceType
                                            .SecurityScheme,

                                    Id =
                                        "Bearer"
                                }
                        },
                        Array.Empty<string>()
                    }
                });
        });

var app =
    builder.Build();

// ---------------------------------------------------------
// DATABASE SEED
// ---------------------------------------------------------

using (var scope =
       app.Services.CreateScope())
{
    var dbContext =
        scope.ServiceProvider
            .GetRequiredService<
                ApplicationDbContext>();

    await DbSeeder.SeedAsync(
        dbContext);
}

// ---------------------------------------------------------
// GLOBAL EXCEPTION HANDLING
// ---------------------------------------------------------

app.UseMiddleware<
    ExceptionHandlingMiddleware>();

// ---------------------------------------------------------
// SWAGGER
// ---------------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(
        options =>
        {
            options.SwaggerEndpoint(
                "/swagger/v1/swagger.json",
                "Event & Parking Reservation API v1");

            options.DocumentTitle =
                "Event Parking Reservation API";
        });
}

// ---------------------------------------------------------
// HTTPS
// ---------------------------------------------------------

app.UseHttpsRedirection();

// ---------------------------------------------------------
// CORS
// ---------------------------------------------------------

app.UseCors(
    CorsExtensions
        .AngularCorsPolicy);

// ---------------------------------------------------------
// AUTHENTICATION + AUTHORIZATION
// ---------------------------------------------------------

app.UseAuthentication();

app.UseAuthorization();

// ---------------------------------------------------------
// CONTROLLERS
// ---------------------------------------------------------

app.MapControllers();

// ---------------------------------------------------------
// HEALTH ENDPOINT
// ---------------------------------------------------------

app.MapGet(
        "/api/health",
        () =>
            Results.Ok(
                new
                {
                    status =
                        "Healthy",

                    application =
                        "EventParking.Api",

                    utcTime =
                        DateTime.UtcNow
                }))
    .AllowAnonymous();

// ---------------------------------------------------------
// RUN
// ---------------------------------------------------------

app.Run();
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EventParking.Business.Interfaces;
using EventParking.Models.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace EventParking.Business.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime ExpiresAt)
        GenerateJwtToken(Customer customer)
    {
        var key = _configuration["Jwt:Key"];

        var issuer = _configuration["Jwt:Issuer"];

        var audience = _configuration["Jwt:Audience"];

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException(
                "JWT Key is not configured.");
        }

        var expiryMinutes =
            int.TryParse(
                _configuration["Jwt:ExpiryMinutes"],
                out var minutes)
                ? minutes
                : 60;

        var expiresAt =
            DateTime.UtcNow.AddMinutes(expiryMinutes);

        var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                customer.Id.ToString()),

            new(
                ClaimTypes.Name,
                customer.FullName),

            new(
                ClaimTypes.Email,
                customer.Email),

            new(
                ClaimTypes.Role,
                customer.Role),

            new(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
        };

        var securityKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(key));

        var credentials =
            new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);

        var jwtToken =
            new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials);

        var token =
            new JwtSecurityTokenHandler()
                .WriteToken(jwtToken);

        return (token, expiresAt);
    }
}
using EventParking.Models.Entities;

namespace EventParking.Business.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt)
        GenerateJwtToken(Customer customer);
}
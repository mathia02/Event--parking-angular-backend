using EventParking.Models.DTOs.Auth;
using EventParking.Models.DTOs.Customer;

namespace EventParking.Business.Interfaces;

public interface IAuthService
{
    Task<CustomerDto> RegisterAsync(
        RegisterRequestDto dto);

    Task<LoginResponseDto> LoginAsync(
        LoginRequestDto dto);

    Task VerifyEmailAsync(string token);

    Task ResendVerificationAsync(
        ResendVerificationDto dto);

    Task ForgotPasswordAsync(
        ForgotPasswordDto dto);

    Task ResetPasswordAsync(
        ResetPasswordDto dto);
}
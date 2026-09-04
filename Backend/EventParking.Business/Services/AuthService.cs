using System.Security.Cryptography;
using System.Text;
using EventParking.Business.Exceptions;
using EventParking.Business.Interfaces;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.DTOs.Auth;
using EventParking.Models.DTOs.Customer;
using EventParking.Models.Entities;
using EventParking.Models.Enums;
using Microsoft.Extensions.Configuration;

namespace EventParking.Business.Services;

public class AuthService : IAuthService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public AuthService(
        ICustomerRepository customerRepository,
        ITokenService tokenService,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _customerRepository = customerRepository;
        _tokenService = tokenService;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<CustomerDto> RegisterAsync(
        RegisterRequestDto dto)
    {
        if (dto.Password != dto.ConfirmPassword)
        {
            throw new ValidationException(
                "Password and confirmation password do not match.");
        }

        var email = dto.Email.Trim();

        if (await _customerRepository.EmailExistsAsync(email))
        {
            throw new ConflictException(
                "An account with this email already exists.");
        }

        var verificationToken =
            GenerateSecureToken();

        var verificationHours =
            int.TryParse(
                _configuration[
                    "Authentication:EmailVerificationExpiryHours"],
                out var hours)
                ? hours
                : 24;

        var customer = new Customer
        {
            FullName = dto.FullName.Trim(),
            Email = email,
            Phone = dto.Phone.Trim(),

            PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(
                    dto.Password),

            Role = "Customer",

            Status = CustomerStatus.Active,

            EmailVerified = false,

            EmailVerificationTokenHash =
                HashToken(verificationToken),

            EmailVerificationTokenExpiresAt =
                DateTime.UtcNow.AddHours(
                    verificationHours),

            CreatedAt = DateTime.UtcNow
        };

        await _customerRepository.AddAsync(customer);

        await _customerRepository.SaveChangesAsync();

        await _emailService
            .SendVerificationEmailAsync(
                customer.Email,
                customer.FullName,
                verificationToken);

        return MapToCustomerDto(customer);
    }

    public async Task<LoginResponseDto> LoginAsync(
        LoginRequestDto dto)
    {
        var customer =
            await _customerRepository
                .GetByEmailAsync(dto.Email.Trim());

        if (customer is null ||
            !BCrypt.Net.BCrypt.Verify(
                dto.Password,
                customer.PasswordHash))
        {
            throw new UnauthorizedException(
                "Invalid email or password.",
                401);
        }

        if (customer.Status ==
            CustomerStatus.Deactivated)
        {
            throw new UnauthorizedException(
                "This account has been deactivated.",
                403);
        }

        if (!customer.EmailVerified)
        {
            throw new UnauthorizedException(
                "Please verify your email before logging in.",
                403);
        }

        var (token, expiresAt) =
            _tokenService.GenerateJwtToken(customer);

        return new LoginResponseDto
        {
            CustomerId = customer.Id,
            FullName = customer.FullName,
            Email = customer.Email,
            Role = customer.Role,
            EmailVerified = customer.EmailVerified,
            Token = token,
            ExpiresAt = expiresAt
        };
    }

    public async Task VerifyEmailAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ValidationException(
                "Verification token is required.");
        }

        var tokenHash = HashToken(token);

        var customer =
            await _customerRepository
                .GetByEmailVerificationTokenHashAsync(
                    tokenHash);

        if (customer is null)
        {
            throw new ValidationException(
                "Invalid verification token.");
        }

        if (customer.EmailVerificationTokenExpiresAt
            is null ||
            customer.EmailVerificationTokenExpiresAt
                <= DateTime.UtcNow)
        {
            throw new ValidationException(
                "Verification token has expired.");
        }

        customer.EmailVerified = true;

        customer.EmailVerificationTokenHash = null;

        customer.EmailVerificationTokenExpiresAt = null;

        customer.UpdatedAt = DateTime.UtcNow;

        _customerRepository.Update(customer);

        await _customerRepository.SaveChangesAsync();
    }

    public async Task ResendVerificationAsync(
        ResendVerificationDto dto)
    {
        var customer =
            await _customerRepository
                .GetByEmailAsync(dto.Email.Trim());

        if (customer is null ||
            customer.EmailVerified)
        {
            return;
        }

        var verificationToken =
            GenerateSecureToken();

        var verificationHours =
            int.TryParse(
                _configuration[
                    "Authentication:EmailVerificationExpiryHours"],
                out var hours)
                ? hours
                : 24;

        customer.EmailVerificationTokenHash =
            HashToken(verificationToken);

        customer.EmailVerificationTokenExpiresAt =
            DateTime.UtcNow.AddHours(
                verificationHours);

        customer.UpdatedAt = DateTime.UtcNow;

        _customerRepository.Update(customer);

        await _customerRepository.SaveChangesAsync();

        await _emailService
            .SendVerificationEmailAsync(
                customer.Email,
                customer.FullName,
                verificationToken);
    }

    public async Task ForgotPasswordAsync(
        ForgotPasswordDto dto)
    {
        var customer =
            await _customerRepository
                .GetByEmailAsync(dto.Email.Trim());

        // Generic success behaviour.
        if (customer is null)
        {
            return;
        }

        var resetToken =
            GenerateSecureToken();

        var resetMinutes =
            int.TryParse(
                _configuration[
                    "Authentication:PasswordResetExpiryMinutes"],
                out var minutes)
                ? minutes
                : 60;

        customer.PasswordResetTokenHash =
            HashToken(resetToken);

        customer.PasswordResetTokenExpiresAt =
            DateTime.UtcNow.AddMinutes(
                resetMinutes);

        customer.UpdatedAt = DateTime.UtcNow;

        _customerRepository.Update(customer);

        await _customerRepository.SaveChangesAsync();

        await _emailService
            .SendPasswordResetEmailAsync(
                customer.Email,
                customer.FullName,
                resetToken);
    }

    public async Task ResetPasswordAsync(
        ResetPasswordDto dto)
    {
        if (dto.NewPassword != dto.ConfirmPassword)
        {
            throw new ValidationException(
                "Password and confirmation password do not match.");
        }

        var tokenHash =
            HashToken(dto.Token);

        var customer =
            await _customerRepository
                .GetByPasswordResetTokenHashAsync(
                    tokenHash);

        if (customer is null)
        {
            throw new ValidationException(
                "Invalid password reset token.");
        }

        if (customer.PasswordResetTokenExpiresAt
            is null ||
            customer.PasswordResetTokenExpiresAt
                <= DateTime.UtcNow)
        {
            throw new ValidationException(
                "Password reset token has expired.");
        }

        customer.PasswordHash =
            BCrypt.Net.BCrypt.HashPassword(
                dto.NewPassword);

        // Makes token single-use.
        customer.PasswordResetTokenHash = null;

        customer.PasswordResetTokenExpiresAt = null;

        customer.UpdatedAt = DateTime.UtcNow;

        _customerRepository.Update(customer);

        await _customerRepository.SaveChangesAsync();
    }

    private static string GenerateSecureToken()
    {
        return Convert.ToHexString(
            RandomNumberGenerator.GetBytes(32));
    }

    private static string HashToken(string token)
    {
        var bytes =
            SHA256.HashData(
                Encoding.UTF8.GetBytes(token));

        return Convert.ToHexString(bytes);
    }

    private static CustomerDto MapToCustomerDto(
        Customer customer)
    {
        return new CustomerDto
        {
            Id = customer.Id,
            FullName = customer.FullName,
            Email = customer.Email,
            Phone = customer.Phone,
            Role = customer.Role,
            Status = customer.Status,
            EmailVerified = customer.EmailVerified,
            CreatedAt = customer.CreatedAt
        };
    }
}
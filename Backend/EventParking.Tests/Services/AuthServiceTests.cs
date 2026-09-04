using System.Security.Cryptography;
using System.Text;
using EventParking.Business.Exceptions;
using EventParking.Business.Interfaces;
using EventParking.Business.Services;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.DTOs.Auth;
using EventParking.Models.Entities;
using EventParking.Models.Enums;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace EventParking.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<ICustomerRepository> _customerRepositoryMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IConfiguration> _configurationMock;

    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _customerRepositoryMock =
            new Mock<ICustomerRepository>();

        _tokenServiceMock =
            new Mock<ITokenService>();

        _emailServiceMock =
            new Mock<IEmailService>();

        _configurationMock =
            new Mock<IConfiguration>();

        _configurationMock
            .Setup(x =>
                x["Authentication:EmailVerificationExpiryHours"])
            .Returns("24");

        _configurationMock
            .Setup(x =>
                x["Authentication:PasswordResetExpiryMinutes"])
            .Returns("60");

        _authService = new AuthService(
            _customerRepositoryMock.Object,
            _tokenServiceMock.Object,
            _emailServiceMock.Object,
            _configurationMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateUnverifiedCustomer()
    {
        // Arrange
        var dto = new RegisterRequestDto
        {
            FullName = "Test Customer",
            Email = "customer@test.com",
            Phone = "0771234567",
            Password = "Test@123",
            ConfirmPassword = "Test@123"
        };

        Customer? savedCustomer = null;

        _customerRepositoryMock
            .Setup(x =>
                x.EmailExistsAsync(
                    dto.Email,
                    null))
            .ReturnsAsync(false);

        _customerRepositoryMock
            .Setup(x => x.AddAsync(
                It.IsAny<Customer>()))
            .Callback<Customer>(
                customer =>
                    savedCustomer = customer)
            .Returns(Task.CompletedTask);

        _customerRepositoryMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        _emailServiceMock
            .Setup(x =>
                x.SendVerificationEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result =
            await _authService.RegisterAsync(dto);

        // Assert
        Assert.NotNull(savedCustomer);

        Assert.Equal(
            "Test Customer",
            savedCustomer!.FullName);

        Assert.Equal(
            "customer@test.com",
            savedCustomer.Email);

        Assert.Equal(
            "Customer",
            savedCustomer.Role);

        Assert.False(
            savedCustomer.EmailVerified);

        Assert.Equal(
            CustomerStatus.Active,
            savedCustomer.Status);

        Assert.NotEqual(
            dto.Password,
            savedCustomer.PasswordHash);

        Assert.NotNull(
            savedCustomer.EmailVerificationTokenHash);

        Assert.NotNull(
            savedCustomer.EmailVerificationTokenExpiresAt);

        Assert.Equal(
            dto.Email,
            result.Email);

        _emailServiceMock.Verify(
            x =>
                x.SendVerificationEmailAsync(
                    dto.Email,
                    dto.FullName,
                    It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailExists_ShouldThrowConflictException()
    {
        // Arrange
        var dto = new RegisterRequestDto
        {
            FullName = "Test Customer",
            Email = "customer@test.com",
            Phone = "0771234567",
            Password = "Test@123",
            ConfirmPassword = "Test@123"
        };

        _customerRepositoryMock
            .Setup(x =>
                x.EmailExistsAsync(
                    dto.Email,
                    null))
            .ReturnsAsync(true);

        // Act + Assert
        await Assert.ThrowsAsync<ConflictException>(
            () =>
                _authService.RegisterAsync(dto));

        _customerRepositoryMock.Verify(
            x => x.AddAsync(
                It.IsAny<Customer>()),
            Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenEmailNotVerified_ShouldReturn403()
    {
        // Arrange
        var customer = new Customer
        {
            Id = 1,
            FullName = "Test Customer",
            Email = "customer@test.com",
            Phone = "0771234567",
            PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(
                    "Test@123"),
            Role = "Customer",
            Status = CustomerStatus.Active,
            EmailVerified = false
        };

        _customerRepositoryMock
            .Setup(x =>
                x.GetByEmailAsync(
                    "customer@test.com"))
            .ReturnsAsync(customer);

        var dto = new LoginRequestDto
        {
            Email = "customer@test.com",
            Password = "Test@123"
        };

        // Act
        var exception =
            await Assert.ThrowsAsync<
                UnauthorizedException>(
                () =>
                    _authService.LoginAsync(dto));

        // Assert
        Assert.Equal(
            403,
            exception.StatusCode);
    }

    [Fact]
    public async Task LoginAsync_WithVerifiedCustomer_ShouldReturnJwtToken()
    {
        // Arrange
        var customer = new Customer
        {
            Id = 1,
            FullName = "Test Customer",
            Email = "customer@test.com",
            Phone = "0771234567",
            PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(
                    "Test@123"),
            Role = "Customer",
            Status = CustomerStatus.Active,
            EmailVerified = true
        };

        _customerRepositoryMock
            .Setup(x =>
                x.GetByEmailAsync(
                    customer.Email))
            .ReturnsAsync(customer);

        var expiry =
            DateTime.UtcNow.AddHours(1);

        _tokenServiceMock
            .Setup(x =>
                x.GenerateJwtToken(customer))
            .Returns(
                ("test-jwt-token", expiry));

        var dto = new LoginRequestDto
        {
            Email = customer.Email,
            Password = "Test@123"
        };

        // Act
        var result =
            await _authService.LoginAsync(dto);

        // Assert
        Assert.Equal(
            customer.Id,
            result.CustomerId);

        Assert.Equal(
            "Customer",
            result.Role);

        Assert.True(
            result.EmailVerified);

        Assert.Equal(
            "test-jwt-token",
            result.Token);

        Assert.Equal(
            expiry,
            result.ExpiresAt);
    }

    [Fact]
    public async Task VerifyEmailAsync_WithValidToken_ShouldVerifyCustomer()
    {
        // Arrange
        const string token =
            "EMAIL-VERIFICATION-TOKEN";

        var tokenHash =
            HashToken(token);

        var customer = new Customer
        {
            Id = 1,
            FullName = "Test Customer",
            Email = "customer@test.com",
            EmailVerified = false,
            EmailVerificationTokenHash =
                tokenHash,
            EmailVerificationTokenExpiresAt =
                DateTime.UtcNow.AddHours(1)
        };

        _customerRepositoryMock
            .Setup(x =>
                x.GetByEmailVerificationTokenHashAsync(
                    tokenHash))
            .ReturnsAsync(customer);

        _customerRepositoryMock
            .Setup(x =>
                x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        await _authService
            .VerifyEmailAsync(token);

        // Assert
        Assert.True(
            customer.EmailVerified);

        Assert.Null(
            customer.EmailVerificationTokenHash);

        Assert.Null(
            customer.EmailVerificationTokenExpiresAt);

        _customerRepositoryMock.Verify(
            x => x.Update(customer),
            Times.Once);

        _customerRepositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WhenEmailDoesNotExist_ShouldStillComplete()
    {
        // Arrange
        var dto = new ForgotPasswordDto
        {
            Email = "unknown@test.com"
        };

        _customerRepositoryMock
            .Setup(x =>
                x.GetByEmailAsync(
                    dto.Email))
            .ReturnsAsync((Customer?)null);

        // Act
        await _authService
            .ForgotPasswordAsync(dto);

        // Assert
        _emailServiceMock.Verify(
            x =>
                x.SendPasswordResetEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_ShouldChangePasswordAndInvalidateToken()
    {
        // Arrange
        const string resetToken =
            "VALID-RESET-TOKEN";

        var tokenHash =
            HashToken(resetToken);

        var customer = new Customer
        {
            Id = 1,
            FullName = "Test Customer",
            Email = "customer@test.com",

            PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(
                    "OldPassword@123"),

            PasswordResetTokenHash =
                tokenHash,

            PasswordResetTokenExpiresAt =
                DateTime.UtcNow.AddMinutes(30)
        };

        _customerRepositoryMock
            .Setup(x =>
                x.GetByPasswordResetTokenHashAsync(
                    tokenHash))
            .ReturnsAsync(customer);

        _customerRepositoryMock
            .Setup(x =>
                x.SaveChangesAsync())
            .ReturnsAsync(1);

        var dto = new ResetPasswordDto
        {
            Token = resetToken,
            NewPassword = "NewPassword@123",
            ConfirmPassword = "NewPassword@123"
        };

        // Act
        await _authService
            .ResetPasswordAsync(dto);

        // Assert
        Assert.True(
            BCrypt.Net.BCrypt.Verify(
                "NewPassword@123",
                customer.PasswordHash));

        Assert.False(
            BCrypt.Net.BCrypt.Verify(
                "OldPassword@123",
                customer.PasswordHash));

        Assert.Null(
            customer.PasswordResetTokenHash);

        Assert.Null(
            customer.PasswordResetTokenExpiresAt);

        _customerRepositoryMock.Verify(
            x => x.Update(customer),
            Times.Once);

        _customerRepositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }

    private static string HashToken(string token)
    {
        var bytes =
            SHA256.HashData(
                Encoding.UTF8.GetBytes(token));

        return Convert.ToHexString(bytes);
    }
}
using EventParking.Api.Controllers;
using EventParking.Business.Exceptions;
using EventParking.Business.Interfaces;
using EventParking.Models.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace EventParking.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock =
            new Mock<IAuthService>();

        _controller =
            new AuthController(
                _authServiceMock.Object);
    }

    [Fact]
    public async Task Login_ShouldReturnOk_AndCallService()
    {
        // Arrange
        var dto =
            new LoginRequestDto();

        // Act
        var result =
            await _controller.Login(dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);

        _authServiceMock.Verify(
            x => x.LoginAsync(dto),
            Times.Once);
    }

    [Fact]
    public async Task VerifyEmail_ShouldReturnOk_WhenTokenIsValid()
    {
        // Arrange
        const string token =
            "valid-token";

        // Act
        var result =
            await _controller.VerifyEmail(token);

        // Assert
        Assert.IsType<OkObjectResult>(result);

        _authServiceMock.Verify(
            x => x.VerifyEmailAsync(token),
            Times.Once);
    }

    [Fact]
    public async Task VerifyEmail_ShouldReturnBadRequest_WhenValidationExceptionOccurs()
    {
        // Arrange
        const string token =
            "invalid-token";

        _authServiceMock
            .Setup(x =>
                x.VerifyEmailAsync(token))
            .ThrowsAsync(
                new ValidationException(
                    "Invalid verification token."));

        // Act
        var result =
            await _controller.VerifyEmail(token);

        // Assert
        Assert.IsType<BadRequestObjectResult>(
            result);
    }

    [Fact]
    public async Task ResendVerification_ShouldReturnOk_AndCallService()
    {
        // Arrange
        var dto =
            new ResendVerificationDto();

        // Act
        var result =
            await _controller
                .ResendVerification(dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);

        _authServiceMock.Verify(
            x =>
                x.ResendVerificationAsync(dto),
            Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_ShouldReturnOk_AndCallService()
    {
        // Arrange
        var dto =
            new ForgotPasswordDto();

        // Act
        var result =
            await _controller
                .ForgotPassword(dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);

        _authServiceMock.Verify(
            x =>
                x.ForgotPasswordAsync(dto),
            Times.Once);
    }

    [Fact]
    public async Task ResetPassword_ShouldReturnBadRequest_WhenValidationExceptionOccurs()
    {
        // Arrange
        var dto =
            new ResetPasswordDto();

        _authServiceMock
            .Setup(x =>
                x.ResetPasswordAsync(dto))
            .ThrowsAsync(
                new ValidationException(
                    "Invalid reset token."));

        // Act
        var result =
            await _controller
                .ResetPassword(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(
            result);
    }
}
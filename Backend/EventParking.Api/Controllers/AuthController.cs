using EventParking.Business.Exceptions;
using EventParking.Business.Interfaces;
using EventParking.Models.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventParking.Api.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto dto)
    {
        try
        {
            var result =
                await _authService.LoginAsync(dto);

            return Ok(result);
        }
        catch (UnauthorizedException ex)
        {
            return StatusCode(
                ex.StatusCode,
                new
                {
                    message = ex.Message
                });
        }
    }

    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail(
        [FromQuery] string token)
    {
        try
        {
            await _authService.VerifyEmailAsync(token);

            return Ok(new
            {
                message =
                    "Email verified successfully."
            });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult>
        ResendVerification(
            [FromBody] ResendVerificationDto dto)
    {
        await _authService
            .ResendVerificationAsync(dto);

        return Ok(new
        {
            message =
                "If the account exists and is not verified, a new verification email has been sent."
        });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordDto dto)
    {
        await _authService
            .ForgotPasswordAsync(dto);

        return Ok(new
        {
            message =
                "If an account exists for this email, password reset instructions have been sent."
        });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordDto dto)
    {
        try
        {
            await _authService
                .ResetPasswordAsync(dto);

            return Ok(new
            {
                message =
                    "Password reset successfully."
            });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
}
using System.Security.Claims;
using EventParking.Business.Exceptions;
using EventParking.Business.Interfaces;
using EventParking.Models.DTOs.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventParking.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService
        _paymentService;

    public PaymentsController(
        IPaymentService paymentService)
    {
        _paymentService =
            paymentService;
    }

    [Authorize(Roles = "Administrator")]
    [HttpGet("payments")]
    public async Task<IActionResult> GetAll()
    {
        var result =
            await _paymentService
                .GetAllAsync();

        return Ok(result);
    }

    [Authorize(Roles = "Customer")]
    [HttpGet("payments/my")]
    public async Task<IActionResult>
        GetMyPayments()
    {
        try
        {
            var customerId =
                GetCurrentUserId();

            var result =
                await _paymentService
                    .GetMyPaymentsAsync(
                        customerId);

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new
                {
                    message = ex.Message
                });
        }
    }

    [HttpGet("payments/{id:int}")]
    public async Task<IActionResult>
        GetById(int id)
    {
        try
        {
            var requesterId =
                GetCurrentUserId();

            var isAdministrator =
                User.IsInRole(
                    "Administrator");

            var result =
                await _paymentService
                    .GetByIdAsync(
                        id,
                        requesterId,
                        isAdministrator);

            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(
                new
                {
                    message = ex.Message
                });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new
                {
                    message = ex.Message
                });
        }
    }

    [Authorize(Roles = "Customer")]
    [HttpPost(
        "bookings/{bookingId:int}/payments")]
    public async Task<IActionResult> Create(
        int bookingId,
        [FromBody] PaymentCreateDto dto)
    {
        try
        {
            var customerId =
                GetCurrentUserId();

            var result =
                await _paymentService
                    .CreateAsync(
                        bookingId,
                        customerId,
                        dto);

            return CreatedAtAction(
                nameof(GetById),
                new
                {
                    id = result.Id
                },
                result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(
                new
                {
                    message = ex.Message
                });
        }
        catch (ValidationException ex)
        {
            return BadRequest(
                new
                {
                    message = ex.Message
                });
        }
        catch (ConflictException ex)
        {
            return Conflict(
                new
                {
                    message = ex.Message
                });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new
                {
                    message = ex.Message
                });
        }
    }

    private int GetCurrentUserId()
    {
        var value =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!int.TryParse(
                value,
                out var userId))
        {
            throw new UnauthorizedAccessException(
                "The authenticated user ID is invalid.");
        }

        return userId;
    }
}
using System.Security.Claims;
using EventParking.Business.Exceptions;
using EventParking.Business.Interfaces;
using EventParking.Models.DTOs.Booking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventParking.Api.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(
        IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [Authorize(Roles = "Administrator")]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result =
            await _bookingService.GetAllAsync();

        return Ok(result);
    }

    [Authorize(Roles = "Customer")]
    [HttpGet("my")]
    public async Task<IActionResult> GetMyBookings()
    {
        try
        {
            var customerId =
                GetCurrentUserId();

            var result =
                await _bookingService
                    .GetMyBookingsAsync(
                        customerId);

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var requesterId =
                GetCurrentUserId();

            var isAdministrator =
                User.IsInRole(
                    "Administrator");

            var result =
                await _bookingService
                    .GetByIdAsync(
                        id,
                        requesterId,
                        isAdministrator);

            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(
                new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Customer")]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] BookingCreateDto dto)
    {
        try
        {
            var customerId =
                GetCurrentUserId();

            var result =
                await _bookingService
                    .CreateAsync(
                        customerId,
                        dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Id },
                result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(
                new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(
                new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(
                new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        try
        {
            var requesterId =
                GetCurrentUserId();

            var isAdministrator =
                User.IsInRole(
                    "Administrator");

            var result =
                await _bookingService
                    .CancelAsync(
                        id,
                        requesterId,
                        isAdministrator);

            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(
                new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(
                new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(
                new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { message = ex.Message });
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
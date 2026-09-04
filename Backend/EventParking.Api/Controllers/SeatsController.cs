using EventParking.Business.Exceptions;
using EventParking.Business.Interfaces;
using EventParking.Models.DTOs.Seat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventParking.Api.Controllers;

[ApiController]
[Route("api")]
public class SeatsController : ControllerBase
{
    private readonly ISeatService _seatService;

    public SeatsController(
        ISeatService seatService)
    {
        _seatService = seatService;
    }

    // GET /api/events/1/seats
    // GET /api/events/1/seats?availableOnly=true
    [AllowAnonymous]
    [HttpGet("events/{eventId:int}/seats")]
    public async Task<IActionResult>
        GetEventSeats(
            int eventId,
            [FromQuery]
            bool availableOnly = false)
    {
        try
        {
            var seats =
                await _seatService
                    .GetByEventIdAsync(
                        eventId,
                        availableOnly);

            return Ok(seats);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
    }

    // GET /api/seats/1
    [AllowAnonymous]
    [HttpGet("seats/{id:int}")]
    public async Task<IActionResult>
        GetSeatById(int id)
    {
        try
        {
            var seat =
                await _seatService
                    .GetByIdAsync(id);

            return Ok(seat);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
    }

    // POST /api/events/1/seats
    [Authorize(Roles = "Administrator")]
    [HttpPost("events/{eventId:int}/seats")]
    public async Task<IActionResult> Create(
        int eventId,
        [FromBody] SeatCreateDto dto)
    {
        try
        {
            var seat =
                await _seatService
                    .CreateAsync(
                        eventId,
                        dto);

            return CreatedAtAction(
                nameof(GetSeatById),
                new
                {
                    id = seat.Id
                },
                seat);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (ConflictException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    // POST
    // /api/events/1/seats/generate
    [Authorize(Roles = "Administrator")]
    [HttpPost(
        "events/{eventId:int}/seats/generate")]
    public async Task<IActionResult>
        GenerateSeatMap(
            int eventId,
            [FromQuery] int rows,
            [FromQuery] int seatsPerRow,
            [FromQuery] string? seatType = null,
            [FromQuery] decimal? priceOverride = null)
    {
        try
        {
            var seats =
                await _seatService
                    .GenerateSeatMapAsync(
                        eventId,
                        rows,
                        seatsPerRow,
                        seatType,
                        priceOverride);

            return StatusCode(
                StatusCodes.Status201Created,
                new
                {
                    message =
                        "Seat map generated successfully.",

                    totalSeats =
                        seats.Count,

                    seats
                });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (ConflictException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    // PUT /api/seats/1
    [Authorize(Roles = "Administrator")]
    [HttpPut("seats/{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SeatUpdateDto dto)
    {
        try
        {
            var seat =
                await _seatService
                    .UpdateAsync(
                        id,
                        dto);

            return Ok(seat);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (ConflictException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    // DELETE /api/seats/1
    [Authorize(Roles = "Administrator")]
    [HttpDelete("seats/{id:int}")]
    public async Task<IActionResult> Delete(
        int id)
    {
        try
        {
            await _seatService
                .DeleteAsync(id);

            return Ok(new
            {
                message =
                    "Seat deleted successfully."
            });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (ConflictException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }
}
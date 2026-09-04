using EventParking.Business.Exceptions;
using EventParking.Business.Interfaces;
using EventParking.Models.DTOs.Venue;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventParking.Api.Controllers;

[ApiController]
[Route("api/venues")]
public class VenuesController : ControllerBase
{
    private readonly IVenueService _venueService;

    public VenuesController(IVenueService venueService)
    {
        _venueService = venueService;
    }

    // GET /api/venues
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var venues =
            await _venueService.GetAllAsync();

        return Ok(venues);
    }

    // GET /api/venues/1
    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var venue =
                await _venueService.GetByIdAsync(id);

            return Ok(venue);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
    }

    // GET /api/venues/available
    // ?date=2026-09-10
    // &startTime=10:00
    // &endTime=12:00
    // &venueId=1
    [AllowAnonymous]
    [HttpGet("available")]
    public async Task<IActionResult> GetAvailability(
        [FromQuery] DateOnly date,
        [FromQuery] TimeOnly startTime,
        [FromQuery] TimeOnly endTime,
        [FromQuery] int? venueId = null)
    {
        try
        {
            var startDateTime =
                date.ToDateTime(startTime);

            var endDateTime =
                date.ToDateTime(endTime);

            if (venueId.HasValue)
            {
                var venue =
                    await _venueService
                        .GetByIdAsync(
                            venueId.Value);

                var isAvailable =
                    await _venueService
                        .IsAvailableAsync(
                            venueId.Value,
                            startDateTime,
                            endDateTime);

                return Ok(new
                {
                    venueId = venue.Id,
                    venueName = venue.Name,
                    startDateTime,
                    endDateTime,
                    isAvailable
                });
            }

            var availableVenues =
                await _venueService
                    .GetAvailableAsync(
                        startDateTime,
                        endDateTime);

            return Ok(availableVenues);
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
    }

    // POST /api/venues
    [Authorize(Roles = "Administrator")]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] VenueCreateDto dto)
    {
        try
        {
            var venue =
                await _venueService
                    .CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new
                {
                    id = venue.Id
                },
                venue);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // PUT /api/venues/1
    [Authorize(Roles = "Administrator")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] VenueUpdateDto dto)
    {
        try
        {
            var venue =
                await _venueService
                    .UpdateAsync(
                        id,
                        dto);

            return Ok(venue);
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
    }

    // DELETE /api/venues/1
    [Authorize(Roles = "Administrator")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _venueService.DeleteAsync(id);

            return Ok(new
            {
                message =
                    "Venue deleted successfully."
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
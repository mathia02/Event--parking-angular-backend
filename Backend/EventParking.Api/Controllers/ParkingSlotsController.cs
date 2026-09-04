using EventParking.Business.Exceptions;
using EventParking.Business.Interfaces;
using EventParking.Models.DTOs.Parking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventParking.Api.Controllers;

[ApiController]
[Route("api")]
public class ParkingSlotsController : ControllerBase
{
    private readonly IParkingService _parkingService;

    public ParkingSlotsController(
        IParkingService parkingService)
    {
        _parkingService = parkingService;
    }

    [AllowAnonymous]
    [HttpGet("events/{eventId:int}/parking-slots")]
    public async Task<IActionResult> GetByEvent(
        int eventId,
        [FromQuery] bool availableOnly = false)
    {
        try
        {
            var result =
                await _parkingService
                    .GetByEventIdAsync(
                        eventId,
                        availableOnly);

            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(
                new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet("parking-slots/{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var result =
                await _parkingService.GetByIdAsync(id);

            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(
                new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Administrator")]
    [HttpPost("events/{eventId:int}/parking-slots")]
    public async Task<IActionResult> Create(
        int eventId,
        [FromBody] ParkingSlotCreateDto dto)
    {
        try
        {
            var result =
                await _parkingService.CreateAsync(
                    eventId,
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
    }

    [Authorize(Roles = "Administrator")]
    [HttpPut("parking-slots/{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] ParkingSlotUpdateDto dto)
    {
        try
        {
            var result =
                await _parkingService.UpdateAsync(
                    id,
                    dto);

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
    }

    [Authorize(Roles = "Administrator")]
    [HttpDelete("parking-slots/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _parkingService.DeleteAsync(id);

            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(
                new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(
                new { message = ex.Message });
        }
    }
}
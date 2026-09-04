using System.Security.Claims;
using EventParking.Business.Exceptions;
using EventParking.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventParking.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController
    : ControllerBase
{
    private readonly INotificationService
        _notificationService;

    public NotificationsController(
        INotificationService notificationService)
    {
        _notificationService =
            notificationService;
    }

    [Authorize(Roles = "Administrator")]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result =
            await _notificationService
                .GetAllAsync();

        return Ok(result);
    }

    [Authorize(Roles = "Customer")]
    [HttpGet("my")]
    public async Task<IActionResult>
        GetMyNotifications(
            [FromQuery] bool unreadOnly = false)
    {
        var customerId =
            GetCurrentUserId();

        var result =
            await _notificationService
                .GetMyNotificationsAsync(
                    customerId,
                    unreadOnly);

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(
        int id)
    {
        try
        {
            var requesterId =
                GetCurrentUserId();

            var isAdministrator =
                User.IsInRole(
                    "Administrator");

            var result =
                await _notificationService
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

    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(
        int id)
    {
        try
        {
            var requesterId =
                GetCurrentUserId();

            var isAdministrator =
                User.IsInRole(
                    "Administrator");

            var result =
                await _notificationService
                    .MarkAsReadAsync(
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
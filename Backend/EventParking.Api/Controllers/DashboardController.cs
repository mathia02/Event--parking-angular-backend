using System.Security.Claims;
using EventParking.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventParking.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController
    : ControllerBase
{
    private readonly IDashboardService
        _dashboardService;

    public DashboardController(
        IDashboardService dashboardService)
    {
        _dashboardService =
            dashboardService;
    }

    [Authorize(Roles = "Administrator")]
    [HttpGet("admin")]
    public async Task<IActionResult>
        GetAdminDashboard()
    {
        var result =
            await _dashboardService
                .GetAdminDashboardAsync();

        return Ok(result);
    }

    [Authorize(Roles = "Customer")]
    [HttpGet("customer")]
    public async Task<IActionResult>
        GetCustomerDashboard()
    {
        var customerId =
            GetCurrentUserId();

        var result =
            await _dashboardService
                .GetCustomerDashboardAsync(
                    customerId);

        return Ok(result);
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
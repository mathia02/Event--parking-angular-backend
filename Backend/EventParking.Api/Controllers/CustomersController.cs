using System.Security.Claims;
using EventParking.Business.Exceptions;
using EventParking.Business.Interfaces;
using EventParking.Models.DTOs.Auth;
using EventParking.Models.DTOs.Customer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventParking.Api.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly IAuthService _authService;

    public CustomersController(
        ICustomerService customerService,
        IAuthService authService)
    {
        _customerService = customerService;
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequestDto dto)
    {
        try
        {
            var customer =
                await _authService.RegisterAsync(dto);

            return StatusCode(
                StatusCodes.Status201Created,
                customer);
        }
        catch (ConflictException ex)
        {
            return Conflict(new
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

    [Authorize(Roles = "Administrator")]
    [HttpGet]
    public async Task<IActionResult> GetCustomers(
        [FromQuery] string? search)
    {
        var customers =
            await _customerService
                .GetCustomersAsync(search);

        return Ok(customers);
    }

    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<IActionResult>
        GetCustomerById(int id)
    {
        if (!CanAccessCustomer(id))
        {
            return Forbid();
        }

        try
        {
            var customer =
                await _customerService
                    .GetByIdAsync(id);

            return Ok(customer);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCustomer(
        int id,
        [FromBody] CustomerUpdateDto dto)
    {
        if (!CanAccessCustomer(id))
        {
            return Forbid();
        }

        try
        {
            var customer =
                await _customerService
                    .UpdateProfileAsync(
                        id,
                        dto);

            return Ok(customer);
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

    [Authorize(Roles = "Administrator")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult>
        DeactivateCustomer(int id)
    {
        try
        {
            await _customerService
                .DeactivateAsync(id);

            return Ok(new
            {
                message =
                    "Customer account deactivated successfully."
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
    }

    [Authorize(Roles = "Administrator")]
    [HttpPost("{id:int}/reactivate")]
    public async Task<IActionResult>
        ReactivateCustomer(int id)
    {
        try
        {
            var customer =
                await _customerService
                    .ReactivateAsync(id);

            return Ok(new
            {
                message =
                    "Customer account reactivated successfully.",

                customer
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
    }

    private bool CanAccessCustomer(int customerId)
    {
        if (User.IsInRole("Administrator"))
        {
            return true;
        }

        var idValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        return int.TryParse(
                   idValue,
                   out var loggedCustomerId)
               &&
               loggedCustomerId == customerId;
    }
}
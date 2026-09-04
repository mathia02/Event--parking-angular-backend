using EventParking.Business.Exceptions;
using EventParking.Business.Services;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.DTOs.Customer;
using EventParking.Models.Entities;
using EventParking.Models.Enums;
using Moq;
using Xunit;

namespace EventParking.Tests.Services;

public class CustomerServiceTests
{
    private readonly Mock<ICustomerRepository> _customerRepositoryMock;
    private readonly CustomerService _customerService;

    public CustomerServiceTests()
    {
        _customerRepositoryMock = new Mock<ICustomerRepository>();

        _customerService = new CustomerService(
            _customerRepositoryMock.Object);
    }

    [Fact]
    public async Task GetCustomersAsync_ShouldReturnCustomers()
    {
        // Arrange
        var customers = new List<Customer>
        {
            new Customer
            {
                Id = 1,
                FullName = "Test Customer",
                Email = "test@example.com",
                Phone = "0771234567",
                Role = "Customer",
                Status = CustomerStatus.Active,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        _customerRepositoryMock
            .Setup(r => r.SearchAsync(null))
            .ReturnsAsync(customers);

        // Act
        var result = await _customerService
            .GetCustomersAsync(null);

        // Assert
        Assert.Single(result);

        Assert.Equal(
            "Test Customer",
            result[0].FullName);

        Assert.Equal(
            "test@example.com",
            result[0].Email);
    }

    [Fact]
    public async Task GetByIdAsync_WhenCustomerDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        _customerRepositoryMock
            .Setup(r => r.GetByIdWithBookingsAsync(99))
            .ReturnsAsync((Customer?)null);

        // Act + Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _customerService.GetByIdAsync(99));
    }

    [Fact]
    public async Task UpdateProfileAsync_ShouldUpdateCustomer()
    {
        // Arrange
        var customer = new Customer
        {
            Id = 1,
            FullName = "Old Name",
            Email = "test@example.com",
            Phone = "0711111111",
            Role = "Customer",
            Status = CustomerStatus.Active
        };

        var dto = new CustomerUpdateDto
        {
            FullName = "New Name",
            Phone = "0779999999"
        };

        _customerRepositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(customer);

        _customerRepositoryMock
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result =
            await _customerService.UpdateProfileAsync(
                1,
                dto);

        // Assert
        Assert.Equal(
            "New Name",
            result.FullName);

        Assert.Equal(
            "0779999999",
            result.Phone);

        _customerRepositoryMock.Verify(
            r => r.Update(customer),
            Times.Once);

        _customerRepositoryMock.Verify(
            r => r.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_WhenCustomerHasFutureBooking_ShouldReject()
    {
        // Arrange
        var customer = new Customer
        {
            Id = 1,
            FullName = "Test Customer",
            Email = "test@example.com",
            Phone = "0771234567",
            Status = CustomerStatus.Active
        };

        _customerRepositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(customer);

        _customerRepositoryMock
            .Setup(r =>
                r.HasActiveFutureBookingsAsync(
                    1,
                    It.IsAny<DateTime>()))
            .ReturnsAsync(true);

        // Act + Assert
        await Assert.ThrowsAsync<
            EventParking.Business.Exceptions.ValidationException>(
            () => _customerService.DeactivateAsync(1));

        Assert.Equal(
            CustomerStatus.Active,
            customer.Status);

        _customerRepositoryMock.Verify(
            r => r.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task DeactivateAsync_WhenNoFutureBooking_ShouldDeactivateCustomer()
    {
        // Arrange
        var customer = new Customer
        {
            Id = 1,
            FullName = "Test Customer",
            Email = "test@example.com",
            Phone = "0771234567",
            Status = CustomerStatus.Active
        };

        _customerRepositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(customer);

        _customerRepositoryMock
            .Setup(r =>
                r.HasActiveFutureBookingsAsync(
                    1,
                    It.IsAny<DateTime>()))
            .ReturnsAsync(false);

        _customerRepositoryMock
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        await _customerService.DeactivateAsync(1);

        // Assert
        Assert.Equal(
            CustomerStatus.Deactivated,
            customer.Status);

        _customerRepositoryMock.Verify(
            r => r.Update(customer),
            Times.Once);

        _customerRepositoryMock.Verify(
            r => r.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task ReactivateAsync_ShouldReactivateCustomer()
    {
        // Arrange
        var customer = new Customer
        {
            Id = 1,
            FullName = "Test Customer",
            Email = "test@example.com",
            Phone = "0771234567",
            Status = CustomerStatus.Deactivated
        };

        _customerRepositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(customer);

        _customerRepositoryMock
            .Setup(r => r.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result =
            await _customerService.ReactivateAsync(1);

        // Assert
        Assert.Equal(
            CustomerStatus.Active,
            result.Status);

        Assert.Equal(
            CustomerStatus.Active,
            customer.Status);

        _customerRepositoryMock.Verify(
            r => r.Update(customer),
            Times.Once);

        _customerRepositoryMock.Verify(
            r => r.SaveChangesAsync(),
            Times.Once);
    }
}
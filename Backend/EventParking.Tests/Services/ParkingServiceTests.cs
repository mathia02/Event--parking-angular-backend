using EventParking.Business.Exceptions;
using EventParking.Business.Services;
using EventParking.DataAccess.Interfaces;
using EventParking.Models.DTOs.Parking;
using EventParking.Models.Entities;
using EventParking.Models.Enums;
using Moq;
using Xunit;

using EventEntity = EventParking.Models.Entities.Event;

namespace EventParking.Tests.Services;

public class ParkingServiceTests
{
    private readonly Mock<IParkingRepository> _parkingRepositoryMock;
    private readonly Mock<IEventRepository> _eventRepositoryMock;
    private readonly ParkingService _parkingService;

    public ParkingServiceTests()
    {
        _parkingRepositoryMock =
            new Mock<IParkingRepository>();

        _eventRepositoryMock =
            new Mock<IEventRepository>();

        _parkingService =
            new ParkingService(
                _parkingRepositoryMock.Object,
                _eventRepositoryMock.Object);
    }

    [Fact]
    public async Task GetByEventIdAsync_ShouldReturnParkingSlots()
    {
        // Arrange
        var eventEntity = CreateEvent();

        var parkingSlots = new List<ParkingSlot>
        {
            new ParkingSlot
            {
                Id = 1,
                EventId = 1,
                SlotNumber = "A1",
                Zone = "Zone A",
                Fee = 500,
                Status = ParkingSlotStatus.Available
            },
            new ParkingSlot
            {
                Id = 2,
                EventId = 1,
                SlotNumber = "A2",
                Zone = "Zone A",
                Fee = 500,
                Status = ParkingSlotStatus.Unavailable
            }
        };

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(eventEntity);

        _parkingRepositoryMock
            .Setup(x => x.GetByEventIdAsync(1))
            .ReturnsAsync(parkingSlots);

        // Act
        var result =
            await _parkingService.GetByEventIdAsync(
                1,
                false);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("A1", result[0].SlotNumber);
        Assert.Equal("A2", result[1].SlotNumber);

        _parkingRepositoryMock.Verify(
            x => x.GetByEventIdAsync(1),
            Times.Once);
    }

    [Fact]
    public async Task GetByEventIdAsync_WhenAvailableOnlyTrue_ShouldReturnAvailableSlots()
    {
        // Arrange
        var eventEntity = CreateEvent();

        var parkingSlots = new List<ParkingSlot>
        {
            new ParkingSlot
            {
                Id = 1,
                EventId = 1,
                SlotNumber = "A1",
                Zone = "Zone A",
                Fee = 500,
                Status = ParkingSlotStatus.Available
            }
        };

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(eventEntity);

        _parkingRepositoryMock
            .Setup(x =>
                x.GetAvailableByEventIdAsync(1))
            .ReturnsAsync(parkingSlots);

        // Act
        var result =
            await _parkingService.GetByEventIdAsync(
                1,
                true);

        // Assert
        Assert.Single(result);
        Assert.Equal(
            ParkingSlotStatus.Available,
            result[0].Status);

        _parkingRepositoryMock.Verify(
            x => x.GetAvailableByEventIdAsync(1),
            Times.Once);

        _parkingRepositoryMock.Verify(
            x => x.GetByEventIdAsync(
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByEventIdAsync_WhenEventDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((EventEntity?)null);

        // Act + Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () =>
                _parkingService.GetByEventIdAsync(
                    999,
                    false));
    }

    [Fact]
    public async Task GetByIdAsync_WhenParkingSlotDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        _parkingRepositoryMock
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((ParkingSlot?)null);

        // Act + Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () =>
                _parkingService.GetByIdAsync(999));
    }

    [Fact]
    public async Task CreateAsync_WithValidData_ShouldCreateParkingSlot()
    {
        // Arrange
        var eventEntity = CreateEvent();

        var dto = new ParkingSlotCreateDto
        {
            SlotNumber = "A1",
            Zone = "Zone A",
            Fee = 500
        };

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(eventEntity);

        _parkingRepositoryMock
            .Setup(x =>
                x.SlotNumberExistsAsync(
                    1,
                    "A1",
                    null))
            .ReturnsAsync(false);

        _parkingRepositoryMock
            .Setup(x =>
                x.AddAsync(
                    It.IsAny<ParkingSlot>()))
            .Callback<ParkingSlot>(
                slot => slot.Id = 1)
            .Returns(Task.CompletedTask);

        _parkingRepositoryMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result =
            await _parkingService.CreateAsync(
                1,
                dto);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal(1, result.EventId);
        Assert.Equal("A1", result.SlotNumber);
        Assert.Equal("Zone A", result.Zone);
        Assert.Equal(500, result.Fee);

        Assert.Equal(
            ParkingSlotStatus.Available,
            result.Status);

        _parkingRepositoryMock.Verify(
            x =>
                x.AddAsync(
                    It.IsAny<ParkingSlot>()),
            Times.Once);

        _parkingRepositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenEventDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        var dto = new ParkingSlotCreateDto
        {
            SlotNumber = "A1",
            Zone = "Zone A",
            Fee = 500
        };

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((EventEntity?)null);

        // Act + Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () =>
                _parkingService.CreateAsync(
                    999,
                    dto));

        _parkingRepositoryMock.Verify(
            x =>
                x.AddAsync(
                    It.IsAny<ParkingSlot>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenSlotNumberAlreadyExists_ShouldThrowConflictException()
    {
        // Arrange
        var eventEntity = CreateEvent();

        var dto = new ParkingSlotCreateDto
        {
            SlotNumber = "A1",
            Zone = "Zone A",
            Fee = 500
        };

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(eventEntity);

        _parkingRepositoryMock
            .Setup(x =>
                x.SlotNumberExistsAsync(
                    1,
                    "A1",
                    null))
            .ReturnsAsync(true);

        // Act + Assert
        await Assert.ThrowsAsync<ConflictException>(
            () =>
                _parkingService.CreateAsync(
                    1,
                    dto));

        _parkingRepositoryMock.Verify(
            x =>
                x.AddAsync(
                    It.IsAny<ParkingSlot>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenFeeIsNegative_ShouldThrowValidationException()
    {
        // Arrange
        var eventEntity = CreateEvent();

        var dto = new ParkingSlotCreateDto
        {
            SlotNumber = "A1",
            Zone = "Zone A",
            Fee = -100
        };

        _eventRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(eventEntity);

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(
            () =>
                _parkingService.CreateAsync(
                    1,
                    dto));

        _parkingRepositoryMock.Verify(
            x =>
                x.AddAsync(
                    It.IsAny<ParkingSlot>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenParkingSlotHasActiveReservation_ShouldThrowConflictException()
    {
        // Arrange
        var parkingSlot = CreateParkingSlot();

        var dto = new ParkingSlotUpdateDto
        {
            SlotNumber = "A1",
            Zone = "Zone VIP",
            Fee = 750,
            Status = ParkingSlotStatus.Available
        };

        _parkingRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(parkingSlot);

        _parkingRepositoryMock
            .Setup(x =>
                x.HasActiveReservationAsync(1))
            .ReturnsAsync(true);

        // Act + Assert
        await Assert.ThrowsAsync<ConflictException>(
            () =>
                _parkingService.UpdateAsync(
                    1,
                    dto));

        _parkingRepositoryMock.Verify(
            x =>
                x.Update(
                    It.IsAny<ParkingSlot>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenSlotNumberAlreadyExists_ShouldThrowConflictException()
    {
        // Arrange
        var parkingSlot = CreateParkingSlot();

        var dto = new ParkingSlotUpdateDto
        {
            SlotNumber = "A2",
            Zone = "Zone A",
            Fee = 500,
            Status = ParkingSlotStatus.Available
        };

        _parkingRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(parkingSlot);

        _parkingRepositoryMock
            .Setup(x =>
                x.HasActiveReservationAsync(1))
            .ReturnsAsync(false);

        _parkingRepositoryMock
            .Setup(x =>
                x.SlotNumberExistsAsync(
                    1,
                    "A2",
                    1))
            .ReturnsAsync(true);

        // Act + Assert
        await Assert.ThrowsAsync<ConflictException>(
            () =>
                _parkingService.UpdateAsync(
                    1,
                    dto));

        _parkingRepositoryMock.Verify(
            x =>
                x.Update(
                    It.IsAny<ParkingSlot>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenAdminTriesToSetHeld_ShouldThrowValidationException()
    {
        // Arrange
        var parkingSlot = CreateParkingSlot();

        var dto = new ParkingSlotUpdateDto
        {
            SlotNumber = "A1",
            Zone = "Zone A",
            Fee = 500,
            Status = ParkingSlotStatus.Held
        };

        _parkingRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(parkingSlot);

        // Act + Assert
        await Assert.ThrowsAsync<ValidationException>(
            () =>
                _parkingService.UpdateAsync(
                    1,
                    dto));

        _parkingRepositoryMock.Verify(
            x =>
                x.Update(
                    It.IsAny<ParkingSlot>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WithValidData_ShouldUpdateParkingSlot()
    {
        // Arrange
        var parkingSlot = CreateParkingSlot();

        var dto = new ParkingSlotUpdateDto
        {
            SlotNumber = "A1",
            Zone = "VIP Zone",
            Fee = 750,
            Status = ParkingSlotStatus.Unavailable
        };

        _parkingRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(parkingSlot);

        _parkingRepositoryMock
            .Setup(x =>
                x.HasActiveReservationAsync(1))
            .ReturnsAsync(false);

        _parkingRepositoryMock
            .Setup(x =>
                x.SlotNumberExistsAsync(
                    1,
                    "A1",
                    1))
            .ReturnsAsync(false);

        _parkingRepositoryMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result =
            await _parkingService.UpdateAsync(
                1,
                dto);

        // Assert
        Assert.Equal("A1", result.SlotNumber);
        Assert.Equal("VIP Zone", result.Zone);
        Assert.Equal(750, result.Fee);

        Assert.Equal(
            ParkingSlotStatus.Unavailable,
            result.Status);

        _parkingRepositoryMock.Verify(
            x => x.Update(parkingSlot),
            Times.Once);

        _parkingRepositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenParkingSlotHasActiveReservation_ShouldThrowConflictException()
    {
        // Arrange
        var parkingSlot = CreateParkingSlot();

        _parkingRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(parkingSlot);

        _parkingRepositoryMock
            .Setup(x =>
                x.HasActiveReservationAsync(1))
            .ReturnsAsync(true);

        // Act + Assert
        await Assert.ThrowsAsync<ConflictException>(
            () =>
                _parkingService.DeleteAsync(1));

        _parkingRepositoryMock.Verify(
            x =>
                x.Delete(
                    It.IsAny<ParkingSlot>()),
            Times.Never);

        _parkingRepositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenNoActiveReservation_ShouldDeleteParkingSlot()
    {
        // Arrange
        var parkingSlot = CreateParkingSlot();

        _parkingRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(parkingSlot);

        _parkingRepositoryMock
            .Setup(x =>
                x.HasActiveReservationAsync(1))
            .ReturnsAsync(false);

        _parkingRepositoryMock
            .Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        await _parkingService.DeleteAsync(1);

        // Assert
        _parkingRepositoryMock.Verify(
            x => x.Delete(parkingSlot),
            Times.Once);

        _parkingRepositoryMock.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }

    private static EventEntity CreateEvent()
    {
        return new EventEntity
        {
            Id = 1,
            Name = "Tech Conference 2026",
            VenueId = 1,
            CategoryId = 1,
            StartDateTime =
                new DateTime(
                    2026,
                    9,
                    10,
                    10,
                    0,
                    0),
            EndDateTime =
                new DateTime(
                    2026,
                    9,
                    10,
                    12,
                    0,
                    0),
            TicketPrice = 2500,
            ParkingFee = 500,
            Capacity = 300
        };
    }

    private static ParkingSlot CreateParkingSlot()
    {
        return new ParkingSlot
        {
            Id = 1,
            EventId = 1,
            SlotNumber = "A1",
            Zone = "Zone A",
            Fee = 500,
            Status = ParkingSlotStatus.Available
        };
    }
}
using LibrarySystem.Business.DTOs.Requests;
using LibrarySystem.Business.Interfaces;
using LibrarySystem.Business.Messaging;
using LibrarySystem.Business.Services;
using LibrarySystem.DataAccess.Entities;
using LibrarySystem.DataAccess.UnitOfWork;
using LibrarySystem.Shared.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace LibrarySystem.Tests;

/// <summary>
/// Tests for borrowing-request creation, covering validation, ownership and
/// availability rules.
/// </summary>
[TestClass]
public sealed class BorrowingServiceCreateTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IBorrowRequestPublisher> _publisher = new();
    private readonly Mock<INotificationService> _notifications = new();
    private readonly BorrowingService _sut;

    public BorrowingServiceCreateTests()
    {
        _sut = new BorrowingService(
            _unitOfWork.Object,
            _publisher.Object,
            _notifications.Object,
            Mock.Of<ILogger<BorrowingService>>());
    }

    [TestMethod]
    public async Task Create_WithInvalidPeriod_ReturnsValidationError()
    {
        var request = new BorrowRequestCreateDto
        {
            BookId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            BorrowingPeriodDays = 45
        };

        var result = await _sut.CreateRequestAsync(request, request.UserId, UserRole.User);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("VALIDATION_ERROR");
        _publisher.Verify(p => p.PublishAsync(It.IsAny<BorrowRequestMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Create_ByDifferentUser_ReturnsForbidden()
    {
        var request = new BorrowRequestCreateDto
        {
            BookId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            BorrowingPeriodDays = 7
        };

        var result = await _sut.CreateRequestAsync(request, Guid.NewGuid(), UserRole.User);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("FORBIDDEN");
    }

    [TestMethod]
    public async Task Create_WithUnknownBook_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var request = new BorrowRequestCreateDto
        {
            BookId = Guid.NewGuid(),
            UserId = userId,
            BorrowingPeriodDays = 14
        };

        _unitOfWork.Setup(u => u.Users.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = userId, IsActive = true });
        _unitOfWork.Setup(u => u.Books.GetByIdAsync(request.BookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);

        var result = await _sut.CreateRequestAsync(request, userId, UserRole.User);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("NOT_FOUND");
    }

    [TestMethod]
    public async Task Create_WithInactiveUser_ReturnsBusinessRuleViolation()
    {
        var userId = Guid.NewGuid();
        var request = new BorrowRequestCreateDto
        {
            BookId = Guid.NewGuid(),
            UserId = userId,
            BorrowingPeriodDays = 14
        };

        _unitOfWork.Setup(u => u.Users.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = userId, IsActive = false });

        var result = await _sut.CreateRequestAsync(request, userId, UserRole.User);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("BUSINESS_RULE_VIOLATION");
    }

    [TestMethod]
    public async Task Create_WithNoAvailableCopies_ReturnsConflict()
    {
        var userId = Guid.NewGuid();
        var request = new BorrowRequestCreateDto
        {
            BookId = Guid.NewGuid(),
            UserId = userId,
            BorrowingPeriodDays = 14
        };

        _unitOfWork.Setup(u => u.Users.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = userId, IsActive = true });
        _unitOfWork.Setup(u => u.Books.GetByIdAsync(request.BookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Book { Id = request.BookId, Title = "Clean Code", IsAvailable = false });

        var result = await _sut.CreateRequestAsync(request, userId, UserRole.User);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("CONFLICT");
    }

    [TestMethod]
    public async Task Create_ValidRequest_PersistsPendingAndPublishes()
    {
        var userId = Guid.NewGuid();
        var request = new BorrowRequestCreateDto
        {
            BookId = Guid.NewGuid(),
            UserId = userId,
            BorrowingPeriodDays = 14
        };

        _unitOfWork.Setup(u => u.Users.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = userId, IsActive = true });
        _unitOfWork.Setup(u => u.Books.GetByIdAsync(request.BookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Book { Id = request.BookId, Title = "Clean Code", IsAvailable = true, AvailableCopies = 3 });
        _unitOfWork.Setup(u => u.BorrowingRequests.AddAsync(It.IsAny<BorrowingRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.CreateRequestAsync(request, userId, UserRole.User);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(nameof(BorrowingRequestStatus.Pending));
        result.Value.BookTitle.Should().Be("Clean Code");
        _publisher.Verify(
            p => p.PublishAsync(
                It.Is<BorrowRequestMessage>(m => m.RequestId == result.Value.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

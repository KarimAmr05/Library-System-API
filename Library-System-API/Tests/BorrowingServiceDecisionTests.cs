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
/// Tests for admin approval/denial: status transitions, concurrency-safe
/// availability updates and authorization of actor identifiers.
/// </summary>
[TestClass]
public sealed class BorrowingServiceDecisionTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IBorrowRequestPublisher> _publisher = new();
    private readonly Mock<INotificationService> _notifications = new();
    private readonly BorrowingService _sut;

    public BorrowingServiceDecisionTests()
    {
        _sut = new BorrowingService(
            _unitOfWork.Object,
            _publisher.Object,
            _notifications.Object,
            Mock.Of<ILogger<BorrowingService>>());
    }

    private void SetupTransaction()
    {
        // Execute the operation delegate immediately, simulating the Unit of Work.
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<(TransitionOutcome Outcome, BorrowingRequest? Request)>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<(TransitionOutcome, BorrowingRequest?)>>, CancellationToken>(
                (operation, _) => operation(CancellationToken.None));
    }

    [TestMethod]
    public async Task Approve_WithMismatchedAdmin_ReturnsForbidden()
    {
        var request = new BorrowRequestApproveDto { ApprovedByAdminId = Guid.NewGuid() };

        var result = await _sut.ApproveAsync(Guid.NewGuid(), request, Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("FORBIDDEN");
    }

    [TestMethod]
    public async Task Approve_NonPendingRequest_ReturnsConflict()
    {
        SetupTransaction();
        var requestId = Guid.NewGuid();
        var tracked = TrackedRequest(requestId, BorrowingRequestStatus.Denied);
        _unitOfWork.Setup(u => u.BorrowingRequests.GetByIdWithBookTrackedAsync(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tracked);

        var adminId = Guid.NewGuid();
        var result = await _sut.ApproveAsync(
            requestId,
            new BorrowRequestApproveDto { ApprovedByAdminId = adminId },
            adminId);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("CONFLICT");
    }

    [TestMethod]
    public async Task Approve_UnknownRequest_ReturnsNotFound()
    {
        SetupTransaction();
        var requestId = Guid.NewGuid();
        _unitOfWork.Setup(u => u.BorrowingRequests.GetByIdWithBookTrackedAsync(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BorrowingRequest?)null);

        var adminId = Guid.NewGuid();
        var result = await _sut.ApproveAsync(
            requestId,
            new BorrowRequestApproveDto { ApprovedByAdminId = adminId },
            adminId);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("NOT_FOUND");
    }

    [TestMethod]
    public async Task Approve_WithNoAvailableCopies_ReturnsConflictWithoutDecrementing()
    {
        SetupTransaction();
        var requestId = Guid.NewGuid();
        var tracked = TrackedRequest(requestId, BorrowingRequestStatus.Pending);
        tracked.Book.AvailableCopies = 0;
        tracked.Book.IsAvailable = false;

        _unitOfWork.Setup(u => u.BorrowingRequests.GetByIdWithBookTrackedAsync(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tracked);

        var adminId = Guid.NewGuid();
        var result = await _sut.ApproveAsync(
            requestId,
            new BorrowRequestApproveDto { ApprovedByAdminId = adminId },
            adminId);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("CONFLICT");
        tracked.Book.AvailableCopies.Should().Be(0);
        tracked.Status.Should().Be(BorrowingRequestStatus.Pending);
    }

    [TestMethod]
    public async Task Approve_PendingRequest_DecrementsCopiesAndNotifiesUser()
    {
        SetupTransaction();
        var requestId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tracked = TrackedRequest(requestId, BorrowingRequestStatus.Pending, userId);
        tracked.Book.AvailableCopies = 2;

        _unitOfWork.Setup(u => u.BorrowingRequests.GetByIdWithBookTrackedAsync(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tracked);

        var adminId = Guid.NewGuid();
        var result = await _sut.ApproveAsync(
            requestId,
            new BorrowRequestApproveDto { ApprovedByAdminId = adminId },
            adminId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(nameof(BorrowingRequestStatus.Approved));
        tracked.Book.AvailableCopies.Should().Be(1);
        tracked.ReviewedBy.Should().Be(adminId);
        tracked.ReviewedAt.Should().NotBeNull();

        _notifications.Verify(n =>
            n.CreateAndDispatchAsync(
                It.Is<Notification>(x =>
                    x.RecipientUserId == userId &&
                    x.Type == NotificationType.RequestApproved),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task Deny_PendingRequest_SetsDeniedWithReasonAndNotifiesUser()
    {
        SetupTransaction();
        var requestId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tracked = TrackedRequest(requestId, BorrowingRequestStatus.Pending, userId);

        _unitOfWork.Setup(u => u.BorrowingRequests.GetByIdWithBookTrackedAsync(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tracked);

        var adminId = Guid.NewGuid();
        const string reason = "Requested period unavailable";
        var result = await _sut.DenyAsync(
            requestId,
            new BorrowRequestDenyDto { DeniedByAdminId = adminId, Reason = reason },
            adminId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(nameof(BorrowingRequestStatus.Denied));
        result.Value.DenyReason.Should().Be(reason);
        tracked.Book.AvailableCopies.Should().Be(5); // unchanged by denial

        _notifications.Verify(n =>
            n.CreateAndDispatchAsync(
                It.Is<Notification>(x =>
                    x.RecipientUserId == userId &&
                    x.Type == NotificationType.RequestDenied),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static BorrowingRequest TrackedRequest(
        Guid id,
        BorrowingRequestStatus status,
        Guid? userId = null)
    {
        var book = new Book
        {
            Id = Guid.NewGuid(),
            Title = "Domain-Driven Design",
            IsAvailable = true,
            TotalCopies = 5,
            AvailableCopies = 5
        };

        return new BorrowingRequest
        {
            Id = id,
            BookId = book.Id,
            BookTitle = book.Title,
            Book = book,
            UserId = userId ?? Guid.NewGuid(),
            Status = status,
            BorrowingPeriodDays = 14,
            RequestedAt = DateTime.UtcNow
        };
    }
}

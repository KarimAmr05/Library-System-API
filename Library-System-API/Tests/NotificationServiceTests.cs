using LibrarySystem.Business.Notifications;
using LibrarySystem.Business.Services;
using LibrarySystem.DataAccess.Entities;
using LibrarySystem.DataAccess.Interfaces;
using LibrarySystem.DataAccess.UnitOfWork;
using LibrarySystem.Shared.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;

namespace LibrarySystem.Tests;

/// <summary>
/// Tests for notification inbox security: users must never access another
/// user's private notifications.
/// </summary>
[TestClass]
public sealed class NotificationServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<INotificationRepository> _notificationRepo = new();
    private readonly NotificationService _sut;

    public NotificationServiceTests()
    {
        _unitOfWork.SetupGet(u => u.Notifications).Returns(_notificationRepo.Object);
        _sut = new NotificationService(
            _unitOfWork.Object,
            new SignalRNotificationDispatcher(
                Mock.Of<IHubContext<LibrarySystem.Business.Hubs.NotificationsHub>>(),
                Mock.Of<ILogger<SignalRNotificationDispatcher>>()),
            Mock.Of<ILogger<NotificationService>>());
    }

    [TestMethod]
    public async Task MarkAsRead_ForeignNotificationAsUser_ReturnsNotFound()
    {
        var ownerId = Guid.NewGuid();
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            RecipientUserId = ownerId,
            RecipientRole = UserRole.User,
            Type = NotificationType.RequestApproved,
            Title = "t",
            Message = "m"
        };

        _notificationRepo
            .Setup(r => r.GetByIdTrackedAsync(notification.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        var result = await _sut.MarkAsReadAsync(notification.Id, Guid.NewGuid(), UserRole.User);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("NOT_FOUND");
        notification.IsRead.Should().BeFalse();
    }

    [TestMethod]
    public async Task MarkAsRead_OwnNotification_MarksRead()
    {
        var userId = Guid.NewGuid();
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            RecipientUserId = userId,
            RecipientRole = UserRole.User,
            Type = NotificationType.BorrowRequestCreated,
            Title = "t",
            Message = "m"
        };

        _notificationRepo
            .Setup(r => r.GetByIdTrackedAsync(notification.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.MarkAsReadAsync(notification.Id, userId, UserRole.User);

        result.IsSuccess.Should().BeTrue();
        notification.IsRead.Should().BeTrue();
    }

    [TestMethod]
    public async Task SendDueReminder_DoesNotDuplicateForSameRequestAndRecipient()
    {
        var requestId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _notificationRepo.Setup(r => r.ExistsForRequestAsync(
                userId, requestId, NotificationType.BorrowDueReminder, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _sut.SendDueReminderAsync(userId, UserRole.User, requestId, "title", "message");

        _notificationRepo.Verify(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

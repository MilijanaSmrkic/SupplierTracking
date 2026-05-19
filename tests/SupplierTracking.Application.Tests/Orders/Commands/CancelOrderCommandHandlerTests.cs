using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SupplierTracking.Application.Abstractions;
using SupplierTracking.Application.Abstractions.Repositories;
using SupplierTracking.Application.Orders.Commands.CancelOrder;
using SupplierTracking.Application.Orders.Events;
using SupplierTracking.Domain.Entities;

namespace SupplierTracking.Application.Tests.Orders.Commands;

public class CancelOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository>    _orderRepo   = new();
    private readonly Mock<IUnitOfWork>         _uow         = new();
    private readonly Mock<IPublisher>          _publisher   = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly CancelOrderCommandHandler _handler;

    public CancelOrderCommandHandlerTests()
    {
        _currentUser.Setup(s => s.UserId).Returns(1);
        _currentUser.Setup(s => s.Role).Returns(UserRoles.Manager);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _publisher
            .Setup(p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new CancelOrderCommandHandler(
            _orderRepo.Object, _uow.Object, _publisher.Object,
            _currentUser.Object,
            NullLogger<CancelOrderCommandHandler>.Instance);
    }

    private Order MakeOrderWithDetails()
    {
        var order = Order.Create(Guid.NewGuid(), 1);

        // Attach supplier navigation via reflection so handler can read SupplierName
        var supplierProp = typeof(Order).GetProperty("Supplier",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!;
        supplierProp.SetValue(order, Supplier.Create("ACME", "acme@test.com"));

        return order;
    }

    [Fact]
    public async Task Handle_ShouldCancelOrderAndPublishEvent()
    {
        var order = MakeOrderWithDetails();
        _orderRepo.Setup(r => r.GetByIdWithDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        await _handler.Handle(new CancelOrderCommand(order.Id, "Test reason"), CancellationToken.None);

        Assert.Equal(OrderStatuses.Cancelled, order.Status);
        _publisher.Verify(p => p.Publish(
            It.Is<OrderStatusChangedEvent>(e =>
                e.OrderId == order.Id && e.ToStatus == OrderStatuses.Cancelled),
            It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenOrderNotFound()
    {
        _orderRepo.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new CancelOrderCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenViewerTriesToCancelOtherUsersOrder()
    {
        _currentUser.Setup(s => s.UserId).Returns(99);
        _currentUser.Setup(s => s.Role).Returns(UserRoles.Viewer);

        // Order created by user 1
        var order = Order.Create(Guid.NewGuid(), createdByUserId: 1);
        _orderRepo.Setup(r => r.GetByIdWithDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldAllow_WhenManagerCancelsAnyOrder()
    {
        _currentUser.Setup(s => s.UserId).Returns(99);
        _currentUser.Setup(s => s.Role).Returns(UserRoles.Manager);
        _currentUser.Setup(s => s.IsManager).Returns(true);

        var order = MakeOrderWithDetails();
        _orderRepo.Setup(r => r.GetByIdWithDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        await _handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        Assert.Equal(OrderStatuses.Cancelled, order.Status);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenOrderAlreadyDelivered()
    {
        var order = MakeOrderWithDetails();
        order.AddItem(Guid.NewGuid(), 1, 5m);
        order.Send(1);
        order.Confirm(1);
        order.MarkInTransit();
        order.MarkDelivered(1);

        _orderRepo.Setup(r => r.GetByIdWithDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None));
    }
}

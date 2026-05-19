using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SupplierTracking.Application.Abstractions;
using SupplierTracking.Application.Abstractions.Repositories;
using SupplierTracking.Application.Orders.Commands.MarkDelivered;
using SupplierTracking.Application.Orders.Events;
using SupplierTracking.Domain.Entities;

namespace SupplierTracking.Application.Tests.Orders.Commands;

public class MarkDeliveredCommandHandlerTests
{
    private readonly Mock<IOrderRepository>    _orderRepo   = new();
    private readonly Mock<IUnitOfWork>         _uow         = new();
    private readonly Mock<IPublisher>          _publisher   = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly MarkDeliveredCommandHandler _handler;

    public MarkDeliveredCommandHandlerTests()
    {
        _currentUser.Setup(s => s.UserId).Returns(1);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _publisher
            .Setup(p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new MarkDeliveredCommandHandler(
            _orderRepo.Object, _uow.Object, _publisher.Object,
            _currentUser.Object,
            NullLogger<MarkDeliveredCommandHandler>.Instance);
    }

    private Order MakeInTransitOrder()
    {
        var order = Order.Create(Guid.NewGuid(), 1);
        order.AddItem(Guid.NewGuid(), 1, 75m);
        order.Send(1);
        order.Confirm(1);
        order.MarkInTransit("TRACK-001");
        return order;
    }

    [Fact]
    public async Task Handle_ShouldTransitionInTransitToDelivered()
    {
        var order = MakeInTransitOrder();
        _orderRepo.Setup(r => r.GetByIdWithDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        await _handler.Handle(new MarkDeliveredCommand(order.Id), CancellationToken.None);

        Assert.Equal(OrderStatuses.Delivered, order.Status);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPublishStatusChangedEvent()
    {
        var order = MakeInTransitOrder();
        _orderRepo.Setup(r => r.GetByIdWithDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        await _handler.Handle(new MarkDeliveredCommand(order.Id), CancellationToken.None);

        _publisher.Verify(p => p.Publish(
            It.Is<OrderStatusChangedEvent>(e =>
                e.OrderId    == order.Id &&
                e.FromStatus == OrderStatuses.InTransit &&
                e.ToStatus   == OrderStatuses.Delivered),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenOrderNotFound()
    {
        _orderRepo.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new MarkDeliveredCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenOrderNotInTransit()
    {
        var order = Order.Create(Guid.NewGuid(), 1);
        order.AddItem(Guid.NewGuid(), 1, 5m);
        order.Send(1);
        order.Confirm(1); // Confirmed, not InTransit

        _orderRepo.Setup(r => r.GetByIdWithDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new MarkDeliveredCommand(order.Id), CancellationToken.None));
    }
}

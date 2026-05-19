using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SupplierTracking.Application.Abstractions;
using SupplierTracking.Application.Abstractions.Repositories;
using SupplierTracking.Application.Orders.Commands.MarkInTransit;
using SupplierTracking.Application.Orders.Events;
using SupplierTracking.Domain.Entities;

namespace SupplierTracking.Application.Tests.Orders.Commands;

public class MarkInTransitCommandHandlerTests
{
    private readonly Mock<IOrderRepository>    _orderRepo   = new();
    private readonly Mock<IUnitOfWork>         _uow         = new();
    private readonly Mock<IPublisher>          _publisher   = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly MarkInTransitCommandHandler _handler;

    public MarkInTransitCommandHandlerTests()
    {
        _currentUser.Setup(s => s.UserId).Returns(1);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _publisher
            .Setup(p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new MarkInTransitCommandHandler(
            _orderRepo.Object, _uow.Object, _publisher.Object,
            _currentUser.Object,
            NullLogger<MarkInTransitCommandHandler>.Instance);
    }

    private Order MakeConfirmedOrder()
    {
        var order = Order.Create(Guid.NewGuid(), 1);
        order.AddItem(Guid.NewGuid(), 1, 50m);
        order.Send(1);
        order.Confirm(1);
        return order;
    }

    [Fact]
    public async Task Handle_ShouldTransitionConfirmedToInTransit()
    {
        var order = MakeConfirmedOrder();
        _orderRepo.Setup(r => r.GetByIdWithDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        await _handler.Handle(new MarkInTransitCommand(order.Id, "DHL-123"), CancellationToken.None);

        Assert.Equal(OrderStatuses.InTransit, order.Status);
        Assert.Equal("DHL-123", order.TrackingCode);
    }

    [Fact]
    public async Task Handle_ShouldPublishStatusChangedEvent()
    {
        var order = MakeConfirmedOrder();
        _orderRepo.Setup(r => r.GetByIdWithDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        await _handler.Handle(new MarkInTransitCommand(order.Id, "FDX-456"), CancellationToken.None);

        _publisher.Verify(p => p.Publish(
            It.Is<OrderStatusChangedEvent>(e =>
                e.OrderId    == order.Id &&
                e.FromStatus == OrderStatuses.Confirmed &&
                e.ToStatus   == OrderStatuses.InTransit),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenOrderNotFound()
    {
        _orderRepo.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new MarkInTransitCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenOrderNotConfirmed()
    {
        var order = Order.Create(Guid.NewGuid(), 1);
        order.AddItem(Guid.NewGuid(), 1, 5m);
        order.Send(1); // Sent, not Confirmed

        _orderRepo.Setup(r => r.GetByIdWithDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new MarkInTransitCommand(order.Id), CancellationToken.None));
    }
}

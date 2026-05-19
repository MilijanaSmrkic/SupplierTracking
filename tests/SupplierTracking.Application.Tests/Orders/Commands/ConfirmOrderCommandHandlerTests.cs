using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SupplierTracking.Application.Abstractions;
using SupplierTracking.Application.Abstractions.Repositories;
using SupplierTracking.Application.Orders.Commands.ConfirmOrder;
using SupplierTracking.Application.Orders.Events;
using SupplierTracking.Domain.Entities;

namespace SupplierTracking.Application.Tests.Orders.Commands;

public class ConfirmOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository>    _orderRepo   = new();
    private readonly Mock<IUnitOfWork>         _uow         = new();
    private readonly Mock<IPublisher>          _publisher   = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly ConfirmOrderCommandHandler _handler;

    public ConfirmOrderCommandHandlerTests()
    {
        _currentUser.Setup(s => s.UserId).Returns(1);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _publisher
            .Setup(p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new ConfirmOrderCommandHandler(
            _orderRepo.Object, _uow.Object, _publisher.Object,
            _currentUser.Object,
            NullLogger<ConfirmOrderCommandHandler>.Instance);
    }

    private Order MakeSentOrder()
    {
        var order = Order.Create(Guid.NewGuid(), 1);
        order.AddItem(Guid.NewGuid(), 2, 10m);
        order.Send(1);
        return order;
    }

    [Fact]
    public async Task Handle_ShouldTransitionSentToConfirmed()
    {
        var order = MakeSentOrder();
        _orderRepo.Setup(r => r.GetByIdWithDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        await _handler.Handle(new ConfirmOrderCommand(order.Id), CancellationToken.None);

        Assert.Equal(OrderStatuses.Confirmed, order.Status);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPublishStatusChangedEvent()
    {
        var order = MakeSentOrder();
        _orderRepo.Setup(r => r.GetByIdWithDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        await _handler.Handle(new ConfirmOrderCommand(order.Id, "All items verified"), CancellationToken.None);

        _publisher.Verify(p => p.Publish(
            It.Is<OrderStatusChangedEvent>(e =>
                e.OrderId     == order.Id &&
                e.FromStatus  == OrderStatuses.Sent &&
                e.ToStatus    == OrderStatuses.Confirmed),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenOrderNotFound()
    {
        _orderRepo.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new ConfirmOrderCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenOrderNotInSentStatus()
    {
        var order = Order.Create(Guid.NewGuid(), 1); // still Draft
        _orderRepo.Setup(r => r.GetByIdWithDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new ConfirmOrderCommand(order.Id), CancellationToken.None));
    }
}

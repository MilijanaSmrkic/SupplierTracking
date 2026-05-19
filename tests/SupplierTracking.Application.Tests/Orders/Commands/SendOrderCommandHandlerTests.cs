using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SupplierTracking.Application.Abstractions;
using SupplierTracking.Application.Abstractions.Repositories;
using SupplierTracking.Application.Orders.Commands.SendOrder;
using SupplierTracking.Application.Orders.Events;
using SupplierTracking.Domain.Entities;

namespace SupplierTracking.Application.Tests.Orders.Commands;

public class SendOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository>    _orderRepo   = new();
    private readonly Mock<IUnitOfWork>         _uow         = new();
    private readonly Mock<IPublisher>          _publisher   = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly SendOrderCommandHandler   _handler;

    public SendOrderCommandHandlerTests()
    {
        _currentUser.Setup(s => s.UserId).Returns(1);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _publisher
            .Setup(p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new SendOrderCommandHandler(
            _orderRepo.Object, _uow.Object, _publisher.Object,
            _currentUser.Object,
            NullLogger<SendOrderCommandHandler>.Instance);
    }

    private Order MakeOrderWithItem()
    {
        var order = Order.Create(Guid.NewGuid(), 1);
        order.AddItem(Guid.NewGuid(), 2, 15m);

        var supplierProp = typeof(Order).GetProperty("Supplier",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!;
        supplierProp.SetValue(order, Supplier.Create("ACME", "acme@test.com"));

        return order;
    }

    [Fact]
    public async Task Handle_ShouldTransitionDraftToSent()
    {
        var order = MakeOrderWithItem();
        _orderRepo.Setup(r => r.GetByIdWithDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        await _handler.Handle(new SendOrderCommand(order.Id), CancellationToken.None);

        Assert.Equal(OrderStatuses.Sent, order.Status);
        _publisher.Verify(p => p.Publish(
            It.Is<OrderStatusChangedEvent>(e =>
                e.FromStatus == OrderStatuses.Draft &&
                e.ToStatus   == OrderStatuses.Sent),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenOrderHasNoItems()
    {
        var order = Order.Create(Guid.NewGuid(), 1); // no items
        _orderRepo.Setup(r => r.GetByIdWithDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new SendOrderCommand(order.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenOrderNotFound()
    {
        _orderRepo.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(new SendOrderCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenOrderAlreadySent()
    {
        var order = MakeOrderWithItem();
        order.Send(1);

        _orderRepo.Setup(r => r.GetByIdWithDetailsAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new SendOrderCommand(order.Id), CancellationToken.None));
    }
}

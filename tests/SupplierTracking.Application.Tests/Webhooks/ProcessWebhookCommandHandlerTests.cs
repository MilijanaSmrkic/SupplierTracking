using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SupplierTracking.Application.Abstractions;
using SupplierTracking.Application.Abstractions.Repositories;
using SupplierTracking.Application.Orders.Commands.ConfirmOrder;
using SupplierTracking.Application.Orders.Commands.MarkDelivered;
using SupplierTracking.Application.Orders.Commands.MarkInTransit;
using SupplierTracking.Application.Webhooks;
using SupplierTracking.Domain.Entities;

namespace SupplierTracking.Application.Tests.Webhooks;

public class ProcessWebhookCommandHandlerTests
{
    private readonly Mock<IOrderRepository>    _orderRepo    = new();
    private readonly Mock<ISupplierRepository> _supplierRepo = new();
    private readonly Mock<IUnitOfWork>         _uow          = new();
    private readonly Mock<IPublisher>          _publisher    = new();
    private readonly ProcessWebhookCommandHandler _handler;

    private readonly Guid     _supplierId = Guid.NewGuid();
    private readonly Supplier _supplier;

    public ProcessWebhookCommandHandlerTests()
    {
        _supplier = Supplier.Create("ACME Corp", "acme@test.com");

        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _publisher
            .Setup(p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new ProcessWebhookCommandHandler(
            _orderRepo.Object, _supplierRepo.Object, _uow.Object, _publisher.Object,
            NullLogger<ProcessWebhookCommandHandler>.Instance);
    }

    private Order MakeSentOrder()
    {
        var order = Order.Create(_supplierId, 1);
        order.AddItem(Guid.NewGuid(), 1, 10m);
        order.Send(1);

        // Attach supplier navigation via reflection
        var supplierProp = typeof(Order).GetProperty("Supplier",
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public   |
            System.Reflection.BindingFlags.NonPublic)!;
        supplierProp.SetValue(order, _supplier);

        return order;
    }

    private void SetupHappyPath(Order order)
    {
        _supplierRepo
            .Setup(r => r.GetByIdAsync(_supplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_supplier);
        _orderRepo
            .Setup(r => r.GetByOrderNumberAsync(order.OrderNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
    }

    [Fact]
    public async Task Handle_ConfirmedEvent_ShouldPublishConfirmOrderCommand()
    {
        var order = MakeSentOrder();
        SetupHappyPath(order);

        await _handler.Handle(
            new ProcessWebhookCommand(_supplierId,
                new WebhookPayload(order.OrderNumber, WebhookEvents.Confirmed, null, "OK")),
            CancellationToken.None);

        _publisher.Verify(p => p.Publish(
            It.Is<ConfirmOrderCommand>(c => c.OrderId == order.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShippedEvent_ShouldPublishMarkInTransitCommand()
    {
        var order = MakeSentOrder();
        order.Confirm(1);
        SetupHappyPath(order);

        await _handler.Handle(
            new ProcessWebhookCommand(_supplierId,
                new WebhookPayload(order.OrderNumber, WebhookEvents.Shipped, "DHL-999", null)),
            CancellationToken.None);

        _publisher.Verify(p => p.Publish(
            It.Is<MarkInTransitCommand>(c =>
                c.OrderId == order.Id && c.TrackingCode == "DHL-999"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DeliveredEvent_ShouldPublishMarkDeliveredCommand()
    {
        var order = MakeSentOrder();
        order.Confirm(1);
        order.MarkInTransit();
        SetupHappyPath(order);

        await _handler.Handle(
            new ProcessWebhookCommand(_supplierId,
                new WebhookPayload(order.OrderNumber, WebhookEvents.Delivered, null, null)),
            CancellationToken.None);

        _publisher.Verify(p => p.Publish(
            It.Is<MarkDeliveredCommand>(c => c.OrderId == order.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenSupplierNotFound()
    {
        _supplierRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Supplier?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(
                new ProcessWebhookCommand(_supplierId,
                    new WebhookPayload("ORD-001", WebhookEvents.Confirmed, null, null)),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenOrderNotFound()
    {
        _supplierRepo
            .Setup(r => r.GetByIdAsync(_supplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_supplier);
        _orderRepo
            .Setup(r => r.GetByOrderNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(
                new ProcessWebhookCommand(_supplierId,
                    new WebhookPayload("ORD-UNKNOWN", WebhookEvents.Confirmed, null, null)),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenOrderBelongsToDifferentSupplier()
    {
        // Order belongs to a different supplier
        var otherSupplierId = Guid.NewGuid();
        var order = Order.Create(otherSupplierId, 1);
        order.AddItem(Guid.NewGuid(), 1, 10m);
        order.Send(1);

        _supplierRepo
            .Setup(r => r.GetByIdAsync(_supplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_supplier);
        _orderRepo
            .Setup(r => r.GetByOrderNumberAsync(order.OrderNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(
                new ProcessWebhookCommand(_supplierId,
                    new WebhookPayload(order.OrderNumber, WebhookEvents.Confirmed, null, null)),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenEventIsUnknown()
    {
        var order = MakeSentOrder();
        SetupHappyPath(order);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new ProcessWebhookCommand(_supplierId,
                    new WebhookPayload(order.OrderNumber, "dispatched", null, null)),
                CancellationToken.None));
    }
}

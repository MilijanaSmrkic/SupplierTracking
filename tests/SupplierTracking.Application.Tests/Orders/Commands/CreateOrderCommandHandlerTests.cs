using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SupplierTracking.Application.Abstractions;
using SupplierTracking.Application.Abstractions.Repositories;
using SupplierTracking.Application.Orders.Commands.CreateOrder;
using SupplierTracking.Domain.Entities;

namespace SupplierTracking.Application.Tests.Orders.Commands;

public class CreateOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository>    _orderRepo    = new();
    private readonly Mock<ISupplierRepository> _supplierRepo = new();
    private readonly Mock<IProductRepository>  _productRepo  = new();
    private readonly Mock<IUnitOfWork>         _uow          = new();
    private readonly Mock<ICurrentUserService> _currentUser  = new();
    private readonly CreateOrderCommandHandler _handler;

    private readonly Supplier _supplier;
    private readonly Product  _product;

    public CreateOrderCommandHandlerTests()
    {
        _supplier = Supplier.Create("ACME", "acme@test.com");
        _product  = Product.Create("Widget", "WDG-001", 25.00m, _supplier.Id);

        _currentUser.Setup(s => s.UserId).Returns(1);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new CreateOrderCommandHandler(
            _orderRepo.Object, _supplierRepo.Object, _productRepo.Object,
            _uow.Object, _currentUser.Object,
            NullLogger<CreateOrderCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_ShouldCreateOrderAndReturnId()
    {
        _supplierRepo.Setup(r => r.GetByIdAsync(_supplier.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_supplier);
        _productRepo.Setup(r => r.GetByIdAsync(_product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_product);

        Order? added = null;
        _orderRepo.Setup(r => r.Add(It.IsAny<Order>()))
            .Callback<Order>(o => added = o);

        var command = new CreateOrderCommand(
            _supplier.Id,
            [new OrderItemRequest(_product.Id, 3)]);

        var id = await _handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        Assert.NotNull(added);
        Assert.Equal(OrderStatuses.Draft, added.Status);
        Assert.Single(added.Items);
        Assert.Equal(75.00m, added.TotalAmount);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenSupplierNotFound()
    {
        _supplierRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Supplier?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(
                new CreateOrderCommand(Guid.NewGuid(), [new OrderItemRequest(Guid.NewGuid(), 1)]),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenProductNotFound()
    {
        _supplierRepo.Setup(r => r.GetByIdAsync(_supplier.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_supplier);
        _productRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _handler.Handle(
                new CreateOrderCommand(_supplier.Id, [new OrderItemRequest(Guid.NewGuid(), 1)]),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenProductBelongsToDifferentSupplier()
    {
        var otherSupplier = Supplier.Create("Other", "other@test.com");
        var foreignProduct = Product.Create("Widget", "WDG-XYZ", 10m, otherSupplier.Id);

        _supplierRepo.Setup(r => r.GetByIdAsync(_supplier.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_supplier);
        _productRepo.Setup(r => r.GetByIdAsync(foreignProduct.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(foreignProduct);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new CreateOrderCommand(_supplier.Id, [new OrderItemRequest(foreignProduct.Id, 1)]),
                CancellationToken.None));
    }
}

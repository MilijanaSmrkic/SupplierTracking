using SupplierTracking.Domain.Entities;

namespace SupplierTracking.Application.Tests.Domain;

public class OrderTests
{
    private static readonly Guid SupplierId = Guid.NewGuid();
    private const int UserId = 1;

    [Fact]
    public void Create_ShouldInitializeWithDraftStatusAndOrderNumber()
    {
        var order = Order.Create(SupplierId, UserId);

        Assert.Equal(OrderStatuses.Draft, order.Status);
        Assert.StartsWith("ORD-", order.OrderNumber);
        Assert.Equal(SupplierId, order.SupplierId);
        Assert.Equal(UserId, order.CreatedByUserId);
        Assert.Single(order.StatusLogs); // initial Draft log
        Assert.Empty(order.Items);
    }

    [Fact]
    public void AddItem_ShouldAddItemAndUpdateTotal()
    {
        var order = Order.Create(SupplierId, UserId);
        var productId = Guid.NewGuid();

        order.AddItem(productId, 3, 10.00m);

        Assert.Single(order.Items);
        Assert.Equal(30.00m, order.TotalAmount);
    }

    [Fact]
    public void AddItem_ShouldThrow_WhenOrderIsNotDraft()
    {
        var order = Order.Create(SupplierId, UserId);
        order.AddItem(Guid.NewGuid(), 1, 5m);
        order.Send(UserId);

        Assert.Throws<InvalidOperationException>(() =>
            order.AddItem(Guid.NewGuid(), 1, 5m));
    }

    [Fact]
    public void Send_ShouldTransitionDraftToSent()
    {
        var order = Order.Create(SupplierId, UserId);
        order.AddItem(Guid.NewGuid(), 1, 5m);

        order.Send(UserId);

        Assert.Equal(OrderStatuses.Sent, order.Status);
        Assert.Equal(2, order.StatusLogs.Count); // Draft + Sent
    }

    [Fact]
    public void Send_ShouldThrow_WhenNotDraft()
    {
        var order = Order.Create(SupplierId, UserId);
        order.AddItem(Guid.NewGuid(), 1, 5m);
        order.Send(UserId);

        Assert.Throws<InvalidOperationException>(() => order.Send(UserId));
    }

    [Fact]
    public void Confirm_ShouldTransitionSentToConfirmed()
    {
        var order = Order.Create(SupplierId, UserId);
        order.AddItem(Guid.NewGuid(), 1, 5m);
        order.Send(UserId);

        order.Confirm(UserId, "Looks good");

        Assert.Equal(OrderStatuses.Confirmed, order.Status);
        var lastLog = order.StatusLogs.OrderByDescending(l => l.ChangedAt).First();
        Assert.Equal("Looks good", lastLog.Notes);
    }

    [Fact]
    public void MarkInTransit_ShouldSetTrackingCode()
    {
        var order = Order.Create(SupplierId, UserId);
        order.AddItem(Guid.NewGuid(), 1, 5m);
        order.Send(UserId);
        order.Confirm(UserId);

        order.MarkInTransit("DHL-999");

        Assert.Equal(OrderStatuses.InTransit, order.Status);
        Assert.Equal("DHL-999", order.TrackingCode);
    }

    [Fact]
    public void MarkInTransit_ShouldThrow_WhenNotConfirmed()
    {
        var order = Order.Create(SupplierId, UserId);
        order.AddItem(Guid.NewGuid(), 1, 5m);
        order.Send(UserId);

        Assert.Throws<InvalidOperationException>(() => order.MarkInTransit());
    }

    [Fact]
    public void MarkDelivered_ShouldTransitionInTransitToDelivered()
    {
        var order = Order.Create(SupplierId, UserId);
        order.AddItem(Guid.NewGuid(), 1, 5m);
        order.Send(UserId);
        order.Confirm(UserId);
        order.MarkInTransit();

        order.MarkDelivered(UserId);

        Assert.Equal(OrderStatuses.Delivered, order.Status);
    }

    [Fact]
    public void Cancel_ShouldAllowCancelFromAnySentStatus()
    {
        foreach (var setup in new Action<Order>[]
        {
            o => { },                                                       // Draft
            o => { o.AddItem(Guid.NewGuid(), 1, 5m); o.Send(UserId); },   // Sent
            o => { o.AddItem(Guid.NewGuid(), 1, 5m); o.Send(UserId); o.Confirm(UserId); } // Confirmed
        })
        {
            var order = Order.Create(SupplierId, UserId);
            setup(order);

            order.Cancel(UserId, "Test cancel");

            Assert.Equal(OrderStatuses.Cancelled, order.Status);
        }
    }

    [Fact]
    public void Cancel_ShouldThrow_WhenAlreadyDelivered()
    {
        var order = Order.Create(SupplierId, UserId);
        order.AddItem(Guid.NewGuid(), 1, 5m);
        order.Send(UserId);
        order.Confirm(UserId);
        order.MarkInTransit();
        order.MarkDelivered(UserId);

        Assert.Throws<InvalidOperationException>(() => order.Cancel(UserId));
    }

    [Fact]
    public void Cancel_ShouldThrow_WhenAlreadyCancelled()
    {
        var order = Order.Create(SupplierId, UserId);
        order.Cancel(UserId);

        Assert.Throws<InvalidOperationException>(() => order.Cancel(UserId));
    }

    [Fact]
    public void IsOverdue_ShouldReturnTrue_WhenPastExpectedDelivery()
    {
        var order = Order.Create(SupplierId, UserId,
            expectedDeliveryDate: DateTime.UtcNow.AddDays(-1));

        Assert.True(order.IsOverdue);
    }

    [Fact]
    public void IsOverdue_ShouldReturnFalse_WhenDelivered()
    {
        var order = Order.Create(SupplierId, UserId,
            expectedDeliveryDate: DateTime.UtcNow.AddDays(-1));
        order.AddItem(Guid.NewGuid(), 1, 5m);
        order.Send(UserId);
        order.Confirm(UserId);
        order.MarkInTransit();
        order.MarkDelivered(UserId);

        Assert.False(order.IsOverdue);
    }

    [Fact]
    public void TotalAmount_ShouldSumAllItems()
    {
        var order = Order.Create(SupplierId, UserId);
        order.AddItem(Guid.NewGuid(), 2, 10.00m);  // 20
        order.AddItem(Guid.NewGuid(), 3, 5.50m);   // 16.50

        Assert.Equal(36.50m, order.TotalAmount);
    }
}

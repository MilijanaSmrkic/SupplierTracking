namespace SupplierTracking.Domain.Entities;

public static class OrderStatuses
{
    public const string Draft      = "Draft";
    public const string Sent       = "Sent";
    public const string Confirmed  = "Confirmed";
    public const string InTransit  = "InTransit";
    public const string Delivered  = "Delivered";
    public const string Cancelled  = "Cancelled";
}

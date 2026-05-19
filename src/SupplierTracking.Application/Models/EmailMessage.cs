namespace SupplierTracking.Application.Models;

public record EmailMessage(
    string To,
    string Subject,
    string Body);

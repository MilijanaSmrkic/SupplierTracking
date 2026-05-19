# Supplier Tracking API

Real-time supplier order tracking built with **.NET 9**, Clean Architecture, CQRS, SignalR, Hangfire, and inbound webhooks.

---

## Overview

The system lets internal users manage purchase orders with a full lifecycle state machine, while suppliers can push status updates back via signed webhooks. Every status change is broadcast in real time over SignalR, logged in an audit trail, and background jobs escalate overdue orders automatically.

```
Draft ──► Sent ──► Confirmed ──► InTransit ──► Delivered
  │         │           │
  └─────────┴───────────┴──────────────────────► Cancelled
```

---

## Tech Stack

| Concern              | Technology                                      |
|----------------------|-------------------------------------------------|
| Framework            | ASP.NET Core 9                                  |
| Architecture         | Clean Architecture (Domain / Application / Infrastructure / Api) |
| CQRS / Mediator      | MediatR 14                                      |
| Persistence          | EF Core 9 + SQL Server (LocalDB for dev)        |
| Real-time            | SignalR (`/hubs/orders`)                        |
| Background Jobs      | Hangfire (SQL Server storage)                   |
| Authentication       | JWT Bearer                                      |
| Validation           | FluentValidation (MediatR pipeline behaviour)   |
| Logging              | Serilog (console + rolling file)                |
| API Docs             | Swagger / OpenAPI (Swashbuckle)                 |
| Email                | SMTP with retry                                 |
| Testing              | xUnit + Moq + NullLogger                        |

---

## Project Structure

```
src/
├── SupplierTracking.Domain          # Entities, value objects, domain logic
├── SupplierTracking.Application     # CQRS commands/queries, abstractions, validators
├── SupplierTracking.Infrastructure  # EF Core, repositories, JWT, SignalR, Hangfire, SMTP
└── SupplierTracking.Api             # Controllers, middleware, startup

tests/
├── SupplierTracking.Application.Tests   # Domain + command handler unit tests (56 tests)
├── SupplierTracking.Integration.Tests   # Infrastructure integration tests (8 tests)
└── SupplierTracking.Api.Tests           # API-level tests
```

---

## Getting Started

### Prerequisites

- .NET 9 SDK
- SQL Server LocalDB (ships with Visual Studio) **or** any SQL Server instance

### 1. Configure secrets (user-secrets)

Sensitive values are **never** stored in `appsettings.json`. Use [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) for local development:

```bash
cd src/SupplierTracking.Api

dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=(localdb)\mssqllocaldb;Database=SupplierTrackingDb;Trusted_Connection=True;"

dotnet user-secrets set "JwtSettings:SecretKey" "your-secret-key-min-32-characters!!"
```

In production, set environment variables instead:
```
CONNECTIONSTRINGS__DEFAULTCONNECTION=...
JWTSETTINGS__SECRETKEY=...
SMTP__PASSWORD=...
```

### 2. Apply migrations

```bash
cd src/SupplierTracking.Api
dotnet ef database update
```

### 3. Run the API

```bash
dotnet run --project src/SupplierTracking.Api
```

The API starts on `https://localhost:7xxx`. Navigate to:

- **Swagger UI** → `https://localhost:7xxx/swagger`
- **Hangfire Dashboard** → `https://localhost:7xxx/hangfire` *(Development only)*

### 4. Login

On first start the admin user is seeded automatically.

```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "Admin123!"
}
```

Copy the returned `token` and click **Authorize** in Swagger UI → paste as `Bearer <token>`.

---

## API Reference

### Authentication

| Method | Route            | Description             |
|--------|------------------|-------------------------|
| POST   | `/api/auth/login`| Get a JWT token         |

### Suppliers

| Method | Route                  | Role          | Description              |
|--------|------------------------|---------------|--------------------------|
| GET    | `/api/suppliers`       | Any           | List all suppliers       |
| GET    | `/api/suppliers/{id}`  | Any           | Get supplier details     |
| POST   | `/api/suppliers`       | Admin/Manager | Create a supplier        |

### Orders

| Method | Route                         | Role          | Description                          |
|--------|-------------------------------|---------------|--------------------------------------|
| GET    | `/api/orders`                 | Any           | Paged list (filter by supplier/status) |
| GET    | `/api/orders/{id}`            | Any           | Full order detail + items + log      |
| POST   | `/api/orders`                 | Admin/Manager | Create order (Draft)                 |
| POST   | `/api/orders/{id}/send`       | Admin/Manager | Draft → Sent                         |
| POST   | `/api/orders/{id}/confirm`    | Admin/Manager | Sent → Confirmed                     |
| POST   | `/api/orders/{id}/in-transit` | Admin/Manager | Confirmed → InTransit                |
| POST   | `/api/orders/{id}/deliver`    | Admin/Manager | InTransit → Delivered                |
| POST   | `/api/orders/{id}/cancel`     | Any*          | Cancel (Draft/Sent/Confirmed)        |

> *Viewers can only cancel their own orders. Managers and Admins can cancel any order.

### Webhooks

| Method | Route                               | Description                              |
|--------|-------------------------------------|------------------------------------------|
| POST   | `/api/webhooks/supplier/{id}`       | Supplier pushes a status update          |

See the [Webhook Integration](#webhook-integration) section for signing details.

---

## User Roles

| Role    | Capabilities                                              |
|---------|-----------------------------------------------------------|
| Admin   | Full access — all operations + role management           |
| Manager | Create/manage suppliers and orders                       |
| Viewer  | Read-only + can cancel their own orders                  |

---

## Real-time with SignalR

Connect to the hub at `/hubs/orders` using a standard SignalR client and a JWT token:

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/orders", { accessTokenFactory: () => token })
    .build();

// Subscribe to status changes for all orders
connection.on("OrderStatusChanged", (data) => {
    console.log(`Order ${data.orderNumber}: ${data.fromStatus} → ${data.toStatus}`);
});

// Subscribe to a specific supplier's orders only
await connection.invoke("JoinSupplierGroup", supplierId);
connection.on("OrderStatusChanged", handler);

await connection.start();
```

---

## Webhook Integration

Each supplier has a **webhook secret** generated on creation (Base64-encoded 32-byte random key). Retrieve it from `GET /api/suppliers/{id}`.

To push an order status update, the supplier POSTs a signed request:

### Request

```http
POST /api/webhooks/supplier/{supplierId}
Content-Type: application/json
X-Supplier-Signature: sha256={hmac_hex}

{
  "orderNumber": "ORD-20240101-ABC123",
  "event": "shipped",
  "trackingCode": "DHL-999888",
  "notes": null
}
```

### Signature Calculation

```python
import hmac, hashlib

signature = "sha256=" + hmac.new(
    key=webhook_secret.encode(),
    msg=raw_body.encode(),
    digestmod=hashlib.sha256
).hexdigest()
```

### Supported Events

| Event       | Transition               |
|-------------|--------------------------|
| `confirmed` | Sent → Confirmed         |
| `shipped`   | Confirmed → InTransit    |
| `delivered` | InTransit → Delivered    |

The signature is validated with **constant-time comparison** (HMAC timing-attack safe).

---

## Background Jobs

Hangfire runs two recurring jobs:

| Job                 | Schedule        | Description                                          |
|---------------------|-----------------|------------------------------------------------------|
| Overdue Orders Check| Every 6 hours   | Finds orders past expected delivery; emails supplier after 3+ overdue days |
| Daily Digest        | 08:00 UTC daily | Sends a summary email of active and overdue orders   |

Configure SMTP in `appsettings.json`:

```json
"Smtp": {
  "Host": "smtp.example.com",
  "Port": 587,
  "UseSsl": true,
  "Username": "user@example.com",
  "Password": "secret",
  "From": "noreply@suppliertracking.com",
  "DigestRecipient": "ops-team@example.com"
}
```

---

## Running Tests

```bash
dotnet test
```

```
Passed! - Failed: 0, Passed: 56  - SupplierTracking.Application.Tests
Passed! - Failed: 0, Passed:  8  - SupplierTracking.Integration.Tests
```

### Test Coverage

| Area                           | Tests |
|--------------------------------|-------|
| Order domain logic             | 14    |
| Supplier domain logic          | 9     |
| CreateOrder handler            | 4     |
| SendOrder handler              | 4     |
| ConfirmOrder handler           | 4     |
| MarkInTransit handler          | 4     |
| MarkDelivered handler          | 4     |
| CancelOrder handler            | 5     |
| ProcessWebhook handler         | 7     |
| WebhookSignatureValidator      | 8     |

---

## Configuration Reference

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "<SQL Server connection string>"
  },
  "JwtSettings": {
    "SecretKey": "<min 32 chars>",
    "Issuer": "SupplierTracking.Api",
    "Audience": "SupplierTracking.Client",
    "ExpiryMinutes": 60
  },
  "Smtp": {
    "Host": "",
    "Port": 587,
    "UseSsl": true,
    "Username": "",
    "Password": "",
    "From": "noreply@suppliertracking.com",
    "DigestRecipient": ""
  }
}
```

> **Note:** Leave `Smtp.Host` empty to disable email sending in development.

# Library System – Backend API

Production-oriented .NET 10 Web API for the Library System. Implements the
documented API contract (v1.0.0) with N-Tier architecture, JWT authentication
with refresh-token rotation, RabbitMQ-driven request processing, SignalR real-time
notifications and a configurable borrowing-expiration background job.

## Solution structure

Single ASP.NET Core project organized into N-Tier folders:

```
Library-System-API/
└── Library-System-API/
    ├── Controllers/           # One controller per category:
    │                          #   Auth, Books, BorrowingRequests,
    │                          #   AdminActions, Notifications, Users
    ├── Extensions/            # DI composition root, result→HTTP mapping,
    │                          # claims helpers, seeder
    ├── Middleware/            # Centralized exception handling
    │                          # (sanitized error envelope + traceId)
    ├── Business/              # Business layer
    │   ├── BackgroundJobs/    # BorrowingExpirationJob + configurable settings
    │   ├── DTOs/              # Auth / Books / Requests / Notifications / Users
    │   ├── Hubs/              # SignalR NotificationsHub (/hubs/notifications)
    │   ├── Interfaces/        # Service abstractions
    │   ├── Mappings/          # Manual entity→DTO mappings (no mapping library)
    │   ├── Messaging/         # RabbitMQ publisher, consumer (hosted service),
    │   │                        # settings
    │   ├── Notifications/     # Real-time dispatcher abstraction + SignalR impl
    │   ├── Services/          # Business rules and workflows
    │   └── Validators/        # DataAnnotations-based validation gate
    ├── DataAccess/            # Data access layer
    │   ├── Configurations/    # IEntityTypeConfiguration<T> per entity
    │   ├── Context/           # LibraryDBContext
    │   ├── Entities/
    │   ├── Interfaces/        # IGenericRepository<T>, entity-specific repos
    │   ├── Migrations/        # EF Core migrations
    │   ├── Repositories/      # AsNoTracking reads; tracked reads only for updates
    │   └── UnitOfWork/        # Coordinates SaveChanges + transactions
    ├── Shared/                # Cross-cutting concerns
    │   ├── Authentication/    # JwtService, refresh-token management,
    │   │                        # PBKDF2 password hashing
    │   ├── Constants/         # Stable machine-readable error codes
    │   ├── Enums/             # UserRole, BorrowingRequestStatus, NotificationType
    │   ├── Results/           # Result / Result<T> / PagedResult<T> pattern
    │   └── Security/          # PBKDF2 password hashing
    ├── Tests/                 # MSTest + FluentAssertions + Moq unit tests
    │   ├── AuthAndUserManagementTests.cs
    │   ├── BorrowingServiceCreateTests.cs
    │   ├── BorrowingServiceDecisionTests.cs
    │   └── NotificationServiceTests.cs
    └── Program.cs
```

Layer discipline is enforced by namespace and convention: Controllers never touch
`DataAccess`; business services never touch `DbContext`; all persistence is
coordinated through repositories and the Unit of Work.

## Key architectural decisions

| Decision | Rationale |
| --- | --- |
| Added `POST /api/auth/login` | The documentation mandates JWT Bearer auth on every endpoint but defines no token-issuing endpoint; without it the contract is unusable. All other documented routes/methods are unchanged. |
| Refresh-token rotation (`POST /api/auth/refresh-token`) | Short-lived access tokens (120 min) paired with rotating refresh tokens (7 days) limit the window of credential exposure. Every rotation revokes the old token. |
| `POST /api/auth/forgot-password` / `reset-password` | Password reset via emailed single-use token; endpoint responds identically whether or not the email is registered to prevent account enumeration. |
| `POST /api/auth/create-admin` | One-time bootstrap to create the first administrator; subsequent admins are created via `POST /api/admin/users`. |
| `DELETE /api/auth/account` | Self-service account deletion with password verification; removes borrowing history and notifications. |
| Client-supplied `userId` / `approvedByAdminId` validated against JWT claims | Security: never trust identity from the body when it can come from signed claims. Regular users can only act as themselves. |
| Pending request is persisted synchronously, then a message (`RequestId`) is published to RabbitMQ | The documented `POST /api/borrow` must return **201** with a BorrowingRequest body — impossible if the row were created asynchronously by the consumer. Publishing only the id makes redeliveries idempotent (the consumer re-checks state). |
| Approval/denial run inside DB transactions with tracked reloads | Concurrent approvals of different requests see committed state inside their transaction; availability can never go negative (also guarded by SQL check constraints). |
| Result pattern instead of exceptions | Expected business failures map to documented status codes (400/403/404/409/422); exceptions are reserved for system failures handled by the middleware with sanitized payloads + traceId. |
| Reminder de-duplication via internal `Notification.RelatedRequestId` column | Prevents repeated due-date reminders per (request, recipient); never exposed in API contracts. |
| Manual mapping instead of AutoMapper | Small, explicit surface; no extra dependency. |
| Admin user management (`/api/admin/users`) | Centralized admin-controlled user CRUD with role assignment and account-status toggling; the last remaining admin cannot be demoted or deactivated. |

## Endpoints

### Authentication (`/api/auth`)

| Method | Route | Roles | Notes |
| --- | --- | --- | --- |
| POST | `/api/auth/login` | anonymous | Returns `{ token, expiresAtUtc, userId, email, role }` |
| POST | `/api/auth/register` | anonymous | Returns `{ token, expiresAtUtc, userId, email, role }` (201) |
| POST | `/api/auth/forgot-password` | anonymous | Emails reset link; response identical regardless of email existence |
| POST | `/api/auth/reset-password` | anonymous | Completes reset with single-use token; 400/422 |
| POST | `/api/auth/create-admin` | anonymous | One-time bootstrap; 409 if admin already exists |
| POST | `/api/auth/refresh-token` | anonymous | Exchanges refresh token; rotates (revokes old) |
| POST | `/api/auth/revoke-refresh-token` | authenticated | Idempotent logout; 204 |
| DELETE | `/api/auth/account` | authenticated | Self-deletion with password verification; 204 |

### Books (`/api/books`)

| Method | Route | Roles | Notes |
| --- | --- | --- | --- |
| GET | `/api/books` | User, Admin | Paging/search/sort/`availableOnly`, max pageSize 100 |
| GET | `/api/books/{id}` | User, Admin | |

### Borrowing requests (`/api`)

| Method | Route | Roles | Notes |
| --- | --- | --- | --- |
| POST | `/api/borrow` | User | 201; publishes to RabbitMQ |
| GET | `/api/requests` | Admin | Paging + status/user/book/date filters |
| GET | `/api/requests/my` | User | Scoped to caller from JWT |
| GET | `/api/requests/{id}` | User, Admin | Users see only their own requests |
| PUT | `/api/requests/{id}/approve` | Admin | Atomic copy decrement |
| PUT | `/api/requests/{id}/deny` | Admin | Reason required |

### Admin user management (`/api/admin/users`)

| Method | Route | Roles | Notes |
| --- | --- | --- | --- |
| GET | `/api/admin/users` | Admin | Paging + name/email search |
| POST | `/api/admin/users` | Admin | Creates account with explicit role; 201 |
| PUT | `/api/admin/users/{userId}/role` | Admin | Last admin cannot be demoted |
| PUT | `/api/admin/users/{userId}/status` | Admin | Cannot deactivate self or last admin |

### Notifications (`/api/notifications`)

| Method | Route | Roles | Notes |
| --- | --- | --- | --- |
| GET | `/api/notifications` | User, Admin | Recipient scope enforced from JWT |
| PUT | `/api/notifications/{id}/read` | User, Admin | Ownership enforced |

### Real-time

| Method | Route | Roles | Notes |
| --- | --- | --- | --- |
| WS | `/hubs/notifications` | authenticated | SignalR; JWT via `access_token` query string |

## Configuration

All sensitive values come from configuration. Never commit secrets.

| Section | Keys |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | SQL Server connection string |
| `Jwt` | `Secret` (min 32 chars), `Issuer`, `Audience`, `ExpiryMinutes`, `RefreshTokenExpirationDays` |
| `RabbitMq` | `HostName`, `Port`, `UserName`, `Password`, `BorrowRequestQueue` |
| `BackgroundJobs` | `ExpirationCheckIntervalMinutes`, `ReminderDaysBeforeDue` |
| `Smtp` | `Host`, `Port`, `UserName`, `Password`, `From`, `EnableSsl` |
| `App` | `FrontendBaseUrl`, `CorsOrigins` |
| `Logging` | `LogLevel` defaults |

Local development uses user secrets or `appsettings.Development.json`
(a dev-only placeholder secret is included there). Production must supply the
JWT secret and RabbitMQ credentials via environment variables or a secure store.

## Running

1. Configure `DefaultConnection` to your SQL Server instance.
2. `dotnet ef database update --project Library-System-API`
   (the app also migrates automatically at startup outside production).
3. Start RabbitMQ (e.g., `docker run -p 5672:5672 rabbitmq:3`). If the broker is
   unreachable, requests are still persisted and reviewed manually; publishing
   failures are logged.
4. Ensure SMTP credentials are configured for password-reset and reminder emails.
5. `dotnet run --project Library-System-API` and open Swagger at `/swagger`.

Seeded development accounts (created automatically when the users table is empty):

| Email | Password | Role |
| --- | --- | --- |
| `admin@library.local` | `Admin@12345` | Admin |
| `user@library.local` | `User@12345` | User |

## Tests

```sh
dotnet test
```

Covers borrowing validation, ownership rules, availability logic,
approval/denial state transitions, notification inbox security, reminder
de-duplication, auth flows (login, register, refresh-token, forgot-password,
reset-password, account deletion), and admin user management.

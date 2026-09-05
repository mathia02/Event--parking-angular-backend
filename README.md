# Event & Parking Reservation System

Backend REST API for an Event and Parking Reservation System.

## Technology Stack

- ASP.NET Core Web API
- .NET 8
- Entity Framework Core
- SQL Server / LocalDB
- JWT Authentication
- Swagger / OpenAPI
- xUnit
- Moq

## Project Structure

- EventParking.Api
- EventParking.Business
- EventParking.DataAccess
- EventParking.Models
- EventParking.Tests

## Main Features

- Customer registration and login
- Email verification
- JWT authentication and authorization
- Administrator and Customer roles
- Venue management
- Category management
- Event management
- Seat management
- Parking slot management
- Booking management
- Payment management
- Notification management
- Admin dashboard
- Customer dashboard
- Booking expiry background service
- Event reminder background service

## Business Rules

- Events must start in the future.
- Event end time must be later than start time.
- Event capacity cannot exceed venue capacity.
- Event capacity must match the generated seat map.
- The same venue cannot contain overlapping events.
- Customers must be active and email verified before booking.
- Only available seats and parking slots can be reserved.
- Paid bookings remain pending until payment is completed.
- Free bookings are confirmed automatically.
- Paid confirmed bookings cannot be cancelled because refund support is not implemented.
- Pending booking holds expire automatically.
- Seats and parking slots are released when eligible bookings are cancelled or expired.

## Requirements

Install:

- .NET 8 SDK
- Visual Studio 2022
- SQL Server LocalDB or SQL Server
- Git

## Restore Packages

```bash
dotnet restore EventParkingReservationSystem.sln
```

## Development Secrets

Sensitive values are stored using .NET User Secrets.

Initialize User Secrets:

```bash
dotnet user-secrets init --project Backend/EventParking.Api/EventParking.Api.csproj
```

Set development passwords:

```bash
dotnet user-secrets set "Seed:AdminPassword" "<admin-password>" --project Backend/EventParking.Api/EventParking.Api.csproj

dotnet user-secrets set "Seed:CustomerPassword" "<customer-password>" --project Backend/EventParking.Api/EventParking.Api.csproj
```

Set JWT signing key:

```bash
dotnet user-secrets set "Jwt:Key" "<strong-random-key>" --project Backend/EventParking.Api/EventParking.Api.csproj
```

Do not commit secret values to GitHub.

## Build

```bash
dotnet build EventParkingReservationSystem.sln
```

## Run API

```bash
dotnet run --project Backend/EventParking.Api/EventParking.Api.csproj
```

Swagger is available in Development mode.

## Run Tests

```bash
dotnet test EventParkingReservationSystem.sln
```

## Health Check

```text
GET /api/health
```

Expected response:

```json
{
  "status": "Healthy",
  "application": "EventParking.Api"
}
```

## Database

In Development mode, pending migrations and demo seed data are applied during API startup.

Production database migrations should be applied explicitly during deployment.

```bash
dotnet ef database update --project Backend/EventParking.DataAccess/EventParking.DataAccess.csproj --startup-project Backend/EventParking.Api/EventParking.Api.csproj
```

## Security

- JWT signing keys are not stored in source control.
- Development account passwords use User Secrets.
- Swagger is enabled only in Development.
- Demo database seeding runs only in Development.
- Database conflicts are converted into appropriate API responses.

## Frontend

The frontend will be developed using Angular.

Default local Angular URL:

```text
http://localhost:4200
```

## Final Verification

```bash
dotnet restore EventParkingReservationSystem.sln
dotnet build EventParkingReservationSystem.sln --configuration Release
dotnet test EventParkingReservationSystem.sln --configuration Release
```
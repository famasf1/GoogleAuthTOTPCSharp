---
layout: default
title: Local Development Setup
---

# Local Development Setup

## Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 or Visual Studio Code
- Git
- Google Cloud Console account (for OAuth setup)

## Getting Started

### 1. Clone the Repository

```bash
git clone <repository-url>
cd google-auth-totp-prototype
```

### 2. Install Dependencies

```bash
dotnet restore
```

### 3. Database Setup

```bash
# Create and update the database
dotnet ef database update
```

### 4. Configuration

Create or update `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=app.db"
  },
  "Authentication": {
    "Google": {
      "ClientId": "your-google-client-id",
      "ClientSecret": "your-google-client-secret"
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### 5. Run the Application

```bash
dotnet run
```

The application will be available at `https://localhost:5001` or `http://localhost:5000`.

## Development Workflow

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Database Migrations

```bash
# Add a new migration
dotnet ef migrations add <MigrationName>

# Update database
dotnet ef database update

# Remove last migration
dotnet ef migrations remove
```

## Troubleshooting

*Common issues and solutions will be documented as they are encountered.*
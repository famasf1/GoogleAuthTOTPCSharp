# Google Auth TOTP Prototype

A C# ASP.NET Core Razor Pages web application prototype that demonstrates secure authentication using Google OAuth combined with Time-based One-Time Password (TOTP) two-factor authentication.

## Overview

This application provides a complete authentication flow where users:
1. Register with basic information and authenticate through Google OAuth
2. Set up two-factor authentication using TOTP (compatible with Google Authenticator)
3. Login using both Google authentication and TOTP verification
4. Access a protected dashboard area

## Features

- **Google OAuth Integration**: Secure authentication using Google accounts
- **TOTP Two-Factor Authentication**: Time-based one-time passwords for enhanced security
- **Account Lockout Protection**: Temporary account locking after failed TOTP attempts
- **QR Code Generation**: Easy TOTP setup with QR codes for authenticator apps
- **Session Management**: Secure session handling with proper timeout and cleanup
- **Comprehensive Error Handling**: User-friendly error messages and logging

## Technology Stack

- **Framework**: ASP.NET Core 8.0 with Razor Pages
- **Authentication**: ASP.NET Core Identity + Google OAuth
- **Database**: Entity Framework Core with SQLite
- **TOTP**: OtpNet library for TOTP generation and validation
- **QR Codes**: QRCoder library for generating setup QR codes
- **Containerization**: Docker support for easy deployment

## Current Implementation Status

### ✅ Completed Tasks

1. **Project Structure and Dependencies** - Complete ASP.NET Core setup with all required NuGet packages
2. **Database and Identity System** - ApplicationUser model, Entity Framework configuration, and database migrations
3. **TOTP Service Implementation** - Full TOTP functionality with secret generation, QR codes, and validation
4. **Documentation Deployment** - GitHub Pages setup with Jekyll for comprehensive documentation

### 🚧 In Progress

- GitHub Pages documentation deployment and configuration

### 📋 Upcoming Tasks

- User service for TOTP management and account lockout
- Google OAuth configuration and integration
- User registration and login pages
- TOTP setup and verification pages
- Protected dashboard implementation
- Comprehensive error handling and security features

## Quick Start

### Prerequisites

- .NET 8.0 SDK
- Docker (optional, for containerized development)
- Google Cloud Console project with OAuth 2.0 credentials

### Local Development

1. Clone the repository
2. Install dependencies:
   ```bash
   dotnet restore
   ```

3. Set up the database:
   ```bash
   dotnet ef database update
   ```

4. Configure Google OAuth in `appsettings.json`:
   ```json
   {
     "Authentication": {
       "Google": {
         "ClientId": "your-google-client-id",
         "ClientSecret": "your-google-client-secret"
       }
     }
   }
   ```

5. Run the application:
   ```bash
   dotnet run
   ```

### Docker Development

1. Build and run with Docker Compose:
   ```bash
   docker-compose up --build
   ```

## Documentation

Comprehensive documentation is available at our [GitHub Pages site](https://your-username.github.io/your-repo-name/):

- [Architecture Overview](Docs/Architecture/Overview.md)
- [Service Documentation](Docs/Services/)
- [API Reference](Docs/API/)
- [Testing Strategy](Docs/Testing/)
- [Deployment Guide](Docs/Deployment/)

## Testing

Run the test suite:

```bash
# Unit tests
dotnet test

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Contributing

This is a prototype project for demonstration purposes. The implementation follows best practices for:

- Secure authentication flows
- TOTP implementation
- Session management
- Error handling
- Testing strategies

## Security Considerations

- TOTP secrets are securely generated and stored
- Account lockout protection against brute force attacks
- Secure session management with proper timeouts
- HTTPS enforcement for all authentication flows
- Comprehensive logging for security monitoring

## License

This project is for educational and demonstration purposes.

---

For detailed documentation, visit our [documentation site](https://your-username.github.io/your-repo-name/).
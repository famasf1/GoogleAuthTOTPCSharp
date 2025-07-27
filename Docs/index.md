---
layout: default
title: Google Auth TOTP Prototype Documentation
---

# Google Auth TOTP Prototype Documentation

Welcome to the comprehensive documentation for the Google Auth TOTP Prototype - a secure authentication system combining Google OAuth with Time-based One-Time Password (TOTP) two-factor authentication.

## Quick Navigation

### 🏗️ Architecture & Design
- [Architecture Overview](Architecture/Overview.md) - System architecture and design decisions
- [Security Design](Architecture/Security.md) - Security considerations and implementation
- [Error Handling](Architecture/ErrorHandling.md) - Error handling strategies and implementation

### 🔧 Services & Components
- [TOTP Service](Services/TotpService.md) - Time-based One-Time Password service implementation
- [User Service](Services/UserService.md) - User management and TOTP integration
- [API Reference](API/Services.md) - Complete service interface documentation

### 📄 Pages & UI
- [Registration Page](Pages/Registration.md) - User registration with Google OAuth
- [Login Page](Pages/Login.md) - Google OAuth authentication flow
- [TOTP Setup](Pages/TotpSetup.md) - Two-factor authentication setup
- [TOTP Verification](Pages/TotpVerification.md) - Login verification process
- [Dashboard](Pages/Dashboard.md) - Protected user dashboard

### ⚙️ Configuration & Setup
- [Google OAuth Setup](Configuration/GoogleOAuth.md) - Google Cloud Console configuration
- [Local Development](Deployment/LocalDevelopment.md) - Development environment setup
- [Docker Deployment](Deployment/Docker.md) - Containerized deployment guide

### 🧪 Testing & Quality
- [Testing Strategy](Testing/TestingStrategy.md) - Comprehensive testing approach
- [Test Coverage](Testing/Coverage.md) - Current test coverage and goals

## Getting Started

If you're new to this project, we recommend starting with:

1. **[Architecture Overview](Architecture/Overview.md)** - Understand the system design
2. **[Local Development Guide](Deployment/LocalDevelopment.md)** - Set up your development environment
3. **[TOTP Service Documentation](Services/TotpService.md)** - Learn about the core authentication component

## Implementation Progress

### ✅ Completed Components

- **Project Structure**: Complete ASP.NET Core setup with dependencies
- **Database Layer**: Entity Framework with Identity integration
- **TOTP Service**: Full implementation with QR code generation and validation
- **Documentation**: Comprehensive documentation with GitHub Pages deployment

### 🚧 Current Development

- User service implementation for account management
- Google OAuth integration and configuration
- Razor Pages for authentication flow

### 📋 Upcoming Features

- Complete authentication workflow
- Protected dashboard implementation
- Comprehensive error handling
- CI/CD pipeline setup

## Key Features

### 🔐 Security First
- Google OAuth 2.0 integration for primary authentication
- TOTP-based two-factor authentication
- Account lockout protection against brute force attacks
- Secure session management with proper timeouts

### 🛠️ Developer Friendly
- Clean architecture with clear separation of concerns
- Comprehensive unit and integration tests
- Docker support for consistent development environments
- Detailed documentation for all components

### 📱 User Experience
- Simple registration and setup process
- QR code generation for easy authenticator app setup
- Clear error messages and user guidance
- Responsive design for mobile and desktop

## Technology Stack

- **Backend**: ASP.NET Core 8.0 with Razor Pages
- **Authentication**: ASP.NET Core Identity + Google OAuth
- **Database**: Entity Framework Core with SQLite
- **TOTP**: OtpNet library for secure TOTP implementation
- **QR Codes**: QRCoder library for setup convenience
- **Testing**: xUnit with comprehensive test coverage
- **Documentation**: Jekyll with GitHub Pages
- **Containerization**: Docker and Docker Compose

## Contributing to Documentation

This documentation is automatically built and deployed using GitHub Actions. To contribute:

1. Edit markdown files in the `Docs/` directory
2. Follow the existing structure and naming conventions
3. Add new pages to the navigation in `_config.yml`
4. Test locally using Jekyll before submitting changes

For detailed instructions, see our [Documentation Update Guide](Contributing/Documentation.md).

## Support

For questions about implementation details, check the relevant service documentation or review the test files for usage examples.

---

*Last updated: {{ site.time | date: "%B %d, %Y" }}*
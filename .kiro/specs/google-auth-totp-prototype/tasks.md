# Implementation Plan

- [x] 1. Set up project structure and dependencies
  - Create new ASP.NET Core Razor Pages project
  - Install required NuGet packages (Identity, Google OAuth, OtpNet, QRCoder, Entity Framework)
  - Configure basic project structure with folders for Models, Services, and Data
  - Create Dockerfile for containerizing the application
  - Create docker-compose.yml for development environment setup
  - _Requirements: All requirements depend on basic project setup_

- [ ] 2. Configure database and Identity system
  - Create ApplicationUser model extending IdentityUser with TOTP properties
  - Set up ApplicationDbContext with Identity configuration
  - Configure Entity Framework with SQLite connection
  - Create and run initial database migration
  - _Requirements: 1.3, 2.4, 3.5_

- [ ] 3. Implement TOTP service functionality
  - Create ITotpService interface with methods for secret generation, QR code creation, and validation
  - Implement TotpService class using OtpNet library for TOTP operations
  - Add QR code generation functionality using QRCoder library
  - Write unit tests for TOTP service methods
  - _Requirements: 2.1, 2.2, 2.3, 2.4_

- [ ] 4. Implement user service for TOTP management
  - Create IUserService interface for user-related TOTP operations
  - Implement UserService class with TOTP setup, validation, and lockout logic
  - Add methods for account locking and failure count management
  - Write unit tests for user service lockout scenarios
  - _Requirements: 2.4, 3.6, 3.7_

- [ ] 5. Configure Google OAuth authentication
  - Add Google OAuth configuration to Program.cs
  - Set up authentication middleware and services
  - Configure OAuth client ID and secret in appsettings.json
  - Add required OAuth scopes and redirect URIs
  - _Requirements: 1.2, 3.2, 5.1, 5.2_

- [ ] 6. Create user registration page and functionality
  - Create Register.cshtml Razor page with username and email fields
  - Implement RegisterModel PageModel with Google OAuth integration
  - Add form validation and error handling
  - Implement redirect to TOTP setup after successful registration
  - Write integration tests for registration workflow
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

- [ ] 7. Create TOTP setup page and functionality
  - Create SetupTotp.cshtml Razor page with QR code display
  - Implement SetupTotpModel PageModel with QR code generation
  - Add TOTP verification form for setup confirmation
  - Implement setup completion and user redirect logic
  - Write integration tests for TOTP setup process
  - _Requirements: 2.1, 2.2, 2.3, 2.4_

- [ ] 8. Create login page and Google OAuth integration
  - Create Login.cshtml Razor page with Google login button
  - Implement LoginModel PageModel with OAuth redirect handling
  - Add error handling for Google authentication failures
  - Implement redirect to TOTP verification after successful Google auth
  - Write integration tests for Google OAuth login flow
  - _Requirements: 3.1, 3.2, 5.1, 5.2_

- [ ] 9. Create TOTP verification page and functionality
  - Create VerifyTotp.cshtml Razor page with 6-digit code input
  - Implement VerifyTotpModel PageModel with code validation logic
  - Add account lockout handling for failed attempts
  - Implement successful verification and dashboard redirect
  - Write integration tests for TOTP verification scenarios
  - _Requirements: 3.3, 3.4, 3.5, 3.6, 3.7_

- [ ] 10. Create protected dashboard page
  - Create Dashboard/Index.cshtml Razor page with user information display
  - Implement DashboardModel PageModel with authorization requirements
  - Add logout functionality and session clearing
  - Implement proper authorization checks and redirects
  - Write integration tests for dashboard access control
  - _Requirements: 4.1, 4.2, 4.3, 4.4_

- [ ] 11. Implement comprehensive error handling
  - Add global error handling middleware
  - Implement user-friendly error pages for authentication failures
  - Add proper logging for security events and errors
  - Create error handling for service unavailability scenarios
  - Write tests for error handling scenarios
  - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_

- [ ] 12. Add session management and security features
  - Configure secure session cookies and timeout settings
  - Implement proper session state management for authentication flow
  - Add CSRF protection for forms
  - Configure HTTPS enforcement and security headers
  - Write tests for session security features
  - _Requirements: 3.3, 4.2, 4.3, 5.3_

- [ ] 13. Create comprehensive integration tests
  - Write end-to-end tests for complete registration and login workflows
  - Add tests for account lockout and recovery scenarios
  - Create tests for error handling and edge cases
  - Implement tests for authorization and session management
  - _Requirements: All requirements validation through automated testing_

- [ ] 14. Add configuration and deployment preparation
  - Create comprehensive appsettings.json with all required configurations
  - Add environment-specific configuration files
  - Create database seeding for development/testing
  - Add logging configuration for different environments
  - Update docker-compose.yml with environment variables and volume mounts
  - Create Docker development and production configurations
  - Document Docker setup and deployment instructions
  - _Requirements: Support for all functional requirements through proper configuration_

- [ ] 15. Set up GitHub Actions CI/CD pipeline
  - Create GitHub Actions workflow for automated building and testing
  - Configure automated testing pipeline with unit and integration tests
  - Add Docker image building and artifact creation
  - Create deployment artifacts (published application, Docker images)
  - Set up artifact storage and release management
  - Add configuration for free hosting options (Railway, Render, or Azure free tier)
  - Document CI/CD pipeline and deployment options for free platforms
  - _Requirements: Automated build and artifact creation for deployment flexibility_
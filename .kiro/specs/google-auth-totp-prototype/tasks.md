# Implementation Plan

## Phase 1: Core TOTP Functionality

- [x] 1. Set up project structure and dependencies
  - Create new ASP.NET Core Razor Pages project
  - Install required NuGet packages (Identity, OtpNet, QRCoder, Entity Framework)
  - Configure basic project structure with folders for Models, Services, and Data
  - Create Dockerfile for containerizing the application
  - Create docker-compose.yml for development environment setup
  - _Requirements: All requirements depend on basic project setup_

- [x] 2. Configure database and Identity system
  - Create ApplicationUser model extending IdentityUser with TOTP properties
  - Set up ApplicationDbContext with Identity configuration
  - Configure Entity Framework with SQLite connection
  - Create and run initial database migration
  - _Requirements: 1.3, 2.4, 3.5_

- [x] 3. Implement TOTP service functionality
  - Create ITotpService interface with methods for secret generation, QR code creation, and validation
  - Implement TotpService class using OtpNet library for TOTP operations
  - Add QR code generation functionality using QRCoder library
  - Write unit tests for TOTP service methods
  - Create documentation for TOTP service classes in Docs/Services/TotpService.md
  - _Requirements: 2.1, 2.2, 2.3, 2.4_

- [x] 4. Set up GitHub Pages documentation deployment
  - Create main README.md with project overview and current progress
  - Create Docs/index.md as documentation homepage with navigation to existing docs
  - Set up GitHub Pages configuration with Jekyll for documentation site
  - Create _config.yml for Jekyll configuration with proper theme and navigation
  - Create GitHub Actions workflow (.github/workflows/docs.yml) to automatically build and deploy documentation
  - Configure GitHub repository settings to enable GitHub Pages from gh-pages branch
  - Create documentation template structure for future task documentation
  - Test documentation deployment and verify all existing docs are accessible
  - Add documentation update instructions for future tasks
  - _Requirements: Immediate documentation availability for completed components_

- [x] 5. Implement user service for TOTP management
  - Create IUserService interface for user-related TOTP operations
  - Implement UserService class with TOTP setup, validation, and lockout logic
  - Add methods for account locking and failure count management
  - Write unit tests for user service lockout scenarios
  - Create documentation for user service classes in Docs/Services/UserService.md
  - _Requirements: 2.4, 3.6, 3.7_

- [ ] 6. Create basic user registration page (without OAuth)
  - Create Register.cshtml Razor page with username, email, and password fields
  - Implement RegisterModel PageModel with basic Identity registration
  - Add form validation and error handling
  - Implement redirect to TOTP setup after successful registration
  - Write integration tests for registration workflow
  - Create documentation for registration page in Docs/Pages/Registration.md
  - _Requirements: 1.1, 1.3, 1.4, 1.5_

- [ ] 7. Create TOTP setup page and functionality
  - Create SetupTotp.cshtml Razor page with QR code display
  - Implement SetupTotpModel PageModel with QR code generation
  - Add TOTP verification form for setup confirmation
  - Implement setup completion and user redirect logic
  - Write integration tests for TOTP setup process
  - Create documentation for TOTP setup page in Docs/Pages/TotpSetup.md
  - _Requirements: 2.1, 2.2, 2.3, 2.4_

- [ ] 8. Create basic login page (without OAuth)
  - Create Login.cshtml Razor page with username/email and password fields
  - Implement LoginModel PageModel with Identity authentication
  - Add error handling for authentication failures
  - Implement redirect to TOTP verification after successful login
  - Write integration tests for basic login flow
  - Create documentation for login page in Docs/Pages/Login.md
  - _Requirements: 3.1, 3.3_

- [ ] 9. Create TOTP verification page and functionality
  - Create VerifyTotp.cshtml Razor page with 6-digit code input
  - Implement VerifyTotpModel PageModel with code validation logic
  - Add account lockout handling for failed attempts
  - Implement successful verification and dashboard redirect
  - Write integration tests for TOTP verification scenarios
  - Create documentation for TOTP verification page in Docs/Pages/TotpVerification.md
  - _Requirements: 3.3, 3.4, 3.5, 3.6, 3.7_

- [ ] 10. Create protected dashboard page
  - Create Dashboard/Index.cshtml Razor page with user information display
  - Implement DashboardModel PageModel with authorization requirements
  - Add logout functionality and session clearing
  - Implement proper authorization checks and redirects
  - Write integration tests for dashboard access control
  - Create documentation for dashboard page in Docs/Pages/Dashboard.md
  - _Requirements: 4.1, 4.2, 4.3, 4.4_

- [ ] 11. Implement comprehensive error handling
  - Add global error handling middleware
  - Implement user-friendly error pages for authentication failures
  - Add proper logging for security events and errors
  - Create error handling for service unavailability scenarios
  - Write tests for error handling scenarios
  - Create documentation for error handling in Docs/Architecture/ErrorHandling.md
  - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_

- [ ] 12. Add session management and security features
  - Configure secure session cookies and timeout settings
  - Implement proper session state management for authentication flow
  - Add CSRF protection for forms
  - Configure HTTPS enforcement and security headers
  - Write tests for session security features
  - Create documentation for security features in Docs/Architecture/Security.md
  - _Requirements: 3.3, 4.2, 4.3, 5.3_

- [ ] 13. Create comprehensive integration tests for Phase 1
  - Write end-to-end tests for complete registration and login workflows
  - Add tests for account lockout and recovery scenarios
  - Create tests for error handling and edge cases
  - Implement tests for authorization and session management
  - _Requirements: All Phase 1 requirements validation through automated testing_

## Phase 2: Google OAuth Integration

- [ ] 14. Configure Google OAuth authentication
  - Install Google OAuth NuGet packages
  - Add Google OAuth configuration to Program.cs
  - Set up authentication middleware and services
  - Configure OAuth client ID and secret in appsettings.json
  - Add required OAuth scopes and redirect URIs
  - Create documentation for Google OAuth setup in Docs/Configuration/GoogleOAuth.md
  - _Requirements: 1.2, 3.2, 5.1, 5.2_

- [ ] 15. Enhance registration page with Google OAuth
  - Add Google OAuth registration option to existing Register.cshtml
  - Implement Google OAuth registration flow in RegisterModel
  - Add error handling for Google authentication failures
  - Maintain existing username/password registration as alternative
  - Update integration tests for OAuth registration workflow
  - Update documentation for enhanced registration in Docs/Pages/Registration.md
  - _Requirements: 1.2, 1.3, 1.4, 1.5_

- [ ] 16. Enhance login page with Google OAuth
  - Add Google login button to existing Login.cshtml
  - Implement Google OAuth login flow in LoginModel
  - Add error handling for Google authentication failures
  - Maintain existing username/password login as alternative
  - Update integration tests for OAuth login workflow
  - Update documentation for enhanced login in Docs/Pages/Login.md
  - _Requirements: 3.1, 3.2, 5.1, 5.2_

- [ ] 17. Create comprehensive integration tests for Phase 2
  - Write end-to-end tests for Google OAuth registration and login workflows
  - Add tests for OAuth error handling scenarios
  - Create tests for mixed authentication methods (OAuth + basic)
  - Update existing tests to cover both authentication methods
  - _Requirements: All Phase 2 requirements validation through automated testing_

## Phase 3: Deployment and Documentation

- [ ] 18. Add configuration and deployment preparation
  - Create comprehensive appsettings.json with all required configurations
  - Add environment-specific configuration files
  - Create database seeding for development/testing
  - Add logging configuration for different environments
  - Update docker-compose.yml with environment variables and volume mounts
  - Create Docker development and production configurations
  - Document Docker setup and deployment instructions
  - _Requirements: Support for all functional requirements through proper configuration_

- [ ] 19. Create comprehensive project documentation and GitHub Pages
  - Update main README.md with complete project overview, setup instructions, and usage guide
  - Update Docs/index.md as documentation homepage with navigation structure
  - Create Docs/Architecture/Overview.md with system architecture and design decisions
  - Create Docs/API/Services.md documenting all service interfaces and implementations
  - Create Docs/Testing/TestingStrategy.md documenting testing approach and coverage
  - Create Docs/Deployment/LocalDevelopment.md with local setup and development guide
  - Create Docs/Deployment/Docker.md with containerization and Docker deployment guide
  - Update GitHub Pages configuration for complete documentation site
  - Update GitHub Actions workflow to automatically build and deploy documentation
  - Add documentation versioning and update process
  - _Requirements: Comprehensive documentation for all implemented features and deployment options_

- [ ] 20. Set up GitHub Actions CI/CD pipeline
  - Create GitHub Actions workflow for automated building and testing
  - Configure automated testing pipeline with unit and integration tests
  - Add Docker image building and artifact creation
  - Create deployment artifacts (published application, Docker images)
  - Set up artifact storage and release management
  - Add configuration for free hosting options (Railway, Render, or Azure free tier)
  - Document CI/CD pipeline and deployment options for free platforms
  - _Requirements: Automated build and artifact creation for deployment flexibility_
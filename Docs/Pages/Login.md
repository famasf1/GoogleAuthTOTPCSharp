---
layout: default
title: Login Page
---

# Login Page

The login page provides basic username/password authentication for existing users. This is the first phase implementation without Google OAuth integration.

## Overview

The login page allows registered users to authenticate using their email address and password. After successful authentication, users are redirected to either the TOTP verification page (if TOTP is enabled) or the main application (if TOTP is not enabled).

## Features

- Email and password authentication fields
- "Remember me" checkbox for persistent login
- Form validation with client-side and server-side validation
- Error handling for authentication failures
- Account lockout protection after multiple failed attempts
- Redirect to TOTP verification after successful login (for TOTP-enabled users)
- Redirect to main application for users without TOTP enabled

## Implementation

### Controller: AuthenticationController

The login functionality is implemented in the `AuthenticationController` class:

- **GET /Authentication/Login**: Displays the login form
- **POST /Authentication/Login**: Processes login credentials

### View Model: VMPARAMLoginRequest

```csharp
public class VMPARAMLoginRequest
{
    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me?")]
    public bool RememberMe { get; set; }
}
```

### Service: AuthenticationService

The `AuthenticationService` handles the authentication logic:

- User lookup by email address
- Password verification using ASP.NET Core Identity
- Account lockout management
- Redirect logic based on TOTP status

### View: Login.cshtml

The Razor view provides:
- Bootstrap-styled form with email, password, and remember me fields
- Client-side validation using ASP.NET Core validation helpers
- Error message display
- Link to registration page for new users

## User Flow

### Successful Login (No TOTP)
1. User visits `/Authentication/Login`
2. User enters email and password
3. System validates credentials
4. User is redirected to main application (`/`)

### Successful Login (With TOTP)
1. User visits `/Authentication/Login`
2. User enters email and password
3. System validates credentials
4. User is redirected to TOTP verification (`/Totp/Verify`)

### Failed Login
1. User visits `/Authentication/Login`
2. User enters invalid credentials
3. System displays error message: "Invalid email or password"
4. User remains on login page

### Account Lockout
1. User makes multiple failed login attempts (5 attempts by default)
2. Account is locked for 5 minutes
3. System displays: "Account locked out. Please try again later."

## Security Features

- Password hashing using ASP.NET Core Identity
- Account lockout after failed attempts (configurable)
- Anti-forgery token protection
- Secure session management
- Generic error messages to prevent user enumeration

## Error Handling

The login page handles various error scenarios:

- **Invalid credentials**: Generic "Invalid email or password" message
- **Account lockout**: "Account locked out. Please try again later."
- **Validation errors**: Field-specific validation messages
- **Service errors**: Generic error message with logging

## Testing

Comprehensive integration tests cover:

- Successful login scenarios (with and without TOTP)
- Invalid credential handling
- Account lockout functionality
- Form validation
- Remember me functionality
- Multiple failed attempt scenarios

## Configuration

Login behavior is configured in `Program.cs`:

```csharp
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    
    // Password requirements
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
});
```

## Future Enhancements

Phase 2 will add Google OAuth integration:
- Google login button alongside basic authentication
- OAuth error handling
- Mixed authentication method support
# Registration Page Documentation

## Overview

The registration page allows new users to create accounts in the Google Auth TOTP Prototype application. This page has been refactored to follow the MVVM (Model-View-ViewModel) pattern with proper separation of concerns.

## Architecture

### MVVM Structure

The registration functionality is organized using the following structure:

```
Services/Authentication/
├── ViewModel/
│   ├── VMPARAMRegisterRequest.cs    # Input model for registration
│   └── VMPARAMRegisterResponse.cs   # Response model for registration
├── Service/
│   ├── IAuthenticationService.cs    # Service interface
│   └── AuthenticationService.cs     # Service implementation
└── Profile/
    └── AuthenticationProfile.cs     # AutoMapper profile
```

### Components

#### ViewModel (VMPARAMRegisterRequest)

The `VMPARAMRegisterRequest` class contains:
- **Username**: 3-50 characters, required
- **Email**: Valid email address, required
- **Password**: 6-100 characters with complexity requirements
- **ConfirmPassword**: Must match password

#### Service Layer (AuthenticationService)

The `AuthenticationService` handles:
- User creation using ASP.NET Core Identity
- Password validation
- Automatic sign-in after successful registration
- Error handling and logging
- Redirect to TOTP setup page

#### Controller (AuthenticationController)

The `AuthenticationController` manages:
- GET requests to display registration form
- POST requests to process registration
- Model validation
- Error display
- Redirects after successful registration

## Features

### Form Validation

- **Client-side validation**: Real-time validation using data annotations
- **Server-side validation**: Comprehensive validation in the service layer
- **Custom validation**: Password confirmation matching

### Security Features

- Anti-forgery token protection
- Password complexity requirements
- Email uniqueness validation
- Secure password hashing via Identity

### User Experience

- Bootstrap-styled responsive form
- Clear error messaging
- Loading states and feedback
- Automatic redirect to TOTP setup

## Usage

### Registration Flow

1. User navigates to `/Authentication/Register`
2. User fills out the registration form
3. Form is validated client-side
4. On submission, server-side validation occurs
5. If valid, user account is created
6. User is automatically signed in
7. User is redirected to TOTP setup page

### Error Handling

The registration process handles various error scenarios:
- Invalid email format
- Password complexity violations
- Password confirmation mismatch
- Duplicate email addresses
- Database connection issues
- General system errors

## API Endpoints

### GET /Authentication/Register
- Displays the registration form
- Returns empty `VMPARAMRegisterRequest` model

### POST /Authentication/Register
- Processes registration form submission
- Accepts `VMPARAMRegisterRequest` model
- Returns success redirect or error view

## Configuration

### Password Requirements

Password requirements are configured in `Program.cs`:
- Minimum length: 6 characters
- Requires digit: Yes
- Requires lowercase: Yes
- Requires uppercase: Yes
- Requires non-alphanumeric: No
- Unique characters: 1

### Lockout Settings

Account lockout is configured for security:
- Lockout duration: 5 minutes
- Max failed attempts: 5
- Enabled for new users: Yes

## Testing

The registration functionality includes comprehensive integration tests:
- Form display validation
- Successful registration flow
- Validation error handling
- Duplicate email handling
- Database integration testing

## Dependencies

- ASP.NET Core Identity
- Entity Framework Core
- AutoMapper
- Bootstrap (UI framework)
- jQuery (client-side validation)

## Future Enhancements

Planned improvements include:
- Email verification workflow
- Social login integration (Google OAuth)
- Enhanced password strength indicators
- CAPTCHA integration
- Account activation via email
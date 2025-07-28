# Registration Page Documentation

## Overview

The Registration page allows new users to create accounts in the Google Auth TOTP Prototype application. This page implements basic username/password registration as the first step in the authentication flow, followed by mandatory TOTP setup.

## Location

- **URL**: `/Authentication/Register`
- **Controller**: `AuthenticationController`
- **Action**: `Register`
- **View**: `Views/Authentication/Register.cshtml`

## Features

### Form Fields

1. **Username**
   - Required field
   - 3-50 characters in length
   - Used as both username and display name
   - Must be unique across the system

2. **Email**
   - Required field
   - Must be a valid email address format
   - Must be unique across the system
   - Used for account identification

3. **Password**
   - Required field
   - Minimum 6 characters
   - Must contain at least one uppercase letter, one lowercase letter, and one digit
   - No special characters required

4. **Confirm Password**
   - Required field
   - Must match the password field exactly
   - Client-side and server-side validation

### Validation

#### Client-Side Validation
- Real-time validation using jQuery Validation
- Immediate feedback on field requirements
- Password confirmation matching

#### Server-Side Validation
- Model validation using Data Annotations
- Identity framework validation rules
- Duplicate email/username checking
- Password complexity requirements

### Security Features

- **Anti-Forgery Token**: CSRF protection on form submission
- **Password Hashing**: Passwords are hashed using ASP.NET Core Identity
- **Input Sanitization**: All inputs are validated and sanitized
- **Secure Redirects**: Post-registration redirect to TOTP setup

## User Flow

1. User navigates to `/Authentication/Register`
2. User fills out the registration form
3. Form is validated client-side and server-side
4. If valid:
   - User account is created in the database
   - User is automatically signed in
   - User is redirected to `/Totp/Setup` for mandatory TOTP configuration
5. If invalid:
   - User remains on registration page
   - Validation errors are displayed
   - User can correct errors and resubmit

## Technical Implementation

### View Model

The registration page uses `VMPARAMRegisterRequest` view model:

```csharp
public class VMPARAMRegisterRequest
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [DataType(DataType.Password)]
    [Compare("Password")]
    public string ConfirmPassword { get; set; }
}
```

### Controller Actions

#### GET /Authentication/Register
- Returns the registration view with empty model
- Sets return URL if provided in query string

#### POST /Authentication/Register
- Validates the submitted model
- Creates new user account if validation passes
- Signs in the user automatically
- Redirects to TOTP setup page
- Returns view with errors if validation fails

### Database Integration

- Uses ASP.NET Core Identity for user management
- Creates `ApplicationUser` entity with extended properties
- Automatically sets `IsTotpEnabled = false` for new users
- Maps username to both `UserName` and `DisplayName` fields

## Error Handling

### Validation Errors
- Field-level validation errors displayed below each input
- Summary validation errors displayed at the top of the form
- Maintains user input on validation failure

### Common Error Scenarios
1. **Duplicate Email**: "Email 'email@example.com' is already taken."
2. **Duplicate Username**: "User name 'username' is already taken."
3. **Password Requirements**: "Passwords must have at least one uppercase, one lowercase, and one digit."
4. **Password Mismatch**: "The password and confirmation password do not match."

### System Errors
- Generic error message for unexpected failures
- Detailed logging for debugging purposes
- Graceful degradation with user-friendly messages

## Styling and UI

### Bootstrap Integration
- Uses Bootstrap 5 for responsive design
- Card-based layout for clean presentation
- Form validation styling with Bootstrap classes

### Responsive Design
- Mobile-friendly form layout
- Proper spacing and typography
- Accessible form controls and labels

### User Experience
- Clear field labels and placeholders
- Intuitive form flow
- Link to login page for existing users

## Testing

### Integration Tests
- Form rendering validation
- Successful registration flow
- Validation error scenarios
- Database user creation verification
- Duplicate email/username handling

### Test Coverage
- Valid registration data processing
- Invalid data validation
- Error message display
- Database integration
- Redirect behavior

## Security Considerations

### Input Validation
- All inputs validated on both client and server
- SQL injection prevention through Entity Framework
- XSS prevention through proper encoding

### Authentication Security
- Passwords hashed using Identity framework
- Secure session management
- Anti-forgery token protection

### Data Protection
- No sensitive data logged
- Secure password storage
- Proper error message handling (no information disclosure)

## Future Enhancements

### Planned Features (Phase 2)
- Google OAuth registration option
- Email verification requirement
- Enhanced password requirements
- Account activation workflow

### Accessibility Improvements
- ARIA labels for screen readers
- Keyboard navigation support
- High contrast mode compatibility

## Related Documentation

- [Authentication Service](../Services/AuthenticationService.md)
- [TOTP Setup Page](TotpSetup.md)
- [Login Page](Login.md)
- [Security Architecture](../Architecture/Security.md)
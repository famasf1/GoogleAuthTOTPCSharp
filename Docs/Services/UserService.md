# User Service Documentation

## Overview

The `UserService` class provides user-related TOTP (Time-based One-Time Password) operations and account lockout management for the Google Auth TOTP Prototype application. It implements the `IUserService` interface and handles TOTP setup, validation, and security features like account locking after failed attempts.

## Interface: IUserService

The `IUserService` interface defines the contract for user-related TOTP operations:

```csharp
public interface IUserService
{
    Task<bool> SetupTotpAsync(string userId, string secret);
    Task<bool> ValidateTotpAsync(string userId, string code);
    Task<bool> IsAccountLockedAsync(string userId);
    Task LockAccountAsync(string userId);
    Task ResetTotpFailuresAsync(string userId);
    Task<TimeSpan?> GetRemainingLockoutTimeAsync(string userId);
    Task<ApplicationUser?> GetUserByIdAsync(string userId);
}
```

## Implementation: UserService

### Dependencies

The `UserService` class depends on the following services:

- `UserManager<ApplicationUser>`: ASP.NET Core Identity user management
- `ApplicationDbContext`: Entity Framework database context
- `ITotpService`: TOTP generation and validation service
- `IConfiguration`: Application configuration
- `ILogger<UserService>`: Logging service

### Configuration

The service reads the following configuration values:

```json
{
  "Totp": {
    "MaxFailureAttempts": 3,
    "LockoutDurationMinutes": 5
  }
}
```

- `MaxFailureAttempts`: Number of consecutive failed TOTP attempts before account lockout (default: 3)
- `LockoutDurationMinutes`: Duration of account lockout in minutes (default: 5)

## Methods

### SetupTotpAsync

Sets up TOTP for a user by storing the secret and enabling TOTP.

```csharp
public async Task<bool> SetupTotpAsync(string userId, string secret)
```

**Parameters:**
- `userId`: User's unique identifier
- `secret`: Base32-encoded TOTP secret

**Returns:** `true` if setup was successful, `false` otherwise

**Behavior:**
- Validates input parameters
- Finds the user by ID
- Stores the TOTP secret and enables TOTP
- Resets any existing failure counts and lockout
- Logs the operation result

### ValidateTotpAsync

Validates a TOTP code for a user and handles lockout logic.

```csharp
public async Task<bool> ValidateTotpAsync(string userId, string code)
```

**Parameters:**
- `userId`: User's unique identifier
- `code`: 6-digit TOTP code to validate

**Returns:** `true` if validation was successful, `false` otherwise

**Behavior:**
- Validates input parameters
- Checks if account is currently locked
- Verifies TOTP is enabled for the user
- Validates the TOTP code using `ITotpService`
- On success: Resets failure count
- On failure: Increments failure count and locks account if threshold reached

### IsAccountLockedAsync

Checks if a user's account is currently locked due to failed TOTP attempts.

```csharp
public async Task<bool> IsAccountLockedAsync(string userId)
```

**Parameters:**
- `userId`: User's unique identifier

**Returns:** `true` if account is locked, `false` otherwise

**Behavior:**
- Finds the user by ID
- Checks if lockout end time exists and is in the future
- Automatically clears expired lockouts

### LockAccountAsync

Locks a user's account for the configured lockout duration.

```csharp
public async Task LockAccountAsync(string userId)
```

**Parameters:**
- `userId`: User's unique identifier

**Behavior:**
- Finds the user by ID
- Sets `TotpLockoutEnd` to current time plus lockout duration
- Updates the user in the database
- Logs the lockout event

### ResetTotpFailuresAsync

Resets TOTP failure count and clears lockout for a user.

```csharp
public async Task ResetTotpFailuresAsync(string userId)
```

**Parameters:**
- `userId`: User's unique identifier

**Behavior:**
- Finds the user by ID
- Resets `TotpFailureCount` to 0
- Clears `LastTotpFailure` and `TotpLockoutEnd`
- Updates the user in the database

### GetRemainingLockoutTimeAsync

Gets the remaining lockout time for a user.

```csharp
public async Task<TimeSpan?> GetRemainingLockoutTimeAsync(string userId)
```

**Parameters:**
- `userId`: User's unique identifier

**Returns:** Remaining lockout time, or `null` if not locked

### GetUserByIdAsync

Gets a user by their ID.

```csharp
public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
```

**Parameters:**
- `userId`: User's unique identifier

**Returns:** `ApplicationUser` if found, `null` otherwise

## Security Features

### Account Lockout Logic

The service implements a progressive lockout system:

1. **Failure Tracking**: Each failed TOTP attempt increments `TotpFailureCount`
2. **Lockout Trigger**: When failures reach `MaxFailureAttempts`, account is locked
3. **Lockout Duration**: Account remains locked for `LockoutDurationMinutes`
4. **Automatic Unlock**: Expired lockouts are automatically cleared
5. **Reset on Success**: Successful validation resets failure count

### Data Model Integration

The service works with the following `ApplicationUser` properties:

```csharp
public class ApplicationUser : IdentityUser
{
    public string? TotpSecret { get; set; }
    public bool IsTotpEnabled { get; set; }
    public DateTime? LastTotpFailure { get; set; }
    public int TotpFailureCount { get; set; }
    public DateTime? TotpLockoutEnd { get; set; }
}
```

## Error Handling

The service includes comprehensive error handling:

- **Input Validation**: Checks for null/empty parameters
- **User Existence**: Verifies user exists before operations
- **Exception Handling**: Catches and logs exceptions
- **Graceful Degradation**: Returns appropriate values on errors
- **Security Logging**: Logs security-relevant events

## Logging

The service logs the following events:

- **Information**: Successful operations (TOTP setup, validation success)
- **Warning**: Security events (validation failures, lockout attempts)
- **Error**: System errors and exceptions

## Usage Example

```csharp
// Dependency injection setup
services.AddScoped<IUserService, UserService>();

// Usage in a controller or page model
public class SetupTotpModel : PageModel
{
    private readonly IUserService _userService;
    private readonly ITotpService _totpService;

    public SetupTotpModel(IUserService userService, ITotpService totpService)
    {
        _userService = userService;
        _totpService = totpService;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var secret = _totpService.GenerateSecret();
        
        var success = await _userService.SetupTotpAsync(userId, secret);
        
        if (success)
        {
            // Redirect to verification page
            return RedirectToPage("/Account/VerifyTotp");
        }
        
        ModelState.AddModelError("", "Failed to set up TOTP");
        return Page();
    }
}
```

## Testing

The service includes comprehensive unit tests covering:

- **Happy Path**: Successful TOTP setup and validation
- **Error Cases**: Invalid inputs, non-existent users
- **Lockout Scenarios**: Progressive failures and account locking
- **Edge Cases**: Expired lockouts, boundary conditions

Test files:
- `GoogleAuthTotpPrototype.Tests/UserServiceTests.cs`

## Related Components

- **ITotpService**: TOTP generation and validation
- **ApplicationUser**: User model with TOTP properties
- **ApplicationDbContext**: Database context
- **UserManager<ApplicationUser>**: ASP.NET Core Identity user management

## Requirements Satisfied

This implementation satisfies the following requirements:

- **Requirement 2.4**: TOTP setup and verification functionality
- **Requirement 3.6**: Account lockout after failed attempts
- **Requirement 3.7**: Temporary account locking (5 minutes after 3 failures)
# TOTP Setup Page Documentation

## Overview

The TOTP Setup page (`/Totp/Setup`) allows authenticated users to configure two-factor authentication using Time-based One-Time Passwords (TOTP). This page is part of the user registration flow and provides a secure way to enable 2FA using authenticator apps like Google Authenticator, Microsoft Authenticator, or Authy.

## Page Location

- **URL**: `/Totp/Setup`
- **Controller**: `TotpController`
- **Action**: `Setup` (GET), `Enable` (POST)
- **View**: `Views/Totp/Setup.cshtml`
- **Authorization**: Requires authenticated user

## Features

### 1. QR Code Generation
- Displays a QR code that can be scanned by authenticator apps
- QR code contains the TOTP secret and account information
- Generated as a base64-encoded PNG image for immediate display

### 2. Manual Entry Option
- Provides the TOTP secret key for manual entry
- Useful when QR code scanning is not available
- Secret is displayed in Base32 format as required by TOTP standard

### 3. Code Verification
- Allows users to verify their authenticator app setup
- Requires entering a 6-digit TOTP code
- Validates the code before enabling 2FA

### 4. User-Friendly Interface
- Step-by-step instructions for setup process
- Lists compatible authenticator apps
- Responsive design that works on mobile and desktop

## Technical Implementation

### Backend Components

#### TotpController
```csharp
[HttpGet]
public async Task<IActionResult> Setup()
{
    var userId = await _authenticationService.GetCurrentUserIdAsync();
    var request = new VMPARAMTotpSetupRequest { UserId = userId };
    var result = await _totpManagementService.SetupTotpAsync(request);
    return View(result);
}

[HttpPost]
public async Task<IActionResult> Enable(VMPARAMTotpVerifyRequest model)
{
    var result = await _totpManagementService.EnableTotpAsync(model);
    if (result.IsSuccess && result.IsValid)
    {
        return Redirect(result.RedirectUrl ?? "/");
    }
    // Handle errors and reload setup view
}
```

#### TotpManagementService
- Generates TOTP secrets using cryptographically secure random generation
- Creates QR code URIs following the `otpauth://` standard
- Generates base64-encoded QR code images using QRCoder library
- Validates TOTP codes with time window tolerance
- Manages user TOTP state in the database

#### TotpService
- `GenerateSecret()`: Creates a 20-byte random secret encoded in Base32
- `GenerateQrCodeUri()`: Creates standard TOTP URI for QR codes
- `GenerateQrCodeBase64()`: Generates PNG QR code as base64 string
- `ValidateTotp()`: Validates 6-digit codes with ±1 time step tolerance

### Frontend Components

#### View Model
```csharp
public class VMPARAMTotpSetupResponse
{
    public bool IsSuccess { get; set; }
    public string Secret { get; set; }
    public string QrCodeUrl { get; set; }
    public string QrCodeBase64 { get; set; }
    public string ManualEntryKey { get; set; }
    public List<string> Errors { get; set; }
}
```

#### JavaScript Features
- Auto-formats TOTP code input (digits only, 6-character limit)
- Auto-submits form when 6 digits are entered
- Input validation and user experience enhancements

## User Flow

### 1. Initial Setup
1. User completes registration
2. System redirects to `/Totp/Setup`
3. Page generates new TOTP secret and QR code
4. Secret is stored in database but TOTP is not yet enabled

### 2. Authenticator App Configuration
1. User installs compatible authenticator app
2. User scans QR code or manually enters the secret key
3. Authenticator app begins generating 6-digit codes

### 3. Verification and Enablement
1. User enters current 6-digit code from authenticator app
2. System validates the code against the stored secret
3. If valid, TOTP is enabled for the user account
4. User is redirected to the main application

## Security Features

### Secret Generation
- Uses cryptographically secure random number generation
- 20-byte secrets provide 160 bits of entropy
- Base32 encoding ensures compatibility with all TOTP apps

### Code Validation
- Implements RFC 6238 TOTP standard
- Allows ±1 time step (30 seconds) for clock drift tolerance
- Prevents replay attacks through time-based validation

### Database Security
- TOTP secrets are stored securely in the database
- Secrets are only enabled after successful verification
- Failed attempts are tracked for security monitoring

## Error Handling

### Common Error Scenarios
1. **Invalid TOTP Code**: User enters incorrect 6-digit code
2. **Empty Code**: User submits form without entering code
3. **User Not Found**: Authentication issues or session problems
4. **QR Code Generation Failure**: Technical issues with QR code creation

### Error Display
- Validation errors are displayed using ASP.NET Core model validation
- User-friendly error messages guide users to resolution
- Technical errors are logged for debugging

## Testing

### Integration Tests
The page includes comprehensive integration tests covering:

1. **Authentication Requirements**
   - Unauthenticated users are redirected to login
   - Authenticated users can access the setup page

2. **QR Code Generation**
   - QR codes are generated and displayed correctly
   - Manual entry keys are provided as fallback

3. **Code Verification**
   - Valid TOTP codes enable 2FA successfully
   - Invalid codes display appropriate errors
   - Empty codes trigger validation errors

4. **Security Features**
   - Multiple setup requests generate new secrets
   - TOTP is only enabled after successful verification

### Test Coverage
- Unit tests for TOTP service methods
- Integration tests for complete setup workflow
- Error handling and edge case testing

## Configuration

### Required Settings
```json
{
  "Totp": {
    "Issuer": "GoogleAuthTotpPrototype",
    "LockoutDurationMinutes": 5,
    "MaxFailureAttempts": 3
  }
}
```

### Dependencies
- **OtpNet**: TOTP generation and validation
- **QRCoder**: QR code image generation
- **ASP.NET Core Identity**: User management
- **Entity Framework Core**: Data persistence

## Best Practices

### User Experience
1. Provide clear, step-by-step instructions
2. Support both QR code and manual entry methods
3. Auto-format and validate user input
4. Display helpful error messages

### Security
1. Generate new secrets for each setup attempt
2. Validate codes with appropriate time windows
3. Log security events for monitoring
4. Store secrets securely in the database

### Performance
1. Generate QR codes efficiently using optimized libraries
2. Cache QR code images when appropriate
3. Minimize database queries during setup process

## Future Enhancements

### Potential Improvements
1. **Recovery Codes**: Generate backup codes for account recovery
2. **Multiple Devices**: Support for multiple TOTP devices
3. **QR Code Customization**: Branded QR codes with logos
4. **Setup Analytics**: Track setup completion rates
5. **Progressive Enhancement**: Improved mobile experience

### Integration Opportunities
1. **SMS Backup**: SMS codes as TOTP fallback
2. **Hardware Tokens**: Support for FIDO2/WebAuthn
3. **Enterprise Integration**: SAML/OIDC compatibility
4. **Audit Logging**: Enhanced security event tracking
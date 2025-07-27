---
layout: default
title: TOTP Service
---

# TOTP Service Documentation

## Overview

The TOTP (Time-based One-Time Password) service provides functionality for generating, validating, and managing TOTP codes for two-factor authentication. This service implements the RFC 6238 standard for TOTP generation and validation.

## Architecture

The TOTP service consists of two main components:

- **ITotpService**: Interface defining the contract for TOTP operations
- **TotpService**: Concrete implementation using OtpNet and QRCoder libraries

## Interface: ITotpService

Located in `Services/ITotpService.cs`

### Methods

#### GenerateSecret()
```csharp
string GenerateSecret()
```
- **Purpose**: Generates a new cryptographically secure Base32-encoded secret for TOTP
- **Returns**: Base32-encoded secret string (20 bytes / 160 bits)
- **Usage**: Called during user registration or TOTP setup

#### GenerateQrCodeUri(string email, string secret)
```csharp
string GenerateQrCodeUri(string email, string secret)
```
- **Purpose**: Generates a QR code URI compatible with Google Authenticator and other TOTP apps
- **Parameters**:
  - `email`: User's email address
  - `secret`: Base32-encoded TOTP secret
- **Returns**: QR code URI string in format: `otpauth://totp/Issuer:email?secret=SECRET&issuer=ISSUER`
- **Throws**: `ArgumentException` if email or secret is null/empty

#### ValidateTotp(string secret, string code)
```csharp
bool ValidateTotp(string secret, string code)
```
- **Purpose**: Validates a TOTP code against the provided secret with time window tolerance
- **Parameters**:
  - `secret`: Base32-encoded TOTP secret
  - `code`: 6-digit TOTP code to validate
- **Returns**: `true` if the code is valid, `false` otherwise
- **Time Window**: ±30 seconds tolerance (±1 step)
- **Throws**: `ArgumentException` if secret is null/empty

#### GenerateQrCode(string qrCodeUri)
```csharp
byte[] GenerateQrCode(string qrCodeUri)
```
- **Purpose**: Generates a QR code image as PNG byte array
- **Parameters**:
  - `qrCodeUri`: QR code URI to encode
- **Returns**: PNG image as byte array
- **Throws**: 
  - `ArgumentException` if URI is null/empty
  - `InvalidOperationException` if QR code generation fails

## Implementation: TotpService

Located in `Services/TotpService.cs`

### Dependencies

- **OtpNet**: For TOTP generation and validation
- **QRCoder**: For QR code image generation
- **IConfiguration**: For accessing application configuration

### Configuration

The service reads configuration from `appsettings.json`:

```json
{
  "Totp": {
    "Issuer": "Google Auth Prototype"
  }
}
```

### Key Features

#### Cryptographic Security
- Uses `RandomNumberGenerator` for secure random secret generation
- Generates 160-bit (20-byte) secrets as recommended by RFC 6238
- Base32 encoding for compatibility with authenticator apps

#### Time Window Tolerance
- Implements ±30 second tolerance for TOTP validation
- Accounts for clock drift between client and server
- Uses standard 30-second time steps

#### QR Code Generation
- Generates PNG images with 10 pixels per module
- Uses Error Correction Level Q for optimal scanning
- Compatible with Google Authenticator, Authy, and other TOTP apps

## Usage Examples

### Basic TOTP Setup Flow

```csharp
// Inject the service
private readonly ITotpService _totpService;

// Generate a new secret for user
var secret = _totpService.GenerateSecret();

// Generate QR code URI for user to scan
var qrUri = _totpService.GenerateQrCodeUri(user.Email, secret);

// Generate QR code image
var qrCodeImage = _totpService.GenerateQrCode(qrUri);

// Validate user's TOTP code during setup
var isValid = _totpService.ValidateTotp(secret, userEnteredCode);
```

### TOTP Validation During Login

```csharp
// Validate TOTP code during authentication
var isValid = _totpService.ValidateTotp(user.TotpSecret, userEnteredCode);
if (isValid)
{
    // Proceed with authentication
}
else
{
    // Handle invalid code
}
```

## Testing

The TOTP service includes comprehensive unit tests in `GoogleAuthTotpPrototype.Tests/TotpServiceTests.cs`:

### Test Coverage

- **Secret Generation**: Validates Base32 format and uniqueness
- **QR Code URI Generation**: Tests proper URI formatting and encoding
- **TOTP Validation**: Tests valid/invalid codes and time window tolerance
- **QR Code Image Generation**: Validates PNG format and structure
- **Error Handling**: Tests exception scenarios and edge cases
- **Configuration**: Tests default and custom issuer settings

### Running Tests

```bash
dotnet test GoogleAuthTotpPrototype.Tests --filter "TotpServiceTests"
```

## Security Considerations

### Secret Storage
- TOTP secrets should be encrypted when stored in the database
- Secrets are generated using cryptographically secure random number generation
- Base32 encoding ensures compatibility with standard authenticator apps

### Time Synchronization
- Server time should be synchronized with NTP
- Time window tolerance accounts for minor clock drift
- Consider implementing time drift detection and adjustment

### Rate Limiting
- Implement rate limiting for TOTP validation attempts
- Consider account lockout after multiple failed attempts
- Log failed validation attempts for security monitoring

## Dependencies

### NuGet Packages

```xml
<PackageReference Include="OtpNet" Version="1.3.0" />
<PackageReference Include="QRCoder" Version="1.4.3" />
```

### System Requirements

- .NET 8.0 or later
- System.Security.Cryptography for secure random generation

## Error Handling

The service implements comprehensive error handling:

- **ArgumentException**: For null/empty parameters
- **InvalidOperationException**: For QR code generation failures
- **Graceful degradation**: Invalid secrets return false rather than throwing exceptions

## Performance Considerations

- TOTP validation is computationally lightweight
- QR code generation may be cached for repeated requests
- Consider async variants for I/O-bound operations in future versions

## Future Enhancements

- Support for different TOTP algorithms (SHA-256, SHA-512)
- Configurable time step intervals
- Backup code generation and validation
- HOTP (counter-based) support
- Async method variants
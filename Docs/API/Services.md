---
layout: default
title: API Reference - Services
---

# Service API Reference

*This document will be populated as services are implemented.*

## TOTP Service API

### ITotpService

The TOTP Service provides functionality for Time-based One-Time Password operations.

#### Methods

- `string GenerateSecret()` - Generates a cryptographically secure TOTP secret
- `string GenerateQrCodeUri(string email, string secret)` - Creates QR code URI for authenticator apps
- `bool ValidateTotp(string secret, string code)` - Validates a TOTP code against a secret
- `byte[] GenerateQrCode(string qrCodeUri)` - Generates QR code image as byte array

*Detailed API documentation will be added as services are implemented.*

## User Service API

### IUserService

*API documentation will be added when the User Service is implemented.*

## Authentication Service API

*API documentation will be added when authentication services are implemented.*
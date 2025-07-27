---
layout: default
title: Testing Strategy
---

# Testing Strategy

## Overview

The testing strategy focuses on comprehensive coverage of authentication flows, service functionality, and user interactions.

## Testing Levels

### Unit Tests

#### TOTP Service Tests
- Secret generation validation
- QR code URI format verification
- TOTP code validation with time windows
- QR code image generation

#### User Service Tests
- TOTP setup workflow
- Account lockout logic
- Failure count management
- Lockout expiration handling

#### Authentication Logic Tests
- Google OAuth integration points
- Session state management
- Authorization requirements

### Integration Tests

#### Authentication Flow Tests
- Complete registration workflow
- Login with Google + TOTP verification
- TOTP setup process
- Account lockout scenarios

#### Page Tests
- Razor page rendering
- Form submission handling
- Redirect behavior
- Authorization enforcement

### Manual Testing Scenarios

#### Happy Path Testing
- New user registration and TOTP setup
- Successful login with both authentication factors
- Dashboard access and logout

#### Error Scenario Testing
- Invalid TOTP codes
- Account lockout and recovery
- Google OAuth failures
- Session expiration handling

## Test Coverage Goals

- **Unit Tests**: 90%+ coverage for service classes
- **Integration Tests**: All authentication flows covered
- **End-to-End Tests**: Complete user workflows tested

## Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test GoogleAuthTotpPrototype.Tests
```

*Detailed test implementation will be documented as tests are written.*
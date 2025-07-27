---
layout: default
title: User Service
---

# User Service

*This document will be populated when the User Service is implemented.*

## Overview

The User Service manages user-related TOTP operations including setup, validation, and account lockout logic.

## Interface

```csharp
public interface IUserService
{
    Task<bool> SetupTotpAsync(string userId, string secret);
    Task<bool> ValidateTotpAsync(string userId, string code);
    Task<bool> IsAccountLockedAsync(string userId);
    Task LockAccountAsync(string userId);
    Task ResetTotpFailuresAsync(string userId);
}
```

## Implementation

*Implementation details will be documented when the service is created.*

## Usage Examples

*Usage examples will be provided when the service is implemented.*

## Testing

*Test coverage and examples will be documented when tests are written.*
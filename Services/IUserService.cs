using GoogleAuthTotpPrototype.Models;

namespace GoogleAuthTotpPrototype.Services;

/// <summary>
/// Interface for user-related TOTP operations and account management
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Sets up TOTP for a user by storing the secret and enabling TOTP
    /// </summary>
    /// <param name="userId">User's unique identifier</param>
    /// <param name="secret">Base32-encoded TOTP secret</param>
    /// <returns>True if setup was successful, false otherwise</returns>
    Task<bool> SetupTotpAsync(string userId, string secret);

    /// <summary>
    /// Validates a TOTP code for a user and handles lockout logic
    /// </summary>
    /// <param name="userId">User's unique identifier</param>
    /// <param name="code">6-digit TOTP code to validate</param>
    /// <returns>True if validation was successful, false otherwise</returns>
    Task<bool> ValidateTotpAsync(string userId, string code);

    /// <summary>
    /// Checks if a user's account is currently locked due to failed TOTP attempts
    /// </summary>
    /// <param name="userId">User's unique identifier</param>
    /// <returns>True if account is locked, false otherwise</returns>
    Task<bool> IsAccountLockedAsync(string userId);

    /// <summary>
    /// Locks a user's account for the configured lockout duration
    /// </summary>
    /// <param name="userId">User's unique identifier</param>
    Task LockAccountAsync(string userId);

    /// <summary>
    /// Resets TOTP failure count and clears lockout for a user
    /// </summary>
    /// <param name="userId">User's unique identifier</param>
    Task ResetTotpFailuresAsync(string userId);

    /// <summary>
    /// Gets the remaining lockout time for a user
    /// </summary>
    /// <param name="userId">User's unique identifier</param>
    /// <returns>Remaining lockout time, or null if not locked</returns>
    Task<TimeSpan?> GetRemainingLockoutTimeAsync(string userId);

    /// <summary>
    /// Gets a user by their ID
    /// </summary>
    /// <param name="userId">User's unique identifier</param>
    /// <returns>ApplicationUser if found, null otherwise</returns>
    Task<ApplicationUser?> GetUserByIdAsync(string userId);
}
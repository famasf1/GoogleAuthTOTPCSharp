using Microsoft.AspNetCore.Identity;
using GoogleAuthTotpPrototype.Models;
using GoogleAuthTotpPrototype.Data;
using Microsoft.EntityFrameworkCore;

namespace GoogleAuthTotpPrototype.Services;

/// <summary>
/// Implementation of user service for TOTP management and account lockout logic
/// </summary>
public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly ITotpService _totpService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserService> _logger;

    private readonly int _maxFailureAttempts;
    private readonly TimeSpan _lockoutDuration;

    public UserService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        ITotpService totpService,
        IConfiguration configuration,
        ILogger<UserService> logger)
    {
        _userManager = userManager;
        _context = context;
        _totpService = totpService;
        _configuration = configuration;
        _logger = logger;

        // Load configuration values with defaults
        _maxFailureAttempts = _configuration.GetValue<int>("Totp:MaxFailureAttempts", 3);
        _lockoutDuration = TimeSpan.FromMinutes(_configuration.GetValue<int>("Totp:LockoutDurationMinutes", 5));
    }

    /// <summary>
    /// Sets up TOTP for a user by storing the secret and enabling TOTP
    /// </summary>
    public async Task<bool> SetupTotpAsync(string userId, string secret)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("SetupTotpAsync called with null or empty userId");
            return false;
        }

        if (string.IsNullOrWhiteSpace(secret))
        {
            _logger.LogWarning("SetupTotpAsync called with null or empty secret for user {UserId}", userId);
            return false;
        }

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for TOTP setup: {UserId}", userId);
                return false;
            }

            // Store the TOTP secret and enable TOTP
            user.TotpSecret = secret;
            user.IsTotpEnabled = true;
            
            // Reset any existing failure counts and lockout
            user.TotpFailureCount = 0;
            user.LastTotpFailure = null;
            user.TotpLockoutEnd = null;

            var result = await _userManager.UpdateAsync(user);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("TOTP setup completed successfully for user {UserId}", userId);
                return true;
            }
            else
            {
                _logger.LogError("Failed to update user during TOTP setup for user {UserId}: {Errors}", 
                    userId, string.Join(", ", result.Errors.Select(e => e.Description)));
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during TOTP setup for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Validates a TOTP code for a user and handles lockout logic
    /// </summary>
    public async Task<bool> ValidateTotpAsync(string userId, string code)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("ValidateTotpAsync called with null or empty userId");
            return false;
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            _logger.LogWarning("ValidateTotpAsync called with null or empty code for user {UserId}", userId);
            return false;
        }

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for TOTP validation: {UserId}", userId);
                return false;
            }

            // Check if account is currently locked
            if (await IsAccountLockedAsync(userId))
            {
                _logger.LogWarning("TOTP validation attempted for locked account: {UserId}", userId);
                return false;
            }

            // Check if TOTP is enabled and secret exists
            if (!user.IsTotpEnabled || string.IsNullOrWhiteSpace(user.TotpSecret))
            {
                _logger.LogWarning("TOTP validation attempted for user without TOTP enabled: {UserId}", userId);
                return false;
            }

            // Validate the TOTP code
            bool isValid = _totpService.ValidateTotp(user.TotpSecret, code);

            if (isValid)
            {
                // Reset failure count on successful validation
                await ResetTotpFailuresAsync(userId);
                _logger.LogInformation("TOTP validation successful for user {UserId}", userId);
                return true;
            }
            else
            {
                // Increment failure count and check for lockout
                await IncrementTotpFailureAsync(userId);
                _logger.LogWarning("TOTP validation failed for user {UserId}", userId);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred during TOTP validation for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Checks if a user's account is currently locked due to failed TOTP attempts
    /// </summary>
    public async Task<bool> IsAccountLockedAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return false;

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            // Check if lockout end time exists and is in the future
            if (user.TotpLockoutEnd.HasValue && user.TotpLockoutEnd.Value > DateTime.UtcNow)
            {
                return true;
            }

            // If lockout time has passed, clear the lockout
            if (user.TotpLockoutEnd.HasValue && user.TotpLockoutEnd.Value <= DateTime.UtcNow)
            {
                await ClearExpiredLockoutAsync(userId);
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred checking account lock status for user {UserId}", userId);
            return false;
        }
    }

    /// <summary>
    /// Locks a user's account for the configured lockout duration
    /// </summary>
    public async Task LockAccountAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for account locking: {UserId}", userId);
                return;
            }

            user.TotpLockoutEnd = DateTime.UtcNow.Add(_lockoutDuration);
            
            var result = await _userManager.UpdateAsync(user);
            
            if (result.Succeeded)
            {
                _logger.LogWarning("Account locked for user {UserId} until {LockoutEnd}", userId, user.TotpLockoutEnd);
            }
            else
            {
                _logger.LogError("Failed to lock account for user {UserId}: {Errors}", 
                    userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred locking account for user {UserId}", userId);
        }
    }

    /// <summary>
    /// Resets TOTP failure count and clears lockout for a user
    /// </summary>
    public async Task ResetTotpFailuresAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found for resetting TOTP failures: {UserId}", userId);
                return;
            }

            user.TotpFailureCount = 0;
            user.LastTotpFailure = null;
            user.TotpLockoutEnd = null;

            var result = await _userManager.UpdateAsync(user);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("TOTP failures reset for user {UserId}", userId);
            }
            else
            {
                _logger.LogError("Failed to reset TOTP failures for user {UserId}: {Errors}", 
                    userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred resetting TOTP failures for user {UserId}", userId);
        }
    }

    /// <summary>
    /// Gets the remaining lockout time for a user
    /// </summary>
    public async Task<TimeSpan?> GetRemainingLockoutTimeAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.TotpLockoutEnd.HasValue)
                return null;

            var remaining = user.TotpLockoutEnd.Value - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred getting remaining lockout time for user {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    /// Gets a user by their ID
    /// </summary>
    public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        try
        {
            return await _userManager.FindByIdAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred getting user by ID: {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    /// Increments TOTP failure count and locks account if threshold is reached
    /// </summary>
    private async Task IncrementTotpFailureAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return;

            user.TotpFailureCount++;
            user.LastTotpFailure = DateTime.UtcNow;

            // Check if we've reached the maximum failure attempts
            if (user.TotpFailureCount >= _maxFailureAttempts)
            {
                user.TotpLockoutEnd = DateTime.UtcNow.Add(_lockoutDuration);
                _logger.LogWarning("Account locked due to {FailureCount} failed TOTP attempts for user {UserId}", 
                    user.TotpFailureCount, userId);
            }

            await _userManager.UpdateAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred incrementing TOTP failure for user {UserId}", userId);
        }
    }

    /// <summary>
    /// Clears expired lockout for a user
    /// </summary>
    private async Task ClearExpiredLockoutAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return;

            user.TotpLockoutEnd = null;
            await _userManager.UpdateAsync(user);
            
            _logger.LogInformation("Expired lockout cleared for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred clearing expired lockout for user {UserId}", userId);
        }
    }
}
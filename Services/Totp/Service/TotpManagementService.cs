using Microsoft.AspNetCore.Identity;
using GoogleAuthTotpPrototype.Models;
using GoogleAuthTotpPrototype.Services.Totp.ViewModel;

namespace GoogleAuthTotpPrototype.Services;

public class TotpManagementService : ITotpManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITotpService _totpService;
    private readonly ILogger<TotpManagementService> _logger;

    public TotpManagementService(
        UserManager<ApplicationUser> userManager,
        ITotpService totpService,
        ILogger<TotpManagementService> logger)
    {
        _userManager = userManager;
        _totpService = totpService;
        _logger = logger;
    }

    public async Task<VMPARAMTotpSetupResponse> SetupTotpAsync(VMPARAMTotpSetupRequest request)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
            {
                return new VMPARAMTotpSetupResponse
                {
                    IsSuccess = false,
                    Errors = new List<string> { "User not found." }
                };
            }

            // Generate new secret
            var secret = _totpService.GenerateSecret();
            
            // Generate QR code URI
            var qrCodeUri = _totpService.GenerateQrCodeUri(user.Email!, secret);
            
            // Store the secret temporarily (not enabled yet)
            user.TotpSecret = secret;
            user.IsTotpEnabled = false;
            
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return new VMPARAMTotpSetupResponse
                {
                    IsSuccess = false,
                    Errors = result.Errors.Select(e => e.Description).ToList()
                };
            }

            _logger.LogInformation("TOTP setup initiated for user {UserId}", user.Id);

            return new VMPARAMTotpSetupResponse
            {
                IsSuccess = true,
                Secret = secret,
                QrCodeUrl = qrCodeUri,
                ManualEntryKey = secret
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up TOTP for user {UserId}", request.UserId);
            return new VMPARAMTotpSetupResponse
            {
                IsSuccess = false,
                Errors = new List<string> { "An error occurred during TOTP setup." }
            };
        }
    }

    public async Task<VMPARAMTotpVerifyResponse> VerifyTotpAsync(VMPARAMTotpVerifyRequest request)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
            {
                return new VMPARAMTotpVerifyResponse
                {
                    IsSuccess = false,
                    Errors = new List<string> { "User not found." }
                };
            }

            if (string.IsNullOrEmpty(user.TotpSecret))
            {
                return new VMPARAMTotpVerifyResponse
                {
                    IsSuccess = false,
                    Errors = new List<string> { "TOTP is not set up for this user." }
                };
            }

            // Check if user is locked out
            if (user.TotpLockoutEnd.HasValue && user.TotpLockoutEnd > DateTime.UtcNow)
            {
                return new VMPARAMTotpVerifyResponse
                {
                    IsSuccess = false,
                    IsLockedOut = true,
                    Errors = new List<string> { "Account is temporarily locked due to too many failed TOTP attempts." }
                };
            }

            // Validate TOTP code
            var isValid = _totpService.ValidateTotp(user.TotpSecret, request.Code);
            
            if (isValid)
            {
                // Reset failure count on successful verification
                user.TotpFailureCount = 0;
                user.LastTotpFailure = null;
                user.TotpLockoutEnd = null;
                
                await _userManager.UpdateAsync(user);
                
                _logger.LogInformation("TOTP verification successful for user {UserId}", user.Id);
                
                return new VMPARAMTotpVerifyResponse
                {
                    IsSuccess = true,
                    IsValid = true,
                    RedirectUrl = "/"
                };
            }
            else
            {
                // Handle failed attempt
                user.TotpFailureCount++;
                user.LastTotpFailure = DateTime.UtcNow;
                
                // Lock out after 5 failed attempts for 15 minutes
                if (user.TotpFailureCount >= 5)
                {
                    user.TotpLockoutEnd = DateTime.UtcNow.AddMinutes(15);
                    _logger.LogWarning("User {UserId} locked out due to too many TOTP failures", user.Id);
                }
                
                await _userManager.UpdateAsync(user);
                
                return new VMPARAMTotpVerifyResponse
                {
                    IsSuccess = false,
                    IsValid = false,
                    IsLockedOut = user.TotpLockoutEnd.HasValue && user.TotpLockoutEnd > DateTime.UtcNow,
                    Errors = new List<string> { "Invalid TOTP code." }
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying TOTP for user {UserId}", request.UserId);
            return new VMPARAMTotpVerifyResponse
            {
                IsSuccess = false,
                Errors = new List<string> { "An error occurred during TOTP verification." }
            };
        }
    }

    public async Task<VMPARAMTotpVerifyResponse> EnableTotpAsync(VMPARAMTotpVerifyRequest request)
    {
        var verifyResult = await VerifyTotpAsync(request);
        
        if (verifyResult.IsSuccess && verifyResult.IsValid)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(request.UserId);
                if (user != null)
                {
                    user.IsTotpEnabled = true;
                    await _userManager.UpdateAsync(user);
                    
                    _logger.LogInformation("TOTP enabled for user {UserId}", user.Id);
                    
                    verifyResult.RedirectUrl = "/";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling TOTP for user {UserId}", request.UserId);
                return new VMPARAMTotpVerifyResponse
                {
                    IsSuccess = false,
                    Errors = new List<string> { "An error occurred while enabling TOTP." }
                };
            }
        }
        
        return verifyResult;
    }

    public async Task<bool> DisableTotpAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.IsTotpEnabled = false;
            user.TotpSecret = null;
            user.TotpFailureCount = 0;
            user.LastTotpFailure = null;
            user.TotpLockoutEnd = null;
            
            var result = await _userManager.UpdateAsync(user);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("TOTP disabled for user {UserId}", userId);
            }
            
            return result.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling TOTP for user {UserId}", userId);
            return false;
        }
    }
}
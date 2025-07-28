using Microsoft.AspNetCore.Identity;
using GoogleAuthTotpPrototype.Models;
using GoogleAuthTotpPrototype.Services.Totp.ViewModel;

namespace GoogleAuthTotpPrototype.Services;

public class TotpManagementService : ITotpManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITotpService _totpService;
    private readonly ILogger<TotpManagementService> _logger;

    public TotpManagementService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITotpService totpService,
        ILogger<TotpManagementService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
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
            
            _logger.LogInformation("Generated TOTP secret for user {UserId}. Secret: {Secret}, Length: {Length}", 
                user.Id, secret, secret.Length);
            
            // Generate QR code URI
            var qrCodeUri = _totpService.GenerateQrCodeUri(user.Email!, secret);
            
            _logger.LogInformation("Generated QR code URI for user {UserId}: {QrCodeUri}", user.Id, qrCodeUri);
            
            // Generate QR code base64 image
            var qrCodeBase64 = _totpService.GenerateQrCodeBase64(qrCodeUri);
            
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
                QrCodeBase64 = qrCodeBase64,
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

            // Debug: Generate current code for comparison
            var currentCode = _totpService.GenerateCurrentCode(user.TotpSecret);
            _logger.LogInformation("TOTP verification attempt for user {UserId}. Input code: {InputCode}, Current expected code: {CurrentCode}, Secret length: {SecretLength}", 
                user.Id, request.Code, currentCode, user.TotpSecret?.Length ?? 0);

            // Validate TOTP code
            var isValid = _totpService.ValidateTotp(user.TotpSecret, request.Code);
            
            if (isValid)
            {
                // Reset failure count on successful verification
                user.TotpFailureCount = 0;
                user.LastTotpFailure = null;
                user.TotpLockoutEnd = null;
                
                await _userManager.UpdateAsync(user);
                
                // Complete the sign-in process after successful TOTP verification
                await _signInManager.SignInAsync(user, isPersistent: false);
                
                _logger.LogInformation("TOTP verification successful for user {UserId}, user signed in", user.Id);
                
                return new VMPARAMTotpVerifyResponse
                {
                    IsSuccess = true,
                    IsValid = true,
                    RedirectUrl = "/Home/Success"
                };
            }
            else
            {
                // Handle failed attempt
                user.TotpFailureCount++;
                user.LastTotpFailure = DateTime.UtcNow;
                
                // Lock out after 3 failed attempts for 5 minutes (per requirements)
                if (user.TotpFailureCount >= 3)
                {
                    user.TotpLockoutEnd = DateTime.UtcNow.AddMinutes(5);
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
                    
                    verifyResult.RedirectUrl = "/Home/Success";
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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GoogleAuthTotpPrototype.Services;
using GoogleAuthTotpPrototype.Services.Totp.ViewModel;

namespace GoogleAuthTotpPrototype.Controllers;

[Authorize]
public class TotpController : Controller
{
    private readonly ITotpManagementService _totpManagementService;
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<TotpController> _logger;

    public TotpController(
        ITotpManagementService totpManagementService,
        IAuthenticationService authenticationService,
        ILogger<TotpController> logger)
    {
        _totpManagementService = totpManagementService;
        _authenticationService = authenticationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Setup()
    {
        var userId = await _authenticationService.GetCurrentUserIdAsync();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Authentication");
        }

        var request = new VMPARAMTotpSetupRequest { UserId = userId };
        var result = await _totpManagementService.SetupTotpAsync(request);

        if (!result.IsSuccess)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
        }

        return View(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enable(VMPARAMTotpVerifyRequest model)
    {
        if (!ModelState.IsValid)
        {
            // Reload setup data for the view
            var setupRequest = new VMPARAMTotpSetupRequest { UserId = model.UserId };
            var setupResult = await _totpManagementService.SetupTotpAsync(setupRequest);
            return View("Setup", setupResult);
        }

        var result = await _totpManagementService.EnableTotpAsync(model);

        if (result.IsSuccess && result.IsValid)
        {
            _logger.LogInformation("TOTP successfully enabled for user {UserId}", model.UserId);
            return Redirect(result.RedirectUrl ?? "/");
        }

        // Add errors and reload setup view
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        var reloadRequest = new VMPARAMTotpSetupRequest { UserId = model.UserId };
        var reloadResult = await _totpManagementService.SetupTotpAsync(reloadRequest);
        return View("Setup", reloadResult);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Verify()
    {
        return View(new VMPARAMTotpVerifyRequest());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Verify(VMPARAMTotpVerifyRequest model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _totpManagementService.VerifyTotpAsync(model);

        if (result.IsSuccess && result.IsValid)
        {
            _logger.LogInformation("TOTP verification successful for user {UserId}", model.UserId);
            return Redirect(result.RedirectUrl ?? "/");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Disable()
    {
        var userId = await _authenticationService.GetCurrentUserIdAsync();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Authentication");
        }

        var success = await _totpManagementService.DisableTotpAsync(userId);

        if (success)
        {
            _logger.LogInformation("TOTP disabled for user {UserId}", userId);
            TempData["Message"] = "Two-factor authentication has been disabled.";
        }
        else
        {
            TempData["Error"] = "Failed to disable two-factor authentication.";
        }

        return RedirectToAction("Index", "Home");
    }
}
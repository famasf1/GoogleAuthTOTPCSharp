using Microsoft.AspNetCore.Mvc;
using GoogleAuthTotpPrototype.Services.Authentication.ViewModel;
using GoogleAuthTotpPrototype.Services;

namespace GoogleAuthTotpPrototype.Controllers;

public class AuthenticationController : Controller
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<AuthenticationController> _logger;

    public AuthenticationController(
        IAuthenticationService authenticationService,
        ILogger<AuthenticationController> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new VMPARAMRegisterRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(VMPARAMRegisterRequest model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authenticationService.RegisterAsync(model);
        
        if (result.IsSuccess)
        {
            _logger.LogInformation("User {Username} registered successfully", result.Username);
            return Redirect(result.RedirectUrl ?? returnUrl ?? "/");
        }

        // Add errors to ModelState
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new VMPARAMLoginRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(VMPARAMLoginRequest model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authenticationService.LoginAsync(model);
        
        // No Two-Factor setup
        if (!result.RequiresTwoFactor) 
        {
            _logger.LogInformation("User {Username} logged in successfully. But TOTP setup failed. Redirect to TOTP Setup", result.Username);
            return Redirect(result.RedirectUrl ?? returnUrl ?? "/");
        }

        if (result.IsSuccess && result.RequiresTwoFactor)
        {
            _logger.LogInformation("User {Username} requires TOTP verification", result.Username);
            // Store the user ID in TempData for TOTP verification
            TempData["PendingTotpUserId"] = result.UserId;
            TempData["PendingTotpUsername"] = result.Username;
            return Redirect(result.RedirectUrl ?? returnUrl ?? "/");
        } 

        // Add errors to ModelState
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _authenticationService.SignOutAsync();
        _logger.LogInformation("User logged out");
        return RedirectToAction("Index", "Home");
    }
}
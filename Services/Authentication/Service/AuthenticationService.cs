using AutoMapper;
using Microsoft.AspNetCore.Identity;
using GoogleAuthTotpPrototype.Models;
using GoogleAuthTotpPrototype.Services.Authentication.ViewModel;

namespace GoogleAuthTotpPrototype.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IMapper mapper,
        ILogger<AuthenticationService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<VMPARAMRegisterResponse> RegisterAsync(VMPARAMRegisterRequest request)
    {
        try
        {
            // Map request to ApplicationUser
            var user = _mapper.Map<ApplicationUser>(request);
            
            // Create user
            var result = await _userManager.CreateAsync(user, request.Password);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("User {Username} created successfully", user.UserName);
                
                // Sign in the user
                await _signInManager.SignInAsync(user, isPersistent: false);
                
                // Map to response
                var response = _mapper.Map<VMPARAMRegisterResponse>(user);
                response.RedirectUrl = "/Totp/Setup";
                
                return response;
            }
            else
            {
                _logger.LogWarning("Failed to create user {Username}. Errors: {Errors}", 
                    request.Username, string.Join(", ", result.Errors.Select(e => e.Description)));
                
                return new VMPARAMRegisterResponse
                {
                    IsSuccess = false,
                    Errors = result.Errors.Select(e => e.Description).ToList()
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during user registration for {Username}", request.Username);
            
            return new VMPARAMRegisterResponse
            {
                IsSuccess = false,
                Errors = new List<string> { "An error occurred during registration. Please try again." }
            };
        }
    }

    public async Task<VMPARAMLoginResponse> LoginAsync(VMPARAMLoginRequest request)
    {
        try
        {
            // Find user by email
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return new VMPARAMLoginResponse
                {
                    IsSuccess = false,
                    Errors = new List<string> { "Invalid email or password." }
                };
            }

            // Attempt sign in
            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!, 
                request.Password, 
                request.RememberMe, 
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Username} logged in successfully", user.UserName);
                
                var response = _mapper.Map<VMPARAMLoginResponse>(user);
                response.RedirectUrl = user.IsTotpEnabled ? "/Totp/Verify" : "/";
                
                return response;
            }
            else if (result.RequiresTwoFactor)
            {
                _logger.LogInformation("User {Username} requires two-factor authentication", user.UserName);
                
                return new VMPARAMLoginResponse
                {
                    IsSuccess = true,
                    RequiresTwoFactor = true,
                    UserId = user.Id,
                    Username = user.UserName!,
                    RedirectUrl = "/Totp/Verify"
                };
            }
            else if (result.IsLockedOut)
            {
                _logger.LogWarning("User {Username} account locked out", user.UserName);
                
                return new VMPARAMLoginResponse
                {
                    IsSuccess = false,
                    Errors = new List<string> { "Account locked out. Please try again later." }
                };
            }
            else
            {
                _logger.LogWarning("Failed login attempt for {Username}", user.UserName);
                
                return new VMPARAMLoginResponse
                {
                    IsSuccess = false,
                    Errors = new List<string> { "Invalid email or password." }
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during login for {Email}", request.Email);
            
            return new VMPARAMLoginResponse
            {
                IsSuccess = false,
                Errors = new List<string> { "An error occurred during login. Please try again." }
            };
        }
    }

    public async Task SignOutAsync()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User signed out");
    }

    public bool IsUserSignedInAsync()
    {
        return _signInManager.IsSignedIn(_signInManager.Context.User);
    }

    public async Task<string?> GetCurrentUserIdAsync()
    {
        if (_signInManager.IsSignedIn(_signInManager.Context.User))
        {
            var user = await _userManager.GetUserAsync(_signInManager.Context.User);
            return user?.Id;
        }
        return null;
    }
}
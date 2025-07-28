using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using GoogleAuthTotpPrototype.Data;
using GoogleAuthTotpPrototype.Models;
using Microsoft.AspNetCore.Identity;
using Xunit;
using GoogleAuthTotpPrototype.Services;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace GoogleAuthTotpPrototype.Tests.Integration;

public class TotpVerificationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TotpVerificationIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid().ToString());
                });
            });
        });
        
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task TotpVerify_Get_WithoutPendingUserId_RedirectsToLogin()
    {
        // Act
        var response = await _client.GetAsync("/Totp/Verify");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Authentication/Login", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task TotpVerify_Get_WithPendingUserId_ReturnsVerificationPage()
    {
        // Arrange - Create user and simulate login flow
        var user = await CreateUserWithTotpEnabledAsync();
        await SimulateLoginFlowAsync(user);

        // Act
        var response = await _client.GetAsync("/Totp/Verify");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Two-Factor Authentication", content);
        Assert.Contains("Enter the 6-digit code", content);
        Assert.Contains("Verification Code", content);
        Assert.Contains($"value=\"{user.Id}\"", content); // Hidden UserId field
    }

    [Fact]
    public async Task TotpVerify_Post_ValidCode_SignsInUserAndRedirects()
    {
        // Arrange - Create user with TOTP enabled and simulate login flow
        var user = await CreateUserWithTotpEnabledAsync();
        await SimulateLoginFlowAsync(user);
        
        // Generate valid TOTP code
        using var scope = _factory.Services.CreateScope();
        var totpService = scope.ServiceProvider.GetRequiredService<ITotpService>();
        var validCode = GenerateValidTotpCode(totpService, user.TotpSecret!);
        
        var formData = new Dictionary<string, string>
        {
            {"UserId", user.Id},
            {"Code", validCode}
        };

        // Get anti-forgery token
        var verifyResponse = await _client.GetAsync("/Totp/Verify");
        var verifyContent = await verifyResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(verifyContent);
        formData.Add("__RequestVerificationToken", token);

        var formContent = new FormUrlEncodedContent(formData);

        // Act
        var response = await _client.PostAsync("/Totp/Verify", formContent);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.ToString());
        
        // Verify user failure count is reset
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var updatedUser = await userManager.FindByIdAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal(0, updatedUser.TotpFailureCount);
        Assert.Null(updatedUser.LastTotpFailure);
        Assert.Null(updatedUser.TotpLockoutEnd);
    }

    [Fact]
    public async Task TotpVerify_Post_InvalidCode_ReturnsViewWithError()
    {
        // Arrange - Create user with TOTP enabled and simulate login flow
        var user = await CreateUserWithTotpEnabledAsync();
        await SimulateLoginFlowAsync(user);
        
        var formData = new Dictionary<string, string>
        {
            {"UserId", user.Id},
            {"Code", "123456"} // Invalid code
        };

        // Get anti-forgery token
        var verifyResponse = await _client.GetAsync("/Totp/Verify");
        var verifyContent = await verifyResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(verifyContent);
        formData.Add("__RequestVerificationToken", token);

        var formContent = new FormUrlEncodedContent(formData);

        // Act
        var response = await _client.PostAsync("/Totp/Verify", formContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Two-Factor Authentication", content);
        Assert.Contains("Invalid TOTP code", content);
        
        // Verify failure count is incremented
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var updatedUser = await userManager.FindByIdAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal(1, updatedUser.TotpFailureCount);
        Assert.NotNull(updatedUser.LastTotpFailure);
    }

    [Fact]
    public async Task TotpVerify_Post_ThreeFailedAttempts_LocksAccount()
    {
        // Arrange - Create user with TOTP enabled and simulate login flow
        var user = await CreateUserWithTotpEnabledAsync();
        await SimulateLoginFlowAsync(user);
        
        // Make 3 failed attempts
        for (int i = 0; i < 3; i++)
        {
            var formData = new Dictionary<string, string>
            {
                {"UserId", user.Id},
                {"Code", "123456"} // Invalid code
            };

            // Get anti-forgery token
            var verifyResponse = await _client.GetAsync("/Totp/Verify");
            var verifyContent = await verifyResponse.Content.ReadAsStringAsync();
            var token = ExtractAntiForgeryToken(verifyContent);
            formData.Add("__RequestVerificationToken", token);

            var formContent = new FormUrlEncodedContent(formData);
            await _client.PostAsync("/Totp/Verify", formContent);
        }

        // Act - Make one more attempt
        var finalFormData = new Dictionary<string, string>
        {
            {"UserId", user.Id},
            {"Code", "654321"} // Another invalid code
        };

        var finalVerifyResponse = await _client.GetAsync("/Totp/Verify");
        var finalVerifyContent = await finalVerifyResponse.Content.ReadAsStringAsync();
        var finalToken = ExtractAntiForgeryToken(finalVerifyContent);
        finalFormData.Add("__RequestVerificationToken", finalToken);

        var finalFormContent = new FormUrlEncodedContent(finalFormData);
        var response = await _client.PostAsync("/Totp/Verify", finalFormContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Account Temporarily Locked", content);
        Assert.Contains("too many failed verification attempts", content);
        Assert.Contains("try again in 5 minutes", content);
        
        // Verify user is locked out
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var lockedUser = await userManager.FindByIdAsync(user.Id);
        Assert.NotNull(lockedUser);
        Assert.Equal(3, lockedUser.TotpFailureCount);
        Assert.NotNull(lockedUser.TotpLockoutEnd);
        Assert.True(lockedUser.TotpLockoutEnd > DateTime.UtcNow);
    }

    [Fact]
    public async Task TotpVerify_Post_LockedAccount_ShowsLockoutMessage()
    {
        // Arrange - Create user with TOTP enabled and lock the account
        var user = await CreateUserWithTotpEnabledAsync();
        await LockUserAccountAsync(user);
        await SimulateLoginFlowAsync(user);
        
        var formData = new Dictionary<string, string>
        {
            {"UserId", user.Id},
            {"Code", "123456"}
        };

        // Get anti-forgery token
        var verifyResponse = await _client.GetAsync("/Totp/Verify");
        var verifyContent = await verifyResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(verifyContent);
        formData.Add("__RequestVerificationToken", token);

        var formContent = new FormUrlEncodedContent(formData);

        // Act
        var response = await _client.PostAsync("/Totp/Verify", formContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Account is temporarily locked", content);
    }

    [Fact]
    public async Task TotpVerify_Post_EmptyCode_ReturnsValidationError()
    {
        // Arrange - Create user with TOTP enabled and simulate login flow
        var user = await CreateUserWithTotpEnabledAsync();
        await SimulateLoginFlowAsync(user);
        
        var formData = new Dictionary<string, string>
        {
            {"UserId", user.Id},
            {"Code", ""} // Empty code
        };

        // Get anti-forgery token
        var verifyResponse = await _client.GetAsync("/Totp/Verify");
        var verifyContent = await verifyResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(verifyContent);
        formData.Add("__RequestVerificationToken", token);

        var formContent = new FormUrlEncodedContent(formData);

        // Act
        var response = await _client.PostAsync("/Totp/Verify", formContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Two-Factor Authentication", content);
        Assert.Contains("field-validation-error", content);
    }

    [Fact]
    public async Task TotpVerify_Post_InvalidCodeFormat_ReturnsValidationError()
    {
        // Arrange - Create user with TOTP enabled and simulate login flow
        var user = await CreateUserWithTotpEnabledAsync();
        await SimulateLoginFlowAsync(user);
        
        var formData = new Dictionary<string, string>
        {
            {"UserId", user.Id},
            {"Code", "12345"} // Too short
        };

        // Get anti-forgery token
        var verifyResponse = await _client.GetAsync("/Totp/Verify");
        var verifyContent = await verifyResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(verifyContent);
        formData.Add("__RequestVerificationToken", token);

        var formContent = new FormUrlEncodedContent(formData);

        // Act
        var response = await _client.PostAsync("/Totp/Verify", formContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Two-Factor Authentication", content);
        Assert.Contains("field-validation-error", content);
    }

    private async Task<ApplicationUser> CreateUserWithTotpEnabledAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var totpService = scope.ServiceProvider.GetRequiredService<ITotpService>();
        
        var user = new ApplicationUser
        {
            UserName = "testuser_" + Guid.NewGuid().ToString("N")[..8],
            Email = $"test_{Guid.NewGuid():N}@example.com",
            DisplayName = "Test User",
            TotpSecret = totpService.GenerateSecret(),
            IsTotpEnabled = true
        };
        
        await userManager.CreateAsync(user, "TestPassword123!");
        return user;
    }

    private async Task SimulateLoginFlowAsync(ApplicationUser user)
    {
        // Simulate the login flow by setting TempData values that would be set by AuthenticationController
        using var scope = _factory.Services.CreateScope();
        var httpContext = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext;
        
        // We need to simulate this by making a request that sets TempData
        // For testing purposes, we'll make a login request first
        var loginData = new Dictionary<string, string>
        {
            {"Email", user.Email!},
            {"Password", "TestPassword123!"}
        };

        var loginResponse = await _client.GetAsync("/Authentication/Login");
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(loginContent);
        loginData.Add("__RequestVerificationToken", token);

        var loginFormContent = new FormUrlEncodedContent(loginData);
        await _client.PostAsync("/Authentication/Login", loginFormContent);
    }

    private async Task LockUserAccountAsync(ApplicationUser user)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        user.TotpFailureCount = 3;
        user.TotpLockoutEnd = DateTime.UtcNow.AddMinutes(5);
        user.LastTotpFailure = DateTime.UtcNow;
        
        await userManager.UpdateAsync(user);
    }

    private static string ExtractAntiForgeryToken(string html)
    {
        var tokenStart = html.IndexOf("name=\"__RequestVerificationToken\"");
        if (tokenStart == -1) return string.Empty;
        
        var valueStart = html.IndexOf("value=\"", tokenStart) + 7;
        var valueEnd = html.IndexOf('"', valueStart);
        return html[valueStart..valueEnd];
    }

    private static string GenerateValidTotpCode(ITotpService totpService, string secret)
    {
        // Generate a real TOTP code using the same algorithm as the service
        var secretBytes = OtpNet.Base32Encoding.ToBytes(secret);
        var totp = new OtpNet.Totp(secretBytes);
        return totp.ComputeTotp();
    }
}
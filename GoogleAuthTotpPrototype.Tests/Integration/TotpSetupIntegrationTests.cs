using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using GoogleAuthTotpPrototype.Data;
using GoogleAuthTotpPrototype.Models;
using Microsoft.AspNetCore.Identity;
using Xunit;
using GoogleAuthTotpPrototype.Services;

namespace GoogleAuthTotpPrototype.Tests.Integration;

public class TotpSetupIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TotpSetupIntegrationTests(WebApplicationFactory<Program> factory)
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
    public async Task TotpSetup_Get_WithoutAuthentication_RedirectsToLogin()
    {
        // Act
        var response = await _client.GetAsync("/Totp/Setup");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Authentication/Login", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task TotpSetup_Get_WithAuthentication_ReturnsSuccessAndCorrectContent()
    {
        // Arrange - Create and authenticate a user
        var user = await CreateAndAuthenticateUserAsync();

        // Act
        var response = await _client.GetAsync("/Totp/Setup");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Set up Two-Factor Authentication", content);
        Assert.Contains("Install an Authenticator App", content);
        Assert.Contains("Scan QR Code", content);
        Assert.Contains("Manual Entry", content);
        Assert.Contains("Verify Setup", content);
        Assert.Contains("data:image/png;base64,", content); // QR code image
    }

    [Fact]
    public async Task TotpSetup_Get_GeneratesSecretAndQrCode()
    {
        // Arrange - Create and authenticate a user
        var user = await CreateAndAuthenticateUserAsync();

        // Act
        var response = await _client.GetAsync("/Totp/Setup");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        // Verify QR code is present
        Assert.Contains("data:image/png;base64,", content);
        
        // Verify manual entry key is present (should be base32 encoded)
        Assert.Matches(@"<code>[A-Z2-7]{32}</code>", content);
        
        // Verify user has TOTP secret but not enabled yet
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var updatedUser = await userManager.FindByIdAsync(user.Id);
        
        Assert.NotNull(updatedUser);
        Assert.NotNull(updatedUser.TotpSecret);
        Assert.False(updatedUser.IsTotpEnabled);
    }

    [Fact]
    public async Task TotpEnable_Post_ValidCode_EnablesTotpAndRedirects()
    {
        // Arrange - Create and authenticate a user, then setup TOTP
        var user = await CreateAndAuthenticateUserAsync();
        await _client.GetAsync("/Totp/Setup"); // This generates the secret
        
        // Get the user's TOTP secret from database
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var totpService = scope.ServiceProvider.GetRequiredService<ITotpService>();
        
        var userWithSecret = await userManager.FindByIdAsync(user.Id);
        Assert.NotNull(userWithSecret?.TotpSecret);
        
        // Generate a valid TOTP code
        var validCode = GenerateValidTotpCode(totpService, userWithSecret.TotpSecret);
        
        var formData = new Dictionary<string, string>
        {
            {"UserId", user.Id},
            {"Code", validCode}
        };

        // Get anti-forgery token
        var setupResponse = await _client.GetAsync("/Totp/Setup");
        var setupContent = await setupResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(setupContent);
        formData.Add("__RequestVerificationToken", token);

        var formContent = new FormUrlEncodedContent(formData);

        // Act
        var response = await _client.PostAsync("/Totp/Enable", formContent);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.ToString());
        
        // Verify TOTP is now enabled
        var enabledUser = await userManager.FindByIdAsync(user.Id);
        Assert.NotNull(enabledUser);
        Assert.True(enabledUser.IsTotpEnabled);
        Assert.NotNull(enabledUser.TotpSecret);
    }

    [Fact]
    public async Task TotpEnable_Post_InvalidCode_ReturnsViewWithError()
    {
        // Arrange - Create and authenticate a user, then setup TOTP
        var user = await CreateAndAuthenticateUserAsync();
        await _client.GetAsync("/Totp/Setup"); // This generates the secret
        
        var formData = new Dictionary<string, string>
        {
            {"UserId", user.Id},
            {"Code", "123456"} // Invalid code
        };

        // Get anti-forgery token
        var setupResponse = await _client.GetAsync("/Totp/Setup");
        var setupContent = await setupResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(setupContent);
        formData.Add("__RequestVerificationToken", token);

        var formContent = new FormUrlEncodedContent(formData);

        // Act
        var response = await _client.PostAsync("/Totp/Enable", formContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Set up Two-Factor Authentication", content); // Back to setup page
        Assert.Contains("Invalid TOTP code", content); // Error message
        
        // Verify TOTP is still not enabled
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var userStillNotEnabled = await userManager.FindByIdAsync(user.Id);
        Assert.NotNull(userStillNotEnabled);
        Assert.False(userStillNotEnabled.IsTotpEnabled);
    }

    [Fact]
    public async Task TotpEnable_Post_EmptyCode_ReturnsViewWithValidationError()
    {
        // Arrange - Create and authenticate a user, then setup TOTP
        var user = await CreateAndAuthenticateUserAsync();
        await _client.GetAsync("/Totp/Setup"); // This generates the secret
        
        var formData = new Dictionary<string, string>
        {
            {"UserId", user.Id},
            {"Code", ""} // Empty code
        };

        // Get anti-forgery token
        var setupResponse = await _client.GetAsync("/Totp/Setup");
        var setupContent = await setupResponse.Content.ReadAsStringAsync();
        var token = ExtractAntiForgeryToken(setupContent);
        formData.Add("__RequestVerificationToken", token);

        var formContent = new FormUrlEncodedContent(formData);

        // Act
        var response = await _client.PostAsync("/Totp/Enable", formContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Set up Two-Factor Authentication", content); // Back to setup page
        Assert.Contains("field-validation-error", content); // Validation error
    }

    [Fact]
    public async Task TotpSetup_MultipleRequests_GeneratesNewSecretEachTime()
    {
        // Arrange - Create and authenticate a user
        var user = await CreateAndAuthenticateUserAsync();

        // Act - Make multiple setup requests
        await _client.GetAsync("/Totp/Setup");
        
        using var scope1 = _factory.Services.CreateScope();
        var userManager1 = scope1.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var userAfterFirst = await userManager1.FindByIdAsync(user.Id);
        var firstSecret = userAfterFirst?.TotpSecret;
        
        await _client.GetAsync("/Totp/Setup");
        
        using var scope2 = _factory.Services.CreateScope();
        var userManager2 = scope2.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var userAfterSecond = await userManager2.FindByIdAsync(user.Id);
        var secondSecret = userAfterSecond?.TotpSecret;

        // Assert - Secrets should be different
        Assert.NotNull(firstSecret);
        Assert.NotNull(secondSecret);
        Assert.NotEqual(firstSecret, secondSecret);
    }

    private async Task<ApplicationUser> CreateAndAuthenticateUserAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        
        var user = new ApplicationUser
        {
            UserName = "testuser_" + Guid.NewGuid().ToString("N")[..8],
            Email = $"test_{Guid.NewGuid():N}@example.com",
            DisplayName = "Test User"
        };
        
        await userManager.CreateAsync(user, "TestPassword123!");
        
        // Simulate authentication by signing in the user
        await signInManager.SignInAsync(user, isPersistent: false);
        
        return user;
    }

    private static string ExtractAntiForgeryToken(string html)
    {
        var tokenStart = html.IndexOf("name=\"__RequestVerificationToken\"");
        var valueStart = html.IndexOf("value=\"", tokenStart) + 7;
        var valueEnd = html.IndexOf('"', valueStart);
        return html[valueStart..valueEnd];
    }

    private static string GenerateValidTotpCode(ITotpService totpService, string secret)
    {
        // This is a simplified approach - in a real scenario, we'd need to generate
        // a valid TOTP code based on the current time
        // For testing purposes, we'll use a mock approach
        
        // Generate multiple codes and try to find one that validates
        // This simulates the time-based nature of TOTP
        for (int i = 0; i < 10; i++)
        {
            var testCode = (100000 + (i * 111111) % 900000).ToString("D6");
            if (totpService.ValidateTotp(secret, testCode))
            {
                return testCode;
            }
        }
        
        // If no mock code works, we need to generate a real TOTP code
        // This requires using the same algorithm as the TOTP service
        var secretBytes = OtpNet.Base32Encoding.ToBytes(secret);
        var totp = new OtpNet.Totp(secretBytes);
        return totp.ComputeTotp();
    }
}
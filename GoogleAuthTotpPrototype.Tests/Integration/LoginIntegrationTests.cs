using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using GoogleAuthTotpPrototype.Data;
using GoogleAuthTotpPrototype.Models;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace GoogleAuthTotpPrototype.Tests.Integration;

public class LoginIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public LoginIntegrationTests(WebApplicationFactory<Program> factory)
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
    public async Task Login_Get_ReturnsSuccessAndCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/Authentication/Login");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Sign In", content);
        Assert.Contains("Email", content);
        Assert.Contains("Password", content);
        Assert.Contains("Remember me", content);
    }

    [Fact]
    public async Task Login_Post_ValidCredentials_WithoutTotp_RedirectsToHome()
    {
        // Arrange - Create a user without TOTP enabled
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        var user = new ApplicationUser
        {
            UserName = "testuser",
            Email = "test@example.com",
            DisplayName = "testuser",
            IsTotpEnabled = false
        };
        
        await userManager.CreateAsync(user, "TestPassword123!");

        var formData = new Dictionary<string, string>
        {
            {"Email", "test@example.com"},
            {"Password", "TestPassword123!"},
            {"RememberMe", "false"}
        };

        // Get the login page first to get the anti-forgery token
        var getResponse = await _client.GetAsync("/Authentication/Login");
        var getContent = await getResponse.Content.ReadAsStringAsync();
        
        // Extract anti-forgery token
        var tokenStart = getContent.IndexOf("name=\"__RequestVerificationToken\"");
        var valueStart = getContent.IndexOf("value=\"", tokenStart) + 7;
        var valueEnd = getContent.IndexOf('"', valueStart);
        var token = getContent[valueStart..valueEnd];
        
        formData.Add("__RequestVerificationToken", token);

        var formContent = new FormUrlEncodedContent(formData);

        // Act
        var response = await _client.PostAsync("/Authentication/Login", formContent);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Login_Post_ValidCredentials_WithTotp_RedirectsToTotpVerification()
    {
        // Arrange - Create a user with TOTP enabled
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        var user = new ApplicationUser
        {
            UserName = "totpuser",
            Email = "totp@example.com",
            DisplayName = "totpuser",
            IsTotpEnabled = true,
            TotpSecret = "JBSWY3DPEHPK3PXP"
        };
        
        await userManager.CreateAsync(user, "TotpPassword123!");

        var formData = new Dictionary<string, string>
        {
            {"Email", "totp@example.com"},
            {"Password", "TotpPassword123!"},
            {"RememberMe", "false"}
        };

        // Get the login page first to get the anti-forgery token
        var getResponse = await _client.GetAsync("/Authentication/Login");
        var getContent = await getResponse.Content.ReadAsStringAsync();
        
        // Extract anti-forgery token
        var tokenStart = getContent.IndexOf("name=\"__RequestVerificationToken\"");
        var valueStart = getContent.IndexOf("value=\"", tokenStart) + 7;
        var valueEnd = getContent.IndexOf('"', valueStart);
        var token = getContent[valueStart..valueEnd];
        
        formData.Add("__RequestVerificationToken", token);

        var formContent = new FormUrlEncodedContent(formData);

        // Act
        var response = await _client.PostAsync("/Authentication/Login", formContent);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Totp/Verify", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Login_Post_InvalidEmail_ReturnsViewWithError()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            {"Email", "nonexistent@example.com"},
            {"Password", "SomePassword123!"},
            {"RememberMe", "false"}
        };

        // Get the login page first to get the anti-forgery token
        var getResponse = await _client.GetAsync("/Authentication/Login");
        var getContent = await getResponse.Content.ReadAsStringAsync();
        
        // Extract anti-forgery token
        var tokenStart = getContent.IndexOf("name=\"__RequestVerificationToken\"");
        var valueStart = getContent.IndexOf("value=\"", tokenStart) + 7;
        var valueEnd = getContent.IndexOf('"', valueStart);
        var token = getContent[valueStart..valueEnd];
        
        formData.Add("__RequestVerificationToken", token);

        var formContent = new FormUrlEncodedContent(formData);

        // Act
        var response = await _client.PostAsync("/Authentication/Login", formContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Sign In", content); // Still on login page
        Assert.Contains("Invalid email or password", content);
    }

    [Fact]
    public async Task Login_Post_InvalidPassword_ReturnsViewWithError()
    {
        // Arrange - Create a user first
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        var user = new ApplicationUser
        {
            UserName = "validuser",
            Email = "valid@example.com",
            DisplayName = "validuser",
            IsTotpEnabled = false
        };
        
        await userManager.CreateAsync(user, "CorrectPassword123!");

        var formData = new Dictionary<string, string>
        {
            {"Email", "valid@example.com"},
            {"Password", "WrongPassword123!"},
            {"RememberMe", "false"}
        };

        // Get the login page first to get the anti-forgery token
        var getResponse = await _client.GetAsync("/Authentication/Login");
        var getContent = await getResponse.Content.ReadAsStringAsync();
        
        // Extract anti-forgery token
        var tokenStart = getContent.IndexOf("name=\"__RequestVerificationToken\"");
        var valueStart = getContent.IndexOf("value=\"", tokenStart) + 7;
        var valueEnd = getContent.IndexOf('"', valueStart);
        var token = getContent[valueStart..valueEnd];
        
        formData.Add("__RequestVerificationToken", token);

        var formContent = new FormUrlEncodedContent(formData);

        // Act
        var response = await _client.PostAsync("/Authentication/Login", formContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Sign In", content); // Still on login page
        Assert.Contains("Invalid email or password", content);
    }

    [Fact]
    public async Task Login_Post_InvalidModelState_ReturnsViewWithValidationErrors()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            {"Email", "invalid-email"}, // Invalid email format
            {"Password", ""}, // Empty password
            {"RememberMe", "false"}
        };

        // Get the login page first to get the anti-forgery token
        var getResponse = await _client.GetAsync("/Authentication/Login");
        var getContent = await getResponse.Content.ReadAsStringAsync();
        
        // Extract anti-forgery token
        var tokenStart = getContent.IndexOf("name=\"__RequestVerificationToken\"");
        var valueStart = getContent.IndexOf("value=\"", tokenStart) + 7;
        var valueEnd = getContent.IndexOf('"', valueStart);
        var token = getContent[valueStart..valueEnd];
        
        formData.Add("__RequestVerificationToken", token);

        var formContent = new FormUrlEncodedContent(formData);

        // Act
        var response = await _client.PostAsync("/Authentication/Login", formContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Sign In", content); // Still on login page
        Assert.Contains("field-validation-error", content); // Contains validation errors
    }

    [Fact]
    public async Task Login_Post_AccountLockout_ReturnsViewWithLockoutMessage()
    {
        // Arrange - Create a user and lock them out
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        var user = new ApplicationUser
        {
            UserName = "lockeduser",
            Email = "locked@example.com",
            DisplayName = "lockeduser",
            IsTotpEnabled = false
        };
        
        await userManager.CreateAsync(user, "LockedPassword123!");
        
        // Lock the user account
        await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(5));

        var formData = new Dictionary<string, string>
        {
            {"Email", "locked@example.com"},
            {"Password", "LockedPassword123!"},
            {"RememberMe", "false"}
        };

        // Get the login page first to get the anti-forgery token
        var getResponse = await _client.GetAsync("/Authentication/Login");
        var getContent = await getResponse.Content.ReadAsStringAsync();
        
        // Extract anti-forgery token
        var tokenStart = getContent.IndexOf("name=\"__RequestVerificationToken\"");
        var valueStart = getContent.IndexOf("value=\"", tokenStart) + 7;
        var valueEnd = getContent.IndexOf('"', valueStart);
        var token = getContent[valueStart..valueEnd];
        
        formData.Add("__RequestVerificationToken", token);

        var formContent = new FormUrlEncodedContent(formData);

        // Act
        var response = await _client.PostAsync("/Authentication/Login", formContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Sign In", content); // Still on login page
        Assert.Contains("Account locked out", content);
    }

    [Fact]
    public async Task Login_Post_RememberMe_SetsCorrectCookieOptions()
    {
        // Arrange - Create a user
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        var user = new ApplicationUser
        {
            UserName = "rememberuser",
            Email = "remember@example.com",
            DisplayName = "rememberuser",
            IsTotpEnabled = false
        };
        
        await userManager.CreateAsync(user, "RememberPassword123!");

        var formData = new Dictionary<string, string>
        {
            {"Email", "remember@example.com"},
            {"Password", "RememberPassword123!"},
            {"RememberMe", "true"}
        };

        // Get the login page first to get the anti-forgery token
        var getResponse = await _client.GetAsync("/Authentication/Login");
        var getContent = await getResponse.Content.ReadAsStringAsync();
        
        // Extract anti-forgery token
        var tokenStart = getContent.IndexOf("name=\"__RequestVerificationToken\"");
        var valueStart = getContent.IndexOf("value=\"", tokenStart) + 7;
        var valueEnd = getContent.IndexOf('"', valueStart);
        var token = getContent[valueStart..valueEnd];
        
        formData.Add("__RequestVerificationToken", token);

        var formContent = new FormUrlEncodedContent(formData);

        // Act
        var response = await _client.PostAsync("/Authentication/Login", formContent);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        
        // Check that authentication cookies are set
        var cookies = response.Headers.GetValues("Set-Cookie");
        Assert.NotEmpty(cookies);
        
        // Should contain authentication cookie
        Assert.Contains(cookies, cookie => cookie.Contains(".AspNetCore.Identity.Application"));
    }

    [Fact]
    public async Task Login_MultipleFailedAttempts_TriggersAccountLockout()
    {
        // Arrange - Create a user
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        var user = new ApplicationUser
        {
            UserName = "failureuser",
            Email = "failure@example.com",
            DisplayName = "failureuser",
            IsTotpEnabled = false
        };
        
        await userManager.CreateAsync(user, "CorrectPassword123!");

        // Perform multiple failed login attempts
        for (int i = 0; i < 6; i++) // More than the configured max attempts (5)
        {
            var formData = new Dictionary<string, string>
            {
                {"Email", "failure@example.com"},
                {"Password", "WrongPassword123!"},
                {"RememberMe", "false"}
            };

            // Get the login page first to get the anti-forgery token
            var getResponse = await _client.GetAsync("/Authentication/Login");
            var getContent = await getResponse.Content.ReadAsStringAsync();
            
            // Extract anti-forgery token
            var tokenStart = getContent.IndexOf("name=\"__RequestVerificationToken\"");
            var valueStart = getContent.IndexOf("value=\"", tokenStart) + 7;
            var valueEnd = getContent.IndexOf('"', valueStart);
            var token = getContent[valueStart..valueEnd];
            
            formData.Add("__RequestVerificationToken", token);

            var formContent = new FormUrlEncodedContent(formData);

            // Act
            var response = await _client.PostAsync("/Authentication/Login", formContent);

            // Assert - Should still return OK but with error message
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // Now try with correct password - should be locked out
        var finalFormData = new Dictionary<string, string>
        {
            {"Email", "failure@example.com"},
            {"Password", "CorrectPassword123!"},
            {"RememberMe", "false"}
        };

        var finalGetResponse = await _client.GetAsync("/Authentication/Login");
        var finalGetContent = await finalGetResponse.Content.ReadAsStringAsync();
        
        var finalTokenStart = finalGetContent.IndexOf("name=\"__RequestVerificationToken\"");
        var finalValueStart = finalGetContent.IndexOf("value=\"", finalTokenStart) + 7;
        var finalValueEnd = finalGetContent.IndexOf('"', finalValueStart);
        var finalToken = finalGetContent[finalValueStart..finalValueEnd];
        
        finalFormData.Add("__RequestVerificationToken", finalToken);

        var finalFormContent = new FormUrlEncodedContent(finalFormData);

        // Act
        var finalResponse = await _client.PostAsync("/Authentication/Login", finalFormContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var finalContent = await finalResponse.Content.ReadAsStringAsync();
        Assert.Contains("Account locked out", finalContent);
    }
}
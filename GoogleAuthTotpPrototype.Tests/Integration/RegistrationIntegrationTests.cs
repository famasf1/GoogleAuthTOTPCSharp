using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using GoogleAuthTotpPrototype.Data;
using GoogleAuthTotpPrototype.Models;
using Microsoft.AspNetCore.Identity;
using Xunit;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace GoogleAuthTotpPrototype.Tests.Integration;

public class RegistrationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public RegistrationIntegrationTests(WebApplicationFactory<Program> factory)
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
    public async Task Register_Get_ReturnsSuccessAndCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/Authentication/Register");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Create Account", content);
        Assert.Contains("Username", content);
        Assert.Contains("Email", content);
        Assert.Contains("Password", content);
        Assert.Contains("Confirm password", content);
    }

    [Fact]
    public async Task Register_Post_ValidData_RedirectsToTotpSetup()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            {"Username", "testuser"},
            {"Email", "test@example.com"},
            {"Password", "Test123!"},
            {"ConfirmPassword", "Test123!"}
        };

        // Get the registration page first to get the anti-forgery token
        var getResponse = await _client.GetAsync("/Authentication/Register");
        var getContent = await getResponse.Content.ReadAsStringAsync();
        
        // Extract anti-forgery token (simplified approach)
        var tokenStart = getContent.IndexOf("name=\"__RequestVerificationToken\"");
        var valueStart = getContent.IndexOf("value=\"", tokenStart) + 7;
        var valueEnd = getContent.IndexOf("\"", valueStart);
        var token = getContent.Substring(valueStart, valueEnd - valueStart);
        
        formData.Add("__RequestVerificationToken", token);

        var formContent = new FormUrlEncodedContent(formData);

        // Act
        var response = await _client.PostAsync("/Authentication/Register", formContent);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Totp/Setup", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Register_Post_InvalidData_ReturnsViewWithErrors()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            {"Username", ""}, // Invalid: empty username
            {"Email", "invalid-email"}, // Invalid: not a valid email
            {"Password", "123"}, // Invalid: too short
            {"ConfirmPassword", "different"} // Invalid: doesn't match password
        };

        // Get the registration page first to get the anti-forgery token
        var getResponse = await _client.GetAsync("/Authentication/Register");
        var getContent = await getResponse.Content.ReadAsStringAsync();
        
        // Extract anti-forgery token (simplified approach)
        var tokenStart = getContent.IndexOf("name=\"__RequestVerificationToken\"");
        var valueStart = getContent.IndexOf("value=\"", tokenStart) + 7;
        var valueEnd = getContent.IndexOf("\"", valueStart);
        var token = getContent.Substring(valueStart, valueEnd - valueStart);
        
        formData.Add("__RequestVerificationToken", token);

        var formContent = new FormUrlEncodedContent(formData);

        // Act
        var response = await _client.PostAsync("/Authentication/Register", formContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Create Account", content); // Still on registration page
        Assert.Contains("field-validation-error", content); // Contains validation errors
    }

    [Fact]
    public async Task Register_Post_DuplicateEmail_ReturnsViewWithError()
    {
        // Arrange - Create a user first
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        var existingUser = new ApplicationUser
        {
            UserName = "existinguser",
            Email = "test@example.com",
            DisplayName = "existinguser"
        };
        
        await userManager.CreateAsync(existingUser, "ExistingPassword123!");

        var formData = new Dictionary<string, string>
        {
            {"Username", "newuser"},
            {"Email", "test@example.com"}, // Same email as existing user
            {"Password", "NewPassword123!"},
            {"ConfirmPassword", "NewPassword123!"}
        };

        // Get the registration page first to get the anti-forgery token
        var getResponse = await _client.GetAsync("/Authentication/Register");
        var getContent = await getResponse.Content.ReadAsStringAsync();
        
        // Extract anti-forgery token (simplified approach)
        var tokenStart = getContent.IndexOf("name=\"__RequestVerificationToken\"");
        var valueStart = getContent.IndexOf("value=\"", tokenStart) + 7;
        var valueEnd = getContent.IndexOf("\"", valueStart);
        var token = getContent.Substring(valueStart, valueEnd - valueStart);
        
        formData.Add("__RequestVerificationToken", token);

        var formContent = new FormUrlEncodedContent(formData);

        // Act
        var response = await _client.PostAsync("/Authentication/Register", formContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Create Account", content); // Still on registration page
        // Should contain error about duplicate email/username
    }

    [Fact]
    public async Task Register_Post_ValidData_CreatesUserInDatabase()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            {"Username", "databaseuser"},
            {"Email", "database@example.com"},
            {"Password", "DatabaseTest123!"},
            {"ConfirmPassword", "DatabaseTest123!"}
        };

        // Get the registration page first to get the anti-forgery token
        var getResponse = await _client.GetAsync("/Authentication/Register");
        var getContent = await getResponse.Content.ReadAsStringAsync();
        
        // Extract anti-forgery token (simplified approach)
        var tokenStart = getContent.IndexOf("name=\"__RequestVerificationToken\"");
        var valueStart = getContent.IndexOf("value=\"", tokenStart) + 7;
        var valueEnd = getContent.IndexOf("\"", valueStart);
        var token = getContent.Substring(valueStart, valueEnd - valueStart);
        
        formData.Add("__RequestVerificationToken", token);

        var formContent = new FormUrlEncodedContent(formData);

        // Act
        var response = await _client.PostAsync("/Authentication/Register", formContent);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        
        // Verify user was created in database
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        var user = await userManager.FindByEmailAsync("database@example.com");
        Assert.NotNull(user);
        Assert.Equal("databaseuser", user.UserName);
        Assert.Equal("databaseuser", user.DisplayName);
        Assert.Equal("database@example.com", user.Email);
        Assert.False(user.IsTotpEnabled); // Should be false initially
        Assert.Null(user.TotpSecret); // Should be null initially
    }
}
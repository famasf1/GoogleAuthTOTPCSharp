using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using GoogleAuthTotpPrototype.Data;
using GoogleAuthTotpPrototype.Models;
using Microsoft.AspNetCore.Identity;
using Xunit;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace GoogleAuthTotpPrototype.Tests.Integration;

public class SuccessPageIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SuccessPageIntegrationTests(WebApplicationFactory<Program> factory)
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
    public async Task Success_Get_UnauthenticatedUser_RedirectsToLogin()
    {
        // Act
        var response = await _client.GetAsync("/Home/Success");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Authentication/Login", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Success_Get_AuthenticatedUser_ReturnsSuccessPage()
    {
        // Arrange - Create and authenticate a user
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        
        var user = new ApplicationUser
        {
            UserName = "successuser",
            Email = "success@example.com",
            DisplayName = "Success User",
            IsTotpEnabled = true
        };
        
        await userManager.CreateAsync(user, "SuccessPassword123!");

        // Create authenticated client
        var authenticatedClient = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                        "Test", options => { });
            });
        }).CreateClient();

        // Set authentication header
        authenticatedClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Test");

        // Act
        var response = await authenticatedClient.GetAsync("/Home/Success");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Login Successful", content);
        Assert.Contains("Welcome!", content);
        Assert.Contains("User Information", content);
        Assert.Contains("Logout", content);
    }

    [Fact]
    public async Task Success_Get_AuthenticatedUser_DisplaysUserInformation()
    {
        // Arrange - Create and authenticate a user
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        var user = new ApplicationUser
        {
            UserName = "infouser",
            Email = "info@example.com",
            DisplayName = "Info User",
            IsTotpEnabled = true
        };
        
        await userManager.CreateAsync(user, "InfoPassword123!");

        // Create authenticated client
        var authenticatedClient = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                        "Test", options => { });
            });
        }).CreateClient();

        // Set authentication header
        authenticatedClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Test");

        // Act
        var response = await authenticatedClient.GetAsync("/Home/Success");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        // Check that user information is displayed
        Assert.Contains("Username:", content);
        Assert.Contains("Email:", content);
        Assert.Contains("Login Time:", content);
        Assert.Contains("Session Information", content);
        Assert.Contains("two-factor authentication", content);
    }

    [Fact]
    public async Task Success_Get_AuthenticatedUser_ContainsLogoutButton()
    {
        // Arrange - Create authenticated client
        var authenticatedClient = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                        "Test", options => { });
            });
        }).CreateClient();

        // Set authentication header
        authenticatedClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Test");

        // Act
        var response = await authenticatedClient.GetAsync("/Home/Success");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        // Check that logout form is present
        Assert.Contains("form", content);
        Assert.Contains("asp-controller=\"Authentication\"", content);
        Assert.Contains("asp-action=\"Logout\"", content);
        Assert.Contains("Logout", content);
        Assert.Contains("__RequestVerificationToken", content);
    }

    [Fact]
    public async Task Success_Get_AuthenticatedUser_ContainsSecurityMessage()
    {
        // Arrange - Create authenticated client
        var authenticatedClient = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                        "Test", options => { });
            });
        }).CreateClient();

        // Set authentication header
        authenticatedClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Test");

        // Act
        var response = await authenticatedClient.GetAsync("/Home/Success");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        
        // Check that security message is displayed
        Assert.Contains("secure and protected", content);
        Assert.Contains("two-factor authentication", content);
    }
}

// Test authentication handler for integration tests
public class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.NameIdentifier, "test-user-id")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
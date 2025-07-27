using GoogleAuthTotpPrototype.Services;
using Microsoft.Extensions.Configuration;
using OtpNet;
using System.Text.RegularExpressions;
using Xunit;

namespace GoogleAuthTotpPrototype.Tests;

public class TotpServiceTests
{
    private readonly ITotpService _totpService;
    private readonly IConfiguration _configuration;
    private readonly ITestOutputHelper _output;

    public TotpServiceTests(ITestOutputHelper output)
    {
        _output = output;
        
        // Create test configuration
        var configurationBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Totp:Issuer"] = "Test Issuer",
                ["Totp:LockoutDurationMinutes"] = "5",
                ["Totp:MaxFailureAttempts"] = "3"
            });
        
        _configuration = configurationBuilder.Build();
        _totpService = new TotpService(_configuration);
    }

    [Fact]
    public void GenerateSecret_ShouldReturnValidBase32String()
    {
        // Act
        var secret = _totpService.GenerateSecret();

        // Output to see the generated secret
        _output.WriteLine($"✅ Generated TOTP Secret: {secret}");
        _output.WriteLine($"✅ Secret Length: {secret.Length} characters");

        // Assert
        Assert.NotNull(secret);
        Assert.NotEmpty(secret);
        
        // Base32 should only contain A-Z and 2-7
        Assert.Matches(@"^[A-Z2-7]+$", secret);
        
        // Should be able to decode back to bytes
        var bytes = Base32Encoding.ToBytes(secret);
        Assert.Equal(20, bytes.Length); // 160 bits = 20 bytes
        
        _output.WriteLine($"✅ Decoded to {bytes.Length} bytes (160 bits)");
    }

    [Fact]
    public void GenerateSecret_ShouldReturnDifferentSecretsOnMultipleCalls()
    {
        // Act
        var secret1 = _totpService.GenerateSecret();
        var secret2 = _totpService.GenerateSecret();

        // Output to see both secrets
        _output.WriteLine($"✅ First Secret:  {secret1}");
        _output.WriteLine($"✅ Second Secret: {secret2}");
        _output.WriteLine($"✅ Secrets are different: {secret1 != secret2}");

        // Assert
        Assert.NotEqual(secret1, secret2);
    }

    [Fact]
    public void GenerateQrCodeUri_WithValidInputs_ShouldReturnCorrectUri()
    {
        // Arrange
        var email = "test@example.com";
        var secret = "JBSWY3DPEHPK3PXP";

        // Act
        var uri = _totpService.GenerateQrCodeUri(email, secret);

        // Output to see the generated QR code URI
        _output.WriteLine($"✅ Generated QR Code URI: {uri}");
        _output.WriteLine($"✅ Email: {email}");
        _output.WriteLine($"✅ Secret: {secret}");
        _output.WriteLine($"✅ URI Length: {uri.Length} characters");

        // Assert
        Assert.NotNull(uri);
        Assert.StartsWith("otpauth://totp/", uri);
        Assert.Contains("Test%20Issuer:test%40example.com", uri);
        Assert.Contains($"secret={secret}", uri);
        Assert.Contains("issuer=Test%20Issuer", uri);
    }

    [Fact]
    public void GenerateQrCodeUri_WithSpecialCharactersInEmail_ShouldEncodeCorrectly()
    {
        // Arrange
        var email = "test+user@example.com";
        var secret = "JBSWY3DPEHPK3PXP";

        _output.WriteLine($"🔍 GenerateQrCodeUri_WithSpecialCharactersInEmail_ShouldEncodeCorrectly:");
        _output.WriteLine($"   Email: {email}");
        _output.WriteLine($"   Secret: {secret}");

        // Act
        var uri = _totpService.GenerateQrCodeUri(email, secret);

        _output.WriteLine($"   Generated URI: {uri}");
        _output.WriteLine($"   Contains encoded email: {uri.Contains("test%2Buser%40example.com")}");

        // Assert
        Assert.Contains("test%2Buser%40example.com", uri);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GenerateQrCodeUri_WithInvalidEmail_ShouldThrowArgumentException(string email)
    {
        // Arrange
        var secret = "JBSWY3DPEHPK3PXP";

        _output.WriteLine($"🔍 GenerateQrCodeUri_WithInvalidEmail_ShouldThrowArgumentException:");
        _output.WriteLine($"   Email: '{email}' (null/empty/whitespace)");
        _output.WriteLine($"   Secret: {secret}");

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _totpService.GenerateQrCodeUri(email, secret));
        
        _output.WriteLine($"   Exception thrown: {exception.GetType().Name}");
        _output.WriteLine($"   Exception message: {exception.Message}");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GenerateQrCodeUri_WithInvalidSecret_ShouldThrowArgumentException(string secret)
    {
        // Arrange
        var email = "test@example.com";

        _output.WriteLine($"🔍 GenerateQrCodeUri_WithInvalidSecret_ShouldThrowArgumentException:");
        _output.WriteLine($"   Email: {email}");
        _output.WriteLine($"   Secret: '{secret}' (null/empty/whitespace)");

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _totpService.GenerateQrCodeUri(email, secret));
        
        _output.WriteLine($"   Exception thrown: {exception.GetType().Name}");
        _output.WriteLine($"   Exception message: {exception.Message}");
    }

    [Fact]
    public void ValidateTotp_WithValidCode_ShouldReturnTrue()
    {
        // Arrange
        var secret = "JBSWY3DPEHPK3PXP";
        var secretBytes = Base32Encoding.ToBytes(secret);
        var totp = new Totp(secretBytes);
        var validCode = totp.ComputeTotp();

        // Output to see the TOTP validation process
        _output.WriteLine($"✅ TOTP Secret: {secret}");
        _output.WriteLine($"✅ Generated TOTP Code: {validCode}");
        _output.WriteLine($"✅ Current Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

        // Act
        var result = _totpService.ValidateTotp(secret, validCode);

        // Output to see validation result
        _output.WriteLine($"✅ Validation Result: {result}");
        _output.WriteLine($"✅ Code is valid for current time window");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateTotp_WithInvalidCode_ShouldReturnFalse()
    {
        // Arrange
        var secret = "JBSWY3DPEHPK3PXP";
        var invalidCode = "000000";

        _output.WriteLine($"🔍 ValidateTotp_WithInvalidCode_ShouldReturnFalse:");
        _output.WriteLine($"   Secret: {secret}");
        _output.WriteLine($"   Invalid Code: {invalidCode}");

        // Act
        var result = _totpService.ValidateTotp(secret, invalidCode);

        _output.WriteLine($"   Validation Result: {result}");
        _output.WriteLine($"   Expected: False (invalid code should fail)");

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateTotp_WithInvalidSecret_ShouldThrowArgumentException(string secret)
    {
        // Arrange
        var code = "123456";

        _output.WriteLine($"🔍 ValidateTotp_WithInvalidSecret_ShouldThrowArgumentException:");
        _output.WriteLine($"   Secret: '{secret}' (null/empty/whitespace)");
        _output.WriteLine($"   Code: {code}");

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _totpService.ValidateTotp(secret, code));
        
        _output.WriteLine($"   Exception thrown: {exception.GetType().Name}");
        _output.WriteLine($"   Exception message: {exception.Message}");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateTotp_WithNullOrEmptyCode_ShouldReturnFalse(string code)
    {
        // Arrange
        var secret = "JBSWY3DPEHPK3PXP";

        _output.WriteLine($"🔍 ValidateTotp_WithNullOrEmptyCode_ShouldReturnFalse:");
        _output.WriteLine($"   Secret: {secret}");
        _output.WriteLine($"   Code: '{code}' (null/empty/whitespace)");

        // Act
        var result = _totpService.ValidateTotp(secret, code);

        _output.WriteLine($"   Validation Result: {result}");
        _output.WriteLine($"   Expected: False (null/empty code should fail)");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateTotp_WithMalformedSecret_ShouldReturnFalse()
    {
        // Arrange
        var malformedSecret = "INVALID_BASE32!";
        var code = "123456";

        _output.WriteLine($"🔍 ValidateTotp_WithMalformedSecret_ShouldReturnFalse:");
        _output.WriteLine($"   Malformed Secret: {malformedSecret}");
        _output.WriteLine($"   Code: {code}");

        // Act
        var result = _totpService.ValidateTotp(malformedSecret, code);

        _output.WriteLine($"   Validation Result: {result}");
        _output.WriteLine($"   Expected: False (malformed secret should fail)");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GenerateQrCode_WithValidUri_ShouldReturnPngBytes()
    {
        // Arrange
        var uri = "otpauth://totp/Test:test@example.com?secret=JBSWY3DPEHPK3PXP&issuer=Test";

        // Act
        var qrCodeBytes = _totpService.GenerateQrCode(uri);

        // Output to see QR code generation details
        _output.WriteLine($"✅ QR Code URI: {uri}");
        _output.WriteLine($"✅ Generated QR Code Size: {qrCodeBytes.Length} bytes");
        _output.WriteLine($"✅ QR Code Format: PNG");
        
        // Show first few bytes as hex for verification
        var firstBytes = qrCodeBytes.Take(8).Select(b => b.ToString("X2"));
        _output.WriteLine($"✅ PNG Header: {string.Join(" ", firstBytes)}");

        // Assert
        Assert.NotNull(qrCodeBytes);
        Assert.True(qrCodeBytes.Length > 0);
        
        // Check PNG signature (first 8 bytes)
        var pngSignature = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var actualSignature = qrCodeBytes.Take(8).ToArray();
        Assert.Equal(pngSignature, actualSignature);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GenerateQrCode_WithInvalidUri_ShouldThrowArgumentException(string uri)
    {
        _output.WriteLine($"🔍 GenerateQrCode_WithInvalidUri_ShouldThrowArgumentException:");
        _output.WriteLine($"   URI: '{uri}' (null/empty/whitespace)");

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _totpService.GenerateQrCode(uri));
        
        _output.WriteLine($"   Exception thrown: {exception.GetType().Name}");
        _output.WriteLine($"   Exception message: {exception.Message}");
    }

    [Fact]
    public void ValidateTotp_WithTimeWindowTolerance_ShouldAcceptRecentCodes()
    {
        // Arrange
        var secret = "JBSWY3DPEHPK3PXP";
        var secretBytes = Base32Encoding.ToBytes(secret);
        
        // Generate a code for the previous time step (30 seconds ago)
        var previousTimeStep = DateTimeOffset.UtcNow.AddSeconds(-30);
        var totp = new Totp(secretBytes);
        var previousCode = totp.ComputeTotp(previousTimeStep.DateTime);

        _output.WriteLine($"🔍 ValidateTotp_WithTimeWindowTolerance_ShouldAcceptRecentCodes:");
        _output.WriteLine($"   Secret: {secret}");
        _output.WriteLine($"   Current Time: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        _output.WriteLine($"   Previous Time Step: {previousTimeStep:yyyy-MM-dd HH:mm:ss} UTC");
        _output.WriteLine($"   Generated Previous Code: {previousCode}");

        // Act
        var result = _totpService.ValidateTotp(secret, previousCode);

        _output.WriteLine($"   Validation Result: {result}");
        _output.WriteLine($"   Expected: True (should accept recent codes)");

        // Assert
        // Should accept codes from previous time window due to tolerance
        Assert.True(result);
    }

    [Fact]
    public void TotpService_WithDefaultConfiguration_ShouldUseDefaultIssuer()
    {
        // Arrange
        var configWithoutIssuer = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        
        var serviceWithDefaultConfig = new TotpService(configWithoutIssuer);
        var email = "test@example.com";
        var secret = "JBSWY3DPEHPK3PXP";

        _output.WriteLine($"🔍 TotpService_WithDefaultConfiguration_ShouldUseDefaultIssuer:");
        _output.WriteLine($"   Email: {email}");
        _output.WriteLine($"   Secret: {secret}");
        _output.WriteLine($"   Configuration: Empty (should use default issuer)");

        // Act
        var uri = serviceWithDefaultConfig.GenerateQrCodeUri(email, secret);

        _output.WriteLine($"   Generated URI: {uri}");
        _output.WriteLine($"   Contains default issuer: {uri.Contains("Google%20Auth%20Prototype")}");
        _output.WriteLine($"   Expected: Should contain 'Google%20Auth%20Prototype'");

        // Assert
        Assert.Contains("Google%20Auth%20Prototype", uri);
    }
}
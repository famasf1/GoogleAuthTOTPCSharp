using OtpNet;
using QRCoder;
using System.Security.Cryptography;

namespace GoogleAuthTotpPrototype.Services;

/// <summary>
/// Implementation of TOTP service using OtpNet and QRCoder libraries
/// </summary>
public class TotpService : ITotpService
{
    private readonly IConfiguration _configuration;
    private readonly string _issuer;

    public TotpService(IConfiguration configuration)
    {
        _configuration = configuration;
        _issuer = _configuration["Totp:Issuer"] ?? "Google Auth Prototype";
    }

    /// <summary>
    /// Generates a cryptographically secure Base32-encoded secret for TOTP
    /// </summary>
    public string GenerateSecret()
    {
        // Generate 20 bytes (160 bits) of random data for the secret
        var secretBytes = new byte[20];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(secretBytes);
        }

        // Convert to Base32 encoding as required by TOTP standard
        return Base32Encoding.ToString(secretBytes);
    }

    /// <summary>
    /// Generates a QR code URI compatible with Google Authenticator and other TOTP apps
    /// </summary>
    public string GenerateQrCodeUri(string email, string secret)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be null or empty", nameof(email));
        
        if (string.IsNullOrWhiteSpace(secret))
            throw new ArgumentException("Secret cannot be null or empty", nameof(secret));

        // Format: otpauth://totp/Issuer:email?secret=SECRET&issuer=ISSUER
        var encodedEmail = Uri.EscapeDataString(email);
        var encodedIssuer = Uri.EscapeDataString(_issuer);
        
        return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={secret}&issuer={encodedIssuer}";
    }

    /// <summary>
    /// Validates a TOTP code with time window tolerance
    /// </summary>
    public bool ValidateTotp(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(secret))
            throw new ArgumentException("Secret cannot be null or empty", nameof(secret));
        
        if (string.IsNullOrWhiteSpace(code))
            return false;

        try
        {
            // Convert Base32 secret to bytes
            var secretBytes = Base32Encoding.ToBytes(secret);
            
            // Create TOTP instance with 30-second step and SHA1 (standard)
            var totp = new Totp(secretBytes, step: 30, mode: OtpHashMode.Sha1, totpSize: 6);
            
            // Validate with time window tolerance (±1 step = ±30 seconds)
            var window = new VerificationWindow(previous: 1, future: 1);
            
            return totp.VerifyTotp(code, out _, window);
        }
        catch (Exception)
        {
            // Invalid secret format or other errors
            return false;
        }
    }

    /// <summary>
    /// Generates a QR code image as PNG byte array
    /// </summary>
    public byte[] GenerateQrCode(string qrCodeUri)
    {
        if (string.IsNullOrWhiteSpace(qrCodeUri))
            throw new ArgumentException("QR code URI cannot be null or empty", nameof(qrCodeUri));

        try
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(qrCodeUri, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            
            // Generate PNG with reasonable size (10 pixels per module)
            return qrCode.GetGraphic(10);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to generate QR code", ex);
        }
    }
}
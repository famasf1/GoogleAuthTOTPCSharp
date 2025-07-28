using OtpNet;
using QRCoder;

namespace GoogleAuthTotpPrototype.Services;

public class TotpService : ITotpService
{
    private const string Issuer = "GoogleAuthTotpPrototype";
    private const int SecretLength = 20;

    public string GenerateSecret()
    {
        var key = KeyGeneration.GenerateRandomKey(SecretLength);
        return Base32Encoding.ToString(key);
    }

    public string GenerateQrCodeUri(string email, string secret)
    {
        return $"otpauth://totp/{Uri.EscapeDataString(Issuer)}:{Uri.EscapeDataString(email)}?secret={secret}&issuer={Uri.EscapeDataString(Issuer)}";
    }

    public string GenerateQrCodeBase64(string qrCodeUri)
    {
        if (string.IsNullOrEmpty(qrCodeUri))
        {
            throw new ArgumentException("QR code URI cannot be null or empty", nameof(qrCodeUri));
        }

        try
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(qrCodeUri, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            
            var qrCodeBytes = qrCode.GetGraphic(20);
            return Convert.ToBase64String(qrCodeBytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to generate QR code", ex);
        }
    }

    public bool ValidateTotp(string secret, string code)
    {
        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(code))
        {
            return false;
        }

        try
        {
            var secretBytes = Base32Encoding.ToBytes(secret);
            var totp = new OtpNet.Totp(secretBytes);

            // First try with a wider window to account for clock drift
            var isValid = totp.VerifyTotp(code, out long timeStepMatched, VerificationWindow.RfcSpecifiedNetworkDelay);
            
            if (!isValid)
            {
                // Try with a wider window (±2 time steps = ±60 seconds)
                var window = new VerificationWindow(previous: 2, future: 2);
                isValid = totp.VerifyTotp(code, out timeStepMatched, window);
            }

            return isValid;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public string GenerateCurrentCode(string secret)
    {
        if (string.IsNullOrEmpty(secret))
        {
            return string.Empty;
        }

        try
        {
            var secretBytes = Base32Encoding.ToBytes(secret);
            var totp = new OtpNet.Totp(secretBytes);
            return totp.ComputeTotp();
        }
        catch
        {
            return string.Empty;
        }
    }
}
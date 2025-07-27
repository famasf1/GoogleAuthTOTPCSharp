using OtpNet;

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

            // Allow a window of ±1 time step (30 seconds each) to account for clock drift
            return totp.VerifyTotp(code, out _, VerificationWindow.RfcSpecifiedNetworkDelay);
        }
        catch
        {
            return false;
        }
    }
}
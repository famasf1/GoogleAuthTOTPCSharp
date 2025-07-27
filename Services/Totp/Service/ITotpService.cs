namespace GoogleAuthTotpPrototype.Services;

public interface ITotpService
{
    /// <summary>
    /// Generates a new TOTP secret key
    /// </summary>
    /// <returns>Base32 encoded secret key</returns>
    string GenerateSecret();

    /// <summary>
    /// Generates a QR code URI for TOTP setup
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="secret">TOTP secret key</param>
    /// <returns>QR code URI string</returns>
    string GenerateQrCodeUri(string email, string secret);

    /// <summary>
    /// Validates a TOTP code against the secret
    /// </summary>
    /// <param name="secret">TOTP secret key</param>
    /// <param name="code">TOTP code to validate</param>
    /// <returns>True if code is valid, false otherwise</returns>
    bool ValidateTotp(string secret, string code);
}
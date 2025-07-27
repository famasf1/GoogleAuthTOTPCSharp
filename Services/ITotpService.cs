namespace GoogleAuthTotpPrototype.Services;

/// <summary>
/// Interface for TOTP (Time-based One-Time Password) operations
/// </summary>
public interface ITotpService
{
    /// <summary>
    /// Generates a new Base32-encoded secret for TOTP
    /// </summary>
    /// <returns>Base32-encoded secret string</returns>
    string GenerateSecret();

    /// <summary>
    /// Generates a QR code URI for TOTP setup in authenticator apps
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="secret">Base32-encoded TOTP secret</param>
    /// <returns>QR code URI string</returns>
    string GenerateQrCodeUri(string email, string secret);

    /// <summary>
    /// Validates a TOTP code against the provided secret
    /// </summary>
    /// <param name="secret">Base32-encoded TOTP secret</param>
    /// <param name="code">6-digit TOTP code to validate</param>
    /// <returns>True if the code is valid, false otherwise</returns>
    bool ValidateTotp(string secret, string code);

    /// <summary>
    /// Generates a QR code image as byte array
    /// </summary>
    /// <param name="qrCodeUri">QR code URI to encode</param>
    /// <returns>PNG image as byte array</returns>
    byte[] GenerateQrCode(string qrCodeUri);
}
namespace GoogleAuthTotpPrototype.Services.Totp.ViewModel;

public class VMPARAMTotpSetupResponse
{
    public bool IsSuccess { get; set; }
    public string Secret { get; set; } = string.Empty;
    public string QrCodeUrl { get; set; } = string.Empty;
    public string ManualEntryKey { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
}
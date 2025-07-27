namespace GoogleAuthTotpPrototype.Services.Totp.ViewModel;

public class VMPARAMTotpVerifyResponse
{
    public bool IsSuccess { get; set; }
    public bool IsValid { get; set; }
    public bool IsLockedOut { get; set; }
    public List<string> Errors { get; set; } = new();
    public string? RedirectUrl { get; set; }
}
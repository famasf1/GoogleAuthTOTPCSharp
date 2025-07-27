namespace GoogleAuthTotpPrototype.Services.Authentication.ViewModel;

public class VMPARAMLoginResponse
{
    public bool IsSuccess { get; set; }
    public bool RequiresTwoFactor { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public string? RedirectUrl { get; set; }
}
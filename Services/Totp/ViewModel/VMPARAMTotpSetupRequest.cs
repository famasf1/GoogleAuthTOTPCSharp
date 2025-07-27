using System.ComponentModel.DataAnnotations;

namespace GoogleAuthTotpPrototype.Services.Totp.ViewModel;

public class VMPARAMTotpSetupRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;
}
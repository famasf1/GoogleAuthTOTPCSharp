using System.ComponentModel.DataAnnotations;

namespace GoogleAuthTotpPrototype.Services.Totp.ViewModel;

public class VMPARAMTotpVerifyRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "TOTP code must be exactly 6 digits")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "TOTP code must contain only digits")]
    public string Code { get; set; } = string.Empty;
}
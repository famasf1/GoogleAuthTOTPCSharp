using Microsoft.AspNetCore.Identity;

namespace GoogleAuthTotpPrototype.Models;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public string? TotpSecret { get; set; }
    public bool IsTotpEnabled { get; set; }
    public DateTime? LastTotpFailure { get; set; }
    public int TotpFailureCount { get; set; }
    public DateTime? TotpLockoutEnd { get; set; }
}
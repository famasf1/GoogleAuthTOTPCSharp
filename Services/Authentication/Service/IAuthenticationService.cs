using GoogleAuthTotpPrototype.Services.Authentication.ViewModel;

namespace GoogleAuthTotpPrototype.Services;

public interface IAuthenticationService
{
    Task<VMPARAMRegisterResponse> RegisterAsync(VMPARAMRegisterRequest request);
    Task<VMPARAMLoginResponse> LoginAsync(VMPARAMLoginRequest request);
    Task SignOutAsync();
    bool IsUserSignedInAsync();
    Task<string?> GetCurrentUserIdAsync();
}
using GoogleAuthTotpPrototype.Services.Totp.ViewModel;

namespace GoogleAuthTotpPrototype.Services;

public interface ITotpManagementService
{
    Task<VMPARAMTotpSetupResponse> SetupTotpAsync(VMPARAMTotpSetupRequest request);
    Task<VMPARAMTotpVerifyResponse> VerifyTotpAsync(VMPARAMTotpVerifyRequest request);
    Task<VMPARAMTotpVerifyResponse> EnableTotpAsync(VMPARAMTotpVerifyRequest request);
    Task<bool> DisableTotpAsync(string userId);
}
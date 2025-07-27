using AutoMapper;
using GoogleAuthTotpPrototype.Models;
using GoogleAuthTotpPrototype.Services.Totp.ViewModel;

namespace GoogleAuthTotpPrototype.Services.Totp.Profile;

public class TotpProfile : AutoMapper.Profile
{
    public TotpProfile()
    {
        // ApplicationUser to TOTP setup response
        CreateMap<ApplicationUser, VMPARAMTotpSetupResponse>()
            .ForMember(dest => dest.Secret, opt => opt.MapFrom(src => src.TotpSecret))
            .ForMember(dest => dest.IsSuccess, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.QrCodeUrl, opt => opt.Ignore())
            .ForMember(dest => dest.ManualEntryKey, opt => opt.MapFrom(src => src.TotpSecret))
            .ForMember(dest => dest.Errors, opt => opt.MapFrom(src => new List<string>()));

        // ApplicationUser to TOTP verify response
        CreateMap<ApplicationUser, VMPARAMTotpVerifyResponse>()
            .ForMember(dest => dest.IsSuccess, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.IsValid, opt => opt.Ignore())
            .ForMember(dest => dest.IsLockedOut, opt => opt.MapFrom(src => src.TotpLockoutEnd.HasValue && src.TotpLockoutEnd > DateTime.UtcNow))
            .ForMember(dest => dest.Errors, opt => opt.MapFrom(src => new List<string>()))
            .ForMember(dest => dest.RedirectUrl, opt => opt.Ignore());
    }
}
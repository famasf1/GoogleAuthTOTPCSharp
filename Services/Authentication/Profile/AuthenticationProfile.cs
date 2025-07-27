using AutoMapper;
using GoogleAuthTotpPrototype.Models;
using GoogleAuthTotpPrototype.Services.Authentication.ViewModel;

namespace GoogleAuthTotpPrototype.Services.Authentication.Profile;

public class AuthenticationProfile : AutoMapper.Profile
{
    public AuthenticationProfile()
    {
        // Register request to ApplicationUser
        CreateMap<VMPARAMRegisterRequest, ApplicationUser>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Username))
            .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.Username))
            .ForMember(dest => dest.IsTotpEnabled, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TotpSecret, opt => opt.Ignore())
            .ForMember(dest => dest.LastTotpFailure, opt => opt.Ignore())
            .ForMember(dest => dest.TotpFailureCount, opt => opt.Ignore())
            .ForMember(dest => dest.TotpLockoutEnd, opt => opt.Ignore());

        // ApplicationUser to Register response
        CreateMap<ApplicationUser, VMPARAMRegisterResponse>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.UserName))
            .ForMember(dest => dest.IsSuccess, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.Errors, opt => opt.MapFrom(src => new List<string>()))
            .ForMember(dest => dest.RedirectUrl, opt => opt.Ignore());

        // ApplicationUser to Login response
        CreateMap<ApplicationUser, VMPARAMLoginResponse>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.UserName))
            .ForMember(dest => dest.RequiresTwoFactor, opt => opt.MapFrom(src => src.IsTotpEnabled))
            .ForMember(dest => dest.IsSuccess, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.Errors, opt => opt.MapFrom(src => new List<string>()))
            .ForMember(dest => dest.RedirectUrl, opt => opt.Ignore());
    }
}
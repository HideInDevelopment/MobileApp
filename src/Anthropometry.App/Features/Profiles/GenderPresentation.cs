using Anthropometry.Domain.Profiles;

namespace Anthropometry.App.Features.Profiles;

public static class GenderPresentation
{
    public static string GetIcon(ProfileGender gender)
        => gender switch
        {
            ProfileGender.Male => "♂",
            ProfileGender.Female => "♀",
            _ => "•"
        };
}

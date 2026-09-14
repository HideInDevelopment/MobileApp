using Anthropometry.Domain.Profiles;
using Anthropometry.App.Features.Profiles;

namespace Anthropometry.App.Tests.Features.Profiles;

public sealed class GenderPresentationTests
{
    [Fact]
    public void Female_uses_the_female_symbol()
        => Assert.Equal("♀", GenderPresentation.GetIcon(ProfileGender.Female));

    [Fact]
    public void Male_uses_the_male_symbol()
        => Assert.Equal("♂", GenderPresentation.GetIcon(ProfileGender.Male));
}

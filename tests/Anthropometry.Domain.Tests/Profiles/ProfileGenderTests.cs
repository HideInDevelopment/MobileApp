using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Domain.Tests.Profiles;

public sealed class ProfileGenderTests
{
    [Fact]
    public void Create_preserves_selected_gender()
    {
        var result = Profile.Create(
            "Anna",
            ValidSettings(),
            DateTimeOffset.UtcNow,
            ProfileGender.Female);

        Assert.True(result.IsSuccess);
        Assert.Equal(ProfileGender.Female, result.Value.Gender);
    }

    [Fact]
    public void Create_rejects_unknown_gender()
    {
        var result = Profile.Create(
            "Anna",
            ValidSettings(),
            DateTimeOffset.UtcNow,
            (ProfileGender)99);

        Assert.False(result.IsSuccess);
        Assert.Equal("profile.gender.invalid", result.Error!.Code);
    }

    [Fact]
    public void Rehydrate_defaults_legacy_profile_to_male()
    {
        var timestamp = DateTimeOffset.UtcNow;

        var result = Profile.Rehydrate(ProfileId.New(), "Manuel", null, timestamp, timestamp);

        Assert.True(result.IsSuccess);
        Assert.Equal(ProfileGender.Male, result.Value.Gender);
    }

    [Fact]
    public void Update_without_gender_preserves_the_existing_gender()
    {
        var profile = Profile.Create(
            "Anna",
            ValidSettings(),
            DateTimeOffset.UtcNow,
            ProfileGender.Female).Value;

        var result = profile.Update("Anna Updated", ValidSettings(), DateTimeOffset.UtcNow.AddMinutes(1));

        Assert.True(result.IsSuccess);
        Assert.Equal(ProfileGender.Female, profile.Gender);
    }

    private static ProfileSettings ValidSettings()
        => ProfileSettings.Create(180m, 35, ActivityLevel.Moderate).Value;
}

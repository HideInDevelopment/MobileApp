using Anthropometry.Domain.Common;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Domain.Tests.Profiles;

public sealed class ProfileTests
{
    [Fact]
    public void Create_rejects_blank_name()
    {
        var result = Profile.Create(" ", ValidSettings(), DateTimeOffset.UtcNow);

        Assert.False(result.IsSuccess);
        Assert.Equal("profile.name.required", result.Error!.Code);
    }

    [Fact]
    public void Create_stores_name_and_utc_timestamps()
    {
        var createdAt = new DateTimeOffset(2026, 9, 8, 12, 30, 0, TimeSpan.Zero);

        var result = Profile.Create("Manuel", ValidSettings(), createdAt);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(default, result.Value.Id);
        Assert.Equal("Manuel", result.Value.Name);
        Assert.Equal(createdAt, result.Value.CreatedAtUtc);
        Assert.Equal(createdAt, result.Value.UpdatedAtUtc);
    }

    [Fact]
    public void Rename_rejects_blank_name_without_changing_profile()
    {
        var profile = Profile.Create("Manuel", ValidSettings(), DateTimeOffset.UtcNow).Value;
        var result = profile.Update(" ", ValidSettings(), DateTimeOffset.UtcNow.AddMinutes(1));

        Assert.False(result.IsSuccess);
        Assert.Equal("profile.name.required", result.Error!.Code);
        Assert.Equal("Manuel", profile.Name);
    }

    [Fact]
    public void Settings_rejects_height_outside_metric_range()
    {
        var result = ProfileSettings.Create(49m, 35, ActivityLevel.Moderate);

        Assert.False(result.IsSuccess);
        Assert.Equal("profile.settings.height.invalid", result.Error!.Code);
    }

    [Fact]
    public void Settings_rejects_age_outside_range()
    {
        var result = ProfileSettings.Create(180m, 0, ActivityLevel.Moderate);

        Assert.False(result.IsSuccess);
        Assert.Equal("profile.settings.age.invalid", result.Error!.Code);
    }

    [Fact]
    public void Settings_rejects_unknown_activity_level()
    {
        var result = ProfileSettings.Create(180m, 35, ActivityLevel.Unknown);

        Assert.False(result.IsSuccess);
        Assert.Equal("profile.settings.activity.invalid", result.Error!.Code);
    }

    [Fact]
    public void Rehydrate_allows_missing_settings_for_legacy_profiles()
    {
        var id = ProfileId.New();
        var timestamp = DateTimeOffset.UtcNow;

        var result = Profile.Rehydrate(id, "Manuel", null, timestamp, timestamp);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Settings);
    }

    private static ProfileSettings ValidSettings()
        => ProfileSettings.Create(180m, 35, ActivityLevel.Moderate).Value;
}

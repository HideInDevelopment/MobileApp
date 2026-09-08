using Anthropometry.Domain.Common;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Domain.Tests.Profiles;

public sealed class ProfileTests
{
    [Fact]
    public void Create_rejects_blank_name()
    {
        var result = Profile.Create(" ", DateTimeOffset.UtcNow);

        Assert.False(result.IsSuccess);
        Assert.Equal("profile.name.required", result.Error!.Code);
    }

    [Fact]
    public void Create_stores_name_and_utc_timestamps()
    {
        var createdAt = new DateTimeOffset(2026, 9, 8, 12, 30, 0, TimeSpan.Zero);

        var result = Profile.Create("Manuel", createdAt);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(default, result.Value.Id);
        Assert.Equal("Manuel", result.Value.Name);
        Assert.Equal(createdAt, result.Value.CreatedAtUtc);
        Assert.Equal(createdAt, result.Value.UpdatedAtUtc);
    }

    [Fact]
    public void Rename_rejects_blank_name_without_changing_profile()
    {
        var profile = Profile.Create("Manuel", DateTimeOffset.UtcNow).Value;
        var result = profile.Rename(" ", DateTimeOffset.UtcNow.AddMinutes(1));

        Assert.False(result.IsSuccess);
        Assert.Equal("profile.name.required", result.Error!.Code);
        Assert.Equal("Manuel", profile.Name);
    }
}

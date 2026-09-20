using Anthropometry.Application.Profiles;
using Anthropometry.Application.Tests.Support;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Application.Tests.Profiles;

public sealed class ProfileUseCaseTests
{
    [Fact]
    public async Task Create_profile_persists_and_returns_dto()
    {
        var repository = new FakeProfileRepository();
        var clock = new FakeClock();
        var useCase = new CreateProfile(repository, clock);

        var result = await useCase.ExecuteAsync(
            new CreateProfileCommand("Manuel", new ProfileSettingsInput(180m, 35, ActivityLevel.Moderate)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Manuel", result.Value.Name);
        Assert.Single(repository.Items);
        Assert.Equal(clock.UtcNow, repository.Items[0].CreatedAtUtc);
    }

    [Fact]
    public async Task Update_profile_updates_repository()
    {
        var repository = new FakeProfileRepository();
        var clock = new FakeClock();
        var profile = TestData.Profile();
        repository.Items.Add(profile);

        var result = await new UpdateProfile(repository, clock).ExecuteAsync(
            profile.Id,
            "Renamed",
            new ProfileSettingsInput(181m, 36, ActivityLevel.High),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Renamed", repository.Items[0].Name);
        Assert.Equal(181m, repository.Items[0].Settings!.HeightCm);
    }

    [Fact]
    public async Task Update_profile_updates_gender()
    {
        var repository = new FakeProfileRepository();
        var clock = new FakeClock();
        var profile = TestData.Profile();
        repository.Items.Add(profile);

        var result = await new UpdateProfile(repository, clock).ExecuteAsync(
            profile.Id,
            "Anna",
            new ProfileSettingsInput(180m, 35, ActivityLevel.Moderate),
            CancellationToken.None,
            ProfileGender.Female);

        Assert.True(result.IsSuccess);
        Assert.Equal(ProfileGender.Female, repository.Items[0].Gender);
        Assert.Equal(ProfileGender.Female, result.Value.Gender);
    }

    [Fact]
    public async Task Delete_missing_profile_returns_controlled_not_found_error()
    {
        var repository = new FakeProfileRepository();

        var result = await new DeleteProfile(repository).ExecuteAsync(Anthropometry.Domain.Profiles.ProfileId.New(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("profile.notFound", result.Error!.Code);
        Assert.Equal(0, repository.DeleteCalls);
    }

    [Fact]
    public async Task Delete_existing_profile_calls_transactional_delete_once()
    {
        var repository = new FakeProfileRepository();
        var profile = TestData.Profile();
        repository.Items.Add(profile);

        var result = await new DeleteProfile(repository).ExecuteAsync(profile.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repository.DeleteCalls);
    }

    [Fact]
    public async Task Get_profiles_returns_profile_dtos()
    {
        var repository = new FakeProfileRepository();
        repository.Items.Add(TestData.Profile("Zoe"));
        repository.Items.Add(TestData.Profile("Ana"));

        var result = await new GetProfiles(repository).ExecuteAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(["Ana", "Zoe"], result.Value.Select(profile => profile.Name));
    }

    [Fact]
    public async Task Create_profile_rejects_the_second_free_profile()
    {
        var repository = new FakeProfileRepository();
        repository.Items.Add(TestData.Profile("Profile 1"));

        var result = await new CreateProfile(repository, new FakeClock()).ExecuteAsync(
            new CreateProfileCommand("Profile 2", new ProfileSettingsInput(180m, 35, ActivityLevel.Moderate)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("profile.limit.reached", result.Error!.Code);
        Assert.Single(repository.Items);
    }

    [Fact]
    public async Task Create_profile_allows_a_second_premium_profile()
    {
        var repository = new FakeProfileRepository();
        repository.Items.Add(TestData.Profile("Profile 1"));

        var result = await new CreateProfile(repository, new FakeClock(), new FakeEntitlementProvider(EntitlementTestData.Premium)).ExecuteAsync(
            new CreateProfileCommand("Profile 2", new ProfileSettingsInput(180m, 35, ActivityLevel.Moderate)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, repository.Items.Count);
    }

    [Fact]
    public async Task Create_profile_persists_name_and_settings()
    {
        var repository = new FakeProfileRepository();

        var result = await new CreateProfile(repository, new FakeClock()).ExecuteAsync(
            new CreateProfileCommand("Anna", new ProfileSettingsInput(180m, 35, ActivityLevel.Moderate), ProfileGender.Female),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Anna", result.Value.Name);
        Assert.Equal(ProfileGender.Female, result.Value.Gender);
        Assert.Equal(ProfileGender.Female, repository.Items[0].Gender);
    }

    [Fact]
    public async Task Create_profile_returns_settings_validation_error()
    {
        var repository = new FakeProfileRepository();

        var result = await new CreateProfile(repository, new FakeClock()).ExecuteAsync(
            new CreateProfileCommand("Manuel", new ProfileSettingsInput(0m, 35, ActivityLevel.Moderate)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("profile.settings.height.invalid", result.Error!.Code);
        Assert.Empty(repository.Items);
    }

    [Fact]
    public async Task Update_profile_returns_not_found_for_missing_profile()
    {
        var result = await new UpdateProfile(new FakeProfileRepository(), new FakeClock()).ExecuteAsync(
            ProfileId.New(),
            "Missing",
            new ProfileSettingsInput(180m, 35, ActivityLevel.Moderate),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("profile.notFound", result.Error!.Code);
    }
}

using Anthropometry.Application.Profiles;
using Anthropometry.Application.Tests.Support;

namespace Anthropometry.Application.Tests.Profiles;

public sealed class ProfileUseCaseTests
{
    [Fact]
    public async Task Create_profile_persists_and_returns_dto()
    {
        var repository = new FakeProfileRepository();
        var clock = new FakeClock();
        var useCase = new CreateProfile(repository, clock);

        var result = await useCase.ExecuteAsync("Manuel", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Manuel", result.Value.Name);
        Assert.Single(repository.Items);
        Assert.Equal(clock.UtcNow, repository.Items[0].CreatedAtUtc);
    }

    [Fact]
    public async Task Rename_profile_updates_repository()
    {
        var repository = new FakeProfileRepository();
        var clock = new FakeClock();
        var profile = TestData.Profile();
        repository.Items.Add(profile);

        var result = await new RenameProfile(repository, clock).ExecuteAsync(profile.Id, "Renamed", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Renamed", repository.Items[0].Name);
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
}

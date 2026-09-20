using Anthropometry.App.Features.Profiles;
using Anthropometry.App.Display;
using Anthropometry.App.Features.Help;
using Anthropometry.Application.Profiles;
using Anthropometry.Application.Common;
using Anthropometry.App.Tests.Support;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Profiles;
using CommunityToolkit.Mvvm.Input;

namespace Anthropometry.App.Tests.Features.Profiles;

public sealed class ProfileEditorViewModelTests
{
    [Fact]
    public async Task Activity_level_guidance_command_passes_the_topic_to_navigation()
    {
        var navigation = new NavigationSpy();
        var viewModel = new ProfileEditorViewModel(
            new CreateProfile(new FakeProfileRepository(), new FakeClock()),
            new UpdateProfile(new FakeProfileRepository(), new FakeClock()),
            null,
            navigation,
            TestData.LanguageService(),
            CreateDisplayPreferences());

        var command = viewModel.GetType().GetProperty("ShowGuidanceCommand")?.GetValue(viewModel)
            as IAsyncRelayCommand<GuidanceTopic>;

        Assert.NotNull(command);
        await command!.ExecuteAsync(GuidanceTopic.ActivityLevel);

        Assert.Equal(GuidanceTopic.ActivityLevel, navigation.LastGuidanceTopic);
    }

    [Fact]
    public void Selector_commands_apply_gender_and_activity_level()
    {
        var viewModel = new ProfileEditorViewModel(
            new CreateProfile(new FakeProfileRepository(), new FakeClock()),
            new UpdateProfile(new FakeProfileRepository(), new FakeClock()),
            null,
            new NavigationSpy(),
            TestData.LanguageService(),
            CreateDisplayPreferences());

        viewModel.SelectGenderCommand.Execute(ProfileGender.Female.ToString());
        viewModel.SelectActivityLevelCommand.Execute(ActivityLevel.High.ToString());

        Assert.Equal(ProfileGender.Female, viewModel.SelectedGender!.Value);
        Assert.Equal(ActivityLevel.High, viewModel.SelectedActivityLevel!.Value);
    }

    [Fact]
    public async Task Save_rejects_required_name_before_persistence()
    {
        var repository = new FakeProfileRepository();
        var viewModel = new ProfileEditorViewModel(
            new CreateProfile(repository, new FakeClock()),
            new UpdateProfile(repository, new FakeClock()),
            null,
            new NavigationSpy(),
            TestData.LanguageService(),
            CreateDisplayPreferences());
        viewModel.Name = " ";

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal("A profile name is required.", viewModel.ValidationMessage);
        Assert.Empty(repository.Items);
    }

    [Fact]
    public async Task Save_creates_profile_and_closes_editor()
    {
        var repository = new FakeProfileRepository();
        var navigation = new NavigationSpy();
        var viewModel = new ProfileEditorViewModel(
            new CreateProfile(repository, new FakeClock()),
            new UpdateProfile(repository, new FakeClock()),
            null,
            navigation,
            TestData.LanguageService(),
            CreateDisplayPreferences())
        {
            Name = "Manuel",
            HeightText = "1.8",
            AgeText = "35",
            SelectedActivityLevel = new ActivityLevelOption(ActivityLevel.Moderate, "Moderately active")
        };

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsCompleted);
        Assert.Equal("Manuel", Assert.Single(repository.Items).Name);
        Assert.Equal("Manuel", navigation.ClosedProfile!.Name);
    }

    [Fact]
    public async Task Save_shows_recoverable_error_when_persistence_fails()
    {
        var repository = new ThrowingProfileRepository();
        var viewModel = new ProfileEditorViewModel(
            new CreateProfile(repository, new FakeClock()),
            new UpdateProfile(repository, new FakeClock()),
            null,
            new NavigationSpy(),
            TestData.LanguageService(),
            CreateDisplayPreferences())
        {
            Name = "Manuel",
            HeightText = "1.8",
            AgeText = "35",
            SelectedActivityLevel = new ActivityLevelOption(ActivityLevel.Moderate, "Moderately active")
        };

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal("We couldn't save this profile. Try again.", viewModel.ErrorMessage);
        Assert.False(viewModel.IsCompleted);
    }

    [Fact]
    public async Task Save_rejects_invalid_profile_settings()
    {
        var repository = new FakeProfileRepository();
        var viewModel = new ProfileEditorViewModel(
            new CreateProfile(repository, new FakeClock()),
            new UpdateProfile(repository, new FakeClock()),
            null,
            new NavigationSpy(),
            TestData.LanguageService(),
            CreateDisplayPreferences())
        {
            Name = "Manuel",
            HeightText = "0",
            AgeText = "35",
            SelectedActivityLevel = new ActivityLevelOption(ActivityLevel.Moderate, "Moderately active")
        };

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Enter a valid height, age, and activity level.", viewModel.ValidationMessage);
        Assert.Empty(repository.Items);
    }

    [Fact]
    public async Task Save_updates_existing_profile_settings()
    {
        var repository = new FakeProfileRepository();
        var profile = TestData.Profile();
        repository.Items.Add(profile);
        var viewModel = new ProfileEditorViewModel(
            new CreateProfile(repository, new FakeClock()),
            new UpdateProfile(repository, new FakeClock()),
            new ProfileDto(profile.Id, profile.Name, new ProfileSettingsDto(180m, 35, ActivityLevel.Moderate), profile.CreatedAtUtc, profile.UpdatedAtUtc),
            new NavigationSpy(),
            TestData.LanguageService(),
            CreateDisplayPreferences())
        {
            Name = "Updated",
            HeightText = "1.81",
            AgeText = "36",
            SelectedActivityLevel = new ActivityLevelOption(ActivityLevel.High, "Highly active")
        };

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsCompleted);
        Assert.Equal("Updated", repository.Items[0].Name);
        Assert.Equal(181m, repository.Items[0].Settings!.HeightCm);
    }

    [Fact]
    public async Task Save_persists_selected_female_gender()
    {
        var repository = new FakeProfileRepository();
        var viewModel = new ProfileEditorViewModel(
            new CreateProfile(repository, new FakeClock()),
            new UpdateProfile(repository, new FakeClock()),
            null,
            new NavigationSpy(),
            TestData.LanguageService(),
            CreateDisplayPreferences())
        {
            Name = "Anna",
            HeightText = "1.8",
            AgeText = "35",
            SelectedActivityLevel = new ActivityLevelOption(ActivityLevel.Moderate, "Moderately active"),
            SelectedGender = new GenderOption(ProfileGender.Female, "Female")
        };

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal(ProfileGender.Female, Assert.Single(repository.Items).Gender);
    }

    [Fact]
    public async Task Imperial_decimal_feet_input_for_180_cm_is_saved_as_centimeters()
    {
        var repository = new FakeProfileRepository();
        var displayPreferences = CreateDisplayPreferences(DisplayPreferencesService.ImperialCode);
        var viewModel = new ProfileEditorViewModel(
            new CreateProfile(repository, new FakeClock()),
            new UpdateProfile(repository, new FakeClock()),
            null,
            new NavigationSpy(),
            TestData.LanguageService(),
            displayPreferences,
            EntitlementTestData.Premium)
        {
            Name = "Manuel",
            HeightText = "5.9055118",
            AgeText = "35",
            SelectedActivityLevel = new ActivityLevelOption(ActivityLevel.Moderate, "Moderately active")
        };

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal(180m, Assert.Single(repository.Items).Settings!.HeightCm, 6);
    }

    [Fact]
    public async Task Imperial_decimal_feet_input_is_saved_as_centimeters()
    {
        var repository = new FakeProfileRepository();
        var displayPreferences = CreateDisplayPreferences(DisplayPreferencesService.ImperialCode);
        var viewModel = new ProfileEditorViewModel(
            new CreateProfile(repository, new FakeClock()),
            new UpdateProfile(repository, new FakeClock()),
            null,
            new NavigationSpy(),
            TestData.LanguageService(),
            displayPreferences,
            EntitlementTestData.Premium)
        {
            Name = "Anna",
            HeightText = "5.1",
            AgeText = "35",
            SelectedActivityLevel = new ActivityLevelOption(ActivityLevel.Moderate, "Moderately active"),
            SelectedGender = new GenderOption(ProfileGender.Female, "Female")
        };

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal(155.448m, Assert.Single(repository.Items).Settings!.HeightCm, 3);
    }

    [Fact]
    public void Imperial_decimal_feet_is_shown_when_reopening_profile()
    {
        var profile = TestData.Profile("Anna");
        var displayPreferences = CreateDisplayPreferences(DisplayPreferencesService.ImperialCode);
        var viewModel = new ProfileEditorViewModel(
            new CreateProfile(new FakeProfileRepository(), new FakeClock()),
            new UpdateProfile(new FakeProfileRepository(), new FakeClock()),
            new ProfileDto(
                profile.Id,
                profile.Name,
                new ProfileSettingsDto(155.448m, 35, ActivityLevel.Moderate),
                profile.CreatedAtUtc,
                profile.UpdatedAtUtc,
                ProfileGender.Female),
            new NavigationSpy(),
            TestData.LanguageService(),
            displayPreferences,
            EntitlementTestData.Premium);

        Assert.Equal("5.1", viewModel.HeightText);
    }

    [Fact]
    public void Changing_height_unit_reformats_an_existing_editor_value()
    {
        var profile = TestData.Profile();
        var displayPreferences = CreateDisplayPreferences();
        var viewModel = new ProfileEditorViewModel(
            new CreateProfile(new FakeProfileRepository(), new FakeClock()),
            new UpdateProfile(new FakeProfileRepository(), new FakeClock()),
            new ProfileDto(profile.Id, profile.Name, new ProfileSettingsDto(180m, 35, ActivityLevel.Moderate), profile.CreatedAtUtc, profile.UpdatedAtUtc),
            new NavigationSpy(),
            TestData.LanguageService(),
            displayPreferences,
            EntitlementTestData.Premium);

        displayPreferences.SetMeasurementSystem(DisplayPreferencesService.ImperialCode);

        Assert.Equal("5.91", viewModel.HeightText);
        Assert.Equal("ft", viewModel.HeightUnitText);
    }

    [Fact]
    public void Metric_profile_height_is_displayed_in_meters()
    {
        var profile = TestData.Profile();
        var viewModel = new ProfileEditorViewModel(
            new CreateProfile(new FakeProfileRepository(), new FakeClock()),
            new UpdateProfile(new FakeProfileRepository(), new FakeClock()),
            new ProfileDto(profile.Id, profile.Name, new ProfileSettingsDto(180m, 35, ActivityLevel.Moderate), profile.CreatedAtUtc, profile.UpdatedAtUtc),
            new NavigationSpy(),
            TestData.LanguageService(),
            CreateDisplayPreferences());

        Assert.Equal("1.8", viewModel.HeightText);
        Assert.Equal("m", viewModel.HeightUnitText);
    }

    private static DisplayPreferencesService CreateDisplayPreferences(string? measurementSystemCode = null)
    {
        var service = new DisplayPreferencesService(new FakeDisplayPreferenceStore { MeasurementSystemCode = measurementSystemCode });
        service.Initialize();
        return service;
    }

    private sealed class FakeDisplayPreferenceStore : IDisplayPreferenceStore
    {
        public string? DateFormatCode { get; set; }

        public string? WeightUnitCode { get; set; }

        public string? HeightUnitCode { get; set; }

        public string? MeasurementSystemCode { get; set; }

        public string? GetDateFormatCode() => DateFormatCode;

        public string? GetWeightUnitCode() => WeightUnitCode;

        public string? GetHeightUnitCode() => HeightUnitCode;

        public string? GetMeasurementSystemCode() => MeasurementSystemCode;

        public void SetDateFormatCode(string code) => DateFormatCode = code;

        public void SetWeightUnitCode(string code) => WeightUnitCode = code;

        public void SetHeightUnitCode(string code) => HeightUnitCode = code;

        public void SetMeasurementSystemCode(string code) => MeasurementSystemCode = code;
    }

    private sealed class NavigationSpy : IProfileNavigation
    {
        public Anthropometry.Application.Common.ProfileDto? ClosedProfile { get; private set; }

        public GuidanceTopic? LastGuidanceTopic { get; private set; }

        public Task CreateProfileAsync() => Task.CompletedTask;

        public Task RenameProfileAsync(Anthropometry.Application.Common.ProfileDto profile) => Task.CompletedTask;

        public Task SelectProfileAsync(Anthropometry.Application.Common.ProfileDto profile) => Task.CompletedTask;

        public Task<bool> ConfirmDeleteAsync(Anthropometry.Application.Common.ProfileDto profile) => Task.FromResult(false);

        public Task CloseEditorAsync(Anthropometry.Application.Common.ProfileDto profile)
        {
            ClosedProfile = profile;
            return Task.CompletedTask;
        }

        public Task CreateMeasurementAsync(Anthropometry.Application.Common.ProfileDto profile, Anthropometry.Domain.Measurements.MeasurementType type) => Task.CompletedTask;

        public Task CancelAsync() => Task.CompletedTask;

        public Task ShowHistoryAsync(Anthropometry.Application.Common.ProfileDto profile) => Task.CompletedTask;

        public Task ShowSettingsAsync() => Task.CompletedTask;

        public Task ShowHelpAsync() => Task.CompletedTask;

        public Task ShowGuidanceAsync(GuidanceTopic topic)
        {
            LastGuidanceTopic = topic;
            return Task.CompletedTask;
        }

        public Task ExportProfileAsync(Anthropometry.Application.Common.ProfileDto profile) => Task.CompletedTask;

        public Task ImportProfileAsync() => Task.CompletedTask;
    }

}

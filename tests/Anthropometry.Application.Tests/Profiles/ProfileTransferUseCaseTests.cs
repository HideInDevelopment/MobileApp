using System.Text;
using Anthropometry.Application.Abstractions;
using Anthropometry.Application.Common;
using Anthropometry.Application.Profiles;
using Anthropometry.Application.Tests.Support;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Application.Tests.Profiles;

public sealed class ProfileTransferUseCaseTests
{
    private const string Passphrase = "correct horse battery staple";

    [Fact]
    public async Task Export_missing_profile_returns_not_found()
    {
        var repository = new FakeProfileTransferRepository();

        var result = await new ExportProfile(repository, new FakeClock(), new FakeEntitlementProvider(EntitlementTestData.Premium)).ExecuteAsync(
            new ExportProfileCommand(ProfileId.New()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("profile.notFound", result.Error!.Code);
    }

    [Fact]
    public async Task Export_returns_sanitized_protected_file()
    {
        var snapshot = CreateSnapshot("Anna / Imported");
        var repository = new FakeProfileTransferRepository { Snapshot = snapshot };

        var result = await new ExportProfile(repository, new FakeClock(), new FakeEntitlementProvider(EntitlementTestData.Premium)).ExecuteAsync(
            new ExportProfileCommand(snapshot.Profile.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("anthropometry-Anna-Imported-20260908.anthropometry", result.Value.FileName);
        Assert.NotEmpty(result.Value.Content);
        Assert.Equal(ProfileTransferProtection.TransferCodeLength, result.Value.TransferCode.Length);
        Assert.All(result.Value.TransferCode, character => Assert.InRange(character, '0', '9'));
        var unprotected = ProfileTransferProtection.Unprotect(result.Value.Content, result.Value.TransferCode);
        Assert.True(unprotected.IsSuccess);
        Assert.False(unprotected.Value.IsLegacyUnprotected);
        Assert.True(ProfileTransferCsvSerializer.Parse(new MemoryStream(unprotected.Value.CsvContent)).IsSuccess);
    }

    [Fact]
    public async Task Preview_returns_profile_name_gender_and_record_counts()
    {
        var snapshot = CreateSnapshot();
        var content = ProfileTransferProtection.Protect(
            ProfileTransferCsvSerializer.Serialize(snapshot),
            Passphrase).Value;

        var result = await new ImportProfile(new FakeProfileTransferRepository(), new FakeProfileRepository(), new FakeEntitlementProvider(EntitlementTestData.Premium))
            .PreviewAsync(content, Passphrase, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Anna", result.Value.Name);
        Assert.Equal(ProfileGender.Female, result.Value.Gender);
        Assert.Equal(2, result.Value.MeasurementCount);
        Assert.Equal(3, result.Value.CalculationResultCount);
    }

    [Fact]
    public async Task Import_creates_new_ids_and_preserves_female_history()
    {
        var source = CreateSnapshot();
        var transferRepository = new FakeProfileTransferRepository();
        var profiles = new FakeProfileRepository();
        var useCase = new ImportProfile(transferRepository, profiles, new FakeEntitlementProvider(EntitlementTestData.Premium));
        var content = ProfileTransferProtection.Protect(
            ProfileTransferCsvSerializer.Serialize(source),
            Passphrase).Value;
        var preview = (await useCase.PreviewAsync(
            content,
            Passphrase,
            CancellationToken.None)).Value;

        var result = await useCase.ExecuteAsync(preview, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(source.Profile.Id, result.Value.Profile.Id);
        Assert.Equal(ProfileGender.Female, result.Value.Profile.Gender);
        Assert.Equal(2, result.Value.MeasurementCount);
        Assert.Equal(3, result.Value.CalculationResultCount);
        Assert.NotNull(transferRepository.ImportedProfile);
        Assert.All(transferRepository.ImportedMeasurements, measurement => Assert.DoesNotContain(
            measurement.Id,
            source.Measurements.Select(item => item.Id)));
        Assert.Contains(transferRepository.ImportedMeasurements, measurement =>
            measurement.Type == MeasurementType.WeightAndSizes
            && measurement.Gender == ProfileGender.Female
            && measurement.HipCm == 42.75m);
        Assert.Contains(transferRepository.ImportedResults, calculation =>
            calculation.FormulaId == "female.body-fat.us-navy"
            && calculation.FormulaVersion == "2.0"
            && calculation.Unit == "percent");
        Assert.All(transferRepository.ImportedResults, calculation =>
            Assert.Contains(transferRepository.ImportedMeasurements, measurement => measurement.Id == calculation.MeasurementId));
    }

    [Fact]
    public async Task Import_at_profile_limit_does_not_write()
    {
        var profiles = new FakeProfileRepository();
        for (var index = 0; index < 10; index++)
        {
            profiles.Items.Add(TestData.Profile($"Existing {index}"));
        }

        var transferRepository = new FakeProfileTransferRepository();
        var useCase = new ImportProfile(transferRepository, profiles, new FakeEntitlementProvider(EntitlementTestData.Premium));
        var content = ProfileTransferProtection.Protect(
            ProfileTransferCsvSerializer.Serialize(CreateSnapshot()),
            Passphrase).Value;
        var preview = (await useCase.PreviewAsync(
            content,
            Passphrase,
            CancellationToken.None)).Value;

        var result = await useCase.ExecuteAsync(preview, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("profile.limit.reached", result.Error!.Code);
        Assert.False(transferRepository.ImportCalled);
        Assert.Equal(10, profiles.Items.Count);
    }

    [Fact]
    public async Task Preview_rejects_malformed_or_unsupported_files()
    {
        var useCase = new ImportProfile(new FakeProfileTransferRepository(), new FakeProfileRepository(), new FakeEntitlementProvider(EntitlementTestData.Premium));

        var malformed = await useCase.PreviewAsync(
            Encoding.UTF8.GetBytes("not,a,profile"),
            null,
            CancellationToken.None);
        var unsupportedContent = Encoding.UTF8.GetString(ProfileTransferCsvSerializer.Serialize(CreateSnapshot()))
            .Replace("meta,1,", "meta,9,", StringComparison.Ordinal);
        var unsupported = await useCase.PreviewAsync(
            Encoding.UTF8.GetBytes(unsupportedContent),
            null,
            CancellationToken.None);

        Assert.False(malformed.IsSuccess);
        Assert.Equal("profile.transfer.file.invalid", malformed.Error!.Code);
        Assert.False(unsupported.IsSuccess);
        Assert.Equal("profile.transfer.format.unsupported", unsupported.Error!.Code);
    }

    [Fact]
    public async Task Import_repository_failure_returns_persistence_error()
    {
        var transferRepository = new FakeProfileTransferRepository { ThrowOnImport = true };
        var useCase = new ImportProfile(transferRepository, new FakeProfileRepository(), new FakeEntitlementProvider(EntitlementTestData.Premium));
        var content = ProfileTransferProtection.Protect(
            ProfileTransferCsvSerializer.Serialize(CreateSnapshot()),
            Passphrase).Value;
        var preview = (await useCase.PreviewAsync(
            content,
            Passphrase,
            CancellationToken.None)).Value;

        var result = await useCase.ExecuteAsync(preview, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("persistence.unavailable", result.Error!.Code);
        Assert.True(transferRepository.ImportCalled);
    }

    [Fact]
    public async Task Preview_requires_a_passphrase_for_protected_content()
    {
        var useCase = new ImportProfile(new FakeProfileTransferRepository(), new FakeProfileRepository(), new FakeEntitlementProvider(EntitlementTestData.Premium));
        var content = ProfileTransferProtection.Protect(
            ProfileTransferCsvSerializer.Serialize(CreateSnapshot()),
            Passphrase).Value;

        var result = await useCase.PreviewAsync(content, null, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("profile.transfer.password.required", result.Error!.Code);
    }

    [Fact]
    public async Task Preview_marks_previous_plain_csv_as_unprotected_legacy_content()
    {
        var useCase = new ImportProfile(new FakeProfileTransferRepository(), new FakeProfileRepository(), new FakeEntitlementProvider(EntitlementTestData.Premium));

        var result = await useCase.PreviewAsync(
            ProfileTransferCsvSerializer.Serialize(CreateSnapshot()),
            null,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsLegacyUnprotected);
    }

    [Fact]
    public async Task Free_export_is_rejected_before_reading_profile_data()
    {
        var repository = new FakeProfileTransferRepository { Snapshot = CreateSnapshot() };
        var result = await new ExportProfile(repository, new FakeClock(), new FakeEntitlementProvider(EntitlementTestData.Free)).ExecuteAsync(
            new ExportProfileCommand(repository.Snapshot!.Profile.Id),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("premium.feature.required", result.Error!.Code);
        Assert.Equal(0, repository.SnapshotReads);
    }

    [Fact]
    public async Task Free_import_is_rejected_before_decrypting_file()
    {
        var useCase = new ImportProfile(
            new FakeProfileTransferRepository(),
            new FakeProfileRepository(),
            new FakeEntitlementProvider(EntitlementTestData.Free));

        var result = await useCase.PreviewAsync(
            Encoding.UTF8.GetBytes("not a protected file"),
            null,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("premium.feature.required", result.Error!.Code);
    }

    [Fact]
    public async Task Free_import_execute_is_rejected_even_for_a_valid_preview()
    {
        var transferRepository = new FakeProfileTransferRepository();
        var profiles = new FakeProfileRepository();
        var content = ProfileTransferProtection.Protect(
            ProfileTransferCsvSerializer.Serialize(CreateSnapshot()),
            Passphrase).Value;
        var premiumUseCase = new ImportProfile(
            transferRepository,
            profiles,
            new FakeEntitlementProvider(EntitlementTestData.Premium));
        var preview = (await premiumUseCase.PreviewAsync(content, Passphrase, CancellationToken.None)).Value;

        var result = await new ImportProfile(
            transferRepository,
            profiles,
            new FakeEntitlementProvider(EntitlementTestData.Free)).ExecuteAsync(preview, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("premium.feature.required", result.Error!.Code);
        Assert.False(transferRepository.ImportCalled);
    }

    private static ProfileTransferSnapshot CreateSnapshot(string name = "Anna")
    {
        var profileId = ProfileId.New();
        var timestamp = new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);
        var profile = Profile.Rehydrate(
            profileId,
            name,
            ProfileSettings.Create(178.25m, 31, ActivityLevel.High).Value,
            timestamp,
            timestamp,
            ProfileGender.Female).Value;
        var sized = Measurement.Rehydrate(
            MeasurementId.New(),
            profileId,
            new MeasurementInput(MeasurementType.WeightAndSizes, 72.5m, 178.25m, 31.5m, 84.75m, 31, ActivityLevel.High, timestamp.AddDays(1), 42.75m, ProfileGender.Female)).Value;
        var weightOnly = Measurement.Rehydrate(
            MeasurementId.New(),
            profileId,
            new MeasurementInput(MeasurementType.WeightOnly, 71.25m, 178.25m, null, null, 31, ActivityLevel.High, timestamp.AddDays(2), null, ProfileGender.Female)).Value;
        var results = new[]
        {
            CalculationResult.Create(sized.Id, CalculationType.BodyFatPercentage, new CalculationResultValue(23.45m, "percent", "female.body-fat.us-navy", "2.0"), timestamp.AddDays(1)).Value,
            CalculationResult.Create(sized.Id, CalculationType.BasalMetabolicRate, new CalculationResultValue(1488m, "kcal/day", "female.bmr.mifflin-st-jeor", "2.0"), timestamp.AddDays(1)).Value,
            CalculationResult.Create(weightOnly.Id, CalculationType.TotalDailyEnergyExpenditure, new CalculationResultValue(2300m, "kcal/day", "tdee.activity-multiplier", "1.0"), timestamp.AddDays(2)).Value
        };
        return new ProfileTransferSnapshot(profile, [sized, weightOnly], results);
    }

    private sealed class FakeProfileTransferRepository : IProfileTransferRepository
    {
        public ProfileTransferSnapshot? Snapshot { get; init; }
        public bool ThrowOnImport { get; init; }
        public bool ImportCalled { get; private set; }
        public int SnapshotReads { get; private set; }
        public Profile? ImportedProfile { get; private set; }
        public IReadOnlyList<Measurement> ImportedMeasurements { get; private set; } = [];
        public IReadOnlyList<CalculationResult> ImportedResults { get; private set; } = [];

        public Task<ProfileTransferSnapshot?> GetSnapshotAsync(ProfileId profileId, CancellationToken cancellationToken)
        {
            SnapshotReads++;
            return Task.FromResult(Snapshot?.Profile.Id == profileId ? Snapshot : null);
        }

        public Task ImportAsync(Profile profile, IReadOnlyList<Measurement> measurements, IReadOnlyList<CalculationResult> results, CancellationToken cancellationToken)
        {
            ImportCalled = true;
            if (ThrowOnImport)
            {
                throw new InvalidOperationException("Test persistence failure");
            }

            ImportedProfile = profile;
            ImportedMeasurements = measurements;
            ImportedResults = results;
            return Task.CompletedTask;
        }
    }
}

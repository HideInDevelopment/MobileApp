using Anthropometry.Application.Abstractions;
using Anthropometry.Application.Common;
using Anthropometry.Domain.Calculations;
using Anthropometry.Domain.Common;
using Anthropometry.Domain.Measurements;
using Anthropometry.Domain.Profiles;

namespace Anthropometry.Application.Profiles;

public sealed class ImportProfile
{
    private const int MaxProfiles = 4;
    private readonly IProfileTransferRepository _transferRepository;
    private readonly IProfileRepository _profileRepository;

    public ImportProfile(IProfileTransferRepository transferRepository, IProfileRepository profileRepository)
    {
        _transferRepository = transferRepository;
        _profileRepository = profileRepository;
    }

    public Task<Result<ProfileImportPreview>> PreviewAsync(
        byte[] content,
        string? passphrase,
        CancellationToken cancellationToken)
    {
        try
        {
            _ = _transferRepository;
            cancellationToken.ThrowIfCancellationRequested();
            var protectedPayload = ProfileTransferProtection.Unprotect(content, passphrase);
            if (!protectedPayload.IsSuccess)
            {
                return Task.FromResult(Result.Failure<ProfileImportPreview>(MapTransferError(protectedPayload.Error!)));
            }

            var parsed = ProfileTransferCsvSerializer.Parse(new MemoryStream(protectedPayload.Value.CsvContent, writable: false));
            if (!parsed.IsSuccess)
            {
                return Task.FromResult(Result.Failure<ProfileImportPreview>(MapCsvError(parsed.Error!)));
            }

            var document = parsed.Value;
            return Task.FromResult(Result.Success(new ProfileImportPreview(
                document,
                document.Profile.Name,
                document.Profile.Gender,
                document.Measurements.Count,
                document.Results.Count,
                protectedPayload.Value.IsLegacyUnprotected)));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Task.FromResult(Result.Failure<ProfileImportPreview>(ApplicationErrors.ProfileTransferFileInvalid));
        }
    }

    public async Task<Result<ImportedProfile>> ExecuteAsync(
        ProfileImportPreview preview,
        CancellationToken cancellationToken)
    {
        try
        {
            var profiles = await _profileRepository.GetAllAsync(cancellationToken);
            if (profiles.Count >= MaxProfiles)
            {
                return Result.Failure<ImportedProfile>(ApplicationErrors.ProfileLimitReached);
            }

            var document = preview.Document;
            var settings = CreateSettings(document.Profile);
            if (!settings.IsSuccess)
            {
                return Result.Failure<ImportedProfile>(ApplicationErrors.ProfileTransferFileInvalid);
            }

            var profile = Profile.Rehydrate(
                ProfileId.New(),
                document.Profile.Name,
                settings.Value,
                document.Profile.CreatedAtUtc,
                document.Profile.UpdatedAtUtc,
                document.Profile.Gender);
            if (!profile.IsSuccess)
            {
                return Result.Failure<ImportedProfile>(ApplicationErrors.ProfileTransferFileInvalid);
            }

            var measurementIds = new Dictionary<Guid, MeasurementId>();
            var measurements = new List<Measurement>();
            foreach (var source in document.Measurements)
            {
                var measurementId = MeasurementId.New();
                measurementIds.Add(source.SourceMeasurementId, measurementId);
                var measurement = Measurement.Rehydrate(
                    measurementId,
                    profile.Value.Id,
                    new MeasurementInput(
                        source.Type,
                        source.WeightKg,
                        source.HeightCm,
                        source.NeckCm,
                        source.AbdomenCm,
                        source.AgeYears,
                        source.ActivityLevel,
                        source.MeasuredAtUtc,
                        source.HipCm,
                        source.Gender));
                if (!measurement.IsSuccess)
                {
                    return Result.Failure<ImportedProfile>(ApplicationErrors.ProfileTransferFileInvalid);
                }

                measurements.Add(measurement.Value);
            }

            var results = new List<CalculationResult>();
            foreach (var source in document.Results)
            {
                if (!measurementIds.TryGetValue(source.SourceMeasurementId, out var measurementId))
                {
                    return Result.Failure<ImportedProfile>(ApplicationErrors.ProfileTransferFileInvalid);
                }

                var result = CalculationResult.Rehydrate(
                    CalculationResultId.New(),
                    measurementId,
                    source.CalculationType,
                    new CalculationResultValue(source.Value, source.Unit, source.FormulaId, source.FormulaVersion),
                    source.CalculatedAtUtc);
                if (!result.IsSuccess)
                {
                    return Result.Failure<ImportedProfile>(ApplicationErrors.ProfileTransferFileInvalid);
                }

                results.Add(result.Value);
            }

            await _transferRepository.ImportAsync(profile.Value, measurements, results, cancellationToken);
            return Result.Success(new ImportedProfile(
                ApplicationModels.ToDto(profile.Value),
                measurements.Count,
                results.Count));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Result.Failure<ImportedProfile>(ApplicationErrors.PersistenceUnavailable);
        }
    }

    private static Result<ProfileSettings?> CreateSettings(ProfileTransferProfile source)
    {
        if (!source.HeightCm.HasValue && !source.AgeYears.HasValue && !source.ActivityLevel.HasValue)
        {
            return Result.Success<ProfileSettings?>(null);
        }

        if (!source.HeightCm.HasValue || !source.AgeYears.HasValue || !source.ActivityLevel.HasValue)
        {
            return Result.Failure<ProfileSettings?>(new DomainError("profileTransfer.profileSettings.invalid", "Errors.ProfileTransferFileInvalid"));
        }

        var settings = ProfileSettings.Create(source.HeightCm.Value, source.AgeYears.Value, source.ActivityLevel.Value);
        return settings.IsSuccess
            ? Result.Success<ProfileSettings?>(settings.Value)
            : Result.Failure<ProfileSettings?>(settings.Error!);
    }

    private static DomainError MapTransferError(DomainError error)
        => error.Code switch
        {
            "profile.transfer.password.required" => ApplicationErrors.ProfileTransferPasswordRequired,
            "profile.transfer.authentication.failed" => ApplicationErrors.ProfileTransferAuthenticationFailed,
            "profile.transfer.passphrase.invalid" => ApplicationErrors.ProfileTransferPassphraseInvalid,
            "profile.transfer.format.unsupported" => ApplicationErrors.ProfileTransferFormatUnsupported,
            _ => ApplicationErrors.ProfileTransferFileInvalid
        };

    private static DomainError MapCsvError(DomainError error)
        => error.Code == "profileTransfer.formatVersion.unsupported"
            ? ApplicationErrors.ProfileTransferFormatUnsupported
            : ApplicationErrors.ProfileTransferFileInvalid;
}

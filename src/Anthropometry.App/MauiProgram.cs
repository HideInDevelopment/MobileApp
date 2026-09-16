using Anthropometry.App.Common;
using Anthropometry.App.Display;
using Anthropometry.App.Features.Measurements;
using Anthropometry.App.Features.Profiles;
using Anthropometry.App.Localization;
using Anthropometry.App.Theme;
using Anthropometry.Application.Abstractions;
using Anthropometry.Application.Calculations;
using Anthropometry.Application.Measurements;
using Anthropometry.Application.Profiles;
using Anthropometry.Domain.Calculations.Bmr;
using Anthropometry.Domain.Calculations.BodyFat;
using Anthropometry.Domain.Calculations.Tdee;
using Anthropometry.Infrastructure.Persistence.Migrations;
using Anthropometry.Infrastructure.Persistence.Sqlite;
using Anthropometry.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Anthropometry.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "anthropometry.db3");
        builder.Services.AddSingleton(new SqliteConnectionFactory(databasePath));
        builder.Services.AddSingleton<IMigration, Migration0001>();
        builder.Services.AddSingleton<IMigration, Migration0002>();
        builder.Services.AddSingleton<IMigration, Migration0003>();
        builder.Services.AddSingleton<IMigration, Migration0004>();
        builder.Services.AddSingleton<MigrationRunner>();
        builder.Services.AddSingleton<IProfileRepository, SqliteProfileRepository>();
        builder.Services.AddSingleton<IMeasurementRepository, SqliteMeasurementRepository>();
        builder.Services.AddSingleton<ICalculationResultRepository, SqliteCalculationResultRepository>();
        builder.Services.AddSingleton<IClock, SystemClock>();
        builder.Services.AddSingleton<ILanguagePreferenceStore, PreferencesLanguagePreferenceStore>();
        builder.Services.AddSingleton<LanguageService>();
        builder.Services.AddSingleton<IThemePreferenceStore, PreferencesThemePreferenceStore>();
        builder.Services.AddSingleton<ThemeService>();
        builder.Services.AddSingleton<IDisplayPreferenceStore, PreferencesDisplayPreferenceStore>();
        builder.Services.AddSingleton<DisplayPreferencesService>();

        builder.Services.AddSingleton<UsNavyMaleBodyFatFormula>();
        builder.Services.AddSingleton<UsNavyFemaleBodyFatFormula>();
        builder.Services.AddSingleton<MifflinStJeorMaleBmrFormula>();
        builder.Services.AddSingleton<MifflinStJeorFemaleBmrFormula>();
        builder.Services.AddSingleton<TdeeFormula>();
        builder.Services.AddSingleton<IFormulaCatalog>(services => new FormulaCatalog(
            services.GetRequiredService<UsNavyMaleBodyFatFormula>(),
            services.GetRequiredService<MifflinStJeorMaleBmrFormula>(),
            services.GetRequiredService<TdeeFormula>(),
            services.GetRequiredService<UsNavyFemaleBodyFatFormula>(),
            services.GetRequiredService<MifflinStJeorFemaleBmrFormula>()));

        builder.Services.AddTransient<CreateProfile>();
        builder.Services.AddTransient<UpdateProfile>();
        builder.Services.AddTransient<DeleteProfile>();
        builder.Services.AddTransient<GetProfiles>();
        builder.Services.AddTransient<RecordMeasurement>();
        builder.Services.AddTransient<UpdateMeasurement>();
        builder.Services.AddTransient<DeleteMeasurement>();
        builder.Services.AddTransient<GetMeasurementHistory>();
        builder.Services.AddTransient<GetMetricHistory>();
        builder.Services.AddTransient<GenerateSampleMeasurementHistory>();
        builder.Services.AddTransient<CalculateBodyFat>();
        builder.Services.AddTransient<CalculateBasalMetabolicRate>();
        builder.Services.AddTransient<CalculateTotalDailyEnergyExpenditure>();
        builder.Services.AddTransient<GetCalculationResults>();

        builder.Services.AddSingleton<MauiNavigation>();
        builder.Services.AddSingleton<IProfileNavigation>(services => services.GetRequiredService<MauiNavigation>());
        builder.Services.AddSingleton<IMeasurementNavigation>(services => services.GetRequiredService<MauiNavigation>());
        builder.Services.AddSingleton<ProfileListViewModel>();
        builder.Services.AddSingleton<ProfileListPage>();
        builder.Services.AddSingleton<AppShell>();

        return builder.Build();
    }
}

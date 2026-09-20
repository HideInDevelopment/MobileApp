using Anthropometry.App.Display;
using Anthropometry.App.Tests.Support;
using Anthropometry.Application.Entitlements;

namespace Anthropometry.App.Tests.Entitlements;

public sealed class EntitledDisplayPreferencesTests
{
    [Fact]
    public void Free_users_are_always_presented_in_metric_units()
    {
        var display = TestData.DisplayPreferences();
        display.SetMeasurementSystem(DisplayPreferencesService.ImperialCode);
        var entitled = new EntitledDisplayPreferences(display, EntitlementTestData.Free);

        Assert.Equal(DisplayPreferencesService.MetricCode, entitled.MeasurementSystemCode);
        Assert.Equal(DisplayPreferencesService.KilogramsCode, entitled.WeightUnitCode);
        Assert.Equal(DisplayPreferencesService.MetersCode, entitled.HeightUnitCode);
        Assert.Equal(DisplayPreferencesService.CentimetersCode, entitled.CircumferenceUnitCode);
        Assert.Equal(80m, entitled.ToDisplayWeight(80m));
    }

    [Fact]
    public void Premium_users_keep_the_persisted_measurement_system()
    {
        var display = TestData.DisplayPreferences();
        display.SetMeasurementSystem(DisplayPreferencesService.ImperialCode);
        var entitled = new EntitledDisplayPreferences(display, EntitlementTestData.Premium);

        Assert.Equal(DisplayPreferencesService.ImperialCode, entitled.MeasurementSystemCode);
        Assert.Equal(DisplayPreferencesService.PoundsCode, entitled.WeightUnitCode);
        Assert.Equal(DisplayPreferencesService.FeetCode, entitled.HeightUnitCode);
        Assert.Equal(DisplayPreferencesService.InchesCode, entitled.CircumferenceUnitCode);
        Assert.Equal(176.369809744m, entitled.ToDisplayWeight(80m), 9);
    }
}

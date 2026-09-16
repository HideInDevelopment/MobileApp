namespace Anthropometry.App.Display;

public interface IDisplayPreferenceStore
{
    string? GetDateFormatCode();

    string? GetWeightUnitCode();

    string? GetHeightUnitCode();

    string? GetMeasurementSystemCode();

    void SetDateFormatCode(string code);

    void SetWeightUnitCode(string code);

    void SetHeightUnitCode(string code);

    void SetMeasurementSystemCode(string code);
}

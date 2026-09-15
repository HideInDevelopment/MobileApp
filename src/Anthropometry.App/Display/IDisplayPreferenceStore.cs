namespace Anthropometry.App.Display;

public interface IDisplayPreferenceStore
{
    string? GetDateFormatCode();

    string? GetWeightUnitCode();

    string? GetHeightUnitCode();

    void SetDateFormatCode(string code);

    void SetWeightUnitCode(string code);

    void SetHeightUnitCode(string code);
}

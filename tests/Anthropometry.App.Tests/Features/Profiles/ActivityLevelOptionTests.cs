using Anthropometry.App.Features.Profiles;
using Anthropometry.App.Tests.Support;
using Anthropometry.Domain.Calculations;

namespace Anthropometry.App.Tests.Features.Profiles;

public sealed class ActivityLevelOptionTests
{
    [Fact]
    public void Create_preserves_the_activity_value_and_localizes_its_description()
    {
        var languageService = TestData.LanguageService();
        languageService.SetLanguage("es");

        var option = ActivityLevelOption.Create(ActivityLevel.Moderate, languageService);

        Assert.Equal(ActivityLevel.Moderate, option.Value);
        Assert.Equal("Actividad moderada", option.DisplayName);
        Assert.Equal("Ejercicio moderado de tres a cinco días por semana.", option.Description);
    }
}

using Anthropometry.App.Features.Help;
using Anthropometry.App.Tests.Support;

namespace Anthropometry.App.Tests.Features.Help;

public sealed class GuidanceContentTests
{
    [Fact]
    public void Every_guidance_topic_has_localized_title_and_body_in_each_supported_language()
    {
        var languageService = TestData.LanguageService();

        foreach (var language in Anthropometry.App.Localization.LanguageService.SupportedLanguages)
        {
            languageService.SetLanguage(language.Code);

            foreach (var topic in Enum.GetValues<GuidanceTopic>())
            {
                var content = GuidanceContent.Get(languageService, topic);

                Assert.False(string.IsNullOrWhiteSpace(content.Title), $"Missing title for {topic} in {language.Code}.");
                Assert.False(string.IsNullOrWhiteSpace(content.Body), $"Missing body for {topic} in {language.Code}.");
                Assert.DoesNotContain("Guidance", content.Title);
                Assert.DoesNotContain("Guidance", content.Body);
            }
        }
    }
}

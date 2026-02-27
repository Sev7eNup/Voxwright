using FluentAssertions;
using WriteSpeech.Core.Services.Modes;

namespace WriteSpeech.Tests.Services;

public class CorrectionModeDefaultsTests
{
    [Fact]
    public void BuiltInModes_HasExactly6Modes()
    {
        CorrectionModeDefaults.BuiltInModes.Should().HaveCount(6);
    }

    [Fact]
    public void BuiltInModes_AllMarkedAsBuiltIn()
    {
        CorrectionModeDefaults.BuiltInModes
            .Should().OnlyContain(m => m.IsBuiltIn);
    }

    [Fact]
    public void TranslateMode_HasTargetLanguageEnglish()
    {
        var translate = CorrectionModeDefaults.BuiltInModes
            .First(m => m.Name == "Translate");

        translate.TargetLanguage.Should().Be("English");
    }

    [Fact]
    public void EmailMode_MatchesOutlookThunderbirdOlk()
    {
        var email = CorrectionModeDefaults.BuiltInModes
            .First(m => m.Name == "Email");

        email.AppPatterns.Should().Contain("Outlook");
        email.AppPatterns.Should().Contain("Thunderbird");
        email.AppPatterns.Should().Contain("olk");
    }

    [Fact]
    public void CodeMode_MatchesIDEs()
    {
        var code = CorrectionModeDefaults.BuiltInModes
            .First(m => m.Name == "Code");

        code.AppPatterns.Should().Contain("Code");
        code.AppPatterns.Should().Contain("Cursor");
        code.AppPatterns.Should().Contain("Windsurf");
    }

    [Fact]
    public void DefaultMode_HasEmptyAppPatterns()
    {
        var defaultMode = CorrectionModeDefaults.BuiltInModes
            .First(m => m.Name == "Default");

        defaultMode.AppPatterns.Should().BeEmpty();
    }

    [Fact]
    public void TranslateMode_HasEmptyAppPatterns()
    {
        var translate = CorrectionModeDefaults.BuiltInModes
            .First(m => m.Name == "Translate");

        translate.AppPatterns.Should().BeEmpty();
    }

    [Fact]
    public void NonTranslateModes_DoNotHaveTargetLanguage()
    {
        var nonTranslate = CorrectionModeDefaults.BuiltInModes
            .Where(m => m.Name != "Translate");

        nonTranslate.Should().OnlyContain(m => m.TargetLanguage == null);
    }
}

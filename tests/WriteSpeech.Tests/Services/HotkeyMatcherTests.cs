using FluentAssertions;
using WriteSpeech.App;
using WriteSpeech.App.Services;
using WriteSpeech.Core.Configuration;

namespace WriteSpeech.Tests.Services;

public class HotkeyMatcherTests
{
    // --- ClassifyMouseMessage ---

    [Fact]
    public void ClassifyMouseMessage_XButton1Down_ReturnsXButton1True()
    {
        var (button, isDown) = HotkeyMatcher.ClassifyMouseMessage(
            NativeMethods.WM_XBUTTONDOWN, NativeMethods.XBUTTON1 << 16);

        button.Should().Be("XButton1");
        isDown.Should().BeTrue();
    }

    [Fact]
    public void ClassifyMouseMessage_XButton2Up_ReturnsXButton2False()
    {
        var (button, isDown) = HotkeyMatcher.ClassifyMouseMessage(
            NativeMethods.WM_XBUTTONUP, NativeMethods.XBUTTON2 << 16);

        button.Should().Be("XButton2");
        isDown.Should().BeFalse();
    }

    [Fact]
    public void ClassifyMouseMessage_MiddleDown_ReturnsMiddleTrue()
    {
        var (button, isDown) = HotkeyMatcher.ClassifyMouseMessage(
            NativeMethods.WM_MBUTTONDOWN, 0);

        button.Should().Be("Middle");
        isDown.Should().BeTrue();
    }

    [Fact]
    public void ClassifyMouseMessage_MiddleUp_ReturnsMiddleFalse()
    {
        var (button, isDown) = HotkeyMatcher.ClassifyMouseMessage(
            NativeMethods.WM_MBUTTONUP, 0);

        button.Should().Be("Middle");
        isDown.Should().BeFalse();
    }

    [Fact]
    public void ClassifyMouseMessage_LeftButtonDown_ReturnsNull()
    {
        var (button, _) = HotkeyMatcher.ClassifyMouseMessage(0x0201, 0); // WM_LBUTTONDOWN

        button.Should().BeNull();
    }

    [Fact]
    public void ClassifyMouseMessage_UnknownMessage_ReturnsNull()
    {
        var (button, _) = HotkeyMatcher.ClassifyMouseMessage(0x9999, 0);

        button.Should().BeNull();
    }

    // --- GetXButton ---

    [Fact]
    public void GetXButton_XButton1_ReturnsXButton1()
    {
        var result = HotkeyMatcher.GetXButton(NativeMethods.XBUTTON1 << 16);
        result.Should().Be("XButton1");
    }

    [Fact]
    public void GetXButton_XButton2_ReturnsXButton2()
    {
        var result = HotkeyMatcher.GetXButton(NativeMethods.XBUTTON2 << 16);
        result.Should().Be("XButton2");
    }

    [Fact]
    public void GetXButton_UnknownValue_ReturnsNull()
    {
        var result = HotkeyMatcher.GetXButton(0x0003 << 16);
        result.Should().BeNull();
    }

    [Fact]
    public void GetXButton_Zero_ReturnsNull()
    {
        var result = HotkeyMatcher.GetXButton(0);
        result.Should().BeNull();
    }

    // --- AreModifiersPressed ---

    [Fact]
    public void AreModifiersPressed_EmptyModifiers_ReturnsTrue()
    {
        var result = HotkeyMatcher.AreModifiersPressed("", _ => 0);
        result.Should().BeTrue();
    }

    [Fact]
    public void AreModifiersPressed_NullModifiers_ReturnsTrue()
    {
        var result = HotkeyMatcher.AreModifiersPressed(null!, _ => 0);
        result.Should().BeTrue();
    }

    [Fact]
    public void AreModifiersPressed_ControlPressed_ReturnsTrue()
    {
        short KeyState(int vk) => vk == NativeMethods.VK_LCONTROL ? unchecked((short)0x8000) : (short)0;

        var result = HotkeyMatcher.AreModifiersPressed("Control", KeyState);
        result.Should().BeTrue();
    }

    [Fact]
    public void AreModifiersPressed_RightControlPressed_ReturnsTrue()
    {
        short KeyState(int vk) => vk == NativeMethods.VK_RCONTROL ? unchecked((short)0x8000) : (short)0;

        var result = HotkeyMatcher.AreModifiersPressed("Control", KeyState);
        result.Should().BeTrue();
    }

    [Fact]
    public void AreModifiersPressed_ControlNotPressed_ReturnsFalse()
    {
        var result = HotkeyMatcher.AreModifiersPressed("Control", _ => 0);
        result.Should().BeFalse();
    }

    [Fact]
    public void AreModifiersPressed_ControlAndShift_BothPressed_ReturnsTrue()
    {
        short KeyState(int vk) => vk is NativeMethods.VK_LCONTROL or NativeMethods.VK_LSHIFT
            ? unchecked((short)0x8000) : (short)0;

        var result = HotkeyMatcher.AreModifiersPressed("Control, Shift", KeyState);
        result.Should().BeTrue();
    }

    [Fact]
    public void AreModifiersPressed_ControlAndShift_OnlyControlPressed_ReturnsFalse()
    {
        short KeyState(int vk) => vk == NativeMethods.VK_LCONTROL
            ? unchecked((short)0x8000) : (short)0;

        var result = HotkeyMatcher.AreModifiersPressed("Control, Shift", KeyState);
        result.Should().BeFalse();
    }

    [Fact]
    public void AreModifiersPressed_Alt_Pressed_ReturnsTrue()
    {
        short KeyState(int vk) => vk == NativeMethods.VK_LMENU
            ? unchecked((short)0x8000) : (short)0;

        var result = HotkeyMatcher.AreModifiersPressed("Alt", KeyState);
        result.Should().BeTrue();
    }

    // --- MatchesMouseBinding ---

    [Fact]
    public void MatchesMouseBinding_CorrectButtonAndModifiers_ReturnsTrue()
    {
        var binding = new HotkeyBinding { MouseButton = "XButton1", Modifiers = "Control" };
        short KeyState(int vk) => vk == NativeMethods.VK_LCONTROL
            ? unchecked((short)0x8000) : (short)0;

        var result = HotkeyMatcher.MatchesMouseBinding(binding, "XButton1", KeyState);
        result.Should().BeTrue();
    }

    [Fact]
    public void MatchesMouseBinding_WrongButton_ReturnsFalse()
    {
        var binding = new HotkeyBinding { MouseButton = "XButton1", Modifiers = "Control" };
        short KeyState(int vk) => vk == NativeMethods.VK_LCONTROL
            ? unchecked((short)0x8000) : (short)0;

        var result = HotkeyMatcher.MatchesMouseBinding(binding, "XButton2", KeyState);
        result.Should().BeFalse();
    }

    [Fact]
    public void MatchesMouseBinding_ModifiersNotPressed_ReturnsFalse()
    {
        var binding = new HotkeyBinding { MouseButton = "XButton1", Modifiers = "Control" };

        var result = HotkeyMatcher.MatchesMouseBinding(binding, "XButton1", _ => 0);
        result.Should().BeFalse();
    }

    [Fact]
    public void MatchesMouseBinding_NoModifiers_ButtonOnly_ReturnsTrue()
    {
        var binding = new HotkeyBinding { MouseButton = "Middle", Modifiers = "" };

        var result = HotkeyMatcher.MatchesMouseBinding(binding, "Middle", _ => 0);
        result.Should().BeTrue();
    }

    [Fact]
    public void MatchesMouseBinding_NotMouseBinding_ReturnsFalse()
    {
        var binding = new HotkeyBinding { Key = "Space", Modifiers = "Control" };

        var result = HotkeyMatcher.MatchesMouseBinding(binding, "XButton1", _ => 0);
        result.Should().BeFalse();
    }

    [Fact]
    public void MatchesMouseBinding_NullButton_ReturnsFalse()
    {
        var binding = new HotkeyBinding { MouseButton = "XButton1" };

        var result = HotkeyMatcher.MatchesMouseBinding(binding, null, _ => 0);
        result.Should().BeFalse();
    }

    // --- MatchesKeyboardBinding ---

    [Fact]
    public void MatchesKeyboardBinding_MouseBinding_ReturnsFalse()
    {
        var binding = new HotkeyBinding { MouseButton = "XButton1", Modifiers = "Control" };

        var result = HotkeyMatcher.MatchesKeyboardBinding(binding, 0x20, _ => 0);
        result.Should().BeFalse();
    }

    [Fact]
    public void MatchesKeyboardBinding_InvalidKey_ReturnsFalse()
    {
        var binding = new HotkeyBinding { Key = "InvalidKeyName", Modifiers = "Control" };

        var result = HotkeyMatcher.MatchesKeyboardBinding(binding, 0x20, _ => unchecked((short)0x8000));
        result.Should().BeFalse();
    }

    // --- MatchesKeyRelease ---

    [Fact]
    public void MatchesKeyRelease_MouseBinding_ReturnsFalse()
    {
        var binding = new HotkeyBinding { MouseButton = "XButton1" };

        var result = HotkeyMatcher.MatchesKeyRelease(binding, 0x20);
        result.Should().BeFalse();
    }

    [Fact]
    public void MatchesKeyRelease_InvalidKey_ReturnsFalse()
    {
        var binding = new HotkeyBinding { Key = "InvalidKeyName" };

        var result = HotkeyMatcher.MatchesKeyRelease(binding, 0x20);
        result.Should().BeFalse();
    }
}

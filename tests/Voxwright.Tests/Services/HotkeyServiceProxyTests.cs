using FluentAssertions;
using NSubstitute;
using Voxwright.App.Services;
using Voxwright.Core.Services.Hotkey;

namespace Voxwright.Tests.Services;

public class HotkeyServiceProxyTests
{
    private static (HotkeyServiceProxy proxy, IGlobalHotkeyService firstInner, List<IGlobalHotkeyService> created) CreateProxy(
        Action<string>? methodObserver = null)
    {
        var created = new List<IGlobalHotkeyService>();
        Func<string, IGlobalHotkeyService> factory = method =>
        {
            methodObserver?.Invoke(method);
            var inner = Substitute.For<IGlobalHotkeyService>();
            created.Add(inner);
            return inner;
        };

        var proxy = new HotkeyServiceProxy(factory, "RegisterHotKey");
        return (proxy, created[0], created);
    }

    // --- Event propagation ---

    [Fact]
    public void ToggleHotkeyPressed_FromInner_IsRaisedByProxy()
    {
        var (proxy, inner, _) = CreateProxy();
        var raised = false;
        proxy.ToggleHotkeyPressed += (_, _) => raised = true;

        inner.ToggleHotkeyPressed += Raise.Event<EventHandler>(this, EventArgs.Empty);

        raised.Should().BeTrue();
    }

    [Fact]
    public void PushToTalkHotkeyPressed_FromInner_IsRaisedByProxy()
    {
        var (proxy, inner, _) = CreateProxy();
        var raised = false;
        proxy.PushToTalkHotkeyPressed += (_, _) => raised = true;

        inner.PushToTalkHotkeyPressed += Raise.Event<EventHandler>(this, EventArgs.Empty);

        raised.Should().BeTrue();
    }

    [Fact]
    public void PushToTalkHotkeyReleased_FromInner_IsRaisedByProxy()
    {
        var (proxy, inner, _) = CreateProxy();
        var raised = false;
        proxy.PushToTalkHotkeyReleased += (_, _) => raised = true;

        inner.PushToTalkHotkeyReleased += Raise.Event<EventHandler>(this, EventArgs.Empty);

        raised.Should().BeTrue();
    }

    [Fact]
    public void EscapePressed_FromInner_IsRaisedByProxy()
    {
        var (proxy, inner, _) = CreateProxy();
        var raised = false;
        proxy.EscapePressed += (_, _) => raised = true;

        inner.EscapePressed += Raise.Event<EventHandler>(this, EventArgs.Empty);

        raised.Should().BeTrue();
    }

    [Fact]
    public void MouseButtonCaptured_FromInner_IsRaisedByProxy()
    {
        var (proxy, inner, _) = CreateProxy();
        MouseButtonCapturedEventArgs? captured = null;
        proxy.MouseButtonCaptured += (_, e) => captured = e;

        var args = new MouseButtonCapturedEventArgs("XButton1");
        inner.MouseButtonCaptured += Raise.Event<EventHandler<MouseButtonCapturedEventArgs>>(this, args);

        captured.Should().NotBeNull();
        captured!.Button.Should().Be("XButton1");
    }

    // --- SuppressActions ---

    [Fact]
    public void SuppressActions_Get_ReadsFromInner()
    {
        var (proxy, inner, _) = CreateProxy();
        inner.SuppressActions.Returns(true);

        proxy.SuppressActions.Should().BeTrue();
    }

    [Fact]
    public void SuppressActions_Set_WritesToInner()
    {
        var (proxy, inner, _) = CreateProxy();

        proxy.SuppressActions = true;

        inner.Received().SuppressActions = true;
    }

    // --- Method forwarding ---

    [Fact]
    public void Register_ForwardsToInner()
    {
        var (proxy, inner, _) = CreateProxy();
        var handle = new IntPtr(0x1234);

        proxy.Register(handle);

        inner.Received(1).Register(handle);
    }

    [Fact]
    public void Unregister_ForwardsToInner()
    {
        var (proxy, inner, _) = CreateProxy();

        proxy.Unregister();

        inner.Received(1).Unregister();
    }

    [Fact]
    public void UpdateToggleHotkey_KeyboardOnly_ForwardsToInner()
    {
        var (proxy, inner, _) = CreateProxy();

        proxy.UpdateToggleHotkey("Control, Shift", "Space");

        inner.Received(1).UpdateToggleHotkey("Control, Shift", "Space");
    }

    [Fact]
    public void UpdatePushToTalkHotkey_KeyboardOnly_ForwardsToInner()
    {
        var (proxy, inner, _) = CreateProxy();

        proxy.UpdatePushToTalkHotkey("Control", "F1");

        inner.Received(1).UpdatePushToTalkHotkey("Control", "F1");
    }

    [Fact]
    public void UpdateToggleHotkey_WithMouseButton_ForwardsToInner()
    {
        var (proxy, inner, _) = CreateProxy();

        proxy.UpdateToggleHotkey("Control", null, "XButton1");

        inner.Received(1).UpdateToggleHotkey("Control", null, "XButton1");
    }

    [Fact]
    public void UpdatePushToTalkHotkey_WithMouseButton_ForwardsToInner()
    {
        var (proxy, inner, _) = CreateProxy();

        proxy.UpdatePushToTalkHotkey("", null, "Middle");

        inner.Received(1).UpdatePushToTalkHotkey("", null, "Middle");
    }

    [Fact]
    public void RegisterEscapeHotkey_ForwardsToInner()
    {
        var (proxy, inner, _) = CreateProxy();

        proxy.RegisterEscapeHotkey();

        inner.Received(1).RegisterEscapeHotkey();
    }

    [Fact]
    public void UnregisterEscapeHotkey_ForwardsToInner()
    {
        var (proxy, inner, _) = CreateProxy();

        proxy.UnregisterEscapeHotkey();

        inner.Received(1).UnregisterEscapeHotkey();
    }

    // --- SwitchMethod ---

    [Fact]
    public void SwitchMethod_DisposesOldInner()
    {
        var (proxy, oldInner, _) = CreateProxy();

        proxy.SwitchMethod("LowLevelHook");

        oldInner.Received(1).Dispose();
    }

    [Fact]
    public void SwitchMethod_CreatesNewInnerWithRequestedMethod()
    {
        var receivedMethods = new List<string>();
        var (proxy, _, created) = CreateProxy(method => receivedMethods.Add(method));

        proxy.SwitchMethod("LowLevelHook");

        receivedMethods.Should().Equal("RegisterHotKey", "LowLevelHook");
        created.Should().HaveCount(2);
    }

    [Fact]
    public void SwitchMethod_PreservesSuppressActions()
    {
        var (proxy, oldInner, created) = CreateProxy();
        oldInner.SuppressActions.Returns(true);

        proxy.SwitchMethod("LowLevelHook");

        var newInner = created[1];
        newInner.Received().SuppressActions = true;
    }

    [Fact]
    public void SwitchMethod_AfterRegister_ReRegistersWindowHandle()
    {
        var (proxy, _, created) = CreateProxy();
        var handle = new IntPtr(0x5678);
        proxy.Register(handle);

        proxy.SwitchMethod("LowLevelHook");

        var newInner = created[1];
        newInner.Received(1).Register(handle);
    }

    [Fact]
    public void SwitchMethod_WithoutRegister_DoesNotRegisterNewInner()
    {
        var (proxy, _, created) = CreateProxy();

        proxy.SwitchMethod("LowLevelHook");

        var newInner = created[1];
        newInner.DidNotReceive().Register(Arg.Any<IntPtr>());
    }

    [Fact]
    public void SwitchMethod_AfterRegisterEscape_ReRegistersEscape()
    {
        var (proxy, _, created) = CreateProxy();
        proxy.Register(new IntPtr(0x1));
        proxy.RegisterEscapeHotkey();

        proxy.SwitchMethod("LowLevelHook");

        var newInner = created[1];
        newInner.Received(1).RegisterEscapeHotkey();
    }

    [Fact]
    public void SwitchMethod_WithoutEscapeRegistered_DoesNotRegisterEscapeOnNewInner()
    {
        var (proxy, _, created) = CreateProxy();
        proxy.Register(new IntPtr(0x1));

        proxy.SwitchMethod("LowLevelHook");

        var newInner = created[1];
        newInner.DidNotReceive().RegisterEscapeHotkey();
    }

    [Fact]
    public void SwitchMethod_AfterUnregisterEscape_DoesNotReRegisterEscape()
    {
        var (proxy, _, created) = CreateProxy();
        proxy.Register(new IntPtr(0x1));
        proxy.RegisterEscapeHotkey();
        proxy.UnregisterEscapeHotkey();

        proxy.SwitchMethod("LowLevelHook");

        var newInner = created[1];
        newInner.DidNotReceive().RegisterEscapeHotkey();
    }

    [Fact]
    public void SwitchMethod_NewInnerReceivesEvents_AfterSwitch()
    {
        var (proxy, _, created) = CreateProxy();
        proxy.SwitchMethod("LowLevelHook");
        var newInner = created[1];
        var raised = false;
        proxy.ToggleHotkeyPressed += (_, _) => raised = true;

        newInner.ToggleHotkeyPressed += Raise.Event<EventHandler>(this, EventArgs.Empty);

        raised.Should().BeTrue();
    }

    [Fact]
    public void SwitchMethod_OldInnerEventsIgnored_AfterSwitch()
    {
        var (proxy, oldInner, _) = CreateProxy();
        proxy.SwitchMethod("LowLevelHook");
        var raised = false;
        proxy.ToggleHotkeyPressed += (_, _) => raised = true;

        oldInner.ToggleHotkeyPressed += Raise.Event<EventHandler>(this, EventArgs.Empty);

        raised.Should().BeFalse();
    }

    [Fact]
    public void SwitchMethod_AfterDispose_IsNoOp()
    {
        var (proxy, _, created) = CreateProxy();
        proxy.Dispose();

        proxy.SwitchMethod("LowLevelHook");

        created.Should().HaveCount(1, "no new inner should be created after disposal");
    }

    // --- Dispose ---

    [Fact]
    public void Dispose_DisposesInner()
    {
        var (proxy, inner, _) = CreateProxy();

        proxy.Dispose();

        inner.Received(1).Dispose();
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var (proxy, inner, _) = CreateProxy();

        proxy.Dispose();
        proxy.Dispose();

        inner.Received(1).Dispose();
    }

    [Fact]
    public void Dispose_UnwiresEvents_FromInner()
    {
        var (proxy, inner, _) = CreateProxy();
        var raised = false;
        proxy.ToggleHotkeyPressed += (_, _) => raised = true;

        proxy.Dispose();
        inner.ToggleHotkeyPressed += Raise.Event<EventHandler>(this, EventArgs.Empty);

        raised.Should().BeFalse();
    }
}

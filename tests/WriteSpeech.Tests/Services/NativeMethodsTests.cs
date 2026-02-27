using FluentAssertions;

namespace WriteSpeech.Tests.Services;

public class NativeMethodsTests
{
    [Fact]
    public void PostThreadMessage_ResolvesEntryPoint()
    {
        // Call with invalid thread ID (0) — returns false but must not throw EntryPointNotFoundException
        var act = () => WriteSpeech.App.NativeMethods.PostThreadMessage(0, 0, IntPtr.Zero, IntPtr.Zero);

        act.Should().NotThrow<EntryPointNotFoundException>();
    }
}

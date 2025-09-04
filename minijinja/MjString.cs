using CsBindgen;

namespace Minijinja;

public readonly ref struct MjString
{
    public ReadOnlySpan<byte> Value { get; init; }

    internal unsafe MjString(byte* value)
    {
        Value = new ReadOnlySpan<byte>(value, InternalHelper.GetUtf8StringLength(value));
    }

    public void Dispose()
    {
        unsafe
        {
            fixed (byte* pin = Value)
            {
                NativeMethods.mj_str_free(pin);
            }
        }
    }
}

file static class InternalHelper
{
    internal static unsafe int GetUtf8StringLength(byte* utf8String)
    {
        byte* ptr = utf8String;
        int length = 0;
        while (*ptr != 0)
        {
            ptr++;
            length++;
        }
        return length;
    }
}

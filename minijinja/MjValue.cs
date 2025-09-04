namespace Minijinja;

using CsBindgen;

public readonly struct MjValue
{
    internal mj_value Value { get; private init; }

    internal mj_value_kind Kind => NativeMethods.mj_value_get_kind(Value);

    public MjValue() : this(NativeMethods.mj_value_new_undefined()) { }

    private MjValue(mj_value value) => Value = value;

    public MjValue(ReadOnlySpan<byte> bytes)
    {
        unsafe
        {
            fixed (byte* pin = bytes)
            {
                Value = NativeMethods.mj_value_new_string(pin);
            }
        }
    }

    public MjValue(Dictionary<byte[], Dictionary<byte[], byte[]>> value)
    {
        unsafe
        {
            var obj1 = NativeMethods.mj_value_new_object();
            foreach (var (firstKey, firstValue) in value)
            {
                var obj2 = NativeMethods.mj_value_new_object();
                foreach (var (secondKey, secondValue) in firstValue)
                {
                    fixed (byte* secondKeyPin = secondKey, secondValuePin = secondValue)
                    {
                        var obj3 = NativeMethods.mj_value_new_string(secondValuePin);
                        NativeMethods.mj_value_set_string_key(&obj2, secondKeyPin, obj3);
                    }
                }

                fixed (byte* firstKeyPin = firstKey)
                {
                    NativeMethods.mj_value_set_string_key(&obj1, firstKeyPin, obj2);
                }
            }
            Value = obj1;
        }
    }
}
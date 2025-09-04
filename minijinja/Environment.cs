using CsBindgen;

namespace Minijinja;

public readonly ref struct Environment
{
    internal ReadOnlySpan<mj_env> Env { get; init; }

    public Environment()
    {
        unsafe
        {
            Env = new Span<mj_env>(NativeMethods.mj_env_new(), sizeof(mj_env));
        }
    }
    public void Dispose()
    {
        unsafe
        {
            fixed (mj_env* pin = Env)
            {
                NativeMethods.mj_env_free(pin);
            }
        }
    }

    public bool AddTemplate(ReadOnlySpan<byte> name, ReadOnlySpan<byte> source)
    {
        unsafe
        {
            fixed (mj_env* env = Env)
            {
                fixed (byte* name_ = name, source_ = source)
                {
                    return NativeMethods.mj_env_add_template(env, name_, source_);
                }
            }
        }
    }

    public MjString RenderTemplate(ReadOnlySpan<byte> name, MjValue value)
    {
        unsafe
        {
            fixed (mj_env* env = Env)
            {
                fixed (byte* name_ = name)
                {
                    var v = value.Value;
                    // Console.WriteLine($"template: {Encoding.UTF8.GetString(name)}");
                    // Console.Write("value: ");

                    // NativeMethods.mj_value_dbg(v);
                    var result = NativeMethods.mj_env_render_template(env, name_, value.Value);
                    var renderString = new MjString(result);
                    // Console.WriteLine($"result: {Encoding.UTF8.GetString(renderString.Value)}");

                    return renderString;
                }
            }
        }
    }
}
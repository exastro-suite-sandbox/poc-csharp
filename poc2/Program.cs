using System.Text;
using System.Text.Unicode;
using Minijinja;

byte[][] jinja2_label_values = [
    "{{ A.chain_sum|int + 1 }}"u8.ToArray(),
    "{{ B.zabbix_clock }}"u8.ToArray(),
    "{{ A.type }}"u8.ToArray(),
    "{{ B.zabbix_eventid }}"u8.ToArray(),
    "{% if A.start_time %}{{ A.start_time }}{% else %}{{ B.zabbix_clock }}{% endif %}"u8.ToArray()
];

const int LOOPS = 1000;
// const bool JINJA2_CACHE = true;

// j2_cache = { }

main();


void main()
{
    using Minijinja.Environment environment = new();
    var start = DateTime.Now;
    Console.WriteLine($"get_unused_event start:{start:yyyy-MM-dd HH:mm:ss.fffffff}");
    for (int i = 0; i < LOOPS; i++)
    {
        foreach (var jinja2_label_value in jinja2_label_values)
        {
            environment.AddTemplate(jinja2_label_value, jinja2_label_value);
            Dictionary<byte[], Dictionary<byte[], byte[]>> dictionary = new(Exastro.Common.Utf8StringEqualityComparer.Default)
            {
                {
                    "A"u8.ToArray(),
                    new (Exastro.Common.Utf8StringEqualityComparer.Default)
                    {
                        { "chain_sum"u8.ToArray(), ConvertUtf16ToUtf8($"{i}") },
                        { "type"u8.ToArray(), "type-1"u8.ToArray() }
                    }
                },
                {
                    "B"u8.ToArray(),
                    new (Exastro.Common.Utf8StringEqualityComparer.Default)
                    {
                        { "zabbix_clock"u8.ToArray(), ConvertUtf16ToUtf8($"2025/09/05 00:00:00.{i:D4}") },
                        { "zabbix_eventid"u8.ToArray(), ConvertUtf16ToUtf8($"event-{i:D4}") }
                    }
                }
            };
            using var label_value = environment.RenderTemplate(jinja2_label_value, new MjValue(dictionary));
            // Utf8.ToUtf16() label_value.Value
        }
    }


    var finish = DateTime.Now;
    Console.WriteLine($"get_unused_event finish:{finish:yyyy-MM-dd HH:mm:ss.fffffff} ({finish - start})");
    // Console.WriteLine($"label_value:{label_value}");
}

static byte[] ConvertUtf16ToUtf8(string str)
{
    Span<byte> strUtf8 = new(new byte[Encoding.UTF8.GetByteCount(str)]);
    Utf8.FromUtf16(str, strUtf8, out _, out _);
    return strUtf8.ToArray();
}
#define ALL_SEARCH
// #define USE_HASHSET

using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Unicode;
using Exastro.Common;

const int ZABBIX_EVENTS = 30_000;
const int CONCLUSION_EVENTS = 1_000;
const int UNUSED_EVENTS = 5;

SetupInitialData(out var labeledEventsDict, out var incidentDict);
GetUnusedEvent(incidentDict, labeledEventsDict);

static void GetUnusedEvent(Dictionary<byte[], byte[][]> incidentDict, Dictionary<byte[], JsonElement> labeledEventsDict)
{
#if ALL_SEARCH
    Console.WriteLine("Use List and all search mode");
#elif USE_HASHSET
    Console.WriteLine("Use HashSet mode");
#else
    Console.WriteLine("Use List and binary search mode");
#endif

    Console.WriteLine(FormatVariableNameEqualValue(ZABBIX_EVENTS));
    Console.WriteLine(FormatVariableNameEqualValue(CONCLUSION_EVENTS));
    Console.WriteLine(FormatVariableNameEqualValue(UNUSED_EVENTS));
    var start = DateTime.Now;
    Console.WriteLine($"get_unused_event start:{start:yyyy-MM-dd HH:mm:ss.fffffff}");
    List<byte[]> unusedEventIds = [];

    // filter_match_list = {id: True for id_value_list in incident_dict.values() for id in id_value_list}
#if USE_HASHSET && !ALL_SEARCH
    HashSet<byte[]> filterMatchList = new(incidentDict.Values.SelectMany(x => x), Utf8StringEqualityComparer.Default);
#else
    List<byte[]> filterMatchList = [.. incidentDict.Values.SelectMany(x => x)];
#endif

    foreach (var (event_id, @event) in labeledEventsDict)
    {
        // readonlyの場合を想定 read/writeな場合JsonNodeになり、
        // @event?["labels"]?["_exastro_evaluated"]?.GetValue<string>() == "0"
        // となるが、パフォーマンスは落ちる
        if (!@event.GetProperty("labels"u8).GetProperty("_exastro_evaluated"u8).ValueEquals("0"u8))
        {
            continue;
        }

#if USE_HASHSET
        if (!filterMatchList.Contains(event_id))
#elif ALL_SEARCH
        if (!filterMatchList.Contains(event_id, Utf8StringEqualityComparer.Default))
#else
        if (filterMatchList.BinarySearch(event_id, Comparer<byte[]>.Create((x, y) => x.AsSpan().SequenceCompareTo(y))) < 0)
#endif
        {
            unusedEventIds.Add(event_id);
        }
    }

    var finish = DateTime.Now;
    Console.WriteLine($"get_unused_event finish:{finish:yyyy-MM-dd HH:mm:ss.fffffff} ({finish - start})");

    Console.WriteLine(FormatVariableNameEqualValue(filterMatchList.Count));
    Console.WriteLine(FormatVariableNameEqualValue(labeledEventsDict.Count));
    Console.WriteLine(FormatVariableNameEqualValue(unusedEventIds.Count));

}

static string FormatVariableNameEqualValue(int length, [CallerArgumentExpression(nameof(length))] string? name = null) => $"{name} = {length}";

static void SetupInitialData(out Dictionary<byte[], JsonElement> labeledEventsDict, out Dictionary<byte[], byte[][]> incidentDict)
{
    labeledEventsDict = new(Utf8StringEqualityComparer.Default);
    incidentDict = new(Utf8StringEqualityComparer.Default);

    var id1JsonText = """
        {
            "eventid": null,
            "source": "0",
            "object": "0",
            "objectid": "16046",
            "clock": "",
            "ns": "906955445",
            "r_eventid": "0",
            "r_clock": "0",
            "r_ns": "0",
            "correlationid": "0",
            "userid": "0",
            "name": "High CPU utilization (over 90% for 5m)",
            "acknowledged": "0",
            "severity": "2",
            "opdata": "Current utilization: 100 %",
            "suppressed": "0",
            "urls": [],
            "labels": {
                "_exastro_evaluated": null
            }
        }
        """u8;
    var id1BaseJson = JsonNode.Parse(id1JsonText);
    var fillter1Key = "fillter-1"u8.ToArray();
    List<byte[]> fillter1List = [];

    var id2JsonText = """
    {
        "eventid": null,
        "labels": {
            "_exastro_evaluated": "1"
        }
    }
    """u8;
    var id2BaseJson = JsonNode.Parse(id2JsonText);
    var fillter2Key = "fillter-2"u8.ToArray();
    List<byte[]> fillter2List = [];

    for (int i = 0; i < ZABBIX_EVENTS; i++)
    {
        var id1 = $"{i:06d}";
        Span<byte> id1Utf8 = new(new byte[6]);
        Utf8.FromUtf16(id1, id1Utf8, out _, out _);
        var id1Key = id1Utf8.ToArray();
        var exastroEvaluated = i < CONCLUSION_EVENTS ? "1" : "0";

        // labeled_events_dict: id1
        var json1 = id1BaseJson!.DeepClone();
        json1!["eventid"] = id1;
        json1!["labels"]!["_exastro_evaluated"] = exastroEvaluated;
        labeledEventsDict[id1Key] = json1!.Deserialize<JsonElement>();

        if (i < ZABBIX_EVENTS - UNUSED_EVENTS)
        {
            // incident_dict: filter-1
            fillter1List.Add(id1Key);
        }

        //
        if (exastroEvaluated == "1")
        {
            var id2 = $"{i + ZABBIX_EVENTS:06d}";
            Span<byte> id2Utf8 = new(new byte[6]);
            Utf8.FromUtf16(id2, id2Utf8, out _, out _);
            var id2Key = id2Utf8.ToArray();

            // labeled_events_dict: id2
            var json2 = id2BaseJson!.DeepClone();
            json2!["eventid"] = id2;
            labeledEventsDict[id2Key] = json2!.Deserialize<JsonElement>();

            // incident_dict: filter-2
            fillter2List.Add(id2Key);
        }
    }

    if (fillter1List.Count > 0) incidentDict[fillter1Key] = [.. fillter1List];
    if (fillter2List.Count > 0) incidentDict[fillter2Key] = [.. fillter2List];
}

/*
#
# 2025/09/02 ディクショナリ操作 POCコード
#
import datetime

ZABBIX_EVENTS = 30_000
CONCLUSION_EVENTS = 1_000
UNUSED_EVENTS = 5

incident_dict = {}
labeled_events_dict = {}


def main():
    Console.WriteLine(f'main start:{datetime.datetime.now()}')
    setup_initial_data()
    get_unused_event()
    Console.WriteLine(f'main finish:{datetime.datetime.now()}')


def get_unused_event():
    Console.WriteLine(f'get_unused_event start:{datetime.datetime.now()}')
    unused_event_ids = []

    # filter_match_list = {id: True for id_value_list in incident_dict.values() for id in id_value_list}
    filter_match_list = []
    for filter_id, id_value_list in incident_dict.items():
        if len(id_value_list) > 0:
            filter_match_list += id_value_list

    for event_id, event in labeled_events_dict.items():
        if event["labels"]["_exastro_evaluated"] != "0":
            continue
        if event_id not in filter_match_list:
            unused_event_ids.append(event_id)

    Console.WriteLine(f'get_unused_event finish:{datetime.datetime.now()}')

    Console.WriteLine(f"{len(filter_match_list)=}")
    Console.WriteLine(f"{len(labeled_events_dict)=}")
    Console.WriteLine(f"{len(unused_event_ids)=}")


def setup_initial_data():

    for i in range(ZABBIX_EVENTS):
        id1 = f"{i:06d}"
        exastro_evaluated = "1" if i < CONCLUSION_EVENTS else "0"

        labeled_events_dict[id1] = {
            "eventid": id1,
            "source": "0",
            "object": "0",
            "objectid": "16046",
            "clock": "",
            "ns": "906955445",
            "r_eventid": "0",
            "r_clock": "0",
            "r_ns": "0",
            "correlationid": "0",
            "userid": "0",
            "name": "High CPU utilization (over 90% for 5m)",
            "acknowledged": "0",
            "severity": "2",
            "opdata": "Current utilization: 100 %",
            "suppressed": "0",
            "urls": [],
            "labels": {
                "_exastro_evaluated": exastro_evaluated
            }
        }

        if i < ZABBIX_EVENTS - UNUSED_EVENTS:
            if 'fillter-1' not in incident_dict:
                incident_dict['fillter-1'] = [id1]
            else:
                incident_dict['fillter-1'].append(id1)

        if exastro_evaluated == '1':
            id2 = f"{i+ZABBIX_EVENTS:06d}"
            labeled_events_dict[id2] = {
                "eventid": id2,
                "labels": {
                    "_exastro_evaluated": "1"
                }
            }

            if 'fillter-2' not in incident_dict:
                incident_dict['fillter-2'] = [id2]
            else:
                incident_dict['fillter-2'].append(id2)
*/
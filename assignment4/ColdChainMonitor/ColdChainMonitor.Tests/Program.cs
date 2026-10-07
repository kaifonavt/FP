using System.Text;
using System.Text.Json;
using ColdChainMonitor;

var cases = new (string Name, Action Body)[]
{
    ("Index loop accepts empty input", () => Check(Pipeline.Analyze(Array.Empty<Reading>()).Length == 0)),
    ("Index loop detects a downward jump inside safe range", () => {
        var a=Pipeline.Analyze(new[]{R("A",0,8),R("A",1,3)}).Single();
        Check(a.AbruptChange && !a.OutsideRange && a.Temperature==3);
    }),
    ("Text writer flushes before return", () => {
        using var s=new MemoryStream(); Pipeline.WriteArchive(s,Sample());
        Check(Encoding.UTF8.GetString(s.ToArray()).TrimEnd().EndsWith("}"));
    }),
    ("Supplied file counts, order and both alert flags", () => {
        var a = Sample();
        Check(a.Readings.Length == 6 && a.Errors.Length == 4 && a.Alerts.Length == 2);
        Check(a.Errors.Select(e => e.LineNumber).SequenceEqual(new[] {7,8,9,10}));
        Check(a.Readings.Select(r => r.Temperature).SequenceEqual(new[] {5.2m,-18m,4m,8.7m,-12m,8m}));
        Check(a.Alerts.All(x => x.OutsideRange && x.AbruptChange));
        Check(a.Alerts.Select(x => x.SensorId).SequenceEqual(new[] {"S1","S2"}));
        Check(a.Alerts[0].Timestamp == Time(5) && a.Alerts[1].Timestamp == Time(6));
    }),
    ("Undefined numeric enum is rejected", () => {
        Check(Enum.TryParse<StorageClass>("99", out _));
        Check(!Parse("S1|2026-09-22T08:00:00Z|99|5"));
    }),
    ("Comma decimal is rejected", () => Check(!Parse(Line("S1","Cold","5,5")))),
    ("Class change uses case insensitive sensor identity", () => {
        var r = Import(Line("S1","Cold","4") + "\n" + Line("s1","Frozen","-18"));
        Check(r.Readings.Length == 1 && r.Errors.Single().LineNumber == 2);
    }),
    ("Inclusive upper boundaries are safe", () =>
        Check(Pipeline.Analyze(new[] {R("A",0,8), R("B",0,-15,StorageClass.Frozen)}).Length == 0)),
    ("Adjacent comparisons never cross sensors", () =>
        Check(Pipeline.Analyze(new[] {R("A",0,4),R("B",1,-18,StorageClass.Frozen)}).Length == 0)),
    ("Timestamp order is independent of input order", () => {
        var values = new[] {R("A",0,4),R("A",5,8.7m),R("A",10,5.2m)};
        Check(Pipeline.Analyze(values).SequenceEqual(Pipeline.Analyze(values.Reverse())));
    }),
    ("ReadReadings leaves supplied stream open", () => {
        using var s = Bytes(Line("A","Cold","4"));
        Pipeline.ReadReadings(s); Check(s.CanRead);
    }),
    ("WriteArchive leaves supplied stream open", () => {
        using var s = new MemoryStream(); Pipeline.WriteArchive(s, Sample());
        Check(s.CanWrite && s.Length > 0);
    }),
    ("Memory round trip preserves values and string enums", () => {
        var a = Sample(); using var s = new MemoryStream(); Pipeline.WriteArchive(s,a);
        var json = Encoding.UTF8.GetString(s.ToArray());
        Check(json.Contains("\"Cold\"") && json.Contains("\"Frozen\"") && json.Contains("\"createdAtUtc\""));
        s.Position = 0; Check(Pipeline.SameValues(a, Pipeline.ReadArchive(s)));
        Check(s.CanRead);
    }),
    ("Results survive disposal of the source stream", () => {
        ImportResult result;
        using (var s = Bytes(Line("A","Cold","4"))) result = Pipeline.ReadReadings(s);
        Check(result.Readings.Single().Temperature == 4);
        Check(!result.Errors.Any());
    }),
    ("Blank lines retain physical error numbers", () => {
        var r = Import("\n  \ninvalid\n" + Line("A","Cold","4"));
        Check(r.Errors.Single().LineNumber == 3 && r.Readings.Length == 1);
    }),
    ("Exactly four degree change is not abrupt", () =>
        Check(Pipeline.Analyze(new[] {R("A",0,4),R("A",1,8)}).Length == 0)),
    ("Malformed timestamps and field counts fail", () => {
        Check(!Parse("A|2026-09-22T08:00:00+00:00|Cold|4"));
        Check(!Parse("A|2026-02-30T08:00:00Z|Cold|4"));
        Check(!Parse("A|2026-09-22T08:00:00Z|Cold|4|extra"));
        Check(!Parse("A|Cold|4")); Check(!Parse(Line(" ","Cold","4")));
    }),
    ("Importer honors current stream position", () => {
        using var s = Bytes("skip\n" + Line("A","Cold","4")); s.Position = 5;
        var r = Pipeline.ReadReadings(s); Check(r.Readings.Length == 1 && r.Errors.Length == 0);
    }),
    ("JSON null is rejected", () => {
        using var s = Bytes("null"); Throws<JsonException>(() => Pipeline.ReadArchive(s));
    }),
    ("JSON helpers honor position without implicit rewind", () => {
        var a = Sample(); using var s = new MemoryStream(); s.Write(Encoding.UTF8.GetBytes("HEAD"));
        Pipeline.WriteArchive(s,a);
        Check(Encoding.UTF8.GetString(s.ToArray(),0,4) == "HEAD");
        Throws<JsonException>(() => Pipeline.ReadArchive(s));
        s.Position = 4; Check(Pipeline.SameValues(a,Pipeline.ReadArchive(s)));
    }),
    ("Fields are trimmed and enum names ignore case", () => {
        Check(Pipeline.TryParseReading(" A | 2026-09-22T08:00:00Z | cOlD | +4.5 ",out var r,out _));
        Check(r!.SensorId == "A" && r.Temperature == 4.5m && r.Timestamp.Offset == TimeSpan.Zero);
    }),
    ("Nondecimal forms are rejected", () => {
        foreach (var t in new[] {"NaN","Infinity","1e1","4-","1,000","79228162514264337593543950336"})
            Check(!Parse(Line("A","Cold",t)));
    }),
    ("First invalid line does not establish sensor class", () => {
        var r = Import(Line("A","Frozen","bad")+"\n"+Line("a","Cold","4"));
        Check(r.Readings.Single().StorageClass == StorageClass.Cold && r.Errors.Length == 1);
    }),
    ("Lower limits are inclusive and first readings can be outside", () => {
        Check(Pipeline.Analyze(new[] {R("A",0,2),R("B",0,-22,StorageClass.Frozen)}).Length == 0);
        var alerts = Pipeline.Analyze(new[] {R("A",0,1.9m),R("B",0,-22.1m,StorageClass.Frozen)});
        Check(alerts.Length == 2 && alerts.All(a => a.OutsideRange && !a.AbruptChange));
    }),
    ("Sensor identity and alert ordering are ordinal ignore case", () => {
        var alerts = Pipeline.Analyze(new[] {R("z",0,9),R("a",2,9),R("A",0,4)});
        Check(alerts.Length == 2 && alerts[0].SensorId == "a" && alerts[0].AbruptChange);
    }),
    ("Value verification notices changed collection contents", () => {
        var a=Sample(); var b=a with { Readings=a.Readings.Skip(1).ToArray() };
        Check(!Pipeline.SameValues(a,b));
        Check(!Pipeline.SameValues(a,a with {CreatedAtUtc=a.CreatedAtUtc.AddSeconds(1)}));
    })
};
int failed=0;
foreach (var (name,body) in cases) {
    try { body(); Console.WriteLine($"PASS {name}"); }
    catch (Exception ex) { failed++; Console.WriteLine($"FAIL {name}: {ex.Message}"); }
}
Console.WriteLine($"{cases.Length-failed}/{cases.Length} passed");
return failed == 0 ? 0 : 1;

static void Check(bool condition) { if (!condition) throw new Exception("Assertion failed"); }
static void Throws<T>(Action action) where T:Exception {
    try { action(); } catch(T) { return; }
    throw new Exception($"Expected {typeof(T).Name}");
}
static DateTimeOffset Time(int minute) => new(2026,9,22,8,minute,0,TimeSpan.Zero);
static Reading R(string id,int m,decimal t,StorageClass c=StorageClass.Cold) => new(id,Time(m),c,t);
static string Line(string id,string c,string t) => $"{id}|2026-09-22T08:00:00Z|{c}|{t}";
static bool Parse(string line) => Pipeline.TryParseReading(line,out _,out _);
static MemoryStream Bytes(string text) => new(Encoding.UTF8.GetBytes(text));
static ImportResult Import(string text) { using var s=Bytes(text); return Pipeline.ReadReadings(s); }
static MonitoringArchive Sample() {
    using var file=File.OpenRead(Path.Combine(AppContext.BaseDirectory,"readings.txt"));
    var r=Pipeline.ReadReadings(file);
    return new(Time(0),r.Readings,r.Errors,Pipeline.Analyze(r.Readings));
}

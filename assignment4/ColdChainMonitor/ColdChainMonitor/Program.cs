using System.Globalization;
using ColdChainMonitor;

try
{
    string inputPath = args.Length > 0 ? args[0] : "readings.txt";
    string outputPath = args.Length > 1 ? args[1] : "archive.json";
    DateTimeOffset createdAtUtc = args.Length > 2
        ? DateTimeOffset.ParseExact(args[2], "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal)
        : DateTimeOffset.UtcNow;
    ImportResult imported;
    using (FileStream input = File.OpenRead(inputPath))
        imported = Pipeline.ReadReadings(input);
    var archive = new MonitoringArchive(createdAtUtc, imported.Readings,
        imported.Errors, Pipeline.Analyze(imported.Readings));

    using (FileStream output = File.Create(outputPath)) Pipeline.WriteArchive(output, archive);
    MonitoringArchive restored;
    using (FileStream input = File.OpenRead(outputPath)) restored = Pipeline.ReadArchive(input);
    if (!Pipeline.SameValues(archive, restored)) throw new InvalidDataException("File round trip failed.");

    using var memory = new MemoryStream();
    Pipeline.WriteArchive(memory, archive);
    memory.Position = 0;
    if (!Pipeline.SameValues(archive, Pipeline.ReadArchive(memory)))
        throw new InvalidDataException("Memory round trip failed.");

    Console.WriteLine($"Valid readings: {archive.Readings.Length}");
    Console.WriteLine($"Import errors: {archive.Errors.Length}");
    Console.WriteLine($"Alerts: {archive.Alerts.Length}");
    foreach (var error in archive.Errors) Console.WriteLine($"Line {error.LineNumber}: {error.Message}");
    foreach (var alert in archive.Alerts)
        Console.WriteLine(FormattableString.Invariant(
            $"{alert.SensorId} {alert.Timestamp:HH:mm} {alert.Temperature} OutsideRange={alert.OutsideRange} AbruptChange={alert.AbruptChange}"));
    Console.WriteLine("File and memory round trips verified by value.");
    return 0;
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
    or System.Text.Json.JsonException or FormatException)
{
    Console.Error.WriteLine($"Processing failed: {ex.Message}");
    return 1;
}
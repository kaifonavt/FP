using DroneEnergyControl.Application;
using DroneEnergyControl.Domain;

var units = new Dictionary<string, BatteryUnit>(StringComparer.OrdinalIgnoreCase)
{
    ["D1"] = new BatteryUnit("D1", 100m, 60m),
    ["D2"] = new BatteryUnit("D2", 80m, 70m),
    ["D3"] = new BatteryUnit("D3", 50m, 10m)
};

string[] rawCommands =
{
    "C001|Charge|D1|-|15",
    "C002|Consume|D2|-|20",
    "C003|Transfer|D1|D3|25",
    "C004|Transfer|D2|D3|20",
    "C005|99|D1|-|5",
    "c003|Consume|D1|-|10",
    "C006|Transfer|D1|D1|5",
    "C007|Charge|D3|-|-1",
    "C008|Consume|D3|-|35"
};

var results = CommandProcessor.ExecuteAll(rawCommands, units);
foreach (var result in results)
    Console.WriteLine($"{result.CommandId ?? "?"}: {result.Classification} - {result.Reason}");

Console.WriteLine();
Console.WriteLine("Final charges:");
foreach (var unit in units.Values.OrderBy(x => x.Id))
    Console.WriteLine($"{unit.Id} = {unit.Charge}");

Console.WriteLine();
Console.WriteLine($"Summary: Raw commands = {rawCommands.Length}; Applied = {results.Count(x => x.Classification == ResultClassification.Applied)}; Rejected valid commands = {results.Count(x => x.Classification == ResultClassification.Rejected)}; Invalid input = {results.Count(x => x.Classification == ResultClassification.Invalid)}");





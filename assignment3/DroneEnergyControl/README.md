# Drone Energy Control

## Build and run

```text
dotnet run --project src/DroneEnergyControl/DroneEnergyControl.csproj
```

The supplied input produces 4 applied commands, 3 rejected valid commands, and 2 invalid inputs. Final charges are `D1 = 50`, `D2 = 50`, and `D3 = 0`.

## Design explanations

1. Pure logic includes parsing, validation decisions, and result creation. Mutation is allowed only inside `BatteryUnit`.
2. `IEnergyCommand.Execute` keeps each command's behavior beside its data and lets the processor treat all commands uniformly. A central runtime-type switch would make the processor know every command type and would become harder to extend.
3. `TransferTo` checks same-unit identity, source balance, and target capacity before changing either charge. Only after every check succeeds does it subtract from the source and add to the target.
4. Duplicate checking happen before `Execute`; duplicate command must not cange state before it is recognized as a duplicate.
5. `yield return` in `ExecuteAll` make execution lazy. 
6. A second reference points to the same mutable `BatteryUnit`, so it observes later changes. A snapshot must store the numeric charge value before the operation.
7. `BatteryUnit` keeps valuse non-empty, positive capacity, and charge within `[0, Capacity]`. 

## Debugging notes

### Numeric enum parsing

The faulty `Enum.TryParse` fragment accepts numeric text such as `99` even though `99` is not a declared `CommandKind`. The parser now compares the trimmed text with each declared enum name using a case-insensitive comparison, so only `Charge`, `Consume`, and `Transfer` are valid.

```csharp
private static bool TryParseKind(string text, out CommandKind kind)
    {
        foreach (var value in Enum.GetValues<CommandKind>())
        {
            if (string.Equals(value.ToString(), text, StringComparison.OrdinalIgnoreCase))
            {
                kind = value;
                return true;
            }
        }

        kind = default;
        return false;
    }
```

### Partial transfer mutation

The faulty transfer subtracts from the source before checking the target. If the target exceed capacity, it returns `false` after the source has already changed. The fixed `TransferTo` validates both balances first, then performs both mutations together.

```csharp
public bool TransferTo(BatteryUnit target, decimal amount)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (ReferenceEquals(this, target) || amount < 0 || Charge < amount || target.Charge + amount > target.Capacity)
            return false;

        Charge -= amount;
        target.Charge += amount;
        return true;
    }
```

### Duplicate checked too late

The faulty loop calls `Execute` before adding the ID to the set. A duplicate therefore mutates state before it is noticed. The fixed processor uses `HashSet<string>(StringComparer.OrdinalIgnoreCase)` and adds/checks the ID before calling `Execute`.

```csharp
var results = new List<CommandResult>();
        var processedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            if (!CommandParser.TryParse(line, out IEnergyCommand? command, out CommandResult? invalidResult))
            {
                results.Add(invalidResult!);
                continue;
            }

            if (!processedIds.Add(command!.CommandId))
            {
                results.Add(CommandResult.Rejected(command.CommandId, "Duplicate command ID."));
                continue;
            }

            results.Add(command.Execute(units));
        }
```

## Project structure

- `src/DroneEnergyControl/Domain` contains the mutable battery type and result data.
- `src/DroneEnergyControl/Commands` contains the interface and command implementations.
- `src/DroneEnergyControl/Application` contains parsing and eager processing.
- `tests/DroneEnergyControl.Tests` contains the automated tests.

using System.Globalization;

namespace ColdChainMonitor;

public static partial class Pipeline
{
    public static bool TryParseReading(string line, out Reading? reading, out string error)
    {
        reading = null;
        error = string.Empty;
        string[] parts = line.Split('|', StringSplitOptions.TrimEntries);
        if (parts.Length != 4) error = "A reading needs four pipe-separated values.";
        else if (string.IsNullOrEmpty(parts[0])) error = "Missing sensor identifier.";
        if (error.Length != 0) return false;

        bool validTime = DateTimeOffset.TryParseExact(parts[1], "yyyy-MM-dd'T'HH:mm:ss'Z'",
            CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out DateTimeOffset time);
        bool validClass = Enum.TryParse(parts[2], true, out StorageClass kind) && Enum.IsDefined(kind);
        bool validNumber = decimal.TryParse(parts[3],
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out decimal value);
        error = !validTime ? "Timestamp must use the exact UTC format."
            : !validClass ? "Storage class is not declared."
            : !validNumber ? "Temperature must be a decimal with a dot." : string.Empty;
        if (error.Length != 0) return false;
        reading = new Reading(parts[0], time, kind, value);
        return true;
    }
}

using System.Text.Json;

namespace BuildingSecurity;

public static class ReportExporter
{
    public static Result<string, AppError> Save(ReportBundle report, string path)
    {
        string? temporary = null;
        try
        {
            if (string.IsNullOrWhiteSpace(path))
                return Result<string, AppError>.Failure(new("InvalidPath", "An export path is required."));
            var target = Path.GetFullPath(path);
            temporary = target + $".{Guid.NewGuid():N}.tmp";
            // Own and close the stream before replacing the destination, including on exceptions.
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                JsonSerializer.Serialize(stream, report, new JsonSerializerOptions { WriteIndented = true });
            File.Move(temporary, target, overwrite: true);
            temporary = null;
            return Result<string, AppError>.Success(target);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return Result<string, AppError>.Failure(new("ReportWriteError", $"Export '{path}': {error.Message}"));
        }
        finally
        {
            if (temporary is not null)
            {
                // Preserve the original failure if the OS also refuses cleanup.
                try { File.Delete(temporary); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }
}

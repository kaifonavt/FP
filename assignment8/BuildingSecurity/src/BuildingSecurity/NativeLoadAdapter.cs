using System.Runtime.InteropServices;

namespace BuildingSecurity;

public sealed class NativeLoadAdapter
{
    private readonly Func<int, int, int> call;
    public NativeLoadAdapter() : this(SecurityLoadPercent) { }
    // The injectable native boundary permits controlled loader failures even after a DLL is loaded.
    public NativeLoadAdapter(Func<int, int, int> call) => this.call = call;

    [DllImport("security_native", EntryPoint = "security_load_percent", ExactSpelling = true,
        CallingConvention = CallingConvention.Cdecl)]
    private static extern int SecurityLoadPercent(int activeGuests, int capacity);

    public Result<int, AppError> Calculate(int activeGuests, int capacity)
    {
        if (activeGuests < 0 || capacity < 0)
            return Result<int, AppError>.Failure(new("InvalidLoadInput", "Guest count and capacity must be nonnegative."));
        try
        {
            var percent = call(activeGuests, capacity);
            return percent is >= 0 and <= 100
                ? Result<int, AppError>.Success(percent)
                : Result<int, AppError>.Failure(new("NativeCalculationFailed", "Native load percentage must be 0..100."));
        }
        catch (Exception error) when (error is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            return Result<int, AppError>.Failure(new("NativeUnavailable",
                $"Cannot call security_native (Windows x64). Run native/build-native.ps1 and rebuild. {error.Message}"));
        }
    }
}

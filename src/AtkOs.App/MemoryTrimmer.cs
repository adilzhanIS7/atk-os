using System.Runtime;
using System.Runtime.InteropServices;

namespace AtkOs.App;

/// <summary>Gives memory back to the OS after the window is closed and the app idles in the tray.</summary>
internal static partial class MemoryTrimmer
{
    /// <param name="delayMs">Wait so the window's resources get finalized first.</param>
    public static void Trim(int delayMs = 1500)
    {
        Task.Delay(delayMs).ContinueWith(_ =>
        {
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();

            if (OperatingSystem.IsWindows())
                SetProcessWorkingSetSize(GetCurrentProcess(), -1, -1);
        });
    }

    [LibraryImport("kernel32.dll")]
    private static partial nint GetCurrentProcess();

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetProcessWorkingSetSize(nint process, nint min, nint max);
}

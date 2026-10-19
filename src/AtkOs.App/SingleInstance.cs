using System.IO.Pipes;

namespace AtkOs.App;

/// <summary>
/// Keeps one copy of the app per user. A second launch asks the running copy
/// to show its window (through a named pipe) and then exits.
/// </summary>
public sealed class SingleInstance : IDisposable
{
    private static readonly string Name = "atk-os-" + Sanitize(Environment.UserName);

    private readonly Mutex _mutex;
    private readonly CancellationTokenSource _stop = new();

    private SingleInstance(Mutex mutex) => _mutex = mutex;

    /// <summary>Returns null when another copy is already running.</summary>
    public static SingleInstance? TryAcquire()
    {
        var mutex = new Mutex(true, Name, out var createdNew);
        if (createdNew) return new SingleInstance(mutex);

        // The previous copy may be shutting down right now: wait a moment for the mutex.
        try
        {
            if (mutex.WaitOne(TimeSpan.FromMilliseconds(300))) return new SingleInstance(mutex);
        }
        catch (AbandonedMutexException)
        {
            return new SingleInstance(mutex); // the previous copy crashed; the mutex is ours now
        }

        mutex.Dispose();
        return null;
    }

    /// <summary>Asks the running copy to show its window.</summary>
    public static void SignalRunning()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", Name, PipeDirection.Out);
            client.Connect(2000);
            client.WriteByte(1);
        }
        catch (Exception)
        {
            // The running copy is still starting or is busy; nothing else to do.
        }
    }

    /// <summary>Calls <paramref name="onShow"/> (on a background thread) whenever another launch signals.</summary>
    public void Listen(Action onShow)
    {
        var thread = new Thread(() =>
        {
            while (!_stop.IsCancellationRequested)
            {
                try
                {
                    using var server = new NamedPipeServerStream(Name, PipeDirection.In, 1);
                    server.WaitForConnectionAsync(_stop.Token).GetAwaiter().GetResult();
                    if (server.ReadByte() == 1) onShow();
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception)
                {
                    Thread.Sleep(500);
                }
            }
        })
        {
            IsBackground = true,
            Name = "single-instance",
        };
        thread.Start();
    }

    public void Dispose()
    {
        _stop.Cancel();
        try { _mutex.ReleaseMutex(); } catch (ApplicationException) { }
        _mutex.Dispose();
    }

    private static string Sanitize(string s) =>
        new(s.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
}

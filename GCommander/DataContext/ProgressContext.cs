using System.ComponentModel;
using Gtk4DotNet;

class ProgressContext : INotifyPropertyChanged
{
    public static ProgressContext Instance = new();

    public bool IsRunning
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                Gtk.BeginInvoke(200, async () =>
                {
                    await Task.Delay(6000);
                    if (!IsRunning)
                        CopyProgress = null;
                });
            }
        }
    }

    public CopyProgress? CopyProgress
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                Gtk.BeginInvoke(200, () => OnChanged(nameof(CopyProgress)));
            }
        }
    }

    public static object GetFraction(object? copyProgress)
    {
        var cp = copyProgress as CopyProgress;
        return cp != null
            ? cp.CurrentMaxBytes != 0
            ? (double)cp.CurrentBytes / cp.CurrentMaxBytes
            : 0
            : 0;
    }

    public static object GetTotalFraction(object? progress)
    {
        var cp = progress as CopyProgress;
        return cp != null
            ? ((double)cp.TotalBytes + (double)cp.CurrentBytes) / (double)cp.TotalMaxBytes
            : 0;
    }

    public static object GetEstimatedDuration(object? copyProgress)
    {
        var cp = copyProgress as CopyProgress;
        var divisor = (double)GetTotalFraction(copyProgress);
        return cp != null && cp.Duration > ThreeSeconds && divisor != 0
            ? (cp.Duration / divisor) - cp.Duration
            : TimeSpan.FromMilliseconds(0);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    void OnChanged(string name) => PropertyChanged?.Invoke(this, new(name));

    readonly static TimeSpan ThreeSeconds = TimeSpan.FromSeconds(3);
}

record CopyProgress(
    string Title,
    string Name,
    int TotalCount,
    int CurrentCount,
    long TotalMaxBytes,
    long TotalBytes,
    long CurrentMaxBytes,
    long CurrentBytes,
    bool IsRunning,
    TimeSpan Duration
);
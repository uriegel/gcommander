using Gtk4DotNet;
using System.ComponentModel;
using System.Reactive.Linq;

class Viewer : Stack
{
    public Viewer(Builder builder, string name, nint parent)
        : base(builder, name, widget => ReplacePlaceHolder(parent, widget))
    {
        Visible = false;

        this["visible"].OnNotify += () =>
        {
            if (Visible)
            {
                if (IsImage(fileName))
                {
                    image.Visible = true;
                    image.SetFileName(fileName ?? "");
                    video.Visible = false;
                    video.SetFileName("");
                }
                else if (IsVideo(fileName))
                {
                    image.Visible = false;
                    image.SetFileName("");
                    video.Visible = true;
                    video.SetFileName(fileName ?? "");
                }
                else
                {
                    image.Visible = false;
                    image.SetFileName("");
                    video.Visible = false;
                    video.SetFileName("");
                }
            }
            else
            {
                image.Visible = false;
                image.SetFileName("");
                video.Visible = false; 
                video.SetFileName("");                   
            }
        };

        var observer = Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                handler => MainContext.Instance.PropertyChanged += handler,
                handler => MainContext.Instance.PropertyChanged -= handler)
            .Where(e => e.EventArgs.PropertyName == nameof(MainContext.SelectedPath))
            .Select(n => MainContext.Instance.SelectedPath)
            .Throttle(TimeSpan.FromMilliseconds(150));

        imageObserver = observer.
            Where(IsImage)
            .Subscribe(file =>
            {
                fileName = file;
                video.Visible = false; 
                video.SetFileName("");                   
                if (!Visible)
                    return;
                image.Visible = true;
                image.SetFileName(file ?? "");
            });
                

        videoObserver = observer.
            Where(IsVideo)
            .Subscribe(file =>
            {
                fileName = file;
                image.SetFileName("");
                image.Visible = false;
                if (!Visible)
                    return;
                video.Visible = true;
                video.SetFileName(file ?? "");
            });

        noObserver = observer.
            Where(IsNotViewable)
            .Subscribe(_ =>
            {
                fileName = null;
                if (!Visible)
                    return;
                image.SetFileName("");
                image.Visible = false;
                video.Visible = false; 
                video.SetFileName("");                   
            });

        OnFinalize(() =>
        {
            imageObserver.Dispose();
            videoObserver.Dispose();
            noObserver.Dispose();
        });
    }

    static bool IsImage(string? file) 
        => file?.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase) == true
        || file?.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase) == true
        || file?.EndsWith(".heic", StringComparison.InvariantCultureIgnoreCase) == true;

    static bool IsVideo(string? file)
        => file?.EndsWith(".mp4", StringComparison.InvariantCultureIgnoreCase) == true
        || file?.EndsWith(".mkv", StringComparison.InvariantCultureIgnoreCase) == true;

    static bool IsNotViewable(string? file) => !IsVideo(file) && !IsImage(file);

    static void ReplacePlaceHolder(nint parent, nint widget)
        => parent.PanedSetEndChild(widget);

    [Widget]
    readonly Picture image = null!;

    [Widget]
    readonly Video video = null!;

    readonly IDisposable imageObserver;
    readonly IDisposable videoObserver;
    readonly IDisposable noObserver;

    string? fileName;
}
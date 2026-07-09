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
                }
                else
                {
                    image.Visible = false;
                    image.SetFileName("");
                }
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
                if (!Visible)
                    return;
                image.Visible = true;
                image.SetFileName(file ?? "");
            });
                

        noObserver = observer.
            Where(IsNotImage)
            .Subscribe(_ =>
            {
                fileName = null;
                if (!Visible)
                    return;
                image.SetFileName("");
                image.Visible = false;
            });

        OnFinalize(() =>
        {
            imageObserver.Dispose();
            noObserver.Dispose();
        });
    }

    static bool IsImage(string? file) 
        => file?.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase) == true
        || file?.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase) == true
        || file?.EndsWith(".heic", StringComparison.InvariantCultureIgnoreCase) == true;

    static bool IsNotImage(string? file) => !IsImage(file);
    static void ReplacePlaceHolder(nint parent, nint widget)
        => parent.PanedSetEndChild(widget);

    [Widget]
    readonly Picture image = null!;

    readonly IDisposable imageObserver;
    readonly IDisposable noObserver;

    string? fileName;
}
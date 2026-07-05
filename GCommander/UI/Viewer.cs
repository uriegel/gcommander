using Gtk4DotNet;
using System.ComponentModel;
using System.Reactive.Linq;

class Viewer : Stack
{
    public Viewer(Builder builder, string name, nint parent)
        : base(builder, name, widget => ReplacePlaceHolder(parent, widget))
    {
        Visible = false;
        var observer = Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                handler => MainContext.Instance.PropertyChanged += handler,
                handler => MainContext.Instance.PropertyChanged -= handler)
            .Where(e => e.EventArgs.PropertyName == nameof(MainContext.SelectedPath))
            .Select(n => MainContext.Instance.SelectedPath)
            .Throttle(TimeSpan.FromMilliseconds(40));

        imageObserver = observer.
            Where(IsImage)
            .Subscribe(file =>
            {
                image.Visible = true;
                image.SetFileName(file ?? "");
            });
                

        noObserver = observer.
            Where(IsNotImage)
            .Subscribe(_ =>
            {
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
        || file?.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase) == true;

    static bool IsNotImage(string? file) => !IsImage(file);
    static void ReplacePlaceHolder(nint parent, nint widget)
        => parent.PanedSetEndChild(widget);

    [Widget]
    readonly Picture image = null!;

    readonly IDisposable imageObserver;
    readonly IDisposable noObserver;
}
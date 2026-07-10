using Gtk4DotNet;
using System.ComponentModel;
using System.Reactive.Linq;

class Viewer : Stack
{
    public Viewer(Builder builder, string name, nint parent)
        : base(builder, name, widget => ReplacePlaceHolder(parent, widget))
    {
        Visible = false;
        location.LoadUri("https://uriegel.de");


        this["visible"].OnNotify += () =>
        {
            if (Visible)
            {
                if (IsImage(fileName))
                {
                    video.Visible = false;
                    video.SetFileName("");
                    webview.Visible = false;
                    webview.LoadUri("about:blank");
                    imageContainer.Visible = true;
                    image.SetFileName(fileName ?? "");
                }
                else if (IsVideo(fileName))
                {
                    imageContainer.Visible = false;
                    image.SetFileName("");
                    webview.Visible = false;
                    webview.LoadUri("about:blank");
                    video.Visible = true;
                    video.SetFileName(fileName ?? "");
                }
                else if (IsPdf(fileName))
                {
                    imageContainer.Visible = false;
                    image.SetFileName("");
                    video.Visible = false;
                    video.SetFileName("");
                    webview.Visible = true;
                    webview.LoadUri($"file://{fileName ?? ""}");
                }
                else
                {
                    imageContainer.Visible = false;
                    image.SetFileName("");
                    video.Visible = false;
                    video.SetFileName("");
                    webview.Visible = false;
                    webview.LoadUri("about:blank");
                }
            }
            else
            {
                imageContainer.Visible = false;
                image.SetFileName("");
                video.Visible = false; 
                video.SetFileName("");                   
                webview.Visible = false;
                webview.LoadUri("about:blank");
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
                webview.Visible = false;
                webview.LoadUri("about:blank");
                imageContainer.Visible = true;
                image.SetFileName(file ?? "");
            });
                

        videoObserver = observer.
            Where(IsVideo)
            .Subscribe(file =>
            {
                fileName = file;
                image.SetFileName("");
                imageContainer.Visible = false;
                if (!Visible)
                    return;
                webview.Visible = false;
                webview.LoadUri("about:blank");
                video.Visible = true;
                video.SetFileName(file ?? "");
            });

        videoObserver = observer.
            Where(IsPdf)
            .Subscribe(file =>
            {
                fileName = file;
                image.SetFileName("");
                imageContainer.Visible = false;
                video.Visible = false;
                video.SetFileName("");
                if (!Visible)
                    return;
                webview.Visible = true;
                webview.LoadUri($"file://{fileName ?? ""}");
            });

        noObserver = observer.
            Where(IsNotViewable)
            .Subscribe(_ =>
            {
                fileName = null;
                image.SetFileName("");
                imageContainer.Visible = false;
                video.Visible = false; 
                video.SetFileName("");                   
                if (!Visible)
                    return;
                webview.Visible = false;
                webview.LoadUri("about:blank");
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

    static bool IsPdf(string? file)
        => file?.EndsWith(".pdf", StringComparison.InvariantCultureIgnoreCase) == true;

    static bool IsNotViewable(string? file) => !IsVideo(file) && !IsImage(file) && !IsPdf(file);

    static void ReplacePlaceHolder(nint parent, nint widget)
        => parent.PanedSetEndChild(widget);

    [Widget]
    readonly Grid imageContainer = null!;
    
    [Widget]
    readonly Picture image = null!;

    [Widget]
    readonly WebView location = null!;

    [Widget]
    readonly Video video = null!;

    [Widget]
    readonly WebView webview = null!;

    readonly IDisposable imageObserver;
    readonly IDisposable videoObserver;
    readonly IDisposable noObserver;

    string? fileName;
}
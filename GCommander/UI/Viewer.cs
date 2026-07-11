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
                    SetImage();
                else if (IsVideo(fileName))
                    SetVideo();
                else if (IsPdf(fileName))
                    SetPdf();
                else if (IsTrack(fileName))
                    SetTrack();
                else
                    SetNothing();
            }
            else
                SetNothing();
        };

        MainContext.Instance.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainContext.PreviewMode))
            {
                switch (MainContext.Instance.PreviewMode)
                {
                    case PreviewMode.Picture:
                        image.Visible = true;
                        location.Visible = false;
                        break;
                    case PreviewMode.PictureLocation:
                        image.Visible = true;
                        location.Visible = true;
                        break;
                    case PreviewMode.Location:
                        image.Visible = false;
                        location.Visible = true;
                        break;
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
                SetImage();
            });
                

        videoObserver = observer.
            Where(IsVideo)
            .Subscribe(file =>
            {
                fileName = file;
                SetVideo();
            });

        trackObserver = observer.
            Where(IsTrack)
            .Subscribe(file =>
            {
                fileName = file;
                SetTrack();
            });

        pdfObserver = observer.
            Where(IsPdf)
            .Subscribe(file =>
            {
                fileName = file;
                SetPdf();
            });

        noObserver = observer.
            Where(IsNotViewable)
            .Subscribe(_ =>
            {
                fileName = null;
                SetNothing();
            });

        OnFinalize(() =>
        {
            imageObserver.Dispose();
            videoObserver.Dispose();
            pdfObserver.Dispose();
            trackObserver.Dispose();
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

    static bool IsTrack(string? file)
        => file?.EndsWith(".gpx", StringComparison.InvariantCultureIgnoreCase) == true;

    static bool IsNotViewable(string? file) => !IsVideo(file) && !IsImage(file) && !IsPdf(file) && !IsTrack(file);

    static void ReplacePlaceHolder(nint parent, nint widget)
        => parent.PanedSetEndChild(widget);

    void SetImage()
    {
        video.Visible = false;
        video.SetFileName("");
        if (!Visible)
            return;
        webview.Visible = false;
        webview.LoadUri("about:blank");
        trackviewer.Visible = false;
        imageContainer.Visible = true;
        image.SetFileName(fileName ?? "");
    }
    
    void SetVideo()
    {
        image.SetFileName("");
        imageContainer.Visible = false;
        if (!Visible)
            return;
        webview.Visible = false;
        webview.LoadUri("about:blank");
        trackviewer.Visible = false;
        video.Visible = true;
        video.SetFileName(fileName ?? "");
    }

    void SetPdf()
    {
        image.SetFileName("");
        imageContainer.Visible = false;
        video.Visible = false;
        video.SetFileName("");
        if (!Visible)
            return;
        webview.Visible = true;
        trackviewer.Visible = false;
        webview.LoadUri($"file://{fileName ?? ""}");
    }

    void SetTrack()
    {
        image.SetFileName("");
        imageContainer.Visible = false;
        video.Visible = false;
        video.SetFileName("");
        if (!Visible)
            return;
        webview.Visible = false;
        trackviewer.Visible = true;
        // trackviewer.LoadUri($"file://{fileName ?? ""}");
    }

    void SetNothing()
    {
        image.SetFileName("");
        imageContainer.Visible = false;
        video.Visible = false; 
        video.SetFileName("");                   
        if (!Visible)
            return;
        webview.Visible = false;
        trackviewer.Visible = false;
        webview.LoadUri("about:blank");
    }

    [Widget]
    readonly Grid imageContainer = null!;
    
    [Widget]
    readonly Picture image = null!;

    [Widget]
    readonly LocationViewer location = null!;

    [Widget]
    readonly Video video = null!;

    [Widget]
    readonly WebView webview = null!;

    [Widget]
    readonly TrackViewer trackviewer = null!;

    readonly IDisposable imageObserver;
    readonly IDisposable videoObserver;
    readonly IDisposable pdfObserver;
    readonly IDisposable trackObserver;
    readonly IDisposable noObserver;

    string? fileName;
}
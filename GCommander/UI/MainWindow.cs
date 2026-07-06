using System.Globalization;
using Gtk4DotNet;

class MainWindow : ApplicationWindow
{
    public MainWindow(WindowBuilder builder) : base(builder)
    {
        StyleContext.AddProviderForDisplay(
            Display.GetDefault(),
            CssProvider.New().FromResource("style"),
            StyleProviderPriority.Application);

        var w = Application.Settings.GetInt("width");
        var h = Application.Settings.GetInt("height");
        if (w.HasValue && w.Value > 0 && h.HasValue && h.Value > 0)
            SetDefaultSize(w.Value, h.Value);

        OnRealize += () => folderpaned.Initialize(Width);

        DataContext = MainContext.Instance;
        statusText.Binding("label", nameof(MainContext.SelectedPath));
        statusText.Binding("visible", nameof(MainContext.StatusChoice), BindingFlags.Default, s => (StatusChoice?)s == StatusChoice.Status);
        labelDirs.Binding("label", nameof(MainContext.CurrentDirectoryCount));
        labelFiles.Binding("label", nameof(MainContext.CurrentFileCount));
        folderpaned.SetFocus();
        banner.SetBinding("revealed", nameof(MainContext.ErrorText), BindingFlags.Default, v => (v as string) != "");
        banner.SetBinding("title", nameof(MainContext.ErrorText));
        banner.OnButtonClicked += () => banner.IsRevealed = false;
        viewer.SetBinding("visible", nameof(MainContext.ViewerVisible));
        backgroundActionText.SetBinding("label", nameof(MainContext.BackgroundAction), BindingFlags.Default, GetBackgroundAction);
        backgroundActionText.Binding("visible", nameof(MainContext.StatusChoice), BindingFlags.Default, s => (StatusChoice?)s == StatusChoice.BackgroundAction);
        actionBar.SetBindingToCss("info", nameof(MainContext.StatusChoice), s => (StatusChoice?)s == StatusChoice.BackgroundAction);

        //AddActions(new SimpleAction("refresh", columnviewLeft.Refresh, "<Ctrl>R"));
        AddActions(new BoolAction("showhidden", false, sh => MainContext.Instance.ShowHiddenItems = sh, "<Ctrl>H"));
        AddActions(new BoolAction("fileview", false, sh => MainContext.Instance.ViewerVisible = sh, "F3"));

        viewerPaned.Position = Height - 300;

        OnFinalize(() =>
        {
            Application.Settings.SetInt("width", Width);
            Application.Settings.SetInt("height", Height);
        });
    }

    string GetBackgroundAction(object? value)
    {
        if (value is BackgroundAction ba)
        {
            if (ba == BackgroundAction.ExifDatas)
                return "Exif-Daten werden geholt...";
        }
        return "";
    }

    [Widget(Template = "folderpaned")]
    readonly FolderPaned folderpaned = null!;

    [Widget]
    readonly Paned viewerPaned = null!;

    [Widget]
    readonly Label statusText = null!;

    [Widget]
    readonly Label labelDirs = null!;

    [Widget]
    readonly Label labelFiles = null!;

    [Widget]
    readonly Label backgroundActionText = null!;

    [Widget]
    readonly Widget actionBar = null!;
    
    [Widget]
    readonly AdwBanner banner = null!;

    [Widget(Template = "viewer")]
    readonly Viewer viewer = null!;
}

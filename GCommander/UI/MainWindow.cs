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
        labelDirs.Binding("label", nameof(MainContext.CurrentDirectoryCount));
        labelFiles.Binding("label", nameof(MainContext.CurrentFileCount));
        folderpaned.SetFocus();

        viewer.SetBinding("visible", nameof(MainContext.ViewerVisible));

        //        AddActions(new SimpleAction("refresh", columnviewLeft.Refresh, "<Ctrl>R"));
        AddActions(new BoolAction("showhidden", false, sh => MainContext.Instance.ShowHiddenItems = sh, "<Ctrl>H"));
        AddActions(new BoolAction("fileview", false, sh => MainContext.Instance.ViewerVisible = sh, "F3"));

        viewerPaned.Position = Height - 300;

        OnFinalize(() =>
        {
            Application.Settings.SetInt("width", Width);
            Application.Settings.SetInt("height", Height);
        });
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

    [Widget(Template = "viewer")]
    readonly Viewer viewer = null!;
}

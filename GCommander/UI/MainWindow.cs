using Gtk4DotNet;

class MainWindow : ApplicationWindow
{
    public MainWindow(WindowBuilder builder) : base(builder)
    {
        StyleContext.AddProviderForDisplay(
            Display.GetDefault(),
            CssProvider.New().FromResource("style"),
            StyleProviderPriority.Application);

        DataContext = MainContext.Instance;
        statusText.Binding("label", nameof(MainContext.SelectedPath), BindingFlags.Default);
        labelDirs.Binding("label", nameof(MainContext.CurrentDirectoryCount), BindingFlags.Default);
        labelFiles.Binding("label", nameof(MainContext.CurrentFileCount), BindingFlags.Default);
        folderpaned.SetFocus();
        //        AddActions(new SimpleAction("refresh", columnviewLeft.Refresh, "<Ctrl>R"));
        AddActions(new BoolAction("showhidden", false, sh => MainContext.Instance.ShowHiddenItems = sh, "<Ctrl>H"));
    }

    [Widget(Template = "folderpaned")]
    readonly FolderPaned folderpaned = null!;

    [Widget]
    readonly Label statusText = null!;

    [Widget]
    readonly Label labelDirs = null!;

    [Widget]
    readonly Label labelFiles = null!;
}

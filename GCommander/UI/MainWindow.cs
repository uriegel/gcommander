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
        folderpaned.SetFocus();
//        AddActions(new SimpleAction("refresh", columnviewLeft.Refresh, "<Ctrl>R"));
    }

    [Widget(Template ="folderpaned")]
    FolderPaned folderpaned = null!;

    [Widget]
    Label statusText = null!;
}

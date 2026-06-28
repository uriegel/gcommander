using Gtk4DotNet;

class MainWindow : ApplicationWindow
{
    public MainWindow(WindowBuilder builder) : base(builder)
    {
        StyleContext.AddProviderForDisplay(
            Display.GetDefault(),
            CssProvider.New().FromResource("style"),
            StyleProviderPriority.Application);


        _ = columnviewLeft;
    }

    [Widget]
    FolderView columnviewLeft = null!;
}

using Gtk4DotNet;

class NewRemote : AdwAlertDialog
{
    public static async Task<RemoteDevice?> PresentAsync()
    {
        using var builder = Builder.FromDotNetResource("new-remote");
        var dialog = new NewRemote(builder, "dialog");
        var res = await dialog.PresentAsync(MainWindow.Instance);
        if (res == "cancel")
            return null;
        return dialog.Result;
    }

    public RemoteDevice? Result
    {
        get
        {
            var editable = nameEntry.AsEditable();
            var text = editable.Text;
            editable = ipEntry.AsEditable();
            var path = editable.Text;
            return new(text, path, android.IsActive);
        }
    }

    public NewRemote(Builder builder, string name) : base(builder, name) { }

    [Widget(Name = "name")]
    public readonly Entry nameEntry = null!;

    [Widget(Name = "ip")]
    public readonly Entry ipEntry = null!;

    [Widget]
    public readonly CheckButton android = null!;
}
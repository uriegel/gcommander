using Gtk4DotNet;

namespace UI;

class Rename : AdwAlertDialog
{
    public static async Task<string?> PresentAsync(string body, string name, Widget parent, string? header = null)
    {
        using var builder = Builder.FromDotNetResource("rename");
        var dialog = new Rename(builder, "dialog", header, body, name);
        var res = await dialog.PresentAsync(parent);
        if (res == "cancel")
            return null;
        return dialog.Result;
    }

    public string? Result
    {
        get
        {
            var editable = nameEntry.AsEditable();
            var text = editable.Text;
            return text;
        }
    }

    public Rename(Builder builder, string name, string? header, string body, string itemName) 
        : base(builder, name)
    {
        if (header != null)
            Heading = header;
        Body = body;
        var editable = nameEntry.AsEditable();
        editable.Text = itemName;
        Gtk.BeginInvoke(300, async () =>
        {
            await Task.Delay(100);
            editable.SelectRegion(0, itemName.LastIndexOf('.'));
        });
    }

    [Widget(Name = "name")]
    public readonly Entry nameEntry = null!;
}
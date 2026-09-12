using Gtk4DotNet;

namespace UI;

class Rename : AdwAlertDialog
{
    public static async Task<string?> PresentAsync(string name, Widget parent)
    {
        using var builder = Builder.FromDotNetResource("rename");
        var dialog = new Rename(builder, "dialog", name);
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

    public Rename(Builder builder, string name, string itemName) : base(builder, name)
    {
        var editable = nameEntry.AsEditable();
        editable.Text = itemName;
    }

    [Widget(Name = "name")]
    public readonly Entry nameEntry = null!;
}
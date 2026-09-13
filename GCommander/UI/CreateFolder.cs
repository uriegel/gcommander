using Gtk4DotNet;

namespace UI;

class CreateFolder : AdwAlertDialog
{
    public static async Task<string?> PresentAsync(string? name, Widget parent)
    {
        using var builder = Builder.FromDotNetResource("create-folder");
        var dialog = new CreateFolder(builder, "dialog", name);
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

    public CreateFolder(Builder builder, string name, string? itemName) : base(builder, name)
    {
        var editable = nameEntry.AsEditable();
        editable.Text = itemName ?? "";
    }

    [Widget(Name = "name")]
    public readonly Entry nameEntry = null!;
}
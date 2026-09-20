using CsTools.Extensions;
using Gtk4DotNet;

namespace UI;

class ExtendedRename : AdwAlertDialog
{
    public static ExtendedRenameValues Value { get; private set; } = new("Bild", 3, 0);
    public static async Task<ExtendedRenameValues?> PresentAsync()
    {
        using var builder = Builder.FromDotNetResource("extendedrename");
        var dialog = new ExtendedRename(builder, "dialog");
        var res = await dialog.PresentAsync(MainWindow.Instance);
        if (res == "cancel")
            return null;
        Value = new(dialog.prefix.AsEditable().Text, dialog.digits.ValueAsInt, int.TryParse(dialog.start.AsEditable().Text, out var n) ? n : 0);
        return Value;
    }

    public ExtendedRename(Builder builder, string name)
        : base(builder, name)
    {
        var editable = prefix.AsEditable();
        editable.Text = Value.Prefix;
        editable = start.AsEditable();
        editable.Text = $"{Value.Start}";
        editable.OnInsertText += text =>
        {
            if (text.All(char.IsDigit) == false)
                start.AsEditable().StopInserting();
        };

        digits.Value = Value.Digits;
    }

    [Widget]
    readonly Entry prefix = null!;

    [Widget]
    readonly SpinButton digits = null!;

    [Widget]
    readonly Entry start = null!;
}

record ExtendedRenameValues(
    string Prefix,
    int Digits,
    int Start
);
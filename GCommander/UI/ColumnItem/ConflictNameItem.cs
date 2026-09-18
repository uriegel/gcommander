using Gtk4DotNet;

class ConflictNameItem : Box
{
    public new string Name
    {
        get => text.Text;
        set => text.Text = value;
    }

    public string SubPath
    {
        get => subPath.Text;
        set => subPath.Text = value;
    }

    public void SetFromIconName(string name)
        => image.SetFromIconName(name);

    public void SetIcon(string name)
    {
        var icon = GIcon.Get(Gio.GuessContentType(name) ?? "none");
        image.SetIcon(icon);
        SetObject(Quark.Get("Hallo"), icon);
    }

    public ConflictNameItem() : base() { }
    public ConflictNameItem(Builder builder) : base(builder, "listitem") { }

    [Widget]
    readonly Image image = null!;

    [Widget]
    readonly Label text = null!;
    [Widget]
    readonly Label subPath = null!;
}


using Gtk4DotNet;

class IconNameItem : Box
{
    public new string Name
    {
        get => text.Text;
        set => text.Text = value;
    }

    public void SetFromIconName(string name)
        => image.SetFromIconName(name);

    public void SetIcon(string name)
    {
        var icon = GIcon.Get(Gio.GuessContentType(name) ?? "none");
        image.SetIcon(icon);
        SetObject(Quark.Get("Hallo"), icon);
    }

    public IconNameItem() : base() { }
    public IconNameItem(Builder builder) : base(builder, "listitem") { }

    [Widget]
    readonly Image image = null!;

    [Widget]
    readonly Label text = null!;
}


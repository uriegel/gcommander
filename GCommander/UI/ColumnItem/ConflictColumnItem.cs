using Gtk4DotNet;

class ConflictColumnItem : Box
{
    public new string Name
    {
        get => text.Text;
        set => text.Text = value;
    }

    public string Name2
    {
        get => text2.Text;
        set => text2.Text = value;
    }

    public ConflictColumnItem() : base() { }
    public ConflictColumnItem(Builder builder) : base(builder, "listitem") { }

    [Widget]
    readonly Label text = null!;
    [Widget]
    readonly Label text2 = null!;
}


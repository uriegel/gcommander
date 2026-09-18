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

    public ConflictType ConflictType
    {
        get;
        set
        {
            field = value;
            switch (value)
            {
                case ConflictType.Yes:
                    AddCssClass("conflict-yes");
                    break;
                case ConflictType.No:
                    AddCssClass("conflict-no");
                    break;
                case ConflictType.Default:
                    AddCssClass("conflict-no", false);
                    AddCssClass("conflict-yes", false);
                    break;
            }
        }        
    }

    public bool RightAligned
    {
        get;
        set
        {
            field = value;
            text.HAlign = value ? Align.End : Align.Start;
            text2.HAlign = value ? Align.End : Align.Start;
        } 
    }

    public ConflictColumnItem() : base() { }
    public ConflictColumnItem(Builder builder) : base(builder, "listitem") { }
 
    [Widget]
    readonly Label text = null!;
    [Widget]
    readonly Label text2 = null!;
}

enum ConflictType
{
    Default,
    Yes,
    No,
    Indifferent
}
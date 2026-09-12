class SelectableItem : Item
{
    public bool IsSelected
    {
        get;
        set
        {
            field = value;
            OnChanged(nameof(IsSelected));
        }
    } 

    public SelectableItem(string name) : base(name) { }
}
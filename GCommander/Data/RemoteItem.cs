class RemoteItem : SelectableItem
{
    public string IP { get; }

    public RemoteItem(string name, string ip) : base(name) => IP = ip;
}
    
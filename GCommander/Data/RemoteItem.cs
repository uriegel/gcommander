class RemoteItem : SelectableItem
{
    public string IP { get; }

    public bool IsAndroid { get; }

    public RemoteItem(string name, string ip, bool isAndroid) : base(name)
    {
        IP = ip;
        IsAndroid = isAndroid;
    }
}

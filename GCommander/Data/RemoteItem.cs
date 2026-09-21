class RemoteDevice : SelectableItem
{
    public string IP { get; }

    public bool IsAndroid { get; }

    public RemoteDevice(string name, string ip, bool isAndroid) : base(name)
    {
        IP = ip;
        IsAndroid = isAndroid;
    }
}

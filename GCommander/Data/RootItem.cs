class RootItem(
    string name,
    string description,
    long? size,
    string mountPoint,
    bool isMounted,
    string iconName,
    string? uuid = null,
    string type = DriveType.HARDDRIVE,
    int? use = null,
    bool removable = false
) : Item(name)
{
    public string Description { get => description; }
    public long? Size { get => size; }
    public string MountPoint { get => mountPoint; }
    public bool IsMounted { get => isMounted; } 
    public string IconName { get => iconName; } 
    public string? Uuid { get => uuid; } 
    public string Type { get => type; } 
    public int? Use { get => use; } 
    public bool Removable { get => removable; } 
}

static class DriveType
{
    public const string HOME = "HOME";
    public const string REMOVABLE_USB = "REMOVABLE_USB";
    public const string HARDDRIVE = "HARDDRIVE";
    public const string HARDDRIVE_USB = "HARDDRIVE_USB";
    public const string SATA = "SATA";
} 

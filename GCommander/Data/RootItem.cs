record RootItem(
    string Name,
    string Description,
    long? Size,
    string MountPoint,
    bool IsMounted,
    string IconName,
    string? Uuid = null,
    string Type = DriveType.HARDDRIVE,
    string? Use = null,
    bool Removable = false
);

static class DriveType
{
    public const string HOME = "HOME";
    public const string REMOVABLE_USB = "REMOVABLE_USB";
    public const string HARDDRIVE = "HARDDRIVE";
    public const string HARDDRIVE_USB = "HARDDRIVE_USB";
    public const string SATA = "SATA";
} 

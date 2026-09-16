record ChangesResult(Change[]? Changes);

record Change(
    Item? Item = null,
    DeleteChange? Deleted = null,
    CreateChange? Created = null,
    RenameChange? Renamed = null);

record DeleteChange(int Position, int Selection);

record CreateChange(Item Item, int Position, int Selection);

record RenameChange(Item Item, int OldPosition, int Position, int Selection);
record FavoriteItem(
    string Name,
    string Path,
    FavoriteItemType Type
);

enum FavoriteItemType
{
    Parent,
    Item,
    New
}
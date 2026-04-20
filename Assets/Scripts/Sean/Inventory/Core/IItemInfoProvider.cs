namespace Sean.Inventory
{
    public interface IItemInfoProvider
    {
        ItemInfo GetItemInfo(string id);
    }
}
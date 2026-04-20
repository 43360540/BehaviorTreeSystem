namespace Sean.Inventory
{
    public readonly struct ItemInfo
    {
        public string Id { get; }
        public int MaxStack { get; }
        public bool IsStackable => MaxStack > 1;

        public ItemInfo(string id, int maxStack)
        {
            Id = id;
            MaxStack = maxStack < 1 ? 1 : maxStack;
        }
    }
}
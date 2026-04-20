using System.Collections.Generic;
using UnityEngine;
using Item;

public class ItemDatabase
{
    private static ItemDatabase _instance = null;
    public static ItemDatabase Instance
    {
        get
        {
            _instance ??= new();
            if (!_instance._isLoaded)
                _instance.LoadItemData();
            return _instance;
        }
    }

    private Dictionary<string, ItemDef> _items;
    private ItemDef[] _sourceItems;
    private bool _isLoaded = false;

    private void LoadItemData()
    {
        if (_isLoaded) return;

        _isLoaded = true;
        _sourceItems = Resources.LoadAll<ItemDef>("");

        _items ??= new();

        foreach (var item in _sourceItems)
        {
            _items.Add(item.itemID, item);
        }
    }

    public ItemDef GetItemDefByID(string id)
    {
        if (_items.TryGetValue(id, out ItemDef item))
            return item;

        return _items["empty"];
    }

    public void ShowItems()
    {
        foreach (var item in _items)
        {
            Debug.Log($"{item.Key}: {item.Value}");
        }
    }
}
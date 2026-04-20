using Sean.Inventory;
using UnityEngine;

namespace Item
{
    [CreateAssetMenu(fileName = "Basic Item", menuName = "New Item/Basic Item")]
    public class ItemDef : ScriptableObject
    {
        public GameObject itemPrefab;
        public string itemID = "empty";
        public string itemName = "Empty";
        public Sprite icon;
        public bool stackable = false;
        public int maxStack = 0;
    }
}


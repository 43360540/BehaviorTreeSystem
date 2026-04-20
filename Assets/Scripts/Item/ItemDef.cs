using Sean.Inventory;
using UnityEngine;

namespace Item
{
    [CreateAssetMenu(fileName = "Basic Item", menuName = "New Item/Basic Item")]
    public class ItemDef : ScriptableObject
    {
        public GameObject ItemPrefab;
        public string ItemID = "empty";
        public string ItemName = "Empty";
        public Sprite Icon;
        public bool Stackable = false;
        public int MaxStack = 0;
    }
}


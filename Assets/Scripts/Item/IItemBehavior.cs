using UnityEngine;

namespace Item
{
    public interface IUsable
    {
        public void Use(UseContext ctx);
    }
    public interface IEquippable
    {
        public void Equip(EquipContext ctx);
        
        public void Unequip(EquipContext ctx);
    }
}

using UnityEngine;

namespace Item
{
   [CreateAssetMenu(fileName = "Equipment", menuName = "New Item/Equipment")]
   class Equipment : ItemDef, IEquippable
   {
       public Transform instantiateParent{ get; set; }
       
       public void Equip(EquipContext ctx)
       {
       }
   
       public void Unequip(EquipContext ctx)
       {
   
       }
   } 
}

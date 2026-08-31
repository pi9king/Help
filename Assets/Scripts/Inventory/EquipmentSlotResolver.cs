using Help.Item;

namespace Help.Inventory
{
    // 아이템 유형 → 장착 슬롯 매핑. 장착 불가(재료/소모품)면 false.
    public static class EquipmentSlotResolver
    {
        public static bool TryResolve(ItemType type, out EquipmentSlotType slot)
        {
            switch (type)
            {
                case ItemType.Weapon: slot = EquipmentSlotType.Weapon; return true;
                case ItemType.SubWeapon: slot = EquipmentSlotType.SubWeapon; return true;
                case ItemType.HeadArmor: slot = EquipmentSlotType.Head; return true;
                case ItemType.BodyArmor: slot = EquipmentSlotType.Body; return true;
                case ItemType.LegArmor: slot = EquipmentSlotType.Legs; return true;
                case ItemType.Accessory: slot = EquipmentSlotType.Accessory; return true;
                default: slot = default; return false;
            }
        }
    }
}

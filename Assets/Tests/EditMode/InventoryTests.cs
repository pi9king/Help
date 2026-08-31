using NUnit.Framework;
using Help.Inventory;
using Help.Item;
using UnityEngine;

namespace Tests.EditMode
{
    public class InventoryTests
    {
        private ItemDefinition MakeWeapon(string id, int attackBonus, int defenseBonus)
        {
            var def = ScriptableObject.CreateInstance<ItemDefinition>();
            def.Id = id;
            def.Word = id.ToUpper();
            def.Type = ItemType.Weapon;
            def.AttackBonus = attackBonus;
            def.DefenseBonus = defenseBonus;
            return def;
        }

        [Test]
        public void ShouldFireOnItemEquippedWhenEquipping()
        {
            var inv = new Inventory();
            var blade = MakeWeapon("blade", 8, 2);
            inv.Add(blade);

            ItemDefinition equipped = null;
            inv.OnItemEquipped += item => equipped = item;
            inv.Equip(inv.Items[0], EquipmentSlotType.Weapon);

            Assert.AreEqual(blade, equipped);
        }

        [Test]
        public void ShouldFireOnItemUnequippedWhenUnequipping()
        {
            var inv = new Inventory();
            var blade = MakeWeapon("blade", 8, 2);
            inv.Add(blade);
            inv.Equip(inv.Items[0], EquipmentSlotType.Weapon);

            ItemDefinition unequipped = null;
            inv.OnItemUnequipped += item => unequipped = item;
            inv.Unequip(EquipmentSlotType.Weapon);

            Assert.AreEqual(blade, unequipped);
        }

        [Test]
        public void ShouldFireOnItemUnequippedForPreviousItemWhenSwappingEquipment()
        {
            var inv = new Inventory();
            var blade = MakeWeapon("blade", 8, 2);
            var spear = MakeWeapon("spear", 7, 0);
            inv.Add(blade);
            inv.Add(spear);
            inv.Equip(inv.Items[0], EquipmentSlotType.Weapon); // equips blade

            ItemDefinition unequipped = null;
            inv.OnItemUnequipped += item => unequipped = item;
            inv.Equip(inv.Items[0], EquipmentSlotType.Weapon); // now equips spear, auto-unequips blade

            Assert.AreEqual(blade, unequipped);
        }
    }
}

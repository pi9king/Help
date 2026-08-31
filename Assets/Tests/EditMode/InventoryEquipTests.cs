using System.Linq;
using NUnit.Framework;
using Help.Inventory;
using Help.Item;
using UnityEngine;

namespace Tests.EditMode
{
    public class InventoryEquipTests
    {
        private ItemDefinition MakeWeapon(string id)
        {
            var def = ScriptableObject.CreateInstance<ItemDefinition>();
            def.Id = id;
            def.Word = id.ToUpper();
            def.Type = ItemType.Weapon;
            return def;
        }

        [Test]
        public void Equip_SplitsSingleUnitFromStack()
        {
            var inv = new Inventory();
            var sword = MakeWeapon("sword");
            inv.Add(sword, 2);

            inv.Equip(inv.Items[0], EquipmentSlotType.Weapon);

            // 장착은 1개만 — 인벤토리엔 1개 남고, 장착 슬롯 수량은 1
            Assert.AreEqual(1, inv.CountOf(sword));
            Assert.AreEqual(1, inv.Equipped[EquipmentSlotType.Weapon].Count);
        }

        [Test]
        public void Equip_RejectsSlotMismatch()
        {
            var inv = new Inventory();
            var sword = MakeWeapon("sword");
            inv.Add(sword);

            // 무기를 머리 슬롯에 장착 시도 → 거부, 상태 불변
            bool ok = inv.Equip(inv.Items[0], EquipmentSlotType.Head);

            Assert.IsFalse(ok);
            Assert.AreEqual(1, inv.CountOf(sword));
            Assert.IsFalse(inv.Equipped.ContainsKey(EquipmentSlotType.Head));
        }

        [Test]
        public void Equip_RejectsNonEquippableItem()
        {
            var inv = new Inventory();
            var mat = ScriptableObject.CreateInstance<ItemDefinition>();
            mat.Id = "mat_A";
            mat.Word = "A";
            mat.Type = ItemType.Material;
            inv.Add(mat);

            bool ok = inv.Equip(inv.Items[0], EquipmentSlotType.Weapon);

            Assert.IsFalse(ok);
            Assert.AreEqual(0, inv.Equipped.Count);
        }

        [Test]
        public void Unequip_MergesBackWithoutDuplicateStack()
        {
            var inv = new Inventory();
            var sword = MakeWeapon("sword");
            inv.Add(sword);                                   // 인벤토리에 1
            inv.Equip(inv.Items[0], EquipmentSlotType.Weapon); // 장착 (인벤토리 0)
            inv.Add(sword);                                   // 같은 무기 1 더 획득

            inv.Unequip(EquipmentSlotType.Weapon);

            // 중복 스택 없이 하나로 병합, 총 2개
            Assert.AreEqual(1, inv.Items.Count(s => s.Definition.Id == "sword"));
            Assert.AreEqual(2, inv.CountOf(sword));
        }

        [Test]
        public void Clear_EmptiesItemsAndEquipment_FiringUnequip()
        {
            var inv = new Inventory();
            var sword = MakeWeapon("sword");
            var mat = ScriptableObject.CreateInstance<ItemDefinition>();
            mat.Id = "mat_A"; mat.Word = "A"; mat.Type = ItemType.Material;
            inv.Add(sword);
            inv.Add(mat, 3);
            inv.Equip(inv.Items.First(s => s.Definition.Id == "sword"), EquipmentSlotType.Weapon);

            int unequipped = 0;
            inv.OnItemUnequipped += _ => unequipped++;

            inv.Clear();

            Assert.IsEmpty(inv.Items, "아이템이 비워지지 않음");
            Assert.AreEqual(0, inv.Equipped.Count, "장착이 비워지지 않음");
            Assert.AreEqual(1, unequipped, "Clear 시 장착품에 OnItemUnequipped 발생해야 함(스탯 보너스 해제)");
        }
    }
}

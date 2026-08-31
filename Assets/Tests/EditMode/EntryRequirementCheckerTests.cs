using System.Collections.Generic;
using NUnit.Framework;
using Help.Combat;
using Help.Crafting;
using Help.Dungeon;
using Help.Inventory;
using Help.Item;
using UnityEngine;

namespace Tests.EditMode
{
    public class EntryRequirementCheckerTests
    {
        private RecipeDatabase _db;

        [SetUp]
        public void SetUp()
        {
            _db = ScriptableObject.CreateInstance<RecipeDatabase>();

            var blade = ScriptableObject.CreateInstance<ItemDefinition>();
            blade.Id = "blade";
            blade.Word = "BLADE";
            blade.Type = ItemType.Weapon;
            blade.Element = ElementType.Steel;
            blade.WeaponCategory = WeaponCategory.Blade;
            blade.Recipe = new List<MaterialRequirement>
            {
                new MaterialRequirement(AlphabetMaterial.B, 1),
                new MaterialRequirement(AlphabetMaterial.L, 1),
                new MaterialRequirement(AlphabetMaterial.A, 1),
                new MaterialRequirement(AlphabetMaterial.D, 1),
            };
            _db.AddItem(blade);

            var axe = ScriptableObject.CreateInstance<ItemDefinition>();
            axe.Id = "axe";
            axe.Word = "AXE";
            axe.Type = ItemType.Weapon;
            axe.Element = ElementType.Stone;
            axe.WeaponCategory = WeaponCategory.Axe;
            axe.Recipe = new List<MaterialRequirement>
            {
                new MaterialRequirement(AlphabetMaterial.A, 2),
                new MaterialRequirement(AlphabetMaterial.X, 1),
            };
            axe.Capabilities = new List<Capability> { Capability.BreakWall };
            _db.AddItem(axe);
        }

        private ItemDefinition MakeMaterial(AlphabetMaterial mat)
        {
            var def = ScriptableObject.CreateInstance<ItemDefinition>();
            def.Id = $"mat_{mat}";
            def.Word = mat.ToString();
            def.Type = ItemType.Material;
            def.Recipe = new List<MaterialRequirement> { new MaterialRequirement(mat, 1) };
            return def;
        }

        [Test]
        public void ShouldAllowFreeEntryRoomRegardlessOfInventory()
        {
            var room = new Room(0, 0, RoomType.Combat);
            var inv = new Inventory();

            Assert.IsTrue(EntryRequirementChecker.CanEnter(room, inv, _db));
        }

        [Test]
        public void ShouldDenyEntryWhenMaterialsInsufficient()
        {
            var room = new Room(1, 0, RoomType.CombatPuzzle);
            room.EntryConditions.Add(new EntryCondition { RequiredElement = ElementType.Steel });
            var inv = new Inventory();

            Assert.IsFalse(EntryRequirementChecker.CanEnter(room, inv, _db));
        }

        [Test]
        public void ShouldAllowEntryWhenElementRequirementSatisfiedByCraftableItem()
        {
            var room = new Room(1, 0, RoomType.CombatPuzzle);
            room.EntryConditions.Add(new EntryCondition { RequiredElement = ElementType.Steel });
            var inv = new Inventory();
            inv.Add(MakeMaterial(AlphabetMaterial.B));
            inv.Add(MakeMaterial(AlphabetMaterial.L));
            inv.Add(MakeMaterial(AlphabetMaterial.A));
            inv.Add(MakeMaterial(AlphabetMaterial.D));

            Assert.IsTrue(EntryRequirementChecker.CanEnter(room, inv, _db));
        }

        [Test]
        public void ShouldAllowEntryWhenWeaponCategoryRequirementSatisfied()
        {
            var room = new Room(1, 0, RoomType.EnvironmentPuzzle);
            room.EntryConditions.Add(new EntryCondition { RequiredWeapon = WeaponCategory.Axe });
            var inv = new Inventory();
            inv.Add(MakeMaterial(AlphabetMaterial.A), 2);
            inv.Add(MakeMaterial(AlphabetMaterial.X));

            Assert.IsTrue(EntryRequirementChecker.CanEnter(room, inv, _db));
        }

        [Test]
        public void ShouldAllowEntryWhenCapabilityRequirementSatisfiedByCraftableItem()
        {
            // axe가 BreakWall 능력 제공 + 재료로 제작 가능 → 통과
            var room = new Room(1, 0, RoomType.EnvironmentPuzzle);
            room.EntryConditions.Add(new EntryCondition { RequiredCapability = Capability.BreakWall });
            var inv = new Inventory();
            inv.Add(MakeMaterial(AlphabetMaterial.A), 2);
            inv.Add(MakeMaterial(AlphabetMaterial.X));

            Assert.IsTrue(EntryRequirementChecker.CanEnter(room, inv, _db));
        }

        [Test]
        public void ShouldDenyEntryWhenCapabilityRequiredButNoCraftableProvider()
        {
            // BreakWall 제공 아이템(axe)이 있어도 재료 부족이면 불가
            var room = new Room(1, 0, RoomType.EnvironmentPuzzle);
            room.EntryConditions.Add(new EntryCondition { RequiredCapability = Capability.BreakWall });
            var inv = new Inventory(); // 재료 없음

            Assert.IsFalse(EntryRequirementChecker.CanEnter(room, inv, _db));
        }

        [Test]
        public void ShouldDenyEntryWhenNoItemProvidesRequiredCapability()
        {
            // CrossGap을 제공하는 아이템이 DB에 없음 → 재료가 있어도 불가
            var room = new Room(1, 0, RoomType.EnvironmentPuzzle);
            room.EntryConditions.Add(new EntryCondition { RequiredCapability = Capability.CrossGap });
            var inv = new Inventory();
            inv.Add(MakeMaterial(AlphabetMaterial.A), 2);
            inv.Add(MakeMaterial(AlphabetMaterial.X));

            Assert.IsFalse(EntryRequirementChecker.CanEnter(room, inv, _db));
        }

        [Test]
        public void ShouldCountEquippedItemDisassemblyTowardRequirement()
        {
            var room = new Room(1, 0, RoomType.CombatPuzzle);
            room.EntryConditions.Add(new EntryCondition { RequiredElement = ElementType.Steel });
            var inv = new Inventory();

            var blade = _db.Find("blade");
            inv.Add(blade);
            inv.Equip(inv.Items[0], EquipmentSlotType.Weapon);

            Assert.IsTrue(EntryRequirementChecker.CanEnter(room, inv, _db));
        }
    }
}

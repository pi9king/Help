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
    public class DoorStateEvaluatorTests
    {
        private RecipeDatabase _db;

        [SetUp]
        public void SetUp()
        {
            _db = ScriptableObject.CreateInstance<RecipeDatabase>();
            var blade = ScriptableObject.CreateInstance<ItemDefinition>();
            blade.Id = "blade"; blade.Word = "BLADE"; blade.Type = ItemType.Weapon;
            blade.Element = ElementType.Steel; blade.WeaponCategory = WeaponCategory.Blade;
            blade.Recipe = new List<MaterialRequirement>
            {
                new MaterialRequirement(AlphabetMaterial.B, 1), new MaterialRequirement(AlphabetMaterial.L, 1),
                new MaterialRequirement(AlphabetMaterial.A, 1), new MaterialRequirement(AlphabetMaterial.D, 1),
            };
            _db.AddItem(blade);
        }

        private ItemDefinition MakeMaterial(AlphabetMaterial mat)
        {
            var def = ScriptableObject.CreateInstance<ItemDefinition>();
            def.Id = $"mat_{mat}"; def.Word = mat.ToString(); def.Type = ItemType.Material;
            def.Recipe = new List<MaterialRequirement> { new MaterialRequirement(mat, 1) };
            return def;
        }

        [Test]
        public void NoNeighbor_IsNone()
        {
            var map = new DungeonMap((0, 0));
            var start = new Room(0, 0, RoomType.Combat);
            map.AddRoom(start);

            var states = DoorStateEvaluator.Evaluate(start, map, new Inventory(), _db);

            Assert.AreEqual(DoorState.None, states[Direction.East]);
            Assert.AreEqual(DoorState.None, states[Direction.North]);
        }

        [Test]
        public void FreeNeighbor_IsOpen()
        {
            var map = new DungeonMap((0, 0));
            var start = new Room(0, 0, RoomType.Combat) { East = (1, 0) };
            map.AddRoom(start);
            map.AddRoom(new Room(1, 0, RoomType.Treasure));

            var states = DoorStateEvaluator.Evaluate(start, map, new Inventory(), _db);

            Assert.AreEqual(DoorState.Open, states[Direction.East]);
        }

        [Test]
        public void LockedNeighborWithoutMaterials_IsLocked()
        {
            var map = new DungeonMap((0, 0));
            var start = new Room(0, 0, RoomType.Combat) { East = (1, 0) };
            var locked = new Room(1, 0, RoomType.CombatPuzzle);
            locked.EntryConditions.Add(new EntryCondition { RequiredElement = ElementType.Steel });
            map.AddRoom(start); map.AddRoom(locked);

            var states = DoorStateEvaluator.Evaluate(start, map, new Inventory(), _db);

            Assert.AreEqual(DoorState.Locked, states[Direction.East]);
        }

        [Test]
        public void LockedNeighborWithCraftableMaterials_IsOpen()
        {
            var map = new DungeonMap((0, 0));
            var start = new Room(0, 0, RoomType.Combat) { East = (1, 0) };
            var locked = new Room(1, 0, RoomType.CombatPuzzle);
            locked.EntryConditions.Add(new EntryCondition { RequiredElement = ElementType.Steel });
            map.AddRoom(start); map.AddRoom(locked);

            var inv = new Inventory();
            inv.Add(MakeMaterial(AlphabetMaterial.B)); inv.Add(MakeMaterial(AlphabetMaterial.L));
            inv.Add(MakeMaterial(AlphabetMaterial.A)); inv.Add(MakeMaterial(AlphabetMaterial.D));

            var states = DoorStateEvaluator.Evaluate(start, map, inv, _db);

            Assert.AreEqual(DoorState.Open, states[Direction.East]);
        }
    }
}

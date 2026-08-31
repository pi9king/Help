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
    public class RoomNavigatorTests
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
        public void GetNeighbor_ReturnsConnectedCoordinateForDirection()
        {
            var room = new Room(0, 0, RoomType.Combat) { East = (1, 0) };

            Assert.AreEqual((1, 0), RoomNavigator.GetNeighbor(room, Direction.East));
            Assert.IsNull(RoomNavigator.GetNeighbor(room, Direction.North));
        }

        [Test]
        public void ShouldReturnNoDoorWhenNoConnectionInDirection()
        {
            var map = new DungeonMap((0, 0));
            var current = new Room(0, 0, RoomType.Combat);
            map.AddRoom(current);
            var inv = new Inventory();

            var result = RoomNavigator.TryEnter(current, Direction.North, map, inv, _db, out var target);

            Assert.AreEqual(RoomEntryResult.NoDoor, result);
            Assert.IsNull(target);
        }

        [Test]
        public void ShouldReturnNoDoorWhenNeighborCoordinateHasNoRoom()
        {
            var map = new DungeonMap((0, 0));
            var current = new Room(0, 0, RoomType.Combat) { East = (1, 0) };
            map.AddRoom(current);
            // (1,0) 방을 맵에 추가하지 않음
            var inv = new Inventory();

            var result = RoomNavigator.TryEnter(current, Direction.East, map, inv, _db, out var target);

            Assert.AreEqual(RoomEntryResult.NoDoor, result);
            Assert.IsNull(target);
        }

        [Test]
        public void ShouldEnterFreeRoomInDirection()
        {
            var map = new DungeonMap((0, 0));
            var current = new Room(0, 0, RoomType.Combat) { East = (1, 0) };
            var neighbor = new Room(1, 0, RoomType.Treasure);
            map.AddRoom(current);
            map.AddRoom(neighbor);
            var inv = new Inventory();

            var result = RoomNavigator.TryEnter(current, Direction.East, map, inv, _db, out var target);

            Assert.AreEqual(RoomEntryResult.Entered, result);
            Assert.AreSame(neighbor, target);
        }

        [Test]
        public void ShouldBlockConditionalRoomWhenRequirementUnmet()
        {
            var map = new DungeonMap((0, 0));
            var current = new Room(0, 0, RoomType.Combat) { East = (1, 0) };
            var neighbor = new Room(1, 0, RoomType.CombatPuzzle);
            neighbor.EntryConditions.Add(new EntryCondition { RequiredElement = ElementType.Steel });
            map.AddRoom(current);
            map.AddRoom(neighbor);
            var inv = new Inventory();

            var result = RoomNavigator.TryEnter(current, Direction.East, map, inv, _db, out var target);

            Assert.AreEqual(RoomEntryResult.Blocked, result);
            Assert.IsNull(target);
        }

        [Test]
        public void ShouldEnterConditionalRoomWhenRequirementMet()
        {
            var map = new DungeonMap((0, 0));
            var current = new Room(0, 0, RoomType.Combat) { East = (1, 0) };
            var neighbor = new Room(1, 0, RoomType.CombatPuzzle);
            neighbor.EntryConditions.Add(new EntryCondition { RequiredElement = ElementType.Steel });
            map.AddRoom(current);
            map.AddRoom(neighbor);

            var inv = new Inventory();
            inv.Add(MakeMaterial(AlphabetMaterial.B));
            inv.Add(MakeMaterial(AlphabetMaterial.L));
            inv.Add(MakeMaterial(AlphabetMaterial.A));
            inv.Add(MakeMaterial(AlphabetMaterial.D));

            var result = RoomNavigator.TryEnter(current, Direction.East, map, inv, _db, out var target);

            Assert.AreEqual(RoomEntryResult.Entered, result);
            Assert.AreSame(neighbor, target);
        }
    }
}

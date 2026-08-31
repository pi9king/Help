using System.Collections.Generic;
using NUnit.Framework;
using Help.Combat;
using Help.Crafting;
using Help.Dungeon;
using Help.Item;
using UnityEngine;

namespace Tests.EditMode
{
    public class FloorValidatorTests
    {
        private RecipeDatabase _db;

        [SetUp]
        public void SetUp()
        {
            _db = ScriptableObject.CreateInstance<RecipeDatabase>();

            // BLADE = Steel 속성, 재료 B+L+A+D
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

            // SABER = Fire 속성, 재료 S+A+B+R (blade와 A·B를 공유)
            var saber = ScriptableObject.CreateInstance<ItemDefinition>();
            saber.Id = "saber";
            saber.Word = "SABER";
            saber.Type = ItemType.Weapon;
            saber.Element = ElementType.Fire;
            saber.WeaponCategory = WeaponCategory.Saber;
            saber.Recipe = new List<MaterialRequirement>
            {
                new MaterialRequirement(AlphabetMaterial.S, 1),
                new MaterialRequirement(AlphabetMaterial.A, 1),
                new MaterialRequirement(AlphabetMaterial.B, 1),
                new MaterialRequirement(AlphabetMaterial.R, 1),
            };
            _db.AddItem(saber);
        }

        private Room FireRoom(int x, int y)
        {
            var room = new Room(x, y, RoomType.CombatPuzzle);
            room.EntryConditions.Add(new EntryCondition { RequiredElement = ElementType.Fire });
            return room;
        }

        private Room FreeRoom(int x, int y, params MaterialRequirement[] loot)
        {
            var room = new Room(x, y, RoomType.Combat);
            room.GuaranteedLoot.AddRange(loot);
            return room;
        }

        private Room SteelRoom(int x, int y)
        {
            var room = new Room(x, y, RoomType.CombatPuzzle);
            room.EntryConditions.Add(new EntryCondition { RequiredElement = ElementType.Steel });
            return room;
        }

        private static MaterialRequirement Mat(AlphabetMaterial m, int c = 1) => new MaterialRequirement(m, c);

        // 격자 인접 두 방을 양방향으로 연결
        private static void Connect(Room a, Room b)
        {
            if (b.X == a.X && b.Y == a.Y + 1) { a.North = (b.X, b.Y); b.South = (a.X, a.Y); }
            else if (b.X == a.X && b.Y == a.Y - 1) { a.South = (b.X, b.Y); b.North = (a.X, a.Y); }
            else if (b.X == a.X + 1 && b.Y == a.Y) { a.East = (b.X, b.Y); b.West = (a.X, a.Y); }
            else if (b.X == a.X - 1 && b.Y == a.Y) { a.West = (b.X, b.Y); b.East = (a.X, a.Y); }
        }

        // 열쇠 후보는 "기본 제작 가능한 아이템"(E 포함 단어)뿐이다.
        // E 없는 단어를 열쇠로 계획하면 재료를 배치해도 실제로는 제작할 수 없어 클리어 불가 층이 된다.
        [Test]
        public void ShouldNotPlanKeyItemWithoutE()
        {
            var iceSword = ScriptableObject.CreateInstance<ItemDefinition>();
            iceSword.Id = "sword";
            iceSword.Word = "SWORD";
            iceSword.Type = ItemType.Weapon;
            iceSword.Element = ElementType.Ice;
            iceSword.Recipe = new List<MaterialRequirement>
            {
                new MaterialRequirement(AlphabetMaterial.S, 1),
                new MaterialRequirement(AlphabetMaterial.W, 1),
                new MaterialRequirement(AlphabetMaterial.O, 1),
                new MaterialRequirement(AlphabetMaterial.R, 1),
                new MaterialRequirement(AlphabetMaterial.D, 1),
            };
            _db.AddItem(iceSword);

            var iceCondition = new[] { new EntryCondition { RequiredElement = ElementType.Ice } };

            Assert.IsFalse(FloorValidator.TryPlanRequiredMaterials(iceCondition, _db, out _),
                "제작 불가(E 없음) 아이템은 열쇠로 계획되면 안 된다");
        }

        [Test]
        public void ShouldPassWhenNoConditionalRooms()
        {
            var map = new DungeonMap((0, 0));
            map.AddRoom(FreeRoom(0, 0));
            map.AddRoom(FreeRoom(1, 0, Mat(AlphabetMaterial.B)));

            Assert.IsTrue(FloorValidator.Validate(map, _db));
        }

        [Test]
        public void ShouldPassWhenFreeRoomLootCoversConditionalRequirement()
        {
            var map = new DungeonMap((0, 0));
            map.AddRoom(FreeRoom(0, 0,
                Mat(AlphabetMaterial.B), Mat(AlphabetMaterial.L),
                Mat(AlphabetMaterial.A), Mat(AlphabetMaterial.D)));
            map.AddRoom(SteelRoom(1, 0));

            Assert.IsTrue(FloorValidator.Validate(map, _db));
        }

        [Test]
        public void ShouldFailWhenFreeRoomLootInsufficient()
        {
            var map = new DungeonMap((0, 0));
            map.AddRoom(FreeRoom(0, 0, Mat(AlphabetMaterial.B), Mat(AlphabetMaterial.L)));
            map.AddRoom(SteelRoom(1, 0)); // A, D 부족 → blade 제작 불가

            Assert.IsFalse(FloorValidator.Validate(map, _db));
        }

        [Test]
        public void ShouldAggregateLootAcrossMultipleFreeRooms()
        {
            var map = new DungeonMap((0, 0));
            var start = FreeRoom(0, 0, Mat(AlphabetMaterial.B), Mat(AlphabetMaterial.L));
            var north = FreeRoom(0, 1, Mat(AlphabetMaterial.A), Mat(AlphabetMaterial.D));
            var steel = SteelRoom(1, 0);
            Connect(start, north); // 자유 방끼리 연결 → 둘 다 도달 가능
            Connect(start, steel);
            map.AddRoom(start); map.AddRoom(north); map.AddRoom(steel);

            Assert.IsTrue(FloorValidator.Validate(map, _db));
        }

        [Test]
        public void ShouldFailWhenDifferentElementRoomsCompeteForSharedScarceMaterials()
        {
            // blade(Steel; B,L,A,D)와 saber(Fire; S,A,B,R)는 A·B를 공유.
            // 풀엔 A·B가 1개씩뿐 → 둘 다 제작 불가 = 클리어 불가능한 층
            var map = new DungeonMap((0, 0));
            map.AddRoom(FreeRoom(0, 0,
                Mat(AlphabetMaterial.B), Mat(AlphabetMaterial.L), Mat(AlphabetMaterial.A),
                Mat(AlphabetMaterial.D), Mat(AlphabetMaterial.S), Mat(AlphabetMaterial.R)));
            map.AddRoom(SteelRoom(1, 0));
            map.AddRoom(FireRoom(2, 0));

            Assert.IsFalse(FloorValidator.Validate(map, _db));
        }

        [Test]
        public void ShouldReuseSingleWeaponForMultipleSameRequirementRooms()
        {
            // Steel 방이 둘이어도 blade 한 자루로 모두 커버(열쇠 재사용) → 재료는 1자루분이면 충분
            var map = new DungeonMap((0, 0));
            map.AddRoom(FreeRoom(0, 0,
                Mat(AlphabetMaterial.B), Mat(AlphabetMaterial.L),
                Mat(AlphabetMaterial.A), Mat(AlphabetMaterial.D)));
            map.AddRoom(SteelRoom(1, 0));
            map.AddRoom(SteelRoom(2, 0));

            Assert.IsTrue(FloorValidator.Validate(map, _db));
        }

        [Test]
        public void BuildFreeRoomPool_SumsReachableFreeRoomsOnly()
        {
            var map = new DungeonMap((0, 0));
            var r00 = FreeRoom(0, 0, Mat(AlphabetMaterial.A, 2));
            var r10 = FreeRoom(1, 0, Mat(AlphabetMaterial.A), Mat(AlphabetMaterial.B));
            var cond = SteelRoom(2, 0);
            cond.GuaranteedLoot.Add(Mat(AlphabetMaterial.A, 10)); // 조건부 방 루트는 포함 안 됨
            Connect(r00, r10);
            Connect(r10, cond);
            map.AddRoom(r00); map.AddRoom(r10); map.AddRoom(cond);

            var pool = FloorValidator.BuildFreeRoomPool(map);

            Assert.AreEqual(3, pool[AlphabetMaterial.A]);
            Assert.AreEqual(1, pool[AlphabetMaterial.B]);
        }

        [Test]
        public void ShouldFailWhenRequiredMaterialsAreOnlyBehindLockedRoom()
        {
            // start(자유,재료없음) ─ Steel잠금(1,0) ─ 자유방(2,0):B,L,A,D
            // BLADE 재료가 잠긴 방 뒤에만 있어 도달 불가 → 교착 → false
            var map = new DungeonMap((0, 0));
            var start = FreeRoom(0, 0);
            var locked = SteelRoom(1, 0);
            var behind = FreeRoom(2, 0,
                Mat(AlphabetMaterial.B), Mat(AlphabetMaterial.L),
                Mat(AlphabetMaterial.A), Mat(AlphabetMaterial.D));
            Connect(start, locked);
            Connect(locked, behind);
            map.AddRoom(start); map.AddRoom(locked); map.AddRoom(behind);

            Assert.IsFalse(FloorValidator.Validate(map, _db));
        }

        [Test]
        public void ShouldPassWhenRequiredMaterialsAreInReachableFreeRoom()
        {
            // 같은 배치라도 재료가 start(도달 가능)에 있으면 통과
            var map = new DungeonMap((0, 0));
            var start = FreeRoom(0, 0,
                Mat(AlphabetMaterial.B), Mat(AlphabetMaterial.L),
                Mat(AlphabetMaterial.A), Mat(AlphabetMaterial.D));
            var locked = SteelRoom(1, 0);
            Connect(start, locked);
            map.AddRoom(start); map.AddRoom(locked);

            Assert.IsTrue(FloorValidator.Validate(map, _db));
        }
    }
}

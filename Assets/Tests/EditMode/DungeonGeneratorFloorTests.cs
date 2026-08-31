using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Help.Combat;
using Help.Crafting;
using Help.Dungeon;
using Help.Item;
using UnityEngine;

namespace Tests.EditMode
{
    public class DungeonGeneratorFloorTests
    {
        private RecipeDatabase _db;
        private static readonly ElementType[] AvailableElements = { ElementType.Steel, ElementType.Fire, ElementType.Stone };
        private static readonly WeaponCategory[] AvailableWeapons = { WeaponCategory.Blade, WeaponCategory.Saber, WeaponCategory.Axe };

        [SetUp]
        public void SetUp()
        {
            _db = ScriptableObject.CreateInstance<RecipeDatabase>();
            _db.AddItem(MakeWeapon("blade", ElementType.Steel, WeaponCategory.Blade,
                (AlphabetMaterial.B, 1), (AlphabetMaterial.L, 1), (AlphabetMaterial.A, 1), (AlphabetMaterial.D, 1)));
            _db.AddItem(MakeWeapon("saber", ElementType.Fire, WeaponCategory.Saber,
                (AlphabetMaterial.S, 1), (AlphabetMaterial.A, 1), (AlphabetMaterial.B, 1), (AlphabetMaterial.R, 1)));
            _db.AddItem(MakeWeapon("axe", ElementType.Stone, WeaponCategory.Axe,
                (AlphabetMaterial.A, 2), (AlphabetMaterial.X, 1)));
        }

        private ItemDefinition MakeWeapon(string id, ElementType element, WeaponCategory cat,
            params (AlphabetMaterial mat, int count)[] recipe)
        {
            var def = ScriptableObject.CreateInstance<ItemDefinition>();
            def.Id = id;
            def.Word = id.ToUpper();
            def.Type = ItemType.Weapon;
            def.Element = element;
            def.WeaponCategory = cat;
            def.Recipe = recipe.Select(r => new MaterialRequirement(r.mat, r.count)).ToList();
            return def;
        }

        // 기본 제작 대상은 E가 들어간 단어뿐이므로, 그런 아이템으로만 충족되는 속성은 진입 조건이 될 수 없다.
        [Test]
        public void ConditionsNeverRequireElementOnlyProvidedByUncraftableItem()
        {
            // Ice를 가진 아이템이 SWORD(E 없음)뿐인 DB
            _db.AddItem(MakeWeapon("sword", ElementType.Ice, WeaponCategory.None,
                (AlphabetMaterial.S, 1), (AlphabetMaterial.W, 1), (AlphabetMaterial.O, 1),
                (AlphabetMaterial.R, 1), (AlphabetMaterial.D, 1)));

            var gen = new DungeonGenerator();
            for (int seed = 0; seed < 30; seed++)
            {
                var map = gen.Generate(new DungeonConfig { Seed = seed, MinRooms = 8, MaxRooms = 12 }, _db);
                foreach (var room in map.ConditionalRooms())
                    foreach (var cond in room.EntryConditions)
                        Assert.AreNotEqual(ElementType.Ice, cond.RequiredElement,
                            $"seed {seed}: 제작 불가 아이템의 속성을 요구하면 안 된다");
            }
        }

        // 제작 불가 아이템이 DB에 섞여 있어도, 제작 가능한 속성/무기 조건은 계속 부여돼야 한다
        // (재시도/폴백으로 조건이 통째로 사라지면 안 됨).
        [Test]
        public void StillAssignsConditionsWhenDatabaseHasUncraftableItems()
        {
            _db.AddItem(MakeWeapon("sword", ElementType.Ice, WeaponCategory.None,
                (AlphabetMaterial.S, 1), (AlphabetMaterial.W, 1), (AlphabetMaterial.O, 1),
                (AlphabetMaterial.R, 1), (AlphabetMaterial.D, 1)));

            var gen = new DungeonGenerator();
            int seedsWithConditions = 0;
            for (int seed = 0; seed < 30; seed++)
            {
                var map = gen.Generate(new DungeonConfig { Seed = seed, MinRooms = 8, MaxRooms = 12 }, _db);
                if (map.ConditionalRooms().Any()) seedsWithConditions++;
            }

            Assert.Greater(seedsWithConditions, 15,
                "제작 불가 아이템 때문에 대부분의 층에서 진입 조건이 사라졌다");
        }

        [Test]
        public void GeneratedFloorSatisfiesMaterialInvariantAcrossSeeds()
        {
            var gen = new DungeonGenerator();
            for (int seed = 0; seed < 30; seed++)
            {
                var map = gen.Generate(new DungeonConfig { Seed = seed, MinRooms = 8, MaxRooms = 12 }, _db);
                Assert.IsTrue(FloorValidator.Validate(map, _db), $"seed {seed} violated material-guarantee invariant");
            }
        }

        [Test]
        public void ConditionalRoomsUseOnlyAvailableCapabilities()
        {
            var gen = new DungeonGenerator();
            for (int seed = 0; seed < 30; seed++)
            {
                var map = gen.Generate(new DungeonConfig { Seed = seed, MinRooms = 8, MaxRooms = 12 }, _db);
                foreach (var room in map.ConditionalRooms())
                    foreach (var cond in room.EntryConditions)
                    {
                        if (cond.RequiredElement != ElementType.None)
                            Assert.Contains(cond.RequiredElement, AvailableElements, $"seed {seed}");
                        if (cond.RequiredWeapon != WeaponCategory.None)
                            Assert.Contains(cond.RequiredWeapon, AvailableWeapons, $"seed {seed}");
                    }
            }
        }

        [Test]
        public void AtLeastOneSeedProducesConditionalRoom()
        {
            var gen = new DungeonGenerator();
            bool any = false;
            for (int seed = 0; seed < 30 && !any; seed++)
            {
                var map = gen.Generate(new DungeonConfig { Seed = seed, MinRooms = 8, MaxRooms = 12 }, _db);
                any = map.ConditionalRooms().Any();
            }
            Assert.IsTrue(any, "생성기가 조건부 방을 하나도 만들지 않음 — 진입 조건 기능이 비활성");
        }

        // 필수/보너스 재료는 모두 "잠긴 방 없이 도달 가능한 자유 방"에만 놓여야 한다 →
        // 열쇠 재료가 조건부 방 뒤에 갇히는 교착(위상 역전)이 없음을 고정.
        [Test]
        public void GuaranteedLootOnlyInReachableFreeRooms()
        {
            var gen = new DungeonGenerator();
            for (int seed = 0; seed < 30; seed++)
            {
                var map = gen.Generate(new DungeonConfig { Seed = seed, MinRooms = 8, MaxRooms = 12 }, _db);
                var reachable = new HashSet<Room>(FloorValidator.ReachableFreeRooms(map));
                foreach (var room in map.Rooms.Values)
                    if (room.GuaranteedLoot.Count > 0)
                        Assert.IsTrue(reachable.Contains(room),
                            $"seed {seed}: 방 ({room.X},{room.Y})에 재료가 있으나 도달 가능 자유 방이 아님(위상 역전)");
            }
        }

        // 진입 조건 유무와 무관하게, 층은 크래프팅에 쓸 재료를 제공해야 한다(루프 활성).
        // 열쇠 재료(GuaranteedLoot)는 조건이 없는 시드에선 0이므로 예산 글자(BonusLoot)까지 함께 센다.
        [Test]
        public void FloorProvidesCraftingMaterialsAcrossSeeds()
        {
            var gen = new DungeonGenerator();
            for (int seed = 0; seed < 30; seed++)
            {
                var map = gen.Generate(new DungeonConfig { Seed = seed, MinRooms = 8, MaxRooms = 12 }, _db);
                Assert.Greater(TotalLetters(map), 0, $"seed {seed}: 층 재료가 0 — 크래프팅 루프 불가");
            }
        }

        private static int TotalLetters(DungeonMap map) =>
            map.Rooms.Values.Sum(r => r.GuaranteedLoot.Sum(l => l.Count) + r.BonusLoot.Sum(l => l.Count));

        // 층에 뿌려진 알파벳 총량 == 그 층이 고른 레시피들의 글자 합.
        // "이 층의 알파벳을 다 모으면 아이템 3개를 만들 수 있다"는 예산이 지켜지는지 고정한다.
        [Test]
        public void FloorLootTotalMatchesSelectedRecipeBudget()
        {
            var gen = new DungeonGenerator();
            for (int seed = 0; seed < 30; seed++)
            {
                var map = gen.Generate(new DungeonConfig { Seed = seed, MinRooms = 8, MaxRooms = 12 }, _db);

                Assert.IsNotEmpty(map.FloorRecipes, $"seed {seed}: 층 테이블이 비어 있음");
                int budget = FloorLootPlan.BuildLetterBudget(map.FloorRecipes).Count;
                Assert.AreEqual(budget, TotalLetters(map), $"seed {seed}: 층 글자 총량이 예산과 불일치");
            }
        }

        // 층 테이블은 예산 개수를 넘지 않아야 한다.
        // 조건부 방이 많으면 열쇠만으로 예산이 꽉 차서(클리어 가능성 우선) 플레이어가 고를 여지가 사라지므로,
        // 생성기가 조건부 방 수를 제한해 "열쇠 + 자유 선택"이 함께 들어가게 한다.
        [Test]
        public void FloorRecipeCountStaysWithinBudget()
        {
            var gen = new DungeonGenerator();
            for (int seed = 0; seed < 30; seed++)
            {
                var map = gen.Generate(new DungeonConfig { Seed = seed, MinRooms = 8, MaxRooms = 12 }, _db);

                Assert.LessOrEqual(map.ConditionalRooms().Count(), DungeonGenerator.MaxConditionalRooms,
                    $"seed {seed}: 조건부 방이 상한을 넘음");
                Assert.LessOrEqual(map.FloorRecipes.Count, DungeonGenerator.FloorRecipeCount,
                    $"seed {seed}: 층 테이블이 예산({DungeonGenerator.FloorRecipeCount}개)을 넘음");
            }
        }

        // 진입 조건을 여는 열쇠 아이템은 반드시 층 테이블에 포함돼야 한다(클리어 가능성).
        [Test]
        public void FloorRecipesCoverEveryEntryCondition()
        {
            var gen = new DungeonGenerator();
            for (int seed = 0; seed < 30; seed++)
            {
                var map = gen.Generate(new DungeonConfig { Seed = seed, MinRooms = 8, MaxRooms = 12 }, _db);

                foreach (var room in map.ConditionalRooms())
                    foreach (var cond in room.EntryConditions)
                        Assert.IsTrue(
                            map.FloorRecipes.Any(it =>
                                (cond.RequiredElement == ElementType.None || it.Element == cond.RequiredElement) &&
                                (cond.RequiredWeapon == WeaponCategory.None || it.WeaponCategory == cond.RequiredWeapon)),
                            $"seed {seed}: 조건을 여는 아이템이 층 테이블에 없음");
            }
        }

        // 특수방은 "있을 수도, 없을 수도" 있는 보너스 방이다 —
        // 반드시 존재하지 않으므로 클리어에 필요한 것을 여기 두면 안 된다.
        [Test]
        public void SecretRoomAppearsSometimesButNeverMoreThanOne()
        {
            var gen = new DungeonGenerator();
            int floorsWith = 0, floorsWithout = 0;

            for (int seed = 0; seed < 30; seed++)
            {
                var map = gen.Generate(new DungeonConfig { Seed = seed, MinRooms = 8, MaxRooms = 12 }, _db);
                int count = map.Rooms.Values.Count(r => r.Type == RoomType.Secret);

                Assert.LessOrEqual(count, 1, $"seed {seed}: 특수방이 여러 개");
                if (count == 1) floorsWith++; else floorsWithout++;
            }

            Assert.Greater(floorsWith, 0, "특수방이 한 번도 안 나옴");
            Assert.Greater(floorsWithout, 0, "특수방이 항상 나옴 — '있을 수도, 없을 수도'가 아님");
        }

        [Test]
        public void SecretRoomIsFreeEntry()
        {
            var gen = new DungeonGenerator();
            for (int seed = 0; seed < 30; seed++)
            {
                var map = gen.Generate(new DungeonConfig { Seed = seed, MinRooms = 8, MaxRooms = 12 }, _db);
                foreach (var room in map.Rooms.Values)
                    if (room.Type == RoomType.Secret)
                        Assert.IsTrue(room.IsFreeEntry, $"seed {seed}: 특수방에 진입 조건이 붙음");
            }
        }

        [Test]
        public void GenerateWithoutDatabaseLeavesAllRoomsFree()
        {
            var gen = new DungeonGenerator();
            var map = gen.Generate(new DungeonConfig { Seed = 3, MinRooms = 8, MaxRooms = 12 });
            foreach (var room in map.Rooms.Values)
                Assert.IsTrue(room.IsFreeEntry, "DB 없는 Generate는 조건 없는 자유 방만 생성해야 함");
        }
    }
}

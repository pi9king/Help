using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Help.Crafting;
using Help.Dungeon;
using Help.Item;
using UnityEngine;

namespace Tests.EditMode
{
    // 층 드랍 테이블: "이 층의 알파벳을 다 모으면 아이템 N개를 만들 수 있다"는 예산을 계산한다.
    // 진입 열쇠 아이템은 반드시 예산에 포함돼야 클리어 가능성이 유지된다.
    public class FloorLootPlanTests
    {
        private RecipeDatabase _db;

        private static ItemDefinition Item(string id, string word, ItemType type,
            params (AlphabetMaterial mat, int count)[] recipe)
        {
            var def = ScriptableObject.CreateInstance<ItemDefinition>();
            def.Id = id;
            def.Word = word;
            def.Type = type;
            def.Recipe = recipe.Select(r => new MaterialRequirement(r.mat, r.count)).ToList();
            return def;
        }

        [SetUp]
        public void SetUp()
        {
            _db = ScriptableObject.CreateInstance<RecipeDatabase>();
            _db.AddItem(Item("blade", "BLADE", ItemType.Weapon,
                (AlphabetMaterial.B, 1), (AlphabetMaterial.L, 1), (AlphabetMaterial.A, 1), (AlphabetMaterial.D, 1)));
            _db.AddItem(Item("saber", "SABER", ItemType.Weapon,
                (AlphabetMaterial.S, 1), (AlphabetMaterial.A, 1), (AlphabetMaterial.B, 1), (AlphabetMaterial.R, 1)));
            _db.AddItem(Item("key", "KEY", ItemType.SubWeapon,
                (AlphabetMaterial.K, 1), (AlphabetMaterial.Y, 1)));
            _db.AddItem(Item("rapier", "RAPIER", ItemType.Weapon,
                (AlphabetMaterial.R, 2), (AlphabetMaterial.A, 1), (AlphabetMaterial.P, 1), (AlphabetMaterial.I, 1)));
        }

        [Test]
        public void SelectRecipes_AlwaysIncludesKeyItems()
        {
            var keys = new List<ItemDefinition> { _db.Find("saber") };

            var picked = FloorLootPlan.SelectRecipes(_db, keys, 3, seed: 1);

            Assert.AreEqual(3, picked.Count);
            CollectionAssert.Contains(picked, _db.Find("saber"), "열쇠 아이템이 층 예산에서 빠지면 클리어 불가");
        }

        [Test]
        public void SelectRecipes_IsDeterministicAndHasNoDuplicates()
        {
            var a = FloorLootPlan.SelectRecipes(_db, null, 3, seed: 7);
            var b = FloorLootPlan.SelectRecipes(_db, null, 3, seed: 7);

            CollectionAssert.AreEqual(a, b, "같은 시드는 같은 층 테이블");
            Assert.AreEqual(a.Count, a.Distinct().Count(), "같은 아이템이 두 번 뽑히면 안 됨");
        }

        [Test]
        public void SelectRecipes_ExcludesItemsWithoutE()
        {
            var sword = Item("sword", "SWORD", ItemType.Weapon,
                (AlphabetMaterial.S, 1), (AlphabetMaterial.W, 1), (AlphabetMaterial.O, 1),
                (AlphabetMaterial.R, 1), (AlphabetMaterial.D, 1));
            _db.AddItem(sword);

            // 후보를 전부 요구해도 기본 제작 불가 아이템은 층 테이블에 들어가지 않는다
            var picked = FloorLootPlan.SelectRecipes(_db, null, 10, seed: 3);

            CollectionAssert.DoesNotContain(picked, sword);
            Assert.AreEqual(4, picked.Count, "후보가 모자라면 있는 만큼만");
        }

        [Test]
        public void SelectRecipes_KeyItemsBeyondCountAreStillIncluded()
        {
            // 조건이 많아 열쇠가 예산 개수를 넘으면, 클리어 가능성이 우선이다
            var keys = new List<ItemDefinition> { _db.Find("blade"), _db.Find("saber"), _db.Find("key") };

            var picked = FloorLootPlan.SelectRecipes(_db, keys, 2, seed: 5);

            Assert.AreEqual(3, picked.Count);
            foreach (var k in keys) CollectionAssert.Contains(picked, k);
        }

        [Test]
        public void BuildLetterBudget_FlattensRecipesWithCounts()
        {
            var budget = FloorLootPlan.BuildLetterBudget(new[] { _db.Find("rapier"), _db.Find("key") });

            // RAPIER = R×2 + A + P + I (5글자), KEY = K + Y (2글자)
            Assert.AreEqual(7, budget.Count);
            Assert.AreEqual(2, budget.Count(m => m == AlphabetMaterial.R));
            Assert.AreEqual(1, budget.Count(m => m == AlphabetMaterial.K));
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using Help.Crafting;
using Help.Item;
using UnityEngine;

namespace Tests.EditMode
{
    // 기본 제작 규칙: 제작 가능한 아이템은 "E가 들어간 영단어"뿐이다(플레이어 = 알파벳 E).
    // 판정(CanCraft)과 실행(Craft)이 같은 기준을 쓰는지도 함께 검증한다.
    public class CraftingSystemTests
    {
        private RecipeDatabase _db;

        private static ItemDefinition MakeItem(string id, string word, ItemType type, params AlphabetMaterial[] letters)
        {
            var def = ScriptableObject.CreateInstance<ItemDefinition>();
            def.Id = id;
            def.Word = word;
            def.Type = type;
            def.Recipe = new List<MaterialRequirement>();
            foreach (var l in letters)
                def.Recipe.Add(new MaterialRequirement(l, 1));
            return def;
        }

        private static ItemDefinition MakeMaterial(AlphabetMaterial mat) =>
            MakeItem($"mat_{mat}", mat.ToString(), ItemType.Material, mat);

        [SetUp]
        public void SetUp()
        {
            _db = ScriptableObject.CreateInstance<RecipeDatabase>();
            _db.AddItem(MakeItem("blade", "BLADE", ItemType.Weapon,
                AlphabetMaterial.B, AlphabetMaterial.L, AlphabetMaterial.A, AlphabetMaterial.D));
            _db.AddItem(MakeItem("sword", "SWORD", ItemType.Weapon,
                AlphabetMaterial.S, AlphabetMaterial.W, AlphabetMaterial.O, AlphabetMaterial.R, AlphabetMaterial.D));

            foreach (AlphabetMaterial mat in System.Enum.GetValues(typeof(AlphabetMaterial)))
                _db.AddMaterial(MakeMaterial(mat));
        }

        // 필요한 글자를 넉넉히 넣은 인벤토리
        private Help.Inventory.Inventory FullyStockedInventory()
        {
            var inv = new Help.Inventory.Inventory();
            foreach (AlphabetMaterial mat in System.Enum.GetValues(typeof(AlphabetMaterial)))
                inv.Add(_db.FindMaterial(mat), 2);
            return inv;
        }

        [Test]
        public void CanCraft_RejectsWordWithoutE_EvenWithEnoughMaterials()
        {
            var pool = FullyStockedInventory().GetRawMaterials();

            Assert.IsTrue(_db.Find("blade") != null && _db.Find("sword") != null);
            Assert.IsTrue(new CraftingSystem(_db).CanCraft("blade", pool), "BLADE는 E가 있어 제작 가능");
            Assert.IsFalse(new CraftingSystem(_db).CanCraft("sword", pool), "SWORD는 E가 없어 제작 불가");
        }

        [Test]
        public void Craft_RejectsWordWithoutE_AndConsumesNothing()
        {
            var inv = FullyStockedInventory();
            var crafting = new CraftingSystem(_db);
            int sBefore = inv.CountOf(_db.FindMaterial(AlphabetMaterial.S));

            Assert.IsFalse(crafting.Craft("sword", inv), "E 없는 단어는 제작되지 않는다");
            Assert.AreEqual(sBefore, inv.CountOf(_db.FindMaterial(AlphabetMaterial.S)), "실패한 제작이 재료를 소모하면 안 된다");
            Assert.AreEqual(0, inv.CountOf(_db.Find("sword")));
        }

        [Test]
        public void Craft_StillCraftsWordWithE()
        {
            var inv = FullyStockedInventory();
            Assert.IsTrue(new CraftingSystem(_db).Craft("blade", inv));
            Assert.AreEqual(1, inv.CountOf(_db.Find("blade")));
        }

        // 특수방의 제작대는 기본 제작 규칙 밖에 있다 — E 없는 단어도 만들 수 있는 유일한 경로.
        [Test]
        public void SpecialMode_AllowsWordWithoutE()
        {
            var inv = FullyStockedInventory();
            var crafting = new CraftingSystem(_db);

            Assert.IsTrue(crafting.CanCraft("sword", inv.GetRawMaterials(), CraftMode.Special));
            Assert.IsTrue(crafting.Craft("sword", inv, CraftMode.Special));
            Assert.AreEqual(1, inv.CountOf(_db.Find("sword")));
        }

        // 특수 제작이어도 레시피가 없으면 만들 수 없다(재료 요구 0 = 무한 제작 방지).
        [Test]
        public void SpecialMode_StillRequiresRecipe()
        {
            var potion = MakeItem("elixir", "ELIXIR", ItemType.Consumable);
            _db.AddItem(potion);

            var crafting = new CraftingSystem(_db);
            Assert.IsFalse(crafting.CanCraft("elixir", FullyStockedInventory().GetRawMaterials(), CraftMode.Special));
        }

        // 기본 제작은 여전히 막혀 있어야 한다 — 특수 모드를 추가해도 일반 크래프팅 창은 그대로.
        [Test]
        public void BasicMode_RemainsGatedAfterSpecialModeExists()
        {
            var inv = FullyStockedInventory();
            Assert.IsFalse(new CraftingSystem(_db).Craft("sword", inv, CraftMode.Basic));
            Assert.AreEqual(0, inv.CountOf(_db.Find("sword")));
        }

        // 제작은 막아도 분해는 막지 않는다 — 다른 경로(보상 등)로 들어온 아이템이 인벤토리에 갇히면 안 된다.
        [Test]
        public void Disassemble_StillWorksForWordWithoutE()
        {
            var inv = new Help.Inventory.Inventory();
            inv.Add(_db.Find("sword"));

            Assert.IsTrue(new CraftingSystem(_db).Disassemble("sword", inv));
            Assert.AreEqual(0, inv.CountOf(_db.Find("sword")));
            Assert.AreEqual(1, inv.CountOf(_db.FindMaterial(AlphabetMaterial.S)));
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using Help.Item;
using UnityEditor;
using UnityEngine;

namespace Tests.EditMode
{
    public class AlphabetWordRuleTests
    {
        private static MaterialRequirement M(AlphabetMaterial m, int c) => new MaterialRequirement(m, c);

        private static ItemDefinition Item(string word, ItemType type, params AlphabetMaterial[] recipe)
        {
            var def = ScriptableObject.CreateInstance<ItemDefinition>();
            def.Id = word.ToLowerInvariant();
            def.Word = word;
            def.Type = type;
            def.Recipe = new List<MaterialRequirement>();
            foreach (var mat in recipe)
                def.Recipe.Add(new MaterialRequirement(mat, 1));
            return def;
        }

        // 드랍 전용 아이템(포션 등)은 레시피가 없다 — 이런 아이템이 제작 대상에 끼면
        // 재료 요구가 0이라 무한 제작된다.
        [Test]
        public void IsBasicCraftable_RejectsItemWithoutRecipe()
        {
            var elixir = Item("ELIXIR", ItemType.Consumable);
            Assert.IsFalse(AlphabetWordRule.IsBasicCraftable(elixir),
                "레시피 없는 아이템은 제작 대상이 아니다");
        }

        [Test]
        public void IsBasicCraftable_OnlyForWordsContainingE()
        {
            Assert.IsTrue(AlphabetWordRule.IsBasicCraftable(
                Item("BLADE", ItemType.Weapon, AlphabetMaterial.B, AlphabetMaterial.L, AlphabetMaterial.A, AlphabetMaterial.D)));
            Assert.IsFalse(AlphabetWordRule.IsBasicCraftable(
                Item("SWORD", ItemType.Weapon, AlphabetMaterial.S, AlphabetMaterial.W, AlphabetMaterial.O, AlphabetMaterial.R, AlphabetMaterial.D)),
                "E가 없는 단어는 기본 제작 대상이 아니다");
        }

        [Test]
        public void WordWithoutE_IsInvalid()
        {
            Assert.IsFalse(AlphabetWordRule.WordContainsE("SWORD"));
            Assert.IsTrue(AlphabetWordRule.WordContainsE("BLADE"));
        }

        [Test]
        public void RecipeMatchesWord_WhenRecipeIsWordMinusOneE()
        {
            // BLADE = B,L,A,D,E → 재료 B,L,A,D
            Assert.IsTrue(AlphabetWordRule.RecipeMatchesWord("BLADE",
                new[] { M(AlphabetMaterial.B, 1), M(AlphabetMaterial.L, 1), M(AlphabetMaterial.A, 1), M(AlphabetMaterial.D, 1) }));
        }

        [Test]
        public void RecipeMatchesWord_RespectsDuplicateLetters()
        {
            // RAPIER = R,A,P,I,E,R → 재료 R×2, A, P, I
            Assert.IsTrue(AlphabetWordRule.RecipeMatchesWord("RAPIER",
                new[] { M(AlphabetMaterial.R, 2), M(AlphabetMaterial.A, 1), M(AlphabetMaterial.P, 1), M(AlphabetMaterial.I, 1) }));
            // R을 하나만 넣으면 불일치
            Assert.IsFalse(AlphabetWordRule.RecipeMatchesWord("RAPIER",
                new[] { M(AlphabetMaterial.R, 1), M(AlphabetMaterial.A, 1), M(AlphabetMaterial.P, 1), M(AlphabetMaterial.I, 1) }));
        }

        [Test]
        public void RecipeMatchesWord_RejectsExtraMaterial()
        {
            // AXE = A,X,E → 재료 A,X. A를 2개 넣으면 불일치
            Assert.IsFalse(AlphabetWordRule.RecipeMatchesWord("AXE",
                new[] { M(AlphabetMaterial.A, 2), M(AlphabetMaterial.X, 1) }));
            Assert.IsTrue(AlphabetWordRule.RecipeMatchesWord("AXE",
                new[] { M(AlphabetMaterial.A, 1), M(AlphabetMaterial.X, 1) }));
        }

        // 특수 아이템(ItemsSpecial 폴더)은 기본 제작으로는 절대 나오지 않아야 한다 —
        // 특수방에서만 얻을 수 있다는 희소성이 이 아이템들의 존재 이유다.
        [Test]
        public void SpecialItemAssets_AreNeverBasicCraftable()
        {
            var guids = AssetDatabase.FindAssets("t:ItemDefinition", new[] { "Assets/ScriptableObjects/ItemsSpecial" });
            Assert.IsNotEmpty(guids, "Assets/ScriptableObjects/ItemsSpecial 에 특수 아이템이 없음");

            foreach (var guid in guids)
            {
                var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (item == null) continue;

                Assert.IsFalse(AlphabetWordRule.IsBasicCraftable(item),
                    $"{item.Id}({item.Word}) 가 기본 제작으로 만들어진다 — 특수방 전용이어야 함");
                Assert.IsTrue(Help.Crafting.CraftRule.CanCraftWith(item, Help.Crafting.CraftMode.Special),
                    $"{item.Id}({item.Word}) 를 특수 제작대에서도 만들 수 없다");
            }
        }

        // 실제 생성된 제작 아이템 에셋이 규칙을 지키는지 검증 (데이터 드리프트 방지).
        // 재료(Material)는 자기참조 레시피라 제외하고, 그 외 제작물(무기·서브무기·방어구…)은 전부 검사한다.
        [Test]
        public void AllCraftedItemAssetsFollowAlphabetWordRule()
        {
            var guids = AssetDatabase.FindAssets("t:ItemDefinition", new[] { "Assets/ScriptableObjects/Items" });
            Assert.IsNotEmpty(guids, "Assets/ScriptableObjects/Items 에 무기 에셋이 없음");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
                if (item == null || item.Type == ItemType.Material) continue;
                // 레시피가 없는 아이템(포션 등 드랍 전용)은 제작 대상이 아니므로 단어 규칙 대상도 아니다
                if (item.Recipe == null || item.Recipe.Count == 0) continue;

                Assert.IsTrue(AlphabetWordRule.WordContainsE(item.Word),
                    $"{item.Id}({item.Word}) 단어에 E가 없음");
                Assert.IsTrue(AlphabetWordRule.RecipeMatchesWord(item.Word, item.Recipe),
                    $"{item.Id}({item.Word}) 레시피가 '단어 − E' 규칙과 불일치");
            }
        }
    }
}

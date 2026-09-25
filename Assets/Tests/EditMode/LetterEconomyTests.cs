using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Help.Editor.Playtest;
using Help.Item;

namespace Tests.EditMode
{
    // 정적 재미 리포트의 글자 경제 측정(체크리스트 P1-7)이 올바르게 세는지만 검증한다.
    // "경쟁 글자가 몇 개 이상이어야 한다" 같은 기준은 여기서 정하지 않는다 — 그건 사용자 몫이다.
    public class LetterEconomyTests
    {
        private readonly List<ItemDefinition> _created = new();

        private ItemDefinition Item(string word, params AlphabetMaterial[] letters)
        {
            var it = ScriptableObject.CreateInstance<ItemDefinition>();
            it.Id = word.ToLowerInvariant();
            it.Word = word;
            it.Recipe = letters.GroupBy(l => l)
                               .Select(g => new MaterialRequirement(g.Key, g.Count()))
                               .ToList();
            _created.Add(it);
            return it;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var it in _created) Object.DestroyImmediate(it);
            _created.Clear();
        }

        [Test]
        public void BudgetShouldCountEveryLetterOfTheFloorRecipes()
        {
            var axe = Item("AXE", AlphabetMaterial.A, AlphabetMaterial.X);
            var rope = Item("ROPE", AlphabetMaterial.R, AlphabetMaterial.O, AlphabetMaterial.P);

            var stats = LetterEconomy.Analyze(new[] { axe, rope }, new[] { axe, rope });

            Assert.AreEqual(5, stats.BudgetSize);
        }

        // 예산으로 층 레시피 말고 무엇을 더 만들 수 있나 = 층 안에서의 선택 폭.
        // 글자 개수까지 따진다(A 하나로 A 두 개짜리는 못 만든다).
        [Test]
        public void ShouldListExtraItemsCraftableFromTheBudget()
        {
            var axe = Item("AXE", AlphabetMaterial.A, AlphabetMaterial.X);
            var rope = Item("ROPE", AlphabetMaterial.R, AlphabetMaterial.O, AlphabetMaterial.P);
            var ore = Item("ORE", AlphabetMaterial.O, AlphabetMaterial.R);
            var flare = Item("FLARE", AlphabetMaterial.F, AlphabetMaterial.L, AlphabetMaterial.A, AlphabetMaterial.R);
            var aa = Item("AAX", AlphabetMaterial.A, AlphabetMaterial.A, AlphabetMaterial.X);

            var stats = LetterEconomy.Analyze(new[] { axe, rope }, new[] { axe, rope, ore, flare, aa });

            CollectionAssert.AreEquivalent(new[] { ore }, stats.ExtraCraftable);
        }
    }
}

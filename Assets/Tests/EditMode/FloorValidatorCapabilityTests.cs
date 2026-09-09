using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Help.Combat;
using Help.Crafting;
using Help.Dungeon;
using Help.Item;

namespace Tests.EditMode
{
    // EntryCondition.RequiredCapability는 예전부터 있었고 EntryRequirementChecker도 지키는데,
    // **FloorValidator만 이 필드를 통째로 무시**하고 있었다(Matches / Distinct / ConstraintCount).
    //
    // 그 상태로 능력 조건을 붙이면, 열쇠 계획이 "능력을 제공하지 않는 아이템"을 골라
    // 재료 보장 불변식이 조용히 거짓말을 한다 — 방에 들어갈 수는 있는데 클리어는 못 하는 층이 된다.
    public class FloorValidatorCapabilityTests
    {
        private RecipeDatabase _db;

        // 같은 재료로 만들 수 있지만 능력이 다른 두 아이템 — 능력을 안 보면 구분되지 않는다.
        [SetUp]
        public void SetUp()
        {
            _db = ScriptableObject.CreateInstance<RecipeDatabase>();
            _db.AddItem(Make("axe", Capability.BreakWall, (AlphabetMaterial.A, 1), (AlphabetMaterial.X, 1)));
            _db.AddItem(Make("flare", Capability.Melt, (AlphabetMaterial.F, 1), (AlphabetMaterial.L, 1)));
        }

        private static ItemDefinition Make(string id, Capability cap,
            params (AlphabetMaterial mat, int count)[] recipe)
        {
            var def = ScriptableObject.CreateInstance<ItemDefinition>();
            def.Id = id;
            def.Word = id.ToUpper() + "E";           // 기본 제작 규칙(E 포함)을 만족시켜야 열쇠가 된다
            def.Type = ItemType.Weapon;
            def.Capabilities = new List<Capability> { cap };
            def.Recipe = recipe.Select(r => new MaterialRequirement(r.mat, r.count)).ToList();
            return def;
        }

        private static EntryCondition Need(Capability cap) =>
            new EntryCondition { RequiredCapability = cap };

        // 능력 조건이면 그 능력을 제공하는 아이템의 재료가 계획돼야 한다.
        [Test]
        public void ShouldPlanMaterialsForTheCapabilityItem()
        {
            Assert.IsTrue(FloorValidator.TryPlanRequiredMaterials(
                new[] { Need(Capability.Melt) }, _db, out var pool, out var keys));

            Assert.AreEqual(1, keys.Count);
            Assert.AreEqual("flare", keys[0].Id, "Melt를 제공하는 아이템이 열쇠로 뽑혀야 한다");
            Assert.IsTrue(pool.ContainsKey(AlphabetMaterial.F) && pool.ContainsKey(AlphabetMaterial.L));
            Assert.IsFalse(pool.ContainsKey(AlphabetMaterial.X), "다른 능력 아이템의 재료가 섞였다");
        }

        // 핵심 회귀: 능력이 서로 다른 두 조건은 **각각** 열쇠가 필요하다.
        // Distinct가 능력을 안 보면 둘을 하나로 합쳐, 한 아이템만 계획하고 넘어간다.
        [Test]
        public void DifferentCapabilitiesShouldNeedSeparateKeys()
        {
            Assert.IsTrue(FloorValidator.TryPlanRequiredMaterials(
                new[] { Need(Capability.BreakWall), Need(Capability.Melt) }, _db, out var pool, out var keys));

            Assert.AreEqual(2, keys.Count, "능력이 다르면 열쇠도 둘이어야 한다");
            foreach (var m in new[] { AlphabetMaterial.A, AlphabetMaterial.X,
                                      AlphabetMaterial.F, AlphabetMaterial.L })
                Assert.IsTrue(pool.ContainsKey(m), $"{m} 재료가 계획에서 빠졌다");
        }

        [Test]
        public void SameCapabilityTwiceShouldReuseOneKey()
        {
            Assert.IsTrue(FloorValidator.TryPlanRequiredMaterials(
                new[] { Need(Capability.Melt), Need(Capability.Melt) }, _db, out _, out var keys));

            Assert.AreEqual(1, keys.Count, "같은 능력이면 열쇠 하나를 재사용한다");
        }

        // 제공하는 아이템이 없는 능력을 요구하면 층 자체가 성립하지 않는다 → 재생성 신호.
        [Test]
        public void ShouldFailWhenNoItemProvidesTheCapability()
        {
            Assert.IsFalse(FloorValidator.TryPlanRequiredMaterials(
                new[] { Need(Capability.Bind) }, _db, out _, out _));
        }
    }
}

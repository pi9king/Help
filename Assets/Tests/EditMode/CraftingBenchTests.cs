using System.Collections.Generic;
using NUnit.Framework;
using Help.Crafting;
using Help.Item;
using UnityEngine;

namespace Tests.EditMode
{
    // 발견형(슬롯 배치) 크래프팅: 슬롯에 글자를 놓아 정확히 일치하는 단어가 되면 그 아이템이 만들어진다.
    // 튜토리얼 시나리오: K + Y → KEY.
    public class CraftingBenchTests
    {
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

        // 튜토리얼 후보 DB: KEY(K,Y) + BLADE(B,L,A,D) + 재료 몇 개
        private static List<ItemDefinition> SampleDb()
        {
            return new List<ItemDefinition>
            {
                MakeItem("key", "KEY", ItemType.SubWeapon, AlphabetMaterial.K, AlphabetMaterial.Y),
                MakeItem("blade", "BLADE", ItemType.Weapon, AlphabetMaterial.B, AlphabetMaterial.L, AlphabetMaterial.A, AlphabetMaterial.D),
                MakeMaterial(AlphabetMaterial.K),
                MakeMaterial(AlphabetMaterial.Y),
            };
        }

        // 기본 제작은 "E가 들어간 단어"만 만든다 — 글자가 정확히 맞아도 E 없는 단어는 매칭되지 않는다.
        [Test]
        public void FindExact_RejectsWordWithoutE()
        {
            var db = SampleDb();
            db.Add(MakeItem("sword", "SWORD", ItemType.Weapon,
                AlphabetMaterial.S, AlphabetMaterial.W, AlphabetMaterial.O, AlphabetMaterial.R, AlphabetMaterial.D));

            var result = RecipeMatcher.FindExact(
                new[] { AlphabetMaterial.S, AlphabetMaterial.W, AlphabetMaterial.O, AlphabetMaterial.R, AlphabetMaterial.D }, db);

            Assert.IsNull(result, "SWORD는 E가 없으므로 기본 제작 불가");
        }

        // 특수 제작대에서는 같은 배치가 E 없는 단어로 매칭된다(발견형 매칭의 특수 모드).
        [Test]
        public void FindExact_SpecialModeMatchesWordWithoutE()
        {
            var db = SampleDb();
            db.Add(MakeItem("sword", "SWORD", ItemType.Weapon,
                AlphabetMaterial.S, AlphabetMaterial.W, AlphabetMaterial.O, AlphabetMaterial.R, AlphabetMaterial.D));
            var placed = new[] { AlphabetMaterial.S, AlphabetMaterial.W, AlphabetMaterial.O, AlphabetMaterial.R, AlphabetMaterial.D };

            Assert.IsNull(RecipeMatcher.FindExact(placed, db), "기본 모드에서는 여전히 불가");
            Assert.AreEqual("SWORD", RecipeMatcher.FindExact(placed, db, CraftMode.Special)?.Word);
        }

        [Test]
        public void FindExact_MatchesKeyFromKandY()
        {
            var result = RecipeMatcher.FindExact(new[] { AlphabetMaterial.K, AlphabetMaterial.Y }, SampleDb());
            Assert.IsNotNull(result);
            Assert.AreEqual("KEY", result.Word);
        }

        [Test]
        public void FindExact_IsOrderIndependent()
        {
            var result = RecipeMatcher.FindExact(new[] { AlphabetMaterial.Y, AlphabetMaterial.K }, SampleDb());
            Assert.AreEqual("KEY", result?.Word);
        }

        [Test]
        public void FindExact_RejectsExtraLetters()
        {
            // K,Y,Z 는 남는 글자(Z)가 있으므로 정확 일치 아님 → null
            var result = RecipeMatcher.FindExact(new[] { AlphabetMaterial.K, AlphabetMaterial.Y, AlphabetMaterial.Z }, SampleDb());
            Assert.IsNull(result);
        }

        [Test]
        public void FindExact_RejectsMissingLetters()
        {
            // K 하나만으로는 KEY가 안 됨(재료 자기참조 mat_K도 제외되므로 null)
            var result = RecipeMatcher.FindExact(new[] { AlphabetMaterial.K }, SampleDb());
            Assert.IsNull(result);
        }

        [Test]
        public void FindExact_EmptyPlacementReturnsNull()
        {
            Assert.IsNull(RecipeMatcher.FindExact(new AlphabetMaterial[0], SampleDb()));
        }

        [Test]
        public void Bench_PlaceAndRemoveTracksMaterials()
        {
            var bench = new CraftingBench(4);
            bench.Place(0, AlphabetMaterial.K);
            bench.Place(2, AlphabetMaterial.Y);

            var placed = bench.PlacedMaterials();
            Assert.AreEqual(2, placed.Count);
            Assert.Contains(AlphabetMaterial.K, placed);
            Assert.Contains(AlphabetMaterial.Y, placed);

            bench.Remove(0);
            Assert.AreEqual(1, bench.PlacedMaterials().Count);
            Assert.IsFalse(bench.IsEmpty);
        }

        [Test]
        public void Bench_ResultActivatesWhenLettersFormKey()
        {
            var db = SampleDb();
            var bench = new CraftingBench(4);

            Assert.IsNull(bench.Result(db), "빈 슬롯 → 결과 없음");

            bench.Place(0, AlphabetMaterial.K);
            Assert.IsNull(bench.Result(db), "K만 → 아직 결과 없음");

            bench.Place(1, AlphabetMaterial.Y);
            Assert.AreEqual("KEY", bench.Result(db)?.Word, "K+Y → KEY 활성화");
        }

        [Test]
        public void Bench_OutOfRangePlacementIgnored()
        {
            var bench = new CraftingBench(2);
            Assert.IsFalse(bench.Place(5, AlphabetMaterial.K));
            Assert.IsTrue(bench.IsEmpty);
        }

        [Test]
        public void Capability_UnlockHasStableIndex()
        {
            // 프리팹이 enum을 정수로 직렬화 — 값 순서가 바뀌면 기존 데이터가 깨진다.
            Assert.AreEqual(6, (int)Capability.Unlock);
        }
    }
}

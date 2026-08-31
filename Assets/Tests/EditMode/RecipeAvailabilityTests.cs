using System.Collections.Generic;
using NUnit.Framework;
using Help.Crafting;
using Help.Item;

namespace Tests.EditMode
{
    // 크래프팅 UI가 "재료별 필요/보유/부족" 내역을 표시하기 위한 순수 표시 로직 검증.
    // 판정(CanCraft)만이 아니라, 각 재료가 얼마나 필요하고 몇 개 있는지를 노출한다.
    public class RecipeAvailabilityTests
    {
        private static List<MaterialRequirement> Recipe(params (AlphabetMaterial mat, int count)[] reqs)
        {
            var list = new List<MaterialRequirement>();
            foreach (var (mat, count) in reqs)
                list.Add(new MaterialRequirement(mat, count));
            return list;
        }

        private static MaterialStatus StatusFor(List<MaterialStatus> statuses, AlphabetMaterial mat)
        {
            var found = statuses.Find(s => s.Material == mat);
            Assert.IsTrue(found.Required > 0, $"{mat} 항목이 결과에 없음");
            return found;
        }

        [Test]
        public void Evaluate_ReportsRequiredAndHavePerMaterial()
        {
            var recipe = Recipe((AlphabetMaterial.B, 1), (AlphabetMaterial.L, 1), (AlphabetMaterial.A, 1), (AlphabetMaterial.D, 1));
            var pool = new Dictionary<AlphabetMaterial, int> { { AlphabetMaterial.B, 1 }, { AlphabetMaterial.A, 3 } };

            var result = RecipeAvailability.Evaluate(recipe, pool);

            Assert.AreEqual(4, result.Count);
            Assert.AreEqual(1, StatusFor(result, AlphabetMaterial.B).Have);
            Assert.AreEqual(3, StatusFor(result, AlphabetMaterial.A).Have);
            Assert.AreEqual(0, StatusFor(result, AlphabetMaterial.L).Have); // 풀에 없음 → 0
        }

        [Test]
        public void Evaluate_AggregatesDuplicateMaterialEntries()
        {
            // HAMMER처럼 같은 글자(M)가 레시피에 두 번 들어오는 경우 하나로 합산되어야 한다.
            var recipe = Recipe((AlphabetMaterial.M, 1), (AlphabetMaterial.M, 1), (AlphabetMaterial.H, 1));
            var pool = new Dictionary<AlphabetMaterial, int> { { AlphabetMaterial.M, 1 }, { AlphabetMaterial.H, 1 } };

            var result = RecipeAvailability.Evaluate(recipe, pool);

            var m = StatusFor(result, AlphabetMaterial.M);
            Assert.AreEqual(2, m.Required);
            Assert.AreEqual(1, m.Have);
            Assert.AreEqual(1, m.Missing);
            Assert.IsFalse(m.Satisfied);
            // 중복 항목이 합쳐져 재료 종류 수만 남는다 (M, H)
            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void Evaluate_MissingIsZeroWhenSatisfied()
        {
            var recipe = Recipe((AlphabetMaterial.B, 2));
            var pool = new Dictionary<AlphabetMaterial, int> { { AlphabetMaterial.B, 5 } };

            var b = StatusFor(RecipeAvailability.Evaluate(recipe, pool), AlphabetMaterial.B);

            Assert.AreEqual(0, b.Missing);
            Assert.IsTrue(b.Satisfied);
        }

        [Test]
        public void Evaluate_ReportsMissingCountWhenShort()
        {
            var recipe = Recipe((AlphabetMaterial.R, 2));
            var pool = new Dictionary<AlphabetMaterial, int> { { AlphabetMaterial.R, 1 } };

            var r = StatusFor(RecipeAvailability.Evaluate(recipe, pool), AlphabetMaterial.R);

            Assert.AreEqual(1, r.Missing);
            Assert.IsFalse(r.Satisfied);
        }

        [Test]
        public void Evaluate_IsDeterministicallyOrderedByMaterial()
        {
            var recipe = Recipe((AlphabetMaterial.R, 1), (AlphabetMaterial.A, 1), (AlphabetMaterial.P, 1));
            var pool = new Dictionary<AlphabetMaterial, int>();

            var result = RecipeAvailability.Evaluate(recipe, pool);

            // enum 값 오름차순으로 안정 정렬 (A < P < R) → UI 표시 순서가 흔들리지 않음
            Assert.AreEqual(AlphabetMaterial.A, result[0].Material);
            Assert.AreEqual(AlphabetMaterial.P, result[1].Material);
            Assert.AreEqual(AlphabetMaterial.R, result[2].Material);
        }

        [Test]
        public void CanCraft_TrueOnlyWhenEveryMaterialSatisfied()
        {
            var recipe = Recipe((AlphabetMaterial.B, 1), (AlphabetMaterial.A, 2));
            var enough = new Dictionary<AlphabetMaterial, int> { { AlphabetMaterial.B, 1 }, { AlphabetMaterial.A, 2 } };
            var short_ = new Dictionary<AlphabetMaterial, int> { { AlphabetMaterial.B, 1 }, { AlphabetMaterial.A, 1 } };

            Assert.IsTrue(RecipeAvailability.CanCraft(recipe, enough));
            Assert.IsFalse(RecipeAvailability.CanCraft(recipe, short_));
        }

        [Test]
        public void CanCraft_MatchesEvaluateSatisfaction()
        {
            var recipe = Recipe((AlphabetMaterial.B, 1), (AlphabetMaterial.A, 2));
            var pool = new Dictionary<AlphabetMaterial, int> { { AlphabetMaterial.B, 1 }, { AlphabetMaterial.A, 1 } };

            var result = RecipeAvailability.Evaluate(recipe, pool);
            bool allSatisfied = result.TrueForAll(s => s.Satisfied);

            Assert.AreEqual(allSatisfied, RecipeAvailability.CanCraft(recipe, pool));
        }
    }
}

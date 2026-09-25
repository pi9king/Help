using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Help.Editor.Playtest;

namespace Tests.EditMode
{
    // 정적 재미 리포트의 "죽은 데이터" 측정이 **올바르게 세는지**만 검증한다.
    // 어떤 필드가 쓰여야 한다는 게임 규칙을 못 박는 테스트가 아니다.
    public class SymbolUsageScannerTests
    {
        private static List<SourceFile> Files(params (string path, string text)[] files) =>
            files.Select(f => new SourceFile(f.path, f.text)).ToList();

        [Test]
        public void ShouldSeparateReadsFromWritesOfMemberAccess()
        {
            var files = Files(
                ("A.cs", "int a = item.AttackBonus;\n" +
                         "item.AttackBonus = 3;\n" +
                         "if (x.AttackBonus == 3) {}\n"));

            var sites = SymbolUsageScanner.FindMemberUsages("AttackBonus", files);

            Assert.AreEqual(3, sites.Count);
            Assert.AreEqual(2, sites.Count(s => !s.IsWrite), "대입 우변과 비교(==)는 읽기");
            Assert.AreEqual(1, sites.Count(s => s.IsWrite), "'.X = ' 는 쓰기");
            Assert.AreEqual(2, sites.Single(s => s.IsWrite).Line, "줄 번호는 1부터");
        }

        // 이 코드베이스는 주석에 필드명을 자주 쓴다("AttackSpeedMult 같은 데이터가…").
        // 주석·문자열을 읽기로 세면 죽은 데이터가 살아 있는 것처럼 보인다.
        [Test]
        public void ShouldIgnoreCommentsAndStringLiterals()
        {
            var files = Files(
                ("A.cs", "// item.AttackSpeedMult 는 아직 안 쓴다\n" +
                         "Debug.Log(\"item.AttackSpeedMult\");\n" +
                         "var s = item.AttackSpeedMult; // 진짜 읽기\n"));

            var sites = SymbolUsageScanner.FindMemberUsages("AttackSpeedMult", files);

            Assert.AreEqual(1, sites.Count);
            Assert.AreEqual(3, sites[0].Line);
        }

        [Test]
        public void ShouldNotMatchLongerMemberNames()
        {
            var files = Files(("A.cs", "var a = item.AttackBonusX; var b = item.XAttackBonus;"));

            Assert.IsEmpty(SymbolUsageScanner.FindMemberUsages("AttackBonus", files));
        }

        // 값이 다른 이름으로 옮겨 담기면 거기서 끊기면 안 된다 —
        // WeaponCategory가 EquippedWeaponCategory로 옮겨진 뒤 아무도 안 읽는 경우를 잡아야 한다.
        [Test]
        public void ShouldFindSymbolsThatReceiveTheMemberValue()
        {
            var files = Files(
                ("P.cs", "        EquippedWeaponCategory = item.WeaponCategory;\n" +
                         "        _cat = other.WeaponCategory ?? x;\n" +
                         "        Use(item.WeaponCategory);\n"));

            var derived = SymbolUsageScanner.FindDerivedSymbols("WeaponCategory", files);

            CollectionAssert.AreEquivalent(new[] { "EquippedWeaponCategory", "_cat" }, derived);
        }

        [Test]
        public void IdentifierReadsShouldExcludeDeclarationsAndWrites()
        {
            var files = Files(
                ("P.cs", "public WeaponCategory EquippedWeaponCategory { get; private set; }\n" +
                         "EquippedWeaponCategory = item.WeaponCategory;\n" +
                         "EquippedWeaponCategory = WeaponCategory.None;\n"),
                ("Q.cs", "if (p.EquippedWeaponCategory == WeaponCategory.Axe) {}\n"));

            var reads = SymbolUsageScanner.FindIdentifierReads("EquippedWeaponCategory", files);

            Assert.AreEqual(1, reads.Count, "선언·대입을 뺀 진짜 읽기만");
            Assert.AreEqual("Q.cs", reads[0].Path);
        }
    }
}

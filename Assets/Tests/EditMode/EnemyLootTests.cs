using NUnit.Framework;
using Help.Enemy;
using Help.Combat;
using Help.Item;

namespace Tests.EditMode
{
    // 순수 로직: 적 처치 재료 드롭(속성 단어 글자 − E) 검증.
    public class EnemyLootTests
    {
        [Test]
        public void ForElement_Fire_DropsFIR()
        {
            CollectionAssert.AreEqual(
                new[] { AlphabetMaterial.F, AlphabetMaterial.I, AlphabetMaterial.R },
                EnemyLoot.ForElement(ElementType.Fire));
        }

        [Test]
        public void ForElement_Steel_RemovesBothEs()
        {
            CollectionAssert.AreEqual(
                new[] { AlphabetMaterial.S, AlphabetMaterial.T, AlphabetMaterial.L },
                EnemyLoot.ForElement(ElementType.Steel));
        }

        [Test]
        public void ForElement_Ether_RemovesBothEs()
        {
            CollectionAssert.AreEqual(
                new[] { AlphabetMaterial.T, AlphabetMaterial.H, AlphabetMaterial.R },
                EnemyLoot.ForElement(ElementType.Ether));
        }

        [Test]
        public void ForElement_None_IsEmpty()
        {
            Assert.IsEmpty(EnemyLoot.ForElement(ElementType.None));
        }

        [Test]
        public void ForElement_EveryLockedElement_HasNonEmptyDrop()
        {
            foreach (ElementType e in System.Enum.GetValues(typeof(ElementType)))
            {
                if (e == ElementType.None) continue;
                Assert.IsNotEmpty(EnemyLoot.ForElement(e), $"{e}는 드롭 재료가 있어야 함");
            }
        }

        // 조용한 글자 누락 방지: 드롭 개수 == (단어 길이 − E 개수). E 외의 글자는 절대 빠지면 안 됨.
        [Test]
        public void ForElement_DropCount_EqualsWordLengthMinusEs()
        {
            foreach (ElementType e in System.Enum.GetValues(typeof(ElementType)))
            {
                if (e == ElementType.None) continue;
                string word = e.ToString().ToUpperInvariant();
                int expected = 0;
                foreach (char c in word) if (c != 'E') expected++;
                Assert.AreEqual(expected, EnemyLoot.ForElement(e).Count,
                    $"{e}: E 외 글자가 조용히 누락됨");
            }
        }
    }
}

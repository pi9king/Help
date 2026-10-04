using NUnit.Framework;
using Help.Combat;
using Help.Item;
using UnityEngine;
using UnityEditor;
using Help.Crafting;

namespace Tests.EditMode
{
    public class AttackMotionClockTests
    {
        [Test]
        public void AxeShouldHaveWorldSpriteForHeldWeapon()
        {
            var axe = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/ScriptableObjects/Items/axe.asset");
            Assert.IsNotNull(axe);
            Assert.IsNotNull(axe.WorldSprite);
        }

        [Test]
        public void AxeShouldHaveWiderAndSlowerAttackThanUnarmed()
        {
            var axe = ScriptableObject.CreateInstance<ItemDefinition>();
            try
            {
                axe.Id = "axe";
                var motion = AttackMotionDef.ForWeapon(axe);
                var unarmed = AttackMotionDef.ForWeapon(null);

                Assert.AreEqual(AttackKind.MeleeArc, motion.Kind);
                Assert.That(motion.TotalDuration, Is.EqualTo(0.52f).Within(1e-5f));
                Assert.That(motion.Reach + motion.HitboxSize.x * 0.5f,
                    Is.EqualTo(1.32f).Within(1e-5f));
                Assert.That(motion.TotalDuration, Is.GreaterThan(unarmed.TotalDuration));
                Assert.That(motion.HitboxSize.y, Is.GreaterThan(unarmed.HitboxSize.y));
                Assert.That(motion.ArcStartDeg - motion.ArcEndDeg,
                    Is.GreaterThan(unarmed.ArcStartDeg - unarmed.ArcEndDeg));
            }
            finally { Object.DestroyImmediate(axe); }
        }

        [Test]
        public void MaceShouldStrikeNarrowerAndFasterThanAxe()
        {
            var mace = ScriptableObject.CreateInstance<ItemDefinition>();
            var axe = ScriptableObject.CreateInstance<ItemDefinition>();
            try
            {
                mace.Id = "mace";
                axe.Id = "axe";
                var motion = AttackMotionDef.ForWeapon(mace);
                var axeMotion = AttackMotionDef.ForWeapon(axe);

                Assert.AreEqual(AttackKind.MeleeArc, motion.Kind);
                Assert.That(motion.TotalDuration, Is.EqualTo(0.43f).Within(1e-5f));
                Assert.That(motion.TotalDuration, Is.LessThan(axeMotion.TotalDuration));
                Assert.That(motion.Reach + motion.HitboxSize.x * 0.5f,
                    Is.EqualTo(1.12f).Within(1e-5f));
                Assert.That(motion.HitboxSize.y, Is.LessThan(axeMotion.HitboxSize.y));
            }
            finally
            {
                Object.DestroyImmediate(mace);
                Object.DestroyImmediate(axe);
            }
        }

        [Test]
        public void MaceShouldBeRegisteredAsPlainCraftableWeapon()
        {
            var db = AssetDatabase.LoadAssetAtPath<RecipeDatabase>("Assets/ScriptableObjects/RecipeDatabase.asset");
            var mace = db.Find("mace");

            Assert.IsNotNull(mace);
            Assert.AreEqual("MACE", mace.Word);
            Assert.AreEqual(ItemType.Weapon, mace.Type);
            Assert.AreEqual(WeaponCategory.Mace, mace.WeaponCategory);
            Assert.AreEqual(3, mace.Recipe.Count);
            Assert.IsTrue(AlphabetWordRule.RecipeMatchesWord(mace.Word, mace.Recipe));
            Assert.IsEmpty(mace.Capabilities);
        }

        [TestCase("pike", .50f, 2.05f, .45f)]
        [TestCase("cane", .33f, 1.35f, .8f)]
        [TestCase("pipe", .38f, 1.25f, 1f)]
        [TestCase("net", .60f, 1.45f, 2.3f)]
        [TestCase("pen", .24f, .85f, .35f)]
        [TestCase("wire", .55f, 2.15f, 1f)]
        [TestCase("stone", .48f, .85f, .9f)]
        [TestCase("spade", .55f, 1.45f, 1.4f)]
        [TestCase("epee", .31f, 1.6f, .35f)]
        [TestCase("needle", .20f, .75f, .2f)]
        [TestCase("pestle", .40f, 1.0f, .85f)]
        [TestCase("skewer", .42f, 1.75f, .5f)]
        public void T1MotionShouldMatchItsFirstPassTimingAndRange(string id, float duration, float forward, float width)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            try
            {
                item.Id = id;
                var motion = AttackMotionDef.ForWeapon(item);
                Assert.AreEqual(AttackKind.MeleeArc, motion.Kind);
                Assert.That(motion.TotalDuration, Is.EqualTo(duration).Within(1e-5f));
                Assert.That(motion.Reach + motion.HitboxSize.x * .5f,
                    Is.EqualTo(forward).Within(1e-5f));
                Assert.That(motion.HitboxSize.y, Is.EqualTo(width).Within(1e-5f));
            }
            finally { Object.DestroyImmediate(item); }
        }

        [TestCase("pike", "PIKE", WeaponCategory.Pike)]
        [TestCase("cane", "CANE", WeaponCategory.Cane)]
        [TestCase("pipe", "PIPE", WeaponCategory.Pipe)]
        [TestCase("net", "NET", WeaponCategory.Net)]
        [TestCase("pen", "PEN", WeaponCategory.Pen)]
        [TestCase("wire", "WIRE", WeaponCategory.Wire)]
        [TestCase("stone", "STONE", WeaponCategory.Stone)]
        [TestCase("spade", "SPADE", WeaponCategory.Spade)]
        public void T1AssetShouldBeRegisteredAndCraftable(string id, string word, WeaponCategory category)
        {
            var db = AssetDatabase.LoadAssetAtPath<RecipeDatabase>("Assets/ScriptableObjects/RecipeDatabase.asset");
            var item = db.Find(id);
            Assert.IsNotNull(item);
            Assert.AreEqual(word, item.Word);
            Assert.AreEqual(ItemType.Weapon, item.Type);
            Assert.AreEqual(category, item.WeaponCategory);
            Assert.IsTrue(AlphabetWordRule.RecipeMatchesWord(word, item.Recipe));
            Assert.IsEmpty(item.Capabilities);
        }

        [TestCase("epee", "EPEE", WeaponCategory.Epee)]
        [TestCase("needle", "NEEDLE", WeaponCategory.Needle)]
        [TestCase("pestle", "PESTLE", WeaponCategory.Pestle)]
        [TestCase("skewer", "SKEWER", WeaponCategory.Skewer)]
        public void MultipleEWeaponShouldBeRegisteredForArenaButNotCraftableYet(
            string id, string word, WeaponCategory category)
        {
            var db = AssetDatabase.LoadAssetAtPath<RecipeDatabase>("Assets/ScriptableObjects/RecipeDatabase.asset");
            var item = db.Find(id);
            Assert.IsNotNull(item);
            Assert.AreEqual(word, item.Word);
            Assert.AreEqual(category, item.WeaponCategory);
            Assert.AreEqual(ItemType.Weapon, item.Type);
            Assert.IsEmpty(item.Recipe);
            Assert.IsEmpty(item.Capabilities);
        }

        [Test]
        public void OtherWeaponsShouldKeepDefaultMotionUntilImplemented()
        {
            var blade = ScriptableObject.CreateInstance<ItemDefinition>();
            try
            {
                blade.Id = "blade";
                Assert.AreEqual(AttackMotionDef.Default().TotalDuration,
                    AttackMotionDef.ForWeapon(blade).TotalDuration, 1e-5f);
            }
            finally { Object.DestroyImmediate(blade); }
        }

        [Test]
        public void ShouldBeInWindupBeforeActive()
        {
            var c = new AttackMotionClock(0.1f, 0.2f, 0.1f);
            c.Start();
            var r = c.Tick(0.05f);
            Assert.AreEqual(AttackPhase.Windup, r.Phase);
            Assert.IsFalse(r.IsActive, "예비동작 중엔 타격 판정이 열리면 안 됨");
        }

        [Test]
        public void ShouldOpenHitWindowDuringActive()
        {
            var c = new AttackMotionClock(0.1f, 0.2f, 0.1f);
            c.Start();
            c.Tick(0.1f);            // 예비 종료 직후
            var r = c.Tick(0.05f);   // t=0.15 → Active
            Assert.AreEqual(AttackPhase.Active, r.Phase);
            Assert.IsTrue(r.IsActive);
            Assert.That(r.ActiveProgress, Is.GreaterThan(0f).And.LessThan(1f));
        }

        [Test]
        public void ShouldEnterRecoveryAfterActive()
        {
            var c = new AttackMotionClock(0.1f, 0.2f, 0.1f);
            c.Start();
            c.Tick(0.31f); // t=0.31 → windup+active(0.3) 초과, total(0.4) 미만
            var r = c.Tick(0f);
            Assert.AreEqual(AttackPhase.Recovery, r.Phase);
            Assert.IsFalse(r.IsActive);
        }

        [Test]
        public void ShouldFinishAfterTotalDuration()
        {
            var c = new AttackMotionClock(0.1f, 0.2f, 0.1f);
            c.Start();
            var r = c.Tick(0.5f); // total(0.4) 초과
            Assert.AreEqual(AttackPhase.Done, r.Phase);
            Assert.IsFalse(c.Running);
        }

        [Test]
        public void ShouldReportDoneWhenNotStarted()
        {
            var c = new AttackMotionClock(0.1f, 0.2f, 0.1f);
            var r = c.Tick(0.1f);
            Assert.AreEqual(AttackPhase.Done, r.Phase);
        }

        [Test]
        public void TotalShouldSumPhases()
        {
            var c = new AttackMotionClock(0.03f, 0.12f, 0.10f);
            Assert.AreEqual(0.25f, c.Total, 1e-5f);
        }
    }
}

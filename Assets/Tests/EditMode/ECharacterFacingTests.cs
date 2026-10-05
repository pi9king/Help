using NUnit.Framework;
using UnityEngine;
using Help.Player;

namespace Tests.EditMode
{
    // 조준 벡터에서 4방향 시트의 행을 고르는 규칙.
    public class ECharacterFacingTests
    {
        [Test]
        public void ShouldPickAxisFacingFromCardinalAim()
        {
            Assert.That(ECharacterFacing.From(Vector2.down), Is.EqualTo(Facing.Down));
            Assert.That(ECharacterFacing.From(Vector2.up), Is.EqualTo(Facing.Up));
            Assert.That(ECharacterFacing.From(Vector2.left), Is.EqualTo(Facing.Left));
            Assert.That(ECharacterFacing.From(Vector2.right), Is.EqualTo(Facing.Right));
        }

        [Test]
        public void ShouldPickDominantAxisWhenAimIsOffCardinal()
        {
            Assert.That(ECharacterFacing.From(new Vector2(1f, 0.3f)), Is.EqualTo(Facing.Right));
            Assert.That(ECharacterFacing.From(new Vector2(-1f, 0.3f)), Is.EqualTo(Facing.Left));
            Assert.That(ECharacterFacing.From(new Vector2(0.3f, 1f)), Is.EqualTo(Facing.Up));
            Assert.That(ECharacterFacing.From(new Vector2(0.3f, -1f)), Is.EqualTo(Facing.Down));
        }

        [Test]
        public void ShouldPreferVerticalOnExactDiagonal()
        {
            // 45도에서 세로를 고른다. 위/아래 시트가 정면·후면이라 읽기가 더 분명하다.
            Assert.That(ECharacterFacing.From(new Vector2(1f, 1f)), Is.EqualTo(Facing.Up));
            Assert.That(ECharacterFacing.From(new Vector2(1f, -1f)), Is.EqualTo(Facing.Down));
            Assert.That(ECharacterFacing.From(new Vector2(-1f, 1f)), Is.EqualTo(Facing.Up));
            Assert.That(ECharacterFacing.From(new Vector2(-1f, -1f)), Is.EqualTo(Facing.Down));
        }

        [Test]
        public void ShouldFallBackToDownWhenAimIsZero()
        {
            Assert.That(ECharacterFacing.From(Vector2.zero), Is.EqualTo(Facing.Down));
        }

        [Test]
        public void ShouldBuildAnimatorStateNameFromActionAndFacing()
        {
            Assert.That(ECharacterFacing.StateName("Idle", Facing.Down), Is.EqualTo("Idle_Down"));
            Assert.That(ECharacterFacing.StateName("Walk", Facing.Left), Is.EqualTo("Walk_Left"));
            Assert.That(ECharacterFacing.StateName("Attack", Facing.Up), Is.EqualTo("Attack_Up"));
        }

        [Test]
        public void ShouldCollapseCommonActionsToDownRow()
        {
            // Death와 Skill은 Down 공통 클립뿐이다.
            Assert.That(ECharacterFacing.StateName("Death", Facing.Left), Is.EqualTo("Death_Down"));
            Assert.That(ECharacterFacing.StateName("Skill", Facing.Right), Is.EqualTo("Skill_Down"));
        }
    }
}

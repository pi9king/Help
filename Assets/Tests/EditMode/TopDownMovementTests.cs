using NUnit.Framework;
using UnityEngine;
using Help.Player;

namespace Tests.EditMode
{
    public class TopDownMovementTests
    {
        [Test]
        public void DiagonalInputShouldNotMoveFasterThanCardinalInput()
        {
            Vector2 cardinal = TopDownMovement.Velocity(Vector2.right, 7f);
            Vector2 diagonal = TopDownMovement.Velocity(new Vector2(1f, 1f), 7f);

            Assert.That(cardinal.magnitude, Is.EqualTo(7f).Within(0.001f));
            Assert.That(diagonal.magnitude, Is.EqualTo(7f).Within(0.001f));
        }

        [Test]
        public void DashShouldUseNormalizedCurrentInput()
        {
            Vector2 direction = TopDownMovement.DashDirection(new Vector2(1f, 1f), Vector2.left);

            Assert.That(direction.magnitude, Is.EqualTo(1f).Within(0.001f));
            Assert.That(direction.x, Is.GreaterThan(0f));
            Assert.That(direction.y, Is.GreaterThan(0f));
        }

        [Test]
        public void StationaryDashShouldUseLastFacingDirection()
        {
            Vector2 direction = TopDownMovement.DashDirection(Vector2.zero, Vector2.up);

            Assert.That(direction, Is.EqualTo(Vector2.up));
        }

        [Test]
        public void DashShouldFallBackToDownWhenNoDirectionExists()
        {
            Vector2 direction = TopDownMovement.DashDirection(Vector2.zero, Vector2.zero);

            Assert.That(direction, Is.EqualTo(Vector2.down));
        }
    }
}

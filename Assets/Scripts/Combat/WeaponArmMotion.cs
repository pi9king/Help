using UnityEngine;

namespace Help.Combat
{
    public struct ArmPose
    {
        public float LeftDegrees;
        public float RightDegrees;
        public float ForwardOffset;

        public ArmPose(float leftDegrees, float rightDegrees, float forwardOffset = 0f)
        {
            LeftDegrees = leftDegrees;
            RightDegrees = rightDegrees;
            ForwardOffset = forwardOffset;
        }

        // E 양옆에서 손끝이 위를 향하는 기본 자세.
        public static ArmPose Rest => new ArmPose(135f, 45f);
    }

    // 두 팔의 어깨 회전과 전진량. 공격 판정과 같은 타이밍 값을 사용한다.
    public static class WeaponArmMotion
    {
        public static ArmPose Evaluate(string weaponId, Vector2 aimDirection, float elapsed, AttackMotionDef motion)
        {
            if (motion == null || elapsed >= motion.TotalDuration) return ArmPose.Rest;
            float aim = AimGeometry.AngleDegrees(AimGeometry.DirectionOrDefault(aimDirection));

            ArmPose windup;
            ArmPose strike;
            switch (weaponId)
            {
                case "axe":
                    windup = new ArmPose(aim + 105f, aim + 80f);
                    strike = new ArmPose(aim - 105f, aim - 80f);
                    break;
                case "mace":
                    windup = new ArmPose(aim + 170f, aim + 100f);
                    strike = new ArmPose(aim + 160f, aim - 70f);
                    break;
                case "pike":
                    windup = new ArmPose(aim + 160f, aim + 155f, -.05f);
                    strike = new ArmPose(aim + 15f, aim - 5f, .16f);
                    break;
                case "cane":
                    windup = new ArmPose(180f, aim - 75f);
                    strike = new ArmPose(180f, aim + 65f);
                    break;
                default:
                    windup = new ArmPose(180f, aim + 75f);
                    strike = new ArmPose(180f, aim - 75f);
                    break;
            }

            if (elapsed < motion.Windup)
                return Blend(ArmPose.Rest, windup, Progress(elapsed, motion.Windup));
            elapsed -= motion.Windup;
            if (elapsed < motion.Active)
                return Blend(windup, strike, Progress(elapsed, motion.Active));
            elapsed -= motion.Active;
            // 회수할 때 같은 방향의 360도 회전을 하지 않도록 가장 가까운 휴식 각도를 고른다.
            var rest = new ArmPose(
                strike.LeftDegrees + Mathf.DeltaAngle(strike.LeftDegrees, ArmPose.Rest.LeftDegrees),
                strike.RightDegrees + Mathf.DeltaAngle(strike.RightDegrees, ArmPose.Rest.RightDegrees));
            return Blend(strike, rest, Progress(elapsed, motion.Recovery));
        }

        private static float Progress(float elapsed, float duration) =>
            duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;

        // 타격 구간은 지정한 회전 방향을 따른다.
        private static ArmPose Blend(ArmPose from, ArmPose to, float t) => new ArmPose(
            Mathf.Lerp(from.LeftDegrees, to.LeftDegrees, t),
            Mathf.Lerp(from.RightDegrees, to.RightDegrees, t),
            Mathf.Lerp(from.ForwardOffset, to.ForwardOffset, t));
    }
}

using UnityEngine;

namespace Help.Combat
{
    // 팔 리그 모션을 "방향별 화면 키 자세"로 정의한다. Docs/ARM_RIG_PLAN.md.
    //
    // 몸 기준 3D 경로를 쿼터뷰로 투영하던 방식(2026-10-06 시도 후 삭제)은 물리적으로는 맞았지만 이 시점에서 잘 읽히지 않았다
    // (옆모습 베기가 한 바퀴 도는 것처럼, 카메라 쪽 찌르기는 팔이 펴지지 않고, Up은 전부 몸 뒤로 숨었다).
    // 그래서 전통 2D 애니메이션처럼 방향마다 화면에서 읽히는 핵심 자세를 직접 정한다:
    //   Hand   어깨 기준 손의 화면 위치(px, y 위)   -> 팔은 TwoBoneIk가 따라간다
    //   Weapon 무기의 화면 각도(0° = 오른쪽)       -> 팔이 접혀도 칼끝이 뒤집히지 않는다
    //   Front  무기 팔을 몸 앞에 그릴지
    //   Shoulder  어깨 위치 이동(px) — 찌르기처럼 어깨까지 밀어 넣는 동작. 손은 어깨 기준이라 같이 나간다
    //   ElbowFlip IK의 다른 해(팔꿈치를 반대쪽으로). 주먹을 몸쪽으로 당기면 "처지는 팔꿈치"가 거꾸로 꺾여 보인다
    public struct ArmKey
    {
        public Vector2 Hand;
        public float Weapon;
        public bool Front;
        public Vector2 Shoulder;
        public bool ElbowFlip;
    }

    public enum ArmStrikeCurve
    {
        Smooth,       // 베기: 부드럽게 휘두른다
        Accelerate,   // 내려찍기: 가속하며 내려친다
        Snap,         // 찌르기: 튀어 나간다
    }

    // 장전(예비동작 70% 지점) · 힘 모으기(예비동작 끝) · 타격 중간 · 타격 끝. 휴식 자세는 방향마다 따로 준다.
    public struct ArmKeyTrack
    {
        public ArmKey Loaded;
        public ArmKey Cocked;
        public ArmKey Mid;
        public ArmKey Through;
        public ArmStrikeCurve Curve;
    }

    // 리듬은 ArmMotion과 같다: 장전(감속) -> 힘 모으기 -> 순간 타격(Mid를 지나는 곡선) -> 끝 자세에서 정지 -> 회수.
    public static class ArmKeyMotion
    {
        public static ArmKey Evaluate(ArmKeyTrack track, ArmKey rest, float elapsed,
                                      float windup, float active, float recovery)
        {
            if (elapsed <= 0f) return rest;

            float loadTime = windup * (1f - ArmMotion.HoldShare);
            if (elapsed < loadTime)
                return Lerp(rest, track.Loaded, EaseOut(elapsed / loadTime));
            if (elapsed < windup)
                return Lerp(track.Loaded, track.Cocked, (elapsed - loadTime) / Mathf.Max(0.0001f, windup - loadTime));
            if (elapsed < windup + active)
            {
                float t = active > 0f ? Mathf.Clamp01((elapsed - windup) / (active * ArmMotion.StrikeShare)) : 1f;
                return Through(track.Cocked, track.Mid, track.Through, Curve(track.Curve, t));
            }
            float back = recovery > 0f ? Smooth((elapsed - windup - active) / recovery) : 1f;
            return Lerp(track.Through, rest, back);
        }

        // u = 0 -> a, 0.5 -> mid, 1 -> b 를 지나는 2차 곡선. 각도도 같은 식을 숫자 그대로 쓴다 —
        // 키에 적은 순서대로 돌고, 최단 경로로 뒤집히지 않는다.
        private static ArmKey Through(ArmKey a, ArmKey mid, ArmKey b, float u)
        {
            return new ArmKey
            {
                Hand = new Vector2(Quadratic(a.Hand.x, mid.Hand.x, b.Hand.x, u),
                                   Quadratic(a.Hand.y, mid.Hand.y, b.Hand.y, u)),
                Weapon = Quadratic(a.Weapon, mid.Weapon, b.Weapon, u),
                Front = u < 0.25f ? a.Front : u < 0.75f ? mid.Front : b.Front,
                Shoulder = new Vector2(Quadratic(a.Shoulder.x, mid.Shoulder.x, b.Shoulder.x, u),
                                       Quadratic(a.Shoulder.y, mid.Shoulder.y, b.Shoulder.y, u)),
                ElbowFlip = u < 0.25f ? a.ElbowFlip : u < 0.75f ? mid.ElbowFlip : b.ElbowFlip,
            };
        }

        private static float Quadratic(float a, float mid, float b, float u)
        {
            float control = 2f * mid - (a + b) * 0.5f;
            float v = 1f - u;
            return v * v * a + 2f * u * v * control + u * u * b;
        }

        private static ArmKey Lerp(ArmKey a, ArmKey b, float t) => new ArmKey
        {
            Hand = Vector2.Lerp(a.Hand, b.Hand, t),
            Weapon = Mathf.Lerp(a.Weapon, b.Weapon, t),
            Front = t < 0.5f ? a.Front : b.Front,
            Shoulder = Vector2.Lerp(a.Shoulder, b.Shoulder, t),
            ElbowFlip = t < 0.5f ? a.ElbowFlip : b.ElbowFlip,
        };

        private static float Curve(ArmStrikeCurve curve, float t)
        {
            switch (curve)
            {
                case ArmStrikeCurve.Accelerate: return t * t;
                case ArmStrikeCurve.Snap: return EaseOut(t);
                default: return Smooth(t);
            }
        }

        private static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private static float EaseOut(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - (1f - t) * (1f - t);
        }
    }
}

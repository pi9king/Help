using UnityEngine;

namespace Help.Combat
{
    // 팔 리그의 기본 공격 모션(순수 로직 — EditMode 테스트 가능). Docs/ARM_RIG_PLAN.md.
    // 무기별 세부 동작은 무기를 하나씩 구현할 때 사용자와 정한다. 이 셋은 출발점이다.
    public enum ArmMotionKind { Slash, Thrust, Smash }

    // 리그 각도 규약: 0° = 팔이 아래로 늘어진 자세, + = 반시계(Unity z 회전).
    // Main = 무기 든 팔, Off = 반대 팔(자기 좌우반전 공간 기준, + = 바깥쪽).
    public struct ArmRigPose
    {
        public float MainShoulder;
        public float MainElbow;
        public float OffShoulder;
        public float OffElbow;
        public float WeaponAngle;   // 아랫팔 기준 무기 각도(손목). 180 = 날이 팔의 연장선
        public float Reach;         // 조준 방향으로 어깨를 미는 거리(월드 유닛)
        public float BodyLean;      // 상체를 조준 쪽으로 싣는 양(px). - = 뒤로 뺌
        public float BodyDrop;      // 상체를 낮추는 양(px). + = 웅크림, - = 일어섬
    }

    // 리듬 (팔만 같은 속도로 도는 "휘적거림"을 없애기 위한 것):
    //   예비동작 앞 70%  휴식 -> 장전 자세        (감속)
    //   예비동작 뒤 30%  장전 -> 조금 더 당김      (힘 모으기, 거의 정지)
    //   타격 구간 앞 45% 당김 -> 오버슈트          (순식간에 지나감)
    //   타격 구간 나머지 오버슈트에서 정지         (타격의 무게)
    //   회수             오버슈트 -> 휴식
    public static class ArmMotion
    {
        public const float HoldShare = 0.3f;
        public const float StrikeShare = 0.45f;

        public static ArmRigPose Rest => new ArmRigPose
        {
            MainShoulder = 10f, MainElbow = 10f, OffShoulder = 10f, OffElbow = 10f, WeaponAngle = 180f
        };

        // 리그 어깨 각도 <-> 팔이 가리키는 월드 각도(0° = 오른쪽).
        public static float PointingDegrees(float shoulder) => shoulder - 90f;
        public static float ShoulderFor(float pointing) => pointing + 90f;

        // 리그 시간을 스프라이트 박자(fps)로 끊는다. 연속 회전은 끊어 그린 픽셀아트 사이에서 미끄러져 보이고,
        // 회전마다 픽셀이 바뀌어 반짝인다. fps <= 0이면 끊지 않는다.
        public static float StepTime(float elapsed, float fps) =>
            fps > 0f ? Mathf.Floor(elapsed * fps + 0.0001f) / fps : elapsed;

        // 휘두르는 순간(타격 구간)만 더 촘촘하게 끊는다. 12fps로는 타격이 한 장에 지나가 팔이 사라졌다
        // 나타나는 것처럼 보였다(사용자: "팔이 사라지는 거 거슬리는데, 프레임 추가해줘").
        public static float StepTime(float elapsed, float strikeStart, float strikeEnd, float fps, float strikeFps)
        {
            if (elapsed < strikeStart || elapsed >= strikeEnd) return StepTime(elapsed, fps);
            return Mathf.Max(strikeStart, StepTime(elapsed, strikeFps));
        }

        // 타격(스윙)이 시작됐는가 — 이펙트를 띄울 시점.
        public static bool StrikeStarted(float elapsed, float windup) => elapsed >= windup;

        public static ArmRigPose Evaluate(ArmMotionKind kind, float aimDegrees, float elapsed,
                                          float windup, float active, float recovery)
        {
            ArmRigPose rest = Rest;
            if (elapsed <= 0f) return rest;

            ArmRigPose loaded = Loaded(kind, aimDegrees, rest);
            ArmRigPose cocked = Cocked(kind, loaded);
            float loadTime = windup * (1f - HoldShare);
            if (elapsed < loadTime)
                return Blend(rest, loaded, EaseOut(elapsed / loadTime));
            if (elapsed < windup)
                return Blend(loaded, cocked, (elapsed - loadTime) / Mathf.Max(0.0001f, windup - loadTime));

            ArmRigPose follow = FollowThrough(kind, loaded);
            float afterWindup = elapsed - windup;
            if (afterWindup < active)
            {
                float t = active > 0f ? Mathf.Clamp01(afterWindup / (active * StrikeShare)) : 1f;
                return Blend(cocked, follow, StrikeCurve(kind, t));
            }

            float afterActive = afterWindup - active;
            // 회수는 가장 가까운 쪽으로 돌아온다 — 한 바퀴 더 돌지 않게.
            ArmRigPose home = rest;
            home.MainShoulder = follow.MainShoulder + Mathf.DeltaAngle(follow.MainShoulder, rest.MainShoulder);
            return Blend(follow, home, recovery > 0f ? Smooth(afterActive / recovery) : 1f);
        }

        // 예비동작 앞부분이 끝난 장전 자세.
        private static ArmRigPose Loaded(ArmMotionKind kind, float aim, ArmRigPose rest)
        {
            ArmRigPose pose = rest;
            switch (kind)
            {
                case ArmMotionKind.Slash:      // 조준 바깥쪽으로 들고 손목을 뒤로 젖힌다, 상체는 살짝 뒤로·아래로
                    pose.MainShoulder = Near(ShoulderFor(aim + 75f), rest.MainShoulder);
                    pose.MainElbow = 45f;
                    pose.WeaponAngle = 230f;
                    pose.OffShoulder = 25f;
                    pose.BodyLean = -1f;
                    pose.BodyDrop = 1f;
                    break;
                case ArmMotionKind.Thrust:     // 조준을 향한 채 주먹을 몸쪽으로 당기고 상체를 뒤로 뺀다
                    pose.MainShoulder = Near(ShoulderFor(aim), rest.MainShoulder);
                    pose.MainElbow = 110f;
                    pose.Reach = -0.06f;
                    pose.OffShoulder = 40f;
                    pose.OffElbow = 60f;
                    pose.BodyLean = -2f;
                    pose.BodyDrop = 1f;
                    break;
                case ArmMotionKind.Smash:      // 조준 반대편 머리 뒤로 높이 들며 몸을 편다
                    pose.MainShoulder = Near(ShoulderFor(aim + 150f), rest.MainShoulder);
                    pose.MainElbow = 60f;
                    pose.WeaponAngle = 220f;
                    pose.OffShoulder = 30f;
                    pose.OffElbow = 40f;
                    pose.BodyLean = -1f;
                    pose.BodyDrop = -1f;
                    break;
            }
            return pose;
        }

        // 힘 모으기: 장전 자세에서 조금 더 당긴다.
        private static ArmRigPose Cocked(ArmMotionKind kind, ArmRigPose loaded)
        {
            ArmRigPose pose = loaded;
            switch (kind)
            {
                case ArmMotionKind.Thrust:
                    pose.MainElbow += 5f;
                    pose.Reach -= 0.02f;
                    break;
                default:
                    pose.MainShoulder += 5f;
                    pose.WeaponAngle += 10f;
                    break;
            }
            return pose;
        }

        // 타격이 지나가 멈추는 자세(목표보다 조금 더 나간 오버슈트). 휘두르는 방향은 숫자로 정해
        // 최단 경로로 뒤집히지 않게 한다.
        private static ArmRigPose FollowThrough(ArmMotionKind kind, ArmRigPose loaded)
        {
            ArmRigPose pose = loaded;
            switch (kind)
            {
                case ArmMotionKind.Slash:      // 조준 -75°를 지나 -85°까지, 손목은 앞으로 챈다
                    pose.MainShoulder = loaded.MainShoulder - 160f;
                    pose.MainElbow = 5f;
                    pose.WeaponAngle = 160f;
                    pose.OffShoulder = -10f;
                    pose.BodyLean = 2f;
                    pose.BodyDrop = 0f;
                    break;
                case ArmMotionKind.Thrust:     // 팔을 끝까지 뻗고 어깨·상체가 따라 나간다
                    pose.MainElbow = 0f;
                    pose.Reach = 0.15f;
                    pose.BodyLean = 3f;
                    pose.BodyDrop = 0f;
                    break;
                case ArmMotionKind.Smash:      // 조준 -10°를 지나 -18°까지 내려치며 웅크린다
                    pose.MainShoulder = loaded.MainShoulder - 168f;
                    pose.MainElbow = 0f;
                    pose.WeaponAngle = 165f;
                    pose.BodyLean = 2f;
                    pose.BodyDrop = 2f;
                    break;
            }
            return pose;
        }

        private static float StrikeCurve(ArmMotionKind kind, float t)
        {
            switch (kind)
            {
                case ArmMotionKind.Thrust: return EaseOut(t);   // 튀어 나간다
                case ArmMotionKind.Smash: return t * t;         // 가속하며 내려친다
                default: return Smooth(t);
            }
        }

        private static float Near(float angle, float reference) => reference + Mathf.DeltaAngle(reference, angle);

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

        private static ArmRigPose Blend(ArmRigPose a, ArmRigPose b, float t) => new ArmRigPose
        {
            MainShoulder = Mathf.Lerp(a.MainShoulder, b.MainShoulder, t),
            MainElbow = Mathf.Lerp(a.MainElbow, b.MainElbow, t),
            OffShoulder = Mathf.Lerp(a.OffShoulder, b.OffShoulder, t),
            OffElbow = Mathf.Lerp(a.OffElbow, b.OffElbow, t),
            WeaponAngle = Mathf.Lerp(a.WeaponAngle, b.WeaponAngle, t),
            Reach = Mathf.Lerp(a.Reach, b.Reach, t),
            BodyLean = Mathf.Lerp(a.BodyLean, b.BodyLean, t),
            BodyDrop = Mathf.Lerp(a.BodyDrop, b.BodyDrop, t),
        };
    }
}

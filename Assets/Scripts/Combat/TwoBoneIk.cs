using UnityEngine;

namespace Help.Combat
{
    // 팔 리그의 2마디 IK(어깨-팔꿈치-손). 키 자세의 손 위치에서 팔 각도를 푼다. Docs/ARM_RIG_PLAN.md.
    public static class TwoBoneIk
    {
        // 어깨(원점)에서 target까지 두 마디로 닿는 각도(화면 기준, 0° = 오른쪽). 닿지 않으면 그쪽으로 쭉 뻗는다.
        // 해가 둘이면 팔꿈치가 아래로 처지는 쪽, 높이가 같으면 바깥쪽을 고른다. flip이면 그 반대 해.
        public static void Solve(Vector2 target, float upperLength, float foreLength,
                                 out float upperDegrees, out float foreDegrees) =>
            Solve(target, upperLength, foreLength, false, out upperDegrees, out foreDegrees);

        public static void Solve(Vector2 target, float upperLength, float foreLength, bool flip,
                                 out float upperDegrees, out float foreDegrees)
        {
            float distance = Mathf.Clamp(target.magnitude,
                Mathf.Abs(upperLength - foreLength) + 0.001f, upperLength + foreLength - 0.0001f);
            float toTarget = Mathf.Atan2(target.y, target.x);
            float cos = (upperLength * upperLength + distance * distance - foreLength * foreLength)
                        / (2f * upperLength * distance);
            float spread = Mathf.Acos(Mathf.Clamp(cos, -1f, 1f));

            float a = toTarget + spread, b = toTarget - spread;
            Vector2 elbowA = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * upperLength;
            Vector2 elbowB = new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * upperLength;
            bool pickA = Mathf.Abs(elbowA.y - elbowB.y) > 0.01f
                ? elbowA.y < elbowB.y
                : Mathf.Abs(elbowA.x) > Mathf.Abs(elbowB.x);
            if (flip) pickA = !pickA;   // 반대쪽 해
            float upper = pickA ? a : b;
            Vector2 elbow = pickA ? elbowA : elbowB;

            Vector2 hand = target.sqrMagnitude > 0f ? target.normalized * distance : Vector2.right * distance;
            Vector2 fore = hand - elbow;
            upperDegrees = upper * Mathf.Rad2Deg;
            foreDegrees = Mathf.Atan2(fore.y, fore.x) * Mathf.Rad2Deg;
        }
    }
}

namespace Help.Player
{
    // 대시를 지금 쓸 수 있는가 (순수).
    //
    // D-3(2026-09-07 인터뷰): 공중 대시는 **착지할 때까지 1회**.
    // 쿨다운은 시간 조건이라 체공이 길어지면 공중 연타가 가능했다 —
    // 착지를 조건으로 걸어야 "갭 하나당 대시 하나"가 보장되고,
    // 도달성 시뮬도 "한 체공에 대시 1회"라는 단순한 모델이 된다.
    public static class DashRule
    {
        public static bool CanDash(bool grounded, bool airDashUsed, float cooldownTimer)
        {
            if (cooldownTimer > 0f) return false;   // 쿨다운은 공중/지상 공통
            return grounded || !airDashUsed;
        }
    }
}

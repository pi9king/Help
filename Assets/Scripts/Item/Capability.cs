namespace Help.Item
{
    // 아이템이 "제공"하고 퍼즐/문/장애물이 "요구"하는 기능 태그.
    // 아이템 타입과 직교 — 무기든 소비템이든 능력을 가질 수 있다.
    // 속성(ElementType)·무기종류(WeaponCategory)와 병렬인 요구 축(핵심 판정: 그 능력 주는 아이템을 지금 재료로 만들 수 있나).
    // 확장: 값 추가만으로 새 능력 도입(enum + switch 컨벤션).
    public enum Capability
    {
        None,       // 조건 없음
        BreakWall,  // 벽 부수기
        CrossGap,   // 틈 건너기(로프 등)
        Melt,       // 녹이기(열)
        Conduct,    // 전도(전기)
        Bind,       // 속박(적을 묶음 — 로프 등). 장애물이 아닌 "적"이 요구자가 되는 첫 능력
        Unlock,     // 잠긴 문 열기(열쇠 등). KEY가 제공 — 튜토리얼 첫 관문
        // ⚠ 새 값은 반드시 여기(끝)에 추가 — 프리팹이 enum을 정수로 직렬화한다(CapabilityMatchTests가 방어)
    }
}

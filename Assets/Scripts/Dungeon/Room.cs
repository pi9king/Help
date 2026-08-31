using System.Collections.Generic;
using Help.Combat;
using Help.Item;

namespace Help.Dungeon
{
    public class EntryCondition
    {
        public ElementType RequiredElement;   // ElementType.None = 조건 없음
        public WeaponCategory RequiredWeapon; // WeaponCategory.None = 조건 없음
        public Capability RequiredCapability; // Capability.None = 조건 없음
    }

    public class Room
    {
        public int X { get; }
        public int Y { get; }
        public RoomType Type { get; }
        public List<EntryCondition> EntryConditions { get; } = new();
        // 이 방을 클리어하면 확정 지급되는 재료 (자유 방일 때 층 재료 풀 계산에 사용).
        // 재료 보장 불변식의 "검증 대상" — 진입 열쇠 제작에 필요한 재료만 여기에 들어가며,
        // 도달 가능한 자유 방에만 배치된다(교착 방지).
        public List<MaterialRequirement> GuaranteedLoot { get; } = new();

        // 층 예산의 나머지 글자(보너스). 검증 대상이 아니므로 조건부/보스 방에도 놓일 수 있다 —
        // 그 방을 열 열쇠는 GuaranteedLoot이 이미 보장하므로 교착이 되지 않는다.
        public List<MaterialRequirement> BonusLoot { get; } = new();
        public bool IsCleared { get; private set; }

        // 상하좌우 연결 방 좌표 (null = 연결 없음)
        public (int x, int y)? North { get; set; }
        public (int x, int y)? South { get; set; }
        public (int x, int y)? East { get; set; }
        public (int x, int y)? West { get; set; }

        public Room(int x, int y, RoomType type)
        {
            X = x;
            Y = y;
            Type = type;
        }

        public bool IsFreeEntry => EntryConditions.Count == 0;

        public void SetCleared() => IsCleared = true;
    }
}

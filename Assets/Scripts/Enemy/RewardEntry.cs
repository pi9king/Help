using Help.Item;

namespace Help.Enemy
{
    // 알파벳 외 보상 테이블 한 줄(골드/포션). 인스펙터에서 적 프리팹별로 채운다.
    // 알파벳(DropEntry)은 층 예산에 묶이지만, 이쪽은 순수 확률 보너스다.
    [System.Serializable]
    public class RewardEntry
    {
        public RewardKind Kind;
        public int Amount = 1;
        [UnityEngine.Range(0f, 1f)] public float Chance = 0.5f;
    }
}

using UnityEngine;
using Help.Item;

namespace Help.Enemy
{
    // 적의 드랍 테이블 한 줄: 어떤 재료(글자)를 몇 개, 어떤 확률로 떨어뜨리는가.
    // ※ 역할 = 보너스(여분) 재료. 클리어에 필수인 재료는 방 GuaranteedLoot(확정 스폰)로 보장되며,
    //    적 드랍(확률)은 크래프팅 다양성을 위한 덤이다 — FloorValidator 검증에 포함되지 않는다.
    [System.Serializable]
    public class DropEntry
    {
        public AlphabetMaterial Material;
        public int Count = 1;
        [Range(0f, 1f)] public float Chance = 1f;
    }
}

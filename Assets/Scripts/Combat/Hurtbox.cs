using UnityEngine;
using Help.Enemy;

namespace Help.Combat
{
    // 적에게 부착하는 피격 판정 콜라이더
    [RequireComponent(typeof(Collider2D))]
    public class Hurtbox : MonoBehaviour
    {
        public EnemyStats OwnerStats { get; private set; }
        // 상태이상 핸들 — 적용원(서브무기 사용 등)이 적에게 효과를 걸 때의 진입점
        public EnemyStatus OwnerStatus { get; private set; }

        public void Init(EnemyStats stats, EnemyStatus status)
        {
            OwnerStats = stats;
            OwnerStatus = status;
            GetComponent<Collider2D>().isTrigger = true;
        }
    }
}

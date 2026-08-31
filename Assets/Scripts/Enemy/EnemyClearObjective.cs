using UnityEngine;

namespace Help.Enemy
{
    // 방 클리어 목표 "적 전멸". 콘텐츠 트리의 EnemyBase들을 수집해 처치를 집계하고,
    // 전부 죽으면 IsMet=true + OnMet 발화. RoomPuzzle이 이 목표를 SolveTracker에 등록해
    // 전멸 시 출구 잠금을 해제한다(기존 퍼즐 게이팅 프레임워크 재사용).
    // 집계는 순수 로직 EnemyCounter에 위임.
    public class EnemyClearObjective : MonoBehaviour
    {
        private readonly EnemyCounter _counter = new();
        private bool _met;
        private bool _started;

        public bool IsMet => _met;
        public event System.Action OnMet;

        private void Start()
        {
            _started = true;
            var enemies = GetComponentsInChildren<EnemyBase>(true);
            foreach (var e in enemies)
            {
                _counter.Register();
                e.OnDied += HandleEnemyDied;
            }
            // 적이 없는 방이면 클리어할 것이 없으므로 즉시 충족(출구 영구 잠금 방지).
            if (enemies.Length == 0) SetMet();
        }

        private void HandleEnemyDied(EnemyBase e)
        {
            _counter.MarkDead();
            if (_counter.AllDead) SetMet();
        }

        private void SetMet()
        {
            if (_met) return;
            _met = true;
            OnMet?.Invoke();
        }

        // RoomPuzzle이 Awake에서 이 목표를 등록할 수 있게, Start 전 접근 시에도 안전한 초기 상태(false)를 보장.
        public bool Started => _started;
    }
}

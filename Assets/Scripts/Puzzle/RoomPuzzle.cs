using System.Collections.Generic;
using UnityEngine;
using Help.Dungeon;

namespace Help.Puzzle
{
    // 방의 목표(CapabilityTarget 해제 등)를 SolveTracker로 집계해, 전부 충족되면 방을 클리어 처리한다.
    // 클리어 플래그 게이팅용 — RoomManager.TryClearCurrentRoom으로 이어져 출구 잠금이 풀린다.
    // 목표 소스는 확장 가능: 이후 적 전멸 등을 SetMet으로 붙이면 됨(프레임워크 불변).
    public class RoomPuzzle : MonoBehaviour
    {
        [SerializeField] private List<CapabilityTarget> _targets = new();
        [SerializeField] private RoomManager _roomManager;

        private readonly SolveTracker _tracker = new();

        public bool IsSolved => _tracker.IsSolved;
        public event System.Action OnSolved;

        private readonly List<Help.Enemy.EnemyClearObjective> _clearObjectives = new();

        private void Awake()
        {
            foreach (var t in _targets)
            {
                if (t == null) continue;
                _tracker.Register(t);
                t.OnResolved += HandleTargetResolved;
                if (t.IsResolved) _tracker.SetMet(t, true);
            }

            // 같은 콘텐츠 트리의 "적 전멸" 목표도 집계(전투 방 클리어 게이팅). CapabilityTarget과 동일 취급.
            foreach (var clear in GetComponentsInChildren<Help.Enemy.EnemyClearObjective>(true))
            {
                _clearObjectives.Add(clear);
                _tracker.Register(clear);
                clear.OnMet += () => _tracker.SetMet(clear, true);
                if (clear.IsMet) _tracker.SetMet(clear, true);
            }

            _tracker.OnSolved += HandleSolved;

            // 목표가 있고 아직 미해결이면 방 출구를 잠근다(클리어 게이팅). 목표 없으면 잠그지 않음(트랩 방지).
            if (!_tracker.IsSolved && _tracker.Count > 0)
            {
                if (_roomManager == null) _roomManager = FindFirstObjectByType<RoomManager>();
                _roomManager?.SetExitLock(true);
            }
        }

        private void OnDestroy()
        {
            foreach (var t in _targets)
                if (t != null) t.OnResolved -= HandleTargetResolved;
        }

        private void HandleTargetResolved(CapabilityTarget t) => _tracker.SetMet(t, true);

        private void HandleSolved()
        {
            OnSolved?.Invoke();
            if (_roomManager == null) _roomManager = FindFirstObjectByType<RoomManager>();
            _roomManager?.TryClearCurrentRoom();
        }
    }
}

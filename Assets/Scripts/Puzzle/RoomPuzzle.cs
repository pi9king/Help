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

        // 진입 조건이 없어도 출구를 잠글 수 있는 방인가.
        // **방 안에서 해결 수단을 제공하는 방에만 켠다** — 예: KEY 튜토리얼(K·Y가 방 안에 있다).
        // 생성된 퍼즐 방은 꺼둔다. 켜면 도구 없이 들어가 갇힌다(RoomGating).
        [SerializeField] private bool _selfContained;

        private readonly SolveTracker _tracker = new();

        public bool IsSolved => _tracker.IsSolved;

        // 재방문 시 출구 잠금을 재평가할 때 RoomManager가 본다.
        public int ObjectiveCount => _tracker.Count;
        public event System.Action OnSolved;

        private readonly List<Help.Enemy.EnemyClearObjective> _clearObjectives = new();

        private void Awake()
        {
            // 마커 기반으로 런타임 스폰된 콘텐츠는 인스펙터에서 목록을 채울 수 없다.
            // 비어 있으면 같은 트리의 장애물을 전부 목표로 삼는다(손배치 프리팹은 기존 목록을 그대로 쓴다).
            if (_targets.Count == 0)
                _targets.AddRange(GetComponentsInChildren<CapabilityTarget>(true));

            // 능력 장애물이 출구를 잠글 자격이 있는가(RoomGating):
            // 진입 조건이 있는 방이거나, 스스로 해결 수단을 주는 방(튜토리얼)이어야 한다.
            // 자격이 없으면 장애물은 그대로 서 있되 게이트가 아니다 —
            // 뒤에 있는 보상을 못 얻을 뿐 방은 통과된다. 안 그러면 도구 없이 들어가 갇힌다.
            if (_roomManager == null) _roomManager = FindFirstObjectByType<RoomManager>();

            if (RoomGating.CapabilityGatesExit(
                    _roomManager != null && _roomManager.CurrentRoomHasEntryConditions, _selfContained))
            {
                foreach (var t in _targets)
                {
                    if (t == null) continue;
                    _tracker.Register(t);
                    t.OnResolved += HandleTargetResolved;
                    if (t.IsResolved) _tracker.SetMet(t, true);
                }
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
            if (RoomGating.ShouldLockExit(_tracker.Count, _tracker.IsSolved))
                _roomManager?.SetExitLock(true);
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

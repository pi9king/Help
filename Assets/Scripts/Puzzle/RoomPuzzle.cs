using System.Collections.Generic;
using UnityEngine;
using Help.Dungeon;

namespace Help.Puzzle
{
    // 방의 목표(능력 장애물 해제 · 적 전멸)를 집계한다. **두 사건을 따로 센다.**
    //
    //   출구 해제(_exitTracker) — 이 방을 나갈 수 있게 되는 시점.
    //                             적 전멸은 항상 포함, 능력 장애물은 잠글 자격이 있을 때만(RoomGating).
    //   완전 클리어(_clearTracker) — 방의 **모든** 목표를 해치운 시점. 퍼즐 클리어 보상이 여기 붙는다.
    //
    // 둘을 갈라놓은 이유(2026-09-10 결정): 보상형 퍼즐 방은 자유 통행이라 장애물이 출구를 잠그면 안 되지만,
    // 그 장애물을 부순 것 자체는 보상을 줄 사건이다. 하나로 세면 둘 중 하나가 반드시 틀린다 —
    // 출구를 안 잠그려고 목표에서 빼면 "깼다"를 영영 모르고, 목표에 넣으면 도구 없이 들어간 방에 갇힌다.
    public class RoomPuzzle : MonoBehaviour
    {
        [SerializeField] private List<CapabilityTarget> _targets = new();
        [SerializeField] private RoomManager _roomManager;

        // 진입 조건이 없어도 출구를 잠글 수 있는 방인가.
        // **방 안에서 해결 수단을 제공하는 방에만 켠다** — 예: KEY 튜토리얼(K·Y가 방 안에 있다).
        // 생성된 퍼즐 방은 꺼둔다. 켜면 도구 없이 들어가 갇힌다(RoomGating).
        [SerializeField] private bool _selfContained;

        private readonly SolveTracker _exitTracker = new();
        private readonly SolveTracker _clearTracker = new();
        private readonly List<Help.Enemy.EnemyClearObjective> _clearObjectives = new();

        // 능력 장애물이 이 방의 출구를 잠글 자격이 있는가(RoomGating). 재방문 재평가에서 RoomManager가 본다.
        public bool CapabilityGatesExit { get; private set; }

        // 출구 게이팅용 — 목표 0개면 잠그지 않는다(영구 잠금 방지).
        public int ExitObjectiveCount => _exitTracker.Count;
        public bool IsExitOpen => _exitTracker.IsSolved;

        // 방을 완전히 깼는가(보상 지급 기준).
        public bool IsFullyCleared => _clearTracker.IsSolved;
        public int ClearObjectiveCount => _clearTracker.Count;

        // 방의 모든 목표를 해치운 순간 1회 발화. 퍼즐 클리어 보상이 구독한다.
        public event System.Action OnFullyCleared;

        private void Awake()
        {
            // 마커 기반으로 런타임 스폰된 콘텐츠는 인스펙터에서 목록을 채울 수 없다.
            // 비어 있으면 같은 트리의 장애물을 전부 목표로 삼는다(손배치 프리팹은 기존 목록을 그대로 쓴다).
            if (_targets.Count == 0)
                _targets.AddRange(GetComponentsInChildren<CapabilityTarget>(true));

            if (_roomManager == null) _roomManager = FindFirstObjectByType<RoomManager>();

            // 능력 장애물이 출구를 잠글 자격이 있는가:
            // 진입 조건이 있는 방이거나, 스스로 해결 수단을 주는 방(튜토리얼)이어야 한다.
            // 자격이 없으면 장애물은 그대로 서 있되 게이트가 아니다 — 부수면 보상만 나온다.
            CapabilityGatesExit = RoomGating.CapabilityGatesExit(
                _roomManager != null && _roomManager.CurrentRoomHasEntryConditions, _selfContained);

            // ── 1단계: 목표를 **전부** 등록한다.
            // 등록과 초기 충족을 섞으면, 먼저 등록된 목표 하나가 이미 충족됐을 때
            // "그 순간의 전부"가 충족된 것으로 판정돼 IsSolved가 조기에 래치된다(뒤 목표는 영영 무시).
            foreach (var t in _targets)
            {
                if (t == null) continue;
                _clearTracker.Register(t);
                if (CapabilityGatesExit) _exitTracker.Register(t);
                t.OnResolved += HandleTargetResolved;
            }

            foreach (var clear in GetComponentsInChildren<Help.Enemy.EnemyClearObjective>(true))
            {
                _clearObjectives.Add(clear);
                _clearTracker.Register(clear);
                _exitTracker.Register(clear); // 적 전멸은 능력이 필요 없어 항상 출구를 잠글 자격이 있다
                var captured = clear;
                clear.OnMet += () => HandleClearObjectiveMet(captured);
            }

            // ── 2단계: 구독을 **초기 충족보다 먼저** 건다.
            // 뒤에 걸면, Awake 시점에 이미 전부 충족된 방(예: 적 0마리)에서 발화를 통째로 놓친다.
            _exitTracker.OnSolved += HandleExitOpened;
            _clearTracker.OnSolved += HandleFullyCleared;

            // ── 3단계: 이미 충족돼 있는 목표를 반영한다.
            foreach (var t in _targets)
                if (t != null && t.IsResolved) SetMet(t);
            foreach (var clear in _clearObjectives)
                if (clear.IsMet) HandleClearObjectiveMet(clear);

            // 목표가 있고 아직 미해결이면 방 출구를 잠근다. 목표 없으면 잠그지 않음(트랩 방지).
            if (RoomGating.ShouldLockExit(_exitTracker.Count, _exitTracker.IsSolved))
                _roomManager?.SetExitLock(true);
        }

        private void OnDestroy()
        {
            foreach (var t in _targets)
                if (t != null) t.OnResolved -= HandleTargetResolved;
        }

        private void HandleTargetResolved(CapabilityTarget t) => SetMet(t);

        private void HandleClearObjectiveMet(Help.Enemy.EnemyClearObjective clear) => SetMet(clear);

        // 하나의 목표를 두 집계에 동시에 반영한다. 출구용에 등록되지 않은 목표는 SolveTracker가 알아서 무시하지 않으므로
        // (Register 안 한 키에 SetMet 하면 새로 생긴다) 등록 여부를 여기서 가른다.
        private void SetMet(object objective)
        {
            _clearTracker.SetMet(objective, true);
            if (objective is CapabilityTarget && !CapabilityGatesExit) return;
            _exitTracker.SetMet(objective, true);
        }

        private void HandleExitOpened()
        {
            if (_roomManager == null) _roomManager = FindFirstObjectByType<RoomManager>();
            _roomManager?.TryClearCurrentRoom();
        }

        private void HandleFullyCleared()
        {
            // 자유 통행 퍼즐 방(출구 목표 0개)은 출구 해제 사건이 없어 여기서만 클리어로 표시된다.
            // 이미 클리어된 방이면 TryClearCurrentRoom이 알아서 무시한다.
            if (_roomManager == null) _roomManager = FindFirstObjectByType<RoomManager>();
            _roomManager?.TryClearCurrentRoom();

            OnFullyCleared?.Invoke();
        }
    }
}

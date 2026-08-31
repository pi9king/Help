using System;
using System.Collections.Generic;

namespace Help.Puzzle
{
    // 방 퍼즐의 여러 목표(장애물 해제/적 전멸 등) 충족을 집계하는 순수 로직.
    // 목표가 1개 이상이고 전부 충족되는 "처음 그 순간" OnSolved를 1회 발화한다(중복 발화 없음).
    // MonoBehaviour(RoomPuzzle)가 각 목표의 이벤트를 SetMet으로 연결한다. EnemyAI처럼 순수·EditMode 테스트 가능.
    public class SolveTracker
    {
        private readonly Dictionary<object, bool> _objectives = new();

        public bool IsSolved { get; private set; }
        public event Action OnSolved;

        public int Count => _objectives.Count;

        public int MetCount
        {
            get
            {
                int n = 0;
                foreach (var v in _objectives.Values) if (v) n++;
                return n;
            }
        }

        public void Register(object key)
        {
            if (key != null && !_objectives.ContainsKey(key)) _objectives[key] = false;
        }

        public void SetMet(object key, bool met)
        {
            if (key == null) return;
            _objectives[key] = met;
            Evaluate();
        }

        private void Evaluate()
        {
            if (IsSolved) return;
            if (_objectives.Count == 0) return; // 목표 없으면 solved 아님(빈 방 오발화 방지)
            foreach (var v in _objectives.Values)
                if (!v) return;
            IsSolved = true;
            OnSolved?.Invoke();
        }

        public void Reset()
        {
            IsSolved = false;
            var keys = new List<object>(_objectives.Keys);
            foreach (var k in keys) _objectives[k] = false;
        }
    }
}

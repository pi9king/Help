using System;
using System.Collections.Generic;
using UnityEngine;
using Help.Item;

namespace Help.Puzzle
{
    // 능력 타깃(장애물). RequiredCapability를 만족하는 능력이 적용되면 Resolve된다.
    // 전투 Hurtbox 미러 — 적용원(Hitbox 공격, 서브무기 사용 등, 출처 무관)이 이 컴포넌트를 찾아 TryApply한다.
    // 물리 장애물 게이팅의 몸체: 해제 시 콜라이더/오브젝트 제거로 지형이 열린다.
    [RequireComponent(typeof(Collider2D))]
    public class CapabilityTarget : MonoBehaviour
    {
        [SerializeField] private Capability _requiredCapability = Capability.BreakWall;
        [Tooltip("해제 시 이 오브젝트를 비활성(지형 장벽 제거). 끄면 콜라이더만 비활성.")]
        [SerializeField] private bool _deactivateOnResolve = true;
        [SerializeField] private GameObject[] _removeOnResolve; // 함께 제거할 장벽 조각(선택)

        public Capability RequiredCapability => _requiredCapability;
        public bool IsResolved { get; private set; }
        public event Action<CapabilityTarget> OnResolved;

        // 능력 집합 적용 시도. 매칭되면 해제하고 true.
        public bool TryApply(IReadOnlyCollection<Capability> applied)
        {
            if (IsResolved) return false;
            if (!CapabilityMatch.Resolves(_requiredCapability, applied)) return false;
            Resolve();
            return true;
        }

        public void Resolve()
        {
            if (IsResolved) return;
            IsResolved = true;
            OnResolved?.Invoke(this);

            if (_removeOnResolve != null)
                foreach (var go in _removeOnResolve)
                    if (go != null) go.SetActive(false);

            if (_deactivateOnResolve)
            {
                gameObject.SetActive(false);
            }
            else
            {
                var col = GetComponent<Collider2D>();
                if (col != null) col.enabled = false;
            }
        }
    }
}

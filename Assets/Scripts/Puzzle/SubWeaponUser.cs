using UnityEngine;
using Help.Combat;
using Help.Player;

namespace Help.Puzzle
{
    // 서브무기 "사용(use)" 액션의 능력 적용원. UsePerformed 시 플레이어 앞쪽을 훑어
    // CapabilityTarget에 장착 서브무기의 능력을 적용한다.
    // 출처 무관(C 모델)의 두 번째 적용원 — Hitbox(주무기 공격)의 use 버전. CapabilityTarget/판정은 불변.
    public class SubWeaponUser : MonoBehaviour
    {
        [SerializeField] private float _reach = 1.2f;                 // 앞쪽 적용 중심 거리
        [SerializeField] private Vector2 _size = new Vector2(1.4f, 1.4f);

        private PlayerController _owner;

        private void Awake() => _owner = GetComponentInParent<PlayerController>();

        private void OnEnable() { if (_owner != null) _owner.UsePerformed += HandleUse; }
        private void OnDisable() { if (_owner != null) _owner.UsePerformed -= HandleUse; }

        private void HandleUse()
        {
            if (_owner == null) return;
            var caps = _owner.EquippedSubCapabilities;
            if (caps == null || caps.Count == 0) return; // 장착 서브무기 없으면 무효

            Vector2 center = (Vector2)_owner.transform.position + new Vector2(_owner.FacingDir * _reach, 0f);
            var hits = Physics2D.OverlapBoxAll(center, _size, 0f);
            var effect = SubWeaponEffectResolver.Resolve(caps);

            foreach (var h in hits)
            {
                if (h.transform.IsChildOf(_owner.transform)) continue; // 자기 자신은 제외(마스크 없는 전방위 스캔이라 필요)

                // 퍼즐 장애물: 능력 적용
                var target = h.GetComponent<CapabilityTarget>();
                if (target != null) target.TryApply(caps);

                // 적: 능력이 전투 효과로 변환돼 상태이상으로 적용(출처 무관 — 사용 액션도 전투에 관여)
                if (!effect.HasEffect) continue;
                var hurtbox = h.GetComponent<Hurtbox>();
                if (hurtbox != null && hurtbox.OwnerStatus != null)
                {
                    hurtbox.OwnerStatus.Apply(effect.Status, effect.Duration);
                    if (effect.PullDuration > 0f) hurtbox.OwnerStatus.ApplyPull(effect.PullDuration);
                }
            }
        }
    }
}

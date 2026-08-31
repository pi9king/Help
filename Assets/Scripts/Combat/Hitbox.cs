using UnityEngine;
using Help.Player;

namespace Help.Combat
{
    // 플레이어 공격 판정 콜라이더. 활성 창/배치/크기는 PlayerAttack(공격 모션)이 구동한다.
    // (예전엔 AttackPerformed를 직접 구독해 고정 시간 켜졌으나, 무기별 사거리/타이밍을 위해 모션이 소유)
    [RequireComponent(typeof(Collider2D))]
    public class Hitbox : MonoBehaviour
    {
        private PlayerController _owner;
        private bool _active;

        private void Awake()
        {
            _owner = GetComponentInParent<PlayerController>();
            GetComponent<Collider2D>().isTrigger = true;
            SetActive(false);
        }

        // 모션의 사거리(전방 offset)와 범위(크기)로 히트박스를 배치. 좌우 방향은 부모 flip이 처리.
        public void Configure(float reachX, Vector2 size)
        {
            var p = transform.localPosition;
            transform.localPosition = new Vector3(reachX, p.y, p.z);
            if (GetComponent<Collider2D>() is BoxCollider2D box) box.size = size;
        }

        public void SetActive(bool active)
        {
            _active = active;
            GetComponent<Collider2D>().enabled = active;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_active || _owner == null) return;

            // 적: 데미지(속성 열쇠-자물쇠) + 피격 연출(플래시/넉백/히트스톱/화면 흔들림)
            var hurtbox = other.GetComponent<Hurtbox>();
            if (hurtbox != null)
            {
                int damage = DamageCalculator.Calculate(
                    _owner.AttackDamage,
                    _owner.EquippedElement,
                    hurtbox.OwnerStats);

                hurtbox.OwnerStats.TakeDamage(damage);

                var enemy = other.GetComponentInParent<Help.Enemy.EnemyBase>();
                if (enemy != null) enemy.OnHitReceived(_owner.transform.position);

                HitStop.Do(0.05f);
                CameraShake.ShakeMain(0.12f, 0.12f);
            }

            // 퍼즐 장애물: 장착 무기의 능력을 적용(출처 무관 — 같은 공격이 능력도 전달)
            var target = other.GetComponent<Help.Puzzle.CapabilityTarget>();
            if (target != null)
                target.TryApply(_owner.EquippedCapabilities);
        }
    }
}

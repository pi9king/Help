using UnityEngine;
using Help.Player;

namespace Help.Combat
{
    // 플레이어 공격의 "재생" 담당: AttackPerformed를 받아 현재 무기의 공격 모션을 실행한다.
    // 근접(MeleeArc)이면 슬래시 VFX를 스윕하고 히트박스를 모션의 사거리/범위/타이밍으로 구동한다.
    // 원거리/마법(Projectile)은 추후 이 드라이버의 분기에 추가된다(타이밍 엔진은 공용).
    // 모션은 데이터(AttackMotionDef)로 서술되므로 무기별 분리 = 데이터 교체.
    public class PlayerAttack : MonoBehaviour
    {
        private PlayerController _pc;
        private Hitbox _hitbox;
        private SlashVFX _slash;

        private AttackMotionClock _clock;
        private AttackMotionDef _current;

        private void Awake()
        {
            _pc = GetComponent<PlayerController>();
            _hitbox = GetComponentInChildren<Hitbox>(true);

            _slash = GetComponentInChildren<SlashVFX>(true);
            if (_slash == null)
            {
                var go = new GameObject("SlashVFX");
                go.transform.SetParent(transform, false);
                _slash = go.AddComponent<SlashVFX>();
            }
        }

        private void OnEnable()
        {
            if (_pc != null) _pc.AttackPerformed += OnAttackPerformed;
        }

        private void OnDisable()
        {
            if (_pc != null) _pc.AttackPerformed -= OnAttackPerformed;
        }

        private void OnAttackPerformed()
        {
            _current = SelectMotion();
            _clock = new AttackMotionClock(_current.Windup, _current.Active, _current.Recovery);
            _clock.Start();

            switch (_current.Kind)
            {
                case AttackKind.MeleeArc:
                    _slash?.Play(_current.Reach, _current.ArcStartDeg, _current.ArcEndDeg,
                                 _current.SlashColor, _current.SlashScale,
                                 _current.Active + _current.Recovery);
                    break;

                case AttackKind.Projectile:
                    // TODO 원거리/마법: _current.ProjectilePrefab을 스폰해 앞으로 발사.
                    // 플레이어의 AttackDamage/EquippedElement/EquippedCapabilities를 실어 보내면
                    // DamageCalculator/CapabilityTarget이 근접과 동일하게 판정한다(출처 무관).
                    break;
            }
        }

        // 추후: WeaponCategory→AttackMotionDef 라이브러리 조회. 지금은 기본 근접 모션.
        private AttackMotionDef SelectMotion() => AttackMotionDef.Default();

        private void Update()
        {
            if (_clock == null) return;
            var r = _clock.Tick(Time.deltaTime);

            if (_current.Kind == AttackKind.MeleeArc && _hitbox != null)
            {
                if (r.IsActive)
                {
                    _hitbox.Configure(_current.Reach, _current.HitboxSize);
                    _hitbox.SetActive(true);
                }
                else
                {
                    _hitbox.SetActive(false);
                }
            }

            if (r.Phase == AttackPhase.Done)
            {
                _hitbox?.SetActive(false);
                _clock = null;
            }
        }
    }
}

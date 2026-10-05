using UnityEngine;
using Help.Combat;

namespace Help.Player
{
    // E 캐릭터 4방향 시트를 PlayerController의 조준/이동에 연결한다.
    // Animator 상태 이름은 "{Action}_{Facing}" 규약 (E_Character.controller).
    //
    // 공격 중에는 두 가지를 더 한다.
    //  1. 몸통을 조준 방향으로 밀었다 되돌린다(AttackLunge) — "제자리에서 팔만 흔드는" 느낌 제거.
    //     위아래는 줄인다 — 쿼터뷰에서 세로 이동은 발이 미끄러지며 다리가 늘어나 보인다.
    //  2. 시트 프레임을 무기의 예비·타격·회수 단계에 맞춰 직접 고른다(AttackFrameTiming) —
    //     바늘과 그물의 리듬이 달라지고, 판정이 나는 순간에 타격 그림이 나온다.
    //
    // 둘 다 이 시각 자식만 움직인다. 루트를 움직이면 자식 히트박스 좌표와 물리가 흔들린다.
    [RequireComponent(typeof(Animator))]
    public class ECharacterAnimator : MonoBehaviour
    {
        [SerializeField] private PlayerController _player;
        [SerializeField] private float _lungePull = 0.06f;          // 와인드업에 뒤로 당기는 거리(월드 유닛)
        [SerializeField] private float _lungeReach = 0.25f;         // 임팩트에 앞으로 나가는 거리
        [SerializeField] private float _lungeVerticalScale = 0.35f; // 위아래 공격의 이동 비율

        private Animator _animator;
        private Vector3 _restPosition;
        private string _current;
        private string _lastAction;
        private Facing _lastFacing = (Facing)(-1);

        private bool _attacking;
        private float _attackElapsed;
        private float _attackTotal;
        private float _attackWindup;
        private float _attackActive;
        private float _attackRecovery;
        private Vector2 _attackAim;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (_player == null) _player = GetComponentInParent<PlayerController>();
            _restPosition = transform.localPosition;
        }

        private void OnEnable()
        {
            if (_player != null) _player.AttackPerformed += OnAttack;
        }

        private void OnDisable()
        {
            if (_player != null) _player.AttackPerformed -= OnAttack;
            ResetPose();
        }

        private void OnAttack()
        {
            AttackMotionDef motion = _player != null ? _player.EquippedAttackMotion : null;
            if (motion == null) return;

            _attacking = true;
            _attackElapsed = 0f;
            _attackWindup = motion.Windup;
            _attackActive = motion.Active;
            _attackRecovery = motion.Recovery;
            _attackTotal = motion.TotalDuration;
            _attackAim = AimGeometry.DirectionOrDefault(_player.AimDirection);

            // 프레임은 Update에서 단계 타이밍으로 직접 고른다.
            _animator.speed = 0f;
        }

        private void Update()
        {
            if (_player == null) return;

            if (_attacking)
            {
                _attackElapsed += Time.deltaTime;
                if (_attackElapsed >= _attackTotal) ResetPose();
                else ApplyLunge();
            }

            Facing facing = ECharacterFacing.From(_attacking ? _attackAim : _player.AimDirection);
            string action = _attacking ? "Attack"
                          : _player.MoveDirection.sqrMagnitude > 0.0001f ? "Walk"
                          : "Idle";

            // 상태가 그대로면 문자열을 새로 만들지 않는다 — 매 프레임 할당을 피한다.
            if (!ReferenceEquals(action, _lastAction) || facing != _lastFacing)
            {
                _lastAction = action;
                _lastFacing = facing;

                string next = ECharacterFacing.StateName(action, facing);
                if (next != _current)
                {
                    _current = next;
                    _animator.Play(next, 0, 0f);
                }
            }

            if (_attacking) ShowAttackFrame();
        }

        private void ShowAttackFrame()
        {
            int frame = AttackFrameTiming.FrameAt(_attackElapsed, _attackWindup, _attackActive, _attackRecovery);
            _animator.Play(_current, 0, AttackFrameTiming.ClipTime(frame));
        }

        private void ApplyLunge()
        {
            float offset = AttackLunge.Offset(_attackElapsed, _attackWindup, _attackActive,
                                              _attackRecovery, _lungePull, _lungeReach);
            transform.localPosition = _restPosition
                + (Vector3)AttackLunge.Displacement(_attackAim, offset, _lungeVerticalScale);
        }

        private void ResetPose()
        {
            _attacking = false;
            transform.localPosition = _restPosition;
            if (_animator != null) _animator.speed = 1f;
        }
    }
}

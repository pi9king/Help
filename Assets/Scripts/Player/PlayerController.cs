using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Help.Combat;
using Help.Core;
using Help.Item;

namespace Help.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private Transform _groundCheck;
        [SerializeField] private float _groundCheckRadius = 0.1f;
        [SerializeField] private float _dashDuration = DefaultDashDuration;
        [SerializeField] private float _dashCooldown = 0.8f;
        [SerializeField] private float _attackDuration = 0.3f;
        [SerializeField] private float _invulnDuration = 0.6f;   // 피격 후 무적시간(i-frame)
        [SerializeField] private float _knockbackForce = 6f;     // 피격 시 밀려나는 수평 힘
        [SerializeField] private float _knockbackUp = 3f;        // 피격 시 살짝 뜨는 수직 힘
        [SerializeField] private float _knockbackStun = 0.18f;   // 넉백 속도를 유지할 시간(이동 입력 무시)

        // --- 점프 손맛 (레벨 디자인 문법의 근거 — PlatformerMetrics와 짝을 이룬다) ---
        [SerializeField] private float _fallMultiplier = PlatformerMetrics.DefaultFallMultiplier; // 낙하 가속 배수
        [SerializeField] private float _shortHopFactor = 0.4f;   // 점프 버튼을 일찍 떼면 상승 속도를 이만큼으로 자른다
        [SerializeField] private float _coyoteTime = 0.10f;      // 발판에서 떨어진 직후에도 점프 허용
        [SerializeField] private float _jumpBufferTime = 0.12f;  // 착지 직전 입력한 점프를 착지 순간 발화

        public const float DefaultDashDuration = 0.15f;

        private Rigidbody2D _rb;
        private PlayerStats _stats;
        private PlayerState _state;
        private Help.Combat.HitFlash _flash;

        private Vector2 _moveInput;
        private bool _isGrounded;
        private bool _airDashUsed;   // D-3: 공중 대시는 착지할 때까지 1회
        private float _dashTimer;
        private float _dashCooldownTimer;
        private float _attackTimer;
        private float _invulnTimer;
        private float _knockbackTimer;
        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private bool _jumpActive;             // 이번 상승이 "점프"인가(넉백 상승은 자르지 않기 위해)
        private InputAction _jumpAction;      // 가변 점프용 — Button 액션은 release 메시지가 오지 않아 직접 폴링한다
        private int _facingDir = 1;

        // 현재 장착 무기 속성 (크래프팅 시스템과 연동)
        public ElementType EquippedElement { get; set; } = ElementType.None;
        // 현재 장착 무기 종류 (공격 모션 선택에 사용 — PlayerAttack)
        public WeaponCategory EquippedWeaponCategory { get; private set; } = WeaponCategory.None;

        // 장착 무기의 공격 속도 배수(ItemDefinition.AttackSpeedMult). 공격 간격과 모션을 같이 줄인다.
        public float AttackSpeedMult { get; private set; } = 1f;
        // 현재 장착 무기가 제공하는 능력(공격 시 Hitbox가 능력 타깃/장애물에 적용)
        private readonly List<Capability> _equippedCapabilities = new();
        public IReadOnlyList<Capability> EquippedCapabilities => _equippedCapabilities;
        // 현재 장착 서브무기가 제공하는 능력("사용(use)" 시 SubWeaponUser가 능력 타깃에 적용)
        private readonly List<Capability> _equippedSubCapabilities = new();
        public IReadOnlyList<Capability> EquippedSubCapabilities => _equippedSubCapabilities;
        public int AttackDamage => _stats?.AttackPower ?? 15;
        public int FacingDir => _facingDir;

        public event System.Action AttackPerformed;
        public event System.Action UsePerformed; // 서브무기 사용 — SubWeaponUser가 구독
        // 인벤토리/크래프팅 토글 입력 (UI가 구독) — PlayerController가 UI를 직접 참조하지 않기 위한 이벤트
        public event System.Action InventoryToggleRequested;
        public event System.Action CraftingToggleRequested;
        public event System.Action InteractRequested; // 방 간 이동(문) 등에 사용

        // UI 패널(인벤토리/크래프팅)이 열려 있는 동안 게임플레이 입력을 막는다.
        // (열린 패널 위에서 좌클릭이 공격으로, WASD가 이동으로 새는 것을 방지 — UI는 비모달이지만 입력은 게이트)
        private readonly HashSet<object> _openUiPanels = new();
        private bool UiBlocking => _openUiPanels.Count > 0;
        public void SetUiPanelOpen(object panel, bool open)
        {
            if (open) _openUiPanels.Add(panel);
            else _openUiPanels.Remove(panel);
        }

        private void Awake()
        {
            // D-1: 벽에 걸려 미끄러지지 않도록 마찰 0 (Help.Core.PhysicsMaterials)
            Help.Core.PhysicsMaterials.ApplyFrictionless(gameObject);
            _rb = GetComponent<Rigidbody2D>();
            // 중력은 코드가 소유한다 — 씬/프리팹 값이 PlatformerMetrics와 어긋나면
            // 도달성 검증이 통과한 방을 실제로는 못 도는 사태가 난다.
            _rb.gravityScale = PlatformerMetrics.DefaultGravityScale;

            // Button 액션은 SendMessages로 canceled가 오지 않는다(Value 액션만).
            // 버튼을 떼는 순간을 알려면 액션을 직접 들고 폴링해야 한다.
            var input = GetComponent<PlayerInput>();
            if (input != null && input.actions != null) _jumpAction = input.actions.FindAction("Jump");

            _stats = new PlayerStats();
            _stats.OnDied += HandleDeath;
            _flash = GetComponentInChildren<Help.Combat.HitFlash>();
            if (_flash == null) _flash = gameObject.AddComponent<Help.Combat.HitFlash>();

            // 공격 모션 드라이버 자동 부착(씬 배선 불필요)
            if (GetComponent<Help.Combat.PlayerAttack>() == null)
                gameObject.AddComponent<Help.Combat.PlayerAttack>();
        }

        private void Start()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.Inventory.OnItemEquipped += HandleItemEquipped;
            GameManager.Instance.Inventory.OnItemUnequipped += HandleItemUnequipped;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.Inventory.OnItemEquipped -= HandleItemEquipped;
            GameManager.Instance.Inventory.OnItemUnequipped -= HandleItemUnequipped;
        }

        private void HandleItemEquipped(ItemDefinition item)
        {
            _stats.ApplyEquipmentBonus(item.AttackBonus, item.DefenseBonus);
            // 무기 장착 시 속성 열쇠 + 능력 반영 (Hitbox → DamageCalculator / CapabilityTarget에서 사용)
            if (item.Type == ItemType.Weapon)
            {
                EquippedElement = item.Element;
                EquippedWeaponCategory = item.WeaponCategory;
                AttackSpeedMult = item.AttackSpeedMult;
                _equippedCapabilities.Clear();
                if (item.Capabilities != null) _equippedCapabilities.AddRange(item.Capabilities);
            }
            else if (item.Type == ItemType.SubWeapon)
            {
                _equippedSubCapabilities.Clear();
                if (item.Capabilities != null) _equippedSubCapabilities.AddRange(item.Capabilities);
            }
        }

        private void HandleItemUnequipped(ItemDefinition item)
        {
            _stats.RemoveEquipmentBonus(item.AttackBonus, item.DefenseBonus);
            if (item.Type == ItemType.Weapon)
            {
                EquippedElement = ElementType.None;
                EquippedWeaponCategory = WeaponCategory.None;
                AttackSpeedMult = 1f;
                _equippedCapabilities.Clear();
            }
            else if (item.Type == ItemType.SubWeapon)
            {
                _equippedSubCapabilities.Clear();
            }
        }

        private void Update()
        {
            _isGrounded = Physics2D.OverlapCircle(_groundCheck.position, _groundCheckRadius, _groundLayer);
            UpdateTimers();
            UpdateFacing();
        }

        private void FixedUpdate()
        {
            TryConsumeBufferedJump();
            ApplyMotion();
            ApplyJumpShaping();
        }

        // 점프 버튼이 눌려 있는가. 배선이 없으면(테스트/에디터) "누르고 있다"로 봐서 상승을 자르지 않는다.
        private bool JumpHeld => _jumpAction == null || _jumpAction.IsPressed();

        // 버퍼에 남은 점프 입력을 코요테 시간 안이면 발화시킨다.
        private void TryConsumeBufferedJump()
        {
            if (_jumpBufferTimer <= 0f || _coyoteTimer <= 0f || UiBlocking) return;

            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;   // 같은 점프를 두 번 쓰지 못하게(공중 이중점프 방지)
            _jumpActive = true;
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _stats.JumpForce);
            _state = PlayerState.Jumping;
        }

        // 상승↔낙하 비대칭이 플랫포머 손맛의 핵심 — 내려올 때 더 빨리 떨어뜨리고,
        // 버튼을 일찍 뗀 점프는 상승을 잘라 낮은 점프가 되게 한다.
        private void ApplyJumpShaping()
        {
            float vy = _rb.linearVelocity.y;

            if (vy < 0f)
            {
                _jumpActive = false;
                float extra = Physics2D.gravity.y * _rb.gravityScale * (_fallMultiplier - 1f) * Time.fixedDeltaTime;
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, vy + extra);
            }
            else if (vy > 0f && _jumpActive && !JumpHeld)
            {
                _jumpActive = false;
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, vy * _shortHopFactor);
            }
        }

        // 상태별 이동 적용(enum + switch 컨벤션). 상태가 수평 속도의 소유권을 갖는 지점 —
        // 새 이동 상태(스윙 등)는 여기에 case를 더한다.
        private void ApplyMotion()
        {
            if (_knockbackTimer > 0f) return; // 넉백 속도 유지 — 이동 입력/상태보다 우선

            switch (_state)
            {
                case PlayerState.Dashing:
                    return;             // 대시 속도 유지 — 입력/UI 게이트보다 우선
                case PlayerState.Attacking:
                    HaltHorizontal();   // 공격 중 제자리
                    return;
            }

            if (UiBlocking) { HaltHorizontal(); return; } // 패널 오픈 중 이동 정지

            _rb.linearVelocity = new Vector2(_moveInput.x * _stats.MoveSpeed, _rb.linearVelocity.y);
            UpdateMovementState();
        }

        // 수직 속도(중력/점프)는 보존하고 수평만 정지
        private void HaltHorizontal() => _rb.linearVelocity = new Vector2(0, _rb.linearVelocity.y);

        // Input System 콜백
        public void OnMove(InputValue value) => _moveInput = value.Get<Vector2>();

        // 입력은 버퍼에만 기록하고 실제 발화는 FixedUpdate에서 — 착지 직전에 누른 점프도 살린다.
        public void OnJump(InputValue value)
        {
            if (!value.isPressed || UiBlocking) return;
            _jumpBufferTimer = _jumpBufferTime;
        }

        public void OnDash(InputValue value)
        {
            if (!value.isPressed || UiBlocking) return;
            if (!DashRule.CanDash(_isGrounded, _airDashUsed, _dashCooldownTimer)) return;
            StartDash();
        }

        public void OnAttack(InputValue value)
        {
            if (!value.isPressed || _state == PlayerState.Attacking || UiBlocking) return;
            _state = PlayerState.Attacking;
            _attackTimer = Help.Combat.AttackSpeed.Scale(_attackDuration, AttackSpeedMult);
            AttackPerformed?.Invoke();
        }

        // 서브무기 사용 — 능력을 앞쪽 능력 타깃에 적용(SubWeaponUser가 처리). 전투/이동/퍼즐 공통 액션.
        public void OnUse(InputValue value)
        {
            if (!value.isPressed || UiBlocking) return;
            UsePerformed?.Invoke();
        }

        public void OnInventory(InputValue value)
        {
            if (value.isPressed) InventoryToggleRequested?.Invoke();
        }

        public void OnCrafting(InputValue value)
        {
            if (value.isPressed) CraftingToggleRequested?.Invoke();
        }

        public void OnInteract(InputValue value)
        {
            if (value.isPressed && !UiBlocking) InteractRequested?.Invoke();
        }

        public void TakeDamage(int damage) => TakeDamage(damage, transform.position);

        // 피해원 위치를 받아 무적시간·넉백·피격 연출까지 처리한다.
        public void TakeDamage(int damage, Vector2 sourcePosition)
        {
            if (_stats == null || _invulnTimer > 0f) return; // 무적 중이면 무시(연속 피해 방지)

            _stats.TakeDamage(damage);
            if (_stats.CurrentHp <= 0) return; // 사망은 HandleDeath에서 처리

            _invulnTimer = _invulnDuration;

            // 피해원 반대 방향으로 넉백(정면충돌 시 바라보던 반대로)
            float dir = Mathf.Sign(transform.position.x - sourcePosition.x);
            if (dir == 0f) dir = -_facingDir;
            _rb.linearVelocity = new Vector2(dir * _knockbackForce, _knockbackUp);
            _knockbackTimer = _knockbackStun;

            _flash?.Flash();
            Help.Combat.HitStop.Do(0.08f);
            Help.Combat.CameraShake.ShakeMain(0.25f, 0.2f);
        }

        public PlayerStats Stats => _stats;

        private void StartDash()
        {
            if (!_isGrounded) _airDashUsed = true;
            _state = PlayerState.Dashing;
            _dashTimer = _dashDuration;
            _dashCooldownTimer = _dashCooldown;
            _rb.linearVelocity = new Vector2(_facingDir * _stats.DashForce, 0);
        }

        private void UpdateTimers()
        {
            if (_dashTimer > 0)
            {
                _dashTimer -= Time.deltaTime;
                if (_dashTimer <= 0 && _state == PlayerState.Dashing)
                    _state = PlayerState.Idle;
            }
            if (_dashCooldownTimer > 0) _dashCooldownTimer -= Time.deltaTime;
            if (_attackTimer > 0)
            {
                _attackTimer -= Time.deltaTime;
                if (_attackTimer <= 0 && _state == PlayerState.Attacking)
                    _state = PlayerState.Idle;
            }
            if (_invulnTimer > 0) _invulnTimer -= Time.deltaTime;
            if (_knockbackTimer > 0) _knockbackTimer -= Time.deltaTime;

            // 접지 중엔 코요테 시간을 계속 채우고, 떨어지면 소진시킨다.
            if (_isGrounded) { _coyoteTimer = _coyoteTime; _airDashUsed = false; } // 착지 = 공중 대시 충전
            else if (_coyoteTimer > 0) _coyoteTimer -= Time.deltaTime;

            if (_jumpBufferTimer > 0) _jumpBufferTimer -= Time.deltaTime;
        }

        private void UpdateFacing()
        {
            if (_moveInput.x > 0) _facingDir = 1;
            else if (_moveInput.x < 0) _facingDir = -1;
            transform.localScale = new Vector3(_facingDir, 1, 1);
        }

        private void UpdateMovementState()
        {
            if (_state == PlayerState.Attacking || _state == PlayerState.Dashing) return;

            if (!_isGrounded)
                _state = _rb.linearVelocity.y > 0 ? PlayerState.Jumping : PlayerState.Falling;
            else if (Mathf.Abs(_moveInput.x) > 0.01f)
                _state = PlayerState.Running;
            else
                _state = PlayerState.Idle;
        }

        private void HandleDeath()
        {
            _state = PlayerState.Dead;
            _invulnTimer = 0f;
            _knockbackTimer = 0f;
            // 런 리셋: 인벤토리/던전 초기화 + 방 재로드·재배치(RoomManager가 OnRunReset 구독) 후 부활
            GameManager.Instance?.RestartRun();
            _stats.Reset();
            _state = PlayerState.Idle;
        }
    }
}

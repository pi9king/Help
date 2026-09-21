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
        [SerializeField] private float _dashDuration = DefaultDashDuration;
        [SerializeField] private float _dashCooldown = 0.8f;
        [SerializeField] private float _attackDuration = 0.3f;
        [SerializeField] private float _invulnDuration = 0.6f;
        [SerializeField] private float _knockbackForce = 6f;
        [SerializeField] private float _knockbackStun = 0.18f;
        [SerializeField] private float _stickAimDeadzone = 0.2f;

        public const float DefaultDashDuration = 0.15f;

        private Rigidbody2D _rb;
        private PlayerStats _stats;
        private PlayerState _state;
        private HitFlash _flash;
        private SpriteRenderer _bodyRenderer;
        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private Vector2 _aimDirection = Vector2.down;
        private Vector2 _dashDirection = Vector2.down;
        private bool _pointerAim;
        private float _dashTimer;
        private float _dashCooldownTimer;
        private float _attackTimer;
        private float _invulnTimer;
        private float _knockbackTimer;

        public ElementType EquippedElement { get; set; } = ElementType.None;
        public WeaponCategory EquippedWeaponCategory { get; private set; } = WeaponCategory.None;

        private readonly List<Capability> _equippedCapabilities = new();
        public IReadOnlyList<Capability> EquippedCapabilities => _equippedCapabilities;

        private readonly List<Capability> _equippedSubCapabilities = new();
        public IReadOnlyList<Capability> EquippedSubCapabilities => _equippedSubCapabilities;

        public int AttackDamage => _stats?.AttackPower ?? 15;
        public Vector2 MoveDirection => _moveInput.sqrMagnitude > 1f ? _moveInput.normalized : _moveInput;
        public Vector2 AimDirection => _aimDirection;

        public event System.Action AttackPerformed;
        public event System.Action UsePerformed;
        public event System.Action InventoryToggleRequested;
        public event System.Action CraftingToggleRequested;
        public event System.Action InteractRequested;

        private readonly HashSet<object> _openUiPanels = new();
        private bool UiBlocking => _openUiPanels.Count > 0;

        public void SetUiPanelOpen(object panel, bool open)
        {
            if (open) _openUiPanels.Add(panel);
            else _openUiPanels.Remove(panel);
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;

            _stats = new PlayerStats();
            _stats.OnDied += HandleDeath;
            _flash = GetComponentInChildren<HitFlash>();
            if (_flash == null) _flash = gameObject.AddComponent<HitFlash>();
            _bodyRenderer = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();

            if (GetComponent<PlayerAttack>() == null)
                gameObject.AddComponent<PlayerAttack>();
        }

        private void Start()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.Inventory.OnItemEquipped += HandleItemEquipped;
            GameManager.Instance.Inventory.OnItemUnequipped += HandleItemUnequipped;
        }

        private void OnDestroy()
        {
            if (_stats != null) _stats.OnDied -= HandleDeath;
            if (GameManager.Instance == null) return;
            GameManager.Instance.Inventory.OnItemEquipped -= HandleItemEquipped;
            GameManager.Instance.Inventory.OnItemUnequipped -= HandleItemUnequipped;
        }

        private void HandleItemEquipped(ItemDefinition item)
        {
            _stats.ApplyEquipmentBonus(item.AttackBonus, item.DefenseBonus);
            if (item.Type == ItemType.Weapon)
            {
                EquippedElement = item.Element;
                EquippedWeaponCategory = item.WeaponCategory;
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
                _equippedCapabilities.Clear();
            }
            else if (item.Type == ItemType.SubWeapon)
            {
                _equippedSubCapabilities.Clear();
            }
        }

        private void Update()
        {
            UpdateTimers();
            UpdateAimDirection();
            UpdateVisualFacing();
        }

        private void FixedUpdate() => ApplyMotion();

        private void ApplyMotion()
        {
            if (_knockbackTimer > 0f) return;

            switch (_state)
            {
                case PlayerState.Dashing:
                    _rb.linearVelocity = _dashDirection * _stats.DashForce;
                    return;
                case PlayerState.Attacking:
                case PlayerState.Hurt:
                case PlayerState.Dead:
                    HaltMotion();
                    return;
            }

            if (UiBlocking)
            {
                HaltMotion();
                return;
            }

            _rb.linearVelocity = TopDownMovement.Velocity(_moveInput, _stats.MoveSpeed);
            _state = _rb.linearVelocity.sqrMagnitude > 0.0001f
                ? PlayerState.Running
                : PlayerState.Idle;
        }

        private void HaltMotion() => _rb.linearVelocity = Vector2.zero;

        public void OnMove(InputValue value) => _moveInput = value.Get<Vector2>();

        public void OnLook(InputValue value)
        {
            _lookInput = value.Get<Vector2>();
            _pointerAim = _lookInput.sqrMagnitude > 4f;
        }

        public void OnDash(InputValue value)
        {
            if (!value.isPressed || UiBlocking || _dashCooldownTimer > 0f) return;
            if (_state == PlayerState.Dashing || _state == PlayerState.Attacking ||
                _state == PlayerState.Hurt || _state == PlayerState.Dead) return;
            StartDash();
        }

        public void OnAttack(InputValue value)
        {
            if (!value.isPressed || UiBlocking || _state == PlayerState.Dashing ||
                _state == PlayerState.Attacking || _state == PlayerState.Hurt ||
                _state == PlayerState.Dead) return;
            _state = PlayerState.Attacking;
            _attackTimer = _attackDuration;
            AttackPerformed?.Invoke();
        }

        public void OnUse(InputValue value)
        {
            if (value.isPressed && !UiBlocking && _state != PlayerState.Dead)
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

        public void TakeDamage(int damage, Vector2 sourcePosition)
        {
            if (_stats == null || _invulnTimer > 0f) return;

            _stats.TakeDamage(damage);
            if (_stats.CurrentHp <= 0) return;

            _invulnTimer = _invulnDuration;
            Vector2 away = (Vector2)transform.position - sourcePosition;
            if (away.sqrMagnitude <= 0.0001f) away = -_aimDirection;
            _rb.linearVelocity = away.normalized * _knockbackForce;
            _knockbackTimer = _knockbackStun;
            _state = PlayerState.Hurt;

            _flash?.Flash();
            HitStop.Do(0.08f);
            CameraShake.ShakeMain(0.25f, 0.2f);
        }

        public PlayerStats Stats => _stats;

        private void StartDash()
        {
            _dashDirection = TopDownMovement.DashDirection(_moveInput, _aimDirection);
            _aimDirection = _dashDirection;
            _state = PlayerState.Dashing;
            _dashTimer = _dashDuration;
            _dashCooldownTimer = _dashCooldown;
            _rb.linearVelocity = _dashDirection * _stats.DashForce;
        }

        private void UpdateTimers()
        {
            if (_dashTimer > 0f)
            {
                _dashTimer -= Time.deltaTime;
                if (_dashTimer <= 0f && _state == PlayerState.Dashing) _state = PlayerState.Idle;
            }
            if (_dashCooldownTimer > 0f) _dashCooldownTimer -= Time.deltaTime;

            if (_attackTimer > 0f)
            {
                _attackTimer -= Time.deltaTime;
                if (_attackTimer <= 0f && _state == PlayerState.Attacking) _state = PlayerState.Idle;
            }

            if (_invulnTimer > 0f) _invulnTimer -= Time.deltaTime;
            if (_knockbackTimer > 0f)
            {
                _knockbackTimer -= Time.deltaTime;
                if (_knockbackTimer <= 0f && _state == PlayerState.Hurt) _state = PlayerState.Idle;
            }
        }

        private void UpdateAimDirection()
        {
            Vector2 candidate = Vector2.zero;
            if (_pointerAim)
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    Vector3 world = cam.ScreenToWorldPoint(new Vector3(_lookInput.x, _lookInput.y, -cam.transform.position.z));
                    candidate = (Vector2)(world - transform.position);
                }
            }
            else if (_lookInput.sqrMagnitude >= _stickAimDeadzone * _stickAimDeadzone)
            {
                candidate = _lookInput;
            }
            else if (_moveInput.sqrMagnitude > 0.0001f)
            {
                candidate = _moveInput;
            }

            if (candidate.sqrMagnitude > 0.0001f) _aimDirection = candidate.normalized;
        }

        private void UpdateVisualFacing()
        {
            if (_bodyRenderer == null || Mathf.Abs(_aimDirection.x) <= 0.001f) return;
            // 루트 Transform을 뒤집으면 자식 Hitbox의 2D 조준 방향까지 반전된다.
            // 시각만 flip하고 물리/공격 좌표계는 월드 방향을 유지한다.
            _bodyRenderer.flipX = _aimDirection.x < 0f;
        }

        private void HandleDeath()
        {
            _state = PlayerState.Dead;
            _rb.linearVelocity = Vector2.zero;
            _invulnTimer = 0f;
            _knockbackTimer = 0f;
            GameManager.Instance?.RestartRun();
            _stats.Reset();
            _state = PlayerState.Idle;
        }
    }
}

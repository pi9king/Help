using System.Collections.Generic;
using UnityEngine;
using Help.Combat;
using Help.Core;
using Help.Item;
using Help.Player;

namespace Help.Enemy
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyBase : MonoBehaviour
    {
        [SerializeField] private int _maxHp = 30;
        [SerializeField] private int _attackPower = 5;
        [SerializeField] private ElementType _lockedElement = ElementType.None;
        [SerializeField] private float _moveSpeed = 2.5f;
        [SerializeField] private float _aggroRange = 5f;
        [SerializeField] private float _deAggroRange = 7f;   // 추격 포기 거리(이력현상)
        [SerializeField] private float _attackRange = 1.3f;  // 이 안이면 멈춰서 공격
        [SerializeField] private float _patrolHalfWidth = 2f; // 정찰 반경(0이면 제자리)
        [SerializeField] private float _attackCooldown = 1f;  // 타격 후 회복(다음 공격까지)
        [SerializeField] private float _attackWindup = 0.4f;  // 예비동작(회피 가능 창)
        [SerializeField] private EnemyArchetype _archetype = EnemyArchetype.Melee;
        [SerializeField] private float _standoffRange = 3f;  // Ranged 전용: 이보다 가까우면 후퇴
        [SerializeField] private float _pullSpeed = 8f;         // 끌어당겨질 때의 등속(이동속도와 무관)
        [SerializeField] private float _pullStopDistance = 0.8f; // 플레이어와 이만큼 가까워지면 끌기 정지
        [SerializeField] private float _knockbackForce = 5f;   // 피격 시 밀려나는 힘
        [SerializeField] private float _hitstunDuration = 0.18f; // 피격 경직(넉백 유지 + 행동 정지)
        [SerializeField] private Color _windupColor = new Color(1f, 0.5f, 0.2f); // 예비동작 텔레그래프 색

        [Header("Ranged (아처)")]
        [SerializeField] private float _projectileSpeed = 8f;
        [SerializeField] private float _projectileLifetime = 2.5f;
        [SerializeField] private Color _projectileColor = new Color(0.6f, 1f, 0.9f);

        [Header("드랍")]
        [SerializeField] private List<DropEntry> _drops = new List<DropEntry>();

        // 알파벳 외 보상(골드/포션). 층 예산에 묶이는 알파벳과 달리 순수 확률 보너스다.
        [SerializeField] private List<RewardEntry> _rewardDrops = new List<RewardEntry>();

        // 방의 필수 재료(GuaranteedLoot)를 이 적의 확정 드랍(chance=1)으로 주입한다.
        // RoomManager가 방 스폰 시 호출 → 이 적을 죽이면 필수 재료가 반드시 나온다(전투→재료 보장).
        public void AddGuaranteedDrop(Help.Item.AlphabetMaterial mat)
        {
            _drops.Add(new DropEntry { Material = mat, Count = 1, Chance = 1f });
        }

        [Header("보스")]
        [SerializeField] private bool _isBoss = false;
        public bool IsBoss => _isBoss;

        // 처치 통지(방 클리어 집계용 — EnemyClearObjective가 구독). HandleDeath에서 1회 발화.
        public event System.Action<EnemyBase> OnDied;

        protected Rigidbody2D Rb { get; private set; }
        protected EnemyStats Stats { get; private set; }
        protected EnemyStatus Status { get; private set; }
        protected Transform Player { get; private set; }

        private PlayerController _playerController;
        private readonly EnemyAI _ai = new EnemyAI();
        private EnemyMeleeAttack _melee;
        private HitFlash _flash;
        private float _patrolAnchorX;
        private float _hitstunTimer;
        private Vector3 _spawnPosition;

        // 디버그/테스트용 현재 상태 노출
        public EnemyState State => _ai.State;

        private void Awake()
        {
            Rb = GetComponent<Rigidbody2D>();
            Stats = new EnemyStats(_maxHp, _attackPower, _lockedElement);
            Stats.OnDied += HandleDeath;
            Status = new EnemyStatus();
            _melee = new EnemyMeleeAttack(_attackWindup, _attackCooldown);

            var hurtbox = GetComponentInChildren<Hurtbox>();
            hurtbox?.Init(Stats, Status);

            _flash = GetComponentInChildren<HitFlash>();
            if (_flash == null) _flash = gameObject.AddComponent<HitFlash>();

            _spawnPosition = transform.position;
            _patrolAnchorX = transform.position.x;
        }

        private void Start()
        {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                Player = playerObj.transform;
                _playerController = playerObj.GetComponent<PlayerController>();
            }

            // 런 리셋 시 생존한 적을 원상 복구(HP/AI/위치). 이미 처치돼 파괴 중이면 IsAlive=false로 건너뜀.
            if (GameManager.Instance != null) GameManager.Instance.OnRunReset += HandleRunReset;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnRunReset -= HandleRunReset;
        }

        private void HandleRunReset()
        {
            if (!Stats.IsAlive) return; // 처치된 적은 재스폰하지 않음(프로토타입 한계 — 던전 데이터 기반 스폰은 추후)
            Stats.Reset();
            _ai.Reset();
            _melee.Reset();
            Status.Reset();  // 상태이상 잔존 방지 — 부활한 적이 속박된 채 시작하지 않도록
            _hitstunTimer = 0f;
            transform.position = _spawnPosition;
            Rb.linearVelocity = Vector2.zero;
        }

        // 물리는 FixedUpdate에서 (CLAUDE.md 컨벤션)
        protected virtual void FixedUpdate()
        {
            if (!Stats.IsAlive) return;
            float dt = Time.fixedDeltaTime;
            Status.Tick(dt);

            // 피격 경직: 행동/공격 진행을 멈추고 넉백 속도를 유지한다.
            if (_hitstunTimer > 0f)
            {
                _hitstunTimer -= dt;
                return;
            }

            var decision = _ai.Tick(BuildPerception(), BuildConfig(), _melee.Phase == MeleePhase.Ready);
            var result = _melee.Advance(dt, decision.Attack);

            if (result.StartedWindup) _flash?.Tint(_windupColor, _attackWindup); // 텔레그래프

            // 예비동작 중엔 제자리에 멈춰 "공격 온다"를 읽히게 한다(끌림 중이면 물리 우선).
            if (!Status.IsPulled && _melee.Phase == MeleePhase.Windup)
                Rb.linearVelocity = new Vector2(0f, Rb.linearVelocity.y);
            else
                ApplyMovement(decision);

            if (result.Strike) DoAttack();
        }

        // AI 결정을 실제 물리 이동으로 적용. 외력(끌어당김)이 AI 의도를 덮어쓰는 단일 지점.
        private void ApplyMovement(EnemyDecision decision)
        {
            // 끌림 중엔 적의 의지와 무관하게 플레이어 쪽으로 등속 이동(속박 중이라 AI는 어차피 MoveDir=0)
            if (Status.IsPulled && Player != null)
            {
                float pullVx = PullMotion.VelocityX(
                    transform.position.x, Player.position.x, _pullSpeed, _pullStopDistance);
                Rb.linearVelocity = new Vector2(pullVx, Rb.linearVelocity.y);
                if (pullVx != 0f) Face(pullVx);
                return;
            }

            Rb.linearVelocity = new Vector2(decision.MoveDir * _moveSpeed, Rb.linearVelocity.y);
            if (decision.MoveDir != 0f) Face(decision.MoveDir);
        }

        private EnemyPerception BuildPerception()
        {
            if (Player == null)
                return new EnemyPerception
                {
                    HasTarget = false,
                    SelfX = transform.position.x,
                    IsRestrained = Status.IsRestrained,
                };

            return new EnemyPerception
            {
                HasTarget = true,
                SelfX = transform.position.x,
                TargetX = Player.position.x,
                DistanceToTarget = Vector2.Distance(transform.position, Player.position),
                IsRestrained = Status.IsRestrained,
            };
        }

        private EnemyAIConfig BuildConfig() => new EnemyAIConfig
        {
            AggroRange = _aggroRange,
            DeAggroRange = _deAggroRange,
            AttackRange = _attackRange,
            PatrolAnchorX = _patrolAnchorX,
            PatrolHalfWidth = _patrolHalfWidth,
            Archetype = _archetype,
            StandoffRange = _standoffRange,
        };

        // 예비동작이 끝난 순간의 실제 공격. Melee는 근접 판정(회피 가능), Ranged는 투사체 발사.
        private void DoAttack()
        {
            if (Player == null || _playerController == null) return;

            if (_archetype == EnemyArchetype.Ranged) { FireProjectile(); return; }

            float dist = Vector2.Distance(transform.position, Player.position);
            if (dist <= _attackRange * 1.25f)
                _playerController.TakeDamage(Stats.AttackPower, transform.position);
        }

        private void FireProjectile()
        {
            Vector2 dir = (Vector2)(Player.position - transform.position);
            if (dir.sqrMagnitude < 0.0001f) dir = new Vector2(transform.localScale.x >= 0f ? 1f : -1f, 0f);
            dir.Normalize();
            Vector3 spawnPos = transform.position + (Vector3)(dir * 0.6f);
            EnemyProjectile.Spawn(spawnPos, dir * _projectileSpeed, Stats.AttackPower, _projectileLifetime, _projectileColor, transform.parent);
        }

        // 플레이어 공격에 맞았을 때(Hitbox가 호출): 플래시 + 넉백 + 경직.
        public void OnHitReceived(Vector3 attackerPosition)
        {
            if (!Stats.IsAlive) return;
            _flash?.Flash();
            float dir = Mathf.Sign(transform.position.x - attackerPosition.x);
            if (dir == 0f) dir = 1f;
            Rb.linearVelocity = new Vector2(dir * _knockbackForce, Rb.linearVelocity.y + 1.5f);
            _hitstunTimer = _hitstunDuration;
        }

        private void Face(float dir)
        {
            var s = transform.localScale;
            s.x = Mathf.Abs(s.x) * (dir >= 0f ? 1f : -1f);
            transform.localScale = s;
        }

        private void HandleDeath()
        {
            Rb.linearVelocity = Vector2.zero;
            DropLoot();
            DropRewards();
            OnDied?.Invoke(this); // 방 클리어 집계 통지
            Destroy(gameObject, 0.5f);
        }

        // 처치 보상: 드랍 테이블을 굴려 재료 글자를 월드에 물리 드랍(주워서 획득) → 전투→크래프팅 루프.
        //
        // 테이블이 비어 있으면 **아무것도 안 나온다**. 예전엔 잠금 속성 글자를 확정 드랍하는 폴백이 있었으나,
        // 층 예산(FloorLootPlan) 밖에서 글자가 새어 나와 알파벳이 난잡해지는 통로였다.
        // 이 적이 무엇을 떨굴지는 방 로드 시 RoomManager가 배분해 주입한다(AddGuaranteedDrop).
        private void DropLoot()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.RecipeDatabase == null) return;

            var drops = DropRoller.Roll(_drops, () => Random.value);

            int i = 0;
            foreach (var d in drops)
                for (int c = 0; c < d.Count; c++)
                {
                    float ox = (i % 2 == 0 ? 1f : -1f) * (0.4f + 0.25f * (i / 2)); // 좌우로 흩뿌림
                    // 방 콘텐츠(transform.parent) 하위로 넣어 방 상태와 함께 캐시/비활성화되게 한다.
                    MaterialPickup.Spawn(d.Material, transform.position, new Vector3(ox, -0.3f, 0f), transform.parent);
                    i++;
                }
        }

        // 알파벳 외 보상(골드/포션) — 확률 보너스라 아무것도 안 나올 수 있다.
        private void DropRewards()
        {
            var rewards = RewardRoller.Roll(_rewardDrops, () => Random.value);

            int i = 0;
            foreach (var r in rewards)
            {
                float ox = (i % 2 == 0 ? -1f : 1f) * (0.55f + 0.25f * (i / 2)); // 재료 드랍과 반대 방향부터
                RewardPickup.Spawn(r.Kind, r.Amount, transform.position, new Vector3(ox, -0.3f, 0f), transform.parent);
                i++;
            }
        }
    }
}

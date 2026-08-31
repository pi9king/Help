using System;

namespace Help.Player
{
    public class PlayerStats
    {
        public int MaxHp { get; private set; }
        public int CurrentHp { get; private set; }
        public int AttackPower { get; private set; }
        public int Defense { get; private set; }
        public float MoveSpeed { get; private set; }
        public float JumpForce { get; private set; }
        public float DashForce { get; private set; }

        public event Action<int, int> OnHpChanged;  // (current, max)
        public event Action OnDied;

        public bool IsAlive => CurrentHp > 0;

        // 런 리셋 시 능력치를 되돌리기 위한 기준값(장비 보너스로 AttackPower/Defense가 변동해도 복원 가능)
        private readonly int _baseAttack;
        private readonly int _baseDefense;

        // attack 기본값 = 맨손 공격력. 무기 없이도 기본 적(HP 30)을 2타에 잡을 수 있는 하한.
        public PlayerStats(int maxHp = 100, int attack = 15, int defense = 0,
                           float moveSpeed = 7f, float jumpForce = 14f, float dashForce = 20f)
        {
            MaxHp = maxHp;
            CurrentHp = maxHp;
            AttackPower = attack;
            Defense = defense;
            _baseAttack = attack;
            _baseDefense = defense;
            MoveSpeed = moveSpeed;
            JumpForce = jumpForce;
            DashForce = dashForce;
        }

        public void TakeDamage(int damage)
        {
            if (CurrentHp == 0) return; // 이미 사망 — OnDied 재발화 방지(1회 사망 계약)
            int effective = Math.Max(0, damage - Defense);
            CurrentHp = Math.Max(0, CurrentHp - effective);
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
            if (CurrentHp == 0) OnDied?.Invoke();
        }

        public void Heal(int amount)
        {
            if (CurrentHp == 0) return; // 사망 상태는 Heal로 부활하지 않음 — 부활은 Reset(런 리셋)으로만
            CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
        }

        // 런 리셋 시 부활: HP를 가득 채우고 능력치를 기준값으로 되돌린다.
        // (장비 보너스는 보통 Inventory.Clear의 Unequip 이벤트로도 해제되지만, 여기서 base로 하드 복원해 이월을 이중 차단)
        public void Reset()
        {
            CurrentHp = MaxHp;
            AttackPower = _baseAttack;
            Defense = _baseDefense;
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
        }

        public void ApplyEquipmentBonus(int attackBonus, int defenseBonus)
        {
            AttackPower += attackBonus;
            Defense += defenseBonus;
        }

        public void RemoveEquipmentBonus(int attackBonus, int defenseBonus)
        {
            AttackPower -= attackBonus;
            Defense -= defenseBonus;
        }
    }
}

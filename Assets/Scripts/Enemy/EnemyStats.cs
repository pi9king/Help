using System;
using Help.Combat;

namespace Help.Enemy
{
    public class EnemyStats
    {
        public int MaxHp { get; private set; }
        public int CurrentHp { get; private set; }
        public int AttackPower { get; private set; }
        public ElementType LockedElement { get; private set; }  // None = 잠금 없음

        public event Action<int, int> OnHpChanged;
        public event Action OnDied;

        public bool IsAlive => CurrentHp > 0;

        public EnemyStats(int maxHp, int attack, ElementType lockedElement = ElementType.None)
        {
            MaxHp = maxHp;
            CurrentHp = maxHp;
            AttackPower = attack;
            LockedElement = lockedElement;
        }

        public void TakeDamage(int damage)
        {
            if (CurrentHp == 0) return; // 이미 사망 — 시체 재타격 시 OnDied 재발화 방지(1회 사망 계약)
            CurrentHp = Math.Max(0, CurrentHp - damage);
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
            if (CurrentHp == 0) OnDied?.Invoke();
        }

        // 런 리셋 시 생존한 적을 원상 복구(HP 가득)
        public void Reset()
        {
            CurrentHp = MaxHp;
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
        }
    }
}

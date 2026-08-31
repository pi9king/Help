namespace Help.Core
{
    // 골드 지갑 — 순수 C#(EditMode 테스트 가능).
    // 알파벳은 인벤토리 아이템이지만, 재화는 슬롯을 차지하지 않는 카운터로 둔다.
    public class Wallet
    {
        public int Gold { get; private set; }

        public event System.Action OnChanged;

        public void Add(int amount)
        {
            if (amount <= 0) return; // 변화 없으면 통지도 없다
            Gold += amount;
            OnChanged?.Invoke();
        }

        // 잔액이 모자라면 아무것도 건드리지 않고 false (부분 지불 없음)
        public bool TrySpend(int amount)
        {
            if (amount <= 0 || Gold < amount) return false;
            Gold -= amount;
            OnChanged?.Invoke();
            return true;
        }

        // 런 리셋(사망) 시 초기화
        public void Reset()
        {
            if (Gold == 0) return;
            Gold = 0;
            OnChanged?.Invoke();
        }
    }
}

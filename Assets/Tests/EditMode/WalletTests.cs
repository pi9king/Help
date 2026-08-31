using NUnit.Framework;
using Help.Core;

namespace Tests.EditMode
{
    // 골드 지갑 — 알파벳 외 재화의 첫 형태. 인벤토리 슬롯을 차지하지 않는 카운터다.
    public class WalletTests
    {
        [Test]
        public void Add_IncreasesGoldAndNotifies()
        {
            var wallet = new Wallet();
            int changed = 0;
            wallet.OnChanged += () => changed++;

            wallet.Add(30);
            wallet.Add(20);

            Assert.AreEqual(50, wallet.Gold);
            Assert.AreEqual(2, changed);
        }

        [Test]
        public void Add_IgnoresNonPositiveAmount()
        {
            var wallet = new Wallet();
            wallet.Add(10);
            int changed = 0;
            wallet.OnChanged += () => changed++;

            wallet.Add(0);
            wallet.Add(-5);

            Assert.AreEqual(10, wallet.Gold);
            Assert.AreEqual(0, changed, "변화가 없으면 통지도 없어야 한다");
        }

        [Test]
        public void TrySpend_FailsWithoutChangingBalance()
        {
            var wallet = new Wallet();
            wallet.Add(10);

            Assert.IsFalse(wallet.TrySpend(11));
            Assert.AreEqual(10, wallet.Gold, "실패한 지불이 잔액을 건드리면 안 된다");
        }

        [Test]
        public void TrySpend_DeductsWhenAffordable()
        {
            var wallet = new Wallet();
            wallet.Add(10);

            Assert.IsTrue(wallet.TrySpend(10));
            Assert.AreEqual(0, wallet.Gold);
        }

        [Test]
        public void Reset_ClearsGoldForNewRun()
        {
            var wallet = new Wallet();
            wallet.Add(99);

            wallet.Reset();

            Assert.AreEqual(0, wallet.Gold);
        }
    }
}

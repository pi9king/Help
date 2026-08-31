using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Help.Core;
using Help.Dungeon;
using Help.Item;
using Help.Player;

namespace Help.UI
{
    // 보스 처치 시 선택형 보상을 띄운다: 완성 아이템 카드 2~3장 중 하나를 골라 인벤토리에 넣고,
    // 선택 후 다음 층 포탈을 스폰한다. RoomManager.OnBossRoomCleared를 구독.
    // 화면은 런타임 자체 구성(UITheme). HUD가 Canvas에 부착한다.
    public class BossRewardUI : MonoBehaviour
    {
        private GameObject _panel;
        private Transform _cardRow;
        private PlayerController _player;
        private RoomManager _rm;
        private bool _built;

        private void Start()
        {
            _player = FindFirstObjectByType<PlayerController>();
            _rm = FindFirstObjectByType<RoomManager>();
            if (_rm != null) _rm.OnBossRoomCleared += HandleBossCleared;
        }

        private void OnDestroy()
        {
            if (_rm != null) _rm.OnBossRoomCleared -= HandleBossCleared;
        }

        private void HandleBossCleared()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.RecipeDatabase == null) { _rm?.SpawnNextFloorPortal(); return; }

            // 비-E(특수) 아이템은 특수방에서만 나와야 희소성이 유지된다 — 보스 보상 후보에서 제외.
            var candidates = new List<ItemDefinition>();
            foreach (var item in gm.RecipeDatabase.AllItems)
                if (AlphabetWordRule.IsBasicCraftable(item)) candidates.Add(item);

            var rewards = BossRewardPool.Pick(candidates, 3, gm.CurrentFloor);
            if (rewards.Count == 0) { _rm?.SpawnNextFloorPortal(); return; } // 후보 없으면 보상 스킵

            Show(rewards);
        }

        private void Show(List<ItemDefinition> rewards)
        {
            EnsureBuilt();
            for (int i = _cardRow.childCount - 1; i >= 0; i--)
            {
                var c = _cardRow.GetChild(i);
                c.SetParent(null, false);
                Destroy(c.gameObject);
            }

            foreach (var item in rewards)
                BuildCard(item);

            _panel.SetActive(true);
            if (_player != null) _player.SetUiPanelOpen(this, true); // 모달: 다른 입력 차단
        }

        private void Choose(ItemDefinition item)
        {
            GameManager.Instance.Inventory.Add(item, 1);
            _panel.SetActive(false);
            if (_player != null) _player.SetUiPanelOpen(this, false);
            _rm?.SpawnNextFloorPortal(); // 보상 수령 후 포탈 등장
        }

        // ---------- 구성 ----------

        private void EnsureBuilt()
        {
            if (_built) return;
            _built = true;

            _panel = new GameObject("BossRewardPanel", typeof(RectTransform));
            _panel.transform.SetParent(transform, false);
            var prt = _panel.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = Vector2.zero;
            prt.sizeDelta = new Vector2(640f, 340f);

            var frame = UITheme.BuildPanelFrame(_panel);
            UITheme.BuildHeader(frame, "보스 보상 선택", null); // 닫기 없음 — 반드시 하나 선택
            var body = UITheme.BuildBody(frame);
            UITheme.Label(body, "Hint", "하나를 선택하세요", 14, UITheme.Dim);
            _cardRow = UITheme.Horizontal(body, "CardRow", 200f);

            _panel.SetActive(false);
        }

        private void BuildCard(ItemDefinition item)
        {
            string label = item.Word.ToUpperInvariant() + "\n\n" + TypeLabel(item.Type);
            if (item.AttackBonus != 0) label += "\n+ATK " + item.AttackBonus;
            if (item.DefenseBonus != 0) label += "\n+DEF " + item.DefenseBonus;
            if (item.Element != Help.Combat.ElementType.None) label += "\n" + item.Element;

            var (btn, text) = UITheme.Button(_cardRow, label, 15);
            var le = btn.GetComponent<LayoutElement>();
            le.minWidth = 170; le.preferredWidth = 180; le.minHeight = 180; le.preferredHeight = 180;
            UITheme.MakePrimary(btn, text);
            text.alignment = TextAnchor.MiddleCenter;
            var captured = item;
            btn.onClick.AddListener(() => Choose(captured));
        }

        private static string TypeLabel(ItemType t)
        {
            switch (t)
            {
                case ItemType.Weapon: return "무기";
                case ItemType.SubWeapon: return "보조무기";
                case ItemType.HeadArmor: return "머리 방어구";
                case ItemType.BodyArmor: return "몸통 방어구";
                case ItemType.LegArmor: return "다리 방어구";
                case ItemType.Accessory: return "장신구";
                default: return t.ToString();
            }
        }
    }
}

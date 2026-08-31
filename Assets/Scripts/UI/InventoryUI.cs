using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Help.Core;
using Help.Inventory;
using Help.Item;
using Help.Player;

namespace Help.UI
{
    public class InventoryUI : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private Transform _itemListParent;
        [SerializeField] private GameObject _itemSlotPrefab;
        [SerializeField] private CraftingUI _craftingUI;

        private PlayerController _player;

        private void Start()
        {
            _panel.SetActive(false);
            GameManager.Instance.OnInventoryChanged += Refresh;
            GameManager.Instance.OnRunReset += CloseOnRunReset; // 사망 시 패널을 닫아 부활 후 입력 잠금 잔존 방지

            // I 키 입력(PlayerController)을 받아 패널 토글
            _player = FindFirstObjectByType<PlayerController>();
            if (_player != null) _player.InventoryToggleRequested += Toggle;

            // HUD 인벤토리 버튼 클릭은 HUD가 직렬화된 참조로 연결한다(여기서 중복 배선하면 Toggle이 두 번 호출됨).
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnInventoryChanged -= Refresh;
                GameManager.Instance.OnRunReset -= CloseOnRunReset;
            }
            if (_player != null) _player.InventoryToggleRequested -= Toggle;
        }

        // 런 리셋(사망) 시 열려 있던 패널을 닫는다(입력 게이트도 함께 해제됨).
        private void CloseOnRunReset()
        {
            if (_panel.activeSelf) Toggle();
        }

        public void Toggle()
        {
            _panel.SetActive(!_panel.activeSelf);
            if (_panel.activeSelf) Refresh();
            // 패널 오픈 중 게임플레이 입력(이동/공격 등) 차단을 위해 상태를 알린다
            if (_player != null) _player.SetUiPanelOpen(this, _panel.activeSelf);
        }

        private void Refresh()
        {
            if (!_panel.activeSelf) return;

            foreach (Transform child in _itemListParent)
                Destroy(child.gameObject);

            var inv = GameManager.Instance.Inventory;
            foreach (var stack in inv.Items)
                CreateSlot(stack, null);
            // 장착 중인 아이템도 목록에 표시 (해제 버튼 제공)
            foreach (var kv in inv.Equipped)
                CreateSlot(kv.Value, kv.Key);
        }

        private void CreateSlot(ItemStack stack, EquipmentSlotType? equippedSlot)
        {
            var go = Instantiate(_itemSlotPrefab, _itemListParent);
            bool isEquipped = equippedSlot.HasValue;

            var nameText = go.transform.Find("Name")?.GetComponent<Text>();
            if (nameText != null)
                nameText.text = isEquipped
                    ? $"[E] {stack.Definition.Word}"
                    : $"{stack.Definition.Word} x{stack.Count}";

            // CraftButton을 인벤토리에서는 장착/해제 버튼으로 재활용
            var actionBtn = go.transform.Find("CraftButton")?.GetComponent<Button>();
            if (actionBtn != null)
                ConfigureActionButton(actionBtn, stack, equippedSlot);

            var disassembleBtn = go.transform.Find("DisassembleButton")?.GetComponent<Button>();
            if (disassembleBtn != null)
            {
                // 장착품/재료는 분해 불가
                if (isEquipped || stack.Definition.Type == ItemType.Material)
                {
                    disassembleBtn.gameObject.SetActive(false);
                }
                else
                {
                    string id = stack.Definition.Id;
                    disassembleBtn.onClick.AddListener(() =>
                    {
                        GameManager.Instance.Crafting.Disassemble(id, GameManager.Instance.Inventory);
                    });
                }
            }
        }

        private void ConfigureActionButton(Button btn, ItemStack stack, EquipmentSlotType? equippedSlot)
        {
            var label = btn.GetComponentInChildren<Text>();

            if (equippedSlot.HasValue)
            {
                if (label != null) label.text = "해제";
                var slot = equippedSlot.Value;
                btn.onClick.AddListener(() => GameManager.Instance.Inventory.Unequip(slot));
                return;
            }

            if (EquipmentSlotResolver.TryResolve(stack.Definition.Type, out var target))
            {
                if (label != null) label.text = "장착";
                var st = stack;
                btn.onClick.AddListener(() => GameManager.Instance.Inventory.Equip(st, target));
                return;
            }

            // 재료 등 장착 불가 아이템은 버튼 숨김
            btn.gameObject.SetActive(false);
        }
    }
}

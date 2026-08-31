using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Help.Core;
using Help.Inventory;
using Help.Item;
using Help.Player;

namespace Help.UI
{
    // 슬롯형 인벤토리 UI (온라인 게임식).
    // 상단 = 장비 슬롯(고정, 유형별). 하단 = 가방(고정 칸 수, 빈 슬롯도 항상 표시하고 아이템이 획득 순서대로 채움).
    // 슬롯을 선택하면 하단 액션 행에 장착/해제/분해 버튼이 뜬다. 화면 구성은 런타임 자체 생성(프리팹 의존 X).
    // 색/폰트/위젯은 공용 UITheme(레트로 픽셀 아케이드 톤)에 위임한다.
    public class InventoryGridUI : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private int _bagCapacity = 24; // 가방 기본 칸 수(6열 × 4행)

        private PlayerController _player;
        private bool _built;

        private Transform _equipRow;
        private Transform _bagGrid;
        private Transform _actionRow;
        private Text _selectedLabel;

        // 장비 슬롯 표시 순서 + 한국어 라벨(빈 슬롯 안내용)
        private static readonly EquipmentSlotType[] EquipOrder =
        {
            EquipmentSlotType.Weapon, EquipmentSlotType.SubWeapon, EquipmentSlotType.Head,
            EquipmentSlotType.Body, EquipmentSlotType.Legs, EquipmentSlotType.Accessory
        };

        private static string EquipLabel(EquipmentSlotType s)
        {
            switch (s)
            {
                case EquipmentSlotType.Weapon: return "무기";
                case EquipmentSlotType.SubWeapon: return "보조";
                case EquipmentSlotType.Head: return "머리";
                case EquipmentSlotType.Body: return "몸통";
                case EquipmentSlotType.Legs: return "다리";
                default: return "장신구";
            }
        }

        // 선택 상태
        private enum SelKind { None, Bag, Equip }
        private SelKind _sel;
        private ItemDefinition _selBagDef;          // Bag 선택 시
        private EquipmentSlotType _selEquipSlot;     // Equip 선택 시

        private void Start()
        {
            if (_panel == null) _panel = gameObject;
            _panel.SetActive(false);

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnInventoryChanged += RefreshDynamic;
                gm.OnRunReset += CloseOnRunReset;
            }

            _player = FindFirstObjectByType<PlayerController>();
            if (_player != null) _player.InventoryToggleRequested += Toggle;
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnInventoryChanged -= RefreshDynamic;
                gm.OnRunReset -= CloseOnRunReset;
            }
            if (_player != null) _player.InventoryToggleRequested -= Toggle;
        }

        private void CloseOnRunReset()
        {
            if (_panel.activeSelf) Toggle();
        }

        public void Toggle()
        {
            _panel.SetActive(!_panel.activeSelf);
            if (_panel.activeSelf)
            {
                EnsureBuilt();
                ClearSelection();
                Refresh();
            }
            if (_player != null) _player.SetUiPanelOpen(this, _panel.activeSelf);
        }

        private void RefreshDynamic()
        {
            if (!_panel.activeSelf) return;
            ClearSelection(); // 인벤토리 변경 시 선택 참조가 무효화될 수 있어 초기화
            Refresh();
        }

        // ---------- 선택 / 액션 ----------

        private void SelectBag(ItemDefinition def)
        {
            _sel = SelKind.Bag;
            _selBagDef = def;
            RefreshAllSlots();
            RefreshActionRow();
        }

        private void SelectEquip(EquipmentSlotType slot)
        {
            _sel = SelKind.Equip;
            _selEquipSlot = slot;
            RefreshAllSlots();
            RefreshActionRow();
        }

        private void ClearSelection()
        {
            _sel = SelKind.None;
            _selBagDef = null;
        }

        private void RefreshActionRow()
        {
            if (!_built) return;
            for (int i = _actionRow.childCount - 1; i >= 0; i--)
            {
                var c = _actionRow.GetChild(i);
                c.SetParent(null, false);
                Destroy(c.gameObject);
            }

            var inv = GameManager.Instance.Inventory;

            if (_sel == SelKind.Bag && _selBagDef != null)
            {
                _selectedLabel.text = _selBagDef.Word.ToUpperInvariant();
                _selectedLabel.color = UITheme.Text;

                if (EquipmentSlotResolver.TryResolve(_selBagDef.Type, out var target))
                {
                    var def = _selBagDef;
                    var (btn, label) = AddActionButton("장착", () =>
                    {
                        var i2 = GameManager.Instance.Inventory;
                        ItemStack stack = null;
                        foreach (var s in i2.Items)
                            if (s.Definition.Id == def.Id) { stack = s; break; }
                        if (stack != null) i2.Equip(stack, target);
                    });
                    UITheme.MakePrimary(btn, label);
                }
                if (_selBagDef.Type == ItemType.Consumable && _selBagDef.HealAmount > 0)
                {
                    var potion = _selBagDef;
                    var (useBtn, useLabel) = AddActionButton("사용", () => UseConsumable(potion));
                    UITheme.MakePrimary(useBtn, useLabel);
                }
                if (_selBagDef.Type != ItemType.Material)
                {
                    var id = _selBagDef.Id;
                    var (btn, label) = AddActionButton("분해", () =>
                        GameManager.Instance.Crafting.Disassemble(id, GameManager.Instance.Inventory));
                    UITheme.MakeDanger(btn, label);
                }
                return;
            }

            if (_sel == SelKind.Equip && inv.Equipped.TryGetValue(_selEquipSlot, out var equipped))
            {
                _selectedLabel.text = equipped.Definition.Word.ToUpperInvariant() + " (장착 중)";
                _selectedLabel.color = UITheme.Accent2;
                var slot = _selEquipSlot;
                var (btn, label) = AddActionButton("해제", () =>
                    GameManager.Instance.Inventory.Unequip(slot));
                UITheme.MakeDanger(btn, label);
                return;
            }

            _selectedLabel.text = "슬롯을 선택하세요";
            _selectedLabel.color = UITheme.Dim;
        }

        // 소모품 사용: 플레이어를 회복시키고 1개 소모. 회복이 불가능하면(만피/플레이어 없음) 소모하지 않는다.
        private void UseConsumable(ItemDefinition potion)
        {
            var playerObj = GameObject.FindWithTag("Player");
            var player = playerObj != null ? playerObj.GetComponent<Help.Player.PlayerController>() : null;
            if (player == null || player.Stats == null) return;
            if (player.Stats.CurrentHp >= player.Stats.MaxHp) return;

            player.Stats.Heal(potion.HealAmount);
            GameManager.Instance.Inventory.Remove(potion, 1);
        }

        private (Button, Text) AddActionButton(string label, UnityEngine.Events.UnityAction action)
        {
            var (btn, t) = UITheme.Button(_actionRow, label, 16);
            btn.GetComponent<LayoutElement>().minHeight = 42;
            btn.onClick.AddListener(action);
            return (btn, t);
        }

        // ---------- 갱신 ----------

        private void Refresh()
        {
            RefreshEquip();
            RefreshBag();
            RefreshActionRow();
        }

        // 선택 강조만 다시 칠할 때(내용 변화 없이) — 슬롯 전체 재구성 대신 색만
        private void RefreshAllSlots()
        {
            RefreshEquip();
            RefreshBag();
        }

        private void RefreshEquip()
        {
            if (!_built) return;
            ClearChildren(_equipRow);

            var inv = GameManager.Instance.Inventory;
            foreach (var slotType in EquipOrder)
            {
                bool has = inv.Equipped.TryGetValue(slotType, out var stack);
                bool selected = _sel == SelKind.Equip && _selEquipSlot == slotType;
                string text = has ? stack.Definition.Word.ToUpperInvariant() : EquipLabel(slotType);

                var (btn, label) = UITheme.Button(_equipRow, text, has ? 13 : 12);
                var le = btn.GetComponent<LayoutElement>();
                le.minWidth = 74; le.preferredWidth = 74; le.minHeight = 50;
                PaintSlot(btn, label, has, selected, has ? UITheme.Accent2 : UITheme.Dim);

                var captured = slotType;
                if (has) btn.onClick.AddListener(() => SelectEquip(captured));
                else UITheme.SetColors(btn, UITheme.Slot, UITheme.Slot, UITheme.Slot); // 빈 장비칸=클릭 무반응 느낌
            }
        }

        private void RefreshBag()
        {
            if (!_built) return;
            ClearChildren(_bagGrid);

            var inv = GameManager.Instance.Inventory;
            var items = inv.Items;
            int slots = Mathf.Max(_bagCapacity, items.Count); // 아이템이 용량 초과해도 숨기지 않음

            for (int i = 0; i < slots; i++)
            {
                bool has = i < items.Count;
                if (has)
                {
                    var stack = items[i];
                    string text = stack.Definition.Word.ToUpperInvariant() +
                                  (stack.Count > 1 ? "\n×" + stack.Count : "");
                    var (btn, label) = UITheme.Button(_bagGrid, text, 13);
                    bool selected = _sel == SelKind.Bag && _selBagDef != null && _selBagDef.Id == stack.Definition.Id;
                    PaintSlot(btn, label, true, selected, UITheme.Text);
                    var def = stack.Definition;
                    btn.onClick.AddListener(() => SelectBag(def));
                }
                else
                {
                    // 빈 슬롯: 어두운 칸(클릭 반응 없음)
                    var (btn, label) = UITheme.Button(_bagGrid, "", 13);
                    PaintSlot(btn, label, false, false, UITheme.Dim);
                    UITheme.SetColors(btn, UITheme.Slot, UITheme.Slot, UITheme.Slot);
                }
            }
        }

        // 슬롯 색 지정: 채움/빈/선택 상태 반영. 선택 시 노랑 하이라이트.
        private void PaintSlot(Button btn, Text label, bool has, bool selected, Color textColor)
        {
            var fill = UITheme.AsSwatch(btn);
            if (fill != null)
                fill.color = selected ? UITheme.ButtonHover : (has ? UITheme.SlotFill : UITheme.Slot);
            label.color = selected ? UITheme.Accent : textColor;
            label.fontStyle = FontStyle.Bold;
        }

        private static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var c = t.GetChild(i);
                c.SetParent(null, false);
                Object.Destroy(c.gameObject);
            }
        }

        // ---------- 런타임 UI 구성(1회) ----------

        private void EnsureBuilt()
        {
            if (_built) return;
            _built = true;

            var frame = UITheme.BuildPanelFrame(_panel);
            UITheme.BuildHeader(frame, "인벤토리", Toggle);
            var body = UITheme.BuildBody(frame);

            UITheme.Label(body, "EquipTitle", "장비", 13, UITheme.Dim).alignment = TextAnchor.MiddleLeft;
            _equipRow = UITheme.Horizontal(body, "EquipRow", 54f);

            UITheme.Label(body, "BagTitle", "가방", 13, UITheme.Dim).alignment = TextAnchor.MiddleLeft;

            var gridGo = new GameObject("BagGrid", typeof(RectTransform));
            gridGo.transform.SetParent(body, false);
            var grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(74f, 46f);
            grid.spacing = new Vector2(6f, 6f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 6;
            grid.childAlignment = TextAnchor.UpperCenter;
            var gridLe = gridGo.AddComponent<LayoutElement>();
            gridLe.minHeight = 210; gridLe.flexibleHeight = 1;
            _bagGrid = gridGo.transform;

            _selectedLabel = UITheme.Label(body, "SelectedLabel", "슬롯을 선택하세요", 15, UITheme.Dim);
            _actionRow = UITheme.Horizontal(body, "ActionRow", 48f);
        }
    }
}

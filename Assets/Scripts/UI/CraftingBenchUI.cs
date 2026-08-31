using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Help.Core;
using Help.Crafting;
using Help.Item;
using Help.Player;

namespace Help.UI
{
    // 발견형 슬롯 배치 크래프팅 UI.
    // 레시피 목록을 보여주지 않고, 보유한 글자를 슬롯에 놓아 유효한 단어(레시피)가 되면 제작 버튼이 활성화된다.
    // 판정은 순수 로직 CraftingBench/RecipeMatcher에 위임하고, 이 클래스는 표시·입력만 담당(로직/뷰 분리).
    // 화면 구성물은 런타임에 스스로 생성한다(_panel 아래) — 프리팹 의존을 없애 씬 배선을 단순화.
    // 색/폰트/위젯은 공용 UITheme(레트로 픽셀 아케이드 톤)에 위임한다.
    public class CraftingBenchUI : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private int _slotCount = 6;

        private CraftingBench _bench;
        private PlayerController _player;

        private readonly List<Text> _slotLabels = new();
        private readonly List<Image> _slotFills = new();
        private Transform _paletteRow;
        private Text _resultText;
        private Button _craftButton;
        private bool _built;

        // 현재 선택된 글자(팔레트에서 고른 뒤 슬롯을 클릭해 배치). null=선택 없음.
        private AlphabetMaterial? _selectedMaterial;

        private void Start()
        {
            _bench = new CraftingBench(_slotCount);
            if (_panel == null) _panel = gameObject;
            _panel.SetActive(false);

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnInventoryChanged += RefreshDynamic;
                gm.OnRunReset += CloseOnRunReset; // 사망 시 패널 닫아 입력 잠금 잔존 방지
            }

            _player = FindFirstObjectByType<PlayerController>();
            if (_player != null) _player.CraftingToggleRequested += Toggle;
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnInventoryChanged -= RefreshDynamic;
                gm.OnRunReset -= CloseOnRunReset;
            }
            if (_player != null) _player.CraftingToggleRequested -= Toggle;
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
                _bench.Clear();
                _selectedMaterial = null;
                RefreshAll();
            }
            if (_player != null) _player.SetUiPanelOpen(this, _panel.activeSelf);
        }

        // 인벤토리 변경 시(제작·획득 등) 열려 있으면 팔레트/결과 갱신
        private void RefreshDynamic()
        {
            if (!_panel.activeSelf) return;
            RefreshAll();
        }

        // ---------- 입력 핸들러 ----------

        private void OnSlotClicked(int index)
        {
            // 글자가 선택돼 있으면 그 글자를 지정한 슬롯에 놓는다(기존 값은 덮어씀).
            if (_selectedMaterial.HasValue)
            {
                _bench.Place(index, _selectedMaterial.Value);
                _selectedMaterial = null;
                RefreshAll();
                return;
            }
            // 선택이 없으면 채워진 슬롯 클릭 → 비우기.
            if (_bench.GetSlot(index).HasValue)
            {
                _bench.Remove(index);
                RefreshAll();
            }
        }

        private void OnMaterialClicked(AlphabetMaterial mat)
        {
            // 글자 선택(같은 글자를 다시 누르면 선택 해제). 이후 원하는 슬롯을 클릭해 배치한다.
            bool same = _selectedMaterial.HasValue && _selectedMaterial.Value == mat;
            _selectedMaterial = same ? (AlphabetMaterial?)null : mat;
            RefreshAll();
        }

        // 현재 제작 모드. 특수방 제작대 범위에 들어가면 Special로 바뀐다(SpecialCraftingStation이 호출).
        private CraftMode _mode = CraftMode.Basic;

        public void SetCraftMode(CraftMode mode)
        {
            if (_mode == mode) return;
            _mode = mode;
            if (_panel != null && _panel.activeSelf) RefreshAll();
        }

        private void OnCraftClicked()
        {
            var gm = GameManager.Instance;
            var result = _bench.Result(gm.RecipeDatabase.AllItems, _mode);
            if (result == null) return;

            // 특수 제작은 골드 비용이 있다 — 제작이 성공한 뒤에 차감해야
            // "골드만 잃고 아이템은 없는" 상태가 생기지 않는다.
            int cost = _mode == CraftMode.Special ? result.GoldCost : 0;
            if (cost > 0 && gm.Wallet.Gold < cost) return;

            // 실제 재료 소모 + 아이템 추가는 기존 CraftingSystem 재사용(판정/실행 일원화)
            if (gm.Crafting.Craft(result.Id, gm.Inventory, _mode))
            {
                if (cost > 0) gm.Wallet.TrySpend(cost);
                _bench.Clear();
                RefreshAll(); // OnInventoryChanged도 오지만 즉시 반영
            }
        }

        // ---------- 갱신 ----------

        private void RefreshAll()
        {
            RefreshSlots();
            RefreshPalette();
            RefreshResult();
        }

        private void RefreshSlots()
        {
            for (int i = 0; i < _slotLabels.Count; i++)
            {
                var s = _bench.GetSlot(i);
                _slotLabels[i].text = s.HasValue ? s.Value.ToString() : "_";
                _slotLabels[i].color = s.HasValue ? UITheme.Accent2 : UITheme.Dim;

                if (i < _slotFills.Count && _slotFills[i] != null)
                {
                    if (!s.HasValue && _selectedMaterial.HasValue)
                        _slotFills[i].color = UITheme.SlotHint;  // 배치 가능 힌트
                    else if (s.HasValue)
                        _slotFills[i].color = UITheme.SlotFill;   // 채워진 슬롯
                    else
                        _slotFills[i].color = UITheme.Slot;       // 빈 슬롯
                }
            }
        }

        private void RefreshResult()
        {
            var gm = GameManager.Instance;
            var result = _bench.Result(gm.RecipeDatabase.AllItems, _mode);

            if (result == null)
            {
                _resultText.text = _mode == CraftMode.Special ? "특수 제작대 ———" : "———";
                _resultText.color = UITheme.Dim;
                if (_craftButton != null) _craftButton.interactable = false;
                return;
            }

            int cost = _mode == CraftMode.Special ? result.GoldCost : 0;
            bool affordable = cost <= 0 || gm.Wallet.Gold >= cost;

            _resultText.text = "→ " + result.Word.ToUpperInvariant() + (cost > 0 ? $"  ({cost}◆)" : "");
            _resultText.color = affordable ? UITheme.Accent2 : UITheme.Danger;
            if (_craftButton != null) _craftButton.interactable = affordable;
        }

        // 보유 재료에서 이미 슬롯에 놓은 개수를 뺀 나머지를 팔레트 버튼으로 표시
        private void RefreshPalette()
        {
            if (!_built || _paletteRow == null) return;
            // Destroy는 프레임 끝에 실행되므로, 한 프레임에 여러 번 갱신되면 옛 버튼이 남아 중복된다.
            // 부모에서 즉시 분리(childCount에서 바로 제외)한 뒤 파괴해 중복을 막는다.
            for (int i = _paletteRow.childCount - 1; i >= 0; i--)
            {
                var c = _paletteRow.GetChild(i);
                c.SetParent(null, false);
                Destroy(c.gameObject);
            }

            var owned = GameManager.Instance.Inventory.GetRawMaterials();
            var placed = new Dictionary<AlphabetMaterial, int>();
            foreach (var m in _bench.PlacedMaterials())
            {
                placed.TryGetValue(m, out int c);
                placed[m] = c + 1;
            }

            bool selectedStillAvailable = false;
            foreach (var kv in owned)
            {
                placed.TryGetValue(kv.Key, out int used);
                int avail = kv.Value - used;
                if (avail <= 0) continue;

                var mat = kv.Key;
                var (btn, label) = UITheme.Button(_paletteRow, mat + "×" + avail, 16);
                if (_selectedMaterial.HasValue && _selectedMaterial.Value == mat)
                {
                    selectedStillAvailable = true;
                    UITheme.MakePrimary(btn, label); // 선택 강조(노랑)
                }
                btn.onClick.AddListener(() => OnMaterialClicked(mat));
            }

            // 선택한 글자를 더 이상 놓을 수 없으면 선택 해제
            if (_selectedMaterial.HasValue && !selectedStillAvailable) _selectedMaterial = null;
        }

        // ---------- 런타임 UI 구성(1회) ----------

        private void EnsureBuilt()
        {
            if (_built) return;
            _built = true;

            var frame = UITheme.BuildPanelFrame(_panel);
            UITheme.BuildHeader(frame, "크래프팅", Toggle);
            var body = UITheme.BuildBody(frame);

            UITheme.Label(body, "Title", "글자를 놓아 단어를 만드세요", 15, UITheme.Dim);

            var slotRow = UITheme.Horizontal(body, "SlotRow");
            for (int i = 0; i < _slotCount; i++)
            {
                int idx = i;
                var (btn, label) = UITheme.Button(slotRow, "_", 22);
                var le = btn.GetComponent<LayoutElement>();
                le.minWidth = 46; le.preferredWidth = 46; le.minHeight = 52;
                _slotFills.Add(UITheme.AsSwatch(btn)); // 슬롯은 상태색을 직접 칠함(hover 없음)
                btn.onClick.AddListener(() => OnSlotClicked(idx));
                _slotLabels.Add(label);
            }

            _resultText = UITheme.Label(body, "Result", "———", 24, UITheme.Accent2);

            var (craftBtn, craftLabel) = UITheme.Button(body, "제작", 18);
            UITheme.MakePrimary(craftBtn, craftLabel);
            craftBtn.GetComponent<LayoutElement>().minHeight = 48;
            craftBtn.onClick.AddListener(OnCraftClicked);
            _craftButton = craftBtn;

            UITheme.Label(body, "PaletteTitle", "보유 글자 (클릭해 슬롯에 놓기)", 13, UITheme.Dim);
            _paletteRow = UITheme.Horizontal(body, "PaletteRow", 48f);
        }
    }
}

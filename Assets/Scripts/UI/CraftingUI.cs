using UnityEngine;
using UnityEngine.UI;
using Help.Core;
using Help.Crafting;
using Help.Inventory;
using Help.Player;

namespace Help.UI
{
    public class CraftingUI : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private Transform _recipeListParent;
        [SerializeField] private GameObject _recipeSlotPrefab;

        private PlayerController _player;

        private void Start()
        {
            _panel.SetActive(false);
            GameManager.Instance.OnInventoryChanged += RefreshCraftability;
            GameManager.Instance.OnRunReset += CloseOnRunReset; // 사망 시 패널 닫아 부활 후 입력 잠금 잔존 방지

            // C 키 입력(PlayerController)을 받아 패널 토글
            _player = FindFirstObjectByType<PlayerController>();
            if (_player != null) _player.CraftingToggleRequested += Toggle;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnInventoryChanged -= RefreshCraftability;
                GameManager.Instance.OnRunReset -= CloseOnRunReset;
            }
            if (_player != null) _player.CraftingToggleRequested -= Toggle;
        }

        // 런 리셋(사망) 시 열려 있던 패널을 닫는다(입력 게이트도 함께 해제됨).
        private void CloseOnRunReset()
        {
            if (_panel.activeSelf) Toggle();
        }

        public void Toggle()
        {
            _panel.SetActive(!_panel.activeSelf);
            if (_panel.activeSelf) BuildRecipeList();
            // 패널 오픈 중 게임플레이 입력 차단을 위해 상태를 알린다
            if (_player != null) _player.SetUiPanelOpen(this, _panel.activeSelf);
        }

        private void BuildRecipeList()
        {
            foreach (Transform child in _recipeListParent)
                Destroy(child.gameObject);

            // 씬에 브릿지가 있으면 사용, 없으면 GameManager가 보유한 DB로 폴백
            var db = FindFirstObjectByType<RecipeDatabaseBridge>()?.Database
                     ?? GameManager.Instance.RecipeDatabase;
            if (db == null) return;

            var inv = GameManager.Instance.Inventory;
            // 실제 Craft는 보유 재료만 소모하므로, 버튼 판정도 보유 재료 기준으로 일치시킨다
            // (분해 잠재 재료까지 포함하는 GetTotalPool은 진입 판정 전용)
            var pool = inv.GetRawMaterials();

            foreach (var item in db.AllItems)
            {
                // 기본 제작 대상(E 포함 단어)만 목록에 띄운다 — 눌러도 안 되는 죽은 버튼 방지
                if (!Help.Item.AlphabetWordRule.IsBasicCraftable(item)) continue;

                var go = Instantiate(_recipeSlotPrefab, _recipeListParent);

                var nameText = go.transform.Find("Name")?.GetComponent<Text>();
                if (nameText != null) nameText.text = item.Word;

                bool canCraft = GameManager.Instance.Crafting.CanCraft(item.Id, pool);

                var craftBtn = go.transform.Find("CraftButton")?.GetComponent<Button>();
                if (craftBtn != null)
                {
                    craftBtn.interactable = canCraft;
                    string id = item.Id;
                    craftBtn.onClick.AddListener(() =>
                    {
                        GameManager.Instance.Crafting.Craft(id, GameManager.Instance.Inventory);
                    });
                }

                // 재료 목록 표시
                var reqText = go.transform.Find("Requirements")?.GetComponent<Text>();
                if (reqText != null)
                {
                    var sb = new System.Text.StringBuilder();
                    foreach (var req in item.Recipe)
                        sb.Append($"{req.Material}×{req.Count} ");
                    reqText.text = sb.ToString().TrimEnd();
                }
            }
        }

        private void RefreshCraftability()
        {
            if (!_panel.activeSelf) return;
            BuildRecipeList();
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using Help.Player;

namespace Help.UI
{
    public class HUD : MonoBehaviour
    {
        [SerializeField] private Slider _hpBar;   // 레거시(중앙 정사각형) — HpBarUI로 대체하며 숨긴다
        [SerializeField] private Text _hpText;
        [SerializeField] private Text _weaponName;
        [SerializeField] private Button _inventoryButton;

        // 인벤토리 패널 컴포넌트(InventoryUI 또는 InventoryGridUI). 구현 무관하게 Toggle()을 호출한다.
        [SerializeField] private MonoBehaviour _inventoryUI;

        private PlayerController _player;

        private void Start()
        {
            _player = FindFirstObjectByType<PlayerController>();

            // 잘못 배치돼 있던 레거시 체력 Slider(화면 중앙 100×100)를 숨기고 HpBarUI로 대체
            if (_hpBar != null) _hpBar.gameObject.SetActive(false);
            EnsureHpBar();
            EnsureCanvasComponent<GoldUI>();
            EnsureCanvasComponent<BossRewardUI>();
            EnsureCanvasComponent<VictoryUI>();
            EnsureCanvasComponent<MinimapUI>();

            // 구현 무관하게 Toggle 호출(InventoryUI/InventoryGridUI 공통 메서드명)
            if (_inventoryUI != null && _inventoryButton != null)
                _inventoryButton.onClick.AddListener(() =>
                    _inventoryUI.SendMessage("Toggle", SendMessageOptions.DontRequireReceiver));

            if (_weaponName != null)
            {
                _weaponName.color = UITheme.Dim;
                _weaponName.fontStyle = FontStyle.Bold;
                AddOutline(_weaponName.gameObject, Color.black, 1.4f);
            }
        }

        // 좌상단 체력바(HpBarUI)가 없으면 Canvas에 추가한다.
        private void EnsureHpBar() => EnsureCanvasComponent<HpBarUI>();

        // 지정 UI 컴포넌트가 씬에 없으면 Canvas에 부착한다(런타임 자체 구성 UI 공통).
        private void EnsureCanvasComponent<T>() where T : MonoBehaviour
        {
            if (FindFirstObjectByType<T>() != null) return;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null) canvas.gameObject.AddComponent<T>();
        }

        private static void AddOutline(GameObject go, Color color, float dist)
        {
            var o = go.GetComponent<Outline>();
            if (o == null) o = go.AddComponent<Outline>();
            o.effectColor = color;
            o.effectDistance = new Vector2(dist, -dist);
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using Help.Core;
using Help.Player;

namespace Help.UI
{
    // 최종 층까지 클리어(GameManager.OnGameCleared) 시 승리 화면을 띄운다. 재시작 버튼으로 새 런 시작.
    // 화면은 런타임 자체 구성(UITheme). HUD가 Canvas에 부착.
    public class VictoryUI : MonoBehaviour
    {
        private GameObject _panel;
        private PlayerController _player;
        private bool _built;

        private void Start()
        {
            _player = FindFirstObjectByType<PlayerController>();
            var gm = GameManager.Instance;
            if (gm != null) gm.OnGameCleared += Show;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnGameCleared -= Show;
        }

        private void Show()
        {
            EnsureBuilt();
            _panel.SetActive(true);
            if (_player != null) _player.SetUiPanelOpen(this, true);
        }

        private void Restart()
        {
            _panel.SetActive(false);
            if (_player != null) _player.SetUiPanelOpen(this, false);
            GameManager.Instance?.RestartRun();
        }

        private void EnsureBuilt()
        {
            if (_built) return;
            _built = true;

            _panel = new GameObject("VictoryPanel", typeof(RectTransform));
            _panel.transform.SetParent(transform, false);
            var prt = _panel.GetComponent<RectTransform>();
            prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = Vector2.zero;
            prt.sizeDelta = new Vector2(520f, 300f);

            var frame = UITheme.BuildPanelFrame(_panel);
            UITheme.BuildHeader(frame, "던전 클리어!", null);
            var body = UITheme.BuildBody(frame);
            UITheme.Label(body, "Msg", "모든 층을 정복했습니다.", 18, UITheme.Accent);
            UITheme.Label(body, "Sub", "축하합니다!", 15, UITheme.Dim);

            var (btn, label) = UITheme.Button(body, "다시 시작", 18);
            UITheme.MakePrimary(btn, label);
            btn.GetComponent<LayoutElement>().minHeight = 52;
            btn.onClick.AddListener(Restart);

            _panel.SetActive(false);
        }
    }
}

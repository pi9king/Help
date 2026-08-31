using UnityEngine;
using UnityEngine.UI;
using Help.Core;

namespace Help.UI
{
    // 좌상단 체력바 아래 골드 카운터를 런타임 구성한다(HpBarUI와 같은 자체 구성 패턴).
    // 골드는 인벤토리 슬롯을 차지하지 않는 재화라 항상 화면에 떠 있어야 한다.
    public class GoldUI : MonoBehaviour
    {
        private const float Width = 150f;
        private const float Height = 26f;
        private const float BorderPx = 3f;

        private Wallet _wallet;
        private Text _text;

        private void Start()
        {
            Build();

            var gm = GameManager.Instance;
            if (gm != null)
            {
                _wallet = gm.Wallet;
                _wallet.OnChanged += Refresh;
            }
            Refresh();
        }

        private void OnDestroy()
        {
            if (_wallet != null) _wallet.OnChanged -= Refresh;
        }

        private void Refresh()
        {
            if (_text == null) return;
            _text.text = "◆ " + (_wallet != null ? _wallet.Gold : 0);
        }

        private void Build()
        {
            var frame = new GameObject("Frame", typeof(RectTransform)).GetComponent<RectTransform>();
            frame.SetParent(transform, false);
            frame.anchorMin = new Vector2(0f, 1f);
            frame.anchorMax = new Vector2(0f, 1f);
            frame.pivot = new Vector2(0f, 1f);
            frame.anchoredPosition = new Vector2(24f, -64f); // 체력바(높이 34, y -24) 바로 아래
            frame.sizeDelta = new Vector2(Width, Height);
            frame.gameObject.AddComponent<Image>().color = UITheme.Border;

            var body = new GameObject("Body", typeof(RectTransform)).GetComponent<RectTransform>();
            body.SetParent(frame, false);
            body.anchorMin = Vector2.zero;
            body.anchorMax = Vector2.one;
            body.offsetMin = new Vector2(BorderPx, BorderPx);
            body.offsetMax = new Vector2(-BorderPx, -BorderPx);
            body.gameObject.AddComponent<Image>().color = UITheme.Slot;

            var textRT = new GameObject("GoldText", typeof(RectTransform)).GetComponent<RectTransform>();
            textRT.SetParent(body, false);
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            _text = textRT.gameObject.AddComponent<Text>();
            _text.font = UITheme.Font;
            _text.fontSize = 16;
            _text.fontStyle = FontStyle.Bold;
            _text.alignment = TextAnchor.MiddleCenter;
            _text.color = UITheme.Accent; // 노랑 = 재화
            _text.horizontalOverflow = HorizontalWrapMode.Overflow;
        }
    }
}

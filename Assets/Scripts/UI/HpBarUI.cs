using UnityEngine;
using UnityEngine.UI;
using Help.Player;

namespace Help.UI
{
    // 화면 좌상단에 테마 일관 체력바를 런타임 구성한다(프레임+어두운 트랙+비율색 채움+데미지 트레일+수치).
    // 씬의 잘못 배치된 Slider를 대체. 플레이어 HP 이벤트에 직접 구독한다.
    // 채움은 스프라이트 없이 anchorMax.x로 폭을 조절(레트로 사각 룩 + 의존성 최소).
    public class HpBarUI : MonoBehaviour
    {
        private const float Width = 380f;
        private const float Height = 34f;
        private const float BorderPx = 3f;
        private const float TrailSpeed = 0.55f; // 데미지 트레일이 줄어드는 속도(비율/초)

        private PlayerController _player;
        private RectTransform _fillRT;
        private RectTransform _trailRT;
        private Image _fill;
        private Text _text;
        private float _fillRatio = 1f;
        private float _trailRatio = 1f;

        private void Start()
        {
            Build();
            _player = FindFirstObjectByType<PlayerController>();
            if (_player != null)
            {
                _player.Stats.OnHpChanged += OnHpChanged;
                OnHpChanged(_player.Stats.CurrentHp, _player.Stats.MaxHp);
            }
        }

        private void OnDestroy()
        {
            if (_player != null) _player.Stats.OnHpChanged -= OnHpChanged;
        }

        private void OnHpChanged(int current, int max)
        {
            int m = Mathf.Max(1, max);
            _fillRatio = Mathf.Clamp01((float)current / m);
            SetWidth(_fillRT, _fillRatio);
            if (_fill != null) _fill.color = HpColor(_fillRatio);
            if (_text != null) _text.text = "HP  " + current + " / " + max;

            // 회복이면 트레일이 즉시 따라 올라오고, 피해면 뒤에 남아 서서히 줄어든다(Update).
            if (_fillRatio >= _trailRatio)
            {
                _trailRatio = _fillRatio;
                SetWidth(_trailRT, _trailRatio);
            }
        }

        private void Update()
        {
            if (_trailRatio > _fillRatio)
            {
                _trailRatio = Mathf.MoveTowards(_trailRatio, _fillRatio, TrailSpeed * Time.deltaTime);
                SetWidth(_trailRT, _trailRatio);
            }
        }

        private static void SetWidth(RectTransform rt, float ratio)
        {
            if (rt == null) return;
            rt.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
        }

        private static Color HpColor(float r)
        {
            if (r > 0.5f) return UITheme.Accent2; // 시안(양호)
            if (r > 0.25f) return UITheme.Accent;  // 노랑(주의)
            return UITheme.Danger;                  // 빨강(위험)
        }

        // ---------- 구성 ----------

        private void Build()
        {
            // 프레임(흰 테두리) — 좌상단 고정
            var frame = MakeChild(transform, "Frame");
            frame.anchorMin = new Vector2(0f, 1f);
            frame.anchorMax = new Vector2(0f, 1f);
            frame.pivot = new Vector2(0f, 1f);
            frame.anchoredPosition = new Vector2(24f, -24f);
            frame.sizeDelta = new Vector2(Width, Height);
            frame.gameObject.AddComponent<Image>().color = UITheme.Border;

            // 트랙(어두운 배경) — 테두리만큼 안쪽
            var track = MakeStretch(frame, "Track", BorderPx);
            track.gameObject.AddComponent<Image>().color = UITheme.Slot;

            // 데미지 트레일(빨강, 채움 뒤) — 왼쪽 정렬로 폭 스케일
            _trailRT = MakeLeftFill(track, "Trail", UITheme.Danger, out _);

            // 채움(비율색, 트레일 위)
            _fillRT = MakeLeftFill(track, "Fill", UITheme.Accent2, out _fill);

            // 수치 텍스트(외곽선으로 바 위에서도 읽힘)
            var textRT = MakeStretch(frame, "HpText", 0f);
            _text = textRT.gameObject.AddComponent<Text>();
            _text.font = UITheme.Font;
            _text.fontSize = 17;
            _text.fontStyle = FontStyle.Bold;
            _text.alignment = TextAnchor.MiddleCenter;
            _text.color = UITheme.Text;
            _text.horizontalOverflow = HorizontalWrapMode.Overflow;
            var outline = textRT.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        private static RectTransform MakeChild(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        // 부모에 꽉 채우되 inset(px)만큼 안쪽
        private static RectTransform MakeStretch(Transform parent, string name, float inset)
        {
            var rt = MakeChild(parent, name);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        // 왼쪽 정렬 폭-스케일 채움 이미지(anchorMax.x로 폭 조절)
        private static RectTransform MakeLeftFill(Transform parent, string name, Color color, out Image img)
        {
            var rt = MakeChild(parent, name);
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return rt;
        }
    }
}

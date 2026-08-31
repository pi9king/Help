using UnityEngine;
using UnityEngine.UI;

namespace Help.UI
{
    // 게임 UI 공용 테마 + 위젯 팩토리 (레트로 픽셀 아케이드 톤).
    // 색/폰트/여백/테두리를 한곳에 모아, 값 하나로 전체 톤을 바꿀 수 있게 한다.
    // CraftingBenchUI/InventoryGridUI/HUD가 공유해 런타임에 화면을 구성한다(프리팹 의존 X).
    //
    // 룩: 거의 검은 패널 + 굵은 흰 테두리 + 하드(블러 없는) 그림자 + 대문자 라벨.
    // 액센트=노랑(선택/제작/장착), 보조=시안(결과/장착 표시), 위험=빨강(해제/분해).
    public static class UITheme
    {
        // ---- 팔레트 ----
        public static readonly Color Panel       = Hex(0x101820); // 패널 배경(거의 검정)
        public static readonly Color Header      = Hex(0x182634); // 헤더 바
        public static readonly Color Text        = Hex(0xFFFFFF);
        public static readonly Color Dim         = Hex(0x7FA0B8); // 보조 텍스트
        public static readonly Color ButtonBg    = Hex(0x283848);
        public static readonly Color ButtonHover = Hex(0x3A506E);
        public static readonly Color ButtonPress = Hex(0x1E2C3C);
        public static readonly Color Accent      = Hex(0xFFCC00); // 노랑
        public static readonly Color AccentHi    = Hex(0xFFDE4D);
        public static readonly Color AccentPress = Hex(0xE0B000);
        public static readonly Color Accent2     = Hex(0x00E5FF); // 시안
        public static readonly Color Danger      = Hex(0xFF3B3B);
        public static readonly Color DangerHi    = Hex(0xFF6B6B);
        public static readonly Color DangerPress = Hex(0xD02020);
        public static readonly Color Slot        = Hex(0x0A1119); // 빈 슬롯
        public static readonly Color SlotFill    = Hex(0x1E3350); // 채워진 슬롯
        public static readonly Color SlotHint    = Hex(0x2E4A2E); // 배치 가능 힌트
        public static readonly Color Border       = Hex(0xFFFFFF);
        public static readonly Color Shadow       = new Color(0f, 0f, 0f, 0.5f);

        public const int BorderThickness = 3;

        static Font _font;
        public static Font Font =>
            _font != null ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        static Color Hex(int rgb) => new Color(
            ((rgb >> 16) & 0xFF) / 255f,
            ((rgb >> 8) & 0xFF) / 255f,
            (rgb & 0xFF) / 255f, 1f);

        // ---- 패널 프레임: 흰 테두리 + 하드 그림자 + 내부 배경. 컨텐츠를 담을 세로 프레임 반환 ----
        // 반환된 프레임에 BuildHeader → BuildBody 순으로 붙이면 된다.
        public static RectTransform BuildPanelFrame(GameObject panel)
        {
            var border = panel.GetComponent<Image>();
            if (border == null) border = panel.AddComponent<Image>();
            border.color = Border;

            var shadow = panel.GetComponent<Shadow>();
            if (shadow == null) shadow = panel.AddComponent<Shadow>();
            shadow.effectColor = Shadow;
            shadow.effectDistance = new Vector2(6f, -6f);

            var frameGo = new GameObject("Frame", typeof(RectTransform));
            frameGo.transform.SetParent(panel.transform, false);
            frameGo.AddComponent<Image>().color = Panel;
            var frt = frameGo.GetComponent<RectTransform>();
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
            frt.offsetMin = new Vector2(BorderThickness, BorderThickness);
            frt.offsetMax = new Vector2(-BorderThickness, -BorderThickness);

            var v = frameGo.AddComponent<VerticalLayoutGroup>();
            v.spacing = 0;
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            return frt;
        }

        // ---- 헤더 바: 제목(좌) + 닫기 ✕(우, onClose 있을 때만) ----
        public static void BuildHeader(RectTransform frame, string title, UnityEngine.Events.UnityAction onClose)
        {
            var bar = new GameObject("Header", typeof(RectTransform));
            bar.transform.SetParent(frame, false);
            bar.AddComponent<Image>().color = Header;
            var h = bar.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(14, 8, 6, 6);
            h.spacing = 6;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = true;
            bar.AddComponent<LayoutElement>().minHeight = 42;

            var titleT = Label(bar.transform, "Title", title.ToUpperInvariant(), 16, Accent);
            titleT.alignment = TextAnchor.MiddleLeft;
            var titleLe = titleT.GetComponent<LayoutElement>();
            titleLe.flexibleWidth = 1;

            if (onClose != null)
            {
                var (x, xlabel) = Button(bar.transform, "✕", 16);
                var xle = x.GetComponent<LayoutElement>();
                xle.minWidth = 34; xle.preferredWidth = 34; xle.minHeight = 30;
                SetColors(x, Danger, DangerHi, DangerPress, xlabel, Text);
                x.onClick.AddListener(onClose);
            }
        }

        // ---- 본문 컨테이너(세로, 패딩) ----
        public static RectTransform BuildBody(RectTransform frame)
        {
            var go = new GameObject("Body", typeof(RectTransform));
            go.transform.SetParent(frame, false);
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = 10;
            v.padding = new RectOffset(16, 16, 16, 16);
            v.childAlignment = TextAnchor.UpperCenter;
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            go.AddComponent<LayoutElement>().flexibleHeight = 1;
            return go.GetComponent<RectTransform>();
        }

        public static RectTransform Horizontal(Transform parent, string name, float minHeight = 52f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 6;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            go.AddComponent<LayoutElement>().minHeight = minHeight;
            return go.GetComponent<RectTransform>();
        }

        public static Text Label(Transform parent, string name, string text, int size, Color? color = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = Font; t.fontSize = size; t.color = color ?? Text;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter; t.text = text;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            go.AddComponent<LayoutElement>().minHeight = size + 10;
            return t;
        }

        // 흰 테두리 + 하드 그림자 + hover(ColorBlock)를 갖춘 버튼. (Button, 라벨 Text) 반환.
        // 채움(Fill) 이미지는 흰색으로 두고 색은 ColorBlock으로 구동한다(hover/press 무료).
        public static (Button, Text) Button(Transform parent, string label, int size)
        {
            var outer = new GameObject("Btn_" + label, typeof(RectTransform));
            outer.transform.SetParent(parent, false);
            outer.AddComponent<Image>().color = Border; // 바깥 = 테두리
            var le = outer.AddComponent<LayoutElement>();
            le.minWidth = 52; le.preferredWidth = 60; le.minHeight = 46;
            var sh = outer.AddComponent<Shadow>();
            sh.effectColor = Shadow; sh.effectDistance = new Vector2(4f, -4f);

            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(outer.transform, false);
            var fill = fillGo.AddComponent<Image>();
            fill.color = Color.white; // ColorBlock이 곱해 실제 색을 낸다
            var frt = fillGo.GetComponent<RectTransform>();
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
            frt.offsetMin = new Vector2(BorderThickness, BorderThickness);
            frt.offsetMax = new Vector2(-BorderThickness, -BorderThickness);

            var btn = outer.AddComponent<Button>();
            btn.targetGraphic = fill;

            var txtGo = new GameObject("Text", typeof(RectTransform));
            txtGo.transform.SetParent(fillGo.transform, false);
            var t = txtGo.AddComponent<Text>();
            t.font = Font; t.fontSize = size; t.color = Text; t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter; t.text = label;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            var trt = txtGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;

            SetColors(btn, ButtonBg, ButtonHover, ButtonPress); // 기본
            return (btn, t);
        }

        // 버튼의 상태별 색을 지정한다. label/labelColor를 주면 라벨 색도 바꾼다.
        public static void SetColors(Button btn, Color normal, Color highlight, Color pressed,
                                     Text label = null, Color? labelColor = null)
        {
            var cb = btn.colors;
            cb.normalColor = normal;
            cb.highlightedColor = highlight;
            cb.pressedColor = pressed;
            cb.selectedColor = normal;
            cb.disabledColor = new Color(normal.r, normal.g, normal.b, 0.35f);
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.06f;
            btn.colors = cb;
            if (label != null && labelColor.HasValue) label.color = labelColor.Value;
        }

        // 주(主) 액션 버튼: 노랑 채움 + 어두운 라벨.
        public static void MakePrimary(Button btn, Text label)
            => SetColors(btn, Accent, AccentHi, AccentPress, label, Panel);

        // 위험 버튼(해제/분해): 빨강 채움.
        public static void MakeDanger(Button btn, Text label)
            => SetColors(btn, Danger, DangerHi, DangerPress, label, Text);

        // 슬롯처럼 hover 없이 색을 직접 칠하는 용도: transition 끄고 Fill 이미지 반환.
        public static Image AsSwatch(Button btn)
        {
            btn.transition = Selectable.Transition.None;
            return btn.targetGraphic as Image;
        }
    }
}

using UnityEngine;

namespace Help.Combat
{
    // 공격 시 앞쪽에 슬래시 호(arc)를 그렸다 사라지게 한다.
    // 초승달 스프라이트를 런타임에 절차적으로 생성(에셋/씬 배선 불필요).
    // 플레이어 자식으로 두면 부모의 localScale.x 뒤집힘이 좌우 방향을 자동 처리한다.
    [RequireComponent(typeof(SpriteRenderer))]
    public class SlashVFX : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private float _timer;
        private float _duration;
        private float _fromDeg, _toDeg;

        private static Sprite _sharedSprite; // 초승달 스프라이트(한 번만 생성해 공유)

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _sr.sprite = GetSprite();
            _sr.sortingOrder = 20; // 캐릭터 위
            _sr.enabled = false;
        }

        // reach=전방 거리, from/to=스윙 각도, color/scale=외형, duration=스윙+페이드 총 시간
        public void Play(float reach, float fromDeg, float toDeg, Color color, float scale, float duration)
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            transform.localPosition = new Vector3(reach, 0f, 0f);
            transform.localScale = Vector3.one * scale;
            _sr.color = color;
            _fromDeg = fromDeg;
            _toDeg = toDeg;
            _duration = Mathf.Max(0.01f, duration);
            _timer = 0f;
            _sr.enabled = true;
            transform.localRotation = Quaternion.Euler(0f, 0f, fromDeg);
        }

        private void Update()
        {
            if (!_sr.enabled) return;
            _timer += Time.deltaTime;
            float t = Mathf.Clamp01(_timer / _duration);

            // 각도 스윕
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(_fromDeg, _toDeg, t));

            // 뒤쪽 40%에서 페이드 아웃
            var c = _sr.color;
            c.a = t < 0.6f ? _sr.color.a : Mathf.Lerp(_sr.color.a, 0f, (t - 0.6f) / 0.4f);
            _sr.color = c;

            if (t >= 1f) _sr.enabled = false;
        }

        // ---- 초승달 스프라이트 절차 생성 ----
        private static Sprite GetSprite()
        {
            if (_sharedSprite != null) return _sharedSprite;

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var clear = new Color(1f, 1f, 1f, 0f);
            var px = new Color[size * size];
            for (int i = 0; i < px.Length; i++) px[i] = clear;

            float cx = size * 0.5f, cy = size * 0.5f;
            float outer = size * 0.46f, inner = size * 0.30f;
            float halfArc = 60f * Mathf.Deg2Rad; // 개구부: +x 축 기준 ±60°

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    if (r < inner || r > outer) continue;
                    float ang = Mathf.Atan2(dy, dx);           // -π..π, 0 = +x
                    if (Mathf.Abs(ang) > halfArc) continue;
                    // 가장자리(반지름/각도)로 갈수록 부드럽게 페이드
                    float rEdge = 1f - Mathf.Abs((r - (inner + outer) * 0.5f) / ((outer - inner) * 0.5f));
                    float aEdge = 1f - Mathf.Abs(ang) / halfArc;
                    float a = Mathf.Clamp01(rEdge * 1.4f) * Mathf.Clamp01(aEdge * 1.6f);
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(px);
            tex.Apply();

            _sharedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return _sharedSprite;
        }
    }
}

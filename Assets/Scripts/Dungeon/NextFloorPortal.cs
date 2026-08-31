using UnityEngine;
using Help.Core;

namespace Help.Dungeon
{
    // 보스 방 클리어(+보상 수령) 후 등장하는 다음 층 포탈. 플레이어가 닿으면 다음 층으로 진행한다.
    // 런타임 생성(static Spawn) — 프리팹 불필요. 시각은 절차 생성한 시안 원 + 부드러운 펄스/회전.
    [RequireComponent(typeof(Collider2D))]
    public class NextFloorPortal : MonoBehaviour
    {
        private bool _used;
        private SpriteRenderer _sr;

        public static NextFloorPortal Spawn(Vector3 pos, Transform parent = null)
        {
            var go = new GameObject("NextFloorPortal");
            go.transform.position = pos;
            if (parent != null) go.transform.SetParent(parent, true);

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.6f, 2.2f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = RingSprite();
            sr.color = new Color(0.2f, 0.9f, 1f, 0.9f);
            sr.sortingOrder = 14;

            var p = go.AddComponent<NextFloorPortal>();
            p._sr = sr;
            go.transform.localScale = Vector3.one * 1.6f;
            return p;
        }

        private void Update()
        {
            // 부드러운 펄스 + 회전으로 "포탈"임을 알림
            transform.Rotate(0f, 0f, 60f * Time.deltaTime);
            if (_sr != null)
            {
                float a = 0.7f + 0.25f * Mathf.Sin(Time.time * 4f);
                var c = _sr.color; c.a = a; _sr.color = c;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_used || !other.CompareTag("Player")) return;
            _used = true;
            GameManager.Instance?.AdvanceFloor();
        }

        private static Sprite _ring;
        private static Sprite RingSprite()
        {
            if (_ring != null) return _ring;
            const int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            float c = (s - 1) * 0.5f, outer = s * 0.46f, inner = s * 0.28f;
            var px = new Color[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    float a = 0f;
                    if (d <= outer && d >= inner)
                    {
                        float mid = (outer + inner) * 0.5f;
                        a = 1f - Mathf.Abs(d - mid) / ((outer - inner) * 0.5f);
                    }
                    px[y * s + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(a));
                }
            tex.SetPixels(px);
            tex.Apply();
            _ring = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _ring;
        }
    }
}

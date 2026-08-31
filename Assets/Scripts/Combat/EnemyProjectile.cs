using UnityEngine;
using Help.Player;

namespace Help.Combat
{
    // 적(아처)이 발사하는 투사체. 등속 이동, 플레이어에 닿으면 데미지(피격 연출은 PlayerController가 처리),
    // 벽/바닥(Ground)에 막히면 소멸, 수명 초과 시 소멸. 런타임 생성(프리팹 불필요).
    [RequireComponent(typeof(Collider2D))]
    public class EnemyProjectile : MonoBehaviour
    {
        private Vector2 _velocity;
        private int _damage;
        private float _life;
        private int _groundLayer;

        public void Init(Vector2 velocity, int damage, float lifetime)
        {
            _velocity = velocity;
            _damage = damage;
            _life = lifetime;
            _groundLayer = LayerMask.NameToLayer("Ground");
            GetComponent<Collider2D>().isTrigger = true;
            if (velocity.sqrMagnitude > 0.001f)
                transform.right = velocity.normalized; // 진행 방향으로 회전
        }

        private void Update()
        {
            transform.position += (Vector3)(_velocity * Time.deltaTime);
            _life -= Time.deltaTime;
            if (_life <= 0f) Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                var pc = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
                if (pc != null) pc.TakeDamage(_damage, transform.position);
                Destroy(gameObject);
                return;
            }
            // 벽/바닥에 막히면 소멸(트리거가 아닌 Ground 콜라이더)
            if (!other.isTrigger && other.gameObject.layer == _groundLayer)
                Destroy(gameObject);
        }

        public static EnemyProjectile Spawn(Vector3 pos, Vector2 velocity, int damage, float lifetime, Color color, Transform parent = null)
        {
            var go = new GameObject("EnemyProjectile");
            go.transform.position = pos;
            if (parent != null) go.transform.SetParent(parent, true);

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.4f, 0.4f);

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = DotSprite();
            sr.color = color;
            sr.sortingOrder = 15;

            var p = go.AddComponent<EnemyProjectile>();
            p.Init(velocity, damage, lifetime);
            return p;
        }

        private static Sprite _dot;
        private static Sprite DotSprite()
        {
            if (_dot != null) return _dot;
            const int s = 16;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            float c = (s - 1) * 0.5f, rad = s * 0.45f;
            var px = new Color[s * s];
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    float a = Mathf.Clamp01((rad - d) * 0.9f);
                    px[y * s + x] = new Color(1f, 1f, 1f, a);
                }
            tex.SetPixels(px);
            tex.Apply();
            _dot = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
            return _dot;
        }
    }
}

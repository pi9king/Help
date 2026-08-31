using UnityEngine;
using Help.Core;

namespace Help.Item
{
    // 알파벳 외 보상(골드/포션)의 월드 드랍. MaterialPickup의 재화판 —
    // 팝 애니메이션·부모 귀속(방 캐시와 함께 보존) 규약을 그대로 따른다.
    [RequireComponent(typeof(Collider2D))]
    public class RewardPickup : MonoBehaviour
    {
        // 포션으로 지급할 아이템 Id (SetupGameAssets가 만드는 ELIXIR)
        public const string PotionItemId = "elixir";

        [SerializeField] private RewardKind _kind;
        [SerializeField] private int _amount = 1;
        [SerializeField] private float _popTime = 0.4f;

        private bool _collected;
        private bool _popping;
        private float _popTimer;
        private Vector3 _popFrom;
        private Vector3 _popTo;

        public static RewardPickup Spawn(RewardKind kind, int amount, Vector3 pos, Vector3 landOffset, Transform parent = null)
        {
            var go = new GameObject($"Reward_{kind}");
            go.transform.position = pos;
            if (parent != null) go.transform.SetParent(parent, true);

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.9f, 0.9f);

            // 팝 애니메이션 중에도 트리거가 잡히도록 키네매틱 RB 부착(MaterialPickup과 동일)
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            var pickup = go.AddComponent<RewardPickup>();
            pickup._kind = kind;
            pickup._amount = amount;

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var tm = textGo.AddComponent<TextMesh>();
            tm.text = kind == RewardKind.Gold ? $"{amount}G" : "＋";
            tm.characterSize = 0.14f;
            tm.fontSize = 64;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = kind == RewardKind.Gold ? new Color(1f, 0.84f, 0.2f) : new Color(0.5f, 1f, 0.6f);
            var mr = textGo.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 12;

            pickup.BeginPop(pos, pos + landOffset);
            return pickup;
        }

        private void BeginPop(Vector3 from, Vector3 to)
        {
            _popFrom = from;
            _popTo = to;
            _popTimer = 0f;
            _popping = true;
        }

        private void Update()
        {
            if (!_popping) return;
            _popTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_popTimer / _popTime);
            var p = Vector3.Lerp(_popFrom, _popTo, t);
            p.y += Mathf.Sin(t * Mathf.PI) * 0.6f;
            transform.position = p;
            if (t >= 1f) _popping = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_collected || !other.CompareTag("Player")) return;

            var gm = GameManager.Instance;
            if (gm == null) return;

            switch (_kind)
            {
                case RewardKind.Gold:
                    gm.Wallet.Add(_amount);
                    break;

                case RewardKind.Potion:
                    var def = gm.RecipeDatabase != null ? gm.RecipeDatabase.Find(PotionItemId) : null;
                    if (def == null) return; // 포션 에셋이 없으면 줍지 않고 남겨둔다(조용한 소실 방지)
                    gm.Inventory.Add(def, _amount);
                    break;
            }

            _collected = true;
            Destroy(gameObject);
        }
    }
}

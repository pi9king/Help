using UnityEngine;
using Help.Core;

namespace Help.Item
{
    // 방/월드에 놓인 알파벳 글자 줍기. 플레이어가 트리거에 닿으면 인벤토리에 재료 1개를 넣고 사라진다.
    // 방 콘텐츠(RoomContentLibrary)로 배치되거나, 적 처치 시 Spawn으로 월드에 튀어나온다.
    [RequireComponent(typeof(Collider2D))]
    public class MaterialPickup : MonoBehaviour
    {
        [SerializeField] private AlphabetMaterial _material;
        [SerializeField] private float _popTime = 0.4f;

        public AlphabetMaterial Material { get => _material; set => _material = value; }

        private bool _collected;
        private bool _popping;
        private float _popTimer;
        private Vector3 _popFrom;
        private Vector3 _popTo;

        // 적 드랍용 런타임 생성: 처치 위치에서 살짝 튀어나와(landOffset) 착지하는 글자 줍기.
        // parent를 주면 그 아래로 넣어(월드 좌표 유지) 방 콘텐츠와 함께 캐시/비활성화된다.
        public static MaterialPickup Spawn(AlphabetMaterial mat, Vector3 pos, Vector3 landOffset, Transform parent = null)
        {
            var go = new GameObject("Drop_" + mat);
            go.transform.position = pos;
            if (parent != null) go.transform.SetParent(parent, true);

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.9f, 0.9f);

            // 팝 애니메이션 중 트리거 이벤트가 확실히 잡히도록 키네매틱 RB 부착
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            var pickup = go.AddComponent<MaterialPickup>();
            pickup._material = mat;

            var textGo = new GameObject("Letter");
            textGo.transform.SetParent(go.transform, false);
            var tm = textGo.AddComponent<TextMesh>();
            tm.text = mat.ToString();
            tm.characterSize = 0.14f;
            tm.fontSize = 64;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.yellow;
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
            p.y += Mathf.Sin(t * Mathf.PI) * 0.6f; // 위로 살짝 솟았다 착지하는 호
            transform.position = p;
            if (t >= 1f) _popping = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_collected || !other.CompareTag("Player")) return;

            var gm = GameManager.Instance;
            if (gm == null || gm.RecipeDatabase == null) return;

            var def = gm.RecipeDatabase.FindMaterial(_material);
            if (def == null) return;

            gm.Inventory.Add(def, 1);
            _collected = true;
            Destroy(gameObject);
        }
    }
}

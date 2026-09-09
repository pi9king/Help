using UnityEngine;
using Help.Player;

namespace Help.Dungeon
{
    // 피해 바닥(^)의 실제 판정. 방을 그릴 때 RoomManager가 칸마다 만들어 붙인다.
    //
    // 지형 문자에 실제 효과가 없으면 템플릿 문법이 거짓말이 되고,
    // 도달성 검증이 "여기는 못 지나간다"고 한 곳을 플레이어가 걸어서 통과해 버린다.
    [RequireComponent(typeof(BoxCollider2D))]
    public class RoomHazard : MonoBehaviour
    {
        [SerializeField] private int _damage = 10;

        public static RoomHazard Create(Transform parent, Vector3 worldCenter, Vector2 size,
                                        int damage, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = worldCenter;

            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = size;

            var hazard = go.AddComponent<RoomHazard>();
            hazard._damage = damage;
            return hazard;
        }

        private void OnTriggerEnter2D(Collider2D other) => Hit(other);

        // 가시 위에 계속 서 있는 경우도 다뤄야 한다 — 실제 피해 빈도는
        // PlayerController의 무적시간(i-frame)이 조절한다.
        private void OnTriggerStay2D(Collider2D other) => Hit(other);

        private void Hit(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;

            // D-12: 피해만 준다. 예전엔 구덩이가 방 입구로 순간이동시켰는데,
            // 물리적으로 말이 안 되고 방을 평면적으로 만들었다 — 이제 빠지면 걸어 나온다.
            player.TakeDamage(_damage, transform.position);
        }
    }
}

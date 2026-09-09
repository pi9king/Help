using UnityEngine;
using Help.Dungeon;

namespace Help.Core
{
    // 방 경계 안에서만 플레이어를 따라가는 카메라.
    // - 방이 화면보다 작으면(Small) 방 중앙에 고정 — 퍼즐 방을 한눈에 보게 한다.
    // - 방이 크면(Wide/Tall) 따라가되 방 밖 빈 공간은 절대 비추지 않는다.
    // 실제 클램프 계산은 CameraBounds(순수 로직)가 담당한다.
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _smoothSpeed = 5f;
        [SerializeField] private Vector3 _offset = new Vector3(0, 2, -10);

        private Camera _cam;
        private Rect _roomBounds;
        private bool _hasBounds;
        private Vector3 _base;   // 흔들림을 뺀 "진짜" 카메라 위치

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            // 직교 크기는 코드가 소유한다 — 방 크기 규격(RoomDimensions)과 어긋나면
            // Small 방이 한 화면에 안 들어오거나 방 밖이 보인다.
            if (_cam != null && _cam.orthographic) _cam.orthographicSize = RoomDimensions.CameraOrthoSize;
            _base = transform.position;
        }

        // 방을 로드할 때 RoomManager가 호출한다. 방이 바뀌면 부드럽게 미끄러지지 않고 즉시 맞춘다.
        public void SetRoomBounds(Rect bounds)
        {
            _roomBounds = bounds;
            _hasBounds = true;
            _base = Resolve();
            transform.position = _base;
        }

        private void LateUpdate()
        {
            _base = Vector3.Lerp(_base, Resolve(), _smoothSpeed * Time.deltaTime);
            transform.position = _base + Help.Combat.CameraShake.Offset;
        }

        // 이번 프레임 카메라가 있어야 할 곳(흔들림 제외)
        private Vector3 Resolve()
        {
            Vector3 desired = _target != null
                ? _target.position + _offset
                : new Vector3(_base.x, _base.y, _offset.z);

            if (!_hasBounds) return desired;

            float halfH = _cam != null ? _cam.orthographicSize : RoomDimensions.CameraOrthoSize;
            float halfW = halfH * (_cam != null ? _cam.aspect : 16f / 9f);
            Vector2 clamped = CameraBounds.Clamp(desired, _roomBounds, halfW, halfH);
            return new Vector3(clamped.x, clamped.y, _offset.z);
        }
    }
}

using UnityEngine;

namespace Help.Combat
{
    // 스프라이트를 잠깐 다른 색으로 칠했다 되돌린다.
    // 피격 플래시(흰색)와 적 공격 예비동작 텔레그래프(경고색) 양쪽에 공용으로 쓴다.
    // 자신+자식의 모든 SpriteRenderer에 적용. 히트스톱(timeScale=0) 중에도 보이도록 unscaled 시간 사용.
    public class HitFlash : MonoBehaviour
    {
        [SerializeField] private Color _flashColor = Color.white;
        [SerializeField] private float _flashDuration = 0.1f;

        private SpriteRenderer[] _renderers;
        private Color[] _original;
        private float _timer;
        private bool _flashing;

        private void Awake() => EnsureInit();

        private void EnsureInit()
        {
            if (_renderers != null) return;
            _renderers = GetComponentsInChildren<SpriteRenderer>(true);
            _original = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
                _original[i] = _renderers[i].color;
        }

        public void Flash() => Tint(_flashColor, _flashDuration);

        public void Tint(Color color, float duration)
        {
            EnsureInit();
            if (_renderers.Length == 0) return;
            if (!_flashing) CaptureOriginal(); // 텔레그래프→플래시 연속 시 원색 보존
            for (int i = 0; i < _renderers.Length; i++)
                if (_renderers[i] != null) _renderers[i].color = color;
            _timer = duration;
            _flashing = true;
        }

        private void CaptureOriginal()
        {
            for (int i = 0; i < _renderers.Length; i++)
                if (_renderers[i] != null) _original[i] = _renderers[i].color;
        }

        private void Update()
        {
            if (!_flashing) return;
            _timer -= Time.unscaledDeltaTime;
            if (_timer <= 0f) Restore();
        }

        private void Restore()
        {
            _flashing = false;
            for (int i = 0; i < _renderers.Length; i++)
                if (_renderers[i] != null) _renderers[i].color = _original[i];
        }

        private void OnDisable()
        {
            if (_flashing) Restore();
        }
    }
}

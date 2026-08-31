using UnityEngine;

namespace Help.Combat
{
    // 카메라를 짧게 흔든다(타격/피격 강조). 씬 배선 없이 Camera.main에 자동 부착된다.
    // 히트스톱(timeScale=0) 중에도 동작하도록 unscaled 시간 사용.
    public class CameraShake : MonoBehaviour
    {
        private static CameraShake _instance;

        private float _timer;
        private float _duration;
        private float _intensity;
        private Vector3 _baseLocalPos;

        // 정적 진입점 — 필요 시 메인 카메라에 컴포넌트를 붙여 흔든다.
        public static void ShakeMain(float intensity, float duration)
        {
            var inst = Ensure();
            if (inst != null) inst.Begin(intensity, duration);
        }

        private static CameraShake Ensure()
        {
            if (_instance != null) return _instance;
            var cam = Camera.main;
            if (cam == null) return null;
            _instance = cam.GetComponent<CameraShake>();
            if (_instance == null) _instance = cam.gameObject.AddComponent<CameraShake>();
            return _instance;
        }

        private void Awake() => _instance = this;

        private void Begin(float intensity, float duration)
        {
            if (_timer <= 0f) _baseLocalPos = transform.localPosition; // 흔들림 시작 시점 기준 저장
            _intensity = Mathf.Max(_intensity, intensity); // 연타 시 약해지지 않게 최댓값 유지
            _duration = Mathf.Max(_duration, duration);
            _timer = _duration;
        }

        private void LateUpdate()
        {
            if (_timer <= 0f) return;
            _timer -= Time.unscaledDeltaTime;
            if (_timer <= 0f)
            {
                transform.localPosition = _baseLocalPos;
                _intensity = 0f;
                return;
            }
            float mag = _intensity * (_timer / _duration); // 시간에 따라 감쇠
            transform.localPosition = _baseLocalPos + (Vector3)(Random.insideUnitCircle * mag);
        }
    }
}

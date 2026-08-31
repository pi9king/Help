using UnityEngine;

namespace Help.Combat
{
    // 타격 순간 아주 짧게 시간을 멈춰 타격감을 준다(hitstop/hitstun).
    // 자신은 unscaled 시간으로 카운트해 timeScale=0 중에도 복원 시점을 잰다. 씬 배선 불필요(자동 생성).
    public class HitStop : MonoBehaviour
    {
        private static HitStop _runner;
        private float _timer;

        public static void Do(float seconds)
        {
            if (seconds <= 0f) return;
            Ensure()._Do(seconds);
        }

        private static HitStop Ensure()
        {
            if (_runner != null) return _runner;
            var go = new GameObject("~HitStop") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go);
            _runner = go.AddComponent<HitStop>();
            return _runner;
        }

        private void _Do(float seconds)
        {
            _timer = Mathf.Max(_timer, seconds);
            Time.timeScale = 0f;
        }

        private void Update()
        {
            if (_timer <= 0f) return;
            _timer -= Time.unscaledDeltaTime;
            if (_timer <= 0f) Time.timeScale = 1f;
        }
    }
}

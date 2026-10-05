using UnityEngine;
using UnityEngine.InputSystem;

namespace Help.Player
{
    // 조준 분할(360° 연속 / 8방향 / 4방향)을 플레이 중에 바꿔 가며 비교하는 테스트 하네스.
    // 비교가 끝나면 이 컴포넌트와 PlayerController._aimSteps는 지운다.
    public class AimModeTester : MonoBehaviour
    {
        private static readonly int[] Modes = { 1, 4, 8 };
        private static readonly string[] Labels = { "360° 연속 (현재 구현)", "4방향 스냅 (시트와 일치)", "8방향 스냅" };

        [SerializeField] private PlayerController _player;
        [SerializeField] private Key _toggleKey = Key.T;

        private int _index;
        private GUIStyle _style;

        private void Awake()
        {
            if (_player == null) _player = GetComponentInParent<PlayerController>();
            if (_player == null) _player = FindFirstObjectByType<PlayerController>();
            if (_player != null) _index = Mathf.Max(0, System.Array.IndexOf(Modes, _player.AimSteps));
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || _player == null) return;
            if (!keyboard[_toggleKey].wasPressedThisFrame) return;

            _index = (_index + 1) % Modes.Length;
            _player.AimSteps = Modes[_index];
        }

        private void OnGUI()
        {
            if (_player == null) return;

            // OnGUI는 프레임마다 여러 번 돈다. 스타일을 매번 만들지 않는다.
            _style ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                normal = { textColor = Color.yellow }
            };

            GUI.Label(new Rect(16f, 16f, 640f, 30f), $"조준: {Labels[_index]}", _style);
            GUI.Label(new Rect(16f, 44f, 640f, 30f), $"[{_toggleKey}] 키로 전환 · 마우스 조준 · 좌클릭 공격", _style);

            Vector2 aim = _player.AimDirection;
            GUI.Label(new Rect(16f, 72f, 640f, 30f),
                      $"조준각 {Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg:0.0}°  ·  시트 행 {ECharacterFacing.From(aim)}", _style);
        }
    }
}

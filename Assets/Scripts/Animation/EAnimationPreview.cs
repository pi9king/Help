using UnityEngine;
using UnityEngine.InputSystem;

namespace Help.Animation
{
    // A self-contained viewer. It deliberately does not use PlayerController or flipX.
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public sealed class EAnimationPreview : MonoBehaviour
    {
        private static readonly string[] ActionNames = { "Idle", "Walk", "Attack", "Hit", "Death", "Skill" };
        private static readonly string[] DirectionNames = { "Down", "Left", "Right", "Up" };
        private static readonly float[] Durations = { 4f / 7f, 6f / 11f, 6f / 12f, 3f / 11f, 8f / 9f, 6f / 11f };

        private Animator _animator;
        private SpriteRenderer _renderer;
        private int _action;
        private int _direction;
        private float _remaining;
        private bool _paused;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _renderer = GetComponent<SpriteRenderer>();
            _renderer.flipX = false;
            Play(0);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame) SetDirection(0);
            if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame) SetDirection(1);
            if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame) SetDirection(2);
            if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame) SetDirection(3);

            if (keyboard.digit1Key.wasPressedThisFrame) Play(0);
            if (keyboard.digit2Key.wasPressedThisFrame) Play(1);
            if (keyboard.digit3Key.wasPressedThisFrame) Play(2);
            if (keyboard.digit4Key.wasPressedThisFrame) Play(3);
            if (keyboard.digit5Key.wasPressedThisFrame) Play(4);
            if (keyboard.digit6Key.wasPressedThisFrame) Play(5);
            if (keyboard.rKey.wasPressedThisFrame) Play(_action);
            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                _paused = !_paused;
                _animator.speed = _paused ? 0f : 1f;
            }

            if (!_paused && _remaining > 0f)
            {
                _remaining -= Time.deltaTime;
                if (_remaining <= 0f && _action != 4) Play(0);
            }
        }

        private void SetDirection(int direction)
        {
            if (_direction == direction) return;
            _direction = direction;
            // Death and Skill have a single common Down clip.
            if (_action < 4) Play(_action);
        }

        private void Play(int action)
        {
            _action = action;
            _remaining = action < 2 ? 0f : Durations[action];
            string direction = action >= 4 ? "Down" : DirectionNames[_direction];
            _animator.speed = _paused ? 0f : 1f;
            _animator.Play(ActionNames[action] + "_" + direction, 0, 0f);
            _renderer.flipX = false;
        }

        private void OnGUI()
        {
            const int width = 510;
            GUILayout.BeginArea(new Rect(16, 16, width, 205), GUI.skin.box);
            GUILayout.Label("E CHARACTER ANIMATION PREVIEW");
            GUILayout.Label("Direction: WASD / arrow keys     Action: 1 Idle  2 Walk  3 Attack  4 Hit  5 Death  6 Skill");
            GUILayout.Label("Space: pause/resume     R: restart current animation");
            GUILayout.Label("Selected: " + ActionNames[_action] + " / " + DirectionNames[_direction] +
                (_action >= 4 ? " (common Down animation)" : "") + (_paused ? " / Paused" : ""));
            GUILayout.BeginHorizontal();
            for (int direction = 0; direction < DirectionNames.Length; direction++)
                if (GUILayout.Button(DirectionNames[direction])) SetDirection(direction);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            for (int action = 0; action < ActionNames.Length; action++)
                if (GUILayout.Button(ActionNames[action])) Play(action);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // The character's bottom-center pivot sits on this guide.
            var oldColor = GUI.color;
            GUI.color = new Color(0.25f, 0.85f, 0.85f, 0.45f);
            GUI.DrawTexture(new Rect(Screen.width * 0.5f - 110f, Screen.height * 0.5f, 220f, 1f), Texture2D.whiteTexture);
            GUI.color = oldColor;
        }
    }
}

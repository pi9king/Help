using Help.Combat;
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

        // Arm rig (Docs/ARM_RIG_PLAN.md): shown on the same spot as the character.
        private static readonly float[] RigSpeeds = { 0.25f, 0.5f, 1f };
        private ArmRigPreview _rig;
        private bool _rigMode;
        private bool _rigMouseAim;
        private int _rigSpeed = 2;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _renderer = GetComponent<SpriteRenderer>();
            _renderer.flipX = false;
            _rig = gameObject.AddComponent<ArmRigPreview>();
            _rig.Build(_renderer.sortingOrder);
            _rig.SetDirection(_direction);
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
            if (keyboard.digit7Key.wasPressedThisFrame) PlayRig(ArmMotionKind.Slash);
            if (keyboard.digit8Key.wasPressedThisFrame) PlayRig(ArmMotionKind.Thrust);
            if (keyboard.digit9Key.wasPressedThisFrame) PlayRig(ArmMotionKind.Smash);
            if (keyboard.digit0Key.wasPressedThisFrame) ShowRigRest();
            if (keyboard.rKey.wasPressedThisFrame) Play(_action);
            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                _paused = !_paused;
                _animator.speed = _paused ? 0f : 1f;
                ApplyRigSpeed();
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
            if (_rig != null) _rig.SetDirection(direction);
            if (_rigMode)
            {
                // Replay the current rig motion facing the new way (rest stays rest).
                if (_rig.Attacking || _rig.Repeat) _rig.Play(_rig.CurrentMotion, RigAimDegrees());
                return;
            }
            // Death and Skill have a single common Down clip.
            if (_action < 4) Play(_action);
        }

        private void Play(int action)
        {
            ExitRig();
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
            GUILayout.BeginArea(new Rect(16, 16, width, _rig == null || !_rig.Available ? 205 : _rig.EditKeys ? 580 : 390), GUI.skin.box);
            GUILayout.Label("E CHARACTER ANIMATION PREVIEW");
            GUILayout.Label("Direction: WASD / arrow keys     Action: 1 Idle  2 Walk  3 Attack  4 Hit  5 Death  6 Skill");
            GUILayout.Label("Space: pause/resume     R: restart");
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
            if (_rig != null && _rig.Available) DrawRigControls();
            GUILayout.EndArea();

            // The character's bottom-center pivot sits on this guide.
            var oldColor = GUI.color;
            GUI.color = new Color(0.25f, 0.85f, 0.85f, 0.45f);
            GUI.DrawTexture(new Rect(Screen.width * 0.5f - 110f, Screen.height * 0.5f, 220f, 1f), Texture2D.whiteTexture);
            GUI.color = oldColor;
        }

        private void DrawRigControls()
        {
            GUILayout.Space(6);
            GUILayout.Label("ARM RIG (separated arms, 4 directions)   keys: 7 Slash  8 Thrust  9 Smash  0 Rig Idle");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Slash")) PlayRig(ArmMotionKind.Slash);
            if (GUILayout.Button("Thrust")) PlayRig(ArmMotionKind.Thrust);
            if (GUILayout.Button("Smash")) PlayRig(ArmMotionKind.Smash);
            if (GUILayout.Button("Rig Idle")) ShowRigRest();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            for (int i = 0; i < RigSpeeds.Length; i++)
                if (GUILayout.Toggle(_rigSpeed == i, "x" + RigSpeeds[i], GUI.skin.button)) { _rigSpeed = i; ApplyRigSpeed(); }
            if (GUILayout.Button("Weapon: " + _rig.WeaponName)) _rig.NextWeapon();
            _rig.Repeat = GUILayout.Toggle(_rig.Repeat, "Repeat", GUI.skin.button);
            _rig.ShowGrip = GUILayout.Toggle(_rig.ShowGrip, "Grip", GUI.skin.button);
            _rigMouseAim = GUILayout.Toggle(_rigMouseAim, _rigMouseAim ? "Aim: Mouse" : "Aim: Facing", GUI.skin.button);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            _rig.KeyframeMotion = GUILayout.Toggle(_rig.KeyframeMotion,
                _rig.KeyframeMotion ? "Motion: Key poses (new)" : "Motion: Screen angles (old)", GUI.skin.button);
            _rig.Stepped = GUILayout.Toggle(_rig.Stepped, _rig.Stepped ? "Stepped 12fps: ON" : "Stepped 12fps: OFF", GUI.skin.button);
            _rig.PauldronOnBody = GUILayout.Toggle(_rig.PauldronOnBody,
                _rig.PauldronOnBody ? "Pauldron: Body (fixed)" : "Pauldron: Arm (old)", GUI.skin.button);
            GUILayout.EndHorizontal();
            if (_rig.KeyframeMotion) DrawKeyEditor();
            GUILayout.Label(_rigMode
                ? "Rig: " + _rig.CurrentMotion + " (" + _rig.Timing + ")   aim " + RigAimDegrees().ToString("0") +
                  "°   Idle / Rig Idle buttons compare original and rig on the same spot"
                : "Press a rig button to swap the character for the rig on the same spot.");
        }

        // 키 자세 조정: 지금 방향·모션의 키를 골라 멈춰 보고, 슬라이더로 고친 뒤 저장한다.
        private void DrawKeyEditor()
        {
            GUILayout.BeginHorizontal();
            bool edit = GUILayout.Toggle(_rig.EditKeys, _rig.EditKeys ? "Edit keys: ON" : "Edit keys: OFF", GUI.skin.button,
                GUILayout.Width(110));
            if (edit != _rig.EditKeys)
            {
                if (edit) { EnterRig(); _rig.BeginEdit(); }
                else _rig.EditKeys = false;
            }
            if (_rig.EditKeys)
            {
                for (int i = 0; i < ArmRigPreview.KeyNames.Length; i++)
                    if (GUILayout.Toggle(_rig.EditKeyIndex == i, ArmRigPreview.KeyNames[i], GUI.skin.button))
                        _rig.EditKeyIndex = i;
            }
            GUILayout.EndHorizontal();
            if (!_rig.EditKeys) return;

            ArmKey key = _rig.EditKey;
            ArmKey edited = key;
            edited.Hand.x = KeySlider("Hand X", key.Hand.x, -22f, 22f, 1f);
            edited.Hand.y = KeySlider("Hand Y", key.Hand.y, -22f, 22f, 1f);
            edited.Weapon = KeySlider("Weapon°", key.Weapon, -270f, 270f, 5f);
            edited.Shoulder.x = KeySlider("Shoulder X", key.Shoulder.x, -6f, 6f, 1f);
            edited.Shoulder.y = KeySlider("Shoulder Y", key.Shoulder.y, -6f, 6f, 1f);
            GUILayout.BeginHorizontal();
            edited.Front = GUILayout.Toggle(key.Front, key.Front ? "Arm: in front of body" : "Arm: behind body", GUI.skin.button);
            edited.ElbowFlip = GUILayout.Toggle(key.ElbowFlip, key.ElbowFlip ? "Elbow: flipped" : "Elbow: hangs down", GUI.skin.button);
            if (GUILayout.Button("Save")) _rig.SaveMotions();
            if (GUILayout.Button("Reload")) _rig.ReloadMotions();
            GUILayout.EndHorizontal();
            if (edited.Hand != key.Hand || edited.Weapon != key.Weapon || edited.Front != key.Front ||
                edited.Shoulder != key.Shoulder || edited.ElbowFlip != key.ElbowFlip) _rig.EditKey = edited;
            GUILayout.Label(_rig.CurrentMotion + " / " + DirectionNames[_direction] + " / " +
                            ArmRigPreview.KeyNames[_rig.EditKeyIndex] + "   " + _rig.MotionsStatus);
        }

        private static float KeySlider(string label, float value, float min, float max, float step)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + " " + value.ToString("0"), GUILayout.Width(90));
            float next = GUILayout.HorizontalSlider(value, min, max);
            GUILayout.EndHorizontal();
            return Mathf.Round(next / step) * step;
        }

        private void PlayRig(ArmMotionKind kind)
        {
            if (_rig == null || !_rig.Available) return;
            EnterRig();
            _rig.EditKeys = false;
            _rig.Play(kind, RigAimDegrees());
        }

        private void ShowRigRest()
        {
            if (_rig == null || !_rig.Available) return;
            EnterRig();
            _rig.ShowRest();
        }

        private void EnterRig()
        {
            _rigMode = true;
            _remaining = 0f;          // don't fall back to Idle when a previous clip ends
            _renderer.enabled = false;
            ApplyRigSpeed();
        }

        private void ExitRig()
        {
            if (!_rigMode) return;
            _rigMode = false;
            _rig.Show(false);
            _renderer.enabled = true;
        }

        private void ApplyRigSpeed()
        {
            if (_rig != null) _rig.Speed = _paused ? 0f : RigSpeeds[_rigSpeed];
        }

        // Facing aims along the selected direction; mouse aims from the weapon shoulder.
        private float RigAimDegrees()
        {
            if (_rigMouseAim && Mouse.current != null && Camera.main != null)
            {
                Vector2 world = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                return AimGeometry.AngleDegrees(world - _rig.ShoulderWorld);
            }
            switch (_direction)
            {
                case 1: return 180f;
                case 2: return 0f;
                case 3: return 90f;
                default: return -90f;
            }
        }
    }
}

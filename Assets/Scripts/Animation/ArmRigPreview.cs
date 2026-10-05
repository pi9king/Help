using System.Collections.Generic;
using System.IO;
using Help.Combat;
using UnityEngine;

namespace Help.Animation
{
    // 임시 미리보기 부품 — 팔 리그 기본 모션(베기·찌르기·내려찍기), 4방향. Docs/ARM_RIG_PLAN.md.
    // EAnimationPreview가 같은 자리(캐릭터 피벗)에 띄우고 버튼으로 조작한다. 입력·GUI는 갖지 않는다.
    // 에셋은 Tools/build_arm_rig_poc.py가 Assets/Resources/ArmRig/에 만든다.
    // 팔 조각은 네 방향이 공유하고, 방향마다 어깨 위치·앞뒤 순서·몸통 그림만 다르다.
    // 리그가 확정되면 정식 ECharacterRig로 옮기고 이 컴포넌트는 지운다.
    public sealed class ArmRigPreview : MonoBehaviour
    {
        private const float PixelsPerUnit = 32f;
        private const string Folder = "ArmRig/";
        private const float SpriteFps = 12f;
        private const float StrikeFps = 24f;

        [System.Serializable]
        public class WeaponData   // JsonUtility가 채운다 (public이어야 CS0649가 안 난다)
        {
            public string name;
            public string motion;
            public int[] grip;
        }

        [System.Serializable]
        public class DirectionData
        {
            public string name;
            public int[] mainShoulder;
            public int mainSide;
            public bool mainFront;
            public int[] offShoulder;
            public int offSide;
            public bool offFront;
        }

        [System.Serializable]
        public class RigData
        {
            public int cell;
            public int[] elbowFromShoulder;
            public int[] gripFromElbow;
            public int[] upperPivot;
            public int[] forePivot;
            public int[] pauldronPivot;
            public int[] bareUpperPivot;
            public int[] jointForePivot;
            public DirectionData[] directions;
            public WeaponData[] weapons;
        }

        // Assets/Resources/ArmRig/arm_motions.json — 방향별 키 자세. 초안은 Tools/arm_motion_defaults.py,
        // 이후로는 미리보기의 키 조정 패널이 고쳐서 저장한다.
        [System.Serializable]
        public class KeyData
        {
            public string name;
            public float[] hand;
            public float weapon;
            public bool front;
            public float[] shoulder;   // 없으면 (0, 0)
            public bool elbowFlip;
        }

        [System.Serializable]
        public class RestData
        {
            public string direction;
            public float[] hand;
            public float weapon;
            public bool front;
        }

        [System.Serializable]
        public class TrackData
        {
            public string motion;
            public string direction;
            public string curve;
            public KeyData[] keys;
        }

        [System.Serializable]
        public class MotionFile
        {
            public RestData[] rests;
            public TrackData[] tracks;
        }

        public static readonly string[] KeyNames = { "Rest", "Loaded", "Cocked", "Mid", "Through" };

        private class Arm
        {
            public Transform Shoulder;
            public Transform Elbow;
            public Transform Grip;
            public Vector3 Home;
            public SpriteRenderer Upper;
            public SpriteRenderer Fore;
            public SpriteRenderer Pauldron;
        }

        private class DirectionRig
        {
            public DirectionData Data;
            public int BaseOrder;
            public Transform Root;
            public Transform Upper;     // 상체(몸통·망토·양팔). 다리는 땅에 고정
            public Arm Main;
            public Arm Off;
            public SpriteRenderer Weapon;
            public SlashVFX Smear;
        }

        // 1차 테스트 타이밍(예비/타격/회수 초): WEAPON_ROSTER.md의 AXE·PIKE·MACE 값.
        private static readonly (ArmMotionKind kind, float windup, float active, float recovery)[] Motions =
        {
            (ArmMotionKind.Slash, 0.12f, 0.18f, 0.22f),
            (ArmMotionKind.Thrust, 0.16f, 0.09f, 0.25f),
            (ArmMotionKind.Smash, 0.15f, 0.10f, 0.18f),
        };

        private RigData _rig;
        private Transform _root;
        private DirectionRig[] _directions;
        private DirectionRig _current;
        private Sprite _upperWithGuard, _foreOld, _upperBare, _foreJoint, _pauldron;
        private Sprite[] _weaponSprites;
        private bool _pauldronOnBody = true;
        private int _motion;
        private int _weaponIndex;
        private bool _attacking;
        private bool _smearShown;
        private float _elapsed;
        private float _aim;
        private float _repeatWait;

        public bool Available => _rig != null;
        public bool Visible => _root != null && _root.gameObject.activeSelf;
        public bool Attacking => _attacking;
        public float Speed { get; set; } = 1f;
        public bool Repeat { get; set; }
        public bool ShowGrip { get; set; }
        // 스프라이트 박자(12fps)로 끊어 보여 준다. 끄면 매 프레임 연속 회전.
        public bool Stepped { get; set; } = true;
        // 어깨 보호대를 몸통에 고정하고 팔만 그 아래에서 돌린다. 끄면 예전처럼 보호대째 회전.
        public bool PauldronOnBody
        {
            get => _pauldronOnBody;
            set
            {
                if (_pauldronOnBody == value) return;
                _pauldronOnBody = value;
                if (_directions == null) return;
                foreach (var rig in _directions) { ApplyArmSprites(rig.Main); ApplyArmSprites(rig.Off); }
            }
        }
        // 방향별 화면 키 자세(arm_motions.json)로 움직인다. 끄면 예전 화면 각도 방식(비교용).
        public bool KeyframeMotion { get; set; } = true;
        // 키 조정: 켜면 고른 키 자세에서 멈춰 보여 주고, 패널에서 값을 고친다.
        public bool EditKeys { get; set; }
        public int EditKeyIndex { get; set; } = 3;
        public string MotionsStatus { get; private set; } = "";
        public ArmMotionKind CurrentMotion => Motions[_motion].kind;
        public string WeaponName => Available ? _rig.weapons[_weaponIndex].name : "-";
        public string Timing => $"{Motions[_motion].windup:0.00} / {Motions[_motion].active:0.00} / {Motions[_motion].recovery:0.00} s";

        // 같은 자리에 겹쳐 그리므로 캐릭터와 같은 정렬 순서를 기준으로 쌓는다.
        public void Build(int baseOrder)
        {
            var json = Resources.Load<TextAsset>(Folder + "rig");
            if (json == null)
            {
                Debug.LogWarning("ArmRig assets missing — run: python -B Tools/build_arm_rig_poc.py");
                return;
            }
            _rig = JsonUtility.FromJson<RigData>(json.text);
            _upperWithGuard = PieceSprite("E_Arm_Upper", _rig.upperPivot);
            _foreOld = PieceSprite("E_Arm_Fore", _rig.forePivot);
            _upperBare = PieceSprite("E_Arm_UpperBare", _rig.bareUpperPivot);
            _foreJoint = PieceSprite("E_Arm_ForeJoint", _rig.jointForePivot);
            _pauldron = PieceSprite("E_Pauldron", _rig.pauldronPivot);
            _weaponSprites = new Sprite[_rig.weapons.Length];
            for (int i = 0; i < _rig.weapons.Length; i++)
                _weaponSprites[i] = PieceSprite("Weapon_" + _rig.weapons[i].name, _rig.weapons[i].grip);

            ReloadMotions();

            _root = new GameObject("Arm Rig Preview").transform;
            _root.SetParent(transform, false);
            _directions = new DirectionRig[_rig.directions.Length];
            for (int i = 0; i < _directions.Length; i++)
                _directions[i] = BuildDirection(_rig.directions[i], baseOrder);
            SelectMotion(ArmMotionKind.Slash);
            SetDirection(0);
            Show(false);
        }

        // 0 Down, 1 Left, 2 Right, 3 Up — EAnimationPreview의 방향 순서와 같다.
        public void SetDirection(int index)
        {
            if (_directions == null) return;
            index = Mathf.Clamp(index, 0, _directions.Length - 1);
            for (int i = 0; i < _directions.Length; i++)
                _directions[i].Root.gameObject.SetActive(i == index);
            _current = _directions[index];
            _current.Weapon.sprite = _weaponSprites[_weaponIndex];
            Refresh();
        }

        public void Show(bool visible)
        {
            if (_root == null) return;
            _root.gameObject.SetActive(visible);
            if (!visible) _attacking = false;
        }

        // 공격 없이 휴식 자세만 보여 준다.
        public void ShowRest()
        {
            Show(true);
            _attacking = false;
            Refresh();
        }

        public void Play(ArmMotionKind kind, float aimDegrees)
        {
            if (!Available) return;
            Show(true);
            SelectMotion(kind);
            _aim = aimDegrees;
            _elapsed = 0f;
            _repeatWait = 0f;
            _smearShown = false;
            _attacking = true;
        }

        public void NextWeapon() => SelectWeapon(_weaponIndex + 1);

        // 어깨 위치(월드) — 마우스 조준각을 잴 때 쓴다.
        public Vector2 ShoulderWorld => _current.Upper.TransformPoint(_current.Main.Home);

        private void SelectMotion(ArmMotionKind kind)
        {
            for (int i = 0; i < Motions.Length; i++)
                if (Motions[i].kind == kind) _motion = i;
            // 모션마다 어울리는 시험용 무기를 같이 고른다. NextWeapon으로 따로 바꿀 수 있다.
            for (int i = 0; i < _rig.weapons.Length; i++)
                if (_rig.weapons[i].motion == kind.ToString()) SelectWeapon(i);
        }

        private void SelectWeapon(int index)
        {
            _weaponIndex = (index + _weaponSprites.Length) % _weaponSprites.Length;
            if (_current != null) _current.Weapon.sprite = _weaponSprites[_weaponIndex];
        }

        // 스프라이트 박자(12fps)로 끊되, 휘두르는 타격 구간만 24fps로 촘촘하게 보여 준다.
        private float ShownTime()
        {
            if (!Stepped) return _elapsed;
            var (_, windup, active, _) = Motions[_motion];
            return ArmMotion.StepTime(_elapsed, windup, windup + active * ArmMotion.StrikeShare, SpriteFps, StrikeFps);
        }

        // 지금 시각의 자세를 그린다(공격 중이 아니면 휴식 자세).
        private void Refresh()
        {
            if (_current == null) return;
            var (kind, windup, active, recovery) = Motions[_motion];
            float time = _attacking ? ShownTime() : 0f;
            if (KeyframeMotion)
            {
                ArmKey key = EditKeys ? EditKey : (_attacking && TryTrack(out ArmKeyTrack track)
                    ? ArmKeyMotion.Evaluate(track, CurrentRest, time, windup, active, recovery)
                    : CurrentRest);
                // 상체 체중 이동은 각도 방식과 같은 타이밍 값을 쓴다. 키를 고치는 동안에는 멈춘다.
                ArmRigPose body = EditKeys ? ArmMotion.Rest : ArmMotion.Evaluate(kind, _aim, time, windup, active, recovery);
                ApplyKey(key, body.BodyLean, body.BodyDrop);
            }
            else
            {
                Apply(ArmMotion.Evaluate(kind, _aim, time, windup, active, recovery), _aim);
                SetMainOrder(_current.Data.mainFront);
                _current.Weapon.transform.localScale = Vector3.one;
            }
        }

        private void Update()
        {
            if (!Visible || _current == null) return;
            var (kind, windup, active, recovery) = Motions[_motion];
            float delta = Time.deltaTime * Speed;
            if (_attacking)
            {
                _elapsed += delta;
                if (!_smearShown && ArmMotion.StrikeStarted(ShownTime(), windup)) PlaySmear(kind, active);
                if (_elapsed >= windup + active + recovery) _attacking = false;
            }
            else if (Repeat)
            {
                _repeatWait += delta;
                if (_repeatWait >= 0.35f) Play(kind, _aim);
            }
            Refresh();
            _current.Main.Grip.GetChild(0).gameObject.SetActive(ShowGrip);
            _current.Off.Grip.GetChild(0).gameObject.SetActive(ShowGrip);
        }

        private void PlaySmear(ArmMotionKind kind, float active)
        {
            _smearShown = true;
            if (KeyframeMotion && TryTrack(out ArmKeyTrack keys))
            {
                // 호는 키에 적힌 무기 각도(힘 모으기 -> 끝)를 따라 그린다.
                float mid = keys.Mid.Weapon * Mathf.Deg2Rad;
                var along = new Vector2(Mathf.Cos(mid), Mathf.Sin(mid));
                _current.Smear.Play(along, 0.45f, keys.Cocked.Weapon - keys.Mid.Weapon, keys.Through.Weapon - keys.Mid.Weapon,
                    new Color(1f, 1f, 1f, 0.85f), 1.2f, (active + 0.08f) / Mathf.Max(0.05f, Speed));
                return;
            }
            var direction = new Vector2(Mathf.Cos(_aim * Mathf.Deg2Rad), Mathf.Sin(_aim * Mathf.Deg2Rad));
            float duration = (active + 0.08f) / Mathf.Max(0.05f, Speed);
            var color = new Color(1f, 1f, 1f, 0.85f);
            switch (kind)
            {
                case ArmMotionKind.Slash: _current.Smear.Play(direction, 0.55f, 75f, -85f, color, 1.3f, duration); break;
                case ArmMotionKind.Thrust: _current.Smear.Play(direction, 0.8f, 6f, -6f, color, 1.0f, duration); break;
                case ArmMotionKind.Smash: _current.Smear.Play(direction, 0.6f, 40f, -18f, color, 1.2f, duration); break;
            }
        }

        private void Apply(ArmRigPose pose, float aim)
        {
            if (_current == null) return;
            var direction = new Vector2(Mathf.Cos(aim * Mathf.Deg2Rad), Mathf.Sin(aim * Mathf.Deg2Rad));
            ApplyBody(pose.BodyLean, pose.BodyDrop, aim);
            Arm main = _current.Main, off = _current.Off;
            main.Shoulder.localPosition = main.Home + (Vector3)(direction * pose.Reach);
            main.Pauldron.transform.localPosition = main.Home;
            main.Shoulder.localRotation = Quaternion.Euler(0f, 0f, pose.MainShoulder);
            main.Elbow.localRotation = Quaternion.Euler(0f, 0f, pose.MainElbow);
            _current.Weapon.transform.localRotation = Quaternion.Euler(0f, 0f, pose.WeaponAngle);
            off.Shoulder.localRotation = Quaternion.Euler(0f, 0f, pose.OffShoulder);
            off.Elbow.localRotation = Quaternion.Euler(0f, 0f, pose.OffElbow);
        }

        // ---- 키 자세 방식 ----

        private readonly Dictionary<string, TrackData> _tracks = new Dictionary<string, TrackData>();
        private readonly Dictionary<string, RestData> _rests = new Dictionary<string, RestData>();
        private MotionFile _motionFile;

        private static string MotionsPath => Path.Combine(Application.dataPath, "Resources/ArmRig/arm_motions.json");
        private string DirectionName => _current.Data.name;
        private string TrackKey => DirectionName + "/" + CurrentMotion;

        public void ReloadMotions()
        {
            string json = File.Exists(MotionsPath) ? File.ReadAllText(MotionsPath)
                : Resources.Load<TextAsset>(Folder + "arm_motions")?.text;
            _tracks.Clear();
            _rests.Clear();
            if (string.IsNullOrEmpty(json))
            {
                MotionsStatus = "arm_motions.json missing — run python -B Tools/arm_motion_defaults.py";
                _motionFile = null;
                return;
            }
            _motionFile = JsonUtility.FromJson<MotionFile>(json);
            foreach (var rest in _motionFile.rests) _rests[rest.direction] = rest;
            foreach (var track in _motionFile.tracks) _tracks[track.direction + "/" + track.motion] = track;
            MotionsStatus = "loaded " + _motionFile.tracks.Length + " tracks";
            Refresh();
        }

        // 에디터 Play 중 저장. 빌드에서는 파일을 쓰지 않는다.
        public void SaveMotions()
        {
#if UNITY_EDITOR
            if (_motionFile == null) return;
            File.WriteAllText(MotionsPath, JsonUtility.ToJson(_motionFile, true));
            MotionsStatus = "saved " + System.DateTime.Now.ToString("HH:mm:ss") + " -> Assets/Resources/ArmRig/arm_motions.json";
#else
            MotionsStatus = "save is editor only";
#endif
        }

        private ArmKey CurrentRest => _current != null && _rests.TryGetValue(DirectionName, out RestData rest)
            ? new ArmKey { Hand = new Vector2(rest.hand[0], rest.hand[1]), Weapon = rest.weapon, Front = rest.front }
            : new ArmKey { Hand = new Vector2(1f, -19f), Weapon = -70f, Front = true };

        private bool TryTrack(out ArmKeyTrack track)
        {
            track = default;
            if (_current == null || !_tracks.TryGetValue(TrackKey, out TrackData data)) return false;
            track.Loaded = ToKey(FindKey(data, "Loaded"));
            track.Cocked = ToKey(FindKey(data, "Cocked"));
            track.Mid = ToKey(FindKey(data, "Mid"));
            track.Through = ToKey(FindKey(data, "Through"));
            track.Curve = data.curve == "Accelerate" ? ArmStrikeCurve.Accelerate
                        : data.curve == "Snap" ? ArmStrikeCurve.Snap : ArmStrikeCurve.Smooth;
            return true;
        }

        private static KeyData FindKey(TrackData track, string name)
        {
            foreach (var key in track.keys) if (key.name == name) return key;
            return track.keys[0];
        }

        private static ArmKey ToKey(KeyData key) => new ArmKey
        {
            Hand = new Vector2(key.hand[0], key.hand[1]),
            Weapon = key.weapon,
            Front = key.front,
            Shoulder = key.shoulder != null && key.shoulder.Length >= 2 ? new Vector2(key.shoulder[0], key.shoulder[1]) : Vector2.zero,
            ElbowFlip = key.elbowFlip,
        };

        // 키 조정 패널이 읽고 쓰는 값 — 지금 방향·모션의 EditKeyIndex 번 키(0 = 휴식).
        public ArmKey EditKey
        {
            get
            {
                if (EditKeyIndex == 0 || !_tracks.TryGetValue(TrackKey, out TrackData data)) return CurrentRest;
                return ToKey(FindKey(data, KeyNames[EditKeyIndex]));
            }
            set
            {
                if (_current == null) return;
                if (EditKeyIndex == 0)
                {
                    if (!_rests.TryGetValue(DirectionName, out RestData rest)) return;
                    rest.hand = new[] { value.Hand.x, value.Hand.y };
                    rest.weapon = value.Weapon;
                    rest.front = value.Front;
                }
                else if (_tracks.TryGetValue(TrackKey, out TrackData data))
                {
                    KeyData key = FindKey(data, KeyNames[EditKeyIndex]);
                    key.hand = new[] { value.Hand.x, value.Hand.y };
                    key.weapon = value.Weapon;
                    key.front = value.Front;
                    key.shoulder = new[] { value.Shoulder.x, value.Shoulder.y };
                    key.elbowFlip = value.ElbowFlip;
                }
                Refresh();
            }
        }

        // 고른 키 자세를 보여 주며 조정할 수 있게 공격 재생을 멈춘다.
        public void BeginEdit()
        {
            Show(true);
            _attacking = false;
            EditKeys = true;
            Refresh();
        }

        private void ApplyKey(ArmKey key, float bodyLean, float bodyDrop)
        {
            ApplyBody(bodyLean, bodyDrop, _aim);
            Arm main = _current.Main, off = _current.Off;
            // 어깨까지 쓰는 동작(찌르기): 어깨와 보호대를 함께 옮긴다. 손은 어깨 기준이라 같이 나간다.
            Vector3 shoulderAt = main.Home + (Vector3)(key.Shoulder / PixelsPerUnit);
            main.Shoulder.localPosition = shoulderAt;
            main.Pauldron.transform.localPosition = shoulderAt;
            // 반대 팔은 손 개선 전까지 휴식 자세로 둔다(각도 방식의 반대 팔이 이상하게 움직였다).
            off.Shoulder.localRotation = Quaternion.Euler(0f, 0f, ArmMotion.Rest.OffShoulder);
            off.Elbow.localRotation = Quaternion.Euler(0f, 0f, ArmMotion.Rest.OffElbow);

            TwoBoneIk.Solve(key.Hand, Length(_rig.elbowFromShoulder), Length(_rig.gripFromElbow), key.ElbowFlip,
                            out float upperDir, out float foreDir);
            float weaponDir = key.Weapon;
            if (_current.Data.mainSide < 0)
            {
                // 좌우반전된 팔의 로컬 공간에서는 화면 각도가 거울상이 된다.
                upperDir = 180f - upperDir;
                foreDir = 180f - foreDir;
                weaponDir = 180f - weaponDir;
            }
            // 조각을 자른 그대로일 때 어깨->팔꿈치, 팔꿈치->주먹이 가리키던 방향을 빼서 회전량을 구한다.
            float shoulder = upperDir - RestDirection(_rig.elbowFromShoulder);
            float elbow = foreDir - shoulder - RestDirection(_rig.gripFromElbow);
            main.Shoulder.localRotation = Quaternion.Euler(0f, 0f, shoulder);
            main.Elbow.localRotation = Quaternion.Euler(0f, 0f, elbow);
            // 무기 그림은 날이 위(+90°)를 향한다. 키의 무기 각도는 화면 기준이라 팔이 접혀도 뒤집히지 않는다.
            _current.Weapon.transform.localRotation = Quaternion.Euler(0f, 0f, weaponDir - 90f - shoulder - elbow);
            _current.Weapon.transform.localScale = Vector3.one;
            SetMainOrder(key.Front);
        }

        private void SetMainOrder(bool front)
        {
            int order = front ? _current.BaseOrder + 1 : _current.BaseOrder - 5;
            _current.Main.Upper.sortingOrder = order;
            _current.Main.Pauldron.sortingOrder = order + 1;
            _current.Main.Fore.sortingOrder = order + 2;
            _current.Weapon.sortingOrder = order + 3;
        }

        private static float Length(int[] v) => Mathf.Sqrt(v[0] * v[0] + v[1] * v[1]);

        // 이미지 픽셀(y 아래) 벡터의 화면 각도(y 위).
        private static float RestDirection(int[] v) => Mathf.Atan2(-v[1], v[0]) * Mathf.Rad2Deg;

        private void ApplyBody(float bodyLean, float bodyDrop, float aim)
        {
            var direction = new Vector2(Mathf.Cos(aim * Mathf.Deg2Rad), Mathf.Sin(aim * Mathf.Deg2Rad));
            // 상체만 움직인다. 쿼터뷰라 세로 쏠림은 4분의 1로 줄인다(Up에서 상체가 위로 떠 보였다).
            // 픽셀 단위로 맞춰 흔들림을 막는다.
            var lean = new Vector2(direction.x, direction.y * 0.25f) * bodyLean + Vector2.down * bodyDrop;
            _current.Upper.localPosition = new Vector3(Mathf.Round(lean.x), Mathf.Round(lean.y), 0f) / PixelsPerUnit;
        }

        private DirectionRig BuildDirection(DirectionData data, int baseOrder)
        {
            var rig = new DirectionRig { Data = data, BaseOrder = baseOrder };
            rig.Root = new GameObject("Rig " + data.name).transform;
            rig.Root.SetParent(_root, false);
            CreateRenderer("Legs (planted)", rig.Root, Vector3.zero, BodySprite("E_Body_" + data.name + "_Legs"), baseOrder);
            rig.Upper = new GameObject("Upper Body").transform;
            rig.Upper.SetParent(rig.Root, false);
            CreateRenderer("Armless Upper Body", rig.Upper, Vector3.zero,
                BodySprite("E_Body_" + data.name + "_Upper"), baseOrder);

            rig.Main = CreateArm(rig.Upper, "Main Arm (weapon)", data.mainShoulder, data.mainSide, data.mainFront, baseOrder);
            rig.Off = CreateArm(rig.Upper, "Off Arm", data.offShoulder, data.offSide, data.offFront, baseOrder);
            rig.Weapon = CreateRenderer("Weapon", rig.Main.Grip, Vector3.zero, null,
                baseOrder + (data.mainFront ? 4 : -1));

            // 게임과 같은 슬래시 호. 어깨에 붙여 상체와 함께 움직인다.
            var smear = new GameObject("Slash VFX");
            smear.transform.SetParent(rig.Upper, false);
            smear.transform.localPosition = rig.Main.Home;
            rig.Smear = smear.AddComponent<SlashVFX>();
            return rig;
        }

        // 그리는 순서: 윗팔 < 어깨 보호대 < 아랫팔 < 무기. 몸 뒤 팔은 이 묶음 전체를 몸통 아래로 내린다.
        private Arm CreateArm(Transform upperBody, string name, int[] shoulderPixel, int side, bool front, int baseOrder)
        {
            int order = front ? baseOrder + 1 : baseOrder - 5;
            var shoulder = new GameObject(name).transform;
            shoulder.SetParent(upperBody, false);
            Vector3 home = BodyPoint(shoulderPixel);
            shoulder.localPosition = home;
            shoulder.localScale = new Vector3(side, 1f, 1f);
            SpriteRenderer upper = CreateRenderer("Upper", shoulder, Vector3.zero, null, order);

            // 보호대는 어깨 회전과 상관없이 상체에 붙어 있다.
            SpriteRenderer guard = CreateRenderer(name + " Pauldron", upperBody, home, _pauldron, order + 1);
            guard.transform.localScale = new Vector3(side, 1f, 1f);

            var elbow = new GameObject("Elbow").transform;
            elbow.SetParent(shoulder, false);
            elbow.localPosition = Offset(_rig.elbowFromShoulder);
            SpriteRenderer fore = CreateRenderer("Fore", elbow, Vector3.zero, null, order + 2);

            var grip = new GameObject("Grip").transform;
            grip.SetParent(elbow, false);
            grip.localPosition = Offset(_rig.gripFromElbow);
            CreateRenderer("Grip Marker", grip, Vector3.zero, Dot(new Color(0f, 1f, 0.3f)), baseOrder + 9);

            var arm = new Arm { Shoulder = shoulder, Elbow = elbow, Grip = grip, Home = home,
                                Upper = upper, Fore = fore, Pauldron = guard };
            ApplyArmSprites(arm);
            return arm;
        }

        private void ApplyArmSprites(Arm arm)
        {
            if (arm == null) return;
            arm.Upper.sprite = _pauldronOnBody ? _upperBare : _upperWithGuard;
            arm.Fore.sprite = _pauldronOnBody ? _foreJoint : _foreOld;
            arm.Pauldron.enabled = _pauldronOnBody;
        }

        // 이미지 픽셀 좌표(위가 0) -> 몸통 스프라이트 로컬 좌표(피벗 = 아래 가운데).
        private Vector3 BodyPoint(int[] pixel) => new Vector3(
            (pixel[0] + 0.5f - _rig.cell / 2f) / PixelsPerUnit,
            (_rig.cell - pixel[1] - 0.5f) / PixelsPerUnit, 0f);

        private static Vector3 Offset(int[] pixels) =>
            new Vector3(pixels[0] / PixelsPerUnit, -pixels[1] / PixelsPerUnit, 0f);

        private static Sprite BodySprite(string name)
        {
            Texture2D texture = LoadTexture(name);
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0f), PixelsPerUnit);
        }

        private static Sprite PieceSprite(string name, int[] pivot)
        {
            Texture2D texture = LoadTexture(name);
            var normalized = new Vector2((pivot[0] + 0.5f) / texture.width,
                                         1f - (pivot[1] + 0.5f) / texture.height);
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), normalized, PixelsPerUnit);
        }

        private static Texture2D LoadTexture(string name)
        {
            var texture = Resources.Load<Texture2D>(Folder + name);
            texture.filterMode = FilterMode.Point;
            return texture;
        }

        private static Sprite Dot(Color color)
        {
            var texture = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        private static SpriteRenderer CreateRenderer(string name, Transform parent, Vector3 position, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using Help.Core;
using Help.Enemy;
using Help.Inventory;
using Help.Item;
using Help.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Help.Combat
{
    // T1 공격의 거리·폭·속도를 나란히 체감하는 별도 씬 전용 컴포넌트.
    public class WeaponTestArena : MonoBehaviour
    {
        private static readonly string[] WeaponIds =
        {
            "axe", "mace", "pike", "cane", "pipe", "net", "pen", "wire",
            "stone", "spade", "epee", "needle", "pestle", "skewer"
        };

        private static readonly Vector2[] TargetPositions =
        {
            new Vector2(1.0f, 0f), new Vector2(1.5f, 0f), new Vector2(2.1f, 0f),
            new Vector2(1.2f, 0.8f), new Vector2(1.2f, -0.8f)
        };

        private readonly List<EnemyStats> _targets = new();
        private readonly List<int> _hits = new();
        private PlayerController _player;
        private int _index;
        private string _message;
        private Sprite _swatch;
        private Texture2D _swatchTexture;

        private IEnumerator Start()
        {
            // PlayerController.Start가 인벤토리 장착 이벤트를 구독한 뒤 장착한다.
            yield return null;
            _player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerController>();
            var gm = GameManager.Instance;
            if (_player == null || gm == null || gm.RecipeDatabase == null)
            {
                _message = "Player, GameManager 또는 RecipeDatabase가 연결되지 않았습니다.";
                yield break;
            }

            _player.transform.position = Vector3.zero;
            CreateSwatch();
            CreateBackground();
            for (int i = 0; i < TargetPositions.Length; i++) CreateTarget(i);

            foreach (string id in WeaponIds)
            {
                var item = gm.RecipeDatabase.Find(id);
                if (item != null && gm.Inventory.CountOf(item) == 0)
                    gm.Inventory.Add(item);
            }
            Select(0);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.nKey.wasPressedThisFrame) Select((_index + 1) % WeaponIds.Length);
            if (keyboard.pKey.wasPressedThisFrame) Select((_index + WeaponIds.Length - 1) % WeaponIds.Length);
            if (keyboard.rKey.wasPressedThisFrame) ResetTargets();
        }

        private void Select(int index)
        {
            _index = index;
            var inventory = GameManager.Instance.Inventory;
            var item = GameManager.Instance.RecipeDatabase.Find(WeaponIds[index]);
            if (item == null) { _message = WeaponIds[index].ToUpperInvariant() + " 에셋이 없습니다."; return; }
            if (_player.EquippedWeapon == item) return;

            ItemStack stack = null;
            foreach (var candidate in inventory.Items)
                if (candidate.Definition == item) { stack = candidate; break; }
            if (stack == null || !inventory.Equip(stack, EquipmentSlotType.Weapon))
            {
                _message = item.Word + " 장착에 실패했습니다.";
                return;
            }
            _message = item.Word + " 장착";
        }

        private void ResetTargets()
        {
            for (int i = 0; i < _targets.Count; i++)
            {
                _targets[i].Reset();
                _hits[i] = 0;
            }
            _player.transform.position = Vector3.zero;
            if (_player.TryGetComponent<Rigidbody2D>(out var body)) body.linearVelocity = Vector2.zero;
        }

        private void CreateSwatch()
        {
            _swatchTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _swatchTexture.SetPixel(0, 0, Color.white);
            _swatchTexture.Apply();
            _swatch = Sprite.Create(_swatchTexture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1f);
        }

        private void CreateBackground()
        {
            var floor = new GameObject("Test Floor");
            floor.transform.position = new Vector3(0f, 0f, 1f);
            floor.transform.localScale = new Vector3(18f, 12f, 1f);
            var renderer = floor.AddComponent<SpriteRenderer>();
            renderer.sprite = _swatch;
            renderer.color = new Color(.11f, .14f, .18f);
            renderer.sortingOrder = -50;
        }

        private void CreateTarget(int index)
        {
            var target = new GameObject("Target " + (index + 1));
            target.transform.position = TargetPositions[index];
            var renderer = target.AddComponent<SpriteRenderer>();
            renderer.sprite = _swatch;
            renderer.color = new Color(.3f, .9f, .8f);
            renderer.sortingOrder = 3;
            target.transform.localScale = new Vector3(.4f, .4f, 1f);
            var collider = target.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            var hurtbox = target.AddComponent<Hurtbox>();
            var stats = new EnemyStats(999, 0);
            hurtbox.Init(stats, null);
            _targets.Add(stats);
            _hits.Add(0);
            int captured = index;
            int previousHp = stats.CurrentHp;
            stats.OnHpChanged += (hp, maxHp) =>
            {
                if (hp < previousHp) _hits[captured]++;
                previousHp = hp;
                renderer.color = hp < maxHp ? new Color(1f, .55f, .3f) : new Color(.3f, .9f, .8f);
            };
        }

        private void OnGUI()
        {
            const int left = 12;
            GUILayout.BeginArea(new Rect(left, 12, 300, Screen.height - 24), GUI.skin.box);
            GUILayout.Label("T1 무기 공격 비교");
            GUILayout.Label("이동 WASD · 조준 마우스 · 공격 좌클릭");
            GUILayout.Label("N 다음 · P 이전 · R 표적 초기화");
            GUILayout.Label(_message ?? "준비 중...");
            if (_player != null && _player.EquippedWeapon != null)
            {
                var motion = _player.EquippedAttackMotion;
                float forward = motion.Reach + motion.HitboxSize.x * .5f;
                GUILayout.Label($"간격 {motion.TotalDuration:0.00}s  전방 {forward:0.00}  폭 {motion.HitboxSize.y:0.00}");
            }
            for (int i = 0; i < WeaponIds.Length; i++)
            {
                string label = (i == _index ? "▶ " : "   ") + WeaponIds[i].ToUpperInvariant();
                if (GUILayout.Button(label) && _player != null) Select(i);
            }
            if (GUILayout.Button("표적 초기화 (R)") && _player != null) ResetTargets();
            for (int i = 0; i < _hits.Count; i++)
                GUILayout.Label($"표적 {i + 1}: {_hits[i]}회 적중");
            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            if (_swatch != null) Destroy(_swatch);
            if (_swatchTexture != null) Destroy(_swatchTexture);
        }
    }
}

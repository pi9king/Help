using Help.Combat;
using Help.Item;
using UnityEngine;

namespace Help.Player
{
    // 장착 무기의 월드 스프라이트를 손잡이 회전축에 붙인다. 현재는 AXE의 크기와 궤적만 조정했다.
    public class PlayerWeaponVisual : MonoBehaviour
    {
        private PlayerController _player;
        private Transform _grip;
        private SpriteRenderer _renderer;
        private AttackMotionDef _attackMotion;
        private Vector2 _attackDirection;
        private float _attackElapsed;
        private bool _attacking;
        private string _weaponId;
        private int _side = 1;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            var body = GetComponent<SpriteRenderer>();
            var grip = new GameObject("Weapon Grip");
            grip.transform.SetParent(transform, false);
            _grip = grip.transform;

            var image = new GameObject("Held Weapon");
            image.transform.SetParent(_grip, false);
            image.transform.localPosition = new Vector3(0f, .32f, 0f);
            image.transform.localScale = Vector3.one * .20f;
            _renderer = image.AddComponent<SpriteRenderer>();
            if (body != null)
            {
                _renderer.sortingLayerID = body.sortingLayerID;
                _renderer.sortingOrder = body.sortingOrder + 1;
            }
            _renderer.enabled = false;
        }

        public void Equip(ItemDefinition item)
        {
            _weaponId = item != null ? item.Id : null;
            _renderer.sprite = item != null ? item.WorldSprite : null;
            _renderer.enabled = _renderer.sprite != null;
            EndAttack();
        }

        public void BeginAttack(Vector2 direction, AttackMotionDef motion)
        {
            _attackDirection = AimGeometry.DirectionOrDefault(direction);
            _attackMotion = motion;
            _attackElapsed = 0f;
            _attacking = _renderer.enabled && _weaponId == "axe" && motion != null;
        }

        public void SetAttackElapsed(float elapsed) => _attackElapsed = elapsed;

        public void EndAttack()
        {
            _attacking = false;
            _attackMotion = null;
        }

        private void LateUpdate()
        {
            if (!_renderer.enabled || _player == null) return;
            Vector2 direction = _attacking ? _attackDirection : _player.AimDirection;
            if (direction.x > .1f) _side = 1;
            else if (direction.x < -.1f) _side = -1;

            // 아래로 휘두를 때 도끼날이 E 위를 가로지르지 않게 손잡이를 바깥쪽으로 뺀다.
            float outward = _attacking ? .12f * Mathf.Max(0f, -direction.y) * AttackEnvelope() : 0f;
            _grip.localPosition = new Vector3(_side * (.48f + outward), -.05f, 0f);
            _renderer.flipX = _side < 0;
            _grip.localRotation = Quaternion.Euler(0f, 0f,
                _attacking ? AxeSwingDegrees(_attackDirection, _attackElapsed, _attackMotion) : 0f);
        }

        private static float AxeSwingDegrees(Vector2 direction, float elapsed, AttackMotionDef motion)
        {
            if (motion == null || elapsed >= motion.TotalDuration) return 0f;
            float start = Mathf.DeltaAngle(0f, AimGeometry.AngleDegrees(direction) + 15f);
            float strike = start - 170f;
            if (elapsed < motion.Windup)
                return Mathf.Lerp(0f, start, PhaseProgress(elapsed, motion.Windup));
            elapsed -= motion.Windup;
            if (elapsed < motion.Active)
                return Mathf.Lerp(start, strike, PhaseProgress(elapsed, motion.Active));
            elapsed -= motion.Active;
            float rest = strike + Mathf.DeltaAngle(strike, 0f);
            return Mathf.Lerp(strike, rest, PhaseProgress(elapsed, motion.Recovery));
        }

        private static float PhaseProgress(float elapsed, float duration) =>
            duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;

        private float AttackEnvelope()
        {
            if (_attackMotion == null) return 0f;
            if (_attackElapsed < _attackMotion.Windup)
                return PhaseProgress(_attackElapsed, _attackMotion.Windup);
            float recoveryStart = _attackMotion.Windup + _attackMotion.Active;
            if (_attackElapsed < recoveryStart) return 1f;
            return 1f - PhaseProgress(_attackElapsed - recoveryStart, _attackMotion.Recovery);
        }
    }
}

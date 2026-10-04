using Help.Combat;
using UnityEngine;

namespace Help.Player
{
    // E 몸통과 독립된 두 팔. 어깨가 회전축이며 플레이어 루트는 회전·반전하지 않는다.
    public class PlayerArms : MonoBehaviour
    {
        private Transform _left;
        private Transform _right;
        private Sprite _armSprite;
        private Texture2D _armTexture;

        public Transform LeftArm => _left;
        public Transform RightArm => _right;

        private void Awake()
        {
            _armSprite = CreateArmSprite();
            var body = GetComponent<SpriteRenderer>();
            _left = CreateArm("Left Arm", body, -1);
            _right = CreateArm("Right Arm", body, 1);
            Rest();
        }

        public void SetAttackPose(string weaponId, Vector2 aimDirection, float elapsed, AttackMotionDef motion)
        {
            Apply(WeaponArmMotion.Evaluate(weaponId, aimDirection, elapsed, motion), aimDirection);
        }

        public void Rest() => Apply(ArmPose.Rest, Vector2.right);

        private void Apply(ArmPose pose, Vector2 aimDirection)
        {
            if (_left == null || _right == null) return;
            Vector2 forward = AimGeometry.DirectionOrDefault(aimDirection) * pose.ForwardOffset;
            _left.localPosition = new Vector3(-.18f + forward.x, .05f + forward.y, 0f);
            _right.localPosition = new Vector3(.18f + forward.x, .05f + forward.y, 0f);
            _left.localRotation = Quaternion.Euler(0f, 0f, pose.LeftDegrees);
            _right.localRotation = Quaternion.Euler(0f, 0f, pose.RightDegrees);
        }

        private Transform CreateArm(string name, SpriteRenderer body, int orderOffset)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = _armSprite;
            if (body != null) renderer.sortingLayerID = body.sortingLayerID;
            renderer.sortingOrder = (body != null ? body.sortingOrder : 0) + orderOffset;
            return go.transform;
        }

        private Sprite CreateArmSprite()
        {
            const int width = 12;
            const int height = 4;
            var pixels = new Color32[width * height];
            var outline = new Color32(20, 60, 90, 255);
            var sleeve = new Color32(79, 195, 247, 255);
            var hand = new Color32(124, 220, 255, 255);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = y == 0 || y == height - 1 || x == 0 || x == width - 1
                        ? outline : x >= width - 3 ? hand : sleeve;

            _armTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            _armTexture.filterMode = FilterMode.Point;
            _armTexture.wrapMode = TextureWrapMode.Clamp;
            _armTexture.SetPixels32(pixels);
            _armTexture.Apply();
            return Sprite.Create(_armTexture, new Rect(0, 0, width, height), new Vector2(0f, .5f), 32f);
        }

        private void OnDestroy()
        {
            if (_armSprite != null)
            {
                if (Application.isPlaying) Destroy(_armSprite);
                else DestroyImmediate(_armSprite);
            }
            if (_armTexture != null)
            {
                if (Application.isPlaying) Destroy(_armTexture);
                else DestroyImmediate(_armTexture);
            }
        }
    }
}

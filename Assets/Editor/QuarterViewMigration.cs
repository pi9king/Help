using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Help.Core;
using Help.Player;

namespace Help.EditorTools
{
    public static class QuarterViewMigration
    {
        private const string SourceScene = "Assets/Scenes/TestScene/TestScene.unity";
        private const string PrototypeScene = "Assets/Scenes/QuarterViewPrototype.unity";

        [MenuItem("Help/Setup/Apply Quarter View Migration")]
        public static void Apply()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(PrototypeScene) == null)
            {
                if (!AssetDatabase.CopyAsset(SourceScene, PrototypeScene))
                    throw new System.InvalidOperationException("쿼터뷰 프로토타입 씬을 복사하지 못했습니다.");
                AssetDatabase.Refresh();
            }

            Scene scene = EditorSceneManager.OpenScene(PrototypeScene, OpenSceneMode.Single);
            MigratePlayer();
            MigrateCamera();
            RemoveLegacyGroundObjects();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(PrototypeScene, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"[Help] 쿼터뷰 프로토타입 씬 마이그레이션 완료: {PrototypeScene}");
        }

        private static void MigratePlayer()
        {
            PlayerController controller = Object.FindFirstObjectByType<PlayerController>();
            if (controller == null) throw new System.InvalidOperationException("씬에서 PlayerController를 찾지 못했습니다.");

            GameObject player = controller.gameObject;
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb == null) rb = player.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            foreach (Collider2D collider in player.GetComponents<Collider2D>())
                Object.DestroyImmediate(collider);

            var body = player.AddComponent<CircleCollider2D>();
            body.radius = 0.34f;
            body.offset = new Vector2(0f, -0.22f);

            Transform groundCheck = player.transform.Find("GroundCheck");
            if (groundCheck != null) Object.DestroyImmediate(groundCheck.gameObject);

            foreach (SpriteRenderer renderer in player.GetComponentsInChildren<SpriteRenderer>(true))
                renderer.spriteSortPoint = SpriteSortPoint.Pivot;
        }

        private static void MigrateCamera()
        {
            CameraFollow follow = Object.FindFirstObjectByType<CameraFollow>();
            if (follow == null) return;
            var serialized = new SerializedObject(follow);
            SerializedProperty offset = serialized.FindProperty("_offset");
            if (offset != null) offset.vector3Value = new Vector3(0f, 0f, -10f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RemoveLegacyGroundObjects()
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name == "Ground" || root.name == "LegacyGround")
                    Object.DestroyImmediate(root);
            }
        }
    }
}

using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Help.Player;

namespace Help.EditorTools
{
    // 조준 분할(360° / 8방향 / 4방향)을 플레이로 비교하기 위한 1회성 셋업.
    // E 캐릭터 4방향 시트를 플레이어에 붙이고 전환 토글을 올린다.
    // 비교가 끝나면 Revert로 되돌린다.
    public static class AimTestSetup
    {
        private const string PrototypeScene = "Assets/Scenes/QuarterViewPrototype.unity";
        private const string ControllerPath = "Assets/Animations/E_Character/E_Character.controller";
        private const string BodyName = "E Body";

        [MenuItem("Help/Setup/Wire E Character For Aim Test")]
        public static void Wire()
        {
            Scene scene = EditorSceneManager.OpenScene(PrototypeScene, OpenSceneMode.Single);

            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            if (player == null) throw new System.InvalidOperationException("씬에서 PlayerController를 찾지 못했습니다.");

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null) throw new System.InvalidOperationException($"{ControllerPath} 를 찾지 못했습니다.");

            GameObject go = player.gameObject;

            // 기존 player_E 스프라이트는 그리지 않는다. 컴포넌트는 남겨 둔다(HitFlash 등이 참조).
            var rootRenderer = go.GetComponent<SpriteRenderer>();
            if (rootRenderer != null) rootRenderer.sprite = null;

            Transform bodyTf = go.transform.Find(BodyName);
            GameObject body = bodyTf != null ? bodyTf.gameObject : new GameObject(BodyName);
            body.transform.SetParent(go.transform, false);

            // 시트 피벗이 하단 중앙이므로 콜라이더 바닥에 발을 맞춘다.
            float drop = 0.5f;
            var collider = go.GetComponent<Collider2D>();
            if (collider != null) drop = collider.bounds.extents.y;
            body.transform.localPosition = new Vector3(0f, -drop, 0f);
            body.transform.localScale = Vector3.one;

            var renderer = body.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = body.AddComponent<SpriteRenderer>();
            if (rootRenderer != null)
            {
                renderer.sortingLayerID = rootRenderer.sortingLayerID;
                renderer.sortingOrder = rootRenderer.sortingOrder;
            }

            var animator = body.GetComponent<Animator>();
            if (animator == null) animator = body.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            var eAnim = body.GetComponent<ECharacterAnimator>();
            if (eAnim == null) eAnim = body.AddComponent<ECharacterAnimator>();
            SetPrivateField(eAnim, "_player", player);

            var tester = go.GetComponent<AimModeTester>();
            if (tester == null) tester = go.AddComponent<AimModeTester>();
            SetPrivateField(tester, "_player", player);

            // 비교 시작점은 현재 구현(360° 연속).
            player.AimSteps = 1;
            EditorUtility.SetDirty(player);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Help] 조준 비교 셋업 완료. Play 후 T 키로 360° / 4방향 / 8방향을 전환하세요.");
        }

        [MenuItem("Help/Setup/Revert E Character Aim Test")]
        public static void Revert()
        {
            Scene scene = EditorSceneManager.OpenScene(PrototypeScene, OpenSceneMode.Single);

            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            if (player == null) throw new System.InvalidOperationException("씬에서 PlayerController를 찾지 못했습니다.");

            GameObject go = player.gameObject;
            Transform body = go.transform.Find(BodyName);
            if (body != null) Object.DestroyImmediate(body.gameObject);

            var tester = go.GetComponent<AimModeTester>();
            if (tester != null) Object.DestroyImmediate(tester);

            var rootRenderer = go.GetComponent<SpriteRenderer>();
            if (rootRenderer != null && rootRenderer.sprite == null)
                rootRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/player_E.png");

            player.AimSteps = 1;
            EditorUtility.SetDirty(player);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Help] 조준 비교 셋업을 되돌렸습니다.");
        }

        private static void SetPrivateField(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null) throw new System.InvalidOperationException($"{target.GetType().Name}.{field} 를 찾지 못했습니다.");
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

using UnityEditor;
using UnityEngine;
using Help.Enemy;

namespace Help.Editor
{
    // LayerLab "2D Minimal - Enemy Monster2" 아트를 기존 적 프리팹에 입히는 샘플 생성기.
    //
    // 게임플레이(EnemyBase/Rigidbody2D/콜라이더/Hurtbox/드랍 테이블)는 Enemy_Grunt를 그대로
    // 복제해 쓰고, **보이는 것만** 교체한다. 전투 로직은 한 줄도 건드리지 않는다.
    //
    // 크기: 에셋은 PPU 100 기준이라 몸통이 2.4타일쯤 된다. 그대로 쓰면 LEVEL_DESIGN.md의
    // "통로 높이 2칸"에 안 들어가므로, 실제 렌더 바운즈를 재서 목표 높이로 맞춘다.
    public static class MonsterArtSetup
    {
        private const string ArtRoot =
            "Assets/Layer Lab/2D Minimal-EnemyMonster/EnemyMonster 2/Prefabs";
        private const string BasePrefab = "Assets/Prefabs/Enemy_Grunt.prefab";
        private const string OutDir = "Assets/Prefabs";

        [MenuItem("Help/Art/Create Sample Monster (Ant)")]
        public static void CreateAntSample()
        {
            Build("Ant/Ant_Worker", "Enemy_Ant", targetHeight: 1.0f);
        }

        // artPath: ArtRoot 기준 상대 경로(확장자 없음). targetHeight: 월드 유닛(=타일) 높이.
        private static void Build(string artPath, string outName, float targetHeight)
        {
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefab);
            if (basePrefab == null) { Debug.LogError($"베이스 프리팹 없음: {BasePrefab}"); return; }

            string full = $"{ArtRoot}/{artPath}.prefab";
            var art = AssetDatabase.LoadAssetAtPath<GameObject>(full);
            if (art == null) { Debug.LogError($"아트 프리팹 없음: {full}"); return; }

            var root = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = outName;

            // 기존 placeholder 사각형 제거 (루트에 붙어 있다)
            var placeholder = root.GetComponent<SpriteRenderer>();
            if (placeholder != null) Object.DestroyImmediate(placeholder);

            var visual = (GameObject)PrefabUtility.InstantiatePrefab(art, root.transform);
            PrefabUtility.UnpackPrefabInstance(visual, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;

            FitToHeight(visual, targetHeight, footY: -0.5f); // 1×1 콜라이더의 바닥선

            // Animator가 자식에 있으므로 EnemyBase가 Awake에서 자동 부착하지만,
            // 프리팹에 미리 박아두면 인스펙터에서 존재가 보인다.
            if (root.GetComponent<EnemyAnimatorDriver>() == null)
                root.AddComponent<EnemyAnimatorDriver>();

            string outPath = $"{OutDir}/{outName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, outPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            Debug.Log($"[MonsterArtSetup] {outPath} 생성 — 아트 {artPath}, 높이 {targetHeight}타일");
        }

        // 활성 SpriteRenderer들의 합집합 바운즈를 재서 목표 높이로 스케일하고,
        // 발이 footY에 닿도록 내린다. 몬스터마다 원본 크기가 달라 수치를 손으로 못 박는다.
        //
        // 비활성 렌더러는 제외한다 — 이 에셋은 Hit/Attack/Dead 스프라이트를 자식으로 두고
        // 애니메이션(m_IsActive)으로 껐다 켠다. 꺼진 렌더러의 bounds는 원점의 빈 상자라
        // 포함시키면 측정이 원점 쪽으로 늘어나 크기가 엉뚱해진다.
        private static void FitToHeight(GameObject visual, float targetHeight, float footY)
        {
            if (!TryMeasure(visual, out var bounds)) return;
            if (bounds.size.y <= 0.0001f) return;

            visual.transform.localScale = Vector3.one * (targetHeight / bounds.size.y);

            // 스케일이 반영된 바운즈를 다시 재서 바닥을 맞춘다.
            if (!TryMeasure(visual, out var scaled)) return;
            visual.transform.localPosition += new Vector3(0f, footY - scaled.min.y, 0f);
        }

        private static bool TryMeasure(GameObject visual, out Bounds bounds)
        {
            bounds = default;
            bool any = false;
            foreach (var r in visual.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!r.gameObject.activeInHierarchy || r.sprite == null) continue;
                if (!any) { bounds = r.bounds; any = true; }
                else bounds.Encapsulate(r.bounds);
            }
            return any;
        }
    }
}

using System.Collections.Generic;
using Help.Combat;
using Help.Item;
using Help.Crafting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Help.Editor
{
    public static class SetupGameAssets
    {
        [MenuItem("Help/Setup/Create All Game Assets")]
        public static void CreateAll()
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                EnsureFolder("Assets/ScriptableObjects");
                EnsureFolder("Assets/ScriptableObjects/Materials");
                EnsureFolder("Assets/ScriptableObjects/Items");
                EnsureFolder("Assets/ScriptableObjects/ItemsSpecial");

                var matItems = CreateMaterials();
                var weaponItems = CreateWeapons();
                weaponItems.AddRange(CreateSpecialItems());
                CreateRecipeDatabase(matItems, weaponItems);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log("[Help] All game assets created successfully.");
        }

        static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parts = path.Split('/');
                var current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    var next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(current, parts[i]);
                    current = next;
                }
            }
        }

        static List<ItemDefinition> CreateMaterials()
        {
            var list = new List<ItemDefinition>();
            var allMats = System.Enum.GetValues(typeof(AlphabetMaterial));
            foreach (AlphabetMaterial mat in allMats)
            {
                string path = $"Assets/ScriptableObjects/Materials/mat_{mat}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
                if (existing != null) { list.Add(existing); continue; }

                var item = ScriptableObject.CreateInstance<ItemDefinition>();
                item.Id = $"mat_{mat}";
                item.Word = mat.ToString();
                item.Type = ItemType.Material;
                item.Element = ElementType.None;
                item.AttackBonus = 0;
                item.DefenseBonus = 0;
                item.AttackSpeedMult = 1f;
                item.Recipe = new List<MaterialRequirement> { new MaterialRequirement(mat, 1) };
                AssetDatabase.CreateAsset(item, path);
                list.Add(item);
            }
            return list;
        }

        // (AlphabetMaterial letter, int count)
        static (AlphabetMaterial, int)[] Req(params (AlphabetMaterial, int)[] r) => r;

        static List<ItemDefinition> CreateWeapons()
        {
            var list = new List<ItemDefinition>();

            list.Add(MakeWeapon("blade",  "BLADE",  WeaponCategory.Blade,  ElementType.Steel,
                Req((AlphabetMaterial.B,1),(AlphabetMaterial.L,1),(AlphabetMaterial.A,1),(AlphabetMaterial.D,1)),
                attackBonus: 8));

            list.Add(MakeWeapon("spear",  "SPEAR",  WeaponCategory.Spear,  ElementType.Spike,
                Req((AlphabetMaterial.S,1),(AlphabetMaterial.P,1),(AlphabetMaterial.A,1),(AlphabetMaterial.R,1)),
                attackBonus: 7));

            list.Add(MakeWeapon("axe",    "AXE",    WeaponCategory.Axe,    ElementType.Stone,
                Req((AlphabetMaterial.A,1),(AlphabetMaterial.X,1)),
                attackBonus: 10, capabilities: new[] { Capability.BreakWall }));

            list.Add(MakeWeapon("knife",  "KNIFE",  WeaponCategory.Knife,  ElementType.Steel,
                Req((AlphabetMaterial.K,1),(AlphabetMaterial.N,1),(AlphabetMaterial.I,1),(AlphabetMaterial.F,1)),
                attackBonus: 5, attackSpeedMult: 1.4f));

            list.Add(MakeWeapon("rapier", "RAPIER", WeaponCategory.Rapier, ElementType.Pulse,
                Req((AlphabetMaterial.R,2),(AlphabetMaterial.A,1),(AlphabetMaterial.P,1),(AlphabetMaterial.I,1)),
                attackBonus: 6, attackSpeedMult: 1.3f));

            list.Add(MakeWeapon("saber",  "SABER",  WeaponCategory.Saber,  ElementType.Fire,
                Req((AlphabetMaterial.S,1),(AlphabetMaterial.A,1),(AlphabetMaterial.B,1),(AlphabetMaterial.R,1)),
                attackBonus: 7, attackSpeedMult: 1.1f));

            // 서브무기(유틸): FLARE = 사용(use)으로 Melt 능력 적용(얼음벽 녹이기 등). 레시피 FLARE−E = F,L,A,R.
            list.Add(MakeSubWeapon("flare", "FLARE", ElementType.None,
                Req((AlphabetMaterial.F,1),(AlphabetMaterial.L,1),(AlphabetMaterial.A,1),(AlphabetMaterial.R,1)),
                new[] { Capability.Melt }, attackBonus: 3));

            // 서브무기(유틸): ROPE = 사용(use)으로 적 속박(Bind) + 앵커에 걸어 틈 건너기(CrossGap).
            // 능력 2개 = 전투/퍼즐 양쪽에 관여. 레시피 ROPE−E = R,O,P.
            list.Add(MakeSubWeapon("rope", "ROPE", ElementType.None,
                Req((AlphabetMaterial.R,1),(AlphabetMaterial.O,1),(AlphabetMaterial.P,1)),
                new[] { Capability.CrossGap, Capability.Bind }, attackBonus: 2));

            // 서브무기(유틸): KEY = 사용(use)으로 잠긴 문 열기(Unlock). 튜토리얼 첫 관문.
            // 레시피 KEY−E = K,Y (주울 글자 2개로 가장 간결한 첫 제작).
            list.Add(MakeSubWeapon("key", "KEY", ElementType.None,
                Req((AlphabetMaterial.K,1),(AlphabetMaterial.Y,1)),
                new[] { Capability.Unlock }, attackBonus: 0));

            // 소모품: ELIXIR = 몬스터가 떨구는 회복 포션. 레시피가 없어 제작되지 않고 드랍으로만 얻는다
            // (AlphabetWordRule.IsBasicCraftable이 레시피 없는 아이템을 제작 대상에서 제외).
            list.Add(MakePotion("elixir", "ELIXIR", healAmount: 30));

            return list;
        }

        // 특수 아이템: E가 들어가지 않은 단어 — 기본 제작으로는 절대 나오지 않고,
        // 특수방의 상자(획득)나 특수 제작대(골드 비용)로만 손에 들어온다.
        // 기본 제작물보다 성능이 좋아야 특수방을 만난 값어치가 있다.
        // 에셋을 Items가 아닌 ItemsSpecial 폴더에 두어 "단어 규칙 검사 대상"과 구분한다.
        static List<ItemDefinition> CreateSpecialItems()
        {
            var list = new List<ItemDefinition>();

            list.Add(MakeSpecial("sword", "SWORD", ItemType.Weapon, WeaponCategory.Blade,
                Req((AlphabetMaterial.S,1),(AlphabetMaterial.W,1),(AlphabetMaterial.O,1),(AlphabetMaterial.R,1),(AlphabetMaterial.D,1)),
                attackBonus: 14, defenseBonus: 0, goldCost: 40));

            list.Add(MakeSpecial("armor", "ARMOR", ItemType.BodyArmor, WeaponCategory.None,
                Req((AlphabetMaterial.A,1),(AlphabetMaterial.R,2),(AlphabetMaterial.M,1),(AlphabetMaterial.O,1)),
                attackBonus: 0, defenseBonus: 8, goldCost: 35));

            list.Add(MakeSpecial("club", "CLUB", ItemType.Weapon, WeaponCategory.Axe,
                Req((AlphabetMaterial.C,1),(AlphabetMaterial.L,1),(AlphabetMaterial.U,1),(AlphabetMaterial.B,1)),
                attackBonus: 11, defenseBonus: 0, goldCost: 25));

            return list;
        }

        static ItemDefinition MakeSpecial(string id, string word, ItemType type, WeaponCategory cat,
            (AlphabetMaterial, int)[] reqs, int attackBonus, int defenseBonus, int goldCost)
        {
            string path = $"Assets/ScriptableObjects/ItemsSpecial/{id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (existing != null)
            {
                existing.GoldCost = goldCost;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.Id = id;
            item.Word = word;
            item.Type = type;
            item.Element = ElementType.None;
            item.WeaponCategory = cat;
            item.AttackBonus = attackBonus;
            item.DefenseBonus = defenseBonus;
            item.GoldCost = goldCost;
            item.Recipe = new System.Collections.Generic.List<MaterialRequirement>();
            foreach (var (mat, cnt) in reqs)
                item.Recipe.Add(new MaterialRequirement { Material = mat, Count = cnt });

            AssetDatabase.CreateAsset(item, path);
            return item;
        }

        static ItemDefinition MakePotion(string id, string word, int healAmount)
        {
            string path = $"Assets/ScriptableObjects/Items/{id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (existing != null)
            {
                existing.Type = ItemType.Consumable;
                existing.HealAmount = healAmount;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.Id = id;
            item.Word = word;
            item.Type = ItemType.Consumable;
            item.Element = ElementType.None;
            item.WeaponCategory = WeaponCategory.None;
            item.HealAmount = healAmount;
            item.Recipe = new System.Collections.Generic.List<MaterialRequirement>(); // 제작 불가(드랍 전용)

            AssetDatabase.CreateAsset(item, path);
            return item;
        }

        static ItemDefinition MakeSubWeapon(string id, string word, ElementType elem,
            (AlphabetMaterial, int)[] reqs, Capability[] capabilities, int attackBonus = 3)
        {
            string path = $"Assets/ScriptableObjects/Items/{id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (existing != null)
            {
                existing.Type = ItemType.SubWeapon;
                SetCapabilities(existing, capabilities);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.Id = id;
            item.Word = word;
            item.Type = ItemType.SubWeapon;
            item.Element = elem;
            item.WeaponCategory = WeaponCategory.None;
            item.AttackBonus = attackBonus;
            SetCapabilities(item, capabilities);
            item.Recipe = new System.Collections.Generic.List<MaterialRequirement>();
            foreach (var (mat, cnt) in reqs)
                item.Recipe.Add(new MaterialRequirement { Material = mat, Count = cnt });

            AssetDatabase.CreateAsset(item, path);
            return item;
        }

        static ItemDefinition MakeWeapon(string id, string word, WeaponCategory cat, ElementType elem,
            (AlphabetMaterial, int)[] reqs, int attackBonus = 5, int defenseBonus = 0, float attackSpeedMult = 1f,
            Capability[] capabilities = null)
        {
            string path = $"Assets/ScriptableObjects/Items/{id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (existing != null)
            {
                // 기존 에셋엔 신규 필드(능력)만 보강해 재실행 시 최신화
                SetCapabilities(existing, capabilities);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.Id = id;
            item.Word = word;
            item.Type = ItemType.Weapon;
            item.Element = elem;
            item.WeaponCategory = cat;
            item.AttackBonus = attackBonus;
            item.DefenseBonus = defenseBonus;
            item.AttackSpeedMult = attackSpeedMult;
            SetCapabilities(item, capabilities);
            item.Recipe = new System.Collections.Generic.List<MaterialRequirement>();
            foreach (var (mat, cnt) in reqs)
                item.Recipe.Add(new MaterialRequirement { Material = mat, Count = cnt });

            AssetDatabase.CreateAsset(item, path);
            return item;
        }

        static void SetCapabilities(ItemDefinition item, Capability[] capabilities)
        {
            item.Capabilities = new System.Collections.Generic.List<Capability>();
            if (capabilities != null) item.Capabilities.AddRange(capabilities);
        }

        static void CreateRecipeDatabase(List<ItemDefinition> materials, List<ItemDefinition> weapons)
        {
            string path = "Assets/ScriptableObjects/RecipeDatabase.asset";
            var db = AssetDatabase.LoadAssetAtPath<RecipeDatabase>(path);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<RecipeDatabase>();
                AssetDatabase.CreateAsset(db, path);
            }

            var so = new SerializedObject(db);
            var itemsProp = so.FindProperty("_items");
            var matsProp  = so.FindProperty("_materials");

            itemsProp.ClearArray();
            for (int i = 0; i < weapons.Count; i++)
            {
                itemsProp.InsertArrayElementAtIndex(i);
                itemsProp.GetArrayElementAtIndex(i).objectReferenceValue = weapons[i];
            }

            matsProp.ClearArray();
            for (int i = 0; i < materials.Count; i++)
            {
                matsProp.InsertArrayElementAtIndex(i);
                matsProp.GetArrayElementAtIndex(i).objectReferenceValue = materials[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 기존 레시피 목록형 CraftingUI를 슬롯 배치형 CraftingBenchUI로 교체한다.
        // Canvas의 CraftingUI 컴포넌트를 제거하고 CraftingBenchUI를 붙여 _panel을 배선,
        // 옛 RecipeListParent는 비활성화한다(배경 패널은 그대로 유지).
        [MenuItem("Help/Setup/Switch To Slot Crafting UI")]
        public static void SwitchToSlotCraftingUI()
        {
            var scene = SceneManager.GetActiveScene();

            var craftingUI = Object.FindFirstObjectByType<Help.UI.CraftingUI>(FindObjectsInactive.Include);
            GameObject host = craftingUI != null ? craftingUI.gameObject : null;

            GameObject panel = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                var f = FindDeep(root.transform, "CraftingPanel");
                if (f != null) { panel = f.gameObject; break; }
            }

            if (host == null || panel == null)
            {
                Debug.LogError("[Help] CraftingUI 또는 CraftingPanel을 찾지 못했습니다.");
                return;
            }

            // 옛 레시피 목록 컨테이너 비활성화
            var list = FindDeep(panel.transform, "RecipeListParent");
            if (list != null) list.gameObject.SetActive(false);

            // 옛 CraftingUI 제거 → CraftingBenchUI 부착
            Object.DestroyImmediate(craftingUI);
            var bench = host.GetComponent<Help.UI.CraftingBenchUI>();
            if (bench == null) bench = host.AddComponent<Help.UI.CraftingBenchUI>();

            var so = new SerializedObject(bench);
            so.FindProperty("_panel").objectReferenceValue = panel;
            so.FindProperty("_slotCount").intValue = 6;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Help] 슬롯 배치형 크래프팅 UI로 교체 완료.");
        }

        // 세로 나열형 InventoryUI를 그리드형 InventoryGridUI로 교체한다.
        [MenuItem("Help/Setup/Switch To Grid Inventory UI")]
        public static void SwitchToGridInventoryUI()
        {
            var scene = SceneManager.GetActiveScene();

            var invUI = Object.FindFirstObjectByType<Help.UI.InventoryUI>(FindObjectsInactive.Include);
            GameObject host = invUI != null ? invUI.gameObject : null;

            GameObject panel = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                var f = FindDeep(root.transform, "InventoryPanel");
                if (f != null) { panel = f.gameObject; break; }
            }

            if (host == null || panel == null)
            {
                Debug.LogError("[Help] InventoryUI 또는 InventoryPanel을 찾지 못했습니다.");
                return;
            }

            var list = FindDeep(panel.transform, "ItemListParent");
            if (list != null) list.gameObject.SetActive(false);

            Object.DestroyImmediate(invUI);
            var grid = host.GetComponent<Help.UI.InventoryGridUI>();
            if (grid == null) grid = host.AddComponent<Help.UI.InventoryGridUI>();

            var so = new SerializedObject(grid);
            so.FindProperty("_panel").objectReferenceValue = panel;
            so.ApplyModifiedPropertiesWithoutUndo();

            // HUD 인벤토리 버튼을 새 그리드 UI로 재배선(옛 InventoryUI 참조가 끊어지므로)
            var hud = Object.FindFirstObjectByType<Help.UI.HUD>(FindObjectsInactive.Include);
            if (hud != null)
            {
                var hso = new SerializedObject(hud);
                var prop = hso.FindProperty("_inventoryUI");
                if (prop != null) prop.objectReferenceValue = grid;
                hso.ApplyModifiedProperties();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Help] 그리드형 인벤토리 UI로 교체 완료.");
        }

        static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform c in t)
            {
                var r = FindDeep(c, name);
                if (r != null) return r;
            }
            return null;
        }

        [MenuItem("Help/Setup/Assign RecipeDatabase to Scene GameManager")]
        public static void AssignRecipeDatabase()
        {
            string dbPath = "Assets/ScriptableObjects/RecipeDatabase.asset";
            var db = AssetDatabase.LoadAssetAtPath<RecipeDatabase>(dbPath);
            if (db == null)
            {
                Debug.LogError("[Help] RecipeDatabase not found. Run 'Create All Game Assets' first.");
                return;
            }

            var gm = Object.FindFirstObjectByType<Help.Core.GameManager>();
            if (gm == null)
            {
                Debug.LogError("[Help] GameManager not found in current scene.");
                return;
            }

            var so = new SerializedObject(gm);
            so.FindProperty("_recipeDatabase").objectReferenceValue = db;
            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            Debug.Log("[Help] RecipeDatabase assigned to GameManager.");
        }
    }
}

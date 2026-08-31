using UnityEditor;
using UnityEngine;
using Help.Enemy;
using Help.Item;

namespace Help.Editor
{
    // 베이스 Enemy.prefab에서 3종 적 프리팹을 파생 생성한다:
    // 그런트(근접 기본) / 아처(원거리 투사체·카이팅) / 브루트(둔중 탱커·강타).
    // 각 유형은 스탯·색·크기·드랍 테이블이 다르다. ('Create Building Block Prefabs'로 Enemy.prefab이 먼저 있어야 함)
    public static class EnemyTypeSetup
    {
        const string Dir = "Assets/Prefabs";

        struct Reward { public RewardKind kind; public int amount; public float chance; }

        class Cfg
        {
            public string name;
            public int hp, atk;
            public float speed, aggro, deAggro, attackRange, patrolHalf, cooldown, windup, standoff, knockback, scale;
            public EnemyArchetype archetype;
            public Color color;
            // 알파벳 드랍은 프리팹에 박지 않는다 — 층 드랍 테이블(FloorLootPlan)이 정한 몫을
            // RoomManager가 방 로드 시 배분해 주입한다. 여기 있는 건 재화(골드/포션) 보상뿐이다.
            public Reward[] rewards;
            public bool isBoss;
        }

        [MenuItem("Help/Setup/Create Enemy Type Prefabs")]
        public static void CreateAll()
        {
            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Dir}/Enemy.prefab");
            if (basePrefab == null)
            {
                Debug.LogError("[Help] Enemy.prefab 없음 — 'Create Building Block Prefabs' 먼저 실행.");
                return;
            }

            var cfgs = new[]
            {
                new Cfg {
                    name = "Enemy_Grunt", archetype = EnemyArchetype.Melee,
                    hp = 30, atk = 5, speed = 2.6f, aggro = 5f, deAggro = 7f, attackRange = 1.3f,
                    patrolHalf = 2f, cooldown = 1f, windup = 0.4f, standoff = 0f, knockback = 5f, scale = 1f,
                    color = new Color(1f, 0.55f, 0.5f),
                    rewards = new[] {
                        new Reward { kind = RewardKind.Gold, amount = 5, chance = 0.6f },
                    },
                },
                new Cfg {
                    name = "Enemy_Archer", archetype = EnemyArchetype.Ranged,
                    hp = 20, atk = 4, speed = 2.2f, aggro = 8f, deAggro = 10f, attackRange = 6f,
                    patrolHalf = 2f, cooldown = 1.4f, windup = 0.5f, standoff = 4f, knockback = 6f, scale = 1f,
                    color = new Color(0.5f, 1f, 0.7f),
                    rewards = new[] {
                        new Reward { kind = RewardKind.Gold, amount = 8, chance = 0.55f },
                        new Reward { kind = RewardKind.Potion, amount = 1, chance = 0.1f },
                    },
                },
                new Cfg {
                    name = "Enemy_Brute", archetype = EnemyArchetype.Melee,
                    hp = 90, atk = 14, speed = 1.4f, aggro = 5f, deAggro = 7f, attackRange = 1.7f,
                    patrolHalf = 1f, cooldown = 1.6f, windup = 0.65f, standoff = 0f, knockback = 2.5f, scale = 1.5f,
                    color = new Color(0.75f, 0.3f, 0.3f),
                    rewards = new[] {
                        new Reward { kind = RewardKind.Gold, amount = 16, chance = 0.85f },
                        new Reward { kind = RewardKind.Potion, amount = 1, chance = 0.25f },
                    },
                },
                // 보스 = 강화 브루트: 높은 HP·데미지, 큰 몸집, 느리지만 강한 강타. 보상은 선택형(별도).
                new Cfg {
                    name = "Enemy_Boss", archetype = EnemyArchetype.Melee, isBoss = true,
                    hp = 320, atk = 22, speed = 1.7f, aggro = 9f, deAggro = 12f, attackRange = 2.2f,
                    patrolHalf = 0f, cooldown = 1.8f, windup = 0.75f, standoff = 0f, knockback = 1.2f, scale = 2.4f,
                    color = new Color(0.55f, 0.15f, 0.4f),
                    rewards = new[] {
                        new Reward { kind = RewardKind.Gold, amount = 70, chance = 1f },
                        new Reward { kind = RewardKind.Potion, amount = 1, chance = 1f },
                    },
                },
            };

            NormalizeBasePrefab(basePrefab);
            foreach (var cfg in cfgs) BuildOne(basePrefab, cfg);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Help] Enemy type prefabs created: Enemy_Grunt / Enemy_Archer / Enemy_Brute.");
        }

        // 베이스 Enemy.prefab은 씬에서 저장된 물건이라 옛 알파벳 드랍이 굳어 있을 수 있다.
        // 파생 원본이므로 여기서 정규화한다 — 알파벳은 비우고(층 예산 전용), 최소 재화 보상만 남긴다.
        static void NormalizeBasePrefab(GameObject basePrefab)
        {
            var eb = basePrefab.GetComponent<EnemyBase>();
            if (eb == null) return;

            var so = new SerializedObject(eb);
            so.FindProperty("_drops").arraySize = 0;

            var rewards = so.FindProperty("_rewardDrops");
            if (rewards.arraySize == 0)
            {
                rewards.arraySize = 1;
                var el = rewards.GetArrayElementAtIndex(0);
                el.FindPropertyRelative("Kind").enumValueIndex = (int)RewardKind.Gold;
                el.FindPropertyRelative("Amount").intValue = 4;
                el.FindPropertyRelative("Chance").floatValue = 0.5f;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(basePrefab);
        }

        static void BuildOne(GameObject basePrefab, Cfg cfg)
        {
            var inst = (GameObject)Object.Instantiate(basePrefab); // 원본과 분리된 복제본
            inst.name = cfg.name;
            inst.transform.localScale = Vector3.one * cfg.scale;

            var sr = inst.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.color = cfg.color;

            var eb = inst.GetComponent<EnemyBase>();
            if (eb != null)
            {
                var so = new SerializedObject(eb);
                SetInt(so, "_maxHp", cfg.hp);
                SetInt(so, "_attackPower", cfg.atk);
                SetFloat(so, "_moveSpeed", cfg.speed);
                SetFloat(so, "_aggroRange", cfg.aggro);
                SetFloat(so, "_deAggroRange", cfg.deAggro);
                SetFloat(so, "_attackRange", cfg.attackRange);
                SetFloat(so, "_patrolHalfWidth", cfg.patrolHalf);
                SetFloat(so, "_attackCooldown", cfg.cooldown);
                SetFloat(so, "_attackWindup", cfg.windup);
                SetFloat(so, "_standoffRange", cfg.standoff);
                SetFloat(so, "_knockbackForce", cfg.knockback);
                SetEnum(so, "_archetype", (int)cfg.archetype);
                // 기본 적은 속성 잠금 없음(DESIGN.md). 베이스 프리팹에 잠금이 남아 있어도
                // 파생 시 반드시 지운다 — 예전에 데모용 Fire 잠금이 여기로 전파돼
                // 일치 속성 무기 외 데미지가 ×0.1로 깎이는 사고가 있었다.
                SetEnum(so, "_lockedElement", (int)Help.Combat.ElementType.None);
                var bossProp = so.FindProperty("_isBoss");
                if (bossProp != null) bossProp.boolValue = cfg.isBoss;

                // 알파벳 드랍은 비운다 — 층 예산에서 배분받은 것만 런타임에 주입된다(AddGuaranteedDrop)
                so.FindProperty("_drops").arraySize = 0;

                var rewards = so.FindProperty("_rewardDrops");
                rewards.arraySize = cfg.rewards.Length;
                for (int i = 0; i < cfg.rewards.Length; i++)
                {
                    var el = rewards.GetArrayElementAtIndex(i);
                    el.FindPropertyRelative("Kind").enumValueIndex = (int)cfg.rewards[i].kind;
                    el.FindPropertyRelative("Amount").intValue = cfg.rewards[i].amount;
                    el.FindPropertyRelative("Chance").floatValue = cfg.rewards[i].chance;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(inst, $"{Dir}/{cfg.name}.prefab");
            Object.DestroyImmediate(inst);
            Debug.Log($"[Help] saved prefab: {Dir}/{cfg.name}.prefab");
        }

        static void SetInt(SerializedObject so, string n, int v) { var p = so.FindProperty(n); if (p != null) p.intValue = v; }
        static void SetFloat(SerializedObject so, string n, float v) { var p = so.FindProperty(n); if (p != null) p.floatValue = v; }
        static void SetEnum(SerializedObject so, string n, int i) { var p = so.FindProperty(n); if (p != null) p.enumValueIndex = i; }
    }
}

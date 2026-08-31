using System.Collections.Generic;
using System.Linq;
using Help.Combat;
using Help.Crafting;
using Help.Item;

namespace Help.Dungeon
{
    // 던전 생성 검증: "자유 입장 방에서 얻는 재료만으로 모든 조건부 방을 진입/클리어할 수 있는가"
    // (DESIGN.md 재료 보장 불변식). 생성 파이프라인의 마지막 단계에서 사용.
    //
    // 무기는 재사용 "열쇠"이므로 같은 요구를 가진 방들은 무기 1자루로 커버되고(중복 제거),
    // 서로 다른 요구는 각각 무기를 제작하며 재료를 실제로 소모한다. 이렇게 해야
    // 서로 다른 무기가 공유 재료를 놓고 경쟁할 때 클리어 불가능한 층을 걸러낼 수 있다.
    //
    // 주의: 제작 대상 선택이 탐욕적(조건 만족 + 최소 재료)이라, 드문 배치에서는 실제로는
    // 가능한 층을 불가능으로 판정(false-negative)할 수 있다. 생성 검증에서는 재생성을 유도하므로
    // 안전한 방향이며, 클리어 불가능한 층을 통과시키는 false-positive는 제거된다.
    public static class FloorValidator
    {
        public static bool Validate(DungeonMap map, RecipeDatabase database)
        {
            var pool = BuildFreeRoomPool(map);
            return TrySelectKeyItems(AllConditions(map), database, pool, out _);
        }

        // 도달 가능한 자유 방들의 확정 지급 재료를 합산한다 (조건부 방 루트는 제외).
        // 잠긴 방 뒤에 갇힌 자유 방의 재료는 세지 않는다 → 교착(재료가 열쇠 뒤에 있는 층) 방지.
        public static Dictionary<AlphabetMaterial, int> BuildFreeRoomPool(DungeonMap map)
        {
            var pool = new Dictionary<AlphabetMaterial, int>();
            foreach (var room in ReachableFreeRooms(map))
                foreach (var loot in room.GuaranteedLoot)
                    Add(pool, loot.Material, loot.Count);
            return pool;
        }

        // 단순/보수적 도달성: start에서 "자유 방만" 거쳐 도달 가능한 자유 방들.
        // 잠긴 방은 통과하지 못한다(아직 열쇠가 없다고 본다). 생성기가 루트 배치 대상으로도 사용.
        public static List<Room> ReachableFreeRooms(DungeonMap map)
        {
            var result = new List<Room>();
            var start = map.GetRoom(map.StartPosition.x, map.StartPosition.y);
            if (start == null || !start.IsFreeEntry) return result;

            var seen = new HashSet<(int, int)> { (start.X, start.Y) };
            var queue = new Queue<Room>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var room = queue.Dequeue();
                result.Add(room);
                foreach (var n in NeighborsOf(room))
                {
                    if (n == null) continue;
                    var nr = map.GetRoom(n.Value.x, n.Value.y);
                    if (nr == null || !nr.IsFreeEntry) continue; // 잠긴 방은 통과 불가
                    if (seen.Add((nr.X, nr.Y))) queue.Enqueue(nr);
                }
            }
            return result;
        }

        private static IEnumerable<(int x, int y)?> NeighborsOf(Room r)
        {
            yield return r.North;
            yield return r.South;
            yield return r.East;
            yield return r.West;
        }

        // 조건 집합을 만족시키는 데 필요한 최소 재료 풀을 계산한다 (생성기가 루트 배치에 사용).
        // 조건 중 하나라도 만족시킬 제작 아이템이 DB에 없으면 false.
        public static bool TryPlanRequiredMaterials(
            IEnumerable<EntryCondition> conditions,
            RecipeDatabase database,
            out Dictionary<AlphabetMaterial, int> pool)
            => TryPlanRequiredMaterials(conditions, database, out pool, out _);

        // 열쇠 아이템 목록까지 돌려주는 오버로드 — 층 드랍 테이블(FloorLootPlan)이
        // "이 층에서 만들 수 있는 아이템 N개"를 고를 때 열쇠를 먼저 포함시키는 데 쓴다.
        public static bool TryPlanRequiredMaterials(
            IEnumerable<EntryCondition> conditions,
            RecipeDatabase database,
            out Dictionary<AlphabetMaterial, int> pool,
            out List<ItemDefinition> keyItems)
        {
            pool = new Dictionary<AlphabetMaterial, int>();
            if (!TrySelectKeyItems(conditions, database, null, out keyItems)) return false;

            foreach (var item in keyItems)
                foreach (var req in item.Recipe)
                    Add(pool, req.Material, req.Count);
            return true;
        }

        // 모든 조건을 커버하는 열쇠 아이템 집합을 고른다.
        // budget == null: 제약 없음(계획 모드) — 조건을 만족하는 아이템이 있으면 무조건 선택.
        // budget != null: 해당 재료 풀에서 제작 가능한 것만 선택하고, 선택 시 재료를 소비.
        private static bool TrySelectKeyItems(
            IEnumerable<EntryCondition> conditions,
            RecipeDatabase database,
            Dictionary<AlphabetMaterial, int> budget,
            out List<ItemDefinition> chosen)
        {
            chosen = new List<ItemDefinition>();
            var working = budget == null ? null : new Dictionary<AlphabetMaterial, int>(budget);
            var crafting = budget == null ? null : new CraftingSystem(database);

            // 제약이 많은(원소+무기 모두 요구) 조건부터 처리해 재료 낭비를 줄인다.
            var ordered = Distinct(conditions).OrderByDescending(ConstraintCount);

            foreach (var cond in ordered)
            {
                // 이미 고른 무기(열쇠)로 커버되면 재사용 — 추가 재료 불필요
                if (chosen.Any(item => Matches(cond, item))) continue;

                ItemDefinition candidate = working == null
                    ? database.AllItems
                        .Where(item => Matches(cond, item))
                        .OrderBy(TotalRecipeCost).FirstOrDefault()
                    : database.AllItems
                        .Where(item => Matches(cond, item) && crafting.CanCraft(item.Id, working))
                        .OrderBy(TotalRecipeCost).FirstOrDefault();

                if (candidate == null) return false;

                if (working != null)
                    foreach (var req in candidate.Recipe)
                        working[req.Material] -= req.Count;
                chosen.Add(candidate);
            }
            return true;
        }

        private static IEnumerable<EntryCondition> AllConditions(DungeonMap map)
        {
            foreach (var room in map.ConditionalRooms())
                foreach (var cond in room.EntryConditions)
                    yield return cond;
        }

        // 원소+무기 조합이 같은 조건은 하나로 취급 (무기 1자루로 재사용).
        private static List<EntryCondition> Distinct(IEnumerable<EntryCondition> conditions)
        {
            var result = new List<EntryCondition>();
            foreach (var cond in conditions)
            {
                if (!result.Any(c => c.RequiredElement == cond.RequiredElement
                                  && c.RequiredWeapon == cond.RequiredWeapon))
                    result.Add(cond);
            }
            return result;
        }

        private static bool Matches(EntryCondition cond, ItemDefinition item)
        {
            // 기본 제작 대상(E 포함 단어)만 열쇠가 될 수 있다 — 만들 수 없는 아이템을 열쇠로 계획하면 클리어 불가 층이 된다.
            if (!AlphabetWordRule.IsBasicCraftable(item)) return false;
            if (cond.RequiredElement != ElementType.None && item.Element != cond.RequiredElement) return false;
            if (cond.RequiredWeapon != WeaponCategory.None && item.WeaponCategory != cond.RequiredWeapon) return false;
            return true;
        }

        private static int ConstraintCount(EntryCondition cond) =>
            (cond.RequiredElement != ElementType.None ? 1 : 0) +
            (cond.RequiredWeapon != WeaponCategory.None ? 1 : 0);

        private static int TotalRecipeCost(ItemDefinition item) =>
            item.Recipe.Sum(r => r.Count);

        private static void Add(Dictionary<AlphabetMaterial, int> dict, AlphabetMaterial mat, int count)
        {
            dict.TryGetValue(mat, out int cur);
            dict[mat] = cur + count;
        }
    }
}

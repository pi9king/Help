using System;
using System.Collections.Generic;
using System.Linq;
using Help.Combat;
using Help.Crafting;
using Help.Item;

namespace Help.Dungeon
{
    // 순수 C# — MonoBehaviour 의존 없음 (EditMode 테스트 가능)
    public class DungeonGenerator
    {
        private const int MaxAttempts = 30;

        // 층에 뿌리는 알파벳 총량 = 아이템 이만큼을 만들 수 있는 분량.
        // 글자를 무한정 주면 "무엇을 만들지" 고르는 재미가 사라지므로 예산으로 묶는다.
        public const int FloorRecipeCount = 3;

        // 층당 조건부(잠긴) 방 상한. 예산 3개 중 열쇠가 최대 2개까지만 차지하게 해
        // 최소 1개는 플레이어가 자유롭게 고르는 몫으로 남긴다.
        public const int MaxConditionalRooms = 2;

        // 층에 특수방이 등장할 확률. 만나는 것 자체가 행운인 보너스 방이라 항상 나오지는 않는다.
        public const double SecretRoomChance = 0.4;


        private Random _rng;

        // 조건/루트 없는 순수 레이아웃 (하위호환)
        public DungeonMap Generate(DungeonConfig config)
        {
            int seed = config.Seed < 0 ? DrawSeed() : config.Seed;
            _rng = new Random(seed);
            var map = BuildLayout(config);
            map.Seed = seed;
            return map;
        }

        // 랜덤 런에서도 실제로 쓴 시드를 남기기 위해, new Random() 대신 시드를 먼저 뽑는다.
        private static int DrawSeed() => Guid.NewGuid().GetHashCode() & 0x3FFFFFFF;

        // 진입 조건 + 재료 보장 불변식까지 만족하는 층 생성.
        // 조건부 방에 (제작 가능한 능력에서) 조건을 부여하고, 자유 방에 필요 재료를 배치한 뒤
        // FloorValidator로 검증한다. 실패하면 다른 시드로 재시도, 끝까지 실패하면 조건 없는 층으로 폴백.
        // capabilitiesOf: 방 유형 → 그 방 콘텐츠가 요구하는 능력들.
        // 진입 조건을 **콘텐츠에서 역산**해 레이어1(입장)과 레이어2(클리어)를 일치시킨다.
        // null이면 능력 조건 없이 예전처럼 동작한다(테스트·헤드리스용).
        public DungeonMap Generate(DungeonConfig config, RecipeDatabase database,
                                   Func<RoomType, List<Capability>> capabilitiesOf = null)
        {
            var elements = AvailableElements(database);
            var weapons = AvailableWeaponCategories(database);

            DungeonMap last = null;
            int baseSeed = config.Seed < 0 ? DrawSeed() : config.Seed;
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                int seed = baseSeed + attempt;
                _rng = new Random(seed);
                var map = BuildLayout(config);
                map.Seed = seed;
                AssignEntryConditions(map, elements, weapons, capabilitiesOf);
                last = map;

                if (PlaceLoot(map, database, out var keyItems) && FloorValidator.Validate(map, database))
                {
                    PlaceBonusLoot(map, database, keyItems); // 검증 통과 후 예산 나머지 배치(불변식 불변)
                    return map;
                }
            }

            // 폴백: 조건을 모두 제거해 항상 유효한 층으로 만든다.
            // 조건이 사라지면 그에 맞춰 배치했던 열쇠 재료도 무효 — 남겨두면 층 예산을 초과한다.
            foreach (var room in last.Rooms.Values)
            {
                room.EntryConditions.Clear();
                room.GuaranteedLoot.Clear();
            }
            PlaceBonusLoot(last, database, null);
            return last;
        }

        // 층 드랍 테이블을 확정하고, 열쇠 재료를 뺀 나머지 예산 글자를 맵 전체에 분산한다.
        //
        // 열쇠 재료는 PlaceLoot가 이미 GuaranteedLoot(도달 가능 자유 방)에 배치했다.
        // 나머지는 BonusLoot이라 조건부/보스 방에도 놓을 수 있다 — 그 방을 열 열쇠는 이미 보장되므로
        // 교착이 아니고, "맵을 다 돌면 층의 알파벳을 전부 얻는다"가 성립한다.
        private void PlaceBonusLoot(DungeonMap map, RecipeDatabase database, List<ItemDefinition> keyItems)
        {
            map.FloorRecipes.Clear();
            map.FloorRecipes.AddRange(
                FloorLootPlan.SelectRecipes(database, keyItems, FloorRecipeCount, _rng.Next()));

            // 층 예산에서 이미 배치된 열쇠 재료를 뺀 나머지가 보너스 글자다.
            var remaining = FloorLootPlan.BuildLetterBudget(map.FloorRecipes);
            foreach (var room in map.Rooms.Values)
                foreach (var placed in room.GuaranteedLoot)
                    for (int c = 0; c < placed.Count; c++)
                        remaining.Remove(placed.Material);

            var targets = map.Rooms.Values.Where(r => r.Type != RoomType.Tutorial).ToList();
            if (targets.Count == 0) return;

            for (int i = 0; i < remaining.Count; i++)
                targets[_rng.Next(targets.Count)].BonusLoot.Add(new MaterialRequirement(remaining[i], 1));
        }

        private DungeonMap BuildLayout(DungeonConfig config)
        {
            var map = new DungeonMap((0, 0));
            var positions = new HashSet<(int, int)> { (0, 0) };
            var frontier = new List<(int, int)> { (0, 0) };

            int targetCount = _rng.Next(config.MinRooms, config.MaxRooms + 1);

            while (positions.Count < targetCount && frontier.Count > 0)
            {
                int idx = _rng.Next(frontier.Count);
                var (cx, cy) = frontier[idx];

                var neighbors = GetEmptyNeighbors(cx, cy, positions);
                if (neighbors.Count == 0) { frontier.RemoveAt(idx); continue; }

                var next = neighbors[_rng.Next(neighbors.Count)];
                positions.Add(next);
                frontier.Add(next);
            }

            AssignRoomTypes(positions, map);
            ConnectRooms(map);
            return map;
        }

        private void AssignRoomTypes(HashSet<(int, int)> positions, DungeonMap map)
        {
            var list = new List<(int, int)>(positions);
            var start = (0, 0);
            var farthest = start;
            int maxDist = 0;
            foreach (var pos in list)
            {
                int d = Math.Abs(pos.Item1) + Math.Abs(pos.Item2);
                if (d > maxDist) { maxDist = d; farthest = pos; }
            }

            // 환경 퍼즐은 랜덤 풀에 넣지 않는다 — 아래에서 층마다 정확히 1개를 따로 놓는다.
            var typePool = new List<RoomType>
            {
                RoomType.Combat, RoomType.Combat, RoomType.Combat,
                RoomType.Treasure, RoomType.Shop,
                RoomType.PurePuzzle, RoomType.CombatPuzzle
            };

            // 특수방(비-E 아이템 보상/제작)은 층마다 있을 수도, 없을 수도 있다.
            // 반드시 존재하지 않으므로 클리어에 필요한 것은 절대 여기 두지 않는다 — 순수 보너스 방.
            (int, int)? secret = null;
            if (_rng.NextDouble() < SecretRoomChance)
            {
                var candidates = list.Where(p => p != start && p != farthest).ToList();
                if (candidates.Count > 0) secret = candidates[_rng.Next(candidates.Count)];
            }

            // DESIGN.md 2026-09-10: 환경 퍼즐방은 층마다 **정확히 1개**(보스방의 열쇠가 될 방).
            // 랜덤 풀에 두면 0개인 층이 36%, 2개 이상인 층도 나왔다(정적 재미 리포트).
            // 놓을 자리가 없는 층(시작·보스·특수방뿐)은 0개로 두고, 보스방 게이트는 fail-open 한다.
            (int, int)? environment = null;
            var envCandidates = list.Where(p => p != start && p != farthest && !(secret.HasValue && p == secret.Value)).ToList();
            if (envCandidates.Count > 0) environment = envCandidates[_rng.Next(envCandidates.Count)];

            foreach (var pos in list)
            {
                RoomType type;
                if (pos == start) type = RoomType.Tutorial; // 시작 방은 전투 없는 안전한 튜토리얼 방
                else if (pos == farthest) type = RoomType.Boss;
                else if (secret.HasValue && pos == secret.Value) type = RoomType.Secret;
                else if (environment.HasValue && pos == environment.Value) type = RoomType.EnvironmentPuzzle;
                else type = typePool[_rng.Next(typePool.Count)];
                map.AddRoom(new Room(pos.Item1, pos.Item2, type));
            }
        }

        // D-11: 방 크기는 여기서 굴리지 않는다. 크기의 진실은 **템플릿**이고,
        // RoomManager.ApplyRoomSize가 고른 템플릿에서 Room.SizeClass를 채운다.
        // (예전엔 24% 확률로 크기를 먼저 굴렸는데, 그 크기의 템플릿이 없으면
        //  매칭에 실패해 절차적 빈 상자로 떨어졌다 — 실제로 겪은 결함이다.)

        // 조건부 방 유형에 진입 조건을 부여한다. 제작 가능한 능력이 없으면 그 유형은 자유 방으로 남는다.
        //
        // 조건 수를 제한하는 이유: 열쇠 아이템은 클리어 가능성 때문에 층 테이블에 무조건 들어간다.
        // 조건이 많으면 열쇠만으로 예산(FloorRecipeCount)이 꽉 차 플레이어가 무엇을 만들지 고를 여지가
        // 사라지고, 보너스 글자가 0이 되어 맵 전체에 퍼지지도 않는다.
        // 관문형 방(환경 퍼즐·전투 퍼즐)에 진입 조건을 부여한다.
        //
        // DESIGN.md의 퍼즐 3유형 정의를 따른다:
        //   환경 퍼즐 = "기믹을 풀어야 진행"   → 관문형(조건부)
        //   전투 퍼즐 = "처치해야 클리어"      → 관문형(조건부)
        //   독립 퍼즐 = "보상 연결"            → 보상형(자유 입장, 조건 없음)
        //
        // ★ 조건은 그 방 콘텐츠가 **실제로 요구하는 능력**에서 역산한다(capabilitiesOf).
        //   예전엔 조건이 원소/무기 어휘였는데 방 안 장애물은 능력을 요구해서,
        //   조건을 충족해도 클리어가 불가능했다.
        private void AssignEntryConditions(DungeonMap map, List<ElementType> elements,
                                           List<WeaponCategory> weapons,
                                           Func<RoomType, List<Capability>> capabilitiesOf)
        {
            int assigned = 0;
            foreach (var room in map.Rooms.Values)
            {
                if (assigned >= MaxConditionalRooms) return;
                if (room.Type != RoomType.CombatPuzzle && room.Type != RoomType.EnvironmentPuzzle) continue;

                var caps = capabilitiesOf?.Invoke(room.Type);
                if (caps != null && caps.Count > 0)
                {
                    // 콘텐츠가 요구하는 능력을 전부 조건으로 — 하나라도 빠지면 클리어 불가 방이 된다.
                    foreach (var cap in caps)
                        room.EntryConditions.Add(new EntryCondition { RequiredCapability = cap });
                }
                else if (room.Type == RoomType.CombatPuzzle && elements.Count > 0)
                    room.EntryConditions.Add(new EntryCondition { RequiredElement = elements[_rng.Next(elements.Count)] });
                else if (room.Type == RoomType.EnvironmentPuzzle && weapons.Count > 0)
                    room.EntryConditions.Add(new EntryCondition { RequiredWeapon = weapons[_rng.Next(weapons.Count)] });
                else
                    continue;

                assigned++;
            }
        }

        // 조건 충족에 필요한 재료를 계산해 자유 방들의 GuaranteedLoot에 분산 배치한다.
        // 고른 열쇠 아이템(keyItems)은 층 드랍 테이블에 반드시 포함돼야 하므로 함께 돌려준다.
        private bool PlaceLoot(DungeonMap map, RecipeDatabase database, out List<ItemDefinition> keyItems)
        {
            var conditions = map.ConditionalRooms().SelectMany(r => r.EntryConditions);
            if (!FloorValidator.TryPlanRequiredMaterials(conditions, database, out var pool, out keyItems))
                return false;

            if (pool.Count == 0) return true; // 조건 없음 — 배치할 재료도 없음

            // 도달 가능한 자유 방에만 배치 → 재료가 잠긴 방 뒤에 갇히는 교착 방지
            var targets = FloorValidator.ReachableFreeRooms(map);

            // 시작 방(Tutorial)은 K·Y와 잠긴 문이 손으로 짜인 학습 공간이라 예산 글자를 얹지 않는다.
            // (PlaceBonusLoot은 원래부터 제외하고 있었다 — 여기만 빠져 있었다.)
            //
            // 예전엔 "뺐더니 놓을 곳이 없으면 원래 목록으로 되돌린다"고 폴백했는데,
            // 그러면 튜토리얼 방에 열쇠 재료가 떨어진다(seed 1에서 실제로 3개 배치됨).
            // 제외는 예외 없는 규칙이라, 놓을 곳이 없으면 **이 시도를 실패시켜 다른 층을 굴린다** —
            // 30회 모두 실패하면 조건 없는 폴백 층이 되고, 그 경로는 GuaranteedLoot을 비우므로
            // 어느 쪽으로 끝나든 튜토리얼 방은 비어 있다.
            targets = targets.Where(r => r.Type != RoomType.Tutorial).ToList();

            if (targets.Count == 0) return false;

            int i = 0;
            foreach (var kv in pool)
            {
                for (int c = 0; c < kv.Value; c++)
                {
                    targets[i % targets.Count].GuaranteedLoot.Add(new MaterialRequirement(kv.Key, 1));
                    i++;
                }
            }
            return true;
        }

        private static List<ElementType> AvailableElements(RecipeDatabase database) =>
            database.AllItems
                .Where(it => it.Element != ElementType.None && it.Recipe != null && it.Recipe.Count > 0)
                .Select(it => it.Element).Distinct().ToList();

        private static List<WeaponCategory> AvailableWeaponCategories(RecipeDatabase database) =>
            database.AllItems
                .Where(it => it.WeaponCategory != WeaponCategory.None && it.Recipe != null && it.Recipe.Count > 0)
                .Select(it => it.WeaponCategory).Distinct().ToList();

        private void ConnectRooms(DungeonMap map)
        {
            foreach (var room in map.Rooms.Values)
            {
                var n = map.GetRoom(room.X, room.Y + 1);
                var s = map.GetRoom(room.X, room.Y - 1);
                var e = map.GetRoom(room.X + 1, room.Y);
                var w = map.GetRoom(room.X - 1, room.Y);

                if (n != null) { room.North = (n.X, n.Y); n.South = (room.X, room.Y); }
                if (s != null) { room.South = (s.X, s.Y); s.North = (room.X, room.Y); }
                if (e != null) { room.East = (e.X, e.Y); e.West = (room.X, room.Y); }
                if (w != null) { room.West = (w.X, w.Y); w.East = (room.X, room.Y); }
            }
        }

        private List<(int, int)> GetEmptyNeighbors(int x, int y, HashSet<(int, int)> used)
        {
            var result = new List<(int, int)>();
            (int, int)[] dirs = { (0, 1), (0, -1), (1, 0), (-1, 0) };
            foreach (var (dx, dy) in dirs)
            {
                var next = (x + dx, y + dy);
                if (!used.Contains(next)) result.Add(next);
            }
            return result;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Help.Core;
using Help.Player;

namespace Help.Dungeon
{
    public class RoomManager : MonoBehaviour
    {
        [SerializeField] private Tilemap _tilemap;
        [SerializeField] private GameObject _doorPrefab;
        [SerializeField] private TileBase _floorTile;
        [SerializeField] private TileBase _wallTile;
        [SerializeField] private TileBase _doorOpenTile;
        [SerializeField] private TileBase _doorLockedTile;
        [SerializeField] private RoomContentLibrary _contentLibrary; // 방 유형→콘텐츠 프리팹(적/퍼즐/루팅 데이터 스폰)

        private const int RoomWidth = 13;
        private const int RoomHeight = 9;
        private const int SideDoorRow = 1; // 좌우 문 높이: 바닥(y=0) 바로 위

        private DungeonMap _map;
        private Room _currentRoom;
        private PlayerController _player;
        private bool _exitLocked; // 방 퍼즐 미해결 시 출구 잠금(RoomPuzzle이 제어). 기본 false=자유 출입.

        // 방 좌표별 스폰된 콘텐츠 인스턴스 캐시. 나가면 비활성화, 다시 들어오면 재활성화 →
        // 죽은 적·주운 드랍·푼 퍼즐 등 방 상태가 보존된다(매번 초기화되던 문제 해결).
        private readonly Dictionary<Vector2Int, GameObject> _roomContent = new();
        private GameObject _activeContent; // 현재 활성 방의 콘텐츠

        public event System.Action<Room> OnRoomEntered;

        // 진입이 거부됐을 때(문은 있으나 조건 불충족) 통지 — UI 피드백용
        public event System.Action<Room> OnEntryBlocked;

        // 현재 방을 클리어해야 나갈 수 있는데 아직 미클리어 → 나가기 차단 통지
        public event System.Action<Room> OnExitBlocked;

        public Room CurrentRoom => _currentRoom;

        // 프로토타입 런타임 진입점: 던전이 아직 없으면 생성하고 현재 층을 로드한다.
        private void Start()
        {
            // 방 간 이동: 플레이어의 Interact(E) 입력 → 가장 가까운 문으로 진입 시도
            _player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerController>();
            if (_player != null) _player.InteractRequested += HandleInteract;

            // 방 셸(바닥/벽)에 물리 콜라이더를 부착 — 렌더된 방이 실제로 막히는 지형이 되게 한다
            EnsureColliders();

            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.OnRunReset += HandleRunReset; // 사망 시 방 재로드/재배치
            gm.OnFloorChanged += HandleFloorChanged; // 다음 층 진입 시 새 층 로드/재배치
            if (gm.CurrentMap == null)
                gm.StartRun(new DungeonConfig());
            LoadMap(gm.CurrentMap);

            // 레거시 Ground 발판을 제거했으므로 방 바닥 위에 플레이어를 놓는다
            PlacePlayerOnFloor();
        }

        private void OnDestroy()
        {
            if (_player != null) _player.InteractRequested -= HandleInteract;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRunReset -= HandleRunReset;
                GameManager.Instance.OnFloorChanged -= HandleFloorChanged;
            }
        }

        // 다음 층 진입: 새 던전(GameManager가 이미 생성)을 로드하고 플레이어를 시작 방에 배치.
        // LoadMap이 이전 층의 방 콘텐츠 캐시를 파괴하므로 새 층은 깨끗하게 시작된다.
        private void HandleFloorChanged()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.CurrentMap == null) return;
            LoadMap(gm.CurrentMap);
            PlacePlayerOnFloor();
        }

        // 런 리셋(사망) 시 새 던전의 시작 방을 다시 그리고 플레이어를 바닥에 재배치한다.
        private void HandleRunReset()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.CurrentMap == null) return;
            LoadMap(gm.CurrentMap);
            PlacePlayerOnFloor();
        }

        // 실제 존재하는 문 중, 플레이어가 가장 향해 있는 문으로 이동 시도
        private void HandleInteract()
        {
            if (_currentRoom == null || _map == null || _player == null) return;

            var existing = new List<Direction>();
            foreach (var kv in EvaluateDoorStates(_currentRoom))
                if (kv.Value != DoorState.None) existing.Add(kv.Key);

            var p = _player.transform.position;
            var dir = DoorDirectionPicker.NearestAmong(p.x, p.y, existing);
            if (dir.HasValue) TryEnterRoom(dir.Value);
        }

        // 현재 방의 각 방향 문 상태 (UI/시각화용)
        public IReadOnlyDictionary<Direction, DoorState> GetDoorStates() =>
            _currentRoom == null ? null : EvaluateDoorStates(_currentRoom);

        public void LoadMap(DungeonMap map)
        {
            // 새 던전(런 리셋 등)이면 이전 방 콘텐츠 캐시는 무효 → 전부 파괴하고 비운다.
            ClearRoomContentCache();
            _map = map;
            EnterRoom(map.StartPosition.x, map.StartPosition.y);
        }

        private void ClearRoomContentCache()
        {
            foreach (var kv in _roomContent)
                if (kv.Value != null) Destroy(kv.Value);
            _roomContent.Clear();
            _activeContent = null;
        }

        // 지정 방향의 인접 방으로 진입을 시도한다.
        // 진입 조건은 EntryRequirementChecker(보유 재료 + 장비 분해 재료)로 판정.
        public RoomEntryResult TryEnterRoom(Direction dir)
        {
            if (_currentRoom == null || _map == null) return RoomEntryResult.NoDoor;

            // 미해결 방 퍼즐이 출구를 잠갔으면 나갈 수 없다(레이어2 클리어 게이팅)
            if (_exitLocked)
            {
                OnExitBlocked?.Invoke(_currentRoom);
                return RoomEntryResult.Blocked;
            }

            var gm = GameManager.Instance;
            var result = RoomNavigator.TryEnter(
                _currentRoom, dir, _map, gm.Inventory, gm.RecipeDatabase, out var target);

            switch (result)
            {
                case RoomEntryResult.Entered:
                    EnterRoom(target.X, target.Y);
                    // 들어온 방향의 반대편 문으로 플레이어 재배치 (동쪽으로 나갔으면 새 방 서쪽 문에서 등장)
                    RepositionPlayerAtDoor(DoorDirectionPicker.Opposite(dir));
                    break;
                case RoomEntryResult.Blocked:
                    var neighbor = RoomNavigator.GetNeighbor(_currentRoom, dir).Value;
                    OnEntryBlocked?.Invoke(_map.GetRoom(neighbor.x, neighbor.y));
                    break;
            }
            return result;
        }

        public void EnterRoom(int x, int y)
        {
            var room = _map.GetRoom(x, y);
            if (room == null) return;

            _currentRoom = room;
            RenderRoom(room);
            ActivateRoomContent(room);    // 캐시된 콘텐츠 재활성화 or 최초 스폰(상태 보존)
            ApplyExitLockFromContent();   // 방 퍼즐 미해결이면 출구 잠금 재적용(재활성화 시 Awake가 안 돌므로)
            RefreshDoors();
            OnRoomEntered?.Invoke(room);
        }

        // 방 콘텐츠를 활성화한다: 처음이면 스폰해 캐시, 이미 방문했으면 캐시본을 재활성화(상태 보존).
        // 이전 방 콘텐츠는 파괴하지 않고 비활성화만 해 상태를 남긴다.
        // 최초 방문 시 room.GuaranteedLoot(생성기가 보장한 클리어 필수 재료)도 실제 픽업으로 스폰해
        // 콘텐츠 루트 하위에 담는다 → 검증(FloorValidator)이 보증한 재료가 현실에도 존재하고,
        // 방 캐시와 함께 보존되어 재방문 시 이미 주운 재료는 다시 나오지 않는다.
        private void ActivateRoomContent(Room room)
        {
            if (_activeContent != null) _activeContent.SetActive(false);

            var key = new Vector2Int(room.X, room.Y);
            if (_roomContent.TryGetValue(key, out var cached))
            {
                if (cached != null) cached.SetActive(true);
                _activeContent = cached; // null이면 콘텐츠·루트 없는 방으로 확정(재스폰 안 함)
                return;
            }

            // 최초 방문: 유형별 콘텐츠 프리팹(없으면 null) + GuaranteedLoot 픽업을 한 루트에 담는다.
            var prefab = _contentLibrary != null ? _contentLibrary.Pick(room.Type, RoomSeed(room)) : null;
            GameObject root = prefab != null ? Instantiate(prefab, Vector3.zero, Quaternion.identity) : null;

            if (HasLoot(room))
            {
                if (root == null) root = new GameObject($"RoomContent_{room.X}_{room.Y}");
                SpawnRoomLoot(room, root.transform);
            }

            _activeContent = root;
            _roomContent[key] = root;
        }

        private static bool HasLoot(Room room) =>
            (room.GuaranteedLoot != null && room.GuaranteedLoot.Count > 0) ||
            (room.BonusLoot != null && room.BonusLoot.Count > 0);

        // 방에 배정된 층 예산 글자를 공급한다(하이브리드):
        // - 적이 있는 방(전투)=적들에게 확정 드랍으로 배분 → 전멸(=클리어) 시 방 몫 전량 획득.
        //   단 배분이 희소해서 **빈손인 적이 생긴다** — 잡아도 글자가 안 나올 수 있다(LootDistribution).
        // - 적이 없는 방(보물/튜토리얼)=바닥 픽업으로 스폰.
        // 어느 쪽이든 콘텐츠 루트 하위(parent)에 귀속돼 방 캐시와 함께 보존된다.
        private void SpawnRoomLoot(Room room, Transform parent)
        {
            var mats = new List<Help.Item.AlphabetMaterial>();
            AppendLoot(mats, room.GuaranteedLoot); // 진입 열쇠 재료(검증 대상)
            AppendLoot(mats, room.BonusLoot);      // 층 예산의 나머지
            if (mats.Count == 0) return;

            var enemies = parent.GetComponentsInChildren<Help.Enemy.EnemyBase>(true);
            if (enemies.Length > 0)
            {
                var shares = LootDistribution.Assign(mats.Count, enemies.Length, RoomSeed(room));
                int next = 0;
                for (int e = 0; e < enemies.Length; e++)
                    for (int k = 0; k < shares[e]; k++)
                        enemies[e].AddGuaranteedDrop(mats[next++]);
            }
            else
            {
                for (int i = 0; i < mats.Count; i++)
                    Help.Item.MaterialPickup.Spawn(mats[i], FloorSpawnPos(((i % 5) - 2) * 1.6f), Vector3.zero, parent);
            }
        }

        private static void AppendLoot(List<Help.Item.AlphabetMaterial> target, List<Help.Item.MaterialRequirement> loot)
        {
            if (loot == null) return;
            foreach (var entry in loot)
                for (int c = 0; c < entry.Count; c++)
                    target.Add(entry.Material);
        }

        // 방 바닥 중앙 셀 위(+xOffset)의 월드 좌표. GuaranteedLoot 픽업 배치용.
        private Vector3 FloorSpawnPos(float xOffset)
        {
            if (_tilemap == null) return new Vector3(xOffset, 0f, 0f);
            var floorCenter = new Vector3Int(0, 0 - RoomHeight / 2, 0);
            Vector3 w = _tilemap.GetCellCenterWorld(floorCenter) + Vector3.up * 1f;
            w.x += xOffset;
            w.z = 0f;
            return w;
        }

        // 현재 방 콘텐츠의 RoomPuzzle 상태로 출구 잠금을 재평가한다.
        // (RoomPuzzle은 Awake에서만 잠그므로, 재방문 시 미해결 퍼즐 방은 여기서 다시 잠근다)
        private void ApplyExitLockFromContent()
        {
            _exitLocked = false;
            if (_activeContent != null)
            {
                var puzzle = _activeContent.GetComponentInChildren<Help.Puzzle.RoomPuzzle>(true);
                if (puzzle != null && !puzzle.IsSolved) _exitLocked = true;
            }
            if (_currentRoom != null) RenderDoors(_currentRoom);
        }

        // 같은 방=같은 콘텐츠가 되도록 방 좌표로 결정적 seed.
        private static int RoomSeed(Room room) => (room.X * 73856093) ^ (room.Y * 19349663);

        // 현재 방을 Tilemap에 그린다: 테두리 벽 + 내부 바닥 + 연결된 변에 문(바닥).
        private void RenderRoom(Room room)
        {
            if (_tilemap == null) return;
            _tilemap.ClearAllTiles();

            var cells = RoomLayout.Build(RoomWidth, RoomHeight);

            foreach (var kv in cells)
            {
                var tile = kv.Value == TileKind.Wall ? _wallTile : _floorTile;
                if (tile == null) continue;
                // 방을 원점 중심으로 배치
                var pos = new Vector3Int(kv.Key.x - RoomWidth / 2, kv.Key.y - RoomHeight / 2, 0);
                _tilemap.SetTile(pos, tile);
            }

            RenderDoors(room);
        }

        // 문 위치에 상태별 타일(열림/잠김)을 덧그린다.
        // 방 퍼즐이 출구를 잠갔으면(_exitLocked) 모든 출구를 잠김으로 표시(레이어2 게이팅 시각화).
        private void RenderDoors(Room room)
        {
            foreach (var kv in EvaluateDoorStates(room))
            {
                if (kv.Value == DoorState.None) continue;
                var state = _exitLocked ? DoorState.Locked : kv.Value;
                var tile = state == DoorState.Locked ? _doorLockedTile : _doorOpenTile;
                if (tile == null) continue;
                _tilemap.SetTile(DoorTilePos(kv.Key), tile);
            }
        }

        private Dictionary<Direction, DoorState> EvaluateDoorStates(Room room)
        {
            var gm = GameManager.Instance;
            if (gm != null && _map != null)
                return DoorStateEvaluator.Evaluate(room, _map, gm.Inventory, gm.RecipeDatabase);

            // 폴백(테스트/에디터): 연결된 방향은 Open으로 표시
            var res = new Dictionary<Direction, DoorState>();
            foreach (Direction d in Enum.GetValues(typeof(Direction)))
            {
                var n = RoomNavigator.GetNeighbor(room, d);
                bool has = n != null && _map != null && _map.GetRoom(n.Value.x, n.Value.y) != null;
                res[d] = has ? DoorState.Open : DoorState.None;
            }
            return res;
        }

        // 지정한 문 위치(방 안쪽으로 한 칸)로 플레이어를 옮긴다.
        private void RepositionPlayerAtDoor(Direction entryDir)
        {
            if (_player == null || _tilemap == null) return;
            Vector3 world = _tilemap.GetCellCenterWorld(DoorTilePos(entryDir)) + InwardOffset(entryDir);
            world.z = _player.transform.position.z;
            _player.transform.position = world;
        }

        private static Vector3 InwardOffset(Direction doorDir) => doorDir switch
        {
            Direction.East => new Vector3(-1f, 0f, 0f),
            Direction.West => new Vector3(1f, 0f, 0f),
            Direction.North => new Vector3(0f, -1f, 0f),
            _ => new Vector3(0f, 1f, 0f), // South
        };

        private Vector3Int DoorTilePos(Direction dir)
        {
            int cx = RoomWidth / 2;
            switch (dir)
            {
                case Direction.North: return new Vector3Int(cx - RoomWidth / 2, (RoomHeight - 1) - RoomHeight / 2, 0);
                case Direction.South: return new Vector3Int(cx - RoomWidth / 2, 0 - RoomHeight / 2, 0);
                // 좌우 문은 바닥 바로 위 높이 — 플레이어가 딛고 서서 E를 누르고, 진입 후 바닥에 착지하도록
                case Direction.East: return new Vector3Int((RoomWidth - 1) - RoomWidth / 2, SideDoorRow - RoomHeight / 2, 0);
                default: return new Vector3Int(0 - RoomWidth / 2, SideDoorRow - RoomHeight / 2, 0); // West
            }
        }

        // 방 셸(바닥/벽)에 물리 콜라이더를 붙인다: solid 타일(ColliderType=Grid)만 충돌하며,
        // CompositeCollider2D로 인접 셀을 병합해 이음새(플레이어가 걸리는 틈)를 없앤다.
        // Tilemap을 Ground 레이어로 옮겨 PlayerController의 접지 판정(_groundLayer)이 방 바닥을 인식하게 한다.
        private void EnsureColliders()
        {
            if (_tilemap == null) return;
            var go = _tilemap.gameObject;

            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0) go.layer = groundLayer;

            // CompositeCollider2D는 Rigidbody2D를 요구 — 정적 바디로 붙인다
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb == null) rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            var composite = go.GetComponent<CompositeCollider2D>();
            if (composite == null) composite = go.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;

            var tilemapCollider = go.GetComponent<TilemapCollider2D>();
            if (tilemapCollider == null) tilemapCollider = go.AddComponent<TilemapCollider2D>();
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
        }

        // 방 로드 후 플레이어를 방 바닥 중앙 위에 놓는다(레거시 Ground 제거 후 방이 유일한 지면).
        private void PlacePlayerOnFloor()
        {
            if (_player == null || _tilemap == null) return;
            var floorCenter = new Vector3Int(0, 0 - RoomHeight / 2, 0); // 바닥 중앙 셀(원점 중심 좌표)
            Vector3 world = _tilemap.GetCellCenterWorld(floorCenter) + Vector3.up * 1.5f;
            world.z = _player.transform.position.z;
            _player.transform.position = world;
        }

        // RoomPuzzle이 방 목표 미해결 시 출구를 잠근다. 해결되면 TryClearCurrentRoom이 해제.
        public void SetExitLock(bool locked)
        {
            _exitLocked = locked;
            if (_currentRoom != null) RenderDoors(_currentRoom);
        }

        // 보스 방 클리어 시 통지(보상 UI가 구독). 인자=포탈 스폰을 트리거하는 콜백.
        public event System.Action OnBossRoomCleared;

        public void TryClearCurrentRoom()
        {
            if (_currentRoom == null || _currentRoom.IsCleared) return;
            _currentRoom.SetCleared();
            _exitLocked = false;             // 클리어 시 출구 잠금 해제
            RenderDoors(_currentRoom);       // 잠금 해제 시각 반영
            RefreshDoors();

            if (_currentRoom.Type == RoomType.Boss)
            {
                // 보상 UI 구독자가 있으면 그쪽이 보상 후 SpawnNextFloorPortal을 부른다.
                // 없으면(3단계 등) 즉시 포탈을 띄워 다음 층으로 진행 가능하게 한다.
                if (OnBossRoomCleared != null) OnBossRoomCleared.Invoke();
                else SpawnNextFloorPortal();
            }
        }

        // 다음 층 포탈을 현재 방(활성 콘텐츠 하위) 중앙 위에 스폰한다.
        public void SpawnNextFloorPortal()
        {
            Vector3 pos = _tilemap != null
                ? _tilemap.GetCellCenterWorld(new Vector3Int(0, 0 - RoomHeight / 2, 0)) + Vector3.up * 2f
                : Vector3.up * 2f;
            NextFloorPortal.Spawn(pos, _activeContent != null ? _activeContent.transform : null);
        }

        private void RefreshDoors()
        {
            // 실제 구현에서는 문 오브젝트의 상태(열림/잠김)를 갱신
            // 프로토타입: 콘솔 로그로 대체
            Debug.Log($"[Room {_currentRoom.X},{_currentRoom.Y}] Type={_currentRoom.Type} Cleared={_currentRoom.IsCleared}");
        }
    }
}

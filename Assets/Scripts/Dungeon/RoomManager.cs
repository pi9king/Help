using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
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
        [SerializeField] private RoomTemplateLibrary _templateLibrary; // 방 유형→ASCII 지형 템플릿(Assets/Rooms/*.txt)
        [FormerlySerializedAs("_platformTilemap")]
        [SerializeField] private Tilemap _floorTilemap;
        [SerializeField] private TileBase _hazardTile;   // D-12: 가시/구덩이를 합친 피해 바닥

        // 방 크기는 방마다 달라질 수 있으므로 상수가 아니라 현재 방의 값이다.
        // (EnterRoom이 방에 맞춰 갱신한다)
        private int _roomWidth = 13;
        private int _roomHeight = 9;

        private DungeonMap _map;
        private Room _currentRoom;
        private PlayerController _player;
        private bool _exitLocked; // 방 퍼즐 미해결 시 출구 잠금(RoomPuzzle이 제어). 기본 false=자유 출입.

        // 방 좌표별 스폰된 콘텐츠 인스턴스 캐시. 나가면 비활성화, 다시 들어오면 재활성화 →
        // 죽은 적·주운 드랍·푼 퍼즐 등 방 상태가 보존된다(매번 초기화되던 문제 해결).
        private readonly Dictionary<Vector2Int, GameObject> _roomContent = new();
        private GameObject _activeContent; // 현재 활성 방의 콘텐츠
        private Help.Core.CameraFollow _camera;
        private ResolvedRoom _resolved;    // 현재 방의 확정 지형(템플릿이 없으면 null → 절차적 셸)
        private GameObject _hazardRoot;    // 가시/구덩이 트리거 — 방을 다시 그릴 때 통째로 교체

        // 구덩이 트리거가 방을 되돌려 놓기 위해 참조한다(씬에 하나만 존재).
        public static RoomManager Active { get; private set; }

        public event System.Action<Room> OnRoomEntered;

        // 진입이 거부됐을 때(문은 있으나 조건 불충족) 통지 — UI 피드백용
        public event System.Action<Room> OnEntryBlocked;

        // 현재 방을 클리어해야 나갈 수 있는데 아직 미클리어 → 나가기 차단 통지
        public event System.Action<Room> OnExitBlocked;

        public Room CurrentRoom => _currentRoom;

        // 프로토타입 런타임 진입점: 던전이 아직 없으면 생성하고 현재 층을 로드한다.
        private void Awake() => Active = this;

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

            // 쿼터뷰 바닥의 안전한 셀에 플레이어를 놓는다.
            PlacePlayerOnFloor();
        }

        private void OnDestroy()
        {
            if (Active == this) Active = null;
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
        // 문을 쓸 수 있는 거리. 천장 문의 퇴장 지점이 문에서 2칸 아래라(문 칸이 벽이라 머리가 걸린다)
        // 그보다 넉넉해야 사다리를 다 올라간 플레이어가 실제로 나갈 수 있다.
        private const float DoorUseRadius = 2.5f;

        private void HandleInteract()
        {
            if (_currentRoom == null || _map == null || _player == null) return;

            var existing = new List<Direction>();
            foreach (var kv in EvaluateDoorStates(_currentRoom))
                if (kv.Value != DoorState.None) existing.Add(kv.Key);

            var p = _player.transform.position;

            // 타일맵이 있으면 실제 문 위치까지의 거리로 고른다 — 문 앞에 서야 나갈 수 있다.
            // 없으면(헤드리스 등) 예전의 방향 기반 선택으로 폴백한다.
            if (_tilemap != null)
            {
                var doors = new Dictionary<Direction, Vector2>();
                foreach (var d in existing)
                    doors[d] = _tilemap.GetCellCenterWorld(DoorTilePos(d));

                var near = DoorDirectionPicker.NearestWithin(p.x, p.y, doors, DoorUseRadius);
                if (near.HasValue) TryEnterRoom(near.Value);
                return;
            }

            var dir = DoorDirectionPicker.NearestAmong(p.x, p.y, existing);
            if (dir.HasValue) TryEnterRoom(dir.Value);
        }

        // 현재 방이 진입 조건을 가진 방인가(레이어1 게이팅 대상).
        // RoomPuzzle이 "능력 장애물로 출구를 잠가도 되는가"를 판단하는 데 쓴다(RoomGating).
        public bool CurrentRoomHasEntryConditions =>
            _currentRoom != null && _currentRoom.EntryConditions.Count > 0;

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
            ApplyRoomSize(room);          // 렌더/문/스폰 좌표가 전부 이 크기를 기준으로 계산된다
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

            // 최초 방문: 콘텐츠 루트를 만든다.
            // 방 유형이 마커 스폰을 켰고 템플릿이 있으면 마커 위치에 개별 스폰,
            // 아니면 기존의 통짜 콘텐츠 프리팹 경로(손배치)를 그대로 쓴다.
            GameObject root = TrySpawnFromMarkers(room);
            if (root == null)
            {
                var prefab = _contentLibrary != null ? _contentLibrary.Pick(room.Type, CurrentFloor, RoomSeed(room)) : null;
                root = prefab != null ? Instantiate(prefab, ContentOrigin(), Quaternion.identity) : null;
            }

            if (HasLoot(room))
            {
                if (root == null) root = new GameObject($"RoomContent_{room.X}_{room.Y}");
                SpawnRoomLoot(room, root.transform);
            }

            _activeContent = root;
            _roomContent[key] = root;
        }

        // 템플릿 마커('e', 'E', 'x', 'b' 등) 위치에 프리팹을 하나씩 스폰한다.
        // 방 유형이 옵트인하지 않았거나 템플릿/마커가 없으면 null을 돌려 기존 경로로 넘긴다.
        private GameObject TrySpawnFromMarkers(Room room)
        {
            if (_resolved == null || _contentLibrary == null) return null;
            if (!_contentLibrary.UsesTemplateMarkers(room.Type, CurrentFloor)) return null;
            if (_resolved.Markers.Count == 0) return null;

            GameObject root = null;
            int seed = RoomSeed(room);

            foreach (var marker in _resolved.Markers)
            {
                var prefab = _contentLibrary.PickForMarker(marker.Symbol, seed ^ (marker.Cell.x * 31 + marker.Cell.y));
                if (prefab == null) continue;   // 'p'(시작 위치)처럼 스폰 대상이 없는 마커

                if (root == null)
                {
                    root = new GameObject($"RoomContent_{room.X}_{room.Y}");
                    root.transform.position = Vector3.zero;
                }

                Vector3 pos = _tilemap != null
                    ? _tilemap.GetCellCenterWorld(ToCell(marker.Cell.x, marker.Cell.y))
                    : new Vector3(marker.Cell.x, marker.Cell.y, 0f);
                Instantiate(prefab, pos, Quaternion.identity, root.transform);
            }

            // 클리어 게이팅: 장애물이나 적이 스폰됐으면 RoomPuzzle이 목표를 집계하도록 붙인다.
            // (RoomPuzzle은 _targets가 비어 있으면 자식의 CapabilityTarget을 자동 수집한다)
            if (root != null && root.GetComponentInChildren<Help.Puzzle.RoomPuzzle>(true) == null)
                root.AddComponent<Help.Puzzle.RoomPuzzle>();

            return root;
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
                    Help.Item.MaterialPickup.Spawn(
                        mats[i], FloorSpawnPos(RoomGeometry.SpreadX(i, mats.Count, LootSpacing)),
                        Vector3.zero, parent);
            }
        }

        private static void AppendLoot(List<Help.Item.AlphabetMaterial> target, List<Help.Item.MaterialRequirement> loot)
        {
            if (loot == null) return;
            foreach (var entry in loot)
                for (int c = 0; c < entry.Count; c++)
                    target.Add(entry.Material);
        }

        // 루팅 글자 간격. 픽업 콜라이더(0.9)보다 넓어야 서로 물리지 않는다.
        private const float LootSpacing = 1.8f;

        // 방 중앙의 이동 가능한 바닥에 글자를 가로로 펼친다.
        private Vector3 FloorSpawnPos(float xOffset)
        {
            if (_tilemap == null)
                return new Vector3(xOffset, 0f, 0f);

            Vector3 w = _tilemap.GetCellCenterWorld(SafeSpawnCell());
            w.x += xOffset;

            // 스폰 기준점이 방 가장자리에 가까우면 넓게 편 글자가 벽 안으로 밀려난다.
            // 벽에 박힌 글자도 결국 못 줍는 글자다 — 방 안쪽 바닥 범위로 가둔다.
            w.x = Mathf.Clamp(w.x,
                _tilemap.GetCellCenterWorld(ToCell(1, _roomHeight / 2)).x,
                _tilemap.GetCellCenterWorld(ToCell(_roomWidth - 2, _roomHeight / 2)).x);

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
                // 목표 수까지 봐야 한다 — 목표 0개인 퍼즐은 영원히 해결되지 않으므로
                // 잠그는 순간 방이 통째로 갇힌다(RoomPuzzle.Awake와 같은 규칙을 쓴다).
                if (puzzle != null)
                    _exitLocked = RoomGating.ShouldLockExit(puzzle.ExitObjectiveCount, puzzle.IsExitOpen);
            }
            if (_currentRoom != null) RenderDoors(_currentRoom);
        }

        // 같은 방=같은 콘텐츠가 되도록 방 좌표로 결정적 seed.
        private static int RoomSeed(Room room) => (room.X * 73856093) ^ (room.Y * 19349663);

        // 방 크기를 정하고 카메라에 방 경계를 알린다.
        // Small 방은 카메라가 중앙 고정되고, Wide/Tall은 경계 안에서 플레이어를 따라간다.
        //
        // D-11: **크기의 진실은 템플릿이다.** 생성기가 크기를 굴리지 않는다 —
        // 층·유형에 맞는 템플릿을 고르고, 방 크기 등급은 그 결과에서 파생된다.
        private void ApplyRoomSize(Room room)
        {
            var template = _templateLibrary != null
                ? _templateLibrary.Pick(room.Type, CurrentFloor, RoomSeed(room))
                : null;
            _resolved = template != null ? RoomLayout.Resolve(template, RoomSeed(room)) : null;

            // 고른 템플릿의 크기 등급을 방에 되돌려 적는다(미니맵·폴백이 이 값을 본다).
            if (template != null) room.SizeClass = template.SizeClass;

            var dim = RoomDimensions.Of(room.SizeClass);
            _roomWidth = _resolved?.Width ?? dim.Width;
            _roomHeight = _resolved?.Height ?? dim.Height;

            if (_camera == null && Camera.main != null)
                _camera = Camera.main.GetComponent<Help.Core.CameraFollow>();
            _camera?.SetRoomBounds(Help.Core.CameraBounds.RoomRect(_roomWidth, _roomHeight));
        }

        // 현재 방을 Tilemap에 그린다: 장애물 Tilemap에는 벽, 바닥 Tilemap에는 이동 공간.
        private void RenderRoom(Room room)
        {
            if (_tilemap == null) return;
            EnsureFloorTilemap();
            _tilemap.ClearAllTiles();
            if (_floorTilemap != null) _floorTilemap.ClearAllTiles();
            ClearHazards();

            if (_resolved != null) PaintResolved();
            else PaintProceduralShell();

            RenderDoors(room);
        }

        // 템플릿이 없는 방(폴백): 테두리 벽 + 내부 바닥.
        private void PaintProceduralShell()
        {
            foreach (var kv in RoomLayout.Build(_roomWidth, _roomHeight))
            {
                var cell = ToCell(kv.Key.x, kv.Key.y);
                if (kv.Value == TileKind.Wall)
                    _tilemap.SetTile(cell, _wallTile);
                else if (_floorTilemap != null)
                    _floorTilemap.SetTile(cell, _floorTile);
            }
        }

        // ASCII 템플릿에서 확정된 쿼터뷰 지형을 그린다.
        private void PaintResolved()
        {
            for (int x = 0; x < _resolved.Width; x++)
                for (int y = 0; y < _resolved.Height; y++)
                {
                    var cell = ToCell(x, y);
                    switch (_resolved.Tiles[x, y])
                    {
                        case TileKind.Floor:
                            if (_floorTilemap != null) _floorTilemap.SetTile(cell, _floorTile);
                            break;
                        case TileKind.Wall: _tilemap.SetTile(cell, _wallTile); break;
                        case TileKind.Hazard:
                            if (_floorTilemap != null) _floorTilemap.SetTile(cell, _hazardTile ?? _floorTile);
                            AddHazard(cell, name: $"Hazard_{x}_{y}");
                            break;
                    }
                }
        }

        // 템플릿 좌표(좌하단 원점) → 방을 화면 원점 중심에 놓는 타일맵 셀 좌표
        // 지형 템플릿·콘텐츠 풀 모두 이 층 번호를 축으로 쓴다(D-13).
        private static int CurrentFloor =>
            GameManager.Instance != null ? GameManager.Instance.CurrentFloor
                                         : RoomTemplateNaming.DefaultFloor;

        private Vector3Int ToCell(int x, int y) =>
            new Vector3Int(x - _roomWidth / 2, y - _roomHeight / 2, 0);

        // 쿼터뷰 콘텐츠 프리팹의 원점 = 방 중앙 바닥 셀.
        private Vector3 ContentOrigin() =>
            _tilemap != null
                ? _tilemap.GetCellCenterWorld(ToCell(_roomWidth / 2, _roomHeight / 2))
                : Vector3.zero;

        private void ClearHazards()
        {
            if (_hazardRoot == null) return;
            Destroy(_hazardRoot);
            _hazardRoot = null;
        }

        // 피해 바닥의 피해량. 예전엔 가시 12 / 구덩이 15로 나뉘어 있었는데
        // 둘을 합치면서(D-12) 하나로 통일했다 — "구덩이"는 이제 지형 모양이지 타일 종류가 아니다.
        private const int HazardDamage = 12;

        private void AddHazard(Vector3Int cell, string name)
        {
            if (_hazardRoot == null)
            {
                _hazardRoot = new GameObject("RoomHazards");
                _hazardRoot.transform.SetParent(transform, false);
            }
            // 칸보다 살짝 작게 — 옆 칸을 스치기만 해도 맞는 걸 막는다.
            RoomHazard.Create(_hazardRoot.transform, _tilemap.GetCellCenterWorld(cell),
                              new Vector2(0.9f, 0.9f), HazardDamage, name);
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
            // 템플릿이 문 위치를 정해 두었으면 그쪽이 진실이다.
            if (_resolved != null && _resolved.Doors.TryGetValue(dir, out var cell))
                return ToCell(cell.x, cell.y);

            int cx = _roomWidth / 2;
            switch (dir)
            {
                case Direction.North: return new Vector3Int(cx - _roomWidth / 2, (_roomHeight - 1) - _roomHeight / 2, 0);
                case Direction.South: return new Vector3Int(cx - _roomWidth / 2, 0 - _roomHeight / 2, 0);
                case Direction.East: return new Vector3Int((_roomWidth - 1) - _roomWidth / 2, 0, 0);
                default: return new Vector3Int(0 - _roomWidth / 2, 0, 0); // West
            }
        }

        // 장애물 Tilemap에만 물리 콜라이더를 붙인다. 바닥 Tilemap은 시각 전용이다.
        // CompositeCollider2D로 인접 셀을 병합해 이음새(플레이어가 걸리는 틈)를 없앤다.
        // 기존 물리 레이어 이름은 에셋 호환을 위해 Ground로 유지한다.
        private void EnsureColliders()
        {
            if (_tilemap == null) return;
            EnsureFloorTilemap();
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

            // 벽 마찰로 이동이 끊기지 않게 한다.
            Help.Core.PhysicsMaterials.ApplyFrictionless(go);
        }

        // 바닥은 시각 전용이다. 이전 씬에 남은 콜라이더와 이펙터는 즉시 비활성화한다.
        private void EnsureFloorTilemap()
        {
            if (_floorTilemap == null)
            {
                var parent = _tilemap.transform.parent != null ? _tilemap.transform.parent : _tilemap.transform;
                var go = new GameObject("FloorTilemap", typeof(Tilemap), typeof(TilemapRenderer));
                go.transform.SetParent(parent, false);
                _floorTilemap = go.GetComponent<Tilemap>();

                var renderer = go.GetComponent<TilemapRenderer>();
                var source = _tilemap.GetComponent<TilemapRenderer>();
                if (source != null)
                {
                    renderer.sortingLayerID = source.sortingLayerID;
                    renderer.sortingOrder = source.sortingOrder - 1;
                }
            }

            var pgo = _floorTilemap.gameObject;
            var col = pgo.GetComponent<TilemapCollider2D>();
            if (col != null) col.enabled = false;
            var effector = pgo.GetComponent<PlatformEffector2D>();
            if (effector != null) effector.enabled = false;
            var rb = pgo.GetComponent<Rigidbody2D>();
            if (rb != null) rb.simulated = false;
        }

        // 방 로드 후 플레이어를 이동 가능한 바닥 셀 중앙에 놓는다.
        private void PlacePlayerOnFloor()
        {
            if (_player == null || _tilemap == null) return;

            Vector3 world = _tilemap.GetCellCenterWorld(SafeSpawnCell());
            world.z = _player.transform.position.z;
            _player.transform.position = world;
        }

        // 플레이어를 놓을 안전한 칸.
        // 템플릿이 있으면 'p' 마커 → 없으면 바닥에서 위험하지 않은 첫 칸을 찾는다.
        // (예전처럼 무조건 방 중앙에 놓으면 거기가 구덩이인 방에서 즉사한다)
        private Vector3Int SafeSpawnCell()
        {
            if (_resolved == null) return ToCell(_roomWidth / 2, _roomHeight / 2);

            foreach (var marker in _resolved.Markers)
                if (marker.Symbol == 'p') return ToCell(marker.Cell.x, marker.Cell.y);

            int centerX = _resolved.Width / 2;
            int centerY = _resolved.Height / 2;
            int maxRadius = Mathf.Max(_resolved.Width, _resolved.Height);
            for (int radius = 0; radius < maxRadius; radius++)
                for (int y = Mathf.Max(1, centerY - radius); y <= Mathf.Min(_resolved.Height - 2, centerY + radius); y++)
                for (int x = Mathf.Max(1, centerX - radius); x <= Mathf.Min(_resolved.Width - 2, centerX + radius); x++)
                {
                    TileKind tile = _resolved.TileAt(x, y);
                    if (!ResolvedRoom.IsWalkable(tile) || ResolvedRoom.IsHazard(tile)) continue;
                    return ToCell(x, y);
                }

            return ToCell(_roomWidth / 2, _roomHeight / 2);
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
                ? _tilemap.GetCellCenterWorld(SafeSpawnCell())
                : Vector3.zero;
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

using UnityEngine;
using Help.Dungeon;
using Help.Inventory;
using Help.Crafting;
using Help.Item;

namespace Help.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private RecipeDatabase _recipeDatabase;
        [SerializeField] private bool _seedStarterMaterials = true; // 프로토타입 테스트용: 시작 시 재료 지급

        public const int MaxFloors = 3; // 이 층 수를 모두 클리어하면 게임 승리

        public Help.Inventory.Inventory Inventory { get; private set; }
        public CraftingSystem Crafting { get; private set; }
        public Wallet Wallet { get; private set; } // 골드(알파벳 외 재화) — 인벤 슬롯을 차지하지 않는 카운터
        public DungeonMap CurrentMap { get; private set; }
        public RecipeDatabase RecipeDatabase => _recipeDatabase;
        public int CurrentFloor { get; private set; } = 1;

        public event System.Action OnInventoryChanged;
        public event System.Action OnRunReset; // 런 리셋(사망) 시 — RoomManager가 구독해 방 재로드/재배치
        public event System.Action OnFloorChanged; // 다음 층 진입 — RoomManager가 구독해 새 층 로드
        public event System.Action OnGameCleared;  // 최종 층까지 클리어 — 승리 UI가 구독

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Inventory = new Help.Inventory.Inventory();
            Inventory.OnChanged += () => OnInventoryChanged?.Invoke();
            Crafting = new CraftingSystem(_recipeDatabase);
            Wallet = new Wallet();

            if (_seedStarterMaterials) SeedStarterMaterials();
        }

        // 프로토타입 테스트용: RecipeDatabase에 등록된 모든 알파벳 재료를 2개씩 지급
        private void SeedStarterMaterials()
        {
            if (_recipeDatabase == null) return;
            foreach (AlphabetMaterial mat in System.Enum.GetValues(typeof(AlphabetMaterial)))
            {
                var def = _recipeDatabase.FindMaterial(mat);
                if (def != null) Inventory.Add(def, 2);
            }
        }

        public void StartRun(DungeonConfig config)
        {
            var gen = new DungeonGenerator();
            // 진입 조건 + 재료 보장 불변식까지 만족하는 층 생성
            CurrentMap = gen.Generate(config, _recipeDatabase);
        }

        // 보스 처치 후 다음 층 포탈로 진입 시 호출. 최종 층이면 승리 통지, 아니면 새 층을 생성한다.
        public void AdvanceFloor()
        {
            if (CurrentFloor >= MaxFloors) { OnGameCleared?.Invoke(); return; }
            CurrentFloor++;
            StartRun(new DungeonConfig { FloorNumber = CurrentFloor });
            OnFloorChanged?.Invoke(); // RoomManager가 새 층 로드 + 플레이어 재배치
        }

        // 사망 시 런을 처음부터 다시 시작: 인벤토리 초기화(+시드) → 새 던전 → 구독자 통지.
        // (메타 진행/영구 해금은 미구현 — 현재는 완전 초기화)
        public void RestartRun()
        {
            Inventory.Clear();
            Wallet.Reset(); // 재화도 런 단위 — 메타 진행(영구 재화)은 미구현
            if (_seedStarterMaterials) SeedStarterMaterials();
            CurrentFloor = 1; // 사망 시 1층부터 다시
            StartRun(new DungeonConfig());
            OnRunReset?.Invoke();
        }
    }
}

# Architecture Document

> 시스템 구조, 핵심 클래스, 데이터 흐름 정의

## 시스템 개요

```
┌─────────────┐     ┌──────────────┐     ┌─────────────┐
│  GameManager │────▶│ DungeonSystem │────▶│  RoomManager │
│  (런 상태)   │     │ (맵 생성)     │     │ (방 로딩)    │
└──────┬──────┘     └──────────────┘     └──────┬──────┘
       │                                         │
       ▼                                         ▼
┌──────────────┐     ┌──────────────┐     ┌─────────────┐
│ PlayerSystem  │◀───│  CombatSystem │◀───│ EnemySystem  │
│ (입력, 상태)  │     │ (데미지 판정) │     │ (AI, 패턴)  │
└──────┬──────┘     └──────────────┘     └─────────────┘
       │
       ▼
┌──────────────┐     ┌──────────────┐     ┌───────────────┐
│  ItemSystem   │◀──▶│InventorySystem│◀──▶│ CraftingSystem │
│ (정의, 효과)  │     │(보관, 장착)   │     │(조합, 분해)    │
└──────────────┘     └──────────────┘     └───────────────┘
                     ┌──────────────┐
                     │ PuzzleSystem  │
                     │ (퍼즐 판정)   │
                     └──────────────┘
```

## 네임스페이스 구조

```
Help.Core       — GameManager, 이벤트 정의, 공용 타입
Help.Player     — PlayerController, PlayerStats, PlayerState
Help.Dungeon    — DungeonGenerator, Room, RoomTemplate, DoorConnector
Help.Enemy      — EnemyBase, 적 유형별 클래스
Help.Combat     — DamageCalculator, Hitbox/Hurtbox
Help.Item       — ItemDefinition, ItemEffect, LootTable
Help.Inventory  — Inventory, EquipmentSlot, ItemStack
Help.Crafting   — Recipe, CraftingSystem, DisassemblySystem
Help.Puzzle     — PuzzleBase, 퍼즐 유형별 클래스
Help.UI         — HUD, 메뉴, 미니맵, 인벤토리 UI
```

## 핵심 설계 원칙

- **로직과 MonoBehaviour 분리**: 게임 로직은 순수 C# 클래스로 작성하여 EditMode 테스트 가능하게 유지
- **이벤트 기반 통신**: 시스템 간 직접 참조 최소화, `event Action<T>`로 느슨한 결합
- **데이터 주도 설계**: 방 템플릿, 아이템 정의, 적 스탯은 ScriptableObject로 관리

## 핵심 클래스 설계

> TODO: 구현하면서 확정된 클래스 설계를 기록합니다.

### 던전 생성 (Help.Dungeon)

```
DungeonGenerator (순수 C#)
├── 입력: DungeonConfig (층 번호, 방 개수 범위, 필수 방 유형)
├── 출력: DungeonMap (방 좌표 + 유형 + 연결 정보)
└── 알고리즘: (논의 필요 — 랜덤 워크 / BSP / 커스텀)

RoomTemplate (ScriptableObject)
├── 타일 데이터
├── 적 스폰 포인트
├── 아이템 스폰 포인트
├── 문 위치
├── RoomType (Combat, EnvironmentPuzzle, CombatPuzzle, PurePuzzle, Treasure, Shop, Boss, Secret)
└── EntryRequirement (진입에 필요한 장비/속성 조건)

EntryRequirement (순수 C#)
├── List<CraftingRequirement> (필요한 아이템 조건 목록)
│   예: { WeaponType: Hammer }, { Element: Water }
└── CanEnter(Inventory, RecipeDatabase) → bool
    1. 총 재료 풀 계산 = 보유 재료 + 모든 장비 분해 시 재료
    2. 각 조건에 대해 해당 아이템 제작 가능 여부 확인

MaterialPoolCalculator (순수 C#)
├── GetTotalMaterialPool(Inventory) → Dictionary<MaterialId, int>
│   보유 재료 + 장비 분해 결과를 합산
└── EditMode 테스트 핵심 대상

FloorValidator (순수 C#) — 던전 생성 시 사용
├── Validate(FloorData) → bool
│   "자유 입장 방의 루트만으로 모든 조건부 방 진입 가능한가?"
└── 생성 파이프라인의 마지막 검증 단계

RoomManager (MonoBehaviour)
├── DungeonMap을 받아 현재 방을 Tilemap에 렌더링
├── 방 전환 처리
├── 문에 방 유형 아이콘 + 진입 가능 여부 표시
└── 방 클리어 판정
```

## 데이터 흐름

> TODO: 주요 흐름(런 시작, 방 전환, 전투, 아이템 획득 등) 확정 시 추가

## 인벤토리 / 크래프팅 시스템 (Help.Inventory, Help.Crafting)

```
ItemDefinition (ScriptableObject)
├── id, name, description, icon
├── ItemType (Material, Equipment, Consumable)
├── Rarity (논의 필요)
└── 장비일 경우: EquipmentSlotType, StatModifiers

Inventory (순수 C#)
├── List<ItemStack> items (아이템 + 수량)
├── EquipmentSlot[] equippedItems
├── Add / Remove / Find / Sort
└── event OnInventoryChanged

CraftingSystem (순수 C#)
├── Recipe (입력 재료 목록 → 출력 아이템)
├── Craft(Recipe, Inventory) → bool
├── Disassemble(ItemStack, Inventory) → List<ItemStack>
└── 레시피 검색/필터
```

## 결정 로그

| 날짜 | 결정 | 이유 |
|------|------|------|
| 2026-03-31 | 로직/MonoBehaviour 분리 원칙 | EditMode 테스트 가능성 확보, TDD 사이클 유지 |
| 2026-03-31 | DungeonGenerator는 순수 C# | 생성 알고리즘을 Unity 의존성 없이 테스트 |
| 2026-03-31 | EntryRequirement — 크래프팅 기반 진입 판정 | 보유재료+분해가능재료 합산, 순수 C#로 테스트 가능 |
| 2026-03-31 | MaterialPoolCalculator 분리 | 재료 풀 계산 로직을 독립시켜 EditMode 테스트 가능 |
| 2026-03-31 | FloorValidator — 던전 생성 검증 | 재료 보장 불변식을 생성 파이프라인에서 강제 |
| 2026-03-31 | EquipmentSlot 구성: 무기1 + 머리/몸통/다리 + 악세서리1~2 | 인벤토리-장착-크래프팅 연동 |

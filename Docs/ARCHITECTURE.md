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
Help.Puzzle     — Capability 판정/타깃 프레임워크 (CapabilityMatch, SolveTracker, CapabilityTarget, RoomPuzzle)
Help.UI         — HUD, 메뉴, 미니맵, 인벤토리 UI
```

## 핵심 설계 원칙

- **로직과 MonoBehaviour 분리**: 게임 로직은 순수 C# 클래스로 작성하여 EditMode 테스트 가능하게 유지
- **이벤트 기반 통신**: 시스템 간 직접 참조 최소화, `event Action<T>`로 느슨한 결합
- **데이터 주도 설계**: 방 템플릿, 아이템 정의, 적 스탯은 ScriptableObject로 관리

## 구현된 파일 구조 (2026-07-04 기준)

```
Assets/Scripts/
├── Help.asmdef               — 메인 어셈블리 (Unity.InputSystem, UnityEngine.UI 참조)
├── Core/
│   ├── GameManager.cs        — 싱글턴, Inventory + CraftingSystem + DungeonMap + RecipeDatabase(공개 접근자) 보유. _seedStarterMaterials=true면 시작 시 모든 알파벳 재료 2개씩 지급(프로토타입 테스트용). RestartRun()=사망 시 인벤토리 Clear+시드+새 던전+OnRunReset 통지
│   └── CameraFollow.cs       — 플레이어 추적 카메라 (LateUpdate, 보간)
├── Player/
│   ├── PlayerState.cs        — enum (Idle/Running/Jumping/Falling/Dashing/Attacking/Hurt/Dead)
│   ├── PlayerStats.cs        — HP, 방어, 이동속도, 장비 보너스 적용/해제 등 순수 C# (OnHpChanged, OnDied 이벤트). 사망 후 TakeDamage/Heal 무효(부활 방지), Reset()으로만 부활(런 리셋)
│   └── PlayerController.cs   — MonoBehaviour, Input System 콜백, 대시/점프/공격 타이머, AttackPerformed 이벤트,
│                                Start()에서 Inventory.OnItemEquipped/OnItemUnequipped 구독해 장비 스탯+무기 속성(EquippedElement) 반영, OnDestroy에서 구독 해제. SetUiPanelOpen()로 UI 패널 오픈 중 게임플레이 입력(이동/점프/대시/공격/E) 게이트(비모달 UI 입력 누수 방지)
├── Combat/
│   ├── ElementType.cs        — enum 15종 (None + Fire/Ice/Steel/…/Spike)
│   ├── DamageCalculator.cs   — static: 속성 불일치 시 10% 데미지(최소 1 보장 — 완전 면역 아님)
│   ├── Hitbox.cs             — 공격 판정 콜라이더. 활성 창/배치(Configure: reach/size)는 PlayerAttack(모션)이 구동. 적중 시 데미지+EnemyBase.OnHitReceived(넉백/플래시)+HitStop+CameraShake
│   ├── AttackMotionClock.cs — 순수: 공격 타이밍(Windup→Active→Recovery→Done)+Active 진행도. 근접/원거리/마법 공용. EditMode 테스트
│   ├── AttackKind.cs        — enum: MeleeArc(구현)/Projectile(원거리·마법, 추후)
│   ├── AttackMotionDef.cs   — 공격 1종 데이터(타이밍/사거리/범위/호 각도/색). 무기별 분리 = 데이터 교체. 추후 WeaponCategory→라이브러리
│   ├── PlayerAttack.cs      — 공격 재생 드라이버: AttackPerformed→모션 클록 구동, MeleeArc면 히트박스 배치·활성 + SlashVFX 스윕. Kind 분기에 원거리 자리. 자동 부착
│   ├── SlashVFX.cs          — 슬래시 호 연출(초승달 스프라이트 런타임 절차생성, 호 스윕+페이드). 자동 부착
│   ├── Hurtbox.cs            — 피격 판정, EnemyStats 보유
│   ├── HitFlash.cs          — 스프라이트 순간 색칠(피격 플래시/예비동작 텔레그래프 공용, unscaled). 자동 부착
│   ├── CameraShake.cs       — 카메라 흔들림(Camera.main에 자동 부착, static ShakeMain, unscaled)
│   ├── HitStop.cs           — 타격 순간 짧은 timeScale=0(자동 생성 러너, static Do)
│   └── EnemyProjectile.cs   — 아처가 쏘는 투사체(등속, 플레이어 데미지, 벽/수명 소멸). 런타임 생성(static Spawn)
├── Enemy/
│   ├── EnemyStats.cs         — HP, 공격력, LockedElement 순수 C#
│   ├── EnemyState.cs         — enum Idle/Patrol/Chase/Attack
│   ├── EnemyArchetype.cs     — enum Melee/Ranged (행동 유형)
│   ├── EnemyMeleeAttack.cs   — 순수 C# 근접공격 타이밍(Ready→Windup 예비동작→Strike→Recover). 즉발 대신 회피 가능 창. EditMode 테스트
│   ├── DropEntry.cs          — [Serializable] 드랍 테이블 한 줄(Material/Count/Chance)
│   ├── DropRoller.cs         — 순수 C#: 드랍 테이블 확률 판정→나오는 재료 목록. EditMode 테스트
│   ├── EnemyCounter.cs       — 순수 C#: 방 적 처치 집계(AllDead). EditMode 테스트
│   ├── EnemyClearObjective.cs — MonoBehaviour: 방 EnemyBase 전멸을 목표로 집계(IsMet/OnMet). RoomPuzzle이 등록해 전투 방 출구 게이팅
│   ├── EnemyAI.cs            — 순수 C# 상태머신(EnemyPerception/EnemyAIConfig/EnemyDecision + EnemyAI.Tick). Patrol 왕복→Aggro 감지 Chase→AttackRange Attack, DeAggro 이력현상, 수평 데드존. Archetype 분기: Melee=사거리서 정지 공격, Ranged=스탠드오프보다 가까우면 후퇴하며 공격(카이팅, StandoffRange). EditMode 테스트 가능
│   ├── EnemyLoot.cs          — 순수 C#, static: 잠금 속성 단어 글자(−E)를 드롭 재료로 매핑(Fire→F,I,R / Steel→S,T,L). 처치→크래프팅 재료 보상 고리
│   └── EnemyBase.cs          — MonoBehaviour, FixedUpdate에서 EnemyAI 구동. 공격=예비동작 후 DoAttack(Melee=근접판정 / Ranged=EnemyProjectile 발사). 처치 시 DropLoot(드랍 테이블 굴려 MaterialPickup 월드 물리 드랍, 비면 속성글자 폴백)+OnDied 발화(방 클리어 집계). IsBoss 플래그. 4종 프리팹(Grunt/Archer/Brute/Boss=강화브루트)이 스탯·색·크기·드랍만 달리함(지각 구성→결정 적용: 이동/facing/쿨다운 공격). 접촉 상시데미지 대신 Attack 상태에서 _attackCooldown마다 PlayerController.TakeDamage. HandleDeath에서 EnemyLoot로 재료를 Inventory에 지급. OnRunReset 구독→생존 적을 HP/AI/스폰위치로 복구(처치된 적은 미재스폰)
├── Dungeon/
│   ├── RoomType.cs           — enum 9종(+Tutorial=시작 방 전용, 전투 없음)
│   ├── Room.cs               — 방 데이터 + EntryCondition + GuaranteedLoot(확정 지급 재료) 정의
│   ├── DungeonMap.cs         — Dictionary<(int,int), Room>, FreeRooms/ConditionalRooms
│   ├── RoomContentLibrary.cs — ScriptableObject: RoomType→콘텐츠 프리팹 매핑. 방 로드 시 유형별 콘텐츠(적/퍼즐)를 데이터 스폰(SelectIndex로 결정적 선택). Combat/Puzzle/Tutorial/Boss/Treasure/Shop 매핑
│   ├── NextFloorPortal.cs    — 보스 방 보상 수령 후 등장하는 다음 층 포탈(트리거→GameManager.AdvanceFloor). 런타임 생성(절차 링 스프라이트)
│   ├── DungeonConfig.cs      — 생성 파라미터 (방 수 범위, Seed)
│   ├── DungeonGenerator.cs   — 순수 C#, 랜덤 워크. Generate(config)=조건 없는 레이아웃, Generate(config,db)=조건부 방에 진입조건 부여+자유방에 재료 배치+FloorValidator 검증(실패 시 재시도, 폴백은 조건 제거)
│   ├── EntryRequirementChecker.cs — 순수 C#, static: 방 진입 판정. Inventory 버전 + 재료 풀(Dictionary) 오버로드 (FloorValidator가 재사용)
│   ├── FloorValidator.cs     — 순수 C#, static: 재료 보장 불변식 검증(Validate) + 필요재료 계산(TryPlanRequiredMaterials) + 도달성(ReachableFreeRooms, start에서 자유 방만 거쳐 BFS). 조건 중복 제거(열쇠 재사용)+재료 소비+도달성 인식. 잠긴 방 뒤 재료는 미집계
│   ├── Direction.cs          — enum Direction(N/S/E/W) + RoomEntryResult(NoDoor/Blocked/Entered)
│   ├── RoomLayout.cs         — 순수 C#, static: 방 크기 → 사이드뷰 플랫포머 셸(Dictionary<Vector2Int,TileKind>). 바닥 한 줄(y=0)=Floor, 천장/좌우=Wall, 내부는 빈 공간(딕셔너리 미포함). 문은 셸에 구멍을 안 뚫고 RoomManager가 오버레이+E로 처리
│   ├── DoorStateEvaluator.cs — 순수 C#, static: 각 방향 문 상태(None/Open/Locked) 판정 (EntryRequirementChecker 재사용)
│   ├── DoorDirectionPicker.cs — 순수 C#, static: 방 중심 기준 플레이어 위치 → 가장 가까운 문 방향
│   ├── RoomNavigator.cs      — 순수 C#, static: 방향→인접 방 조회 + 진입 판정 (EntryRequirementChecker 재사용)
│   └── RoomManager.cs        — MonoBehaviour, Start()에서 던전 생성+로드, 방 렌더링(RoomLayout→Tilemap, 문 상태 타일 _doorOpenTile/_doorLockedTile 오버레이), GetDoorStates() 노출, Interact(E) 입력→가장 가까운 문으로 TryEnterRoom(방 간 이동), OnRoomEntered/OnEntryBlocked 이벤트. EnsureColliders()로 Tilemap에 런타임 콜라이더(TilemapCollider2D+CompositeCollider2D+static Rigidbody2D, Ground 레이어) 부착→방 셸이 실제 지형. 좌우 문은 바닥 바로 위 높이(SideDoorRow), PlacePlayerOnFloor()로 방 로드 시 플레이어를 바닥에 배치
├── Item/
│   ├── AlphabetMaterial.cs   — enum A~Z (E 제외, 25종)
│   ├── ItemType.cs           — Material/Weapon/HeadArmor/BodyArmor/LegArmor/Accessory/Consumable
│   ├── WeaponCategory.cs     — None + Blade/Spear/Axe/Knife/Rapier/Saber (None=조건 없음, 판정 로직 기준값)
│   ├── Capability.cs         — enum None/BreakWall/CrossGap/Melt/Conduct/Bind/Unlock. 아이템이 제공·퍼즐이 요구하는 기능 태그(타입 직교). Unlock=잠긴 문(KEY)
│   ├── MaterialRequirement.cs — (AlphabetMaterial, Count) 직렬화 가능 struct
│   ├── ItemDefinition.cs     — ScriptableObject, Recipe + Capabilities(List<Capability>, 이 아이템이 제공하는 능력) 포함
│   ├── AlphabetWordRule.cs   — static: "단어에 E 포함" + "레시피=단어−E" 규칙 검증(순수)
│   └── MaterialPickup.cs     — MonoBehaviour: 플레이어 트리거 진입 시 인벤토리에 재료 1개 추가 후 자기 제거(Room_Tutorial의 K·Y 줍기)
├── Inventory/
│   ├── EquipmentSlotType.cs  — Weapon/Head/Body/Legs/Accessory
│   ├── EquipmentSlotResolver.cs — static: ItemType→EquipmentSlotType 매핑 (장착 불가면 false)
│   ├── ItemStack.cs          — ItemDefinition + Count
│   ├── Inventory.cs          — 아이템/장착 관리(Equip 1개 분리+슬롯 검증, Unequip Id 병합, GetRawMaterials가 req.Count 반영, Clear=런 리셋 시 장착 Unequip 후 전체 비움), event OnChanged/OnItemEquipped/OnItemUnequipped
│   └── MaterialPoolCalculator.cs — static: 인벤토리+장착 장비 분해 재료 합산
├── Crafting/
│   ├── CraftingSystem.cs     — Craft / Disassemble 로직, RecipeDatabase 의존
│   ├── RecipeMatcher.cs      — 순수 static: 놓인 글자 멀티셋 → 정확 일치하는 레시피 아이템(초과/부족 시 null). 발견형 크래프팅 두뇌
│   ├── CraftingBench.cs      — 순수: 크래프팅 슬롯 상태(배치/제거/Clear/PlacedMaterials/Result). UI가 그리고 판정은 RecipeMatcher에 위임
│   ├── RecipeAvailability.cs — 순수 static: 레시피+재료풀 → 재료별 필요/보유/부족 내역(표시용)
│   └── RecipeDatabase.cs     — ScriptableObject, ItemDefinition 목록 + FindMaterial + AddItem/AddMaterial(테스트/에디터용)
├── Puzzle/                   — 능력(capability) 판정·타깃 프레임워크 (레이어2 클리어 조건)
│   ├── CapabilityMatch.cs    — 순수 static: required==None이면 항상, 아니면 적용 능력집합에 포함 시 해제(DamageCalculator 스타일)
│   ├── SolveTracker.cs       — 순수: 여러 목표 집계, 전부 충족 시 OnSolved 1회 발화 + Reset(EnemyAI 스타일)
│   ├── CapabilityTarget.cs   — MonoBehaviour 장애물(Hurtbox 미러). TryApply(능력집합) 매칭 시 Resolve → 콜라이더/오브젝트 제거(물리 장애물 게이팅)
│   ├── RoomPuzzle.cs         — MonoBehaviour: 방 목표를 SolveTracker로 집계, 미해결 시 RoomManager.SetExitLock(true)/해결 시 TryClearCurrentRoom(클리어 플래그 게이팅)
│   └── SubWeaponUser.cs      — MonoBehaviour(플레이어 부착): UsePerformed 시 앞쪽 OverlapBox → CapabilityTarget에 EquippedSubCapabilities 적용(Hitbox의 use 버전, 두 번째 적용원)
└── UI/
    ├── BossRewardUI.cs       — 보스 처치 시 선택형 보상 카드(2~3장, UITheme). RoomManager.OnBossRoomCleared 구독→택1 인벤 지급→SpawnNextFloorPortal. HUD가 Canvas 부착
    ├── VictoryUI.cs          — 최종 층 클리어(GameManager.OnGameCleared) 승리 화면 + 재시작. HUD가 Canvas 부착
    ├── MinimapUI.cs          — 우상단 미니맵: 방문한 방 + 그 인접 방만 표시(안 가본 먼 방 숨김). 현재=노랑/방문=유형색/인접 미방문=존재만(특수방은 흐린 유형색+글자 힌트 B·T·$). RoomManager.OnRoomEntered로 방문·현재 갱신, 층변경·사망 시 리셋. HUD가 Canvas 부착
    ├── HpBarUI.cs            — 좌상단 프레임형 체력바(런타임 구성): 흰 테두리+어두운 트랙+비율색 채움(anchorMax.x 스케일)+데미지 트레일(피해 시 빨간 조각이 남았다 서서히 감소)+수치. 플레이어 HP 이벤트 구독. HUD가 Canvas에 자동 부착(레거시 중앙 Slider는 숨김)
    ├── UITheme.cs            — 공용 UI 테마 + 위젯 팩토리(팔레트/폰트/여백 중앙화, 레트로 픽셀 톤). 흰 테두리+하드 그림자+hover 버튼, 패널 프레임/헤더/본문 생성. CraftingBenchUI/InventoryGridUI/HUD가 공유(값 하나로 전체 톤 조정)
    ├── HUD.cs                — 인벤토리 버튼(_inventoryUI는 MonoBehaviour로 SendMessage("Toggle") — UI 구현 무관). Start에서 레거시 중앙 Slider 숨기고 HpBarUI를 Canvas에 부착
    ├── CraftingBenchUI.cs    — 발견형 슬롯 배치 크래프팅(런타임 자체 구성: 슬롯행/글자팔레트/결과/제작). 글자 선택→슬롯 지정 배치, CraftingBench/RecipeMatcher에 위임
    ├── InventoryGridUI.cs    — 슬롯형 인벤토리(장비 슬롯 6칸 고정 + 가방 그리드 _bagCapacity=24 빈슬롯 항상 표시, 선택 시 장착/해제/분해 액션 행, 런타임 자체 구성)
    ├── InventoryUI.cs        — 아이템 목록 + 장착/해제 버튼(CraftButton 재활용, EquipmentSlotResolver로 슬롯 결정) + 분해 버튼. 장착품도 목록 표시
    ├── CraftingUI.cs         — 레시피 목록, 제작 가능 여부 표시 (판정도 GetRawMaterials로 실제 Craft와 일치)
    └── RecipeDatabaseBridge.cs — 씬에서 RecipeDatabase SO를 CraftingUI에 노출

Assets/Editor/
├── SetupGameAssets.cs         — 메뉴: Create All Game Assets(재료·무기·KEY+RecipeDatabase) / Assign RecipeDatabase / Switch To Slot Crafting UI / Switch To Grid Inventory UI
├── RoomContentSetup.cs        — 메뉴: Setup Data-Driven Room Content(콘텐츠 프리팹 Combat/Puzzle/Tutorial + RoomContentLibrary + RoomManager 연결 + 시작 시딩 끔). Room_Tutorial=K·Y 줍기 + 잠긴 문(Unlock)+RoomPuzzle
├── PrefabSetup.cs             — 빌딩블록 프리팹(Enemy/BreakableWall/IceWall/RoomPuzzle) 생성 메뉴
└── SpriteGenerator.cs         — 메뉴: Help/Setup/Generate Placeholder Sprites. 프로시저럴 픽셀아트 PNG 생성(32px,PPU32,Point) → Assets/Sprites/*.png 임포트 + 타일/씬 SpriteRenderer 할당

Assets/Sprites/                — 실제 PNG 스프라이트 에셋(임시 내장 사각형 대체)
├── player_E.png              — 플레이어 = 하늘색 알파벳 E (테마)
├── enemy_blob.png            — 적(빨간 크리처)
├── tile_floor.png / tile_wall.png — 방 셸 타일(잔디+흙 / 벽돌)
└── tile_door_open.png / tile_door_locked.png — 문 상태 타일(녹색 아치 / 빨강+자물쇠)

Assets/Prefabs/
├── ItemSlot.prefab           — InventoryUI 슬롯 (Name/CraftButton/DisassembleButton, HorizontalLayoutGroup)
└── RecipeSlot.prefab         — CraftingUI 슬롯 (Name/Requirements/CraftButton)

Assets/ScriptableObjects/
├── Materials/mat_A.asset ~ mat_Z.asset (E 제외 25개, 자기참조 Recipe 포함)
├── Items/blade.asset, spear.asset, axe.asset, knife.asset, rapier.asset, saber.asset
└── RecipeDatabase.asset      — GameManager._recipeDatabase에 연결됨

Assets/Tests/
├── EditMode/Tests.EditMode.asmdef
│   ├── DamageCalculatorTests.cs          (5개) — 불일치 데미지 최소 1 보장 포함
│   ├── PlayerStatsTests.cs               (11개) — 데미지/방어/힐 + 사망 후 OnDied 미재발화·Heal 부활 방지·Reset 부활 + 장비 보너스 균형·Reset base 복원
│   ├── DungeonGeneratorTests.cs          (5개)
│   ├── MaterialPoolCalculatorTests.cs    (3개)
│   ├── InventoryTests.cs                 (3개) — OnItemEquipped/OnItemUnequipped 이벤트 검증
│   ├── EntryRequirementCheckerTests.cs   (5개) — 방 진입 판정 검증
│   ├── RoomNavigatorTests.cs             (6개) — 방향 이동/진입 판정 (NoDoor/Blocked/Entered)
│   ├── FloorValidatorTests.cs            (9개) — 재료 보장 불변식 (소비/열쇠 재사용/도달성 포함)
│   ├── EquipmentSlotResolverTests.cs     (7개) — ItemType→EquipmentSlotType 매핑
│   ├── InventoryRawMaterialsTests.cs     (2개) — GetRawMaterials가 req.Count 반영
│   ├── InventoryEquipTests.cs            (5개) — Equip 스택 분리/슬롯 검증, Unequip 병합, Clear(장착 Unequip+비움)
│   ├── DungeonGeneratorFloorTests.cs     (4개) — 생성기가 진입조건 부여+재료 보장 불변식 만족(30시드)
│   ├── AlphabetWordRuleTests.cs          (5개) — "단어−E=레시피" 규칙 + 실제 무기 에셋 검증
│   ├── TestSceneWiringTests.cs           (7개) — 씬에 Hitbox/RoomManager/Hurtbox(비활성 포함 조회) 실재 + PlayerController 직렬화 값(_groundLayer/_groundCheck) + InventoryUI/CraftingUI 각 1개·활성·_panel 배선 + EventSystem 존재 검증
│   ├── RoomLayoutTests.cs                (4개) — 사이드뷰 셸(바닥/벽/빈 내부/둘레 셀수) 순수 로직
│   ├── RoomManagerRenderTests.cs         (4개) — 방 셸 렌더링(바닥/벽/빈 내부/재진입) + 열린 문 타일 + EnsureColliders 콜라이더 부착
│   ├── DoorStateEvaluatorTests.cs        (4개) — 문 상태 None/Open/Locked 판정
│   ├── DoorDirectionPickerTests.cs       (7개) — 문 방향 선택(Nearest/NearestAmong/Opposite)
│   ├── EnemyAITests.cs                   (20개) — AI 상태전이(정찰 왕복/Chase/Attack/이력현상/사거리 게이팅) + 경계값 + 수평 데드존 + Reset + Ranged 카이팅(접근/밴드 정지발사/근접 후퇴/양방향/Melee 대조)
│   ├── EnemyStatsTests.cs                (6개) — 데미지/클램프/사망 이벤트/LockedElement + 사망 후 OnDied 미재발화 + Reset
│   └── EnemyLootTests.cs                 (6개) — 처치 재료 드롭(속성 단어 글자−E, None=빈, 전 속성 비어있지 않음, 글자수 불변식)
└── PlayMode/Tests.PlayMode.asmdef

EditMode 테스트 총 145개. (RoomContentLibrary.SelectIndex 3 추가) (전투 AI/스탯/드롭 32개 + 사망·런 리셋 + 퍼즐 프레임워크 13개[CapabilityMatch 5·SolveTracker 5·EntryRequirementChecker 능력 3]) **2026-07-11~12: MCP로 실제 실행 검증(141/141 통과) + Play 실측(전투 6항목 + 퍼즐: 크래프팅→장착→EquippedCapabilities→CapabilityTarget 해제·Hitbox 공격 적용·문잠금).**
```

## TestScene 구성 (Assets/Scenes/TestScene/TestScene.unity)

```
Main Camera       — Orthographic(size 6), CameraFollow → Player
Ground            — (비활성화됨, m_IsActive:0) 레거시 임시 발판. 방 셸 콜라이더가 지면을 대체하므로 비활성. 필요 시 재활성으로 폴백 가능
Player            — SpriteRenderer(하늘색), Rigidbody2D(freezeZ), CapsuleCollider2D,
│                    PlayerController(_groundLayer=Ground, _groundCheck=GroundCheck),
│                    PlayerInput(InputSystem_Actions), Tag: Player
├─ GroundCheck    — pos(0,-0.55,0) 로컬
└─ Hitbox         — BoxCollider2D(trigger, size 1,1), Hitbox, localPos(0.6,0,0) — 공격 판정
Enemy             — SpriteRenderer(빨간색), Rigidbody2D(freezeZ), BoxCollider2D, EnemyBase(AI 상태머신), pos(3,-2,0), _lockedElement=Fire(열쇠-자물쇠 실증), m_IsActive:1(전투 루프 복구)
└─ Hurtbox        — BoxCollider2D(trigger), Hurtbox
GameManager       — GameManager 스크립트, _recipeDatabase → RecipeDatabase.asset 연결 완료
RoomManager       — RoomManager 스크립트(_tilemap/_floorTile/_wallTile 연결). Start()에서 던전 생성(StartRun)+로드(LoadMap)+방 렌더링
└─ Grid           — Grid + 자식 Tilemap(+TilemapRenderer). RoomManager가 여기에 방을 그리고, 런타임에 TilemapCollider2D+CompositeCollider2D+static Rigidbody2D를 부착(Ground 레이어)해 방 셸이 실제 지형이 됨
Canvas            — ScreenSpaceOverlay
├─ HUD            — HUD 스크립트, _hpBar/InventoryButton 연결됨
├─ InventoryPanel — InventoryUI, 화면 정중앙 520×420 배경 패널, ItemListParent(VerticalLayoutGroup) +
│                    ItemSlot.prefab/_craftingUI 전부 연결 완료
└─ CraftingPanel  — CraftingUI + RecipeDatabaseBridge, 화면 정중앙 520×420 배경 패널,
                     RecipeListParent(VerticalLayoutGroup) + RecipeSlot.prefab/Database 전부 연결 완료
```

## MCP 설정

- **MCP for Unity** (`mcp-for-unity-server`) HTTP transport, `http://127.0.0.1:8080/mcp`
- `ProjectSettings/McpUnitySettings.json` — Unity 에디터 측 서버 설정
- `manage_scene`/`manage_gameobject`/`manage_components`/`execute_code`/`read_console`/`run_tests` 등으로 씬·컴포넌트·테스트 원격 제어 가능
- **알려진 제약**: 에디터 창이 OS 포커스를 못 받는 세션에서는 Game 뷰 스크린샷(`manage_camera` screenshot)이 갱신 안 되거나 캐시된 프레임을 반환할 수 있음 (`PlayerSettings.runInBackground`를 켜도 해결 안 됨 — 이건 게임 로직 틱 여부와 별개로 EditorWindow 리페인트 문제). 좌표/컴포넌트 값은 `execute_code`/`resources/read`로 항상 정확히 확인 가능하니 시각적 검증이 필요하면 로컬에서 직접 Play 권장

## 핵심 클래스 설계

> 구현 완료된 항목은 위 파일 구조 참조. 미구현 항목만 아래에 기록.

### 던전 생성 (Help.Dungeon)

```
DungeonGenerator (순수 C#) — 구현 완료
├── Generate(config) → DungeonMap : 랜덤 워크 레이아웃 + 방 유형 (조건/루트 없음)
├── Generate(config, RecipeDatabase) → DungeonMap : 위 + 진입조건 부여 + 재료 배치 + 검증
│   ├── CombatPuzzle 방 → RequiredElement, EnvironmentPuzzle 방 → RequiredWeapon (DB에서 제작 가능한 능력 중 랜덤)
│   ├── FloorValidator.TryPlanRequiredMaterials로 필요 재료 계산 → 자유 방 GuaranteedLoot에 분산
│   ├── FloorValidator.Validate로 검증, 실패 시 다른 시드로 재시도(최대 30회)
│   └── 끝까지 실패 시 조건 전부 제거(폴백 — 항상 유효)
└── GameManager.StartRun이 Generate(config, _recipeDatabase) 호출

RoomTemplate (ScriptableObject)
├── 타일 데이터
├── 적 스폰 포인트
├── 아이템 스폰 포인트
├── 문 위치
├── RoomType (Combat, EnvironmentPuzzle, CombatPuzzle, PurePuzzle, Treasure, Shop, Boss, Secret)
└── EntryCondition 목록 (진입에 필요한 장비/속성 조건)

> `EntryRequirementChecker.CanEnter(Room, Inventory, RecipeDatabase)` 및 `MaterialPoolCalculator`는 구현 완료 — 위 파일 구조 참조.
> `RoomNavigator.TryEnter(current, Direction, DungeonMap, Inventory, RecipeDatabase, out target)` → `RoomEntryResult`도 구현 완료. `RoomManager.TryEnterRoom(Direction)`이 이를 호출해 실제 방 전환(진입 성공 시 EnterRoom, 조건 불충족 시 OnEntryBlocked 통지)을 수행한다. 문 UI(아이콘/잠김 시각화)는 아직 미구현.

FloorValidator (순수 C#) — 구현 완료 (위 파일 구조 참조)
├── Validate(DungeonMap, RecipeDatabase) → bool
│   "자유 입장 방의 GuaranteedLoot 합산 재료로 모든 조건부 방 진입 가능한가?"
├── BuildFreeRoomPool(DungeonMap) → Dictionary<AlphabetMaterial,int> (자유 방 루트만 합산)
├── 데이터 모델: Room.GuaranteedLoot (List<MaterialRequirement>)
├── 소비/열쇠 인식: 같은 요구는 무기 1자루로 재사용(조건 중복 제거), 서로 다른 요구는 각각 제작하며 재료 소비 시뮬레이션 → 공유 재료 경쟁 시 unwinnable 층을 걸러냄
├── 도달성(reachability): ReachableFreeRooms(start에서 자유 방만 거쳐 BFS)의 loot만 집계 → 잠긴 방 뒤에 재료가 갇힌 교착 층을 걸러냄. 생성기 PlaceLoot도 도달 가능한 자유 방에만 배치. (단순/보수적 모델 — 열쇠로 열면 넓어지는 반복 확장은 미적용)
├── TryPlanRequiredMaterials(conditions, db, out pool) : 조건 충족에 필요한 최소 재료 계산 (생성기 루트 배치용)
├── 제작 대상 선택이 탐욕적(조건 만족+최소 재료)이라 드문 배치에서 false-negative 가능(생성 재시도로 안전), false-positive는 제거
└── DungeonGenerator.Generate(config, db)가 생성 파이프라인 마지막 단계로 호출 (연결 완료)

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
ItemDefinition (ScriptableObject) — 실제 필드: Id, Word, Type(ItemType), Element, WeaponCategory, Recipe, AttackBonus/DefenseBonus/AttackSpeedMult
├── 장착 슬롯은 별도 필드가 아니라 ItemType에서 EquipmentSlotResolver로 유도 (Weapon→Weapon, HeadArmor→Head, …)
└── Rarity, icon 등은 미구현 (논의 필요)

Inventory (순수 C#) — 구현 완료
├── List<ItemStack> items (아이템 + 수량), Dictionary<EquipmentSlotType, ItemStack> equipped
├── Add / Remove / CountOf / Equip(1개 분리+슬롯 검증) / Unequip(Id 병합) / GetRawMaterials(req.Count 반영)
└── event OnChanged, OnItemEquipped(ItemDefinition), OnItemUnequipped(ItemDefinition)
    — PlayerController가 구독해 PlayerStats 장비 보너스 + 무기 EquippedElement 적용/해제

CraftingSystem (순수 C#) — 구현 완료
├── CanCraft(itemId, pool) / Craft(itemId, Inventory) / Disassemble(itemId, Inventory)
└── EntryRequirementChecker가 재사용 (방 진입 판정에서 CanCraft 그대로 활용)
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
| 2026-04-20 | Help.asmdef — Unity.InputSystem + UnityEngine.UI 명시 참조 | asmdef 사용 시 패키지 어셈블리를 직접 참조해야 컴파일 가능 |
| 2026-04-20 | DungeonGenerator 알고리즘: 랜덤 워크 | 프로토타입 단계에서 간단하고 EditMode 테스트 가능 |
| 2026-04-20 | UI — 레거시 uGUI (Canvas/Text/Button/Slider) 사용 | 프로토타입 속도 우선, 추후 UI Toolkit 전환 검토 |
| 2026-04-20 | 스프라이트 — 런타임 생성 Texture2D (임시) | 에셋 없이 빠른 시각 확인용, 에셋 작업 시 교체 예정 |
| 2026-04-20 | CameraFollow — LateUpdate 보간 방식 | 물리 업데이트 후 처리로 떨림 방지 |
| 2026-07-04 | 장비 스탯 반영은 Inventory 이벤트 + PlayerController 구독 방식 | Help.Inventory가 Help.Player를 직접 참조하지 않도록 이벤트 기반 결합 유지 (ARCHITECTURE 원칙 준수) |
| 2026-07-04 | EntryRequirementChecker는 CraftingSystem.CanCraft 재사용 | HasEnoughMaterials 로직 중복 방지, 기존 검증된 코드 그대로 활용 |
| 2026-07-04 | RecipeDatabase에 AddItem/AddMaterial 공개 메서드 추가 (구조적 변경) | EditMode 테스트에서 SerializedObject 없이 순수 C#으로 데이터베이스 구성 가능하게 함 |
| 2026-07-04 | UI 프리팹은 uGUI로 유지, UI Toolkit 전환 보류 | 기존 InventoryUI/CraftingUI가 uGUI 전제로 작성되어 있어 지금 전환하면 스크립트 재작성 필요 — 추후 UI 전면 개편 시 논의 |
| 2026-07-04 | 방 이동 판정을 순수 로직 `RoomNavigator`로 분리, `RoomManager`는 호출만 | MonoBehaviour/로직 분리 원칙 유지 → 방향 이동+진입 판정을 EditMode에서 테스트 가능 (RoomNavigatorTests 6개) |
| 2026-07-04 | `RoomManager.TryEnterRoom(Direction)` + `OnEntryBlocked` 이벤트로 진입 판정 연결 | 판정 실패 시 UI 피드백 훅 제공. `GameManager`에 `RecipeDatabase` 공개 접근자 추가해 판정에 필요한 DB 전달 |
| 2026-07-04 | 방 루트 데이터 모델 = `Room.GuaranteedLoot` (확정 지급 재료 리스트) | 재료 보장 불변식 검증에 필요한 최소 모델. 가중치 랜덤 대신 확정 리스트 → 생성기가 필요 재료를 배치하고 검증 가능 |
| 2026-07-04 | `EntryRequirementChecker`에 재료 풀 오버로드 추출 (구조적 변경) | `FloorValidator`가 층 재료 풀로 진입 판정을 재사용 — 로직 중복 방지 |
| 2026-07-04 | `FloorValidator` 구현 (재료 보장 불변식 검증) | 자유 방 루트 합산 → 조건부 방 진입 판정. 테스트 5개 |
| 2026-07-04 | (QA) 통합 결함 일괄 수정 — 독립 테스터 페르소나 보고 기반 | cold-start 서브에이전트가 시스템 결합부 정독 → D1~D5·R1~R5 발견, 코드로 재검증 후 수정 |
| 2026-07-04 | (QA-D4/D5) 무기 장착→EquippedElement→Hitbox 전투 배선 | 크래프팅한 속성 무기가 실제 전투(열쇠-자물쇠)로 이어지도록 연결. Hitbox가 AttackPerformed 구독해 활성화 |
| 2026-07-04 | (QA-D3) 장착 UI = InventoryUI의 CraftButton 재활용 | 프리팹 수술 없이 장착/해제 경로 제공. 슬롯은 EquipmentSlotResolver로 유도 |
| 2026-07-04 | (QA-D1) 크래프팅 판정 풀을 GetRawMaterials로 통일 | 버튼 활성화(판정)와 실제 Craft(실행)가 같은 재료 기준을 쓰도록 — 조용한 실패 제거. GetTotalPool은 진입 판정 전용 |
| 2026-07-04 | (QA-D2) FloorValidator 소비/열쇠 인식으로 개선 | 서로 다른 무기가 공유 재료 경쟁 시 unwinnable 층을 valid로 통과시키던 false-positive 제거 |
| 2026-07-04 | (QA-R1/R2/R3/R5) Inventory 정합성 수정 | Equip 1개 분리·슬롯 검증, Unequip Id 병합, GetRawMaterials req.Count 반영 |
| 2026-07-04 | (QA-R4) PlayerController OnDestroy 구독 해제 | 싱글턴 Inventory 이벤트 누수/유령 적용 방지 |
| 2026-07-04 | 입력 복구 — Inventory(I)/Dash(LeftShift)/Crafting(C) 액션 추가 + DefaultActionMap=Player | 기존엔 인벤토리·대시·크래프팅 여는 입력 자체가 없어 QA로 고친 장착→전투 체인에 인게임에서 도달 불가였음. Attack은 원래 좌클릭 바인딩 존재 |
| 2026-07-04 | 입력→UI는 PlayerController 이벤트(InventoryToggleRequested/CraftingToggleRequested)로 전달, UI가 구독 | PlayerController가 UI를 직접 참조하지 않도록 (이벤트 기반 결합 유지). SendMessages라 On* 핸들러는 Player 오브젝트의 PlayerController에 위치 |
| 2026-07-04 | 시작 재료 시딩(GameManager._seedStarterMaterials) | 인벤토리가 시작 시 비어 있어 제작할 게 없던 문제 — 프로토타입 테스트용 dev 시딩. FindMaterial 25/25 해석 확인 |
| 2026-07-04 | Crafting 단축키 = C (Crouch와 물리 키 공유, Crouch는 핸들러 없어 무해) | I=인벤토리와 직관적 대응. 추후 UI 개편 시 재검토 |
| 2026-07-04 | FloorValidator → DungeonGenerator 연결 (I1) | 생성기가 조건부 방에 진입조건 부여 + 자유방에 재료 배치 + 검증. 30시드 불변식 테스트 통과. FloorValidator 검증/계획 로직을 TrySelectKeyItems로 통일(구조적) |
| 2026-07-04 | 생성기 진입조건은 DB에서 제작 가능한 능력(원소/무기)에서만 부여 | 존재하지 않는 무기를 요구하는 클리어 불가능 방 방지 |
| 2026-07-04 | (QA2-DA) 씬 Player에 Hitbox 오브젝트 추가 | D5에서 Hitbox 스크립트는 고쳤으나 씬에 히트박스 오브젝트가 없어 공격이 무효였음. execute_code로 추가+저장 |
| 2026-07-04 | (QA2-DB) 씬에 RoomManager 추가 + Start()에서 StartRun/LoadMap 호출 | StartRun/LoadMap 호출자가 없어 던전 시스템이 런타임 비활성이던 문제 해결 |
| 2026-07-04 | (QA2-DC) DamageCalculator 불일치 데미지 최소 1 보장 | base<10에서 정수 절삭으로 0(완전 면역)이 되던 불변식 위반 수정 |
| 2026-07-04 | (QA2-DD/DE) 레시피=단어−E 규칙 도입 + AXE/RAPIER 데이터 수정 | AlphabetWordRule로 규칙화, 실제 에셋 검증 테스트 추가. AXE(A2→1)/RAPIER(R1→2) 오류 수정 |
| 2026-07-04 | 씬 배선 회귀는 TestSceneWiringTests(EditorSceneManager)로 자동 검증 | 씬에 필요한 오브젝트 존재 여부를 EditMode에서 검증 — D-A류 재발 방지 |
| 2026-07-04 | (QA3-D1) 씬 _groundLayer 마스크 설정(빈 마스크→Ground) | 마스크가 0이라 접지 판정이 항상 false→점프 불능이던 결함 수정. 씬 테스트를 직렬화 값까지 검증하도록 강화 |
| 2026-07-04 | (QA3-N1) 씬 Enemy _lockedElement=Fire | 기본 씬에 속성 잠금 적이 없어 열쇠-자물쇠 전투를 실증 못 하던 데이터 공백 보완(SABER=Fire로 실증 가능). ※ UI 디버깅 중 임시로 None+비활성했다가 2026-07-09 전투 루프 복구 시 **재활성+Fire 복원**(현재 씬 상태) |
| 2026-07-04 | (QA3-N2) HP Slider에 Fill 생성+FillRect 연결 | HP바가 시각적으로 갱신되지 않던 문제 |
| 2026-07-04 | RoomManager 방 렌더링 구현 (껍데기→실제 그림) | 순수 로직 RoomLayout(방 크기+문 방향→타일 배치)을 TDD, RoomManager가 진입 시 Tilemap에 렌더링. 타일 에셋(Assets/Tilemaps/Floor·WallTile)+씬 Grid/Tilemap을 execute_code로 생성·배선. play 모드 없이 EditMode로 렌더링 검증(HasTile 셀 카운트) |
| 2026-07-04 | (QA4-D1) FloorValidator 도달성(reachability) 검증 추가 | 재료가 잠긴 방 뒤에 갇히는 교착 층 방지. 밸리데이터+생성기 배치 모두 "도달 가능한 자유 방"만 사용(단순 BFS 모델). 30시드 생성 불변식이 도달성까지 포함해 통과 |
| 2026-07-04 | 문 시각화 + 방 간 이동 연결 | DoorStateEvaluator(문 상태)+DoorDirectionPicker(방향) 순수 로직 TDD. RoomManager가 문 상태 타일 오버레이 + Interact(E)로 가장 가까운 문 이동. 문 타일 에셋 생성·배선. "왜 잠겼는지"는 비공개(DESIGN 준수) |
| 2026-07-04 | (QA5-결함2) 방 이동 실사용화 | 실재 문 중 선택(NearestAmong)+전환 시 반대편 문으로 플레이어 재배치(RepositionPlayerAtDoor) → 복귀·자유 탐색 가능. EditMode 87개 |
| 2026-07-09 | 방 콜라이더 연동 + 플랫포머 셸 재설계(QA6-D1) | RoomLayout이 사이드뷰 셸(바닥+벽+빈 내부) 생성, RoomManager.EnsureColliders가 Tilemap 런타임 콜라이더 부착. 타일 ColliderType=Grid(빈 physics shape로 인한 통과 위험 제거). 문=E키(셸 밀폐). EditMode 90개 |
| 2026-07-09 | 몬스터 AI 상태머신 + 전투 루프 복구 | 무지성 추격→순수 로직 EnemyAI(Patrol/Chase/Attack+이력현상+사거리 쿨다운 공격, EditMode 테스트 가능). 접촉 상시데미지 제거. 씬 Enemy 재활성+Fire 잠금(열쇠-자물쇠 실증). EnemyAI/Stats 테스트 19개. QA3 재검증: 사망 후 OnDied 재발화 가드(EnemyStats·PlayerStats), 수평 데드존, 경계값 테스트 |
| 2026-07-09 | 적 처치 재료 드롭(전투→크래프팅 루프) | 순수 EnemyLoot이 잠금 속성 단어 글자(−E)를 재료로 매핑(Fire→F,I,R), EnemyBase.HandleDeath가 Inventory에 지급. 속성 처치 보상이 그 속성 계열 제작 재료가 됨. EditMode 114개 |
| 2026-07-09 | (QA6 통합감사) 비모달 UI 입력 게이트·적 리셋·E 탭 | I-1(High): 패널 오픈 중 좌클릭이 공격으로/WASD가 이동으로 새던 것을 PlayerController.SetUiPanelOpen 게이트로 차단. I-2: 런 리셋 시 생존 적을 HP/AI/위치 복구(OnRunReset 구독). I-3: Interact 액션 Hold 제거→탭 E로 방 이동. EditMode 123개. ⚠ 셋 다 런타임 검증 대기 |
| 2026-07-09 | (QA7 회귀검증) L-1 수정 + L-2 설계결정 | L-1(수정): 패널 연 채 사망→부활 후 입력 잠금 잔존 → InventoryUI/CraftingUI가 OnRunReset 구독해 패널 자동 닫음. L-2(결정): 비모달 UI는 **일시정지 안 함**(입력만 게이트, 적/물리 계속) — Time.timeScale 조작 리스크 회피, 향후 옵션 검토. L-3: HandleRunReset 통합은 PlayMode 테스트 부재(추후). QA7 회귀 Critical/High/Medium 0 |
| 2026-07-09 | 사망 & 런 리셋(로그라이크 루프 닫기) | 플레이어 사망→GameManager.RestartRun(인벤토리 Clear+시드+새 던전+OnRunReset)→RoomManager 방 재로드·재배치, PlayerStats.Reset 부활(base 능력치 보존해 장비 이월 이중 차단). 사망 후 TakeDamage/Heal 무효로 부활은 Reset만. 메타 진행은 미구현(완전 초기화). ⚠ 씬 Enemy는 scene 오브젝트라 리셋 시 재스폰/재배치 안 됨(프로토타입 한계 — 향후 던전 데이터 기반 스폰). EditMode 121개 |
| 2026-07-11 | 런타임 Play 검증 완료(MCP curl 직접 구동) | Play 6항목 실측 PASS(항목별 수치는 memory project_prototype_state 항목27). ⚠ Attack 입력→OnAttack 발화만 시뮬 한계로 미확정(데미지 체인은 정상, 사용자 좌클릭 확인 필요). 방법론: runInBackground=true 세팅해야 비포커스 에디터서 Play 루프가 틱 |
| 2026-07-11 | 적 아키타입 EnemyArchetype(Melee/Ranged) | 순수 EnemyAI.AttackDecision이 Archetype 분기 — Ranged는 dist<StandoffRange면 -ChaseMove(후퇴)하며 공격, 그 외 정지 발사. EnemyBase에 _archetype/_standoffRange 직렬화. 기본 Melee라 회귀 없음. EditMode 128개 |
| 2026-07-12 | 스프라이트 에셋화 — 프로시저럴 PNG(SpriteGenerator) | 임시 내장 사각형(색 틴트)→실제 PNG 6종(player_E/enemy/floor/wall/door×2, 32px·PPU32·Point). 타일 에셋 sprite+색 리셋, 씬 Player/Enemy SpriteRenderer 할당·저장. 1스프라이트=1world unit(타일 1칸·플레이어 콜라이더 1u 정합). 아트는 placeholder, 교체 용이(같은 메뉴 재생성) |
| 2026-07-12 | 퍼즐 프레임워크 — 능력(Capability) 태그 기반, 확장 우선 | 핵심 판정="요구 아이템을 지금 재료로 만들 수 있나"(속성은 다양성용 한 축). Capability enum이 아이템 타입과 직교 — 아이템 제공/퍼즐 요구. 레이어1(입장)=EntryRequirementChecker에 능력 필터 1줄 추가(가산적). 레이어2(클리어)=CapabilityTarget(Hurtbox 미러)+CapabilityMatch, 적용 출처 무관(C 모델): Hitbox 공격이 EquippedCapabilities를 타깃에 전달(적엔 데미지, 장애물엔 능력). PlayerController.EquippedCapabilities(무기 장착 시 반영). 진행 관문 둘 다: 물리 장애물(Resolve→콜라이더 제거) + 클리어 플래그(RoomManager.SetExitLock/TryClearCurrentRoom). 첫 퍼즐=부서지는 벽(axe에 BreakWall). 확장=enum값+아이템+CapabilityTarget 추가로 프레임워크 불변 |
| 2026-07-12 | (구현 발견) 출구 잠금을 RoomType이 아니라 RoomPuzzle 존재로 구동 | 방 콘텐츠가 데이터 스폰 전이라, 퍼즐 유형 방을 RoomType만으로 잠그면 클리어할 목표가 없어 플레이어가 갇힘. → _exitLocked 플래그를 RoomPuzzle(Awake, 목표 있고 미해결 시)만 켜게 변경. 기본 false=자유 출입(회귀 방지). 런타임 확인: 기본 Entered, 잠금 Blocked, 해제 통과 |
| 2026-07-12 | 데이터 기반 방 콘텐츠 스폰 (프레임워크 준비) | 적/퍼즐/루팅을 씬 손배치→콘텐츠 프리팹(방 유형별)으로 데이터 저작. RoomContentLibrary(RoomType→프리팹) + RoomManager가 방 로드 시 Pick→Instantiate, 방 이동/사망리셋 시 교체·재스폰. 씬 손배치 Enemy/BreakableWall/IceWall 제거. **부수효과: "적이 리셋 시 재스폰 안 됨" 기존 한계 해결.** 빌딩블록 프리팹(Enemy/BreakableWall/IceWall/RoomPuzzle)+콘텐츠 프리팹(Room_Combat/Room_Puzzle)+메뉴(Create Building Block Prefabs / Setup Data-Driven Room Content). 개발 시작 가이드 Docs/HOWTO_ADD_CONTENT.md. 런타임 검증(방별 스폰·이동 교체·리셋 재스폰). EditMode 145 |
| 2026-07-12 | 퍼즐 Phase 2 — 서브무기 슬롯 + 사용(use) 액션 | ItemType.SubWeapon + EquipmentSlotType.SubWeapon(무기와 별개 슬롯). PlayerController: EquippedSubCapabilities(서브무기 장착 시), UsePerformed 이벤트, OnUse 입력, FacingDir. SubWeaponUser(플레이어 부착)가 UsePerformed→앞쪽 OverlapBox→CapabilityTarget.TryApply(서브능력) = 출처 무관 두 번째 적용원(CapabilityTarget/판정 불변). Use 입력=우클릭/F. 데모: FLARE(SubWeapon,Melt,레시피 FLAR)+IceWall(Melt). 런타임: axe+flare 동시 장착, use로 얼음 녹임, Melt는 BreakWall 벽 안 부숨(능력 분리). EditMode 142. Phase3(로프 물리)는 추후 |
| 2026-07-23 | 발견형(슬롯 배치) 크래프팅 도입 | 레시피 목록 대신 슬롯에 글자를 놓아 정확 일치 시 제작. `RecipeMatcher`(놓은 글자 멀티셋→정확일치 아이템, 순수)·`CraftingBench`(슬롯 상태, 순수)·`RecipeAvailability`(재료별 필요/보유, 순수) + `CraftingBenchUI`(런타임 자체 구성, 글자 선택→슬롯 지정 배치). 매칭=순서 무관 정확 일치. 씬 배선=에디터 메뉴 `Switch To Slot Crafting UI`. `Help.Crafting`/`Help.UI` |
| 2026-07-23 | 그리드 인벤토리 UI | `InventoryGridUI`(GridLayoutGroup 셀 + 선택 시 장착/해제/분해 액션 행, 런타임 자체 구성). HUD `_inventoryUI` 타입을 `InventoryUI`→`MonoBehaviour`+`SendMessage("Toggle")`로 구현무관화(UI 교체해도 HUD 버튼 유지). 메뉴 `Switch To Grid Inventory UI`가 교체+HUD 재배선 |
| 2026-08-03 | 미니맵(방문+인접 노출) | 우상단 `MinimapUI`(런타임 구성): **방문한 방 + 그 인접 방만** 표시(안 가본 먼 방은 숨김, 로그라이크 표준). 현재=노랑, 방문=유형색(보스 빨강/보물·상점 골드/시작 시안/일반 회청), 인접 미방문=존재만(일반 어둡게) — **특수방(보스/보물/상점)은 인접 시 흐린 유형색+힌트 글자(B/T/$)로 입장 전 확인**. IsAdjacentToVisited(문 연결 기준). RoomManager.OnRoomEntered로 방문·현재 갱신, OnFloorChanged/OnRunReset 리셋. HUD가 Canvas 부착. EditMode 219/219, Play 실측(9방 중 시작 시 4셀=시작+인접3, 비인접 보스 미표시) |
| 2026-08-03 | 절차 생성 클리어 가능성: 검증↔실제 재료 스폰 일치 | 기존 FloorValidator는 GuaranteedLoot 기준 검증(30시드 통과)했으나 **GuaranteedLoot가 런타임에 스폰 안 됨**=검증과 현실 괴리. 수정: RoomManager.ActivateRoomContent가 방 첫 방문 시 room.GuaranteedLoot를 **하이브리드 확정 공급**(적 있는 방=적에게 확정 드랍 chance1 라운드로빈 배분→전멸 시 획득 / 적 없는 방=바닥 MaterialPickup)으로 콘텐츠 캐시 하위 스폰(재방문 시 주운 것 보존). EnemyBase.AddGuaranteedDrop으로 런타임 주입. DungeonGenerator.PlaceBaseMaterials가 자유 방에 기본 재료 소량 추가(크래프팅 루프 활성, 필수 열쇠는 PlaceLoot가 보장). 방향=엄격 불변식+속성락은 진입조건에만(전투는 아무 무기로 클리어→순환의존 원천 배제). DropEntry=보너스(확률, 검증 제외) 명시. EditMode 219/219(신규: GuaranteedLoot⊆ReachableFreeRooms 위상, 30시드 기본재료>0), Play 실측(전투방=바닥픽업0·적확정드랍→전멸 시 재료, 보물방=바닥픽업, 캐시 보존) |
| 2026-07-26 | 돌 수 있는 던전: 전투 방 게이팅 + 보스/층 진행 + 선택형 보상 | "빈 방 지나가기"→실제 진행 루프. ①전투 방 클리어 게이팅: `EnemyCounter`(순수)+`EnemyClearObjective`(방 적 전멸 목표)를 `RoomPuzzle`이 집계→적 다 죽여야 출구 열림(기존 SolveTracker/출구잠금 재사용). ②보스=강화 브루트 `Enemy_Boss`(IsBoss, HP320)+`Room_Boss`. ③층 진행: `GameManager.AdvanceFloor`/`CurrentFloor`/MaxFloors=3, 보스 클리어→`NextFloorPortal` 스폰→진입 시 다음 층(OnFloorChanged→RoomManager 새 층 로드). 최종 층→OnGameCleared→`VictoryUI`. ④선택형 보상: `BossRewardPool`(순수, 완성 아이템 결정적 N개)+`BossRewardUI`(카드 택1→인벤 지급→포탈). ⑤Treasure/Shop 방=재료 픽업 콘텐츠. EditMode 217/217(신규 EnemyCounter 5+BossRewardPool 5), Play 실측(전투방 전멸→출구해제·보스 처치→방클리어·보상 택1→지급→포탈·AdvanceFloor 층++·최종층→승리, 예외0) |
| 2026-07-26 | 방 상태 보존(방별 콘텐츠 캐시) | 기존: 방 진입마다 콘텐츠를 Destroy+재Instantiate → 죽인 적·주운 드랍·푼 퍼즐이 매번 초기화. 수정: RoomManager가 방 좌표별 콘텐츠 인스턴스를 `_roomContent` 딕셔너리에 캐시 — 나가면 SetActive(false), 재방문 시 같은 인스턴스 재활성화(상태 보존). 진입 시 `RoomPuzzle.IsSolved`로 출구 잠금 재평가(Awake는 재활성화 시 안 돌므로). 드랍(MaterialPickup)·투사체(EnemyProjectile)를 방 콘텐츠 하위(transform.parent)로 넣어 함께 캐시. 런 리셋(새 던전) 시 LoadMap이 캐시 전체 파괴. Play 실측(재방문 시 동일 인스턴스·자식 수 보존, 이전 방 비활성화). EditMode 207/207 |
| 2026-07-26 | 몬스터 3종 + 물리 드랍 + 아처 투사체 | 적 유형 3종(Grunt 근접 기본 / Archer 원거리 카이팅+투사체 / Brute 둔중 탱커 HP90·강타·1.5배)을 베이스 Enemy.prefab에서 파생(에디터 `Create Enemy Type Prefabs`, 스탯·색·크기·드랍만 다름). `EnemyProjectile`(런타임 생성, 등속·플레이어 데미지·벽/수명 소멸) + EnemyBase.DoAttack이 Ranged면 FireProjectile. 드랍=`DropEntry`(테이블)+`DropRoller`(순수 확률 판정)→처치 시 `MaterialPickup.Spawn`으로 **월드 물리 드랍**(글자가 튀어나와 주움, 팝 애니메이션). 테이블 비면 속성글자 폴백. Room_Combat이 3종 배치. EditMode 207/207(신규 DropRoller 6), Play 실측(3종 설정·그런트 처치 시 픽업 스폰·아처 투사체 발사·예외0) |
| 2026-07-26 | 체력바 가시성 재구축 | 기존 HP바가 화면 중앙 100×100 정사각형(배경/테두리/텍스트 없음)이라 사실상 안 보임 → `HpBarUI` 신규: 좌상단 프레임형(흰 테두리+어두운 트랙+비율색 채움+**데미지 트레일**(피해 시 빨간 조각 남았다 서서히 감소)+"HP cur/max" 외곽선 텍스트). 채움=anchorMax.x 스케일(스프라이트 불필요). HUD가 레거시 Slider 숨기고 Canvas에 자동 부착. EditMode 201/201, Play 실측(좌상단 380×34 생성·40피해 시 채움 즉시 0.6·트레일 잔존 후 이징·텍스트 갱신·예외0) |
| 2026-07-26 | 공격 모션 시스템(데이터·절차적, 원거리 확장 고려) | "내가 공격했다"를 보이게 + 무기별 모션 분리 대비. Animator 클립 대신 **하나의 절차적 엔진 + 무기별 데이터**. `AttackMotionClock`(순수: Windup/Active/Recovery)·`AttackKind`(MeleeArc 구현/Projectile 자리)·`AttackMotionDef`(타이밍/사거리/범위/호 데이터)·`SlashVFX`(초승달 런타임 절차생성, 호 스윕+페이드)·`PlayerAttack`(드라이버: AttackPerformed→모션 클록, MeleeArc면 Hitbox를 모션 reach/size/타이밍으로 구동, Kind 분기에 원거리 자리). Hitbox는 자체 타이밍 제거→모션이 창 소유(Configure). PlayerController.EquippedWeaponCategory 노출. 원거리/마법은 투사체가 AttackDamage/Element/Capabilities를 그대로 실어 보내면 기존 판정 재사용(출처 무관). 기본 모션 1종, 무기별 분리는 데이터만 채우면 됨. EditMode 201/201(신규 AttackMotionClock 6), Play 실측(자동부착·공격 시 VFX 켜짐·사거리 배치·예외0). ⚠호 스윕/페이드 육안은 사용자 Play |
| 2026-07-26 | 전투 체감 토대(juice) + 적 공격 예비동작 | 재사용 컴포넌트 3종(자동 부착, 씬 배선 X): `HitFlash`(피격 플래시/텔레그래프)·`CameraShake`(Camera.main, static)·`HitStop`(timeScale=0 순간정지, static). **플레이어**: 무적시간(i-frame `_invulnDuration`)로 연속 피해 차단 + 넉백(`_knockbackTimer`가 이동 입력 무시) + 피격 시 플래시/히트스톱/셰이크(`TakeDamage(int,Vector2 source)` 오버로드). **적**: 즉발 데미지 → `EnemyMeleeAttack`(순수) 예비동작(Windup 텔레그래프=정지+경고색)→타격(사거리 재확인=회피 가능)→회복. 피격 시 넉백+경직(`_hitstunTimer`)+플래시. `Hitbox`가 적중 시 넉백/히트스톱/셰이크 유발. EditMode 195/195(신규 EnemyMeleeAttack 7), Play 실측(i-frame 2번째 차단·플레이어 넉백 vx-6·적 넉백 vx5·예외0). ⚠시각 연출 육안은 사용자 Play 필요 |
| 2026-07-26 | 슬롯형 인벤토리 + 체력바 가시성 | `InventoryGridUI` 재작성=온라인게임식 슬롯형: 장비 슬롯 6칸(EquipmentSlotType별 고정, 빈칸=유형 라벨) + 가방 그리드(`_bagCapacity`=24, 빈 슬롯 항상 표시, 아이템이 획득순 채움). 선택→장착/해제/분해. 체력바=`HUD.RefreshHp`가 비율별 색(시안>50%>노랑>25%>빨강) + 배경 어둡게 + 텍스트 외곽선(가독성). EditMode 188/188, Play 실측(장비6·가방24·예외0) |
| 2026-07-26 | UI 비주얼 밸류업 — 공용 테마(레트로 픽셀 아케이드) | `UITheme`(정적 팔레트+위젯 팩토리)로 두 UI에 중복돼 있던 Create* 헬퍼를 중앙화(구조적) 후 C 톤 적용(행위적): 거의 검은 패널 + 굵은 흰 테두리 + 하드 그림자 + 대문자 라벨, 액센트=노랑(선택/제작/장착)·보조=시안(결과/HP)·위험=빨강(해제/분해). 패널에 헤더 바+닫기 ✕ 추가. 버튼 hover/press=ColorBlock. HUD도 테마 색 적용. 값 하나로 전체 톤 교체 가능. EditMode 188/188, Play 실측(두 패널 Frame/Header/Body 생성·예외 0). ⚠실제 색 육안 확인은 사용자 Play 필요(MCP 스크린샷 stale) |
| 2026-07-23 | KEY 튜토리얼 수직 슬라이스 | `RoomType.Tutorial`(시작 방=전투없음) + `MaterialPickup`(트리거로 재료 획득, Room_Tutorial에 K·Y 배치) + `Capability.Unlock` + KEY(SubWeapon, 레시피 K+Y, Unlock). 잠긴 문=`CapabilityTarget`(Unlock)+`RoomPuzzle`(출구 잠금). 빈손 시작(시딩 끔). 흐름: 줍기→조합→장착→사용(F)→출구 해제→진행. 전체 Play 실측. 문서 `Docs/KEY_TUTORIAL.md` |
| 2026-07-23 | (수정) 잠긴 문 = 솔리드 콜라이더 | 트리거로 두면 플레이어가 문을 뚫고 지나가 벽까지 걸어가서 전방 Use 스캔이 문을 놓침(실측: 문 통과 후 x=5 미적용/문 앞 x=3 적용). 솔리드로 두면 문 앞에 막혀 서서 F가 확실히 맞음. 해제 시 오브젝트 비활성→통과 |
| 2026-08-14 | 층 루트를 `GuaranteedLoot`/`BonusLoot` 둘로 분리 | 예산제를 넣으면서 검증 대상과 나머지를 섞으면 재료 보장 불변식이 흔들린다. `GuaranteedLoot`=진입 열쇠 재료(도달 가능 자유 방에만, `FloorValidator` 검증 대상), `BonusLoot`=예산 나머지(조건부·보스 방 포함 맵 전체). 그 방을 열 열쇠는 이미 보장되므로 조건부 방에 보너스를 둬도 교착이 아니다 → 기존 위상 테스트(`GuaranteedLootOnlyInReachableFreeRooms`)가 그대로 성립 |
| 2026-08-14 | 층 예산 계산은 순수 `FloorLootPlan`, 배분은 순수 `LootDistribution` | 생성기(랜덤·상태)에서 "무엇을 만들 수 있게 할까"와 "누가 떨굴까"를 분리해 EditMode로 고정. `SelectRecipes`는 열쇠를 먼저 넣고(클리어 우선, count를 넘더라도) 나머지를 결정적 셔플로 채운다. `LootDistribution.Assign`은 섞은 순서로 라운드로빈 → 몫 차이 ≤1, 재료가 적보다 적으면 뒤쪽 적은 0개 |
| 2026-08-14 | (결함 수정) 레시피가 빈 아이템은 제작 대상에서 제외 | `HasEnoughMaterials`가 빈 레시피에 항상 true를 돌려줘 **재료 없이 무한 제작**되던 구멍. 포션(ELIXIR)처럼 드랍 전용 아이템을 넣는 순간 터질 자리였다. `IsBasicCraftable`에 레시피 존재 조건을 추가하고, 에셋 단어 규칙 검사도 "레시피 있는 제작 대상"만 보도록 좁혔다 |
| 2026-08-14 | 제작 모드는 `CraftRule.CanCraftWith(item, mode)` 한 곳에서 판정 | 특수방 제작대가 E 규칙만 면제받는 두 번째 경로다. 모드별 분기를 호출부(매칭·판정·실행)에 흩뿌리지 않고 술어 하나로 모음 — `RecipeMatcher.FindExact`/`CraftingSystem.CanCraft`/`Craft`가 같은 규칙을 공유하고, 기본값이 `Basic`이라 기존 호출부는 회귀 0. 재료·레시피 요건은 모드와 무관하게 동일 |
| 2026-08-14 | 특수 제작의 골드 차감은 **제작 성공 뒤에** | 먼저 차감하면 제작 실패 시 골드만 잃는다. `CraftingSystem`은 순수하게 두고(지갑 비의존) UI가 잔액 확인 → `Craft` → `TrySpend` 순서로 처리 |
| 2026-08-14 | 재화 픽업은 `MaterialPickup`을 건드리지 않고 `RewardPickup`으로 미러 | 기존 글자 드랍 경로(방 캐시 귀속·팝 애니메이션)는 검증이 끝난 코드라 그대로 두고, 같은 규약을 따르는 형제 컴포넌트를 추가. 적 프리팹도 `_drops`를 유지한 채 `_rewardDrops`를 새로 얹어 직렬화 데이터 유실을 피했다 |
| 2026-08-14 | 기본 제작 규칙의 단일 관문 = `AlphabetWordRule.IsBasicCraftable(item)` | "E 포함 단어만 제작"을 각 호출부에 흩뿌리지 않고 술어 하나로 모았다(`DamageCalculator`/`CapabilityMatch`와 같은 스타일 — 규칙은 순수 함수, 호출부는 질의만). 적용 지점 3곳: `RecipeMatcher.FindExact`(발견형 매칭에서 제외), `CraftingSystem.CanCraft`/`Craft`(판정·실행 동일 기준 — QA-D1 원칙), `FloorValidator.Matches`(제작 불가 아이템이 던전 열쇠로 계획되면 재료를 배치해도 클리어 불가 층이 되므로 후보에서 제외). `Disassemble`은 의도적으로 미적용. UI(`CraftingUI`)도 목록에서 제외해 죽은 버튼 방지. 레시피=단어−E 규칙은 종전대로 에셋 검사 테스트가 담당(런타임 판정은 E 포함 여부만). EditMode 232/232, 실 DB 9종 전부 통과·차단 0 실측 |
| 2026-07-23 | (검증 방법) MCP execute_code는 codedom(C#5) | 문자열 보간·로컬 함수·roslyn 불가, 반환값 필수. 실제 입력 대신 `UsePerformed` 이벤트/핸들러를 리플렉션 발화해 입력 경로 검증. MCP HTTP 8080 curl JSON-RPC(initialize→session-id) |

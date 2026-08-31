# KEY 튜토리얼 — 첫 방 수직 슬라이스

> 2026-07-23 구현. "빈손 시작 → 글자 줍기 → 조합 → 장착 → 사용 → 진행"의 완결 루프.
> 크래프팅·인벤토리 UI 개편과 발견형(슬롯 배치) 크래프팅이 이 슬라이스와 함께 도입됨.

## 개요

플레이어(알파벳 **E**)가 첫 방에서 글자 **K**·**Y**를 주워 **KEY**(= K + E + Y)를 만들고,
그 열쇠로 **잠긴 문**을 열어 다음 방으로 넘어가는 튜토리얼. 게임의 핵심 정체성(E + 글자 → 영단어)과
핵심 루프(줍기 → 조합 → 장착 → 사용 → 진행)를 한 방에서 모두 보여준다.

전투는 없다 — 첫 방은 안전한 학습 공간(`RoomType.Tutorial`).

## 플레이어 흐름

1. **빈손으로 시작** — 인벤토리에 아무것도 없음(`GameManager._seedStarterMaterials = false`).
2. **글자 줍기** — 방 좌우에 떠 있는 노란 글자 `K`·`Y`에 닿으면 인벤토리에 들어온다.
3. **조합** — `C`로 크래프팅 창을 열고, 보유 글자를 **슬롯에 놓아** `→ KEY`가 뜨면 제작.
4. **장착** — `I`로 인벤토리(그리드)를 열고 KEY 셀을 선택 → **장착**(서브무기 슬롯).
5. **사용** — 오른쪽 **잠긴 문** 앞으로 걸어가면 문에 막혀 선다. **`F`(또는 우클릭)** 로 KEY 사용 → 문이 열린다.
6. **진행** — 문이 열리면 방 출구 잠금이 풀린다. 문(벽의 출입구)에서 **`E`** 로 다음 방으로 이동.

### 조작

| 키 | 동작 |
|----|------|
| A/D, ←/→ | 이동 |
| Space | 점프 |
| C | 크래프팅 창 |
| I | 인벤토리 창 |
| F / 우클릭 | 서브무기 사용(KEY로 문 열기) |
| E | 문에서 방 이동 |

## 관련 시스템 & 파일

### 안전한 시작 방
- `RoomType.Tutorial` (신규 enum 값, 끝에 추가) — 전투 콘텐츠 매핑이 없어 적이 스폰되지 않음.
- `DungeonGenerator.AssignRoomTypes` — 시작 방(0,0)을 `Tutorial`로 지정(기존 `Combat`에서 변경).

### 글자 줍기
- `Help.Item.MaterialPickup` — 트리거 진입 시 인벤토리에 재료 1개 추가 후 자기 제거.
- 콘텐츠 프리팹 `Room_Tutorial`에 K·Y 줍기가 배치됨(데이터 기반 스폰, `RoomContentLibrary`의 `Tutorial→Room_Tutorial`).

### 발견형(슬롯 배치) 크래프팅
- `Help.Crafting.RecipeMatcher` (순수) — 놓인 글자 묶음 → **정확히 일치**하는 레시피 아이템(초과 글자 있으면 불일치).
- `Help.Crafting.CraftingBench` (순수) — 슬롯 배치/제거/판정 상태 모델.
- `Help.Crafting.RecipeAvailability` (순수) — 재료별 필요/보유/부족 내역(표시용).
- `Help.UI.CraftingBenchUI` — 런타임에 슬롯·팔레트·결과·제작 버튼을 자체 구성.
  - **글자 선택 → 원하는 슬롯 클릭**으로 지정 배치(선택 글자는 노랑, 배치 가능한 빈 슬롯은 초록 힌트).
  - 레시피 목록을 보여주지 않는다(발견형).

### 그리드 인벤토리
- `Help.UI.InventoryGridUI` — `GridLayoutGroup` 셀 격자 + 셀 선택 시 하단 액션 행(장착/해제/분해).

### KEY 아이템 & 문 열기
- `Help.Item.Capability.Unlock` (신규 enum 값, 끝에 추가) — "잠긴 문 열기" 능력 단위.
- KEY = 서브무기, 레시피 K+Y, 능력 `Unlock` (에디터 셋업에서 생성, `RecipeDatabase` 등록).
- 잠긴 문 = `Help.Puzzle.CapabilityTarget`(요구 능력 `Unlock`) + **솔리드 콜라이더**.
- `Help.Puzzle.RoomPuzzle` — 잠긴 문을 목표로 등록해, 해결 전까지 방 출구를 잠근다(`RoomManager.SetExitLock`).
- `Help.Puzzle.SubWeaponUser` — `F`(사용) 시 플레이어 앞쪽을 훑어 `CapabilityTarget`에 장착 서브무기 능력을 적용.
- 흐름: KEY 장착 → `EquippedSubCapabilities=[Unlock]` → 문 앞에서 사용 → `CapabilityTarget` 해제 →
  `RoomPuzzle` 클리어 → 출구 잠금 해제.

### 에디터 셋업 메뉴 (`Help/Setup/...`)
- **Create All Game Assets** — 재료·무기·**KEY** 아이템 + `RecipeDatabase` 생성.
- **Setup Data-Driven Room Content** — 콘텐츠 프리팹(Combat/Puzzle/**Tutorial**) + `RoomContentLibrary` 구성,
  `RoomManager` 연결, **시작 인벤토리 시딩 끔**.
- **Switch To Slot Crafting UI** — Canvas의 옛 레시피목록 UI를 슬롯형 `CraftingBenchUI`로 교체.
- **Switch To Grid Inventory UI** — 옛 세로목록 UI를 `InventoryGridUI`로 교체(HUD 인벤토리 버튼도 재배선).

## 설계 노트

- **크래프팅 매칭 = 순서 무관 정확 일치.** 글자만 맞으면(K,Y 어느 슬롯이든) KEY 활성화. 남는 글자가 있으면 불활성.
  → 발견형의 관용성. 순서대로 스펠링하는 방식은 추후 조일 수 있음.
- **문은 솔리드 콜라이더.** 트리거로 두면 플레이어가 문을 뚫고 지나가 벽까지 걸어가서, 전방 Use 스캔이 문을 놓친다
  (실측 확인: 문 통과 후 x=5에서 F 미적용 / 문 앞 x=3에서 적용). 솔리드로 두면 플레이어가 문 앞에 막혀 서서 F가 확실히 맞는다.
- **문 열기는 기존 퍼즐 프레임워크 재사용.** 얼음벽(FLARE→Melt)·부서지는 벽(axe→BreakWall)과 동일 구조에
  능력 `Unlock`·요구자 "잠긴 문"만 추가. 프레임워크 자체는 불변.

## 알려진 한계 / 다음 작업

- **튜토리얼 외 방들엔 아직 재료 줍기가 없다.** 빈손 시작이므로, 던전 전체로 확장하려면 다른 방의 재료(방 `GuaranteedLoot`)도
  `MaterialPickup` 스폰으로 연결해야 클리어 가능하다(현재 미연결).
- ~~UI 디자인 폴리시 미적용~~ **✅ 적용(2026-07-26)** — 공용 `UITheme`(레트로 픽셀 아케이드 톤: 검은 패널+흰 테두리+하드 그림자+노랑/시안 액센트, 헤더 바+닫기 ✕)로 두 UI·HUD 통일. 한 화면 레이아웃 미세조정은 여전히 여지 있음.
- 크래프팅 슬롯 수 6 고정(더 긴 단어 대비 조정 가능).

## 확장 방법

- **새 줍기 글자**: `RoomContentSetup`에서 `CreatePickup(parent, AlphabetMaterial.X, pos)` 추가.
- **새 제작 아이템**: `SetupGameAssets`에 `MakeWeapon`/`MakeSubWeapon`로 정의(레시피 = 단어 − E). 슬롯에서 그 글자들을 놓으면 자동 매칭.
- **새 능력/장애물**: `Capability` enum 끝에 값 추가 → 제공 아이템 + 요구 `CapabilityTarget` 배치. 프레임워크 코드는 불변.

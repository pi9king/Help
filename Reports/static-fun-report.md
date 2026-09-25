# 정적 재미 리포트 (자동 측정)

> **이 문서는 측정값이다. 결정도, 판정도 아니다.**
> `Docs/FUN_VALIDATION_CHECKLIST.md` 중 코드·데이터만으로 셀 수 있는 항목을 자동으로 잰 것이다.
> 기준(몇이면 괜찮은가)은 여기서 정하지 않는다. 채택된 결정만 `DESIGN.md`로 옮긴다.
> 재생성: `Help ▸ Playtest ▸ Static Fun Report` — 손으로 고치지 말 것(덮어써진다).

- 생성: 2026-09-20 01:11
- 빌드/커밋: `038736e` + 미커밋 변경 17개
- 표본: 층마다 생성기 시드 0~199 (200장 × 3층)

## 1. 아이템 데이터가 게임 코드에 닿는가 — 체크리스트 P0-1

`ItemDefinition`의 각 필드를 **게임 코드(`Assets/Scripts`, 정의 파일 제외)가 읽는 곳**을 셌다.
값이 다른 **멤버**로 옮겨 담기면 그 멤버의 읽기까지 한 번 더 따라간다(1홉). 지역 변수는 따라가지 않는다.
주석·문자열은 제외. 정규식 근사라 리플렉션·간접 접근은 놓칠 수 있다.

| 필드 | 게임 코드 읽기 | 읽는 폴더 | 값이 옮겨간 곳 → 그 이름의 읽기 | 실제 아이템 값 종류 |
|---|---:|---|---|---:|
| `Id` | 21 | Crafting, Dungeon, Inventory, UI | — | 13 |
| `Word` | 8 | Item, UI | — | 13 |
| `Type` | 32 | Crafting, Dungeon, Inventory, Item, Player, UI | — | 4 |
| `Element` | 7 | Dungeon, Player, UI | `EquippedElement` → 1 | 6 |
| `WeaponCategory` | 5 | Dungeon, Player | `EquippedWeaponCategory` → 0 | 7 |
| `Capabilities` | 8 | Dungeon, Player | — | 5 |
| `Recipe` | 22 | Crafting, Dungeon, Inventory, Item, UI | — | 13 |
| `AttackBonus` | 4 | Player, UI | — | 10 |
| `DefenseBonus` | 4 | Player, UI | — | 2 |
| `AttackSpeedMult` | 0 | — | — | 4 |
| `HealAmount` | 2 | UI | — | 2 |
| `GoldCost` | 2 | UI | — | 4 |

**게임 코드가 한 번도 읽지 않는 필드**: `AttackSpeedMult`
— 아이템마다 값이 달라도 플레이에서는 차이가 나지 않는다.

### 1-1. 체크리스트 1차 후보 세 단어의 데이터

체크리스트 P0-1이 지목한 `AXE` / `ROPE` / `FLARE`. 위 표에서 읽기 0인 필드는 ⚪로 표시 — 값이 달라도 플레이에 안 닿는다.

| 단어 | `Type` | `WeaponCategory` | `Element` | `Capabilities` | `AttackBonus` | `DefenseBonus` | ⚪ `AttackSpeedMult` | `HealAmount` |
|---|---|---|---|---|---|---|---|---|
| AXE | Weapon | Axe | Stone | BreakWall | 10 | 0 | 1 | 0 |
| ROPE | SubWeapon | None | None | CrossGap Bind | 2 | 0 | 1 | 0 |
| FLARE | SubWeapon | None | None | Melt | 3 | 0 | 1 | 0 |

## 2. 방 유형마다 후보가 몇 개인가 — 체크리스트 P1-6

후보가 1개면 그 유형의 방은 **매 런 같은 모양·같은 콘텐츠**다. 층에 항목이 없으면 1층 풀로 폴백한다.

| 방 유형 | 1층 지형 | 2층 지형 | 3층 지형 | 1층 콘텐츠 | 2층 콘텐츠 | 3층 콘텐츠 | 콘텐츠 프리팹 |
|---|---:|---:|---:|---:|---:|---:|---|
| Combat | 3 | (1층) | (1층) | 1 | (1층) | (1층) | Room_Combat |
| EnvironmentPuzzle | 1 | (1층) | (1층) | 1 | (1층) | (1층) | Room_Puzzle |
| CombatPuzzle | 1 | (1층) | (1층) | 1 | (1층) | (1층) | Room_Puzzle |
| PurePuzzle | 1 | (1층) | (1층) | 1 | (1층) | (1층) | Room_Puzzle |
| Treasure | 1 | (1층) | (1층) | 1 | (1층) | (1층) | Room_Treasure |
| Shop | 0 | 0 | 0 | 1 | (1층) | (1층) | Room_Treasure |
| Boss | 1 | (1층) | (1층) | 1 | (1층) | (1층) | Room_Boss |
| Secret | 1 | (1층) | (1층) | 1 | (1층) | (1층) | Room_Secret |
| Tutorial | 1 | (1층) | (1층) | 1 | (1층) | (1층) | Room_Tutorial |

표기: `N` = 그 층 전용 후보 N개, `(1층)` = 전용이 없어 1층 풀을 씀, `0` = 1층에도 없음.

**여러 방 유형이 같은 콘텐츠 프리팹을 공유**:
- `Room_Puzzle` ← EnvironmentPuzzle, CombatPuzzle, PurePuzzle
- `Room_Treasure` ← Treasure, Shop

**코드 관찰**: 방마다 템플릿·콘텐츠를 고르는 시드가 **좌표만으로** 정해진다 (`RoomManager.RoomSeed` = `(X*73856093) ^ (Y*19349663)`, 런 시드·층 번호가 들어가지 않음). 후보가 2개 이상이어도 같은 좌표·같은 유형이면 매 런 같은 것이 뽑힌다. 시작 방(0,0)은 항상 시드 0.

## 3. 생성된 층 표본 — 체크리스트 P1-6·P1-7

실제 `RecipeDatabase`·`RoomContentLibrary`로 게임과 같은 경로(`DungeonGenerator.Generate`)를 돌렸다.
적 수는 방 유형별 콘텐츠 프리팹의 `EnemyBase` 개수(후보가 여럿이면 첫 번째 기준).

기본 제작 가능 아이템은 DB 전체에 **9개**, 층마다 그중 일부가 층 레시피로 뽑힌다.

| 층 | 평균 방 수 | 조건부 방/층 | 환경퍼즐 **없는** 층 | 비밀방 없는 층 | 빈손 적 | 평균 예산(글자) | 예산으로 다른 것을 못 만드는 층 |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1~3층 (전부 동일) | 10.0 | 1.65 | 0% (0/200) | 59% (117/200) | 55% (1018/1853) | 9.8 | 100% (200/200) |

**층 간 차이 없음**: 같은 시드로 돌리면 1·2·3층이 **완전히 같은 층**이 나온다. 생성기가 `DungeonConfig.FloorNumber`를 읽지 않고, 2·3층 콘텐츠·지형이 전부 1층 풀로 폴백하기 때문이다(위 2절). 실제 게임은 층마다 랜덤 시드라 배치는 달라지지만, **분포(방 수·유형·예산·적 구성)는 층과 무관하게 같다.**

- **빈손 적**: 잡아도 글자를 안 떨구는 적의 비율(`LootDistribution` 배분 기준).
- **예산으로 다른 것을 못 만드는 층**: 층 예산 글자만으로는 층 레시피 *외의* 기본 아이템을 하나도 만들 수 없는 층. 이런 층에서는 예산이 곧 정답 목록이라 "무엇을 만들까"의 선택이 없다. (두 레시피가 같은 글자를 쓰는 것은 기회비용이 아니다 — 예산에 그 글자가 두 장 들어 있다.)

### 3-1. 방 유형별 등장 수와 글자 없는 방 (전 층 합계)

| 방 유형 | 등장 | 글자 0개인 방 |
|---|---:|---:|
| Combat | 1653 | 13% (207/1653) |
| EnvironmentPuzzle | 600 | 68% (405/600) |
| CombatPuzzle | 588 | 53% (312/588) |
| PurePuzzle | 579 | 12% (69/579) |
| Treasure | 570 | 16% (93/570) |
| Shop | 570 | 12% (69/570) |
| Boss | 600 | 40% (240/600) |
| Secret | 249 | 14% (36/249) |
| Tutorial | 600 | 100% (600/600) |

## 4. 이 리포트가 재지 않는 것

정적 분석으로는 답할 수 없다. 자동 플레이(체크리스트 ③층) 또는 사람 플레이테스트가 필요하다.

- 제작→장착→사용 입력 수·시간 (P0-5) — 자동 플레이 필요
- 안전지대로 전투 무력화 (P0-3) — 적 도달성 시뮬 필요(D-16 미구현)
- 보스전 딜 불가 구간 (P0-4) — 자동 플레이 필요
- 세 단어를 **행동만으로** 구분하는가, 영리하다고 느낀 순간, 다시 하고 싶은가 — **사람만**


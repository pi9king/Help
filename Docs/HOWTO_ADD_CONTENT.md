# HOWTO: 콘텐츠 추가 가이드 (개발 시작점)

프레임워크가 준비된 상태에서 **콘텐츠를 하나씩 추가**하는 방법. 각 항목은 프레임워크 코드를 건드리지 않고 데이터·프리팹·enum 값만 더한다.

> 먼저 `Docs/DESIGN.md`(게임 규칙)·`Docs/ARCHITECTURE.md`(시스템 구조)를 읽으면 맥락이 잡힌다.

## 0. 핵심 원리 (한 줄)
게임의 핵심 판정 = **"이 방/퍼즐이 요구하는 아이템을, 지금 가진 재료로 크래프팅할 수 있는가?"**
아이템은 **능력(Capability)** 을 제공하고, 퍼즐/문/장애물은 능력을 요구한다. 속성(ElementType)·무기종류는 능력의 한 종류일 뿐.

---

## 1. 능력(Capability) 추가
`Assets/Scripts/Item/Capability.cs`의 enum에 값 추가. 끝.
```csharp
public enum Capability { None, BreakWall, CrossGap, Melt, Conduct, /* ← 여기에 추가 */ Dig }
```
이제 아이템이 `Dig`를 제공하고, 퍼즐이 `Dig`를 요구할 수 있다.

## 2. 무기 / 서브무기 / 아이템 추가
`Assets/Editor/SetupGameAssets.cs` → `CreateWeapons()`에 한 줄 추가 후 메뉴 **Help/Setup/Create All Game Assets** 실행.
- 무기: `MakeWeapon(id, WORD, category, element, Req(...), attackBonus, capabilities: new[]{Capability.X})`
- 서브무기(use 액션): `MakeSubWeapon(id, WORD, element, Req(...), new[]{Capability.X})`
- **규칙**: 단어에 E가 정확히 하나 이상 포함, 레시피 = 단어 − E 하나(플레이어가 E 제공). 예 `AXE`→A,X / `FLARE`→F,L,A,R. (`AlphabetWordRuleTests`가 검증)
- 능력은 아이템 타입과 직교 — 무기든 서브무기든 아무 능력이나 얹을 수 있다.

## 3. 퍼즐 장애물 추가
`CapabilityTarget` 컴포넌트가 붙은 오브젝트(프리팹)를 만들고 `RequiredCapability`를 설정.
- 빌딩블록 프리팹 참고: `Assets/Prefabs/BreakableWall.prefab`(BreakWall), `IceWall.prefab`(Melt).
- 해제되면 콜라이더/오브젝트가 제거되어 지형이 열린다(물리 게이팅).
- 능력 적용은 **출처 무관**: 주무기 공격(Hitbox) 또는 서브무기 사용(SubWeaponUser) 어느 쪽이든 요구 능력이 닿으면 해제.
- **클리어 플래그 게이팅(출구 잠금)이 필요하면**: 방 콘텐츠에 `RoomPuzzle`을 넣고 `_targets`에 CapabilityTarget들을 연결. 미해결이면 출구가 잠기고, 전부 해제 시 열린다. (`Assets/Prefabs/RoomPuzzle.prefab` 시작점)

## 4. 적 추가
- 새 행동: `Assets/Scripts/Enemy/EnemyArchetype.cs`에 값 추가 + `EnemyAI.cs`의 `AttackDecision`/`Tick`에 분기(순수 로직 → EditMode 테스트 가능).
- 새 적 오브젝트: `Assets/Prefabs/Enemy.prefab`를 복제해 `EnemyBase` 필드(HP/공격/속도/aggro/**_archetype**/_standoffRange/**_lockedElement**) 조정. 속성 잠금(_lockedElement)을 주면 그 속성 무기로만 효율적으로 잡힌다(열쇠-자물쇠).

## 5. ★ 방 콘텐츠 저작 (데이터 스폰 — 씬 손배치 대신)
적/퍼즐/루팅은 **방 유형별 콘텐츠 프리팹**으로 데이터 저작한다. 씬에 직접 놓지 않는다.
1. **콘텐츠 프리팹** 만들기: 빈 GameObject 아래에 적/장애물/RoomPuzzle을 **방 중앙 원점 기준 XY 평면**에 배치. `Assets/Prefabs/RoomContent/Room_Combat.prefab`·`Room_Puzzle.prefab` 참고.

   > 쿼터뷰에서는 `y`도 이동 평면의 축이다. 오브젝트의 발 위치는 Transform이 아니라
   > SpriteRenderer의 pivot/sort point로 표현하고, 이동을 막는 콜라이더는 발밑에 작게 둔다.
2. **라이브러리 등록**: `Assets/ScriptableObjects/RoomContentLibrary.asset`의 Entries에 `RoomType → 콘텐츠 프리팹(들)` 추가. 여러 개 넣으면 방마다 결정적으로 하나 선택된다.
3. 끝. `RoomManager`가 방 로드 시 유형에 맞는 콘텐츠를 스폰하고, 방 이동/사망 리셋 시 자동 교체·재스폰한다.
- 셋업 재생성 메뉴: **Help/Setup/Setup Data-Driven Room Content** (빌딩블록 프리팹 → 콘텐츠 프리팹 + 라이브러리 + RoomManager 연결).
- **스폰 포인트**: 방 템플릿의 마커(`e E x l c b`) 위치에 개별 프리팹을 스폰하는 경로가 있다.
  `RoomContentLibrary`의 방 유형 항목에서 `useTemplateMarkers`를 켜고 마커별 프리팹을 등록하면 된다.
  **기본은 꺼짐** — 기존에 검증된 손배치 경로를 그대로 두고 방 유형별로 하나씩 옮기기 위해서다.

## 5.5. ★ 방 지형 저작 (ASCII 템플릿)
방의 **평면 생김새**는 `Assets/Rooms/{RoomType}_{SizeClass}_{NN}.txt`에 아스키로 그린다.
1. 텍스트로 그린다 — `#` 이동 불가 벽, `.` 이동 가능한 바닥, `D` 문 / 마커 `p e E x l c b` / 확률 `? %`. 레거시 `-`는 바닥으로 읽는다.
   (손 타이핑은 줄 길이가 어긋나기 쉽다 — `Tools/gen_rooms.py`처럼 스탬프로 찍는 편이 안전)
2. 눈으로 본다 — **Help/Level/Room Template Viewer** (도달성 오버레이: 초록=갈 수 있음, 빨강=못 감)
3. 검증한다 — EditMode 테스트가 `Assets/Rooms/*.txt` **전량**을 확률 양극단으로 자동 검사
4. 연결한다 — **Help/Setup/Setup Room Templates** (파일명에서 방 유형을 읽어 라이브러리 등록 + 씬 배선)
- **검증 요약**: 문 이외 외곽은 닫혀 있어야 하고, 모든 문·마커는 하나의 이동 가능 영역으로 연결되어야 한다. 대각선 모서리 관통은 허용하지 않는다.
- 템플릿이 없는 (유형, 크기) 조합은 예전 절차적 셸로 폴백된다 — 게임이 멈추지 않는다.

## 6. 스프라이트 추가/교체
`Assets/Editor/SpriteGenerator.cs`가 프로시저럴 PNG를 만든다(플레이어=E 글리프 등). 실제 아트는 `Assets/Sprites/*.png`를 같은 이름으로 덮어쓰면 교체됨(임포트 설정: Sprite, PPU 32, Point). 메뉴 **Help/Setup/Generate Placeholder Sprites**.

## 7. 빌딩블록 프리팹 (드래그로 배치 가능)
`Assets/Prefabs/` : `Enemy`, `BreakableWall`, `IceWall`, `RoomPuzzle` + UI용 `ItemSlot`/`RecipeSlot`.
`Assets/Prefabs/RoomContent/` : `Room_Combat`, `Room_Puzzle`(콘텐츠 프리팹 예시).

## 8. 테스트 / 검증 워크플로
- **순수 로직은 EditMode 테스트로**(TDD): `Assets/Tests/EditMode/`. 로직을 MonoBehaviour에서 분리하면 테스트된다(EnemyAI·CapabilityMatch·SolveTracker·EntryRequirementChecker 참고).
- 실행: Unity Test Runner(Window▸General▸Test Runner) 또는 배치:
  ```bash
  "$UNITY" -batchmode -nographics -runTests -testPlatform EditMode -projectPath . -testResults r.xml
  ```
- 현재 테스트 145개. 씬 배선 회귀는 `TestSceneWiringTests`가 자동 검증.
- **런타임(Play) 검증**: 에디터가 비포커스면 `Application.runInBackground=true`를 세팅해야 물리/AI가 틱한다. 시각 검증은 직접 Play 권장(비포커스 스크린샷은 stale).

## 9. 알려진 캐비앗
- **입력 실발화 미확정**: 좌클릭(공격)·우클릭/F(서브무기 사용)가 실제 발화하는지는 포커스된 Play에서 육안 확인 필요(헤드리스 입력 시뮬 불안정). 배선·바인딩은 정상.
- MCP 리프레시 후 활성 씬이 빈 씬으로 drift할 수 있음 → TestScene 다시 열기.
- `.meta` 파일은 Unity가 자동 생성 — 수동 생성/삭제 금지.

## 10. 에디터 메뉴 요약 (Help/Setup/)
- **Create All Game Assets** — 재료/무기/서브무기 SO + RecipeDatabase
- **Generate Placeholder Sprites** — PNG 스프라이트
- **Create Building Block Prefabs** — Enemy/BreakableWall/IceWall/RoomPuzzle
- **Setup Data-Driven Room Content** — 콘텐츠 프리팹 + 라이브러리 + RoomManager 연결
- **Setup Room Templates** — `Assets/Rooms/*.txt` → RoomTemplateLibrary + 씬 배선
- **Help/Level/Room Template Viewer** — 방 템플릿 뷰어 + 도달성 검증
- **Help/Art/Process Art Source** — AI 아트 후처리(`Docs/ART_PIPELINE.md`)
- **Assign RecipeDatabase to Scene GameManager**

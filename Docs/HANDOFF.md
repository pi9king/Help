# 인수인계 문서 (HANDOFF) — 2026-07-13

> **여기서 시작.** 프로젝트의 현재 상태·구조·실행법·확장법·남은 일을 한 곳에 정리한다.
> 상세는 각 문서 참조: `DESIGN.md`(게임 규칙)·`ARCHITECTURE.md`(시스템 구조)·`HOWTO_ADD_CONTENT.md`(콘텐츠 추가법)·`SPEC_PUZZLE_FRAMEWORK.md`(퍼즐 명세+테스트).

---

# ★ 2026-09-09 현재 상태 — 새 세션은 여기부터

## 지금 어디까지 와 있나 (사실)

- **커밋 11개로 정리 완료.** HEAD = `6587228`, 워킹트리 clean.
  그전까지 8일치(수정 35 + 신규 95 = 130개 파일)가 미커밋 상태였다.
- **EditMode 테스트 409개 — 아직 한 번도 돌려보지 못했다.** Unity 에디터가 열려 있어
  batchmode가 잠겼고, Test Runner 실행 결과도 확인되지 않았다. **컴파일도 미검증이다.**
- 지형 템플릿이 이번에 처음으로 씬에 연결됐다(`RoomTemplateLibrary.asset`이 그전엔 아예 없었다).

## 새 세션 시작 순서 (이 순서대로)

1. Unity 열고 **컴파일 에러 0** 확인
2. `Help ▸ Setup ▸ Generate Placeholder Sprites`
3. `Help ▸ Setup ▸ Setup Room Templates`
4. `Help ▸ Setup ▸ Setup Data-Driven Room Content`
   → **3·4는 건너뛰면 안 된다.** 새로 생긴 직렬화 필드
   (`RoomPuzzle._selfContained`, `GameManager._contentLibrary`, `RoomManager._hazardTile`)가
   여기서 채워진다. 안 돌리면 조용히 null이고 증상이 예전으로 되돌아간다.
5. Test Runner ▸ EditMode 409개
6. Play — **1층 시작부터 보스까지 완주 시도**

## 검증되지 않은 것 (전부 사람이 Play로 봐야 한다)

- [ ] 컴파일 / EditMode 409개
- [ ] **🐛 아무 행동 없이 방이 클리어되는 버그** (아래 전용 항목 참조 — 최우선)
- [ ] **한 런 완주** — 이 프로젝트는 아직 한 번도 처음부터 끝까지 통과된 적이 없다
- [ ] 점프 손맛 (인터뷰 D-4 미결) — 이게 정해져야 방 템플릿을 그릴 수 있다.
      바뀌면 단차·갭 문법과 템플릿을 다시 그려야 하므로 **저작보다 먼저**다
- [ ] 새 몬스터 아트(Enemy_Ant)가 전투 방에 실제로 보이는지, 스케일·애니메이션이 맞는지
- [ ] KEY 튜토리얼 문 게이팅이 복구됐는지 (한 번 깨졌다가 고쳤다)
- [ ] 퍼즐 방 게이팅 — 독립 퍼즐은 자유 통행, 환경/전투 퍼즐은 도구 없이 진입 차단

## ⚠ 왜 버그가 많이 났나 (모델 관찰 — 결정이 아니라 분석이다)

2026-09-08~09 세션에서 버그가 16건쯤 나왔다. 성격이 두 갈래다.

**(1) 잠복해 있던 것 — 13건.** 시스템은 다 있었는데 **함께 돌아간 적이 없어서**
연결부가 전부 어긋나 있었다. 이번에 지형 템플릿을 처음 씬에 물리면서 한꺼번에 드러났다.

- 콘텐츠 좌표가 방 크기 변경을 안 따라감(K·Y가 공중에 뜸)
- 루팅이 바닥 셀 중심에 스폰돼 지면에 묻힘 → 주울 수 없음
- 루팅 가로 배치가 나머지 연산이라 6번째 글자가 1번째와 겹침
- `RoomTemplateLibrary.asset`이 없어 ASCII 템플릿이 전혀 로드되지 않음
- 생성기가 굴린 크기와 템플릿 크기가 안 맞으면 절차적 빈 상자로 폴백
- 천장 북문에 아무도 못 올라가는데 검증기가 통과시킴
- 진입 조건 어휘(원소/무기)와 장애물 어휘(Capability)가 서로 다름
- `FloorValidator`가 `RequiredCapability`를 통째로 무시
- 목표 0개인 퍼즐이 방을 영구 잠금 (`ApplyExitLockFromContent`에 가드 없음)
- 등

**(2) 이번 세션에 모델이 새로 만든 것 — 3건.** 공통 원인이 하나다:
**한쪽만 고치고 반대편을 안 맞췄다.**

| 사고 | 무엇을 놓쳤나 |
|---|---|
| 위험 지형(D-12) | 도달성 시뮬만 "밟고 설 수 있다"로 바꾸고 **타일 콜라이더는 안 줬다** → 적이 빠져 맵 밖으로 사라져 방 클리어 불가 |
| 게이팅(RoomGating) | 조건 없는 방에서 장애물을 목표에서 뺐는데, **출구 잠금 재평가 쪽 가드는 안 맞췄다** → 튜토리얼이 목표 0개로 영구 잠김 |
| `RoomTemplateNaming` | 파일로 쓰는 과정에 역슬래시가 깨져 컴파일 에러 |

**다음 세션이 지킬 것**: 판정 규칙을 바꿀 때 **그 규칙을 읽는 곳을 전부 찾아** 같이 고친다.
특히 이 프로젝트는 같은 규칙이 **순수 로직(시뮬/검증)과 런타임(콜라이더/컴포넌트)** 양쪽에
따로 구현돼 있어, 한쪽만 고치면 시뮬과 게임이 다른 것을 검증하게 된다.

## 🐛 미해결 버그 — 아무 행동 없이 방이 클리어됨 (2026-09-09 보고)

**증상**: 방에 들어가 아무것도 하지 않았는데 방이 클리어 상태가 된다.
콘솔 `[Room x,y] Type=... Cleared=True` (로그 출처는 `RoomManager.RefreshDoors`).

**정적으로 확인한 것 (코드를 읽어 좁힌 범위)**

- `Room.IsCleared`를 켜는 곳은 `RoomManager.TryClearCurrentRoom` **하나뿐**이다.
- 그걸 호출하는 곳도 `RoomPuzzle.HandleSolved` **하나뿐**이고, 이건 `SolveTracker.OnSolved`로만 불린다.
- `SolveTracker`는 **목표가 0개면 OnSolved를 쏘지 않는다**(빈 방 오발화 방지).
- 따라서 증상은 "**등록된 목표가 있는데 그게 즉시 충족됐다**"는 뜻이다.

즉시 충족될 수 있는 경로는 두 개뿐이다:

| # | 경로 | 조건 |
|---|---|---|
| A | `EnemyClearObjective.Start()` | `GetComponentsInChildren<EnemyBase>()`가 **0마리**면 즉시 `SetMet()` |
| B | `RoomPuzzle.Awake()` | `if (t.IsResolved) _tracker.SetMet(t, true)` — 이미 해제된 `CapabilityTarget` |

**정적으로는 원인이 안 잡혔다.** 콘텐츠 프리팹 6종을 전수 확인한 결과
`EnemyClearObjective`를 가진 `Room_Combat`(적 3종)·`Room_Boss`(보스 1종) 모두
적 프리팹을 정상 참조하고 있다. 즉 **런타임에 적이 사라지거나 스폰되지 않는** 조건이다.

**다음 세션이 확인할 것 (Play 한 번이면 갈린다)**

임시 로그를 두 곳에 넣고 어느 경로인지 먼저 가른다:
1. `EnemyClearObjective.Start()` — 수집된 `enemies.Length`를 찍는다. 0이면 경로 A 확정
2. `RoomPuzzle.HandleSolved()` — 어떤 목표가 충족돼 solved가 됐는지 찍는다

경로 A라면 **적이 스폰 직후 사라지는지**를 본다. 유력한 후보는
바로 아래 "적이 방 밖으로 떨어지면 클리어가 막힌다" 항목과 **같은 뿌리**다 —
적이 `Start()` 전에 사라지면 0마리로 집계돼 **자동 클리어**,
`Start()` 후에 사라지면 영원히 안 죽은 것으로 남아 **클리어 불가**가 된다.
증상이 정반대로 보이지만 원인은 하나일 수 있다.

**주의**: 이 버그는 2026-09-08~09 변경(콘텐츠 좌표 바닥 기준 이동, 층 축 도입,
게이팅 규칙) 이후에 보고됐다. 그 전에도 있었는지는 확인되지 않았다.

---

## ⚠ 구조적으로 남아 있는 문제

- **PlayMode 테스트가 0개다.** `Assets/Tests/PlayMode/`에 asmdef만 있다.
  EditMode 409개는 시스템 *안*만 보고, 연결부는 전부 사람이 Play로 찾고 있다.
  방을 50~100장 그리기 시작하면 이 비용이 급증한다.
- **적이 방 밖으로 떨어지면 클리어가 막힌다.** 전투 방 클리어 조건이 적 전멸인데
  적이 바닥 구멍으로 사라지면 그 방은 영영 안 열린다. 지금은 템플릿에 바닥 구멍이
  없어 안 터지지만, 갭을 그리는 순간 재현된다. 안전망이 없다.
- **위험 지형 보류 중.** 코드는 잠들어 있고 템플릿에서 못 쓰게 테스트로 막아뒀다.
  되살리려면 타일 콜라이더부터 주고 시뮬과 런타임을 맞춰야 한다.
- **여러 주제에 걸친 파일**(`RoomManager.cs`가 5개 주제) 때문에 커밋이 파일 단위로만
  갈렸다. 이력은 읽을 수 있지만 bisect는 안 된다. 앞으로는 작업 단위로 바로 커밋할 것.

## 문서 지도 (2026-09-09 갱신)

- `LEVEL_DESIGN_INTERVIEW.md` — **지형 설계 인터뷰 결정 로그 D-1~D-17 + 실행 계획.**
  `LEVEL_DESIGN.md`의 규칙 중 무엇이 승인/폐기/보류인지가 여기 있다
- `LEVEL_DESIGN.md` — ⚠ 대부분 모델이 쓴 것이고 승인된 적 없다(머리말 참조)
- `CLAUDE.md` — **문서 작성 규율** 추가: 사용자가 정하지 않은 것을 규칙으로 쓰지 않는다

---

## 1. 프로젝트 개요
- **장르**: 2D 사이드뷰 플랫포머 + 로그라이크 + 퍼즐. **플레이어 = 알파벳 "E"**.
- **엔진**: Unity 6 (6000.3.10f1), URP 2D, Input System(new), Linear, 1920×1080.
- **핵심 루프**: 던전 진입 → 방(전투/퍼즐) → 알파벳 재료 루팅 → **E + 글자 조합으로 영단어 아이템 제작** → 장착 → 다음 방 → 사망 시 리셋.
- **핵심 판정(정체성)**: *"이 방/퍼즐이 요구하는 아이템을, 지금 가진 재료로 크래프팅할 수 있는가?"* 속성은 핵심이 아니라 크래프팅 다양성의 한 축.

## 2. 현재 상태 — 구현/검증 완료
**EditMode 테스트 145/145 통과. 퍼즐 명세 15개 중 14 PASS(1 미구현). MCP Play 런타임 실측 완료.**

| 시스템 | 상태 | 비고 |
|---|---|---|
| 플레이어 (이동/점프/대시/공격) | ✅ | 물리·접지·UI 입력 게이트 |
| 전투 (속성 열쇠-자물쇠) | ✅ | DamageCalculator, Hitbox/Hurtbox. 일치=정타, 불일치=대폭감소(최소1) |
| 적 AI | ✅ | Patrol/Chase/Attack + 이력현상. 아키타입 **Melee/Ranged(카이팅)**. 수평 추격만(수직/LOS 없음) |
| 적 드롭 | ✅ | 잠금 속성 단어 글자(−E)→재료 (전투→크래프팅 루프) |
| 던전 생성 | ✅ | 랜덤워크 + 재료 보장 불변식 + 도달성 |
| 방 렌더/콜라이더 | ✅ | 사이드뷰 셸(바닥/벽/빈 내부) + 런타임 콜라이더 |
| 문/방 이동 | ✅ | 문 상태(None/Open/Locked) + E키 이동 |
| 방 진입 판정 (레이어1) | ✅ | 크래프팅 가능성 기반. 속성/무기/**능력** 요구 지원 |
| 크래프팅/인벤/장착/분해 | ✅ | RecipeDatabase, 재료 A~Z(E 제외 25종) |
| **퍼즐 프레임워크 (능력 기반)** | ✅ | 아래 3절 |
| **데이터 기반 방 콘텐츠 스폰** | ✅ | RoomContentLibrary → 방 유형별 콘텐츠 프리팹 스폰/교체/재스폰 |
| 사망 & 런 리셋 (로그라이크 루프) | ✅ | 인벤 초기화+새 던전+부활. 콘텐츠 재스폰 |
| 스프라이트 | ✅ | PNG 에셋(player_E/적/바닥/벽/문) — placeholder |
| UI (HUD/인벤/크래프팅) | ✅ | uGUI. 입력 게이트 |

## 3. 퍼즐/능력 프레임워크 (이번에 새로 구축 — 확장 우선 설계)
- **능력(Capability) 태그**(enum): 아이템이 *제공*, 퍼즐/문/장애물이 *요구*. 타입과 직교(무기·서브무기 아무거나 능력 보유).
- **2 레이어**: 레이어1=입장 조건(추상 "만들 수 있나"). 레이어2=클리어 조건(실제 크래프팅→물리적 사용으로 장애물 해제).
- **출처 무관 적용(C 모델)**: 장애물은 "요구 능력이 적용됐는가"만 봄. **주무기 공격(Hitbox)** 또는 **서브무기 사용(SubWeaponUser)** 어느 쪽이든 동작.
- **서브무기 슬롯 + 사용(use) 액션**: 주무기(공격)와 별개 슬롯. Use 입력 = 우클릭/F.
- **진행 관문 둘 다**: 물리 장애물(해제 시 콜라이더 제거) + 클리어 플래그(RoomPuzzle 미해결 시 출구 잠금).
- **데모(씬)**: 데이터 스폰으로 방 유형별 콘텐츠 — Combat 방=적, Puzzle 방=부서지는 벽(BreakWall, 도끼)+얼음벽(Melt, 플레어).
- 파일: `Assets/Scripts/Puzzle/` (CapabilityMatch·SolveTracker·CapabilityTarget·RoomPuzzle·SubWeaponUser), `Item/Capability.cs`.

## 4. 조작
- 이동 **A/D·←→** / 점프 **Space** / 대시 **LeftShift** / 공격 **좌클릭(또는 Enter)** / 서브무기 사용 **우클릭(또는 F)** / 인벤 **I** / 크래프팅 **C** / 방 이동 **E**

## 5. 실행 / 테스트 / 빌드
```bash
UNITY="C:/Program Files/Unity/Hub/Editor/6000.3.10f1/Editor/Unity.exe"
# EditMode 테스트
"$UNITY" -batchmode -nographics -runTests -testPlatform EditMode -projectPath . -testResults r.xml
# Windows 빌드
"$UNITY" -batchmode -nographics -buildWindows64Player ./Build/game.exe -projectPath . -quit
```
- 씬: `Assets/Scenes/TestScene/TestScene.unity` (Play 진입점).
- 에디터에서: Window ▸ General ▸ Test Runner.
- **MCP for Unity**(HTTP `http://127.0.0.1:8080/mcp`)로 원격 제어 가능(에디터 열려있을 때).

## 6. 콘텐츠 추가 / 확장 (→ `HOWTO_ADD_CONTENT.md` 상세)
- **능력 추가**: `Capability.cs` enum 값.
- **무기/서브무기/아이템**: `Editor/SetupGameAssets.cs` + 메뉴 Help/Setup/Create All Game Assets. (단어에 E 포함, 레시피=단어−E)
- **퍼즐 장애물**: `CapabilityTarget` 컴포넌트 + RequiredCapability. 빌딩블록 `Assets/Prefabs/{BreakableWall,IceWall}.prefab`.
- **적**: `EnemyArchetype` + `EnemyAI` 분기, `Assets/Prefabs/Enemy.prefab` 복제·조정.
- **★ 방 콘텐츠 저작**: 콘텐츠 프리팹(원점 기준 자식 배치) 만들어 `RoomContentLibrary.asset`에 `RoomType→프리팹` 등록. RoomManager가 자동 스폰. 메뉴 Help/Setup/Setup Data-Driven Room Content.
- **에디터 메뉴**(Help/Setup/): Create All Game Assets · Generate Placeholder Sprites · Create Building Block Prefabs · Setup Data-Driven Room Content.

## 7. 검증 상태 (신뢰도)
- **자동 회귀**: EditMode 145개(순수 로직·씬 배선). 매 변경 후 실행 권장.
- **퍼즐 명세 테스트**: `SPEC_PUZZLE_FRAMEWORK.md` — 15개 중 **14 PASS**.
- **런타임 실측**: 전투·콜라이더·크래프팅·드롭·사망리셋·퍼즐(부수기/녹이기/게이팅)·데이터 스폰 전부 MCP Play로 확인.

## 8. ⚠ 알려진 한계 / 캐비앗 (인수 시 반드시 인지)
1. ~~입력 실발화 미확정~~ **✅ 해소 (2026-07-20)**: 포커스된 Play에서 임시 진단 로그로 실측 확인 — 좌클릭 → `OnAttack`(`pressed=True state=Idle uiBlocking=False`) 4회, 우클릭/F → `OnUse` → `SubWeaponUser` 3회 발화. 런타임에서 `Attack`/`Use` 액션이 `enabled=True`, 각각 `/Mouse/leftButton`·`/Mouse/rightButton`+`/Keyboard/f`로 해석됨도 확인. 진단 로그는 확인 후 제거(미커밋).
2. ~~FS-12 미구현~~ **✅ 해소 (2026-07-20)**: 서브무기 "사용"이 적에게 **상태이상**으로 작용한다(데미지 아님). ROPE(Bind)로 속박+끌어당김 구현·실측 완료. 남은 것: **투사체 발사 미구현** — 현재는 `SubWeaponUser`의 근접 OverlapBox 범위에서만 적용된다(원거리 발사는 `ProjectileState`/`Projectile2D` 추가 필요).
3. **적 AI 수평 추격만**: 점프/시야(LOS)/수직 대응 없음.
4. **콘텐츠 배치는 프리팹 내 손배치**: 스폰 포인트/가중치 랜덤 없음(필요 시 확장).
5. **아트 = placeholder** 픽셀아트.
6. **개발 환경 팁**: MCP `refresh_unity` 후 활성 씬이 빈 씬으로 drift할 수 있음 → TestScene 다시 열기. 비포커스 에디터는 스크린샷 stale + `Application.runInBackground=true` 세팅해야 Play 틱.
7. **git 미커밋**: 이번 작업분 전부 워킹트리에만 있음(커밋은 지시 시).

## 9. 남은 설계 미결 항목 (개발 시작 시 결정 필요)
- **속성 15개 최종 확정 + 퍼즐 상호작용 콘텐츠**: `ElementType` 15종 하드코딩됨. 각 속성↔능력 매핑·퍼즐 기믹은 미결(프레임워크는 준비됨).
- **무기 유형 카테고리 모션/사거리/속도**: `WeaponCategory` 6종 존재하나 차이 미구현.
- **아이템(영단어) 전체 목록**, 층 수/층당 방 수, 아이템 등급/희귀도.
- **보스 설계 · 메타 진행(영구 해금)**.
- **미니맵/방 유형 아이콘**. (~~E포함 규칙 런타임 강제~~ **✅ 해소 2026-08-14** — `AlphabetWordRule.IsBasicCraftable`이 매칭·제작·던전 열쇠 계획에서 강제)
- **Phase 3(퍼즐 물리 상호작용)**: 로프 매달림 등.

## 10. 권장 다음 스텝
1. 포커스된 Play로 **입력 실발화 육안 확인**(캐비앗 1) — 5분, 최대 리스크 해소.
2. **FS-12 결정**: 문서 정정 vs 전투 사용 구현.
3. 콘텐츠 저작 시작(방/퍼즐/적을 데이터로) 또는 설계 미결(속성↔능력 매핑) 확정.

---

## 11. 2026-09-02 추가 — 레벨 디자인 기반 + AI 아트 파이프라인

**EditMode 353개 통과, 컴파일 경고 0. 런타임(Play) 실측은 아직 안 함 — 아래 ⚠ 참고.**

| 추가된 것 | 요약 |
|---|---|
| 점프 물리 재조정 | gravityScale 1→2.85. 점프 높이 9.9→**3.51타일**(정점 0.5초). 가변점프·코요테 0.1초·점프버퍼 0.12초. 이전엔 방(9타일)을 통째로 뛰어넘어 지형 설계가 불가능했다 |
| 방 크기 등급 | Small 25×15 / Wide 51×15 / Tall 25×31. 보스=Wide 고정, 시작=Small 고정, 나머지 24% 확률 |
| 카메라 | `CameraFollow`가 방 경계 안에서만 추적(Small은 중앙 고정). ortho 7.5. `CameraShake`는 위치 대신 오프셋만 발행하도록 분리 |
| **ASCII 방 템플릿** | `Assets/Rooms/*.txt`. 지형 7문자 + 마커 7종 + 확률 문자 2종. 파서·해석기·라이브러리 |
| 지형 어휘 확장 | 일방통행 발판(별도 Tilemap + PlatformEffector2D), 가시·구덩이(`RoomHazard`가 실제 판정) |
| **도달성 자동 검증** | `ReachabilityAnalyzer` — 근사식이 아니라 실제 물리 시뮬레이션. `RoomTemplateReachabilityTests`가 전 템플릿을 확률 양극단으로 검사 |
| 에디터 뷰어 | Help ▸ Level ▸ Room Template Viewer — 지형 그림 + 도달성 오버레이 + 칸 클릭 탐침 |
| 템플릿 10장 | Tutorial/Combat×3/EnvPuzzle/PurePuzzle/CombatPuzzle/Treasure/Secret/Boss |
| **AI 아트 파이프라인** | `ArtSource/raw/`에 넣고 Help ▸ Art ▸ Process Art Source. 배경제거→트림→최빈색 축소→팔레트 양자화(OkLab)→아웃라인→임포트 |
| 마커 기반 콘텐츠 스폰 | 구현됐으나 **기본 꺼짐**(`RoomContentLibrary.useTemplateMarkers`). 방 유형별로 하나씩 켜서 옮긴다 |

**⚠ 이번 작업분의 미검증 항목** — 전부 Play로 눈으로 봐야 한다:
1. 점프 손맛(3.5타일 도달, 짧게 누르면 낮게, 난간 끝 코요테 점프)
2. Small 방이 화면에 정확히 맞는지 / Wide·Tall에서 카메라가 방 밖을 안 비추는지
3. 일방통행 발판을 아래에서 통과하는지
4. 가시·구덩이 판정(피해량·입구 복귀)
5. 뷰어의 도달성 표시가 실제 Play와 일치하는지

**시작 순서**: Help ▸ Setup ▸ Generate Placeholder Sprites → Help ▸ Setup ▸ Setup Room Templates → Play.

---
### 문서 지도
- `LEVEL_DESIGN.md` — **방 지형 문법·아스키 템플릿·도달성 검증** (레벨 저작 시 여기부터)
- `ART_PIPELINE.md` — **AI 아트 생성 → 픽셀 스프라이트 후처리·프롬프트북**
- `DESIGN.md` — 게임 규칙·결정 로그 (*결정된 것*)
- `OPEN_QUESTIONS.md` — 미결 고민 (*아직 정하지 않은 것*: 분해 희소화, 특수방 상점, 층 테마, 보스 보상 등)
- `ARCHITECTURE.md` — 시스템 구조·파일 구조·결정 로그
- `HOWTO_ADD_CONTENT.md` — 콘텐츠 추가 실무 가이드
- `SPEC_PUZZLE_FRAMEWORK.md` — 퍼즐 기능 명세 + 명세별 테스트 결과
- `HANDOFF_UI_ISSUE_2026-07-09.md` — (과거) I/C UI 버그 조사 기록
- `CLAUDE.md` (루트) — 개발 방법론(TDD/Tidy First)·컨벤션

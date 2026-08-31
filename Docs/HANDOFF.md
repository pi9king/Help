# 인수인계 문서 (HANDOFF) — 2026-07-13

> **여기서 시작.** 프로젝트의 현재 상태·구조·실행법·확장법·남은 일을 한 곳에 정리한다.
> 상세는 각 문서 참조: `DESIGN.md`(게임 규칙)·`ARCHITECTURE.md`(시스템 구조)·`HOWTO_ADD_CONTENT.md`(콘텐츠 추가법)·`SPEC_PUZZLE_FRAMEWORK.md`(퍼즐 명세+테스트).

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
### 문서 지도
- `DESIGN.md` — 게임 규칙·결정 로그 (*결정된 것*)
- `OPEN_QUESTIONS.md` — 미결 고민 (*아직 정하지 않은 것*: 분해 희소화, 특수방 상점, 층 테마, 보스 보상 등)
- `ARCHITECTURE.md` — 시스템 구조·파일 구조·결정 로그
- `HOWTO_ADD_CONTENT.md` — 콘텐츠 추가 실무 가이드
- `SPEC_PUZZLE_FRAMEWORK.md` — 퍼즐 기능 명세 + 명세별 테스트 결과
- `HANDOFF_UI_ISSUE_2026-07-09.md` — (과거) I/C UI 버그 조사 기록
- `CLAUDE.md` (루트) — 개발 방법론(TDD/Tidy First)·컨벤션

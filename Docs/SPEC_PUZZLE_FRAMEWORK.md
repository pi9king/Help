# 기능 명세: 퍼즐/능력 프레임워크 (인터뷰 기반)

> 2026-07-13 인터뷰에서 확정한 설계를 **검증 가능한 기능 명세**로 정리한다. 각 항목은 명세별 테스트로 PASS/FAIL 판정한다.
> 검증 방법: **EM**=EditMode 테스트, **RT**=런타임(MCP Play) 실측, **DATA**=에셋/데이터 검사.

## 핵심 원리 (인터뷰 정정)
> 게임의 핵심 판정 = "이 방/퍼즐이 요구하는 아이템을, 지금 가진 재료로 크래프팅할 수 있는가?"
> 속성은 핵심이 아니라 크래프팅 다양성의 한 축. 능력(Capability)이 요구/제공의 공통 화폐.

## 명세 항목

| # | 명세 | 기대 동작 | 검증 |
|---|------|-----------|------|
| FS-1 | **크래프팅 기반 입장 판정(레이어1)** | 방 요구를 만족하는 아이템을 현재 재료 풀로 제작 가능 → 입장 허용, 불가 → 차단 | EM |
| FS-2 | **요구의 능력 일반화** | 요구를 능력(Capability)으로 표현 가능. 능력 제공 아이템 제작 가능 시 허용. 기존 속성/무기 요구도 계속 동작(회귀 0) | EM |
| FS-3 | **능력·타입 직교** | 한 아이템이 타입(무기/서브무기 등)과 무관하게 능력을 가질 수 있다 | DATA |
| FS-4 | **능력 매칭 정확성** | required=None→항상 해제, 일치→해제, 불일치→미해제 | EM |
| FS-5 | **레이어2 물리 해제** | 요구 능력이 적용되면 장애물이 실제 제거(콜라이더/오브젝트)되어 지형이 열림 | RT |
| FS-6 | **출처 무관 적용(주무기 공격)** | 주무기 공격이 장착 무기의 능력을 장애물에 적용해 해제 | RT |
| FS-7 | **출처 무관 적용(서브무기 사용)** | 서브무기 "사용(use)"이 장착 서브무기 능력을 장애물에 적용해 해제 | RT |
| FS-8 | **주/서브무기 슬롯 독립** | 주무기와 서브무기를 동시 장착, 각각 독립 능력 보유 | RT |
| FS-9 | **능력 분리** | 요구와 다른 능력 적용 시 미해제(예: Melt로 BreakWall 벽 안 부숨) | RT |
| FS-10 | **진행 관문 - 물리 장애물** | 미해제 장애물이 플레이어 이동을 물리적으로 막고, 해제 후 통과 가능 | RT |
| FS-11 | **진행 관문 - 클리어 플래그** | RoomPuzzle 미해결 시 출구 잠김, 해결 시 열림. RoomPuzzle 없으면 자유 출입 | RT |
| FS-12 | **사용 액션의 일반성(전투)** | 인터뷰: "사용 액션은 전투에도" — 서브무기 사용이 전투(적)에도 효과 | RT |
| FS-13 | **속성 열쇠-자물쇠 병존** | 일치 속성 무기=정타, 불일치=대폭 감소(최소 1). 능력과 병렬 축 | EM+RT |
| FS-14 | **확장성** | 능력/아이템/타깃 추가가 프레임워크 코드 변경 없이 가능(enum 값+데이터+컴포넌트) | 구조/EM |
| FS-15 | **데이터 기반 방 콘텐츠 스폰** | 방 유형별 콘텐츠가 데이터에서 스폰, 방 이동 시 교체, 사망 리셋 시 재스폰 | RT |

## 명세별 테스트 결과 (2026-07-13, EditMode 145/145 + MCP Play 실측)

| # | 결과 | 근거 |
|---|------|------|
| FS-1 | ✅ PASS | EM `EntryRequirementCheckerTests`(재료 부족→차단, 속성/무기 요구→제작가능 시 허용, 장착 분해 재료 합산) |
| FS-2 | ✅ PASS | EM 능력 요구 3종(제공 아이템 제작가능→허용, 재료부족→차단, 제공 아이템 없음→차단) + 기존 속성/무기 회귀 0 |
| FS-3 | ✅ PASS | DATA: axe(Type=Weapon,Caps=[BreakWall]), flare(Type=SubWeapon,Caps=[Melt]) — 타입 직교 |
| FS-4 | ✅ PASS | EM `CapabilityMatchTests`(None 항상/일치/불일치/빈집합) |
| FS-5 | ✅ PASS | RT: 능력 적용 시 장애물 GameObject 비활성(콜라이더 제거) |
| FS-6 | ✅ PASS | RT: 도끼(BreakWall) 공격→벽 해제(RESOLVED) |
| FS-7 | ✅ PASS | RT: 서브무기 사용(FLARE/Melt)→얼음벽 해제(MELTED) |
| FS-8 | ✅ PASS | RT: weapon=axe·sub=flare 동시 장착, caps=[BreakWall]·subCaps=[Melt] 독립 |
| FS-9 | ✅ PASS | RT: Melt 사용을 BreakWall 벽에→미해제(정확한 능력 분리) |
| FS-10 | ✅ PASS | RT: 미해제 벽이 이동 차단(x=0.75 정지)→부순 뒤 통과(x=4.73) |
| FS-11 | ✅ PASS | RT: 기본 Entered/SetExitLock(true)→Blocked/해제→통과. RoomPuzzle 없으면 자유(트랩 0) |
| FS-12 | ✅ **PASS** (2026-07-20) | RT: 서브무기 사용이 적에게 **상태이상**으로 작용. ROPE(Bind) 사용→`restrained=True pulled=True`. 속박 중 5초간 위치 고정·플레이어 HP 불변(공격 봉인), 해제 후 공격 재개. 끌기: 적이 2.95→1.22로 끌려와 정지거리(0.8)에서 멈춤. 구현=`SubWeaponEffectResolver`(순수) + `EnemyStatus`/`PullMotion` |
| FS-13 | ✅ PASS | EM `DamageCalculatorTests` + RT: 일치(Fire vs Fire)=정타, 불일치(None)=1(최소 보장). 능력과 병렬 축 |
| FS-14 | ✅ PASS | 구조: 이번 세션에 BreakWall/Melt/SubWeapon/FLARE를 **enum 값+데이터+컴포넌트만으로** 추가, `CapabilityMatch/SolveTracker/CapabilityTarget` 코어 무변경 |
| FS-15 | ✅ PASS | RT: 시작 방(Combat) 콘텐츠 스폰(적 1), 방 이동 시 교체, 사망 리셋 시 재스폰 |

**요약: 15개 전부 PASS (2026-07-20 FS-12 해소).**

### FS-12 해소 방식 (2026-07-20)
- 채택안: **(B) 일반화** — `SubWeaponUser`가 `CapabilityTarget`뿐 아니라 적(`Hurtbox`)에도 작용.
- **데미지가 아니라 상태이상**으로 구현했다. 로프가 데미지를 주는 건 어색하고, 서브무기 일반에 데미지를 붙이는 건 별도 밸런스 결정이기 때문. "전투 효과"의 첫 형태 = 속박.
- 구조: `Capability` → `SubWeaponEffectResolver`(순수 static, `CapabilityMatch`의 전투판 미러) → `EnemyStatus`(순수 타이머) → `EnemyAI`가 `IsRestrained` 지각으로 행동 봉인 / `PullMotion`(순수)이 외력 적용.
- 확장: 새 전투 능력은 `SubWeaponEffectResolver`의 switch에 case 하나. 프레임워크(`CapabilityMatch`/`CapabilityTarget`) 무변경 — FS-14 유지.

> ⚠ 검증 방법 주의: RT 테스트는 공격/사용을 이벤트 직접 발화(AttackPerformed/UsePerformed)로 구동한다 — 즉 **"액션이 발화됐을 때의 동작"**(명세 대상)을 검증한다.
> **입력 캐비앗은 2026-07-20 해소**: 포커스된 Play에서 좌클릭→`OnAttack`, 우클릭/F→`OnUse`→`SubWeaponUser` 발화를 진단 로그로 실측. 런타임 액션 해석(`Attack`→`/Mouse/leftButton`, `Use`→`/Mouse/rightButton`+`/Keyboard/f`, 둘 다 `enabled=True`)도 확인.

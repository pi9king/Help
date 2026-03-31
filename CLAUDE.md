# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

**항상 답변과 설명은 한국어로 합니다.**

## Project Overview

2D 플랫포머 로그라이크 + 퍼즐 게임. Unity 6 (6000.3.10f1), Universal 2D 템플릿 기반. URP 2D 렌더링 사용.

## Key Packages

- **URP 17.3.0** — `Assets/Settings/UniversalRP.asset`, `Assets/Settings/Renderer2D.asset`
- **Input System 1.18.0** — `Assets/InputSystem_Actions.inputactions`
- **2D Animation / Sprite / SpriteShape / Tilemap** — 2D 툴체인
- **Test Framework 1.6.0** — NUnit 기반 Unity Test Runner

## Build & Test Commands

```bash
UNITY="C:/Program Files/Unity/Hub/Editor/6000.3.10f1/Editor/Unity.exe"

# EditMode 테스트
"$UNITY" -batchmode -nographics -runTests -testPlatform EditMode -projectPath . -testResults results-edit.xml

# PlayMode 테스트
"$UNITY" -batchmode -nographics -runTests -testPlatform PlayMode -projectPath . -testResults results-play.xml

# 특정 테스트 필터
"$UNITY" -batchmode -nographics -runTests -testPlatform EditMode -testFilter "ClassName.MethodName" -projectPath . -testResults results.xml

# Windows 빌드
"$UNITY" -batchmode -nographics -buildWindows64Player ./Build/game.exe -projectPath . -quit
```

## 개발 방법론: TDD + Tidy First

### TDD 사이클 (엄격 준수)

1. **Red**: 실패하는 테스트를 먼저 작성
2. **Green**: 테스트를 통과시키기 위한 최소한의 코드만 구현
3. **Refactor**: 테스트가 통과한 후에만 리팩토링

- 한 번에 하나의 테스트만 작성하고 통과시킨다
- 테스트 이름은 행위를 설명한다: `ShouldDealDamageWhenAttacking`, `ShouldGenerateRoomWithinBounds`
- 모든 테스트는 매 사이클마다 실행한다

### Tidy First: 변경 유형 분리

- **구조적 변경 (Structural)**: 동작 변경 없이 코드 재배열 (이름 변경, 메서드 추출, 코드 이동)
- **행위적 변경 (Behavioral)**: 실제 기능 추가/수정
- 두 유형을 동일 커밋에 절대 섞지 않는다
- 둘 다 필요하면 구조적 변경을 먼저 수행한다

### 커밋 규율

- 모든 테스트 통과 + 컴파일 경고 해결 상태에서만 커밋
- 커밋 메시지에 `[structural]` 또는 `[behavioral]` 접두사를 명시
- 작고 빈번한 커밋 선호

## 테스트 전략

### EditMode 테스트 (순수 로직)

MonoBehaviour에 의존하지 않는 순수 C# 로직을 테스트한다:
- 데미지 계산, 스탯 시스템
- 던전 생성 알고리즘, 방 배치 로직
- 아이템 효과, 퍼즐 판정 로직
- 상태 머신 전이 규칙

```
Assets/Tests/EditMode/  — Assembly: Tests.EditMode (asmdef 필요)
```

### PlayMode 테스트 (통합 테스트)

MonoBehaviour 라이프사이클, 물리, 입력 등 Unity 런타임 기능을 테스트한다:
- 플레이어 이동/충돌
- 방 전환 시 씬 상태
- 적 AI 행동
- UI 상호작용

```
Assets/Tests/PlayMode/ — Assembly: Tests.PlayMode (asmdef 필요)
```

## 아키텍처 패턴

### 상태 관리: enum + switch

```csharp
public enum PlayerState { Idle, Running, Jumping, Attacking, Dashing }

// switch 문으로 상태별 로직 분기
```

FSM 프레임워크나 State 패턴 클래스 계층 대신, enum + switch 조합을 사용한다.

### 이벤트 시스템: C# event/delegate

ScriptableObject 이벤트 채널 대신 C# 네이티브 이벤트를 사용한다:

```csharp
public event Action<int> OnDamageTaken;
public event Action OnRoomCleared;
```

### 던전 생성: 타일맵 기반 (Binding of Isaac 스타일)

- 방(Room)은 미리 정의된 레이아웃 템플릿으로 관리
- 방 간 연결은 그리드 기반 좌표로 관리 (상하좌우 4방향)
- `Tilemap`으로 지형을 렌더링하고, 방 데이터는 순수 C# 클래스로 분리
- 절차적 생성 알고리즘은 MonoBehaviour 독립적으로 작성 (EditMode 테스트 가능)

### 폴더 구조

```
Assets/
├── Scripts/
│   ├── Player/          — 플레이어 컨트롤러, 스탯
│   ├── Enemy/           — 적 AI, 패턴
│   ├── Dungeon/         — 방 생성, 맵 데이터, 타일맵 매핑
│   ├── Item/            — 아이템, 효과
│   ├── Puzzle/          — 퍼즐 메커니즘
│   ├── UI/              — HUD, 메뉴
│   └── Core/            — 게임 매니저, 이벤트, 공용 유틸
├── Prefabs/
├── ScriptableObjects/   — 아이템/적/방 데이터 에셋
├── Tilemaps/            — 타일 팔레트, 타일 에셋
├── Scenes/
└── Tests/
    ├── EditMode/
    └── PlayMode/
```

## 프로젝트 문서

- `Docs/DESIGN.md` — 게임 디자인 문서 (기능, 메커니즘, 규칙, 결정 로그)
- `Docs/ARCHITECTURE.md` — 기술 아키텍처 (시스템 구조, 클래스 설계, 데이터 흐름)

세션이 바뀌면 위 문서를 먼저 읽고 맥락을 파악한다.

## C# 코딩 컨벤션

- **네이밍**: C# 표준 (PascalCase 메서드/프로퍼티, _camelCase private 필드, I 접두사 인터페이스)
- **네임스페이스**: `Help.Player`, `Help.Dungeon`, `Help.Item` 등 기능 단위로 구분
- **GetComponent 캐싱**: `Awake`에서 캐시하여 반복 호출 방지
- **물리**: `FixedUpdate`에서 처리, `Rigidbody2D` 사용
- **GC 최소화**: 핫 패스에서 `new` 할당 자제, 오브젝트 풀링 활용
- **Unity null 체크**: `UnityEngine.Object`의 `== null` 오버로드를 인지하고 사용
- `.meta` 파일은 Unity가 자동 생성 — 수동 생성/삭제 금지

## Unity 환경

- Unity 6 (6000.3.10f1), Linear 색공간, 1920x1080 기본 해상도
- Input System (new), URP 2D Renderer

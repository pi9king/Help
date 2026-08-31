# 핸드오프: 인벤토리/크래프팅 UI 입력 문제 (2026-07-09)

> ## ✅ 해결됨 (2026-07-09, 후속 세션)
> **근본 원인 확정 = 씬에 InventoryUI/CraftingUI 컴포넌트가 각각 2개씩 중복 부착** (하나는 항상 활성인 Canvas에, 하나는 비활성 패널 자기 자신에). (B)의 "이벤트 이중 구독"은 **세션 아티팩트가 아니라 저장된 씬에 박힌 결정적 버그**였다(이 문서 3-B의 "저장 씬엔 각 1개뿐"이라는 관측은 오류였음 — grep으로 GUID 실측 시 각 2개).
>
> **메커니즘**: 패널이 비활성(`m_IsActive:0`)으로 저장돼 패널 위의 #2 컴포넌트는 씬 로드 시 `Start()`가 안 돈다. `I` 키 → Canvas의 #1이 패널을 `SetActive(true)` → 그 순간 패널의 #2가 처음 활성화되며 `Start()` 실행 → `_panel.SetActive(false)`(자기 자신을 다시 끔) + 토글 이벤트에 추가 구독 → 이후 이중 토글로 영구 상쇄. 이것이 "구독자=2"·"열려도 즉시 닫힘" 증상을 전부 설명한다.
>
> **수정**: 패널에 붙은 중복 #2 컴포넌트(InventoryUI/CraftingUI) 제거, 항상 활성인 Canvas의 #1만 유지(`_panel`→각 패널로 정상 배선). HUD의 `_inventoryUI` 참조를 살아있는 #1로 재연결. InventoryUI.Start()의 중복 버튼 배선(GameObject.Find fallback) 제거(HUD가 직렬화 참조로 이미 연결 — 중복 시 버튼 클릭도 이중 토글). 회귀 테스트 3개 추가(TestSceneWiringTests: InventoryUI/CraftingUI 각 정확히 1개·활성 오브젝트·_panel 배선·EventSystem 존재).
>
> **남은 확인**: 에디터가 씬을 연 채였으므로 **디스크 수정을 에디터에서 리로드**한 뒤(그냥 Ctrl+S 하면 수정이 덮어써짐!) Play → I/C 키로 육안 확인. 이하 원문은 조사 기록으로 보존.

---

> 다음 세션은 이 문서 + `Docs/DESIGN.md` + `Docs/ARCHITECTURE.md` + 메모리(`project_prototype_state.md` 항목 21~22)를 먼저 읽을 것.
> **한 줄 요약**: `I`(인벤토리)·`C`(크래프팅) 키가 실제 플레이에서 열리지 않는다. 데이터상 UI는 정상인데 화면 반응이 없다. 근본 원인을 좁혀놨고, 깨끗한 새 세션에서 마지막 확인 + 잔여 수정이 필요.

---

## 1. 증상 (사용자 보고)
- 플레이하면 **점프·이동만** 되고, `I`/`C`로 인벤토리·크래프팅이 **열리지 않음**.
- 공격도 체감상 안 됨(단, 이건 원인 규명·부분 수정됨 — 아래 참조).
- 사용자 환경에서 반복 재현됨. 이번 세션 내 시각 검증이 불가해(아래 5번) 세션 이관 결정.

## 2. 핵심 제약 — 이번 세션에서 시각 검증 불가 (중요)
- **MCP `manage_camera` 스크린샷이 이 세션에서 계속 "캐시된(멈춘) 프레임"만 반환**한다. Canvas를 ScreenSpaceCamera로 바꿔도 동일 프레임 → **오버레이 UI가 캡처에 절대 안 잡힘**. 에디터가 OS 포커스를 못 받는 세션의 알려진 제약(메모리 MCP 항목 참조).
- 따라서 "패널이 실제로 화면에 그려지는가"는 **런타임 오브젝트 데이터(execute_code)로만** 간접 확인했다. 최종 시각 확인은 **① 사용자 육안 또는 ② 독립 실행 빌드(.exe)** 로만 가능.

## 3. 이번 세션에서 확정한 근본 원인
### (A) EventSystem 부재 → **이번 세션에서 수정 완료**
- 씬에 `EventSystem`이 아예 없었음 → **모든 UI 클릭/포인터 이벤트 불가**(HUD의 InventoryButton 클릭 fallback도 무효).
- 조치: `EventSystem` + **`InputSystemUIInputModule`**(New Input System용) GameObject 추가 후 씬 저장 완료.

### (B) 이벤트 이중 구독 → `I`/`C` 이중 토글로 상쇄 (미해결, 재현 조건 규명 필요)
- 런타임 실측(execute_code, 리플렉션):
  - `PlayerController.InventoryToggleRequested` **구독자 = 2**
  - `PlayerController.CraftingToggleRequested` **구독자 = 2**
  - 결과: 키 1회 입력 → `Toggle()` 2회 호출 → 열림→닫힘 **상쇄**되어 화면 무반응. (인벤토리 슬롯이 25개가 아니라 **50개**로 관측된 것도 `Refresh()` 이중 실행 정황.)
- **단, 저장된 씬(edit 모드)에는 InventoryUI/CraftingUI 컴포넌트가 각각 1개뿐**이다. 즉 2중 구독은 **이 세션의 오염**(플레이 중 `refresh_unity`로 인한 도메인 리로드 → `Start()` 재실행·`OnDestroy` 미대응으로 델리게이트 잔존) 때문일 가능성이 높다.
- 따라서 **깨끗한 새 세션(에디터 재시작 후 첫 Play)에서 구독자 수가 1인지 반드시 재확인**해야 진짜 버그인지 세션 아티팩트인지 판별된다.

## 4. 입력 경로 자체는 (엔진 내부적으로) 동작함
- New Input System 시뮬레이션으로 `I` 키를 주입하면(코드 아래) **신선한 세션에서는 패널이 열렸다**(`open false→true` 확인). 즉 `PlayerInput(SendMessages) → PlayerController.OnInventory → InventoryToggleRequested → InventoryUI.Toggle → panel.SetActive` 배선은 성립.
- `PlayerInput`: notificationBehavior=**SendMessages**, 활성 맵=**Player**, `inputIsActive=True`, 페어링된 디바이스=Keyboard+Mouse, `user.valid=True`. 액션 `Inventory`(바인딩 `i`)·`Crafting`(`c`)·`Dash`(`leftShift`) 모두 enabled.
- **의심 지점**: 오염된 세션에서는 위 시뮬레이션이 `open true→true`(무변화)로 나왔다 = 이중 토글. 새 세션에서 시뮬레이션이 정상 토글되면 (B)는 세션 아티팩트로 확정.

## 5. 이번 세션에서 이미 적용·저장한 수정 (재작업 금지)
1. **CraftingUI DB 폴백**: 씬에 `RecipeDatabaseBridge` 없으면 `GameManager.RecipeDatabase` 사용 (`Assets/Scripts/UI/CraftingUI.cs`).
2. **InventoryUI/CraftingUI 컴포넌트 배선**: `Canvas`에 부착 + `_panel/_itemListParent/_itemSlotPrefab/_craftingUI`, `_panel/_recipeListParent/_recipeSlotPrefab` 연결. (이전엔 컴포넌트가 아예 없어 I/C 이벤트 수신자 0이었음.)
3. **슬롯 텍스트 흑→백**: `ItemSlot.prefab`/`RecipeSlot.prefab`의 "Name"/"Requirements" Text가 검정(0,0,0)이라 검정 패널 배경에 묻혔음 → **흰색**으로. (버튼 라벨은 밝은 버튼 위라 검정 유지.)
4. **CanvasScaler**: `ConstantPixelSize(800×600)` → **`ScaleWithScreenSize 1920×1080, match 0.5`**. 두 패널 anchor/pivot 중앙 + anchoredPos(0,0)로 화면 중앙 고정.
5. **EventSystem 추가** (3-A).
6. **InventoryUI.Start**: HUD `InventoryButton` onClick→Toggle 런타임 연결(클릭 fallback). ※ EventSystem 없어 그동안 무효였음.
7. **이동/추락**: 이전에 넣었던 투명벽 `WallL/WallR` **제거**(사용자가 "콜라이더로 이동 방해" 지적), `Ground` localScale x 20→30로 확장.
8. **공격**: 씬 적 `_lockedElement`가 Fire라 무속성 기본공격이 `max(1,10×0.1)=1`뎀 → 30방 필요였음. 테스트 적을 **None**으로(기본공격 10, 3방 처치). 이후 사용자 요청으로 **적 GameObject `SetActive(false)`**(현재 공격 대상 없음).

관련 런타임 실측(정상 확인): 인벤토리 시드 25종, 크래프팅 레시피 6개, 슬롯 세로정렬(Y=-20/-64/-108), 텍스트 흰색, 패널 화면 중앙.

## 6. 다음 세션 권장 순서
1. **깨끗한 재시작**: 에디터 재시작(또는 Edit▸Project Settings▸Editor의 Enter Play Mode 확인) 후 **첫 Play**에서:
   - 구독자 수 재확인(아래 스니펫). **1이면** (B)는 세션 아티팩트 → 실제 `I`/`C` 실키로 토글되는지 사용자/빌드로 확인.
   - **2 이상이면** 진짜 버그 → `InventoryUI`/`CraftingUI`의 `Start` 구독을 중복 방지(구독 전 `-=` 후 `+=`)하거나, 토글 입력을 **직접 폴링**(예: 전용 `UIToggleController`가 `InputAction` 콜백을 직접 구독)으로 단순화. 취약한 SendMessages→C#이벤트→UI 3단 체인을 줄이는 리팩터 고려.
2. **시각 검증은 독립 빌드로**: `"$UNITY" -batchmode -buildWindows64Player ./Build/game.exe -projectPath . -quit` 후 실행 → MCP 스크린샷 제약 없이 UI 육안 확인. (또는 사용자 육안.)
3. **회귀 방지 테스트**: `TestSceneWiringTests`에 (a) Canvas의 InventoryUI/CraftingUI **정확히 1개**, (b) 각 직렬화 필드 배선, (c) **EventSystem 존재** 검증 추가.
4. 남은 UX: 방 콜라이더/사이드뷰 레이아웃 재설계(장르 정합성, 기존 과제), 공격 히트 이펙트, 적 복구.

## 7. 재사용 스니펫 (execute_code, codedom — `using` 금지·완전정규화·로컬함수 금지)
**구독자 수 확인:**
```csharp
var pc=UnityEngine.Object.FindFirstObjectByType<Help.Player.PlayerController>();
var f=pc.GetType().GetField("InventoryToggleRequested",
  System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public);
var d=f.GetValue(pc) as System.Delegate;
return "subs="+(d!=null?d.GetInvocationList().Length:0);
```
**`I` 키 시뮬레이션(플레이 중):**
```csharp
var canvas=GameObject.Find("Canvas");
var p=canvas.transform.Find("InventoryPanel").gameObject;
var kb=UnityEngine.InputSystem.Keyboard.current;
UnityEngine.InputSystem.InputSystem.QueueStateEvent(kb, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.I));
UnityEngine.InputSystem.InputSystem.Update();
UnityEngine.InputSystem.InputSystem.QueueStateEvent(kb, new UnityEngine.InputSystem.LowLevel.KeyboardState());
UnityEngine.InputSystem.InputSystem.Update();
return "open="+p.activeSelf;
```
**플레이 상태 확인(editor/state 리소스는 부정확 — 이걸 쓸 것):**
```csharp
return "playing="+UnityEditor.EditorApplication.isPlaying+" compiling="+UnityEditor.EditorApplication.isCompiling;
```

## 8. 관련 파일
- 씬: `Assets/Scenes/TestScene/TestScene.unity` (`Canvas`에 InventoryUI/CraftingUI, `Canvas/InventoryPanel`, `Canvas/CraftingPanel`, 신규 `EventSystem`)
- UI: `Assets/Scripts/UI/InventoryUI.cs`, `CraftingUI.cs`, `RecipeDatabaseBridge.cs`
- 입력: `Assets/InputSystem_Actions.inputactions` (Player맵: Inventory=`i`, Crafting=`c`(+Crouch에도 `c` 중복), Dash=`leftShift`(+Sprint에도 중복), Attack=`좌클릭`/`Enter`)
- 프리팹: `Assets/Prefabs/ItemSlot.prefab`, `RecipeSlot.prefab`
- 입력 배선: `Assets/Scripts/Player/PlayerController.cs`(OnInventory/OnCrafting/OnInteract + 이벤트)
- MCP: HTTP `http://127.0.0.1:8080/mcp`

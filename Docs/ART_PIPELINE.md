# AI 아트 파이프라인

> 도트를 직접 찍지 않고 AI로 그래픽을 만드는 방법.
> 어떤 생성 도구를 쓰든 같은 후처리를 탄다.
> 최종 갱신: 2026-10-05
>
> **플레이어 캐릭터(E 골렘 기사)는 이 파이프라인을 타지 않는다.** 전용 명세서와 전용 추출
> 스크립트를 쓴다 — 3-2-1절 참조. 아래 0~2절은 타일·적·아이콘·이펙트용이다.

## 0. 3단계로 끝난다

1. AI로 이미지를 만든다 (프롬프트는 3절)
2. `ArtSource/raw/` 에 넣는다 — 파일명에 목표 크기를 적는다: `grunt@32x48-o.png`
3. Unity에서 **Help ▸ Art ▸ Process Art Source**

끝나면 `Assets/Sprites/grunt.png` 가 생기고 임포트 설정(PPU 32, Point, 무압축)까지 맞춰져 있다.

---

## 1. 규격

### PPU 32 고정 — 이게 왜 중요한가

**32픽셀 = 월드 1유닛 = 타일 한 칸.** 이 자만 고정하고 스프라이트의 픽셀 크기는 자유롭게 쓴다.

- 타일 32×32 → 화면에서 1×1칸
- 플레이어(E 골렘 기사) 64×64 → 2×2칸
- 보스 96×96 → 3×3칸

**보스를 나중에 더 크게 하고 싶으면 그때 128×160으로 다시 뽑으면 된다.**
다른 에셋에 아무 영향이 없다. 반대로 PPU 자체를 바꾸면 기존 타일·방 좌표를 전부 다시 맞춰야 하고,
그 다음엔 또 못 바꾼다. 그래서 PPU는 건드리지 않는다.

### 파일명 규약

```
name[@WxH][-o].png
```
- `@WxH` — 목표 픽셀 크기. 생략하면 32×32
- `-o` — 1픽셀 어두운 아웃라인을 두른다(캐릭터에 권장, 타일에는 쓰지 않는다)

예: `grunt@32x48-o.png`, `tile_floor.png`, `boss@96x96-o.png`

> 출력 파일명은 `@`와 `-o`를 뗀 이름이다. 기존 스프라이트를 갈아 끼우려면
> `Assets/Sprites/`에 있는 것과 **같은 이름**으로 만들면 된다.

---

## 2. 후처리가 하는 일

| 단계 | 하는 일 | 왜 |
|---|---|---|
| 1. 배경 제거 | 마젠타(#FF00FF) 키잉, 없으면 모서리에서 flood fill | AI는 투명 배경을 잘 못 준다 |
| 2. 여백 잘라내기 | 알파 바운딩 박스로 크롭 | 여백째 축소하면 캐릭터만 작아진다 |
| 3. 축소 | **최빈색** 박스 샘플링 | 평균(bilinear)을 내면 픽셀 경계가 뭉개진다 |
| 4. 팔레트 통일 | `ArtSource/palette.png`의 32색으로 OkLab 최근접 매핑 | 여러 에셋을 한 게임의 그림으로 묶는 단계 |
| 5. 아웃라인 | 실루엣 바깥 1픽셀 (`-o`일 때만) | 배경 위에서 캐릭터가 묻히지 않게 |
| 6. 저장 | PNG + 임포트 설정 | PPU 32 / Point / 무압축 / mipmap 끔 |

디더링은 하지 않는다 — 픽셀아트에서는 노이즈로만 보인다.

### 팔레트 바꾸기

`ArtSource/palette.png` 는 가로 32칸짜리 색 띠다. 이미지 편집기로 직접 고치거나
`Tools/gen_palette.py`의 색 코드를 바꿔 다시 생성하면 된다.
**이 파일 하나가 게임 전체의 색조를 결정한다.**

현재 색조는 UI(`Assets/Scripts/UI/UITheme.cs`)의 레트로 픽셀 아케이드 톤에서 파생했다 —
UI와 게임 화면의 색이 따로 놀지 않게.

---

## 3. 프롬프트북

### 3-1. 공통 스타일 문장 (모든 프롬프트 앞에 붙인다)

> ⚠ **아래 문장과 3-2의 적·타일 프롬프트는 측면 플랫포머 시절에 쓴 것이다**(`side view, facing right`,
> `side-scroller platformer ground`). 게임은 쿼터뷰로 전환됐고 플레이어는 이미 쿼터뷰로 다시 만들었지만,
> 적·타일 프롬프트는 **아직 갱신하지 않았다.** 이것들을 쿼터뷰로 어떻게 고칠지는 정해진 바 없다.

**범용 이미지 AI용 (ChatGPT / Gemini / Midjourney 등)**
```
16-bit pixel art game sprite, side view, orthographic, facing right.
Hard pixel edges with NO anti-aliasing, NO gradients, NO blur, NO dithering.
Limited palette of about 12-16 flat colors. Clear dark outline around the silhouette.
Solid pure magenta background (#FF00FF), nothing else in the background.
Single subject, centered, filling the frame. No text, no watermark, no drop shadow.
Dark retro arcade color scheme: deep navy shadows, cyan and gold accents.
```

**전용 픽셀아트 AI용 (Retro Diffusion / PixelLab 등)** — 이미 격자를 지켜 주므로 짧게
```
side-view game sprite, facing right, dark retro arcade palette, cyan and gold accents,
transparent background
```
격자 크기(32×48 등)와 팔레트 제한은 도구 설정에서 지정한다.

### 3-2. 에셋별 프롬프트

> **`{공통}` = 3-1의 문장.**

#### 플레이어 — `Assets/Sprites/E_Character/*.png`

**→ 3-2-1절 참조.** 이 절의 범용 프롬프트가 아니라 **전용 명세서**를 쓴다:
`ArtSource/E_Animation/Reference/E_Character_Sprite_AI_Spec.md`.

#### 타일 — `tile_floor.png`, `tile_wall.png`, `tile_platform.png`, `tile_spike.png`, `tile_pit.png`

**여기가 화면 면적의 대부분이다. 타일이 바뀌면 게임이 제일 달라 보인다.**
타일은 아웃라인(`-o`)을 붙이지 않는다 — 이웃 타일과 이어져야 한다.

```
{공통}
Seamless 32x32 dungeon floor tile, top surface of stone bricks with worn edges,
side-scroller platformer ground. Tileable horizontally.
```
`tile_wall` — `dungeon wall tile, dark stone blocks with mortar lines, in shadow`
`tile_platform` — `narrow wooden platform, thick top plank with bright top edge, hollow underneath`
`tile_spike` — `row of sharp metal spikes rising from a dark base, dangerous, bright metallic tips`
`tile_pit` — `bottomless dark hole in the ground, ragged stone rim at the top, pure black inside`

#### 적 — `grunt@32x48-o.png`, `archer@32x48-o.png`, `brute@40x56-o.png`, `boss@96x96-o.png`

플레이어가 글자이므로 적도 **기호/문자 계열**로 가면 세계관이 단단해진다.

```
{공통}
A hostile living punctuation mark as a small dungeon monster: a comma-shaped
blob with angry eyes and tiny claws, sickly green body, dark outline.
```
`archer` — `a lean question-mark shaped creature holding a small bow, purple body, alert posture`
`brute` — `a heavy square-bracket shaped brute, thick armored body, stone grey, slow and massive`
`boss` — `a towering ampersand-shaped boss monster, ornate and menacing, gold and deep purple, glowing eyes`

#### 아이템 아이콘 — `icon_axe.png` 등 (32×32)

```
{공통}
Game inventory icon of a battle axe, 3/4 view, clean silhouette,
readable at 32x32, gold and steel colors.
```
무기 6종(AXE, BLADE, KNIFE, RAPIER, SABER, SPEAR)과 서브무기 3종(FLARE, KEY, ROPE).

#### 이펙트 — `fx_slash.png`, `fx_hit.png` (32×32)

```
{공통}
A crescent slash effect, bright cyan energy arc, thick to thin taper,
on magenta background, no character.
```

### 3-2-1. 플레이어 캐릭터 — E 골렘 기사 (쿼터뷰)

> **단일 진실은 명세서다**: `ArtSource/E_Animation/Reference/E_Character_Sprite_AI_Spec.md` (사용자 작성).
> 아래는 요약이며 충돌하면 **명세서가 이긴다**. `§`는 명세서의 절 번호다.
> **2026-10-05 업데이트:** 사용자가 A/B 비교 후 B의 방향 설계를 선택했다.
> 현재 8개 게임 시트는 B 기준이며, 생성 원본은 `ArtSource/E_Animation/Concepts/`다.
>
> 컨셉 레퍼런스(같은 폴더):
> - `E 골렘 기사 픽셀 아트 스프라이트 시트.png` — 동작·장비·색 변형·스킬 이펙트 전체 컨셉
> - `E베이스 8방향.png` — 방향별 회전 기준

#### 정체성 — 양보 불가 3원칙 (§0)

1. **E 자체가 몸이다.** 사람이나 로봇이 E 모양 갑옷을 입은 게 아니다.
2. **머리와 얼굴이 없다.** 눈·입·바이저·얼굴 구멍·사람 머리용 헬멧이 존재하지 않는다.
3. **모든 방향·애니메이션에서 같은 E 비율과 장비 구조를 유지한다.**

정체는 *의지를 얻어 움직이기 시작한 고대 문자 대문자 E*다. 좌우에서는 E 실루엣을,
Down에서는 정면의 구조적 E와 팔·발의 방향 일치를 우선한다 (§1). 32~64px로 줄여도 E로 인식돼야 한다.
망토·무기·장갑을 다 떼면 남는 것은
**E + 짧은 팔 + 짧은 다리**다.

#### 외형 (§2, §4~§7, §13)

| 부위 | 사양 |
|---|---|
| 본체 | 두꺼운 입체 대문자 E. 짙은 청회색 금속+석재. 균열·마모 허용하되 폐허 느낌 금지. 기계 로봇보다 **고대 마법 유물** |
| 테두리 | 낡은 금색/황동 보강재. 화려한 왕실 장식 아님, 과한 장식 금지 |
| 룬 | 세로 획 중심에 얇은 청록(Cyan/Turquoise) 마력 회로. **얼굴처럼 보이면 안 된다** — 두 점·눈 모양·바이저 금지 |
| 팔 | E 본체에서 직접 나온다. 짧고 굵게. 인간형 어깨/몸통 구조 없음. 손은 큼직한 장갑 |
| 다리 | E 하단 획 아래에서 직접 나온다. 사람처럼 긴 다리 금지. 발은 다리보다 크게 |
| 망토 | 진홍/암적. 뒤로 흐르며 E를 가리지 않는다. **정면에서 세 가로획이 충분히 보여야 한다.** 고정 위치는 전 프레임 동일 |
| 검 | 작은 판타지 검. 대검 금지. 본체 E보다 먼저 눈에 띄면 안 된다. 밝은 회백색 날 + 금색 손잡이. **오른손** |
| 투구 | 사람 머리에 씌우는 것이 아니라 **E의 상단 획을 덮거나 감싸는 장비** (§4) |

가장 밝은 요소는 **검날 / 청록 룬 / 금속 하이라이트**로 제한한다.
E의 상단 가로획은 머리가 아니라 그냥 **상단 획**이다 (§3).

#### 방향 — 가장 중요 (§8~§10)

게임은 **Top-down Quarter View**다. 완전한 사이드뷰도 완전한 정면뷰도 아니다.

```text
             UP
            180°

LEFT                     RIGHT
-80°                     +80°

             DOWN
              0°
```

- **Down 0°** — 넓은 정면 E 구조와 중앙 룬, 짧은 팔·발의 정면 배치를 보인다.
- **Right +80° / Left -80°** — 완전 측면(90°)이 **아니다**. 완전 측면보다 **10° 더 정면 쪽**을 보여주며
  **E의 정면 면적이 조금 남아 있어야** 한다. 45° 대각선처럼 보이면 안 된다
- **Up 180°** — 후면. 정면 룬은 거의 보이지 않고 망토와 후면 구조가 중심

**§10 — 좌우 반전 금지.** Left와 Right를 단순 Mirror Flip 하지 않는다. 검을 든 손, 망토 고정 위치,
룬 위치, 금장 위치, E 입체 구조가 달라지기 때문이다. 각 방향을 독립 제작하되 몸 비율과 회전각은
대칭 관계를 유지한다.

> 이전 A 시트의 Left 미러 교체는 `Docs/OPEN_QUESTIONS.md` #7에 기록돼 있다.
> 현재 B 시트의 Left/Right는 별도 생성 원본에서 왔다.

#### 규격 (§11, §24, §25)

- 셀 **64×64 px**, 캐릭터 점유 높이 **48~56px**
- 피벗 **bottom-center (0.5, 0)** — 두 발 사이 바닥 접점. 애니메이션 중 중심이 좌우로 튀면 안 된다
- 생성 배경은 **solid #FF00FF**(환경·바닥·UI·텍스트·가이드라인 없음), 최종 단계에서 제거해 투명 PNG로
- PPU는 프로젝트 공통 32 → 64px 셀 = 월드 2×2유닛

#### 픽셀아트 스타일 (§12)

현대적인 16-bit / 32-bit 판타지 픽셀아트.

```text
hard pixel edges / pixel-perfect silhouette / limited palette
no anti-aliasing / no smooth vector edges / no painterly brush texture
no realistic rendering / no 3D-rendered appearance
```

**컨셉아트보다 실제 스프라이트에서는 디테일을 줄인다.** 작은 균열·장식·금장을 다 표현하려 하지 말고,
작은 크기에서도 읽히는 큰 형태를 우선한다.

#### 애니메이션 (§15~§22)

| 동작 | 방향 | 프레임 | FPS | 핵심 |
|---|---:|---:|---:|---|
| Idle | 4 | 4 | 6~8 | 현재 B는 방향별 기준 자세 1장을 4회 반복해 표면 떨림을 멈춘 상태. 미세 동작은 추가 검수 대상 |
| Walk | 4 | 6 | 10~12 | Contact-Down-Passing ×2. 본체는 1~2px만. **E가 고무처럼 휘면 안 된다.** 팔은 다리와 반대 위상 |
| Attack | 4 | 6 | 10~14 | Idle-Anticipation-Wind-up-**Impact**-Follow-through-Recovery. 검 궤적은 별도 이펙트 레이어 권장 |
| Hit | 4 | 3 | 10~12 | Normal-Impact-Recovery. 1~2px 뒤로 밀림 + 작은 파편 + 짧은 밝은 플래시 |
| Death | 공통/Down | 8 | 8~10 | 사람처럼 넘어지지 않는다. **고대 E 구조물의 붕괴** — 균열→기울어짐→조각 낙하→잔해→룬 소멸 |
| Skill | 4 또는 공통 | 6~8 | 10~12 | E 내부 고대 문자 활성화(지면 룬·문자 회전·청록 마력 기둥·룬 마법진). 본체 비율 유지 |

장비 변형 7종 (§22): **Unarmed / Sword / Shield / Staff / Spellbook / Helmet / Cape Variation**.
장비가 바뀌어도 **기본 E 본체는 바뀌지 않는다.**

#### 금지 사항 (§26)

```text
human head / human face / eyes / mouth / helmet-shaped head / visor / robot face
cute mascot face / floating head / extra limbs / long human legs / oversized weapon
rounded E silhouette / letter F / letter B / reversed E / warped E
different body proportions between frames
different armor design between frames
different cape design between frames
different rune placement between frames
random accessories
perspective changes between animation frames
45-degree diagonal Left/Right view
```

#### 제작 순서 (§14, §29)

**전체 애니메이션을 한 번에 생성하지 않는다.**

1. **Master Direction Sheet 4장(Down/Left/Right/Up)만 먼저 만든다**
2. 방향·비율 검수 — 특히 **Left/Right 각도를 먼저 검수**한다
3. Idle → Walk → Attack → Hit → Death → Skill → 장비 변형
4. 배경 제거 및 Unity용 시트 정리

4방향이 확정되기 전에는 애니메이션 제작을 시작하지 않는다. Master 4장은 크기·E 비율·팔 길이·다리 길이·
금장 위치·망토 디자인·룬 위치·색상 팔레트·검 디자인이 **전부 동일**해야 한다.

#### 마스터 프롬프트

전문은 명세서 **§27**에 있다. **컨셉 이미지와 함께** 제공한다. 새 세션에서의 첫 요청 문구는 §28에 있다.

#### 현재 저장소 상태 (2026-10-05 관찰)

`Assets/Sprites/E_Character/` — 투명 PNG 8장, 총 101셀. 전부 64×64, 피벗 하단 중앙.
행 순서는 **Down, Left, Right, Up**이고 프레임은 왼쪽에서 오른쪽으로 진행한다.

| 파일 | 그리드 | 내용 |
|---|---|---|
| `E_Master.png` | 4×1 | Down / Left / Right / Up |
| `E_Idle.png` | 4×4 | 방향당 4 |
| `E_Walk.png` | 6×4 | 방향당 6 |
| `E_Attack.png` | 6×4 | 방향당 6 |
| `E_Hit.png` | 3×4 | 방향당 3 |
| `E_Death.png` | 8×1 | Down 공통 8 |
| `E_Skill.png` | 6×1 | Down 공통 6 |
| `E_Equipment.png` | 7×1 | 장비 변형 7 (정지) |

`Assets/Editor/ECharacterSpriteImport.cs`가 임포트 시 셀 분할·하단 중앙 피벗·PPU 32·Point·무압축·
밉맵 끔을 적용한다. `Assets/Animations/E_Character/`에 AnimationClip 18개와 `E_Character.controller`,
미리보기 씬은 `Assets/Scenes/EAnimationPreview.unity`.

현재 재생성기는 `Tools/build_e_character_b.py`다. 기존
`Tools/Export-E-Animations.ps1`과 `Tools/Export-E-MasterSprites.ps1`도 이 생성기를 호출한다.
정렬 검사는 `Tools/Test-E-SpriteAlignment.ps1`이다.

> 생성된 B 프레임에도 세부 무늬와 걷기 리듬의 수동 검수는 남아 있다.
> 과거 A 시트의 측정값은 `Docs/OPEN_QUESTIONS.md` #7에 보관했다.

#### 폐기된 옛 컨셉

2026-09-03까지는 **32×48 측면 플랫포머용 다크나이트**였다(얼굴 없음, 투구 속 청록 가로 틈 셋, 빈 손,
한쪽 어깨 망토). 쿼터뷰 전환으로 **폐기**했다. `Assets/Sprites/player_E.png`가 그 시절 잔재로 아직
옛 씬(TestScene·WeaponTestArena)에서 쓰인다. 시안 스프라이트(`player_E_draftA/B`·`knight`·`rogue`)와
`Tools/gen_player_*.py`는 2026-10-06 정리에서 삭제했다.

### 3-3. 알파벳 재료는 AI로 만들지 마라

재료 25종(A~Z, E 제외)은 **글자 글리프**다. AI는 글자를 정확히 못 그리고,
25개의 일관성이 생명이다. `Assets/Editor/SpriteGenerator.cs`에 폰트 렌더 + 픽셀화 경로를 두는 편이
훨씬 정확하고 일관된다. (현재 미구현 — 필요해지면 추가)

### 3-4. 일관성 유지법

1. **첫 에셋을 레퍼런스로 삼는다.** 플레이어를 먼저 만들고, 이후 생성마다 그 이미지를 첨부해
   "match this art style exactly"를 붙인다. 특히 Gemini/Nano Banana 계열이 레퍼런스를 잘 따른다.
2. **팔레트 띠를 함께 첨부한다.** `ArtSource/palette.png`를 붙이고
   "use only colors from this palette"라고 지시한다.
3. **한 번에 여러 포즈를 뽑는다.** "sprite sheet, 4 frames of a walk cycle, evenly spaced,
   same character" — 한 장에 나오면 캐릭터가 프레임마다 달라지는 문제가 줄어든다.
4. **20장 뽑아 3장 고른다.** 이게 정상이다. AI 아트는 선별 작업이지 생성 작업이 아니다.

---

## 4. 작업 우선순위

| 순서 | 에셋 | 크기 | 왜 이 순서인가 |
|---|---|---|---|
| 1 | 타일 5종 | 32×32 | 화면 면적의 대부분. 여기가 바뀌면 게임이 제일 달라 보인다 |
| 2 | 플레이어 E 골렘 기사 | 64×64 | 계속 보고 있는 것. 이후 모든 에셋의 스타일 레퍼런스가 된다 (1차 완료, 3-2-1 참조) |
| 3 | 적 3종 + 보스 | 32×48 / 96×96 | 전투가 게임 시간의 대부분 |
| 4 | 아이템 아이콘 9종 | 32×32 | 인벤토리/크래프팅 UI |
| 5 | 문·포탈·이펙트 | 32×32 | 마무리 |

층 테마별 타일셋(3벌)은 층 테마 설계가 정해진 뒤에 (`Docs/OPEN_QUESTIONS.md` #4).

---

## 5. 애니메이션

**플레이어는 AnimationClip을 쓴다.** `Assets/Animations/E_Character/`에 클립 18개와
`E_Character.controller`가 있다 — Idle/Walk/Attack/Hit 각 4방향 + Death/Skill(Down 공통).
Idle과 Walk만 반복한다. 프레임 수와 FPS는 3-2-1절 표와 `ArtSource/E_Animation/animation_manifest.json`.

**Left와 Right는 별도 그림을 쓰고 `flipX`를 적용하지 않는다** — 명세서 §10(좌우 반전 금지) 때문이다.
플레이어 루트 Transform도 반전하지 않는다(자식 히트박스 좌표 보존, `Docs/QUARTER_VIEW_MIGRATION.md`).

그 외 피격 플래시·슬래시 이펙트는 여전히 코드로 처리한다(`HitFlash`, `SlashVFX`).
적·오브젝트는 아직 AnimationClip이 없고, 필요해지면 `Sprite[]` + fps 경량 컴포넌트로 간다
(Animator 에셋 불필요).

> **현재 상태**: 플레이어 애니메이션 구현됨(미리보기 씬 `Assets/Scenes/EAnimationPreview.unity`).
> 적·오브젝트 스프라이트 애니메이션은 미구현.

---

## 6. 관련 파일

| 파일 | 역할 |
|---|---|
| `ArtSource/raw/` | AI 원본을 넣는 곳 (Assets 밖 — Unity가 임포트하지 않는다) |
| `ArtSource/palette.png` | 프로젝트 팔레트 32색. **게임 전체 색조의 단일 진실** |
| `Tools/gen_palette.py` | 팔레트 생성/수정 |
| `Assets/Editor/PixelArtPipeline.cs` | 후처리 실행(메뉴 Help ▸ Art ▸ Process Art Source) |
| `Assets/Editor/PixelArtOps.cs` | 순수 이미지 연산(배경 제거·축소·양자화·아웃라인) |
| `Assets/Editor/PixelImport.cs` | PNG 저장 + 임포트 규격. 플레이스홀더 생성기와 공유 |
| `Assets/Editor/SpriteGenerator.cs` | 코드로 그리는 플레이스홀더(메뉴 Help ▸ Setup ▸ Generate Placeholder Sprites) |
| `Assets/Tests/EditMode/PixelArtOpsTests.cs` | 후처리 연산 검증 |

### 플레이어 캐릭터 전용 (위 파이프라인과 별개)

| 파일 | 역할 |
|---|---|
| `ArtSource/E_Animation/Reference/E_Character_Sprite_AI_Spec.md` | **캐릭터 사양의 단일 진실** (사용자 작성 명세서) |
| `ArtSource/E_Animation/Reference/*.png` | 컨셉 시트 · 8방향 베이스 |
| `ArtSource/E_Animation/Concepts/` | **현재 B 시트의 고해상도 원본** |
| `ArtSource/E_Animation/animation_manifest.json` | 행 순서 · 프레임 수 · FPS |
| `ArtSource/E_Animation/README.md` | 결과물 설명 · 미리보기 씬 조작법 · 검수 메모 |
| `ArtSource/E_Master/64px/` | Master 4방향 파생 사본 |
| `Tools/build_e_character_b.py` | B 원본 → 게임용 8개 PNG와 프레임·미리보기·마스터 파생본 재생성 |
| `Tools/e_sprite_cleanup.py` · `Tools/e_color_transfer.py` | 컨셉 자르기·마젠타 제거 / 원본 색 복원 (생성기가 사용) |
| `ArtSource/E_Animation/Archive/BeforeB/E_Master.png` | 색 복원의 기준(원본 A Master) |
| `Tools/build_arm_rig_poc.py` · `Tools/arm_motion_defaults.py` | 팔 리그 에셋 / 키 자세 초안 (`Docs/ARM_RIG_PLAN.md`) |
| `Tools/Export-E-Animations.ps1` | B 생성기 호출용 기존 명령 |
| `Tools/Export-E-MasterSprites.ps1` | B 생성기 호출용 기존 명령 |
| `Tools/Test-E-SpriteAlignment.ps1` | 출력 정렬(발 접점·셀 중심) 검사 |
| `Assets/Editor/ECharacterSpriteImport.cs` | 64px 셀 분할 · 하단 중앙 피벗 · PPU 32 · Point · 무압축 |

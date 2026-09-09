# AI 아트 파이프라인

> 도트를 직접 찍지 않고 AI로 그래픽을 만드는 방법.
> 어떤 생성 도구를 쓰든 같은 후처리를 탄다.
> 최종 갱신: 2026-09-03

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
- 플레이어 32×48 → 1×1.5칸
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

예: `player_E@32x48-o.png`, `tile_floor.png`, `boss@96x96-o.png`

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

#### 플레이어 — `player_E@32x48-o.png`

**→ 아래 3-2-1절 참조.** 초기에 "몸통 자체가 글자 E"로 잡았다가 폐기했다
(활자를 박아 놓은 것처럼 보였고, AI도 똑같이 그렸다). 확정 사양과 프롬프트는 3-2-1에 있다.

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

### 3-2-1. 플레이어 캐릭터 — 확정 사양

여러 방향을 시도하고 되돌린 끝에 정해진 것. 되돌린 이유까지 남긴다 —
모르면 같은 실수를 반복한다.

| 항목 | 확정 | 왜 |
|---|---|---|
| 정체성 | 알파벳 E, 단 **글자로 읽히지 않게** | 몸통을 E로 만들었더니 활자가 박힌 것처럼 보였다 |
| E를 녹이는 법 | **가로 세 줄의 반복** | E의 본질은 글자가 아니라 '평행한 세 줄'이다 |
| 금기 | 세 줄에 **세로 기둥을 붙이지 않는다** | 기둥이 붙는 순간 글자가 되고, 없으면 계급장 줄무늬로 읽힌다 |
| 톤 | 다크나이트 — 어둡되 위압적 | 도적으로 만들었더니 주인공이 음침해졌다 |
| 자세 | 꼿꼿이, 어깨 넓게, 대칭에 가깝게 | 웅크리고 굽은 자세가 '음침함'의 원인이었다 |
| 얼굴 | **없음.** 투구 속 빛나는 가로 틈 셋 | 얼굴을 그리는 순간 마스코트가 된다 |
| 무기 | **없음.** 두 손을 비운다 | 무기 교체가 이 게임의 핵심 루프다. 박아 넣으면 설계가 어긋난다 |
| 초점 | 바이저가 화면에서 가장 밝다 | 플레이어는 얼굴을 본다. 금색 채도를 낮춰 자리를 내줬다 |
| 규격 | 32×48, 측면, 오른쪽 향함 | |

초안: `Tools/gen_player_knight.py` → `ArtSource/preview/knight_step*_x8.png`
(0=기준 … 4=최종. 단계별로 무엇을 바꿨는지 비교할 수 있다)

#### 메인 프롬프트

```
Pixel art game character sprite, 16-bit style, strict side profile facing
right, full body, standing idle.

A dark knight hero — grim and imposing, but noble. Never creepy, never
skulking. Stands upright and tall: chest out, head held high, broad angular
pauldrons, torso tapering to the waist, feet planted firmly.

NO FACE. The inside of the helm is total darkness, broken only by three
narrow horizontal glowing cyan slits — and those slits are the brightest
thing in the entire image. Everything else is subdued so the eye lands there.

A heavy cape is clasped at ONE shoulder only. Its mass hangs behind and
widens downward, with one corner swept out past the body into open air,
breaking the outline asymmetrically. A thin crimson lining shows only where
the cloth is turned back — at the shoulder and along that swept corner.

Three short horizontal bronze bars sit on the chest like a rank insignia,
with NO vertical stroke joining them. Three ridges band each gauntlet.

BOTH HANDS ARE EMPTY — no weapon, no sword, no shield of any kind.

Deep shadow but the form must always read — never a flat black blob.
Dark navy plate armor, near-black cape, crimson lining, muted bronze,
cyan visor glow.

Hard pixel edges, flat colors, no anti-aliasing, no gradients, no dithering,
dark outline around the silhouette. Solid pure magenta (#FF00FF) background,
nothing else in frame.
```

#### 짧은 버전 (전용 픽셀아트 AI용)

격자·팔레트를 도구가 잡아주므로 렌더 지시를 전부 뺀다. 20단어면 충분하다.

```
dark knight, side view facing right, no face, three glowing cyan slits in the
helm, cape clasped at one shoulder, empty hands, dark navy armor with crimson
cape lining, bronze chest bars
```

#### 네거티브 (SD 계열)

```
blurry, anti-aliased, smooth gradients, 3d render, realistic, photo, cute,
chibi, mascot, cartoon eyes, face, mouth, weapon, sword, shield, text,
watermark, white background, multiple characters, full scene
```

#### ★ 절대 규칙

**프롬프트에 `letter`나 `E`를 쓰지 않는다.** 글자를 말하면 AI는 글자를 그린다.
예외가 없었다. 세 줄은 `three`라는 단어만으로 나온다.

#### 증상별 처방

| 나온 것 | 고칠 줄 |
|---|---|
| 얼굴을 그린다 | `NO FACE`를 문장 맨 앞으로 옮긴다 |
| 귀엽다 / 마스코트 | `grim, serious, not cute` + 네거티브에 `chibi, mascot` |
| 흐릿하다 | `hard pixel edges, no anti-aliasing, no blur` |
| 웅크린다 | `standing upright, chest out, never crouching` |
| 무기를 들려준다 | `BOTH HANDS EMPTY`를 대문자로 반복한다 |
| 정면을 본다 | `strict side profile, orthographic` |
| 배경이 남는다 | `flat solid magenta #FF00FF, nothing else in background` |
| 캐릭터가 작다 | `full frame, character fills the canvas` |
| 뻣뻣하다 | `weight on one leg, cape swept to one side` |
| 색이 제멋대로 | `ArtSource/palette.png` 첨부 + `use only these colors` |

#### 깎는 방법

**한 번에 한 줄만 바꾼다.** 두 줄을 동시에 바꾸면 어느 쪽이 효과였는지 영영 모른다.
긴 프롬프트가 더 좋은 것도 아니다 — 토큰이 늘수록 각 지시의 가중치가 흩어지고,
앞쪽 토큰이 훨씬 세게 먹는다. 그래서 가장 중요한 지시를 앞에 둔다.

1. 메인 프롬프트로 4장 뽑는다
2. 가장 안 맞는 것 **하나**를 고른다
3. 그 항목의 처방 한 줄만 붙인다
4. 다시 4장. 좋아졌으면 유지, 아니면 되돌린다

시드를 고정할 수 있는 도구면 고정하고 프롬프트만 바꾼다. 변수가 하나여야 판단이 선다.

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
| 2 | 플레이어 E | 32×48 | 계속 보고 있는 것. 이후 모든 에셋의 스타일 레퍼런스가 된다 |
| 3 | 적 3종 + 보스 | 32×48 / 96×96 | 전투가 게임 시간의 대부분 |
| 4 | 아이템 아이콘 9종 | 32×32 | 인벤토리/크래프팅 UI |
| 5 | 문·포탈·이펙트 | 32×32 | 마무리 |

층 테마별 타일셋(3벌)은 층 테마 설계가 정해진 뒤에 (`Docs/OPEN_QUESTIONS.md` #4).

---

## 5. 애니메이션

현재 프로젝트에는 `Animator`/`AnimationClip` 에셋이 **하나도 없다.**
피격 플래시·슬래시 이펙트 전부 코드로 처리한다(`HitFlash`, `SlashVFX`).
그 방식을 유지한다 — 에셋이 늘어나지 않고 스프라이트 교체만으로 굴러간다.

스프라이트 애니메이션이 필요해지면 `Sprite[]` + fps로 도는 경량 컴포넌트를 추가한다
(Animator 에셋 불필요). 1차 범위는 Idle / Walk 각 4프레임까지.
공격 모션은 기존 `SlashVFX`가 담당하므로 후순위.

> **현재 상태**: 스프라이트 애니메이션 미구현. 좌우 반전은 `transform.localScale.x` 부호로 처리 중.

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

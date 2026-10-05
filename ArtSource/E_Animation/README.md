# E 골렘 기사 스프라이트 결과물

> 2026-10-06 기준. 작업 경과와 결정은 `Docs/E_SPRITE_WORKLOG.md`, 팔·무기 분리는 `Docs/ARM_RIG_PLAN.md`.
> 캐릭터 사양의 단일 진실은 `Reference/E_Character_Sprite_AI_Spec.md`(사용자 작성 명세서)다.

## 현재 상태

- **형태 = B 방향 시안**(사용자 선택), **색 = 원본 A Master**(`Archive/BeforeB/E_Master.png`)를 재질별 히스토그램 매칭으로 옮겼다.
- 게임 시트: `Assets/Sprites/E_Character/` 8장, 64×64 셀 총 101개, 피벗 = 셀 하단 중앙, 방향별 행 순서 Down / Left / Right / Up.
- 모든 동작의 몸 크기·위치·비율을 Idle에 맞췄다. E 아랫막대는 윗막대 길이로 늘렸다(정면 프레임).
- **Walk 4방향은 컨셉 그림을 쓰지 않고 Idle에서 합성한다**: 정면·뒷모습 = Idle 상체 + Idle 부츠 걸음, 옆모습 = Idle 상체 + 앞뒤로 내딛는 두 다리.
- 팔·무기를 분리한 리그는 아직 미리보기 전용이다(`Assets/Resources/ArmRig/`, `Docs/ARM_RIG_PLAN.md`).

| 파일 | 그리드 | 프레임 | FPS |
| --- | --- | --- | --- |
| E_Master.png | 4×1 | Down / Left / Right / Up (Idle 첫 프레임) | 정지 |
| E_Idle.png | 4×4 | 방향마다 4 (같은 자세) | 7 |
| E_Walk.png | 6×4 | 방향마다 6 (Idle에서 합성) | 11 |
| E_Attack.png | 6×4 | 방향마다 6 | 12 |
| E_Hit.png | 3×4 | 방향마다 3 | 11 |
| E_Death.png | 8×1 | Down 공통 8 | 9 |
| E_Skill.png | 6×1 | Down 공통 6 | 11 |
| E_Equipment.png | 7×1 | Unarmed, Sword, Shield, Staff, Spellbook, Helmet, Cape Variation | 정지 |

`Assets/Editor/ECharacterSpriteImport.cs`가 셀 분할·피벗·PPU 32·Point·무압축을 적용한다.

## 재생성과 검사

프로젝트 루트에서 (Python + Pillow·numpy·scipy):

```powershell
python -B Tools/build_e_character_b.py          # 게임 시트 8장 + 아래 파생 사본
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-E-SpriteAlignment.ps1
python -B Tools/build_arm_rig_poc.py            # 팔 리그 에셋 (게임 시트 다음에)
```

`Tools/Export-E-Animations.ps1`, `Tools/Export-E-MasterSprites.ps1`은 생성기를 부르는 호환용 진입점이다.

## 폴더

| 경로 | 내용 |
| --- | --- |
| `Concepts/` | 생성기가 읽는 B 컨셉 원본 7장 (`Concepts/README.md`) |
| `Archive/BeforeB/E_Master.png` | 색 복원의 기준(원본 A Master) |
| `Reference/` | 사용자 명세서 · 컨셉 아트 · 8방향 베이스 |
| `64px_Magenta/`, `Frames_Transparent/`, `Frames_Magenta/`, `Preview/` | 생성기가 만드는 파생 사본(마젠타 배경 시트, 개별 프레임, 4배 미리보기) |
| `../E_Master/64px/` | Master 파생 사본 |
| `animation_manifest.json` | 행 순서 · 프레임 수 · FPS |

## 미리보기 씬

`Assets/Scenes/EAnimationPreview.unity` → Play. 씬 재생성: `Tools ▸ E Character ▸ Build Animation Preview`.
애니메이션 관련 테스트는 모두 이 씬의 패널에 버튼으로 들어간다(`CLAUDE.md` "시각 결과 확인").

| 조작 | 기능 |
| --- | --- |
| W/A/S/D · 방향키 · 방향 버튼 | Up / Left / Down / Right |
| 1~6 · 동작 버튼 | Idle / Walk / Attack / Hit / Death / Skill |
| Space / R | 일시정지 / 처음부터 |
| **ARM RIG** 줄 · 7 / 8 / 9 / 0 | 팔 리그 Slash / Thrust / Smash / Rig Idle (원래 캐릭터 자리에 나타남) |
| ARM RIG 보조 버튼 | 속도, 무기 교체, 반복, 손잡이 표시, 조준(바라보는 방향 / 마우스), Edit keys(키 자세 조정·저장) |

> 초기 A 시트 제작 기록(AI 원본 추출, Left 미러 교체 등)은 2026-10-06 정리에서 원본과 함께 삭제했다.
> 경과는 `Docs/E_SPRITE_WORKLOG.md`와 git 이력에 남아 있다.

# E 골렘 기사 스프라이트 결과물

`D:\다운로드\스프라이트\E_Character_Sprite_AI_Spec.md`와 같은 폴더의 컨셉 아트 및 베이스 8방향 이미지에서 방향 1·3·4·7을 참조했다. 캐릭터는 머리와 얼굴이 없는 살아 있는 대문자 E이며, 슬레이트 색 몸체, 청록 룬, 청동 테두리, 진홍 망토, 짧은 팔다리와 검을 사용한다.

## Unity용 완성 시트

`Assets/Sprites/E_Character/`의 PNG는 배경이 투명하다. 모든 셀은 64×64px이며 피벗은 셀 하단 중앙이다. 방향별 시트의 행 순서는 **Down, Left, Right, Up**이고 프레임은 왼쪽에서 오른쪽으로 진행한다.

| 파일 | 그리드 | 프레임 | 권장 FPS |
| --- | --- | --- | --- |
| E_Master.png | 4×1 | Down / Left / Right / Up | 정지 |
| E_Idle.png | 4×4 | 방향마다 4 | 7 |
| E_Walk.png | 6×4 | 방향마다 6 | 11 |
| E_Attack.png | 6×4 | 방향마다 6 | 12 |
| E_Hit.png | 3×4 | 방향마다 3 | 11 |
| E_Death.png | 8×1 | 공통 Down 기준 8 | 9 |
| E_Skill.png | 6×1 | 공통 Down 기준 6 | 11 |
| E_Equipment.png | 7×1 | Unarmed, Sword, Shield, Staff, Spellbook, Helmet, Cape Variation | 정지 |

`Assets/Editor/ECharacterSpriteImport.cs`는 이 폴더의 시트를 Unity가 가져올 때 셀 분할, 하단 중앙 피벗, 32 PPU(프로젝트의 기존 설정), Point 필터, 비압축 및 밉맵 해제를 적용한다. Unity가 열려 있다면 Asset Database가 새 파일을 감지할 때 적용된다.

## 애니메이션과 테스트 씬

`Assets/Animations/E_Character/`에 18개 AnimationClip과 `E_Character.controller`가 있다. Idle, Walk, Attack, Hit은 각 4방향이고 Death와 Skill은 공통 Down 방향이다. Idle과 Walk만 반복한다. 각 클립의 FPS는 위 표와 같다. `E_Equipment.png`는 정지 장비 변형이므로 AnimationClip에 넣지 않았다.

Unity에서 `Assets/Scenes/EAnimationPreview.unity`를 열고 Play를 누르면 애니메이션을 살펴볼 수 있다. 이 씬은 기존 PlayerController나 게임 진행 로직을 사용하지 않는다. Left와 Right는 별도 그림을 사용하며 `flipX`를 적용하지 않는다.

| 키 | 기능 |
| --- | --- |
| W/A/S/D 또는 방향키 | Up / Left / Down / Right 선택 |
| 1 / 2 / 3 / 4 / 5 / 6 | Idle / Walk / Attack / Hit / Death / Skill |
| Space | 일시정지 / 재개 |
| R | 현재 동작 처음부터 재생 |

Attack, Hit, Skill은 재생을 마치면 Idle로 돌아간다. Death는 마지막 프레임을 유지한다. 씬의 청록색 선은 셀 하단 중앙 피벗 위치를 보여 준다. 클립과 씬을 다시 만들려면 Unity 메뉴 `Tools > E Character > Build Animation Preview`를 실행한다.

## 원본과 재생성

- `AI_Source/`: 이미지 생성 도구로 만든 19개의 고해상도 원본 시트.
- `Reference/`: 제작에 사용한 명세서, 컨셉 아트, 베이스 방향 이미지 사본.
- `64px_Magenta/`: 명세서의 #FF00FF 배경을 유지한 시트.
- `Frames_Transparent/`, `Frames_Magenta/`: 개별 64×64 프레임.
- `Preview/`: 4배 최근접 확대 미리보기.
- `animation_manifest.json`: 행 순서, 프레임 수, 재생 속도.

고해상도 원본을 다시 추출하려면 프로젝트 폴더에서 `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Export-E-Animations.ps1`을 실행한다. 방향별 마스터는 `Tools/Export-E-MasterSprites.ps1`로 재생성한다. 원본 생성에는 Codex 내장 이미지 생성 기능을 사용했고, 추출은 최근접 픽셀 샘플링으로 처리했다.

방향별 Idle·Walk·Attack·Hit 프레임은 각 원본 셀의 발 접점을 기준으로 독립 정렬한다. 생성 원본의 셀 내부 배치가 달라도 최종 캐릭터 발 중심이 셀 x=32 부근, 최하단이 y=60에 놓이도록 한다. Skill은 마법 효과를 유지하고 본체는 정렬된 Idle 프레임을 사용한다. 출력 정렬을 다시 검사하려면 `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-E-SpriteAlignment.ps1`을 실행한다.

## 검수 메모

총 101개 셀이 생성되었고, 투명 시트의 프레임 크기와 알파 채널을 검사했다. Master는 51~52px, Idle은 51~53px, Walk는 49~54px, Hit는 50~55px 높이이다. Death 후반은 의도적으로 무너져 낮아진다. Attack 일부 프레임은 검과 망토 동작으로 56px를 넘고, Skill의 청록 마법 기둥은 셀 상단에서 끊긴다. AI 생성 특성상 세부 갑옷 무늬와 검 궤적에는 프레임 사이 차이가 있어 게임 안에서 재생해 보고 픽셀 단위 수동 보정이 필요할 수 있다.

Unity 6000.3.10f1 배치 실행에서 스크립트 컴파일, 101개 셀의 크기·피벗 확인, 18개 클립과 씬 생성이 성공했다. 별도 PlayMode 테스트에서 씬 로드, Walk 프레임 진행, Left·Right 별도 이미지 및 `flipX` 비활성화를 검사해 1건 통과했다. 결과 파일은 `Validation/PlayModeResult.xml`이다.

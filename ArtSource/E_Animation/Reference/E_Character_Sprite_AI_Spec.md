# E 캐릭터 AI 스프라이트 제작 명세서

> **2026-10-05 방향 결정 — B 시안 채택.** 사용자는 A/B의 Idle·걷기·공격을 비교한 뒤
> 움직임은 아직 다듬을 필요가 있지만 **방향 설계는 B가 맞다**고 결정했다.
> 기준 이미지는 `ArtSource/E_Animation/Concepts/IdleFacing_B_fourDirections.png`이며,
> Down은 정면 E 구조, 짧은 두 팔과 두 발의 정면 배치를 우선한다. 아래 규칙은 이 결정을 반영한다.
> 이 결정은 조준을 360°로 유지할지 4방향으로 맞출지까지 정한 것은 아니다.

## 0. 목적
이 문서는 탑뷰 2D 액션 로그라이크의 플레이어블 캐릭터인 **살아 움직이는 대문자 E**를 AI 이미지 생성으로 실제 게임용 스프라이트까지 제작하기 위한 기준 명세입니다.

가장 중요한 원칙은 다음 3가지입니다.

1. **E 자체가 몸이다.** 사람이나 로봇이 E 모양 갑옷을 입고 있는 것이 아닙니다.
2. **머리와 얼굴이 없다.** 눈, 입, 바이저, 얼굴 구멍, 사람 머리용 헬멧이 존재하지 않습니다.
3. **모든 방향과 애니메이션에서 동일한 E 비율과 장비 구조를 유지한다.**

---

# 1. 캐릭터 핵심 정의

캐릭터의 정체는 **의지를 얻어 움직이기 시작한 고대 문자 대문자 E**입니다.

- 대문자 `E` 자체가 완전한 신체입니다.
- E 안에 인간이나 생명체가 들어 있지 않습니다.
- 별도의 머리나 얼굴이 존재하지 않습니다.
- 망토, 무기, 장갑 등을 모두 제거해도 기본 형태는 **E + 작은 팔 + 작은 다리**여야 합니다.
- Left/Right에서는 E의 세 가로획과 세로획이 실루엣으로 읽혀야 합니다.
- **Down에서는 정면 방향성이 우선**입니다. E의 세 가로획과 세로획을 본체의 구조로 뚜렷하게
  드러내되, 팔·발·망토를 정면에 맞게 배치하기 위해 외곽 실루엣의 E 비율을 고정하지 않습니다.
  룬이나 갑옷에 단순히 E를 그려 넣은 인간형 몸통으로 바꾸지는 않습니다.
- 32~64px 크기로 축소해도 즉시 E로 인식되어야 합니다.

---

# 2. 기본 외형

## 본체
- 두껍고 입체적인 대문자 `E` 형태
- 오래된 짙은 청회색 금속 + 석재가 혼합된 질감
- 약간의 균열, 마모, 깨진 모서리 허용
- 너무 부서진 폐허 느낌은 금지
- 기계 로봇보다는 **고대 마법 유물**에 가까운 분위기

## 금속 테두리
- E 외곽과 일부 모서리에 **낡은 금색/황동색 보강재** 사용
- 화려한 왕실 장식보다는 고대 유물을 보강한 금속 프레임 느낌
- 지나친 장식 금지

## 마력 / 룬
- 세로 획 중심부에 얇은 고대 룬 또는 마력 회로 사용 가능
- 기본 마력색은 **Cyan / Turquoise 계열**
- 청록색 요소가 얼굴처럼 보이면 안 됨
- 두 개의 점, 눈 모양, 바이저 모양 금지
- 마력은 본체 내부에서 흐르는 힘으로 표현

---

# 3. 머리 / 얼굴 규칙

다음 요소는 기본 신체에 존재하지 않습니다.

- 머리
- 얼굴
- 눈
- 입
- 코
- 바이저
- 얼굴 구멍
- 얼굴이 들어 있을 법한 검은 공간
- 인간 머리를 감싸는 형태의 헬멧

E의 상단 가로획은 단순히 **E의 상단 획**이며, 머리가 아닙니다.

---

# 4. 투구 장비 규칙

추후 아이템으로 투구 장착은 허용됩니다. 단, 사람 머리에 씌우는 헬멧이 아닙니다.

투구는 **E의 상단 획을 직접 덮거나 감싸는 장비**입니다.

가능한 예:
- 상단 획 위에 얹는 왕관형 방어구
- 상단 획 양쪽을 감싸는 뿔 장식
- 상단 획 위에 얹는 마법사 후드
- E 상단 전체를 감싸는 고대 금속 프레임

AI 프롬프트에는 아래 문장을 강하게 유지합니다.

> There is no head and no face.  
> The helmet is an equipment piece attached directly onto the upper horizontal stroke of the capital letter E.  
> It does not contain or cover a human head.  
> The character remains clearly a living capital letter E.

---

# 5. 팔과 다리

## 팔
- 두 팔은 E 본체에서 직접 나옵니다.
- 별도의 인간형 몸통/어깨 구조가 존재하지 않습니다.
- 팔은 짧고 굵게 설계합니다.
- 손은 작은 스프라이트에서도 보이도록 약간 큼직한 장갑 형태를 사용합니다.
- 기본 무기는 오른손에 검을 듭니다.
- 좌우 방향은 단순 미러링하지 않습니다.

## 다리
- E의 하단 획 아래에서 짧은 두 다리가 직접 나옵니다.
- 사람처럼 긴 다리는 금지합니다.
- 발은 다리보다 약간 크게 하여 이동 가독성을 확보합니다.
- 무게감 있고 안정적인 실루엣을 유지합니다.

---

# 6. 망토

- 진홍색 / 암적색 계열의 낡은 망토
- 외부는 어두운 적색, 내부와 그림자는 검붉은색
- E의 형태를 가리지 않도록 주로 뒤쪽으로 흐릅니다.
- 정면에서 E의 세 가로획이 충분히 보여야 합니다.
- 끝부분은 약간 찢어진 형태 허용
- 망토 고정 위치는 모든 프레임에서 동일하게 유지
- B 기준 Down에서는 좌우 고정 장식이 모두 보이고, Up에서는 망토가 본체 뒤를 덮는다.

---

# 7. 기본 무기

기본 무기는 작은 판타지 검입니다.

- 과도하게 큰 대검 금지
- 본체 E보다 무기가 먼저 눈에 띄면 안 됨
- 밝은 회백색 검날
- 금색/황동 손잡이
- 작은 붉은색 또는 청록색 포인트 허용

최종 게임에서는 무기 스프라이트를 본체와 분리하는 것을 권장합니다.

---

# 8. 카메라 / 시점

게임은 **Top-down Quarter View**입니다.

- 완전한 사이드뷰 아님
- 완전한 정면뷰 아님
- 위쪽 면이 조금 보이는 3/4 탑뷰
- 2D 탑다운 액션 로그라이크에 적합한 elevated three-quarter camera

AI 키워드:

> 2D top-down action roguelike, elevated three-quarter camera, not side-scrolling perspective

---

# 9. 방향 규칙 — 매우 중요

4방향 게임이지만 Left / Right를 완전한 측면으로 만들지 않습니다.

## Down
- 정면에 가장 가까운 방향
- 기준 각도: **0°**
- E의 정면 형태와 룬이 가장 잘 보임
- **상단 획·중앙 획·하단 획이 정면 구조로 보인다.** 척추/룬이 한쪽 끝으로 밀려
  측면 E처럼 읽히지 않아야 한다.
- 팔·발 두 쌍은 본체의 무게 중심 아래에 놓고, 검은 캐릭터의 오른손
  (화면 왼쪽)에 유지한다. 걷기와 공격 중에도 손이 바뀌지 않는다.

## Right
- 완전한 90° 측면이 아님
- 정면에서 오른쪽으로 약 **+80°**
- 완전 측면보다 약 10° 정면 쪽을 더 보여줌
- E 정면 면적이 약간 보여야 함
- 45° 대각선처럼 보여서는 안 됨

## Left
- 정면에서 왼쪽으로 약 **-80°**
- 완전 측면보다 약 10° 정면 쪽을 더 보여줌
- E 정면 면적이 약간 보여야 함
- 45° 대각선처럼 보여서는 안 됨

## Up
- 뒤를 바라보는 방향
- 기준 각도: **180°**
- 정면 룬은 거의 보이지 않음
- 망토와 후면 구조가 중심
- 두 발과 어깨의 후면 배치를 일치시키고, 검을 든 손의 화면상 위치를 프레임마다 유지한다.

방향 요약:

```text
             UP
            180°

LEFT                     RIGHT
-80°                     +80°

             DOWN
              0°
```

---

# 10. 좌우 반전 금지

Left와 Right를 단순 Mirror Flip 하지 않습니다.

이유:
- 검을 든 손
- 망토 고정 위치
- 룬 위치
- 금장 위치
- E 입체 구조

각 방향을 독립적으로 제작하되, 몸 비율과 회전각은 대칭적인 관계를 유지해야 합니다.
B 시안의 Left/Right는 독립 시안이므로 이 규칙을 따른다.

---

# 11. 기준 스프라이트 크기

권장 마스터 기준:

- **Sprite Cell: 64×64 px**
- 실제 캐릭터 점유 높이: 약 48~56px
- 최종적으로 32~64px 범위에서 가독성 테스트

---

# 12. 픽셀아트 스타일

스타일은 현대적인 16-bit / 32-bit 판타지 픽셀아트입니다.

필수 조건:

```text
hard pixel edges
pixel-perfect silhouette
limited palette
no anti-aliasing
no smooth vector edges
no painterly brush texture
no realistic rendering
no 3D-rendered appearance
```

컨셉아트보다 실제 스프라이트에서는 디테일을 줄여야 합니다.

작은 균열, 장식, 금장 요소를 모두 표현하려 하지 말고, 작은 크기에서도 읽히는 큰 형태를 우선합니다.

---

# 13. 기본 팔레트

**현재 제작 기준:** 방향·포즈·프레임의 형태는 B 시안을 따른다. 색은
`ArtSource/E_Animation/Archive/BeforeB/`에 보관한 A 시트의 재질별 색감을
기준으로 한다. B 원본에서 바로 추출한 색을 최종 색으로 사용하지 않는다.
`Tools/build_e_character_b.py`는 A Master 시트에서 32색 색상표를 추출해
B 출력에 직접 적용하며 투명 영역과 프레임 위치는 그대로 유지한다.

| 부위 | 권장 색 |
|---|---|
| 본체 | Dark Slate / Navy Gray |
| 깊은 그림자 | Near Black Navy |
| 금속 테두리 | Muted Gold / Bronze |
| 마력 | Cyan / Turquoise |
| 망토 | Dark Crimson |
| 망토 그림자 | Burgundy / Near Black Red |
| 검날 | Warm White / Light Gray |

가장 밝은 요소는 아래 정도로 제한합니다.
- 검날
- 청록 룬
- 금속 하이라이트

---

# 14. 제작 순서

전체 애니메이션을 한 번에 생성하지 않습니다.

## STEP 1 — Master Direction Sheet
먼저 아래 4개 기준 스프라이트만 제작합니다.

```text
E_Master_Down
E_Master_Left
E_Master_Right
E_Master_Up
```

조건:
- 동일한 크기
- 동일한 E 비율
- 동일한 팔 길이
- 동일한 다리 길이
- 동일한 금장 위치
- 동일한 망토 디자인
- 동일한 룬 위치
- 동일한 색상 팔레트
- 동일한 검 디자인

이 4방향이 확정되기 전에는 애니메이션 제작을 시작하지 않습니다.

특히 **Left / Right 각도가 정확한지 먼저 검수**합니다.

---

# 15. 애니메이션 제작 명세

| Animation | 방향 | Frames | 권장 FPS |
|---|---:|---:|---:|
| Idle | 4 | 4 | 6~8 |
| Walk | 4 | 6 | 10~12 |
| Attack | 4 | 6 | 10~14 |
| Hit | 4 | 3 | 10~12 |
| Death | 공통 또는 Down 기준 | 8 | 8~10 |
| Skill | 4 또는 공통 | 6~8 | 10~12 |

---

# 16. Idle

4프레임.

허용되는 움직임:
- 본체 1px 정도 상하 이동
- 팔 미세 이동
- 망토 끝 흔들림
- 룬 밝기 미세 변화

금지:
- E 실루엣이 늘어나거나 찌그러짐
- 프레임마다 본체 비율 변경
- 프레임마다 갑옷 무늬·룬·망토 주름을 다시 그려 표면이 끓는 현상

현재 B 이관 시트는 **방향별 기준 자세를 네 프레임에 반복**한다. 방향이 확정된 뒤
본체를 재설계하지 않는 범위에서 룬 밝기와 망토 끝 움직임을 별도로 다듬는다.

---

# 17. Walk

6프레임.

기본 구성:

```text
Contact
Down
Passing
Contact opposite
Down
Passing
```

규칙:
- 다리 움직임이 핵심
- 본체는 1~2px 정도만 상하 이동
- E 본체가 고무처럼 휘면 안 됨
- 망토는 이동 반대 방향으로 미세하게 흔들림
- 팔은 다리와 반대 위상으로 움직임
- 검은 같은 손에 고정하고, 몸통·팔·발이 항상 같은 방향으로 걷는다.

---

# 18. Attack

6프레임.

권장 구성:

```text
1. Idle
2. Anticipation
3. Wind-up
4. Impact
5. Follow-through
6. Recovery
```

핵심:
- 4번 Impact가 가장 강한 포즈
- 검 궤적은 가능하면 별도 이펙트 레이어
- 본체·팔·발이 함께 준비→타격→복귀한다. Impact의 체중 이동이 검만 휘두르는 것보다
  분명해야 한다.
- E 형태 자체가 심하게 회전하거나 찌그러지지 않음

---

# 19. Hit

3프레임.

```text
1. Normal
2. Impact
3. Recovery
```

Impact 표현:
- 1~2px 뒤로 밀림
- 작은 파편
- 짧은 밝은 플래시

---

# 20. Death

8프레임.

사람처럼 넘어지는 것보다 **고대 E 구조물이 붕괴하는 연출**을 사용합니다.

예시:

```text
1. 정상
2. 균열 확대
3. 몸 기울어짐
4. 일부 조각 낙하
5. E 구조 붕괴
6. 큰 조각 낙하
7. 잔해
8. 룬 빛 소멸
```

마지막 상태:
- 돌/금속 파편
- 쓰러진 망토
- 꺼진 마력

---

# 21. Skill

E 내부 고대 문자가 활성화되는 연출입니다.

가능한 연출:
- 지면 룬
- 고대 문자 조각 소환
- E 주변 문자 회전
- 청록색 마력 기둥
- 원형 룬 마법진

스킬 중에도 E 본체 비율은 유지합니다.
효과가 셀 상단에서 잘리지 않도록 높이를 제한합니다.

---

# 22. 장비 시스템

가능한 장비 변형:

- Unarmed
- Sword
- Shield
- Staff
- Spellbook
- Helmet
- Cape Variation

장비가 변경되어도 기본 E 본체는 변경하지 않습니다.
Spellbook은 실제 열린 책으로, Cape Variation은 길이·겹과 고정 장식으로 구분합니다.

---

# 23. 스프라이트 시트 구성

최종적으로 애니메이션별 파일 분리를 권장합니다.

```text
E_Idle.png
E_Walk.png
E_Attack.png
E_Hit.png
E_Death.png
E_Skill.png
```

각 프레임은 64×64 px.

## Idle
```text
Down   4
Left   4
Right  4
Up     4
```
→ 4×4 Grid

## Walk
```text
Down   6
Left   6
Right  6
Up     6
```
→ 6×4 Grid

## Attack
```text
Down   6
Left   6
Right  6
Up     6
```
→ 6×4 Grid

---

# 24. 배경

제작 단계:

- **Solid #FF00FF Magenta**
- 그림자와 배경 색이 섞이지 않게 함
- 환경 없음
- 바닥 없음
- UI 없음
- 텍스트 없음
- 셀 내부 가이드라인 없음

최종 단계에서 배경 제거 후 투명 PNG로 변환합니다.

---

# 25. 프레임 정렬 / Pivot

모든 프레임에서 Pivot을 동일하게 유지합니다.

권장:

```text
Bottom Center
```

즉, 두 발 사이의 바닥 접점 기준입니다.

애니메이션 중 캐릭터 중심이 불필요하게 좌우로 튀면 안 됩니다.

---

# 26. AI 생성 금지사항

아래 요소는 생성하지 않습니다.

```text
human head
human face
eyes
mouth
helmet-shaped head
visor
robot face
cute mascot face
floating head
extra limbs
long human legs
oversized weapon
rounded E silhouette
letter F
letter B
reversed E
warped E
different body proportions between frames
different armor design between frames
different cape design between frames
different rune placement between frames
random accessories
perspective changes between animation frames
45-degree diagonal Left/Right view
```

---

# 27. Master Prompt

아래 프롬프트를 컨셉아트 이미지와 함께 AI에게 제공합니다.

```text
Create production-ready pixel-art sprites based strictly on the attached E-character concept.

The character is a living ancient capital letter E. The capital E itself is the complete body. There is no separate head, no face, no eyes, no mouth, no visor, and no human inside the character.

Preserve the capital E as the actual structural stone body. In Down view, prioritize a clearly front-facing broad E structure with balanced short arms and feet; do not force the outer contour to be a side-facing E. Do not turn it into a human torso bearing a painted letter E.

The body is made of ancient dark slate-blue stone and metal, reinforced with restrained aged-gold bronze trim. Small cracks and worn edges show its age. Thin cyan magical runes flow subtly through the vertical stroke of the E. The cyan elements are magical energy, never facial features.

Two short armored arms extend directly from the E body. Two short sturdy legs extend directly from the bottom of the E. Keep the legs short and the feet relatively large for sprite readability.

A worn dark-crimson cape attaches consistently behind the E. Both shoulder fasteners are visible in Down view, while the cape covers the back in Up view.

Use a small fantasy sword in the right hand unless the frame is specifically unarmed.

Style: polished 16-bit fantasy action-roguelike pixel art, hard pixel edges, limited palette, dark outline, no anti-aliasing, no gradients, no painterly rendering.

Camera: elevated top-down quarter view.

Direction rules are extremely important:
Down = front-facing, 0 degrees.
Right = approximately +80 degrees from front, NOT a full 90-degree profile and NOT a 45-degree diagonal view.
Left = approximately -80 degrees from front, NOT a full 90-degree profile and NOT a 45-degree diagonal view.
Up = rear-facing, 180 degrees.

In Down view the vertical rune and all three horizontal E strokes are visible on the broad frontal structure. Both feet, both arms and the sword hand agree on the forward-facing perspective. During walk and attack, the sword never switches hands.

Left and Right must still reveal a small portion of the front face of the E. They should look almost sideways, but approximately 10 degrees more front-facing than a pure side view.

Do not mirror Left and Right sprites. Draw them independently while preserving identical body proportions and equipment placement.

Every sprite must use exactly the same character proportions, E geometry, cape, rune placement, gold trim, arms, legs and palette.

Use a 64x64 sprite cell. Keep the character centered on a consistent bottom-center pivot.

Solid #FF00FF magenta background. No environment, no scenery, no text, no UI.
```

---

# 28. 새 세션에서 첫 요청

컨셉아트 이미지와 이 문서를 함께 첨부한 뒤 아래와 같이 요청합니다.

```text
첨부한 컨셉아트와 제작 명세서를 기준으로 진행해줘.
전체 애니메이션을 만들지 말고 STEP 1부터 진행해.

64×64 기준 Master Direction Sheet만 만들어줘.
총 4프레임:
Down / Left / Right / Up.

특히 Left와 Right는 완전 측면이 아니라 정면에서 ±80° 방향이다.
45° 대각선처럼 만들지 말고, 완전한 90° 측면도 만들지 마.
E의 정면 면적이 아주 조금 남아 있어야 한다.

4방향 캐릭터의 E 비율, 팔/다리 길이, 망토, 룬, 금장 위치, 색상은 완전히 동일하게 유지해.
Down에서는 B 시안처럼 넓은 정면 E 구조와 대칭적인 짧은 팔·발을 우선해.
측면 E 실루엣을 정면에 그대로 세우지 마.
```

---

# 29. 권장 제작 순서 요약

```text
1. Master Direction Sheet (Down / Left / Right / Up)
2. 방향 및 비율 검수
3. Idle
4. Walk
5. Attack
6. Hit
7. Death
8. Skill
9. 장비 변형
10. 최종 배경 제거 및 Unity용 시트 정리
```

**전체 애니메이션보다 4방향 Master Sprite를 먼저 확정하는 것이 최우선입니다.**

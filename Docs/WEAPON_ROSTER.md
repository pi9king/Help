# 무기 이름 후보 목록 (기획안)

> 목표는 최종 무기 약 50종이다. 아래 75종은 **선정 전 후보 풀**이며, 등급과 공격 방식은 검토용 제안이다. 이 문서의 이름은 아직 게임 데이터나 제작식으로 등록되지 않았다.

## 분류 기준

- 주무기 자체를 가리키는 이름과, 고유한 공격 방식을 줄 수 있는 특수 무기명을 후보로 둔다.
- `FIREAXE`, `STEELAXE`, `ICEBLADE`처럼 속성을 기존 무기에 붙인 이름은 별도 무기로 세지 않는다. `STEEL`, `EMBER`, `ETHER`처럼 재료·속성·부무장에 가까운 이름도 주무기 후보에서 제외한다.
- `ICEAXE`는 예외적으로 **등산용 아이스액스**라는 도구 이름이다. 얼음 속성 `AXE`를 뜻하지 않는다.
- T1~T5는 잠정적인 획득 등급이다. 글자 수나 제작 재료 수만으로 강도를 결정하지 않는다. 첫 방 무작위 제공 풀은 최종 선정된 T1 무기로 한정한다.

## 기존 50종 제안

| 잠정 등급 | 후보 |
|---|---|
| T1 | AXE, MACE, PIKE, CANE, PIPE, NET, PEN, WIRE, STONE, SPADE |
| T2 | BLADE, SPEAR, KNIFE, SABER, LANCE, DAGGER, HAMMER, MALLET, CUDGEL, HATCHET |
| T3 | RAPIER, GLAIVE, SCYTHE, JAVELIN, HALBERD, TRIDENT, SICKLE, CHISEL, WRENCH, CHOPPER |
| T4 | MUSKET, RIFLE, CARBINE, SNIPER, LASER, TASER, ROCKET, TURRET, BLASTER, BOLTER |
| T5 | SLICER, RIPPER, SLASHER, STRIKER, CRUSHER, STINGER, BURNER, SHOCKER, FLAMER, ZAPPER |

## 추가 후보: 무기·도구 기반 12종

| 이름 | 잠정 등급 | 형태·공격 방향 |
|---|---|---|
| EPEE | T1 | 가볍고 빠른 펜싱검. RAPIER와 차별화 필요 |
| NEEDLE | T1 | 바늘·침. 짧고 빠른 연속 찌르기 |
| PESTLE | T1 | 절굿공이. 둔기형 임시 무기 |
| SKEWER | T1 | 꼬챙이. 직선 관통형 임시 무기 |
| SLEDGE | T2 | 대형 망치. 느린 광역 타격 |
| CLEAVER | T2 | 식칼·중도. 가까운 거리의 넓은 베기 |
| ICEAXE | T2 | 등산용 아이스액스. 짧은 갈고리형 공격, 얼음 속성 아님 |
| REAPER | T3 | 낫형 무기. 넓은 호를 그리는 베기 |
| POLEAXE | T3 | 장병기. 긴 사거리의 베기와 찌르기 |
| GRENADE | T3 | 투척 폭발 무기. 소모품/부무장과의 경계 검토 필요 |
| REPEATER | T3 | 연사형 발사 무기 |
| REVOLVER | T4 | 단발 화력이 높은 권총형 무기 |

## 추가 후보: 특수 무기명 13종

> 아래 이름은 무기 형태가 정해진 보통명사가 아니다. 실제 채택 시 외형과 기본 공격을 먼저 정의해야 한다.

| 이름 | 잠정 등급 | 차별화 방향(제안) |
|---|---|---|
| BREAKER | T2 | 방어를 깨는 둔기 |
| SHREDDER | T3 | 근거리 연속 타격 무기 |
| BREACHER | T3 | 직선 관통·돌파형 무기 |
| SCREAMER | T4 | 원형 충격파를 내는 무기 |
| FREEZER | T4 | 대상의 움직임을 억제하는 발사 무기. 속성 부무장과 중복 검토 |
| SEEKER | T4 | 적을 추적하는 투사체 무기 |
| BEAMER | T4 | 지속 광선을 발사하는 무기 |
| ERASER | T5 | 긴 선상 범위를 지우듯 공격하는 무기 |
| SPECTER | T5 | 장애물을 통과하는 유령형 무기 |
| REVENANT | T5 | 던졌다가 돌아오는 무기 |
| VENGEANCE | T5 | 피해를 받은 뒤 반격이 강해지는 무기 |
| REDEEMER | T5 | 긴 충전 후 강력한 일격을 내는 무기 |
| DECEIVER | T5 | 환영·분신을 이용해 공격하는 무기 |

## 제작 규칙과 선정 전 확인할 점

- 위 75종은 모두 `E`를 포함하고, **모든 E를 플레이어가 제공한다는 가정**에서는 투입 글자가 최대 6개다. 이 가정으로 E를 제외한 글자 다중집합을 비교하면 후보 간 제작식 충돌은 없다.
- 현재 `AlphabetWordRule`은 **E 하나만** 무료로 제공하며, E 재료 아이템은 없다. 따라서 E가 여러 개인 추가 후보는 현 규칙 그대로는 제작할 수 없다. 모든 E를 무료로 처리할지, 두 번째 E부터 다른 비용을 받을지는 아직 미정이다.
- `EPEE`는 모든 E가 무료라면 `P` 하나만 필요하다. 획득 등급과 제작 비용을 분리해 조정할 필요가 있다.
- 최종 50종을 고를 때 기존 목록의 임시 도구류·유사 동작 무기와 새 후보를 비교해 교체한다. 이름만 다른 동일 공격을 50종으로 세지 않는다.

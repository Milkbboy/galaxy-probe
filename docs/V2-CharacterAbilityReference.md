# 캐릭터 고유 장비 — v2.html 원본 분석

> 최종 갱신: 2026-04-20
> 근거: `docs/v2.html` (1851줄)
> 상위 문서: [Sys-Character.md](Sys-Character.md) (Unity 이식 설계) / [V2-IntegrationPlan.md](V2-IntegrationPlan.md) (총론)

## 0. 이 문서의 역할

v2.html 프로토타입의 **캐릭터 고유 장비(어빌리티 9종)** 코드를 꼼꼼히 읽어 얻은 원본 수치·동작·좌표 계산을 한자리에 정리한 **레퍼런스**. Unity 런타임 구현 시 이 문서가 **타이 브레이커**(숫자 불일치 시 이 문서가 정답)가 되도록 작성.

- 설계·인터페이스는 [Sys-Character.md](Sys-Character.md) 참조
- 수치는 전부 **v2 60fps 프레임 단위**를 병기하고 **초 단위로 환산**해 둠 (Unity는 가변 `Time.deltaTime`)

## 1. v2.html 코드 위치 맵

| 블록 | 라인 | 역할 |
|---|---|---|
| `CHARACTERS` 정의 | 270~298 | 3캐릭터 × 3어빌리티 매핑 + abilityKeys |
| `SKILLS`의 `special_*` 9종 | 362~372 | 해금 스킬 (비용·req 체인) |
| `calcStats().hasXxx` | 444~452 | 영속 해금 상태 bool |
| 입력 라우팅 (`keydown`) | 654~657 | 캐릭터별 1/2/3 → `useItem(id)` |
| 세션 상태 초기화 (`items={}`) | 729~740 | 세션 내 owned/cd/타이머 |
| `useItem(id)` (수동 6종) | 1054~1100 | 키 입력 시 분기 |
| `update()` 내 자동 발동 | 1146~1214 | 메테오·거미드론 AutoInterval |
| `tickBlackhole/Napalm/Drones` | 994~1053 | 지속형 tick 3종 |
| 화염방사기·지뢰·충격파·채굴드론 tick | 1215~1303 | update() 안 인라인 |
| `drawItemUI` | 1557~1600 | 우상단 슬롯 3개 쿨다운 UI |

## 2. 해금 체인 (SKILLS 362~372)

모든 해금 **30💎 · maxLv=1**. req는 **같은 캐릭터의 앞선 스킬**만 참조.

```
victor: special_np ─┬─ special_flamethrower    (req = special_np)
                    └─ special_mine             (req = special_np)
                        ↑ 2번·3번 둘 다 1번을 req로 하는 분기 구조

sara:   special_bh ── special_shockwave ── special_meteor     (선형)

jinus:  special_drone ── special_miningdrone ── special_spiderdrone   (선형)
```

> **주의**: Victor만 분기(2·3번 형제). Sara·Jinus는 선형 체인. 현재 `Ability_Victor_Mine.asset`의 `_requiredAbility`가 `Napalm`을 가리키는지 **검증 필요**.

## 3. 캐릭터별 어빌리티 매트릭스 — 원본 수치

### 🟧 빅터 (victor, `#f4a423` — 중장비 전문가)

| # | 키 | 어빌리티 ID | 타입 | CD (프레임 → 초) | 지속 (프레임 → 초) |
|---|---|---|---|---|---|
| 1 | `1` | `napalm` | Manual 부채꼴 지속 | 2400 → **40s** | 1200 → **20s** |
| 2 | `2` | `flamethrower` | Manual 부채꼴 지속 | 1200 → **20s** | 300 → **5s** |
| 3 | `3` | `mine` | Manual 배치형 | 600 → **10s** | — (armTimer 30f → 0.5s) |

#### 3.1 네이팜 탄 (`napalm`) — useItem: 1056 / tick: 1005~1041

- **시작점**: `CX, CY` (머신 중심, **마우스 아님**)
- **각도**: `atan2(mouseY - CY, mouseX - CX)` (머신에서 마우스 방향)
- **길이**: `Math.sqrt(W*W + H*H)` (화면 대각선) — 사실상 화면 끝까지
- **좌우 폭**: `halfW = 42` (±42)
- **판정**: `pointInRect` — 회전된 사각형의 로컬 좌표로 변환 후 `lx∈[0,len], |ly|≤halfW`
- **데미지**: `0.5` / 틱 (벌레 HP에서 차감)
- **틱 주기**: `dmgTick = 6f` (0.1s)
- **시각효과**: 틱마다 화염 파티클 (v2 1030~1038), 지속 시간 동안 유지

#### 3.2 화염방사기 (`flamethrower`) — useItem: 1087 / tick: 1215~1241

- **발동 시**: `flameActive=true; flameTimer=300`(5s) 세팅만
- **매 프레임**: `fAngle = atan2(mouseY-CY, mouseX-CX)` **재계산** (마우스 추적)
- **부채꼴 길이**: `fLen = 180`
- **부채꼴 반각**: `fSpread = 0.35 rad` (약 ±20°, 총 40°)
- **판정**: 벌레까지 거리 `fd < fLen`, 각도차 `|fda| < fSpread`
- **데미지**: `0.18 * dt` / frame (초당 ~10.8) — **지속 데미지**
- **시각효과**: `Math.random() < 0.7` 확률로 매 프레임 화염 파티클

#### 3.3 폭발 지뢰 (`mine`) — useItem: 1092 / tick: 1242~1263

- **설치 위치**: `mouseX, mouseY`
- **최대 개수**: **5개** (`mines.length < 5` 체크)
- **활성화 지연**: `armTimer = 30f` (0.5s) — 동안 감지 불가
- **감지 반경**: `bug.sz + 14` (벌레와 접촉)
- **폭발 반경**: `ws.bomb.radius * 0.5` (**폭탄 무기 강화 절반 연동**)
- **데미지**: `ws.bomb.dmg * 1.5` (벌레), `ws.bomb.dmg * 2` (보스) — **폭탄 무기 강화 연동**
- **1회 폭발 후 사라짐** (`mines.splice(mni,1)`)

> ⚠️ **폭탄 무기 미해금이면?** v2는 `ws.bomb`이 항상 존재하므로 문제없음. Unity에서는 `WeaponUpgradeManager`가 폭탄 강화 레벨 0을 반환해도 베이스 값(`WeaponData.Damage`, `WeaponData.Radius`)을 노출해야 함.

---

### 🟦 사라 (sara, `#4fc3f7` — 방어 전문가)

| # | 키 | 어빌리티 ID | 타입 | CD (프레임 → 초) | 지속 |
|---|---|---|---|---|---|
| 1 | `1` | `blackhole` | Manual 중력 지속 | 1800 → **30s** | 600 → **10s** |
| 2 | `2` | `shockwave` | Manual 확장 링 | 3000 → **50s** | 40f → **0.67s** (링 확장 수명) |
| 3 | `3` | `meteor` | **AutoInterval** | — | 화염지대 900f → **15s** |

#### 3.4 블랙홀 (`blackhole`) — useItem: 1055 / tick: 994~1000

- **위치**: `mouseX, mouseY` (발동 순간 마우스 위치에 고정)
- **지속**: `timer = maxTimer = 600f` (10s)
- **당기기 반경**: `pullR = 180`
- **당기기 힘**: `pF = 0.9` / frame (벌레 위치를 중심 방향으로 이동)
- **데미지**: **없음** (순수 CC — 벌레를 중심으로 끌어모음)
- **최소 거리 가드**: `d > 4`일 때만 적용 (중심에서 진동 방지)
- **시각효과**: 60% 확률로 파티클이 바깥→중심으로 빨려들어가는 파티클

#### 3.5 충격파 (`shockwave`) — useItem: 1061 / tick: 1264~1295

- **중심**: `CX, CY` (머신 중심, **마우스 아님**)
- **초기 반경**: 0 → **maxR = 360**까지 확장
- **확장 속도**: `spd = 14 / frame`
- **링 두께**: `thickness = 28` (참고용, 판정에는 미사용)
- **수명**: `life = maxLife = 40f` (0.67s) — maxR에 도달하거나 life 소진 시 소멸
- **판정**: `prevR <= bug_dist <= currR` (링이 지나간 위치) — **한 번만 히트** (`hitBugs{}` 플래그)
- **효과**:
  - **밀어내기**: 머신 중심에서 멀어지는 방향으로 `pushDist = 80` 순간이동
  - **슬로우**: `bug.slow = 0.5` (50%), `bug.slowTimer = 180f` (3s)
- **시각효과**: 발동 시 중심 폭발(16 파티클), 링 위에 50% 확률 파티클

#### 3.6 반중력 메테오 (`meteor`) — update: 1146~1155, 1166~1184 / tick: `tickNapalm`이 isMeteor 처리

- **타입**: AutoInterval — **10s마다 자동 발동**
- **자동 타이머**: `autoTimer += dt`, `>= 600f`일 때 발동
- **낙하 위치**: 랜덤 `x ∈ [80, W-80]`, `y ∈ [80, H-80]`
- **낙하**: `y = -60`에서 시작, `vy = 12/frame`, targetY 도달 시 착지
- **착지 시**: 원형 화염지대 생성 (`napalmZone`에 `isMeteor:true, radius:55` 플래그)
  - 지속: `timer = 900f` (**15s**)
  - 판정: **원형** (`isMeteor` 분기, `tickNapalm` 1014~1016) — 거리 `< radius + bug.sz`
  - 데미지: `0.5` / 틱, 틱 주기 `6f` (0.1s)
- **연관**: `tickNapalm`가 **네이팜·메테오 화염지대를 모두 관리** — Unity 구현 시 공통 `FireZone` 엔티티로 통합 권장

---

### 🟩 지누스 (jinus, `#51cf66` — 채굴 전문가)

| # | 키 | 어빌리티 ID | 타입 | CD (프레임 → 초) | 기타 |
|---|---|---|---|---|---|
| 1 | `1` | `drone` | Manual 배치 유닛 | 1200 → **20s** | HP 30, 최대 5 |
| 2 | `2` | `miningdrone` | Manual 자원 생성 | 1800 → **30s** | 지속 600f → **10s** |
| 3 | `3` | `spiderdrone` | **AutoInterval** | — | HP 40, 최대 3 |

#### 3.7 드론 포탑 (`drone`) — useItem: 1057 / tick: 1042~1053

- **설치 위치**: `mouseX, mouseY`
- **최대 개수**: 5
- **HP**: `30` (근접 벌레에 피해 받음 → 파괴)
- **사거리**: `fireRange = 100`
- **발사 쿨**: `fireCD = 30f` (0.5s)
- **타겟팅**: 사거리 내 **가장 가까운 벌레**
- **탄도**: `vx = cos(a) * 8`, `vy = sin(a) * 8`, 수명 `life = 60f` (1s), `fromDrone:true` 플래그
- **명중 시 데미지**: `0.8` (고정) — v2 1321에서 `fromDrone ? 0.8 : ws.gun.dmg`
- **피격**: 반경 `bug.sz + 12` 내 벌레가 있으면 `hp -= 0.5 * dt / 벌레` (여러 벌레 동시 피해 중첩)
- **시각효과**: 20% 확률로 드론 주위 파란 파티클

#### 3.8 채굴 드론 (`miningdrone`) — useItem: 1081 / tick: 1296~1303

- **설치 위치**: `mouseX, mouseY`
- **지속**: `timer = 600f` (10s)
- **채굴량**: `mineAmt += 5 / 60 * dt` (**초당 +5**) — 동시에 `sessionOre += 5/60*dt*0.5` (세션 광석도 누적)
- **보석 주기**: `gemTimer = 60f`마다 **10% 확률**로 `sessionGems += 1`
- **시각효과**: 15% 확률로 초록 파티클, 보석 획득 시 `+1💎` 텍스트 파티클 (v2 1300)
- **HP**: 없음 (지속 시간만으로 소멸)

#### 3.9 드론 거미 (`spiderdrone`) — update: 1156~1214

- **타입**: AutoInterval — **10s마다 자동 소환**
- **자동 타이머**: `autoTimer += dt`, `>= 600f` && `spiderDrones.length < 3`
- **소환 위치**: `CX ± 20, CY ± 20` (머신 근처)
- **HP**: `40` (`sd.hp -= 0.005 * dt`로 **시간 경과 자연 소멸** — 약 130s 수명)
- **사거리**: `fireRange = 120`
- **이동 속도**: `spd = 3`
- **발사 쿨**: `fireCD = 25f` (0.42s)
- **타겟팅 로직**:
  - **타겟 있음**: 타겟 방향으로 이동 + 발사 (탄속 7, `fromDrone:true`)
  - **타겟 없음**: 머신 주위 선회 — `orR = 60 + sin(lp) * 20`, `lp += 0.05` (진동 반경)
- **탄 데미지**: 드론 포탑과 동일 `0.8` 고정

---

## 4. 좌표·단위 변환 주의

> `docs/v2.html`은 **2D 캔버스 (Y = 화면 상하)**, Unity는 **탑다운 3D (Z = 화면 상하)**. sin/cos를 그대로 복붙하면 안 됨.

### 4.1 좌표 매핑

| v2 | Unity |
|---|---|
| `mouseX, mouseY` (픽셀) | `AimController.AimPoint` (XZ 월드) |
| `CX, CY` (머신 중심) | `MachineController.transform.position` |
| `atan2(mouseY-CY, mouseX-CX)` | `atan2(aim.z - machine.z, aim.x - machine.x)` |
| 화면상 "위쪽" 방향 | `Vector3.forward` (❌ `Vector3.up` 금지) |
| `Math.hypot(dx,dy)` | `Vector3.Distance` (XZ만 추출) 또는 `sqrMagnitude` 비교 |

### 4.2 단위 환산 — **핵심 결정 사항**

v2의 반경/사거리/길이는 **픽셀 단위**. Unity 유닛으로 바로 쓸 수 없음.

| v2 수치 예시 | 맥락 |
|---|---|
| `pullR = 180` (블랙홀) | 화면 `W≈1280, H≈720` 기준 약 25% |
| `fireRange = 100` (드론 포탑) | 화면 대각선의 ~7% |
| `maxR = 360` (충격파) | 화면 대각선의 ~25% |
| `halfW = 42` (네이팜) | 매우 좁음 |

**Unity 변환 기준**:
- 현재 머신 스케일(`MachineController` 콜라이더 반경), 벌레 스폰 반경을 측정해 **v2 픽셀 1 ≈ Unity N 유닛**으로 상수화 필요
- 상수 확정 후 SO의 `Range`/`Angle` 필드에 **환산값을 저장** (런타임은 환산 없이 그대로 사용)
- 현재 SO(`Ability_Victor_Napalm.asset` 등)의 `_range: 42`는 **픽셀 값이 그대로 들어가 있음** → 재검토 필요

### 4.3 프레임→초 변환

- v2의 모든 `dt`는 1프레임=1 기준 (60fps 가정), `cd -= dt` 형태
- Unity SO는 **이미 초 단위**로 설계됨 (`_cooldownSec`, `_durationSec`) — 값 자체는 위 표의 "초" 컬럼 사용
- **데미지는 주의**: v2의 `0.18 / frame`(화염방사기)은 초당 10.8 → Unity에서 `Damage * Time.deltaTime` 패턴이면 `Damage = 10.8`로 저장. **틱 주기 방식(`dmgTick = 6f`)은 초 단위 타이머**로 변환 (`0.1s`마다 데미지)

---

## 5. Unity 이식 시 필요한 기존 API/훅

구현 전에 존재 여부를 확인해야 할 기존 시스템 지점:

| 필요 기능 | 이유 | 확인 대상 |
|---|---|---|
| `BugBase.ApplySlow(factor, durationSec)` | 충격파·(향후 네이팜 등) | Saw가 `slowTimer` 쓰니 기반 있을 가능성 큼 |
| `BugBase.Knockback(Vector3 dir, float dist)` | 충격파 80유닛 밀어내기 | 없을 가능성 — 신설 후보 |
| 드론/지뢰가 발사하는 총알 | 기존 `WeaponBase` 총알 풀 재사용? 아니면 `AbilityBullet` 신설? | `Scripts/Weapon/`의 총알 프리펩 구조 |
| 폭탄 강화 값 조회 | 지뢰가 `ws.bomb.dmg/radius`를 씀 | `WeaponUpgradeManager.GetEffective("bomb", …)` 류 |
| 화염지대 `FireZone` 엔티티 | 네이팜·메테오 공용 | 없음 — 신규 `FireZone` 컴포넌트 신설 필요 |
| 머신 중심 위치 | 네이팜·충격파·거미드론 선회의 중심 | `MachineController.transform.position` (OK) |
| 마우스 월드 좌표 | 블랙홀·지뢰·드론 배치, 네이팜 각도 | `AimController.AimPoint` (OK) |
| `MachineController.AddMined(float)` | 채굴 드론이 채굴량 증가 | `MachineController.cs` 확인 필요 |
| 세션 보석 증가 | 채굴 드론의 10% 확률 보석 | `SessionResult` 또는 `PlayerData`에 세션 누적값 |

---

## 6. 인게임 슬롯 UI (참고 — `drawItemUI` 1557~1600)

- 좌상단: 캐릭터 이름 (캐릭터 컬러, 폰트 11px)
- 우상단: **소유한 어빌리티만** 세로로 쌓음 (90×34 박스)
- 박스 내용:
  - 라벨 `[1] 블랙홀` / `[자동] 메테오`
  - 쿨다운 바 `cdPct = 1 - cd/cdMax` (꽉 찬 상태 = 사용가능)
- 메테오·거미드론은 `key='자동'`으로 표기, CD는 `autoTimer` 역수로 표시

Unity 포팅 시 `Sys-Character.md §7`의 `SlotUI` 구조 그대로 사용.

---

## 7. 현재 Unity SO 상태 — 원본과의 갭

`Assets/_Game/Data/Abilities/*.asset` 9개가 생성되어 있음. 원본과 비교할 때 **검증 필요** 항목:

| 필드 | v2 기준 | 현재 SO (Victor_Napalm 샘플) | 메모 |
|---|---|---|---|
| `_cooldownSec` | 40 | 40 | ✅ |
| `_durationSec` | 20 | 20 | ✅ |
| `_damage` | 0.5 | 0.5 | ✅ |
| `_range` | 42 (픽셀, halfW) | 42 | ⚠️ **픽셀 단위** — Unity 유닛 환산 필요 |
| `_angle` | — (네이팜은 부채꼴 아님, 회전된 직사각형) | 0 | ✅ (네이팜 한정) |
| `_maxInstances` | 1 | 1 | ✅ |
| `_requiredAbility` | null (최초) | fileID 0 | ✅ |

**확인 필요**: 나머지 8개 SO도 §3의 수치와 동일한지. 특히:
- 화염방사기 `_angle = 0.35` (rad) 인가
- 지뢰 `_maxInstances = 5`
- 메테오·거미드론의 `_trigger = AutoInterval`, `_autoIntervalSec = 10`
- Sara 충격파·메테오 / Jinus 채굴드론·거미드론의 `_requiredAbility` 체인

---

## 8. 원본 수치 요약표 (한눈에)

| 캐릭터 | 키 | 어빌리티 | CD | 지속 | 데미지 | 범위 | 특이사항 |
|---|---|---|---|---|---|---|---|
| Victor | 1 | 네이팜 | 40s | 20s | 0.5/tick(0.1s) | halfW 42, len 대각선 | 회전된 직사각형, 머신 중심 시작 |
| Victor | 2 | 화염방사기 | 20s | 5s | 10.8/s | fLen 180, ±0.35rad | 매 프레임 마우스 추적 |
| Victor | 3 | 지뢰 | 10s | — | bomb×1.5 | bomb_radius×0.5 | 5개, 0.5s arm, 폭탄 강화 연동 |
| Sara | 1 | 블랙홀 | 30s | 10s | 0 (CC) | 당기기 반경 180 | 힘 0.9/f |
| Sara | 2 | 충격파 | 50s | 0.67s | 0 (CC) | maxR 360 | push 80, 슬로우 50%×3s |
| Sara | 3 | 메테오 | 자동 10s | 화염 15s | 0.5/tick | 원형 r=55 | 랜덤 낙하, 화염지대 |
| Jinus | 1 | 드론 포탑 | 20s | HP 30 | 0.8/탄 | fireRange 100 | 5대, 30f 발사쿨 |
| Jinus | 2 | 채굴드론 | 30s | 10s | — | — | +5 ore/s, 10% gem/s |
| Jinus | 3 | 거미드론 | 자동 10s | HP 40 | 0.8/탄 | fireRange 120 | 3기, 이동 spd 3 |

---

## 9. 참고 문서

- [Sys-Character.md](Sys-Character.md) — Unity 이식 설계 (SO 구조 · IAbilityRunner · 슬롯 UI)
- [V2-IntegrationPlan.md](V2-IntegrationPlan.md) — 총론 · 작업 우선순위 §4 · 좌표계 §5
- [Sys-Weapon.md](Sys-Weapon.md) — 지뢰가 참조하는 폭탄 강화
- `docs/v2.html` 1054~1303 — 어빌리티 tick/use 구현 원본

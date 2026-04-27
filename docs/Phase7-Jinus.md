# Phase 7 — 지누스(Jinus) 어빌리티 구현 계획

> 작성일: 2026-04-21
> **상태**: ✅ **구현 완료 / 머지됨** (2026-04-21) — 현행 시스템: [Sys-Character.md §5.7~5.9](Sys-Character.md)
> 범위: 드론 포탑 / 채굴 드론 / 드론 거미 (지누스 3종)
> 근거 프로토타입: `docs/v2.html` — 270~298(CHARACTERS), 653~657(입력), 704~708(세션 상태), 733~739(items), 1042~1053(tickDrones), 1057~1060·1081~1086(useItem drone/miningdrone), 1136~1164(cd tick + 스파이더 자동 소환), 1185~1214(스파이더 tick), 1296~1303(채굴 드론 tick), 1321(`fromDrone` 탄 데미지 0.8)
> 상위 문서: [Sys-Character.md](Sys-Character.md) §5.7~5.9
> 형제 문서: [Phase5-Victor.md](Phase5-Victor.md) / [Phase6-Sara.md](Phase6-Sara.md)
> v2 레퍼런스: [V2-CharacterAbilityReference.md](V2-CharacterAbilityReference.md) §3.7~3.9

## 0. 목표

`Sys-Character.md` 스펙의 지누스 3종 어빌리티를 Game 씬에 3D 이펙트로 구현. 빅터·사라 페이즈에서 만든 공통 인프라(`AbilityContext`, `IAbilityRunner`, `AbilitySlotController`, `AbilityRangeDecal`, `AbilityDecalMeshBuilder`)는 그대로 재사용.

본 페이즈는 **빅터·사라와 성격이 다름** — 지속 장판·링 확산 같은 즉시 이펙트가 아니라, **HP 가 있는 배치 유닛 + 자체 AI(타겟팅·탄 발사·이동)** 가 핵심. MonoBehaviour 신규 3종(`DroneInstance`, `MiningDroneInstance`, `SpiderDroneInstance`) + 경량 탄(`DroneBullet`) 이 추가됨.

---

## 1. v2.html 원본 동작 분석

### 1-1. 드론 포탑 — `useItem('drone')` / `tickDrones`

| 위치 | 역할 |
|---|---|
| v2.html:1057~1060 | 발동 — `drones.push({x:mouseX, y:mouseY, hp:30, mhp:30, fireCD:0, angle:0, fireRange:100})` + `cd=1200` (20s) |
| v2.html:1042~1053 | `tickDrones(dt)` — 사거리 내 최근접 벌레 추적, 30f(0.5s) 쿨마다 발사, 접촉 벌레에 의한 피해 |
| v2.html:1321 | 탄 명중 — `bugs[j].hp -= b.fromDrone ? 0.8 : ws.gun.dmg` (드론 탄 데미지는 **0.8 고정**) |

```js
// 1042~1053
for (each drone dr) {
  // 타겟팅 — 사거리 내 최근접
  var target=null, minD=dr.fireRange;
  for (each bug) { var d=hypot(bug-dr); if(d<minD){minD=d; target=bug;} }
  if (target) dr.angle = atan2(target.y-dr.y, target.x-dr.x);

  // 발사
  dr.fireCD -= dt;
  if (target && dr.fireCD<=0) {
    dr.fireCD = 30;                     // 0.5s 재장전
    var a = dr.angle + (rand-0.5)*0.1;  // 작은 산포
    bullets.push({ x:dr.x, y:dr.y,
      vx:cos(a)*8, vy:sin(a)*8,          // 탄속 8/frame = 480 pix/s
      life:60, fromDrone:true });        // 수명 1s
  }

  // 접촉 피해 — 벌레가 dr 반경(sz+12) 안에 있으면 HP 감소
  for (each bug) {
    if (hypot(bug-dr) < bug.sz + 12) dr.hp -= 0.5 * dt;
  }
  if (dr.hp<=0) { burst; splice; }
}
```

**핵심**: 드론은 **HP 30짜리 자동 포탑**. 벌레가 근접하면 피해 누적 → 파괴. 사거리 내 벌레만 조준, 벌레가 없으면 대기. 최대 5기.

### 1-2. 채굴 드론 — `useItem('miningdrone')` / 채굴드론 tick

| 위치 | 역할 |
|---|---|
| v2.html:1081~1086 | 발동 — `miningDrones.push({x:mouseX, y:mouseY, timer:600, gemTimer:60})` + `cd=1800` (30s) |
| v2.html:1296~1303 | tick — `mineAmt += 5/60*dt`, `sessionOre += 5/60*dt*0.5`, `gemTimer` 주기로 10% 보석 획득 |

```js
// 1296~1303
for (each md) {
  md.timer -= dt; md.gemTimer -= dt;
  mineAmt    += 5/60 * dt;              // 초당 +5 채굴
  sessionOre += 5/60 * dt * 0.5;        // (연속값 → Unity 에선 이벤트 1회/초 대신 정수 누적 방식)

  if (md.gemTimer <= 0) {
    md.gemTimer = 60;                   // 1s마다 보석 롤
    if (Math.random() < 0.1) sessionGems += 1;  // 10% 확률
  }
  if (md.timer <= 0) splice;             // 10초 수명
}
```

**핵심**: **수명 10초짜리 채굴 버프 유닛**. HP 없음(시간만으로 소멸). 매 프레임 채굴량 증가 + 매 1초마다 10% 확률 보석 드랍. **최대 1기(v2는 제한 없음이지만 Unity SO `_maxInstances:1` 로 제한 — 적정)**.

### 1-3. 드론 거미 — `update()` 자동 소환 + tick

| 위치 | 역할 |
|---|---|
| v2.html:1144 | cd tick — `items.spiderdrone.cd -= dt` (수동 발동 없음) |
| v2.html:1157~1164 | 자동 소환 — `autoTimer >= 600 && spiderDrones.length < 3` → `spiderDrones.push({...})` |
| v2.html:1185~1214 | tick — 타겟 있음/없음 분기 이동 + 발사 + HP 자연감쇠 |

```js
// 1157~1164 - 자동 소환
items.spiderdrone.autoTimer += dt;
if (items.spiderdrone.autoTimer >= 600 && spiderDrones.length < 3) {
  items.spiderdrone.autoTimer = 0;
  spiderDrones.push({
    x:CX + (rand-0.5)*40, y:CY + (rand-0.5)*40,   // 머신 ±20 근처
    target:null, hp:40, mhp:40, fireCD:0, angle:0,
    lp:0, fireRange:120, spd:3,
  });
}

// 1185~1214 - tick
for (each sd) {
  sd.lp += 0.05;
  // 타겟팅 — 사거리 120 내 최근접
  var tgt=null, minD=sd.fireRange;
  for (each bug) { if (hypot<minD) { minD=..; tgt=bug; } }

  if (tgt !== null) {
    // 타겟 방향으로 이동 + 발사
    sd.angle = atan2(bug.y-sd.y, bug.x-sd.x);
    sd.x += cos(sd.angle) * sd.spd;   // 속도 3/frame = 180 pix/s
    sd.y += sin(sd.angle) * sd.spd;
    sd.fireCD -= dt;
    if (sd.fireCD <= 0) {
      sd.fireCD = 25;                  // ~0.42s 재장전
      bullets.push({ x:sd.x, y:sd.y,
        vx:cos(sd.angle)*7, vy:sin(sd.angle)*7,
        life:60, fromDrone:true });
    }
  } else {
    // 타겟 없음 — 머신 주위 선회
    var orA = atan2(sd.y-CY, sd.x-CX) + 0.03;
    var orR = 60 + sin(sd.lp) * 20;    // 진동 반경 40~80
    sd.x += (CX + cos(orA)*orR - sd.x) * 0.05;  // lerp 5%
    sd.y += (CY + sin(orA)*orR - sd.y) * 0.05;
  }

  sd.hp -= 0.005 * dt;                 // 시간 경과 자연 감쇠 (~130s 수명)
  if (sd.hp<=0) splice;
}
```

**핵심**: **10초마다 자동 소환되는 추격형 드론**. 최대 3기. 타겟 없으면 머신 주위 선회. HP는 접촉 피해가 아니라 **시간 자연감쇠** — 전투 중이 아니어도 서서히 사라짐.

### 1-4. 공통 — `fromDrone` 탄 시스템

v2는 기관총 탄 풀(`bullets[]`)을 **드론 포탑·드론 거미가 공유**. `fromDrone:true` 플래그로 데미지만 분기:

```js
// 1321 - bullets 충돌
bugs[j].hp -= (b.fromDrone ? 0.8 : ws.gun.dmg);  // 드론 탄은 0.8 고정
```

Unity 에서는 `MachineGunBullet` 을 재사용하면 `MachineGunData` 의존이 끌려나옴 → **경량 `DroneBullet` MonoBehaviour 신설** (본 계획 Step 2).

### 1-5. 원본 수치 요약

| 어빌리티 | CD | 지속 | 최대 수 | HP | 사거리 | 탄속 | 재장전 | 탄 데미지 | 기타 |
|---|---|---|---|---|---|---|---|---|---|
| 드론 포탑 | 20s (1200f) | ∞ (피격으로만 파괴) | 5 | 30 | 100pix | 8/f (**480 pix/s**) | 0.5s (30f) | 0.8 | 접촉 피해 `0.5*dt / 벌레` 누적 |
| 채굴 드론 | 30s (1800f) | 10s (600f) | 1 (Unity) | — | — | — | — | — | 초당 +5 채굴, 1s마다 10% 보석 |
| 드론 거미 | — (Auto 10s) | ~130s (HP 자연감쇠) | 3 | 40 | 120pix | 7/f (**420 pix/s**) | ~0.42s (25f) | 0.8 | 이동속도 3/f = 180 pix/s |

---

## 2. Unity 현행 상태 (스캐폴딩 검증 결과)

| 항목 | 상태 | 비고 |
|---|---|---|
| `AbilityData` SO 3종 | ✅ | `Assets/_Game/Data/Abilities/Ability_Jinus_{Drone,MiningDrone,SpiderDrone}.asset` 존재 |
| `AbilityType.Drone/MiningDrone/SpiderDrone` 열거 | ✅ | `AbilityData.cs:23~25` |
| Runner 3종 | ❌ | 신규 |
| `AbilitySlotController.CreateRunner` 분기 | ⚠️ | 169~173행 `default: return null` 상태 — 3줄 추가 필요 |
| `AbilityContext` | ✅ | 필드 그대로. **단 `MachineController` 참조 필요** — 채굴 드론이 채굴 증가·보석 누적을 호출 |
| Polygon Arsenal VFX | ✅ | 드론 본체/탄/연출 후보군 있음(§7 참조) |
| `MachineGunBullet` 재사용 가능 여부 | ❌ | `MachineGunData` 의존 — 신규 경량 `DroneBullet` 필요 |
| 채굴 증가 공개 API (`MachineController.AddBonusMining`) | ❌ | 신규 필요. 현재 `_miningAccumulator` 는 private, 외부 호출 수단 없음 |
| 보석 누적 공개 API | ⚠️ | `GameEvents.OnGemCollected?.Invoke(1)` 발행 가능(MachineController 가 구독 중) — 이걸 재사용하면 신규 API 불필요 |
| `SimpleBug.IDamageable.TakeDamage(float)` | ✅ | 드론 탄·사라 어빌리티가 이미 사용 중 |
| 드론 포탑 접촉 피해 — 벌레→드론 HP 감소 | ❌ | 판정 로직 신규 (드론 측 OverlapSphere → HP 감소) |
| 바닥 데칼(드론 사거리 원) | ✅ | `AbilityRangeDecal` + `BuildCircle(range)` 재사용 |

### 2-1. `AbilityContext` 확장 이슈

지누스 채굴 드론은 `MachineController` 의 채굴 증가 경로가 필요. 두 가지 방안:

**A. `AbilityContext` 에 `MachineController` 필드 추가**
- 장점: Runner 에서 `_ctx.Machine.AddBonusMining(x)` 로 바로 호출, 단순.
- 단점: 기존 빅터·사라 Runner 가 안 쓰는 의존성이 Context 에 추가됨. Context 비대화.

**B. `MachineController` → 이벤트 발행**
- 별도 이벤트 없이 기존 `GameEvents.OnMiningGained?.Invoke(mined)` / `OnGemCollected?.Invoke(1)` 재사용.
- Runner 는 이벤트를 쏘기만 하면 됨. MachineController 가 구독해 자기 `_totalMined` / `_sessionGemsCollected` 증가.
- **문제**: 현재 `OnMiningGained` 은 "획득한 채굴량"을 알리는 알림용이지 **채굴량 적립 요청 채널이 아님**. MachineController 가 구독하고 있지 않음.

**결정 → A 로 진행**. 빅터 Phase 에서 `BombWeapon` 을 Context 에 넣은 선례가 있음 — 지누스 채굴 드론에만 필요한 의존이라 동일 패턴이 맞음. Context 에 `MachineController Machine` 필드 1개 추가.

### 2-2. SO 값 검증 이슈

| SO | 필드 | 현재 값 | v2 원본 환산 | 조치 |
|---|---|---|---|---|
| `Ability_Jinus_Drone` | `_themeColor` | `(0.31, 0.76, 0.97)` = **#4fc3f7 (사라 블루)** | #51cf66 (지누스 그린) | **수정 필요** |
| `Ability_Jinus_Drone` | `_range` | 10 | 100pix ÷ 10 = 10 | ✅ |
| `Ability_Jinus_Drone` | `_damage` | 0.8 | 0.8 (드론 탄 고정) | ✅ |
| `Ability_Jinus_Drone` | `_maxInstances` | 5 | 5 | ✅ |
| `Ability_Jinus_MiningDrone` | `_themeColor` | `(0.32, 0.81, 0.4)` ≈ #51cf66 | #51cf66 | ✅ |
| `Ability_Jinus_MiningDrone` | `_cooldownSec/_durationSec` | 30 / 10 | 30 / 10 | ✅ |
| `Ability_Jinus_SpiderDrone` | `_themeColor` | `(0.53, 0.87, 1)` ≈ 하늘색 | #51cf66 로 통일? | 🟡 검토 — §5 참조 |
| `Ability_Jinus_SpiderDrone` | `_range` | 12 | 120pix ÷ 10 = 12 | ✅ |
| `Ability_Jinus_SpiderDrone` | `_autoIntervalSec` | 10 | 10s | ✅ |
| `Ability_Jinus_SpiderDrone` | `_maxInstances` | 3 | 3 | ✅ |

**HUD 색 일관성 결정**: 지누스 3슬롯 테마색을 모두 **#51cf66 (지누스 그린)** 으로 통일 vs 어빌리티별 색 유지. 빅터(주황 고정)·사라(하늘색 고정)는 캐릭터 컬러로 통일되어 있으니 지누스도 통일이 자연스러움 → Step 1 에서 수정.

---

## 3. 탑뷰 좌표계 매핑 (CLAUDE.md 준수)

| v2 2D | Unity 탑뷰 3D |
|---|---|
| `(mouseX, mouseY)` 클릭 위치 | `_ctx.Aim.AimPosition` (Y=0) |
| `(CX, CY)` 머신 중심 | `_ctx.MachineTransform.position` (Y=0 강제) |
| `drone.x/y` 드론 위치 | `Transform.position.x/z` (Y=0 지면 고정) |
| `bullets.push({vx,vy})` | `DroneBullet.Initialize(dir XZ, speed)` — `dir.y=0` 필수 |
| `atan2(dy,dx)` (드론→벌레) | `Quaternion.LookRotation(toBug.normalized, Vector3.up)` — **yaw 회전만** |
| 스파이더 선회 `orA,orR` | `Vector3 orbit = machinePos + new Vector3(cos(a)*r, 0, sin(a)*r)` (sin 은 Z축) |

- [x] 드론·탄·거미 모두 `pos.y = 0f` 강제
- [x] 방향은 XZ 평면에서 계산 (`.y = 0f; .Normalize()`)
- [x] 드론 본체 VFX 가 XY 세로 authoring 이면 wrapper GO `Quaternion.Euler(90,0,0)` 으로 눕힘 (빅터 지뢰·사라 메테오 선례)
- [x] 산포 각도 `(rand-0.5)*0.1` 은 yaw 라디안이므로 `Quaternion.Euler(0, randDeg, 0) * forward` 로 적용

---

## 4. 구현 순서 (Step-by-Step)

> 각 Step 은 독립 커밋 가능. Step 1·2 가 인프라, Step 3~5 는 Runner 3종 순차.

### Step 1 — SO 값 / Context / Controller 분기 수정

**1-1. `AbilitySlotController.CreateRunner` 스위치 분기 3줄 추가**

`Scripts/Ability/AbilitySlotController.cs:169~173`:

```csharp
case AbilityType.Drone:       return new DroneRunner();
case AbilityType.MiningDrone: return new MiningDroneRunner();
case AbilityType.SpiderDrone: return new SpiderDroneRunner();
```

**1-2. `AbilityContext` 에 `MachineController Machine` 필드 추가**

`Scripts/Ability/AbilityContext.cs` + `AbilitySlotController.BuildContext()`:

```csharp
// AbilityContext.cs
public MachineController Machine;

// AbilitySlotController.BuildContext()
Machine = FindAnyObjectByType<MachineController>(),
```

채굴 드론만 사용. null 허용 (BombWeapon 선례).

**1-3. `MachineController.AddBonusMining(float amount)` 공개 API**

```csharp
/// <summary>
/// 외부 소스(채굴 드론 등)에서 채굴량을 주입. _miningAccumulator 에 더해져 정수 단위로 _totalMined 에 누적.
/// sessionOre 도 함께 보정 (v2 `mineAmt += X, sessionOre += X*0.5` 패턴).
/// </summary>
public void AddBonusMining(float amount)
{
    if (!_isSessionActive || amount <= 0f) return;
    _miningAccumulator += amount;
    // Update() 의 Mining() 이 FloorToInt 처리·이벤트 발행 담당 — 여기선 누적만
}
```

**1-4. SO 테마색 통일** — Drone·SpiderDrone 을 `#51cf66` 로 변경 (yaml 직접 수정 또는 인스펙터).

### Step 2 — 공통 인프라: `DroneBullet` + `DroneVfxBinder` 에디터

**2-1. `Scripts/Ability/Runners/DroneBullet.cs` (신규 MonoBehaviour)**

- 경량 투사체. `MachineGunData` 의존 없음.
- `Initialize(Vector3 direction, float speed, float damage, float lifetime, LayerMask bugLayer)` 로 상태 주입.
- Update 매 프레임 `transform.position += vel * dt`.
- `Physics.OverlapSphereNonAlloc(pos, 0.3f, buffer, bugLayer)` 로 벌레 첫 히트 → `IDamageable.TakeDamage(damage)` → Destroy.
- 수명 만료 시 그냥 소멸 (폭발 VFX 없음).

**2-2. `Scripts/Editor/JinusAbilityVfxBinder.cs` (신규 에디터)**

메뉴: `Tools/Drill-Corp/3. 게임 초기 설정/9. 지누스 어빌리티 VFX 바인딩`

3개 AbilityData `_vfxPrefab` 슬롯에 Polygon Arsenal 프리펩 자동 할당:

| SO | 할당 프리펩 (VFX) | 비고 |
|---|---|---|
| `Ability_Jinus_Drone` | `Prefabs/Interactive/Powerups/Orbs/Big/GlowPowerupBigGreen.prefab` | 드론 본체 — 고정 터렛 느낌 |
| `Ability_Jinus_MiningDrone` | `Prefabs/Misc/CrystalGrowthGreen.prefab` | 채굴 드론 — 크리스탈 상승 |
| `Ability_Jinus_SpiderDrone` | `Prefabs/Interactive/Powerups/Orbs/SparkleOrb/SparkleOrbGreen.prefab` | 거미 드론 — 동적 orb |

탄환 프리펩은 별도 상수(에셋 폴더 직접 Load) 로 Runner 에서 읽음.

### Step 3 — DroneRunner + DroneInstance

**`Scripts/Ability/Runners/DroneRunner.cs`**

- `TryUse(aim)` → `_drones.Count < MaxInstances` 검사 → `DroneInstance.Spawn(data, ctx, aim)` → `_drones.Add(drone)`, `_cooldown = data.CooldownSec`.
- `Tick(dt)` → 파괴된 드론 리스트에서 제거.

**`Scripts/Ability/Runners/DroneInstance.cs` (MonoBehaviour, 빅터 `MineInstance` 선례)**

- `Initialize(AbilityData data, LayerMask bugLayer, DroneBulletAssets assets)` → 체력·사거리·쿨·탄 프리펩 주입.
- `Update()`:
  1. 사거리 내 최근접 벌레 탐색 (`Physics.OverlapSphereNonAlloc`).
  2. 타겟 있으면 `transform.rotation = LookRotation(toBug, Vector3.up)` (지면 yaw).
  3. `_fireCooldown -= dt`. 0이하 + 타겟 있으면 탄 스폰 + `_fireCooldown = data.FireDelay` (SO `_cooldownSec` 은 **리스폰 쿨**이지 발사 쿨이 아님 → Runner 내부 상수 `DroneFireDelay = 0.5f`).
  4. 접촉 피해: 반경 `ContactRadius = 1.2f` OverlapSphere → 벌레 수만큼 `_hp -= ContactDpsPerBug * dt` (v2 0.5/dt/벌레 포팅).
  5. `_hp <= 0` → `burst VFX` → `Destroy(gameObject)` + `OnDestroyed?.Invoke()` (Runner 가 리스트 제거).
- HP 바: 작은 WorldSpaceCanvas 로 머리 위 표시 (선택 — 폴리싱 범위).

**사거리 데칼**: 소환 시 자식 `AbilityRangeDecal` + `BuildCircle(fireRange)` — 옅은 초록 링, 평생 유지.

**탄 발사**:
```csharp
var dir = (target.position - transform.position); dir.y = 0f; dir.Normalize();
float spreadRad = (Random.value - 0.5f) * 0.1f;
dir = Quaternion.Euler(0, spreadRad * Mathf.Rad2Deg, 0) * dir;
var bullet = Instantiate(_bulletPrefab, transform.position, Quaternion.LookRotation(dir, Vector3.up));
bullet.GetComponent<DroneBullet>().Initialize(dir, BulletSpeed, data.Damage, BulletLifetime, bugLayer);
```

**Runner 상수**:
- `DroneFireDelay = 0.5f` (v2 30f/60)
- `BulletSpeed = 48f` (v2 8/f × 60 ÷ 10 = 48 u/s)
- `BulletLifetime = 1f` (v2 60f/60)
- `ContactRadius = 1.2f` (v2 12pix/10)
- `ContactDpsPerBug = 0.5f` (v2 0.5/dt/벌레)
- `DroneMaxHp = 30f`
- `Spread = 0.1f rad` (~5.7°)

### Step 4 — MiningDroneRunner + MiningDroneInstance

**`MiningDroneRunner.cs`**

- `TryUse(aim)` → 1기 제한이므로 `_active != null` 면 무시 (쿨다운 별도). 소환 + `_cooldown = data.CooldownSec`.
- `Tick(dt)` → 수명 만료 시 정리.

**`MiningDroneInstance.cs`**

- `Initialize(AbilityData data, MachineController machine)` → `_life = data.DurationSec`, `_gemRollTimer = 1f`.
- `Update()`:
  1. `_life -= dt`. <=0 이면 Destroy.
  2. `machine.AddBonusMining(MiningRatePerSec * dt)` — v2 `mineAmt += 5/60*dt`, 초당 +5.
  3. `_gemRollTimer -= dt`. <=0 이면 `_gemRollTimer += 1f`, `Random.value < GemChance` 시 `GameEvents.OnGemCollected?.Invoke(1)` 발행 → MachineController 가 자동 누적.
  4. 시각 연출: 초당 1~2회 `SparkleGreen` 파티클 짧게 Instantiate, 보석 획득 시 `+1💎` FloatingText (선택).
- 본체 VFX: `CrystalGrowthGreen` 을 자식 VFX 로 Instantiate (수명 = DurationSec 과 동기화).

**Runner 상수**:
- `MiningRatePerSec = 5f` (v2 원본 그대로)
- `GemChance = 0.1f` (10%)
- `GemRollInterval = 1f`

### Step 5 — SpiderDroneRunner + SpiderDroneInstance

**`SpiderDroneRunner.cs`**

- **AutoInterval 트리거** — 키 입력이 아니라 `Tick(dt)` 안에서 자동 발동.
- `_autoTimer += dt`. `>= data.AutoIntervalSec` && `_spiders.Count < data.MaxInstances` → `SpawnSpider()` + `_autoTimer = 0f`.
- 소환 위치: `machine.position + new Vector3((rand-0.5)*SpawnSpread, 0, (rand-0.5)*SpawnSpread)` (`SpawnSpread = 4f`).
- `TryUse` 는 no-op (false 반환 — 키 입력 무시).

**`SpiderDroneInstance.cs`**

- `Initialize(AbilityData data, MachineController machine, LayerMask bugLayer, DroneBulletAssets assets)`
- `Update()`:
  1. `_lp += OrbitLpSpeed * dt` (v2 0.05/f × 60 = 3/s).
  2. 사거리 내 최근접 벌레 탐색 (`data.Range`).
  3. **타겟 있음**: yaw 회전, `transform.position += forward * MoveSpeed * dt`, 발사 쿨 감소·발사.
  4. **타겟 없음**: 머신 주위 선회 — `orA = atan2(pos.z-m.z, pos.x-m.x) + OrbitTurnRate*dt`, `orR = OrbitBase + sin(_lp)*OrbitAmp`, 목표점 `m + (cos*orR, 0, sin*orR)`, `pos = Lerp(pos, target, OrbitLerp*dt)`.
  5. HP 감쇠: `_hp -= HpDecayPerSec * dt`. <=0 이면 burst + Destroy.
- 탄: `DroneBullet` 재사용 (속도 `BulletSpeed = 42f`, 데미지 `data.Damage = 0.8`).
- 본체 VFX: `SparkleOrbGreen` 을 자식 VFX 로 상주. yaw 회전은 VFX 가 아닌 root Transform 에 적용.

**Runner 상수**:
- `MoveSpeed = 18f` (v2 3/f × 60 ÷ 10 = 18 u/s)
- `SpiderFireDelay = 0.42f` (v2 25f/60)
- `BulletSpeed = 42f` (v2 7/f × 60 ÷ 10)
- `BulletLifetime = 1f`
- `SpiderMaxHp = 40f`
- `HpDecayPerSec = 0.3f` (v2 0.005/dt × 60 = 0.3/s) — 수명 ~133초
- `OrbitLpSpeed = 3f` (v2 0.05/f × 60)
- `OrbitBase = 6f`, `OrbitAmp = 2f` (v2 60/10, 20/10)
- `OrbitTurnRate = 1.8f` (v2 0.03/f × 60)
- `OrbitLerp = 3f` (v2 0.05/f × 60 — `Lerp(pos, target, OrbitLerp*dt)` 로 적분)
- `SpawnSpread = 4f` (v2 ±20pix ÷ 10 × 2)

### Step 6 — Game 씬 수동 검증 체크리스트

- [ ] 빅터→지누스 캐릭터 전환 후 키 1 → 드론 5기 배치 제한 확인, 20초 쿨 확인
- [ ] 드론이 사거리 내 벌레 자동 조준·발사 확인 (산포 ±5° 눈으로 확인)
- [ ] 벌레가 드론 근접 시 드론 HP 감소 → 파괴 burst
- [ ] 키 2 → 채굴 드론 소환, 10초간 상단 HUD 광석 숫자 급증 확인
- [ ] 채굴 드론 1기 제한(두 번 눌러도 추가 안 됨) + 30초 쿨 확인
- [ ] 10초 지나면 채굴 드론 자연 소멸
- [ ] 보석 + 1 효과 — 10초 × 10%/s ≈ 평균 1개 기대값 (체감용)
- [ ] 세션 시작 ~10초 후 거미 드론 자동 1기 소환 → 20초, 30초에 2·3기
- [ ] 거미 드론 3기 상한 확인 (10초마다 소환 체크 통과하지만 상한 도달 시 소환 안 됨)
- [ ] 거미 드론 타겟 있음 → 접근 + 발사, 타겟 없음 → 머신 주위 선회
- [ ] 모든 지누스 어빌리티가 HUD 슬롯에 초록 테두리로 표시 확인

---

## 5. 결정 사항 & 리스크

### 5-1. HUD 테마색 통일 (해결됨)

Step 1-4 에서 Drone/SpiderDrone 테마색을 `#51cf66` 로 변경 — 지누스 정체성 유지.

### 5-2. 드론 포탑 HP 표시

v2 에는 드론 HP 바가 없음(파티클 burst 로만 파괴 표현). Unity 에서는:

- **A. HP 바 생략** — v2 와 동일, 파괴 시 burst 만.
- **B. `BugHpBar` 를 재활용한 작은 HP 바** — 드론 전략적 배치감 향상.

**결정**: MVP 는 A (burst only). B 는 Step 6 이후 폴리싱 단계에서 검토.

### 5-3. 거미 드론 "자연감쇠" HP 체감

v2 원본 `hp -= 0.005*dt` → 초당 0.3 감소, HP 40 → 130초 수명. 체감 너무 김.

- 플레이테스트 후 `HpDecayPerSec` 를 1.0~2.0 으로 올리는 튜닝 고려 (30~40초 수명).
- SO 에 `_damage` 필드가 비어있음 — 재활용 해 `_damage` 를 **HP 감쇠 속도**로 쓰는 것도 옵션이나, 의미 혼동 위험 → Runner 상수로 유지.

### 5-4. 채굴량 표기 동기화

`MachineController.AddBonusMining` 은 `_miningAccumulator` 에 누적만 하고, 정수화·이벤트 발행은 기존 `Mining()` 에 의존. 매 프레임 `FloorToInt` 처리 → HUD 가 적절히 갱신됨 (검증 대상).

대안: `AddBonusMining` 에서 내부적으로 `Mining()` 스타일 정수화를 돌려도 되지만, **하나의 진실소스**(매 프레임 `Update().Mining()`) 원칙 유지 권장.

### 5-5. Polygon Arsenal VFX 부피감

`GlowPowerupBigGreen` / `SparkleOrbGreen` 은 원래 "집기" 픽업 연출 — 드론 본체로 쓰기엔 너무 작거나 반짝임 과다일 수 있음. 실측 후:

- `_vfxScale` 을 2~3 배로 올려 본체감 주기
- 필요시 `ForceLoopAllParticleSystems` (네이팜 선례) 로 무한 루프 강제
- 마지막 수단: 드론 전용 프리펩을 `MinePrefabCreator` 처럼 **에디터 스크립트로 합성**(body + ring + particle) — Phase 7 내 Polishing 섹션

### 5-6. 탄 프리펩 로드 전략

`BulletGreen.prefab` 은 SO 가 아니라 **Runner 가 런타임 로드**해야 함. 방법:

- **A. `Resources.Load<GameObject>("DroneBullet")`** — CLAUDE.md 가 Resources 최소화 권고 → 비권장
- **B. Static `[RuntimeInitializeOnLoadMethod]` + `AssetDatabase.LoadAssetAtPath`** — 에디터 전용
- **C. `DroneBulletAssetsSO` ScriptableObject 하나 만들고 AbilityContext 에 참조 전달** — 인프라 비대
- **D. `AbilityData._vfxPrefab` 을 드론 본체로, 탄은 드론 인스턴스에 SerializeField 로 직접 참조** — 에디터 1회 바인딩 필요, 구조 단순

**결정**: **D 채택**. `DroneInstance.prefab` / `SpiderDroneInstance.prefab` 을 `MinePrefabCreator` 스타일 에디터로 생성하면서 `_bulletPrefab` 슬롯에 `BulletGreen` 자동 바인딩. Phase 5 `MinePrefabCreator` 와 동형 패턴.

---

## 6. Phase 7 전체 체크리스트

**Step 1 — 인프라 수정**
- [ ] `AbilitySlotController.CreateRunner` 스위치 3줄 추가
- [ ] `AbilityContext.Machine` 필드 + `BuildContext()` 주입
- [ ] `MachineController.AddBonusMining(float)` public 메소드
- [ ] `Ability_Jinus_Drone._themeColor` → #51cf66
- [ ] `Ability_Jinus_SpiderDrone._themeColor` → #51cf66

**Step 2 — 공통 부품**
- [ ] `DroneBullet.cs` MonoBehaviour
- [ ] `Scripts/Editor/DronePrefabCreator.cs` — `Assets/_Game/Prefabs/Abilities/DroneInstance.prefab` + `SpiderDroneInstance.prefab` + `MiningDroneInstance.prefab` 생성 & `AbilityData._vfxPrefab` 자동 바인딩. 탄 프리펩(`BulletGreen`) 은 각 Instance 프리펩에 SerializeField 로 삽입.

**Step 3 — 드론 포탑**
- [ ] `DroneInstance.cs` MonoBehaviour (타겟팅·발사·접촉피해·HP)
- [ ] `DroneRunner.cs` IAbilityRunner (스폰·리스트 관리·쿨)
- [ ] Game 씬 검증

**Step 4 — 채굴 드론**
- [ ] `MiningDroneInstance.cs` MonoBehaviour (수명·채굴훅·보석롤)
- [ ] `MiningDroneRunner.cs` IAbilityRunner
- [ ] `AddBonusMining` 호출 검증 — HUD 광석 숫자 튀는지 확인

**Step 5 — 드론 거미**
- [ ] `SpiderDroneInstance.cs` MonoBehaviour (타겟팅·추격·선회·자연감쇠)
- [ ] `SpiderDroneRunner.cs` IAbilityRunner (AutoInterval 자동 소환)
- [ ] Game 씬 검증 (10s 간격 확인)

**Step 6 — 문서화 & 커밋**
- [ ] 본 계획서 §10 as-built 로그 추가
- [ ] `docs/Overview-Changelog.md` Phase 7 엔트리
- [ ] 커밋 — 구조 단위(Step 1+2 / Step 3 / Step 4 / Step 5) 로 분할

---

## 7. 레퍼런스 — Polygon Arsenal VFX 후보

| 용도 | 후보 프리펩 | 경로 |
|---|---|---|
| 드론 포탑 본체 | `GlowPowerupBigGreen` | `Prefabs/Interactive/Powerups/Orbs/Big/` |
| 드론 포탑 탄 | `BulletGreen` | `Prefabs/Combat/Missiles/Sci-Fi/Bullet/` |
| 채굴 드론 본체 | `CrystalGrowthGreen` | `Prefabs/Misc/` |
| 채굴 드론 수확 파티클 | `SparkleGreen` | `Prefabs/Interactive/Sparkle/` |
| 보석 획득 팝업 | `StoneEmeraldExplosion` | `Prefabs/Interactive/Mining/` |
| 드론 거미 본체 | `SparkleOrbGreen` | `Prefabs/Interactive/Powerups/Orbs/SparkleOrb/` |
| 드론 거미 탄 | `BulletGreen` 또는 `PlasmaMissileGreen` | `Prefabs/Combat/Missiles/Sci-Fi/{Bullet,Plasma}/` |
| 드론 파괴 burst | `SmokeSwirlExplosionBlue` / `StormExplosionVariant` | `Prefabs/Misc/` |

---

## 8. 작업 의존 그래프

```
Step 1 (인프라 수정)
   │
   ├─▶ Step 2 (DroneBullet + PrefabCreator)
   │       │
   │       ├─▶ Step 3 (DroneRunner + DroneInstance)
   │       └─▶ Step 5 (SpiderDroneRunner + SpiderDroneInstance)
   │
   └─▶ Step 4 (MiningDroneRunner + MiningDroneInstance)
           (Step 2 불필요 — 탄·Instance 프리펩 없이 순수 VFX + 훅만)

Step 3/4/5 완료 후 ──▶ Step 6 (문서·커밋)
```

Step 4 는 Step 2 와 독립 — 병렬 진행 가능. Step 3·5 는 Step 2 의 `DroneBullet` + PrefabCreator 출력물에 의존.

---

## 9. 원본 프로토타입 참조 요약

| v2.html 라인 | 내용 |
|---|---|
| 270~298 | CHARACTERS — jinus skills/abilities/keys |
| 362~372 | SKILLS — special_drone/miningdrone/spiderdrone 체인 |
| 653~657 | keydown — jinus → 1/2/3 = drone/miningdrone/spiderdrone |
| 704~708 | 세션 전역 — `drones, spiderDrones, miningDrones, meteors` |
| 733~739 | `items = { drone, miningdrone, spiderdrone, ... }` 상태 |
| 1042~1053 | `tickDrones(dt)` — 드론 포탑 AI |
| 1057~1060 | `useItem('drone')` — 수동 배치 |
| 1081~1086 | `useItem('miningdrone')` — 수동 배치 |
| 1136~1144 | cd 감소 tick — 모든 아이템 공통 |
| 1156~1164 | 스파이더 자동 소환 루프 |
| 1185~1214 | 스파이더 tick — 타겟/선회/HP 감쇠 |
| 1296~1303 | 채굴 드론 tick — 채굴훅 + 보석롤 |
| 1321~1322 | `fromDrone` 탄 데미지 0.8 고정 |

---

## 10. As-Built 기록

> 2026-04-21 구현 완료. 본 계획 대비 실제 구현의 주요 차이와 추가 발견 사항.

### 10.1 인프라 수정 (Step 1)

- `AbilityContext.Machine` 필드 추가 — 채굴 드론이 `machine.AddBonusMining` 호출.
- `MachineController.AddBonusMining(float)` public — `_miningAccumulator` 누적만, 정수화·이벤트 발행은 기존 `Mining()` 위임.
- `AbilitySlotController.CreateRunner` 3줄 추가 (Drone/MiningDrone/SpiderDrone).
- `Ability_Jinus_Drone._themeColor` / `Ability_Jinus_SpiderDrone._themeColor` → `#51cf66` 통일.
- Runner 스텁 3종 먼저 생성 후 Step 3~5 에서 로직 채움 — 스위치 분기와 Runner 구현 동시 진행의 circular dependency 회피.

### 10.2 공통 부품 (Step 2)

- **`DroneBullet.cs`** — 경량 투사체. `Initialize(dir, speed, damage, lifetime, bugLayer)` 시그니처. XZ 직진 + OverlapSphereNonAlloc(0.3 반경) 첫 명중에 `IDamageable.TakeDamage` → Destroy. 드론 포탑만 사용 (거미는 근접 전환으로 미사용).
- **`DronePrefabCreator.cs`** — 메뉴 `Tools/Drill-Corp/3. 게임 초기 설정/10. 지누스 드론 프리펩 생성`. 4개 프리펩(DroneBullet 래퍼 + 3 Instance) 자동 생성 + AbilityData `_vfxPrefab` 자동 바인딩. 초기엔 body 에 `Quaternion.Euler(90,0,0)` 적용했으나 SparkleOrbGreen 내부 자식의 `-90°X` local rotation 충돌로 orientation 문제 발생 → identity 로 변경 (아래 §10.6 참조).
- Instance 스텁 3종 MonoBehaviour — SerializeField 선언 + Initialize 시그니처만 열어두고 Step 3~5 에서 로직 채움.

### 10.3 드론 포탑 (Step 3)

- **`DroneInstance.cs` / `DroneRunner.cs`** 계획대로 구현. 타겟팅·yaw 회전·±5° 산포 발사·접촉피해·HP·사거리 링·파괴 이벤트.
- 사거리 링 색은 지누스 테마 초록 `(0.32, 0.81, 0.4)`, `baseAlpha=0.18` 옅게.
- 발사 산포는 계획의 ±0.1 rad 을 그대로 유지 (≈ ±5.7°).

### 10.4 채굴 드론 (Step 4)

- **`MiningDroneInstance.cs` / `MiningDroneRunner.cs`** 계획대로 구현.
- **추가**: v2.html:1643~1646 의 **원호 타이머 + "Ns" 라벨** 포팅 — 계획 §4 Step 4 에서 누락됐던 항목. `MiningDroneTimer3D.cs` 신규 (LineRenderer 원호 + TextMeshPro 3D 라벨).
  - 원호는 12시 시작 시계방향 수축, `SetProgress(life/duration)` 매 프레임.
  - `LineAlignment.TransformZ` 초기 구현에서 탑뷰 원호가 vertical plane 으로 서버림 → `LineAlignment.View` 로 수정 (카메라 방향 billboard).
- **추가**: **런타임 디스크 body** — `CrystalGrowthGreen` prefab body 만으로는 "실체 없음" 느낌. `BuildRuntimeBody()` 에서 얇은 Cylinder(반경 0.8, 높이 0.12) 추가. 계획 §5-5 의 "body + ring + particle 합성" 아이디어를 런타임 primitive 로 대체.

### 10.5 드론 포탑·거미 HP 바 (Step 4 연장)

- **`Hp3DBar.cs`** 신규 — primitive Cube 2개(배경+Fill) 3D HP 바. `Create(target, offset, size)` + `SetHealth/SetColors`. Fill 가로 축소 왼쪽 정렬, 30% 이하 빨강 전환.
- 드론 포탑 `_hpBarSize = (2, 0.22, 0.3)` / 거미 드론 `_hpBarSize = (1.2, 0.15, 0.2)` — 바디 크기 대비 튜닝.
- 벌레 접촉 피해 / 거미 자연 감쇠 시 `SetHealth(_currentHp / _maxHp)` 실시간 갱신. 계획 §5-2 A(burst only) 대안을 MVP 에 포함.

### 10.6 body orientation 이슈 (Step 5 디버깅)

SparkleOrbGreen/CrystalGrowthGreen/GlowPowerupBigGreen 모두 Unity Y-up 씬을 가정하고 authoring — 내부 자식들의 local rotation 이 복잡. `DronePrefabCreator.BodyRotation` 을 identity 로 변경해도 SparkleOrbGreen 의 `-90°X` 회전된 mesh renderer 자식은 탑뷰에서 뒤집혀 보임.

**최종 해결**: 거미 드론에 **런타임 primitive Sphere** body 추가 (`_bodySize=0.9`, 지누스 초록). 회전 대칭이라 orientation 이슈 없음. SparkleOrbGreen 은 프리펩에 남아 파티클 데코만 담당. 채굴 드론 디스크와 동일 패턴.

### 10.7 드론 거미 근접 공격 전환 (Step 5 큰 설계 변경)

계획 §4 Step 5 는 v2 원본 따라 `DroneBullet` 원거리 탄을 쓰도록 설계됐으나, **v2 실제 플레이 체감이 "달라붙어 뜯는 근접"** 임이 확인됨 (거미가 타겟으로 공격적으로 접근해 0거리 발사 → 탄 궤적이 안 보임).

**변경 내역**:
- 탄 로직 전부 제거 (`_bulletPrefab`, `_fireCooldown`, `FireBulletAt` 등).
- `_meleeRadius=1.2`, `_meleeDps=3` 접촉 피해로 전환. 멜리 범위 도달 시 이동 정지 (통과 방지), 범위 내 벌레 모두에게 `TakeDamage(dps * dt)`.
- `DronePrefabCreator.BuildSpiderDronePrefab` 시그니처에서 `bulletPrefab` 인자 제거.

### 10.8 드론 거미 타겟 예약 (Step 5 AI 개선)

v2 원본은 단순 "최근접 벌레" 선택이라 3기가 같은 벌레에 몰려 "3마리가 1마리처럼" 움직임. 플레이 테스트에서 즉각 드러남.

**해결**:
- `static HashSet<int> _claimedTargets` — 모든 SpiderDroneInstance 공유 예약 세트.
- `FindBestBugExcludingClaimed` — 미예약 벌레 중 최근접. 전부 예약돼 있으면 fallback 으로 최근접 (starvation 방지).
- 타겟 락 — 사거리 이탈/사망 전까지 재선택 안 함. thrashing 방지.
- `SetTarget(null)` 을 `DestroySelf`/`OnDestroy` 에서 호출해 예약 누수 방지.

### 10.9 편의성: deprecated API 경고 일괄 수정

작업 중 발견된 CS0618 경고 6건 함께 정리:
- `Object.FindFirstObjectByType<T>()` → `FindAnyObjectByType<T>()` (AbilityHudSetupEditor, TopBarHudSetupEditor ×2, ResultPanelSetupEditor)
- `TextureImporter.spritesheet` 참조 제거 (AbilitySlotBorderCreator — 실제 사용 안 됐음)
- **`PolygonArsenalFbxFixer.cs`** 신규 메뉴 — Polygon Arsenal 153개 FBX 의 `MaterialLocation.External → InPrefab` 일괄 변경. deprecated enum 값 직접 참조 회피를 위해 `!= InPrefab` 조건으로 검사.

### 10.10 남은 튜닝 포인트

- 거미 멜리 DPS 3 — 벌레 HP 대비 체감 조정 필요.
- 거미 HP 감쇠 0.3/sec (133초 수명) — 너무 긺. 플레이 후 1.0~2.0 (30~40초) 권장.
- 채굴 드론 디스크 크기 / 거미 구체 크기 인스펙터 튜닝 여지.
- SparkleOrbGreen 파티클이 탑뷰에서 방향이 살짝 어색하지만 런타임 구체 body 가 지배적이라 방치. 완벽한 정렬이 필요하면 프리펩 body 를 `GlowPowerupBigGreen` 등 orientation-agnostic 프리펩으로 교체 고려.

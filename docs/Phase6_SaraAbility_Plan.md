# Phase 6 — 사라(Sara) 어빌리티 구현 계획

> 작성일: 2026-04-21
> 범위: 블랙홀 / 충격파 / 반중력 메테오 (사라 3종)
> 근거 프로토타입: `docs/v2.html` (994~1000, 1055, 1061~1080, 1146~1184, 1265~1295)
> 상위 문서: [CharacterAbilitySystem.md](CharacterAbilitySystem.md) §5.4~5.6
> 형제 문서: [Phase5_VictorAbility_Plan.md](Phase5_VictorAbility_Plan.md)

## 0. 목표

`CharacterAbilitySystem.md` 스펙의 사라 3종 어빌리티를 Game 씬에 3D 이펙트로 구현한다. 빅터 Phase 에서 만든 공통 인프라(`AbilityContext`, `IAbilityRunner`, `AbilitySlotController`, `AbilityRangeDecal`, `AbilityDecalMeshBuilder`) 는 그대로 재사용하고, 본 페이즈는 **사라 Runner 3종 + 메테오 낙하체 MonoBehaviour + VFX 바인딩** 이 전부.

---

## 1. v2.html 원본 동작 분석

### 1-1. 블랙홀 — `useItem('blackhole')`

| 위치 | 역할 |
|---|---|
| v2.html:1055 | 발동 — `items.blackhole.active=true; x=mouseX; y=mouseY; timer=600; cd=1800` |
| v2.html:994~1000 | `tickBlackhole(dt)` — 반경 180 내 벌레를 중심으로 흡인 |

```js
// 발동
items.blackhole.active = true;
items.blackhole.x = mouseX; items.blackhole.y = mouseY;
items.blackhole.timer = 600;    // 10초 지속 (60fps)
items.blackhole.cd = 1800;       // 30초 쿨다운

// tick 매 프레임
var pullR = 180, pF = 0.9;       // 반경 180 픽셀, 흡인력 0.9/frame
for (each bug within pullR && d > 4) {
  bug.x += dx/d * pF * dt;       // 중심으로 끌어당김 (dt는 프레임 수)
  bug.y += dy/d * pF * dt;
}
// 데미지 없음 — 순수 CC
```

**핵심**: **마우스 위치에 10초간 지속되는 중력 존**. 반경 내 벌레를 중심으로 끌어당김. 동시 1개 (`!bh.active` 가드).

**단위 변환 (v2 → Unity, ÷10 비율은 Victor Phase 와 일치)**:
- 반경 180 → `_range: 18`
- 흡인력 0.9/frame = 54/sec → Unity 유닛 스케일 적용 시 `5.4 unit/sec`
- `_durationSec: 10`, `_cooldownSec: 30` (v2 동일)

### 1-2. 충격파 — `useItem('shockwave')`

| 위치 | 역할 |
|---|---|
| v2.html:1061~1080 | 발동 — `shockwaves.push({...})`, 중심 폭발 파티클 |
| v2.html:1265~1295 | tick — 링 확장 + 히트 판정 + 파티클 |

```js
shockwaves.push({
  r: 0, maxR: 360,                // 반경 0→360 확장
  spd: 14,                         // 14/frame 확장속도
  thickness: 28,
  hitBugs: {},                     // 이미 맞은 인덱스 (재히트 금지)
  pushDist: 80,                    // 밀쳐내기 거리
  life: 40, maxLife: 40            // ~0.67초 수명
});
items.shockwave.cd = 3000;         // 50초 쿨다운

// tick 매 프레임
prevR = sw.r;
sw.r += sw.spd * dt;
sw.life -= dt;
if (sw.r > sw.maxR || sw.life <= 0) splice;
for (each bug) {
  if (sw.hitBugs[i]) continue;
  d = distance(bug, CX,CY);
  if (d >= prevR - sz && d <= sw.r + sz) {       // 링이 지금 통과 중
    sw.hitBugs[i] = true;
    bug.x += dx/d * 80;                          // 밖으로 80 순간 변위
    bug.y += dy/d * 80;
    bug.slow = 0.5; bug.slowTimer = 180;          // 3초 50% 슬로우
  }
}
```

**핵심**: **머신 중심에서 확장하는 링 — 1회성(0.67s)**. 링이 지나갈 때 한 벌레는 1회만 히트 → 밖으로 순간 푸시 + 3초 슬로우. 데미지 없음 (CC).

**단위 변환**:
| 속성 | v2 값 | Unity 값 |
|---|---|---|
| maxR | 360 | `_range: 36` ✅ (현재 SO와 일치) |
| spd (확장) | 14/frame = 840/sec | `84 unit/sec` (÷10) — Runner 상수 |
| thickness | 28 | `2.8` (Runner 상수, 판정 margin) |
| pushDist | 80 | `8` (Runner 상수) |
| life | 40 frame ≈ 0.67s | 자연 종료 기준 `maxR/spd = 36/84 ≈ 0.43s`. `_durationSec = 0` 유지 |
| slow | 0.5 × 3s | `BugController.ApplySlow(0.5f, 3f)` |

### 1-3. 반중력 메테오 — 자동 발동

| 위치 | 역할 |
|---|---|
| v2.html:1146~1155 | autoTimer — 10초마다 랜덤 위치에 meteor push |
| v2.html:1167~1184 | 낙하 + 착지 → 메테오 화염 지대 생성 |
| v2.html:1013~1019 | `tickNapalm` 안에서 `isMeteor` 분기(원형 판정) |

```js
// AutoInterval — 매 프레임
items.meteor.autoTimer += dt;
if (autoTimer >= 600) {                       // 10초
  autoTimer = 0;
  var mtx = 80 + rand*(W-160);                 // 화면 가장자리 제외 랜덤
  var mty = 80 + rand*(H-160);
  meteors.push({ x:mtx, y:-60, vy:12,         // 화면 위 -60에서 등장
    targetX:mtx, targetY:mty, sz:18, trail:[] });
}

// 낙하 매 프레임
mt.trail.push({x,y});
mt.y += mt.vy;                                 // 12/frame = 720/sec
if (mt.y >= mt.targetY) {
  // 착지 → 원형 화염 지대 15초
  napalmZones.push({
    x:mt.x, y:mt.targetY, angle:0, len:0, halfW:55,
    timer:900, maxTimer:900, dmgTick:0,
    isMeteor:true, radius:55                    // ← 원형 판정 플래그
  });
}

// tickNapalm 내부 (v2.html:1013~1019)
var inZone = nz.isMeteor
  ? hypot(bug.x - nz.x, bug.y - nz.y) < (nz.radius||55) + bug.sz
  : pointInRect(bug.x, bug.y, nz);
if (inZone) bug.hp -= 0.5;                     // 0.1초 틱당 0.5 dmg (네이팜과 동일)
```

**핵심**: **AutoInterval(10초 주기)**, 사용자 입력 없음. 하늘에서 운석 낙하 애니메이션 → 착지 시 **원형 화염 지대 15초** 유지. 화염 지대는 네이팜의 `isMeteor` 원형 분기로 처리 — Unity 포팅 시엔 독립된 `MeteorFireZone` 으로 분리 권장(판정이 OverlapSphere로 단순).

**단위 변환**:
| 속성 | v2 값 | Unity 값 |
|---|---|---|
| autoTimer | 600 frame = 10s | `_autoIntervalSec: 10` ✅ |
| 낙하 시작 y | `-60` (화면 위 60 밖) | `FallHeight = 12` (targetPos.y + 12) — Runner 상수 |
| 낙하속도 vy | 12/frame = 720/sec | `FallSpeed = 7.2f` — Runner 상수 |
| 화염 반경 | 55 | `_range: 5.5` ✅ |
| 지대 지속 | 900 frame = 15s | `_durationSec: 15` ✅ |
| 틱당 데미지 | 0.5 / 6frame | `_damage: 0.5` ✅ (0.1s 틱당) |
| 착지 위치 | 화면 80px 제외 랜덤 | 머신 기준 반경 3~20 랜덤 — Runner 상수 |

---

## 2. Unity 현황 (이미 있는 것 / 없는 것)

| 레이어 | 상태 | 경로 / 비고 |
|---|---|---|
| `AbilityData` SO 3종 (Sara_BlackHole/Shockwave/Meteor) | ✅ | `Assets/_Game/Data/Abilities/Ability_Sara_*.asset` — `_range`, `_durationSec`, `_cooldownSec`, `_autoIntervalSec`, `_damage` 기본 세팅됨 |
| `AbilityContext` / `IAbilityRunner` / `AbilitySlotController` | ✅ | 빅터 Phase 완성 |
| `AbilityRangeDecal` + `AbilityDecalMeshBuilder` (`BuildCircle`/`BuildRing`/`BuildSector`/`BuildRectangle`) | ✅ | 전부 재사용 |
| `BugController.ApplySlow(strength, durationSec)` | ✅ | `Scripts/Bug/BugController.cs:925` — 충격파 슬로우에 그대로 사용 |
| `BugController` 위치 강제 API | ❌ | 블랙홀 흡인·충격파 푸시 — `transform.position += delta` 직접 조작 (BugController가 매 프레임 머신 방향 재계산하므로 자연 복귀) |
| `AbilitySlotController.CreateRunner` switch 분기 BlackHole/Shockwave/Meteor | ⚠️ default 반환 중 | 3줄 추가 필요 |
| Sara Runner 3종 | ❌ | 신규 |
| MeteorInstance (MonoBehaviour 낙하체) + MeteorProjectile 프리펩 | ❌ | 신규 |

---

## 3. 탑뷰 좌표계 매핑 (CLAUDE.md 준수)

| v2 2D | Unity 탑뷰 3D |
|---|---|
| `(mouseX, mouseY)` 마우스 위치 | `_ctx.Aim.AimPosition` (Y=0 지면 평면) |
| `(CX, CY)` 머신 중심 | `_ctx.MachineTransform.position` (Y=0 강제) |
| "화면 위쪽" (메테오 낙하 시작) | **`+Y` 월드 위쪽** (카메라 방향 반대) — `targetPos + Vector3.up * FallHeight` |
| 2D 거리 `Math.hypot(dx,dy)` | XZ 평면 거리 — `to.y = 0f; to.magnitude` |
| 원형 범위 | `Physics.OverlapSphereNonAlloc` (Y=0 평면 + 충분한 높이 margin) |
| 링 히트 판정 | XZ 거리만 계산 |

- [x] `aim.AimPosition` Y=0 그대로 사용
- [x] 방향 계산 시 `dir.y = 0f; dir.Normalize()`
- [x] 바닥 데칼은 Mesh 기반(`BuildCircle`/`BuildRing`) → 자체가 XZ 평면이라 회전 보정 불필요
- [x] Polygon Arsenal 프리펩은 XY 세로 authoring 가능성 → wrapper GO에 `Quaternion.Euler(90,0,0)` 실측 후 적용

---

## 4. 구현 순서 (Step-by-Step)

### Step 1 — AbilitySlotController switch 분기 3줄 추가

`Scripts/Ability/AbilitySlotController.cs:159` 의 `CreateRunner`:

```csharp
case AbilityType.BlackHole: return new BlackHoleRunner();
case AbilityType.Shockwave: return new ShockwaveRunner();
case AbilityType.Meteor:    return new MeteorRunner();
```

### Step 2 — BlackHoleRunner

**동작**: `TryUse` 시 마우스 위치(`aimPoint`)에 `BlackHoleZone` 1개 생성 (동시 1개, 기존 있으면 무시 = v2 `!bh.active` 가드 포팅).

**구조**: `NapalmRunner` 와 동일 패턴 — inner class `BlackHoleZone` 에 `Tick/Dispose` 책임 분리.

**판정**:
```csharp
int hits = Physics.OverlapSphereNonAlloc(center, pullRadius, buffer, bugLayer);
for (int i=0; i<hits; i++) {
    var col = buffer[i];
    Vector3 to = center - col.transform.position; to.y = 0f;
    float d = to.magnitude;
    if (d > InnerCutoff) {                              // 너무 가까우면 흡인 중단
        col.transform.position += to.normalized * (PullSpeedPerSec * dt);
    }
}
```

**Runner 상수**:
- `PullSpeedPerSec = 5.4f` (v2 0.9/frame × 60 ÷ 10)
- `InnerCutoff = 0.4f` (4 픽셀 ÷ 10 — 너무 가까우면 딱 붙지 않음)

**VFX**:
- **본체 소용돌이**: `Interactive/Portal/Vortex/VortexPortalPurple.prefab` — wrapper GO 에 부착, 실측 후 90°X 회전 필요시 적용. 스케일 = `Range / basePrefabRadius`
- **바닥 데칼**: `BuildCircle(Range)` 메시 + 보라 반투명 (`_tint = new Color(0.6f, 0.43f, 1f, 1f)`)

**정리**: `_life <= 0` → Dispose 에서 VortexPortal Destroy + 데칼 `Dispose()` (페이드아웃).

### Step 3 — ShockwaveRunner

**동작**: `TryUse` 시 머신 중심에서 `ShockwaveRing` 1개 생성. 순간 발동 — 0.43초 후 자연 종료.

**링 확장 + 히트 판정**:
```csharp
float prevR = _radius;
_radius += ExpandSpeed * dt;
if (_radius > _maxRadius) return false;              // Dispose

int hits = Physics.OverlapSphereNonAlloc(center, _radius + HitMargin, buffer, bugLayer);
for (int i=0; i<hits; i++) {
    var col = buffer[i];
    if (_hitBugs.Contains(col)) continue;
    Vector3 to = col.transform.position - center; to.y = 0f;
    float d = to.magnitude;
    if (d >= prevR - HitMargin && d <= _radius + HitMargin) {
        _hitBugs.Add(col);
        if (d > 0.01f) {
            Vector3 dir = to / d;
            col.transform.position += dir * PushDistance;         // 순간 변위
        }
        if (col.TryGetComponent<BugController>(out var bc))
            bc.ApplySlow(SlowStrength, SlowDurationSec);
    }
}
```

**Runner 상수**:
- `ExpandSpeed = 84f` (v2 14/frame × 60 ÷ 10)
- `PushDistance = 8f` (v2 80 ÷ 10)
- `HitMargin = 0.3f` (v2 thickness 28 의 감안치)
- `SlowStrength = 0.5f` / `SlowDurationSec = 3f`

**VFX**:
- **중심 발동**: `Combat/Nova/FrostNova.prefab` — 1회 Instantiate (자체 AutoDestroy 있으면 그대로, 없으면 `Destroy(vfx, 1.5f)`). 청록 테마 #4fc3f7 과 일치
- **확장 링**: `BuildRing(inner, outer)` 메시 **1회만 생성** (반경 `_maxRadius` 기준). 매 프레임 `transform.localScale.x = localScale.z = (_radius / _maxRadius)` 로 스케일 업. 데칼 알파는 `AbilityRangeDecal` 기본 페이드 활용(`SetTint` 청록)

> **Mesh 재빌드 금지** — 매 프레임 `BuildRing` 호출은 GC / mesh upload 비용 커서 반드시 localScale 조절 방식.

### Step 4 — MeteorRunner + MeteorInstance

#### 4-1. MeteorRunner (AutoInterval)

- `TryUse` 는 항상 `false` 반환 (AutoInterval 타입)
- `Tick(dt)`:
  ```csharp
  _autoTimer += dt;
  if (_autoTimer >= _data.AutoIntervalSec) {
      _autoTimer = 0f;
      SpawnMeteor();
  }
  ```
- `CooldownNormalized`:
  ```csharp
  // HUD가 "1 - norm" 을 채우는 방향이라 다음 발동까지 남은 비율을 norm 으로 돌려줌
  return _data.AutoIntervalSec > 0f
      ? Mathf.Clamp01(1f - _autoTimer / _data.AutoIntervalSec)
      : 0f;
  ```

- **첫 발동**: `_autoTimer = 0` 으로 시작 → 10초 대기 후 첫 발 (v2 원본 유지. 사용자 결정 #3)

- **랜덤 착지 위치** (머신 중심):
  ```csharp
  float a = Random.value * Mathf.PI * 2f;
  float r = Random.Range(SpawnRadiusMin, SpawnRadiusMax);    // 3 ~ 20
  Vector3 target = machinePos + new Vector3(Mathf.Sin(a)*r, 0f, Mathf.Cos(a)*r);
  ```

#### 4-2. MeteorInstance (MonoBehaviour, 낙하체)

```csharp
public void Initialize(
    Vector3 targetPos,
    float fireZoneRadius, float fireZoneDuration, float fireZoneTickDamage,
    GameObject impactVfxPrefab, GameObject fireZoneVfxPrefab,
    LayerMask bugLayer, Transform vfxParent);
```

- 초기 위치: `targetPos + Vector3.up * FallHeight` (FallHeight = 12)
- `Update`: `transform.position += Vector3.down * FallSpeed * dt` (FallSpeed = 7.2)
- `transform.position.y <= targetPos.y` 도달 시:
  1. 폭발 VFX 스폰 (`GrenadeExplosionRed` — 지뢰와 동일 프리펩이라 **차별화 필요**, 가능하면 `FireNovaYellow` 또는 `SurfaceExplosionStone` 검토)
  2. `MeteorFireZone` 생성 — 별도 GameObject (Fire Zone 은 운석 본체 destroy 후에도 15초 유지)
  3. `Destroy(gameObject)`
- **낙하 트레일**: `ChargeAura/AuraChargeRed.prefab` 또는 TrailRenderer 자식으로 달아 떨어지는 동안 자동 재생

#### 4-3. MeteorFireZone (MonoBehaviour, 지속 화염)

```csharp
public void Initialize(Vector3 center, float radius, float duration, float damagePerTick, float tickInterval, LayerMask bugLayer, GameObject vfxPrefab);

private void Update() {
    _life -= Time.deltaTime;
    if (_life <= 0f) { Destroy(gameObject); return; }
    _tickTimer -= Time.deltaTime;
    if (_tickTimer <= 0f) {
        _tickTimer = _tickInterval;
        int hits = Physics.OverlapSphereNonAlloc(_center, _radius, _buffer, _bugLayer);
        for (int i=0; i<hits; i++) {
            if (_buffer[i].TryGetComponent<IDamageable>(out var d))
                d.TakeDamage(_damagePerTick);
        }
    }
}
```

- **VFX**: `Environment/FloorTrap/FloorTrapMolten.prefab` (원형 authoring 이라 스케일 그대로 ✅. 네이팜은 비등방이라 타일링 필요했지만 여기는 단일 인스턴스)
- **바닥 데칼**: `BuildCircle(radius)` + 빨강 반투명
- **상수**: `TickInterval = 0.1f` (v2 6frame)

### Step 5 — VFX 프리펩 매핑 (Polygon Arsenal 재사용)

| 어빌리티 / 용도 | 프리펩 경로 | 처리 |
|---|---|---|
| 블랙홀 — 본체 소용돌이 | `Assets/Polygon Arsenal/Prefabs/Interactive/Portal/Vortex/VortexPortalPurple.prefab` | 실측 후 90°X 회전 필요시 적용. 스케일 = Range 비율 |
| 블랙홀 — 범위 데칼 | 런타임 Mesh `BuildCircle(Range)` | 보라 반투명 |
| 충격파 — 중심 발동 | `Assets/Polygon Arsenal/Prefabs/Combat/Nova/FrostNova.prefab` | 1회 Instantiate |
| 충격파 — 확장 링 | 런타임 Mesh `BuildRing(inner, outer)` | 청록 반투명, localScale 으로 확장 |
| 메테오 — 낙하체 | 신규 `Assets/_Game/Prefabs/Abilities/MeteorProjectile.prefab` (에디터 메뉴 자동 생성) | `Interactive/Powerups/Orbs/GlowPowerupMediumRed.prefab` body + Trail |
| 메테오 — 낙하 트레일 | `Combat/Aura/ChargeAura/AuraChargeRed.prefab` (후보 1) / `TrailRenderer` (후보 2) | MeteorProjectile 자식 |
| 메테오 — 착지 폭발 | `Combat/Nova/FireNova/FireNovaYellow.prefab` (지뢰와 분리 위해 Grenade 대신 선택) | 반경 비례 스케일 |
| 메테오 — 지속 화염 | `Environment/FloorTrap/FloorTrapMolten.prefab` | 스케일 = `radius / basePrefabRadius` |
| 메테오 — 지속 화염 데칼 | 런타임 Mesh `BuildCircle(radius)` | 빨강 반투명 |

### Step 6 — AbilityData SO 수정 & VFX 바인딩

| SO | 필드 | 현재 | 변경 후 | 근거 |
|---|---|---|---|---|
| `Ability_Sara_BlackHole` | `_range` | `18` | (유지) | v2 180÷10 ✅ |
| `Ability_Sara_BlackHole` | `_damage` | `0` | **유지** — 흡인 속도는 Runner 상수 (`PullSpeedPerSec`) | SO 의미 혼란 방지 |
| `Ability_Sara_BlackHole` | `_vfxPrefab` | 비어있음 | `VortexPortalPurple.prefab` | SaraVfxBinder 메뉴로 자동 |
| `Ability_Sara_Shockwave` | `_range` | `36` | (유지) | v2 360÷10 ✅ |
| `Ability_Sara_Shockwave` | `_vfxPrefab` | 비어있음 | `FrostNova.prefab` (중심 발동 VFX) | 자동 |
| `Ability_Sara_Meteor` | `_range` | `5.5` | (유지) | v2 radius 55÷10 ✅ |
| `Ability_Sara_Meteor` | `_damage` | `0.5` | (유지) | 0.1s 틱당 ✅ |
| `Ability_Sara_Meteor` | `_durationSec` | `15` | (유지) | 화염 지대 지속 ✅ |
| `Ability_Sara_Meteor` | `_autoIntervalSec` | `10` | (유지) | ✅ |
| `Ability_Sara_Meteor` | `_vfxPrefab` | 비어있음 | `MeteorProjectile.prefab` (신규 생성) | MeteorPrefabCreator 메뉴 |

#### HUD 아이콘 바인딩 (AbilityData._icon)

빅터와 동일하게 **128px 해상도** 권장. 아이콘 에셋은 이미 준비되어 있음 — SO 인스펙터 에 수동 드래그 또는 에디터 메뉴로 자동 바인딩.

```
Assets/_Game/Sprites/UI/drillcorp_sara_abilities/
├── 64px/   ├── 1_blackhole.png  ├── 2_shockwave.png  └── 3_meteor.png
├── 128px/  ├── 1_blackhole.png  ├── 2_shockwave.png  └── 3_meteor.png   ← HUD 권장
└── 256px/  ├── 1_blackhole.png  ├── 2_shockwave.png  └── 3_meteor.png
```

| SO | 아이콘 파일 |
|---|---|
| `Ability_Sara_BlackHole.asset` | `.../128px/1_blackhole.png` |
| `Ability_Sara_Shockwave.asset` | `.../128px/2_shockwave.png` |
| `Ability_Sara_Meteor.asset` | `.../128px/3_meteor.png` |

> 빅터 Phase5 §11.8 기록과 동일하게, `SaraVfxBinder` 메뉴에서 VFX 3종을 세팅할 때 **아이콘도 같이 자동 바인딩**해 PNG → Sprite 임포트만 된 상태면 SO 까지 한 번에 연결되도록 확장 권장 (사용자 수동 드래그도 허용).

> **지누스 참고**: 같은 구조가 `Assets/_Game/Sprites/UI/drillcorp_jinus_abilities/` 에 준비됨 (1_drone / 2_mining_drone / 3_spider_drone). 후속 Phase 7 에서 사용.

**Runner 전용 public 필드** (빅터 MineInstance 패턴 준수 — Resources.Load 금지, 인스펙터 바인딩):
- `MeteorRunner`: `_impactVfxPrefab`, `_fireZoneVfxPrefab` 공개 (SaraVfxBinder 가 같이 세팅)
- `MeteorInstance` 프리펩 내부: `_trailChild` (이미 프리펩에 자식), `_impactVfxPrefab`, `_fireZoneVfxPrefab` 필드

### Step 7 — 에디터 자동화 (신규 2파일)

| 파일 | 메뉴 | 기능 |
|---|---|---|
| `Scripts/Editor/MeteorPrefabCreator.cs` | `Tools/Drill-Corp/3. 게임 초기 설정/사라/1. 메테오 프리펩 생성` | `Assets/_Game/Prefabs/Abilities/MeteorProjectile.prefab` 생성. `GlowPowerupMediumRed` 복제를 Body 자식으로, TrailRenderer(또는 AuraChargeRed) 추가, `MeteorInstance` 컴포넌트 부착 + 내부 필드 자동 바인딩. Meteor SO `_vfxPrefab` 자동 할당 |
| `Scripts/Editor/SaraVfxBinder.cs` | `Tools/Drill-Corp/3. 게임 초기 설정/사라/2. 사라 VFX · 아이콘 바인딩` | **VFX**: BlackHole SO ← VortexPortalPurple, Shockwave SO ← FrostNova, Meteor SO ← MeteorProjectile. **아이콘**: 각 SO `_icon` ← `drillcorp_sara_abilities/128px/{1_blackhole,2_shockwave,3_meteor}.png`. 기존 `NapalmVfxBinder` 패턴 복제 확장 |

메뉴 네이밍은 Phase 5 §10.3 의 `빅터/{1,2}` 서브메뉴와 일관되게 `사라/{1,2}` 로.

### Step 8 — 테스트 시나리오

1. **캐릭터 전환** — Title 씬에서 Sara 선택 후 Game 진입. `AbilitySlotController.ResolvedCharacter.CharacterId == "sara"` 확인.
2. **1키 블랙홀** — 마우스 위치에 보라 소용돌이 생성. 10초간 벌레가 중심으로 끌려옴. 재사용 시 30초 쿨다운 대기.
3. **2키 충격파** — 머신 중심에서 청록 링이 빠르게 바깥으로 확장. 링 통과한 벌레는 밖으로 튕기고 3초간 50% 느려짐. 동일 벌레 1회만 히트. 50초 쿨다운.
4. **3키 무시 + 10초 자동 발동** — 아무 입력 없어도 10초마다 랜덤 위치에 운석 낙하 → 착지 시 빨간 화염 지대 15초 유지. HUD 쿨다운 바는 10초 주기로 차올라 리셋 반복.
5. **블랙홀 동시 1개 제한**: 블랙홀 활성 중에 1키 다시 눌러도 새 존 생성 안 됨 (쿨다운은 발동 시점부터 감소).
6. **해금 가드**: `PlayerData.UnlockedAbilities` 에 `sara_shockwave` 없으면 2번 슬롯이 Runner null → HUD 슬롯 비활성. `_ignoreUnlockGate=true` 체크 시에만 전부 활성.
7. **빅터/지누스 선택 시 사라 Runner 미생성** — `CreateRunner` 가 캐릭터에 맞지 않는 타입을 만나지 않음 (CharacterData.Abilities 가 이미 캐릭터별로 고정).

---

## 5. 좌표계 체크리스트 (CLAUDE.md 준수)

- [x] `aim.AimPosition` 은 이미 Y=0 지면 평면이므로 블랙홀 중심 그대로 사용
- [x] 메테오 낙하 시작점은 `targetPos + Vector3.up * FallHeight` (월드 +Y)
- [x] 블랙홀 흡인 / 충격파 푸시 방향 모두 `to.y = 0f` 강제
- [x] 링 Mesh / 데칼 Mesh 는 `AbilityDecalMeshBuilder` 가 이미 XZ 평면 — 회전 보정 불필요
- [x] Polygon Arsenal 프리펩(VortexPortal, FireNova, FloorTrapMolten) Y축 실측 후 필요시 wrapper 90°X 회전

---

## 6. 주요 리스크

| 리스크 | 대응 |
|---|---|
| Polygon Arsenal VFX 다수가 XY 세로 authoring | 각 Runner에서 실측 후 wrapper GO 에 `Quaternion.Euler(90,0,0)` 보정 (MinePrefabCreator 패턴) |
| 블랙홀 흡인 / 충격파 푸시 vs BugController 자체 이동 경합 | `transform.position += delta` direct 방식 (사용자 결정 #2). BugController 가 매 프레임 머신 방향 재계산 → 자연 복귀. 부자연스러우면 `ApplyExternalDisplacement` API 후속 신설 |
| 충격파 순간 변위(8 유닛)가 벌레를 플레이 영역 밖으로 밀 수 있음 | BugController 다음 프레임부터 머신 쪽으로 이동 재개 → 대부분 자연 복귀. 문제 시 `ClampToPlayArea` 추가 검토 |
| 메테오 첫 발동 = 게임 시작 후 10초 대기 (v2 원본) | 사용자 결정 #3 에 따라 **v2 원본 유지**. HUD 쿨다운 바는 첫 10초 동안 차오름 |
| 런타임 Mesh 매 프레임 재빌드 (충격파) | `BuildRing` 은 1회만, 매 프레임은 `localScale` 만 갱신 |
| `MeteorProjectile` 낙하 중 카메라 프러스텀 밖에서 시작 | FallHeight = 12 가 카메라 높이 대비 위쪽 — 카메라 설정에 따라 컬링될 수 있음. TrailRenderer 로 시각적 연속성 확보 |
| `FireNovaYellow` 폭발이 `GrenadeExplosionRed`(지뢰와) 시각적 중첩 | 프리펩 교체 — 플레이테스트 후 `SurfaceExplosionStone` 등 후보 재검토 |
| 블랙홀 중심과 머신이 겹칠 때 벌레가 머신에 박힘 | `InnerCutoff = 0.4f` 근처에서 흡인 중단 + 머신 위치에 두지 않도록 UX 가이드 (크로스헤어에 경고 색) — 후속 |

---

## 7. 작업 체크리스트

- [x] **Step 1** `AbilitySlotController.CreateRunner` switch 3줄 추가
- [x] **Step 2** `BlackHoleRunner.cs` (+ 내부 `BlackHoleZone`, VortexPortalPurple 실측·회전 보정)
- [x] **Step 3** `ShockwaveRunner.cs` (링 Mesh 스케일업 + `ApplySlow` + 순간 변위)
- [x] **Step 4** `MeteorRunner.cs` + `MeteorInstance.cs` + `MeteorFireZone.cs` (AutoInterval)
- [x] **Step 5** 런타임 VFX 동작 검증 (Y 회전 보정 여부)
- [x] **Step 6** SO 3개 `_vfxPrefab` 바인딩 + 필요시 `_damage` 튜닝
- [x] **Step 7** `MeteorPrefabCreator.cs` 에디터 메뉴 (SaraVfxBinder 는 Step 10.5 에 Deferred 로 이관)
- [~] **Step 8** Game 씬 테스트 시나리오 — 1~5 통과, 슬로우 VFX 교체 튜닝 중(§10.4)

---

## 8. 구현 후 문서 작업

- 본 문서 §10 에 **as-built** 섹션 추가 (빅터 Phase5 §10 패턴) — 생성/수정 파일, 최종 튜닝 값, 구현 중 이슈 기록
- `docs/CharacterAbilitySystem.md` §5.4~5.6 "2026-XX-XX 폴리싱" 주석 업데이트
- `docs/CHANGELOG.md` 엔트리 추가
- `docs/README.md` 인덱스에 Phase6 링크

---

## 9. 사용자 결정 사항 (2026-04-21)

| # | 결정 | 반영 |
|---|---|---|
| 1 | Sara 우선 (Jinus 다음 Phase) | 본 문서 |
| 2 | 블랙홀 흡인은 `Transform.position += delta` direct 조작 | §2, §4 Step 2 |
| 3 | 메테오 첫 발동 = v2 원본 유지 (게임 시작 후 10초 대기) | §4 Step 4-1 |

---

## 10. 구현 결과 (2026-04-21, as-built)

### 10.1 생성/수정 파일

| 경로 | 타입 | 역할 |
|---|---|---|
| `Scripts/Ability/Runners/BlackHoleRunner.cs` | 신규 | 중력 존 1개 지속 흡인 + 내부 `BlackHoleZone` 클래스. VortexPortalPurple wrapper 90°X 회전 |
| `Scripts/Ability/Runners/ShockwaveRunner.cs` | 신규 | 확장 링 1회성 + 내부 `ShockwaveRing` 클래스. `BuildRing(0.85, 1)` Mesh 1회 생성 후 localScale 확장. `SimpleBug.ApplySlow` 호출 + 순간 변위 푸시 |
| `Scripts/Ability/Runners/MeteorRunner.cs` | 신규 | AutoInterval 10초 주기. 머신 기준 반경 3~15 랜덤 위치 + XZ 오프셋 14 유닛 = **비스듬한 낙하 궤적** |
| `Scripts/Ability/Runners/MeteorInstance.cs` | 신규 | 낙하체 MonoBehaviour. startPos→targetPos 직선 이동. 도착 시 폭발 VFX + MeteorFireZone 스폰 + Destroy |
| `Scripts/Ability/Runners/MeteorFireZone.cs` | 신규 | 15초 지속 원형 화염. 0.1s 틱 `OverlapSphereNonAlloc` → `TakeDamage(0.5)`. FloorTrapMolten 자식 PS 전부 loop 강제 (네이팜 타일링 패턴) |
| `Scripts/Ability/AbilitySlotController.cs` | 수정 | switch 분기 BlackHole/Shockwave/Meteor 3줄 추가 |
| `Scripts/Data/AbilityData.cs` | 수정 | `_vfxScale` 필드 신설 (기본 1.0) + **`OnValidated` 이벤트** (`#if UNITY_EDITOR`). Runner가 구독해 Range/VfxScale 실시간 튜닝 가능 |
| `Scripts/Bug/Simple/SimpleBug.cs` | 수정 | **슬로우 기능 이식** — `ApplySlow(strength, duration)`, `_slowTimer` 감소, 이동속도 `(1-_slowStrength)` 배율. **슬로우 VFX + 머티리얼 틴트** (전략 3) 적용 |
| `Scripts/Editor/MeteorPrefabCreator.cs` | 신규 | 메뉴 `Tools/Drill-Corp/3. 게임 초기 설정/사라/1. 메테오 프리펩 생성`. `MeteorInstance.prefab` + `MeteorBody.mat` 자동 생성 + Meteor SO `_vfxPrefab` 바인딩 |
| `Assets/_Game/Materials/MeteorBody.mat` | 신규 | 낙하체 Body Sphere 머티리얼. Unlit/Color 빨강 (`new Material()` 만으로는 프리펩 직렬화 안 돼 .mat 에셋 필요) |
| `Assets/_Game/Prefabs/Abilities/MeteorInstance.prefab` | 신규 | 에디터 메뉴로 자동 생성 — Body(Sphere+MeteorBody.mat) + ChargeAura(AuraChargeRed, 90°X 회전) + MeteorInstance 컴포넌트 |
| `Assets/_Game/Data/Abilities/Ability_Sara_*.asset` | 수정 | 3종 모두 `_vfxPrefab` 바인딩 (VortexPortalPurple / LightningWaveBlue / MeteorInstance) + `_vfxScale` 기본 1.0 |
| `Assets/_Game/Prefabs/Bugs/Simple/SimpleBug_*.prefab` | 수정 | `_slowVfxPrefab` 에 `ChainedFrost.prefab` 바인딩 (Normal/Elite/Swift 3종) |

### 10.2 최종 튜닝 값

#### AbilityData SO

| SO | 필드 | 값 | 주 |
|---|---|---|---|
| `Ability_Sara_BlackHole` | `_range` | `18` | v2 180÷10 (흡인 반경) |
| `Ability_Sara_BlackHole` | `_durationSec` / `_cooldownSec` | `10` / `30` | v2 동일 |
| `Ability_Sara_BlackHole` | `_vfxPrefab` | `VortexPortalPurple.prefab` | 수동 바인딩 |
| `Ability_Sara_Shockwave` | `_range` | `36` | v2 360÷10 (링 최대 반경) |
| `Ability_Sara_Shockwave` | `_cooldownSec` | `50` | v2 동일 |
| `Ability_Sara_Shockwave` | `_vfxPrefab` | `LightningWaveBlue.prefab` | 수동 바인딩 (FrostNova 에서 변경) |
| `Ability_Sara_Meteor` | `_range` | `5.5` | v2 radius 55÷10 (화염 지대 반경) |
| `Ability_Sara_Meteor` | `_damage` | `0.5` | 0.1s 틱당 (v2 동일) |
| `Ability_Sara_Meteor` | `_durationSec` / `_autoIntervalSec` | `15` / `10` | v2 동일 |
| `Ability_Sara_Meteor` | `_vfxPrefab` | `MeteorInstance.prefab` | MeteorPrefabCreator 자동 바인딩 |

#### Runner 내부 상수

| Runner | 상수 | 값 | 의미 |
|---|---|---|---|
| BlackHoleRunner | `PullSpeedPerSec` | `5.4f` | v2 0.9/frame × 60 ÷ 10 |
| BlackHoleRunner | `InnerCutoff` | `0.4f` | 흡인 중단 반경 (딱붙기 방지) |
| BlackHoleRunner | `RotateVfxToLyFlat` | `true` | VortexPortal XY→XZ 보정 |
| BlackHoleRunner | `VortexReferenceRadius` | `2f` | 프리펩 기준 반경 (scale 계산) |
| ShockwaveRunner | `ExpandSpeedPerSec` | `84f` | v2 14/frame × 60 ÷ 10 |
| ShockwaveRunner | `PushDistance` | `8f` | v2 80 ÷ 10 순간 변위 |
| ShockwaveRunner | `HitMargin` | `0.3f` | 링 히트 허용 오차 |
| ShockwaveRunner | `SlowStrength` / `SlowDurationSec` | `0.5f` / `3f` | v2 동일 |
| ShockwaveRunner | `RingInnerRatio` | `0.85f` | 얇은 링 데칼 |
| ShockwaveRunner | `ImpactVfxReferenceRadius` | `2f` | LightningWave 기준 |
| ShockwaveRunner | `RotateVfxToLyFlat` | `true` | 탑뷰 평면화 |
| MeteorRunner | `FallSpeed` | `12f` | 비스듬 낙하 속도 |
| MeteorRunner | `FallHeight` | `18f` | targetPos.y + 이 값 = 스폰 Y |
| MeteorRunner | `FallHorizontalOffset` | `14f` | XZ 오프셋 — 탑뷰에서 대각선 궤적(~38°) |
| MeteorRunner | `SpawnRadiusMin/Max` | `3f` / `15f` | 머신 기준 랜덤 착지 범위 |
| MeteorRunner | `FireZoneTickInterval` | `0.1f` | v2 6frame |

#### SimpleBug 슬로우 관련

| 필드 | 값 | 의미 |
|---|---|---|
| `_slowVfxPrefab` | `ChainedFrost.prefab` | 슬로우 걸리는 **순간** 1회성 임팩트 |
| `_rotateSlowVfxFlat` | `true` | 탑뷰 평면화 (90°X) |
| `_slowVfxScale` | `3f` | 벌레 localScale 상쇄 + 추가 배율 |
| `_slowVfxDuration` | `0.5f` | 임팩트 VFX 표시 시간 (부하 제어용) |
| `_slowTint` | `(0.31, 0.76, 0.97, 0.55)` | 슬로우 지속 중 벌레 몸체 청록 틴트 |

### 10.3 에디터 메뉴

| 메뉴 | 기능 |
|---|---|
| `Tools/Drill-Corp/3. 게임 초기 설정/사라/1. 메테오 프리펩 생성` | `MeteorInstance.prefab` + `MeteorBody.mat` 자동 생성. ChargeAura(90°X 회전) 자식 추가. FireNovaYellow + FloorTrapMolten 필드 자동 바인딩. Meteor SO `_vfxPrefab` 자동 할당 |

### 10.4 구현 중 발견/해결한 이슈

| 이슈 | 원인 | 해결 |
|---|---|---|
| 블랙홀 VortexPortal 이 탑뷰에서 세로로 섬 | XY authoring 프리펩 | wrapper GO 에 `Quaternion.Euler(90,0,0)` (MinePrefabCreator 패턴 재사용) |
| `_vfxBaseRadius` 필드 키워도 VFX 가 작아짐 | 직관 역방향 (분모 증가) | **`_vfxScale` 배율 방식으로 뒤집음** — 키우면 커짐. 참조 반경은 Runner 내부 상수로 분리 |
| 라이브 튜닝 필요 — 인스펙터 값 변경 즉시 반영 | 스폰 시점 박제 구조 | `AbilityData.OnValidated` 이벤트 추가 (`#if UNITY_EDITOR`). Runner 가 구독해 활성 존에 `ApplyLiveTuning` 호출. 빌드 제외 |
| 충격파 Slow 가 안 걸림 | `TryGetComponent<BugController>` 사용 — 현 프로젝트는 `SimpleBug` | `DrillCorp.Bug.Simple.SimpleBug` 로 교체. `SimpleBug` 에 `ApplySlow` 메서드 이식 |
| 90마리 동시 슬로우 시 프레임 드랍 | ChainedFrost 4개 중첩 PS × 90마리 = 360 PS 동시 재생 | **전략 3 — 체인은 0.5초 임팩트만, 지속 표시는 머티리얼 틴트**. Renderer 인스턴스 머티리얼 캐싱 + `_BaseColor`/`_Color` Lerp |
| 메테오 낙하가 수직이라 탑뷰에서 "떨어지는 느낌" 약함 | XZ 오프셋 없음 | `FallHorizontalOffset = 14f` 추가 — targetPos 기준 랜덤 XZ 방향으로 밀어 45° 가까운 대각선 궤적 + 운석이 낙하 방향을 바라보도록 `LookRotation` |
| 메테오 Body Sphere 가 마젠타 | `new Material()` 는 프리펩에 직렬화 안 됨 | `AssetDatabase.CreateAsset` 으로 `MeteorBody.mat` 저장 후 참조. 셰이더 탐색 순서도 프로젝트 컨벤션 `Unlit/Color` → URP → Sprites/Default 로 교정 |
| SimpleBug 스폰이 화면 구석 밖이라 머신 도달까지 오래 걸림 | `_autoRadius=true` 시 카메라 대각선 기준 | 사용자 피드백 — Spawner `_autoRadius=false` + `_manualRadius` 축소 또는 `_radiusMultiplier` 신설 (본 Phase 범위 밖, 후속 조정) |

### 10.5 Deferred (후속 작업)

- **`SaraVfxBinder.cs` 에디터 메뉴** — 계획 §7 에 있었으나 현 단계에선 SO 3개 수동 바인딩으로 충분해 생략. Jinus 어빌리티 구현 시 `JinusVfxBinder` 와 함께 통합 메뉴로 제작 가능.
- **블랙홀 흡인 vs BugController** — 현재 프로젝트는 SimpleBug 전용이라 BugController 이동 경합 이슈 없음. 향후 BugController 재도입 시 `ApplyExternalDisplacement` API 신설 검토.
- **충격파 VFX 재검토** — 현재 LightningWaveBlue. "얼음이 팍!" 느낌을 원하면 `SpikeIce` / `MiniExploFrost` / `IceExplosion` 교체 후보 확인됨 (실시간 비교 튜닝 대기).
- **슬로우 VFX 크기/위치 미세조정** — `_slowVfxScale=3` 이 대량 벌레에 적용 시 과할 수 있음. 플레이테스트 후 Beetle/Elite/Swift 별 차등.
- **에디터 전용 코드 마킹 리팩토링** — `docs/Refactor_EditorOnlyCode_Plan.md` 에 정리 완료. Jinus 완료 후 재개.

---

## 11. 참고 문서

- [CharacterAbilitySystem.md](CharacterAbilitySystem.md) §4~§5.6 — Ability 아키텍처 + 사라 스펙
- [Phase5_VictorAbility_Plan.md](Phase5_VictorAbility_Plan.md) — 형제 Phase (공통 인프라, 네이밍, 에디터 메뉴 컨벤션)
- [Refactor_EditorOnlyCode_Plan.md](Refactor_EditorOnlyCode_Plan.md) — `#if UNITY_EDITOR` 마킹 정리 계획 (defer)
- [VFX_3D_MigrationPlan.md](VFX_3D_MigrationPlan.md) — 2D→3D VFX 컨벤션
- `docs/v2.html:994~1000, 1055, 1061~1080, 1146~1184, 1265~1295` — useItem/tick 원본

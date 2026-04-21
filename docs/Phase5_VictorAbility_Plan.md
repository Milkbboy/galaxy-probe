# Phase 5 — 빅터(Victor) 어빌리티 구현 계획

> 작성일: 2026-04-20
> 범위: 네이팜 / 화염방사기 / 폭발지뢰 (빅터 3종)
> 근거 프로토타입: `docs/v2.html` (1054~1263, 1554, 1724, 1749)
> 상위 문서: [CharacterAbilitySystem.md](CharacterAbilitySystem.md) §5.1~5.3
> 형제 문서: [Phase2_Bomb_Plan.md](Phase2_Bomb_Plan.md) / [Phase4_Laser_Plan.md](Phase4_Laser_Plan.md)

## 0. 목표

`CharacterAbilitySystem.md` 스펙의 빅터 3종 어빌리티를 Game 씬에 3D 이펙트로 구현한다. 캐릭터 데이터·SO·Registry는 이미 존재하므로 **런타임 실행 레이어**와 **3D VFX**가 본 페이즈의 전부.

---

## 1. v2.html 원본 동작 분석

### 1-1. 네이팜 — `useItem('napalm')`

| 위치 | 역할 |
|---|---|
| v2.html:1056 | 발동 — `napalmZones.push({...})` |
| v2.html:1005~1041 | `tickNapalm(dt)` — 6프레임(0.1s) 주기 데미지 + 파티클 |
| v2.html:1001~1004 | `pointInRect` — 회전된 직사각형 포함 판정 |
| v2.html:1554 | `drawNP` — 그라디언트 직사각형 렌더 |

```js
napalmZones.push({
  x: CX, y: CY,                       // 원점 = 머신
  angle: atan2(mouseY-CY, mouseX-CX),
  len: sqrt(W*W+H*H),                 // 화면 대각선 ≒ 무한
  halfW: 42,                          // 직사각형 반폭
  timer: 1200, maxTimer: 1200,        // 20초 (60fps)
  dmgTick: 0
});
items.napalm.cd = 2400;               // 40초
// tickNapalm: nz.dmgTick <=0 → 6프레임 주기로 zone 내 모든 bug에 0.5 dmg
```

**핵심**: **회전된 직사각형(OBB) 지속 장판**. `pointInRect`(v2.html:1001)은 각도로 역회전시킨 로컬좌표에서 `0 ≤ lx ≤ len && |ly| ≤ halfW` 검사 — Unity `Physics.OverlapBox` + `Quaternion` 회전으로 그대로 대응된다.

### 1-2. 화염방사기 — `useItem('flamethrower')`

| 위치 | 역할 |
|---|---|
| v2.html:1087~1091 | 발동 — `flameActive=true; flameTimer=300` |
| v2.html:1215~1241 | tick — 매 프레임 부채꼴 데미지 + 파티클 |
| v2.html:1749~1759 | 렌더 — 부채꼴 그라디언트 |

```js
flameActive = true; flameTimer = 300;  // 5초
items.flamethrower.cd = 1200;          // 20초
// 매 프레임 (v2.html:1220~1231):
fAngle  = atan2(mouseY-CY, mouseX-CX); // 마우스 실시간 추적
fSpread = 0.35 rad;                    // ±20°
fLen    = 180;
// 반경 내 bug의 각도차 |fda| < fSpread → fg.hp -= 0.18 * dt
```

**핵심**: **부채꼴 지속 빔(5초)**. 매 프레임 마우스 위치로 각도 재계산 → 이동·무기 병행 가능.

**데미지 해석**: v2의 `dt`는 **프레임 수**(보통 1) 단위 → 프레임당 0.18씩 감소 → 60fps 기준 **10.8 dps**. Unity로 옮길 때는 초당 값으로 통일해 `bug.TakeDamage(10.8f * Time.deltaTime)` 형태로 호출한다. (AbilityData SO의 `_damage` 필드 값을 "초당 데미지"로 재해석하는 것이 규칙 — Runner 구현 시 주석 필수.)

### 1-3. 폭발지뢰 — `useItem('mine')`

| 위치 | 역할 |
|---|---|
| v2.html:1092~1099 | 발동 — `mines.push({x,y,armTimer:30})` |
| v2.html:1243~1263 | tick — 활성화 지연 + 벌레 접근 시 폭발 |
| v2.html:1724~1746 | 렌더 — 6각 본체 + 감지 반경 |

```js
mines.push({ x: mouseX, y: mouseY, active: true, armTimer: 30 });  // 0.5s 활성 지연
items.mine.cd = 600;                                                // 10초, 최대 5개
// 폭발 시 (v2.html:1251-1259):
mineR = ws.bomb.radius * 0.5;
bugs[mej].hp -= ws.bomb.dmg * 1.5;          // 일반 bug
checkBossHit(mn.x, mn.y, mineR, ws.bomb.dmg * 2);  // 보스는 ×2
```

**핵심**: **폭탄 무기 강화(`ws.bomb.dmg/radius`)가 지뢰에 그대로 전파**. 활성화 전 0.5초 동안 점멸/`...` 표시. **보스 대상 데미지는 ×2 (일반 bug ×1.5와 구분)** — v2 원본 준수.

---

## 2. Unity 현황 (이미 있는 것 / 없는 것)

| 레이어 | 상태 | 경로 |
|---|---|---|
| `AbilityData` SO + `AbilityType` enum | ✅ 완비 | `Assets/_Game/Scripts/Data/AbilityData.cs` |
| Ability SO 에셋 9종 (빅터3 포함, 스탯 채워짐) | ✅ | `Assets/_Game/Data/Abilities/Ability_Victor_{Napalm,Flame,Mine}.asset` |
| `CharacterData` + `CharacterRegistry` (머신 주입까지 완료) | ✅ | `Scripts/Data/CharacterData.cs`, `Scripts/OutGame/CharacterRegistry.cs` |
| `AimController` — `AimPosition`(Y=0), `BugLayer`, `MachineTransform` | ✅ | `Scripts/Aim/AimController.cs:80-85` |
| `IDamageable.TakeDamage(float)` — BugController 구현 | ✅ | `Scripts/Bug/IDamageable.cs`, `Scripts/Bug/BugController.cs` |
| `BombWeapon._effectiveDamage / _effectiveRadius` (지뢰 의존값) | ⚠️ private, getter 추가 필요 | `Scripts/Weapon/Bomb/BombWeapon.cs:40-41` |
| `Physics.OverlapSphereNonAlloc` 패턴, `SimpleVFX`, `AutoDestroyPS` | ✅ | `Scripts/VFX/` |
| 3D VFX 프리펩 컨벤션 (Bomb/MG/Laser 3D 프리펩) | ✅ 참고 가능 | `Assets/_Game/Prefabs/Weapons/VFX_3D/` |
| `Assets/_Game/Scripts/Ability/` 런타임 폴더 | ❌ 전혀 없음 | 신규 |
| `IAbilityRunner` / `AbilityContext` / `AbilitySlotController` | ❌ | 신규 |
| `NapalmRunner` / `FlameRunner` / `MineRunner` | ❌ | 신규 |
| Slot 1/2/3 입력 | ❌ `.inputactions`에 없음 | `Keyboard.current[Key.Digit1..3]` 직접 사용 |
| 네이팜·화염·지뢰 3D VFX | ✅ Polygon Arsenal 재사용 (신규 제작 불필요) | `Assets/Polygon Arsenal/Prefabs/**` — Step 6 매핑표 참조 |

---

## 3. 구현 순서 (Step-by-Step)

### Step 1 — 런타임 스캐폴딩 (신규 파일 3개)

```
Assets/_Game/Scripts/Ability/
├── AbilityContext.cs
├── IAbilityRunner.cs
└── AbilitySlotController.cs
```

**AbilityContext** — Runner에 넘길 데이터 번들:
- `Transform MachineTransform`
- `AimController Aim`
- `LayerMask BugLayer`
- `Transform VfxParent` (Hierarchy 정리용 부모)
- `BombWeapon BombWeapon` (지뢰 전용 의존성, null 허용)

**IAbilityRunner** — 스펙 문서(§4.3) 그대로:
- `AbilityType Type { get; }`
- `void Initialize(AbilityData, AbilityContext)`
- `void Tick(float dt)`
- `bool TryUse(Vector3 aimPoint)`
- `float CooldownNormalized { get; }` — UI용 (0=사용가능, 1=방금 사용)

**AbilitySlotController** — Game 씬에 1개 배치:
- `Start()`에서 `DataManager.PlayerData.SelectedCharacterId` → `CharacterRegistry.Find()` → `UnlockedAbilities` 필터로 3 runner 인스턴스화
- `Update()`에서 모든 runner `Tick(dt)` + `Keyboard.current[Key.Digit1/2/3].wasPressedThisFrame` 체크해 `TryUse(aim.AimPosition)` 호출
- `DataManager`/`CharacterData` 없는 단독 실행도 null-safe (인스펙터에서 `_character` 직접 할당 가능)

> **입력**: `.inputactions` 자산 수정을 피하고 `Keyboard.current` 직접 사용. 추후 키 리매핑 요구가 생기면 `InputActionReference`로 교체 (CLAUDE.md — New Input System 준수).

### Step 2 — BombWeapon 접근자 2줄 추가

`Scripts/Weapon/Bomb/BombWeapon.cs`:
```csharp
public float EffectiveDamage => _effectiveDamage;
public float EffectiveRadius => _effectiveRadius;
```
`BombWeapon.RefreshEffectiveStats()`가 `WeaponUpgradeManager.GetBonus`로 이미 업그레이드 배율을 적용하고 있으므로 SSoT 유지.

### Step 3 — NapalmRunner

**동작**: `TryUse` 시 머신 위치에서 마우스 방향으로 긴 **회전된 직사각형(OBB)** 지속 장판 1개 스폰. 20초간 0.1초 주기로 내부 bug에 0.5 데미지.

**좌표 변환**:
- 원점 `origin = machineTransform.position` (Y=0)
- `dir = aim.AimPosition - origin; dir.y = 0f; dir.Normalize();`
- 회전 `rot = Quaternion.LookRotation(dir, Vector3.up)`
- 중심 `center = origin + dir * (len * 0.5f)`

**판정**: `Physics.OverlapBoxNonAlloc(center, halfExtents, buf, rot, bugLayer)`에서 `halfExtents = new Vector3(halfW, 2f, len * 0.5f)`.

**VFX**: Polygon Arsenal 재사용 — `Environment/FloorTrap/FloorTrapMolten.prefab`을 장판 중심에 1개 인스턴스 + `Environment/Fire/OilFire/OilFireRed.prefab`을 길이축 따라 N개 타일링(예: len/20 유닛 간격). 수명 = `DurationSec`, 루트에 `AutoDestroy` 부착.

**주의 — 단위 재튜닝**: 현재 SO에 `_range: 42`(v2 픽셀 halfW), `_damage: 0.5`(틱당), `_cooldownSec: 40`, `_durationSec: 20`. 길이는 v2처럼 "화면 대각선"이 아니라 **유한 길이(권장 60~80 유닛)** 로 제한. 실제 유닛 크기는 플레이테스트 후 재튜닝(§6 참조).

### Step 4 — FlameRunner

**동작**: `TryUse`로 5초 활성화, 매 프레임 마우스 방향 부채꼴(±20°, 길이 180) 내 bug에 dps 데미지.

**판정**: `Physics.OverlapSphereNonAlloc(machinePos, Range)`로 후보 수집 → 각 collider에 대해
```csharp
Vector3 toBug = bug.position - machinePos; toBug.y = 0f;
float angle = Vector3.Angle(dir, toBug);            // 도 단위
if (angle <= AngleDeg) bug.TakeDamage(DpsDamage * Time.deltaTime);
```

**데미지 단위 규칙**:
- SO의 `_angle`은 라디안 → `Mathf.Rad2Deg` 변환 후 비교.
- SO의 `_damage`는 **"초당 데미지(dps)"로 해석** (v2 `0.18/frame × 60 = 10.8`).
- 현재 `Ability_Victor_Flame.asset`의 `_damage` 값은 확인 후 **10.8로 설정** (v2와 일치시킬 경우). Runner 상단에 `// _damage는 dps — TakeDamage(_damage * Time.deltaTime)` 주석 필수.

**VFX**: Polygon Arsenal 재사용 — `Combat/Flamethrower/Spray/FlamethrowerSprayRed.prefab` 그대로 사용 (Spray 변형은 이미 부채꼴 형태, Pointy는 좁은 빔이라 부적합). 머신 자식으로 Instantiate 후 매 프레임:
```csharp
vfxRoot.rotation = Quaternion.LookRotation(dir, Vector3.up);
```
5초 후 `Destroy`.

### Step 5 — MineRunner + MineInstance

**MineRunner.TryUse**: `_mines.Count >= 5` 가드 → `MineInstance` 프리펩 `Instantiate(prefab, aim.AimPosition, rot)`.

**MineInstance (MonoBehaviour)**:
- `_armTimer` = 0.5s. 진행 중 본체 점멸 (색/Emission 보간).
- 활성화 후 매 프레임 `Physics.OverlapSphereNonAlloc(pos, 1.4f, buf, bugLayer)` — 하나라도 걸리면:
  ```csharp
  float radius      = bomb.EffectiveRadius * 0.5f;
  float bugDamage   = bomb.EffectiveDamage * 1.5f;   // 일반 bug
  float bossDamage  = bomb.EffectiveDamage * 2.0f;   // 보스 (v2.html:1259 준수)

  // 반경 내 모든 대상 damage
  Physics.OverlapSphereNonAlloc(pos, radius, buf, bugLayer);
  foreach (hit) {
      if (hit.TryGetComponent<BossController>(out var boss)) boss.TakeDamage(bossDamage);
      else if (hit.TryGetComponent<IDamageable>(out var dmg)) dmg.TakeDamage(bugDamage);
  }
  // VFX: Polygon Arsenal MiniExploFire 재사용 (scale = radius / basePrefabRadius)
  Instantiate(explosionPrefab, pos, Quaternion.identity);
  AudioManager.Instance?.PlayBombExplosion();
  Destroy(gameObject);
  ```
  > **보스 배율**: v2 원본 `checkBossHit(..., ws.bomb.dmg * 2)` 그대로 이식. BossController 여부로 분기 (없으면 일반 bug 로직만).
- `BombWeapon`이 씬에 없거나 해금 안 된 경우 → fallback으로 `AbilityData.Damage` / `AbilityData.Range` 사용 (이 경우 로그 경고).

**VFX**:
- **본체(설치)**: `Interactive/Zone/Glow/GlowZoneRed.prefab` — 작은 스케일로 Instantiate.
  - `armTimer > 0` 동안 렌더러 emission 약하게 (절반 색 / 점멸), 활성 후 강하게 전환 — MineInstance 스크립트에서 `Material.SetColor`로 토글.
- **폭발**: `Combat/Explosions/Mini/MiniExploFire.prefab` — 반경 비례 스케일 (`localScale *= _effectiveRadius / basePrefabRadius`).

### Step 6 — VFX 프리펩 세팅 (Polygon Arsenal 재사용)

| 어빌리티 / 용도 | 프리펩 경로 | 처리 |
|---|---|---|
| 화염방사기 | `Assets/Polygon Arsenal/Prefabs/Combat/Flamethrower/Spray/FlamethrowerSprayRed.prefab` | 그대로 사용, Runner가 매 프레임 `rotation` 갱신, 5초 후 Destroy |
| 네이팜 — 지면 베이스 | `Assets/Polygon Arsenal/Prefabs/Environment/FloorTrap/FloorTrapMolten.prefab` | 장판 중심에 1개. 길이에 맞게 비등방 스케일 (Z 배율) |
| 네이팜 — 불꽃 타일 | `Assets/Polygon Arsenal/Prefabs/Environment/Fire/OilFire/OilFireRed.prefab` | 길이축 따라 일정 간격(예: 길이/5)으로 N개 Instantiate, 장판 루트 자식 |
| 지뢰 — 본체(설치) | `Assets/Polygon Arsenal/Prefabs/Interactive/Zone/Glow/GlowZoneRed.prefab` | armTimer 중 emission 약/점멸, 활성 후 강 |
| 지뢰 — 폭발 | `Assets/Polygon Arsenal/Prefabs/Combat/Explosions/Mini/MiniExploFire.prefab` | 반경 비례 스케일 |

> **대안 검토용**: Pointy 변형(`FlamethrowerPointyRed`)은 좁은 빔. 다른 불꽃 색(`Yellow`, `Purple`)은 캐릭터 테마에 맞춰 추후 교체 가능. `MiniExploFire` 대신 더 묵직한 폭발 필요 시 `Combat/Surface Explosion/SurfaceExplosionStone.prefab` 사용.

> **Y축 유의**: Polygon Arsenal 프리펩 다수가 Y+ 방향으로 파티클을 쏘도록 설계됨. 지면에 붙는 프리펩(FloorTrap/Fire/OilFire/Glow)은 문제 없으나 `FlamethrowerSpray`는 **+Z forward**가 아닐 수 있음 → 실측 후 필요 시 Runner에서 `Quaternion.LookRotation(dir) * prefabLocalRot` 보정.

### Step 7 — AbilityData SO 에셋 바인딩

각 SO의 `_vfxPrefab` 슬롯에 Step 6 대표 프리펩 드래그 연결 (화염방사기=Spray, 네이팜=FloorTrap, 지뢰=GlowZone). 서브 프리펩(OilFire 타일, MiniExploFire)은 Runner가 직접 `Resources.Load` 하지 말고 **각 Runner에 public 필드로 노출해 인스펙터 바인딩**. `_useSfx`도 필요 시 같이.

### Step 8 — 테스트 시나리오

1. **Game 씬 단독 실행** → 1키: 머신 → 마우스 방향으로 직사각형 장판 20초. bug가 장판 위를 지나가면 HP 감소.
2. **2키**: 5초간 화염방사기. 마우스 회전 시 부채꼴이 실시간 추적. 범위 밖 bug는 무해.
3. **3키**: 마우스 위치에 지뢰 배치. 0.5s 동안 "준비 중" 시각 피드백. 활성화 후 bug 접근 시 폭발 + 광역 데미지. 동시 5개 제한.
4. **폭탄 업그레이드 연동**: 상점에서 폭탄 Damage/Radius Lv 올린 뒤 지뢰 폭발 → 반경·데미지가 커지는지 확인.
5. **해금 슬롯 가드**: `PlayerData.UnlockedAbilities`에 없는 슬롯 키를 눌러도 아무 일 없음.

---

## 4. 좌표계 체크리스트 (CLAUDE.md 준수)

- [x] `aim.AimPosition`은 이미 **Y=0 지면 평면**이므로 그대로 사용.
- [x] 방향 계산 시 `dir.y = 0f`로 강제 후 `Normalize`.
- [x] 회전은 항상 `Quaternion.LookRotation(dir, Vector3.up)` — Z+가 모델 forward.
- [x] 2D `atan2(dy, dx)` 포팅 시 `Vector3.SignedAngle(Vector3.forward, dir, Vector3.up)` 또는 `Vector3.Angle(a, b)` 사용. `Vector3.up` 쓰지 않기 위해 방향에만 XZ 성분만 쓸 것.
- [x] 월드 UI(지뢰 감지 링 등)는 Z로 오프셋, 회전 `Quaternion.Euler(90,0,0)` 고정.

---

## 5. 주요 리스크

| 리스크 | 대응 |
|---|---|
| v2 픽셀 수치(halfW=42, fLen=180 등)가 Unity 유닛 그대로 저장됨 → 3D에서 거대 | Step 8 플레이테스트 후 SO `_range` 일괄 재튜닝 (별도 튜닝 커밋) |
| `_damage` 해석이 ability마다 다름 (네이팜=틱당 0.5 / 화염=초당 10.8 dps / 지뢰=BombWeapon 실효값에 배율 ×1.5·보스 ×2) | Runner 상단에 `// 해석: 틱당 / 초당 / 회당` 주석 필수, SO description에도 명시 |
| 지뢰의 BombWeapon 의존이 `FindAnyObjectByType` → 씬에 없으면 null | AbilityContext 생성 시 1회 탐색 + 캐시, null이면 SO fallback + 경고 로그 |
| Input Actions 자산 수정 없이 `Keyboard.current` 직접 사용 | 의도된 타협. 키 리매핑 요구 생기면 `InputActionReference`로 교체 (후속) |
| VFX가 벌레 스프라이트에 가려짐 | 기존 `_crosshairHeight` 패턴 참고 — 지면+0.1f 띄우기 |

---

## 6. 작업 체크리스트

- [x] **Step 1** `AbilityContext.cs` / `IAbilityRunner.cs` / `AbilitySlotController.cs` 3파일
- [x] **Step 2** `BombWeapon.EffectiveDamage`, `EffectiveRadius` getter 2줄
- [x] **Step 3** `NapalmRunner.cs` (직사각형 판정 + `OverlapBoxNonAlloc`, 타일링 VFX)
- [x] **Step 4** `FlameRunner.cs` (부채꼴 각도 판정 + 매 프레임 추적)
- [x] **Step 5** `MineRunner.cs` + `MineInstance.cs` (BombWeapon 의존)
- [x] **Step 6** Polygon Arsenal 프리펩 바인딩 (FlamethrowerSprayRed / **OilFireRed 타일링** / GlowZoneRed / MiniExploFire)
- [x] **Step 7** Ability SO에 VFX 바인딩 (에디터 메뉴 자동화 — `MinePrefabCreator` / `NapalmVfxBinder`)
- [x] **Step 8** Game 씬 Play 테스트 + SO/상수 튜닝

---

## 10. 구현 결과 (2026-04-20, as-built)

### 10.1 생성/수정 파일

| 경로 | 타입 | 역할 |
|---|---|---|
| `Scripts/Ability/AbilityContext.cs` | 신규 | Runner 초기화 시 주입되는 참조 묶음 (Machine/Aim/BombWeapon/VfxParent/BugLayer) |
| `Scripts/Ability/IAbilityRunner.cs` | 신규 | 실행 계약: Type / Initialize / Tick / TryUse / CooldownNormalized |
| `Scripts/Ability/AbilitySlotController.cs` | 신규 | Game 씬용. 캐릭터 해결 + 3슬롯 Runner 인스턴스화 + Key.Digit1/2/3 입력 |
| `Scripts/Ability/Runners/NapalmRunner.cs` | 신규 | 직사각형 OBB 장판 + OilFireRed 타일링 VFX |
| `Scripts/Ability/Runners/FlameRunner.cs` | 신규 | 부채꼴 dps 빔 + 매 프레임 마우스 추적 + VFX rotation 갱신 |
| `Scripts/Ability/Runners/MineRunner.cs` | 신규 | 배치 수(5) 관리 + MineInstance 스폰 |
| `Scripts/Ability/Runners/MineInstance.cs` | 신규 | MonoBehaviour. armTimer 점멸 + OverlapSphere 탐지 + BombWeapon 의존 폭발 |
| `Scripts/Weapon/Bomb/BombWeapon.cs` | 수정 | `EffectiveDamage`·`EffectiveRadius` public getter 2줄 추가 |
| `Scripts/Editor/MinePrefabCreator.cs` | 신규 | 메뉴로 `MineInstance.prefab` 자동 생성 + Mine SO 자동 바인딩 |
| `Scripts/Editor/NapalmVfxBinder.cs` | 신규 | 메뉴로 Napalm SO의 `_vfxPrefab` 에 OilFireRed 자동 바인딩 |

### 10.2 최종 튜닝 값

#### AbilityData SO 수정값

| SO | 필드 | 값 | 주 |
|---|---|---|---|
| `Ability_Victor_Napalm` | `_range` | `4` | v2 halfW=42 픽셀 → Unity 4 유닛 (÷10) |
| `Ability_Victor_Napalm` | `_damage` | `0.5` | 0.1초 틱당 데미지 (v2 동일) |
| `Ability_Victor_Napalm` | `_cooldownSec`·`_durationSec` | `40`·`20` | v2 동일 |
| `Ability_Victor_Napalm` | `_vfxPrefab` | `OilFireRed.prefab` | NapalmVfxBinder 메뉴로 바인딩 |
| `Ability_Victor_Flame` | `_range` | `18` | 부채꼴 길이 (v2 180 ÷ 10) |
| `Ability_Victor_Flame` | `_damage` | `10.8` | **dps로 해석** (v2: 0.18/frame × 60) |
| `Ability_Victor_Flame` | `_angle` | `0.35` rad | ±20° (v2 동일) |
| `Ability_Victor_Flame` | `_cooldownSec`·`_durationSec` | `20`·`5` | v2 동일 |
| `Ability_Victor_Flame` | `_vfxPrefab` | `FlamethrowerSprayRed.prefab` | 인스펙터 수동 바인딩 |
| `Ability_Victor_Mine` | `_range`·`_damage` | `0`·`0` | 런타임에 BombWeapon 실효값 조회 (SO 값 사용 안 함) |
| `Ability_Victor_Mine` | `_cooldownSec` | `10` | v2 동일 |
| `Ability_Victor_Mine` | `_maxInstances` | `5` | v2 동일 |
| `Ability_Victor_Mine` | `_vfxPrefab` | `MineInstance.prefab` | MinePrefabCreator 메뉴로 자동 생성 + 바인딩 |

#### Runner 내부 상수 (코드 상수 — 플레이테스트로 결정)

| Runner | 상수 | 값 | 의미 |
|---|---|---|---|
| NapalmRunner | `LengthToHalfWidthRatio` | `10f` | length = halfW × 10 (v2는 ~20, 탑뷰 프레임에 맞게 축소) |
| NapalmRunner | `DamageTickInterval` | `0.1f` | v2 6프레임 = 0.1s |
| NapalmRunner | `ZoneHalfHeight` | `4f` | OverlapBox Y 여유 |
| NapalmRunner | `TileSpacingMultiplier` | `1f` | 타일 간격 = halfW × 1 (촘촘) |
| NapalmRunner | `TileScaleMultiplier` | `1f` | 각 타일 스케일 = halfW × 1 |
| MineInstance | `_armDuration` | `0.5s` | v2 30 frame |
| MineInstance | `_detectionRadius` | `1.4f` | v2 14 픽셀 ÷ 10 |
| MineInstance | 폭발 반경 | `bomb.EffectiveRadius × 0.5` | v2 동일 |
| MineInstance | 폭발 데미지 (bug) | `bomb.EffectiveDamage × 1.5` | v2 동일 |
| MineInstance | 폭발 데미지 (보스) | `bomb.EffectiveDamage × 2.0` | v2.html:1259 (`checkBossHit(..., ws.bomb.dmg*2)`) |

### 10.3 에디터 메뉴

| 메뉴 | 기능 |
|---|---|
| `Tools/Drill-Corp/3. 게임 초기 설정/7. 빅터 지뢰 프리펩 생성` | `Assets/_Game/Prefabs/Abilities/MineInstance.prefab` 생성 (Body = GlowZoneRed 복제, 90°X 회전, scale 0.5). MineInstance 필드 자동 바인딩. Mine SO `_vfxPrefab` 자동 할당 |
| `Tools/Drill-Corp/3. 게임 초기 설정/8. 빅터 네이팜 VFX 바인딩` | Napalm SO `_vfxPrefab` 에 `Assets/Polygon Arsenal/Prefabs/Environment/Fire/OilFire/OilFireRed.prefab` 자동 할당 |

### 10.4 구현 중 발견/해결한 이슈

| 이슈 | 원인 | 해결 |
|---|---|---|
| 네이팜 사이즈가 엄청 큼 | SO `_range: 42` (v2 픽셀 그대로) | `_range: 4` 로 재튜닝 (Flame의 180→18 비율과 일치) |
| 네이팜 VFX 3초 만에 사라짐 | FloorTrapMolten 은 `looping=false, lengthInSec=3` authoring | NapalmRunner가 인스턴스화 직후 자식 ParticleSystem 전부 `main.loop = true` 강제 |
| 네이팜 모양이 정사각형 | 단일 FloorTrapMolten 은 원형 + 비등방 스케일 금지(자식 90°X 회전으로 shear) | OilFireRed **타일링 방식**으로 전환 (길이축으로 N개 복제) |
| 지뢰가 세로로 선다 | GlowZoneRed 프리펩이 XY 평면 authoring | MinePrefabCreator 가 Body 자식에 `Quaternion.Euler(90, 0, 0)` 적용해 바닥 평면화 |
| 지뢰 폭발 반경·데미지가 폭탄 강화를 못 따라감 | `BombWeapon._effectiveDamage/_effectiveRadius` private | public getter 2줄 공개 (SSoT 유지, 중복 계산 방지) |
| 화염방사기 매 프레임 재계산 부담 | OverlapSphere 후 각도 필터 | 반경(18)이 작아 후보 수 적음 + 단일 인스턴스 + `_overlapBuffer[64]` NonAlloc — 무시 가능 |

### 10.5 좌표계 검증 (CLAUDE.md 준수)

- **물리**: 3 Runner 전부 XZ 평면 가정 (`dir.y = 0f` 강제, `OverlapBox(..., rotation)` / `OverlapSphere` + 각도 필터).
- **VFX 회전**:
  - FlamethrowerSprayRed: Cone Shape +Z forward → `Quaternion.LookRotation(dir, Vector3.up)` 정상 ✅
  - OilFireRed (네이팜 타일): wrapper GO를 dir로 회전, 타일은 wrapper 로컬 +Z 따라 배치 ✅
  - GlowZoneRed (지뢰 Body): XY 세로 authoring → 90°X 회전으로 바닥 평면화 ✅
  - MiniExploFire (폭발): 방사상 효과 → `Quaternion.identity` 무관 ✅

### 10.6 Deferred (후속 작업)

- **UI**: 인게임 어빌리티 슬롯 HUD — **§11 에 별도 구현 계획**. `AbilitySlotController.GetRunner(slotKey).CooldownNormalized` 를 소비.
- **보스 판정 교체**: `MineInstance.IsBoss` 가 현재 `CompareTag("Boss")`. BossController 실구현 시 `TryGetComponent<BossController>` 로 교체.
- **Sara/Jinus 6개 어빌리티**: `AbilityType.BlackHole` ~ `SpiderDrone` — `AbilitySlotController.CreateRunner` switch 의 default 분기.
- **AbilitySlotController 씬 배치 자동화**: 현재 사용자가 수동으로 빈 GO 생성 + 컴포넌트 부착. Title/Game 씬 Setup Editor 에 편입 여지.
- **오디오**: `AbilityData._useSfx` 필드는 있지만 Runner들이 아직 `AudioManager` 호출 안 함.
- **지뢰 플레이스먼트 feedback**: 마우스 위에 "설치 가능" 고스트 프리뷰가 없음 (v2 도 없음이라 Deferred).

---

## 11. 인게임 어빌리티 HUD (v2 `drawItemUI` 포팅 계획)

### 11.1 v2 원본 UI 분석 (v2.html:1557~1593)

```js
function drawItemUI(){
  // ① 캐릭터 이름 — 좌상단
  var ch = CHARACTERS[save.character || 'victor'];
  ctx.fillStyle = ch.color + 'cc';          // 테마 컬러 + alpha 0x80
  ctx.font = '500 11px sans-serif';
  ctx.fillText(ch.name, 10, 18);

  // ② 소유한 어빌리티만 필터 → owned[] 구성
  //    각 항목: { id, key: '1'/'2'/'3'/'자동', name, color, cdMax, st:{cd} }

  // ③ 우측 상단 스택 — y=28 부터 아래로
  var y = 28;
  owned.forEach(function(item){
    var bw = 90, bh = 34, x = W - bw - 8;

    // 배경 + 테두리
    ctx.fillStyle = 'rgba(10,10,30,0.75)';
    ctx.roundRect(x, y, bw, bh, 5);  ctx.fill();
    ctx.strokeStyle = item.color + '88';
    ctx.roundRect(x, y, bw, bh, 5);  ctx.stroke();

    // 상단 라벨 "[1] 네이팜"
    ctx.fillStyle = item.color;
    ctx.font = '500 10px sans-serif';
    ctx.fillText('[' + item.key + '] ' + item.name, x+5, y+13);

    // 쿨다운 바 (하단, bw-8 × 5)
    var cdPct = item.st.cd > 0 ? 1 - (item.st.cd / item.cdMax) : 1;
    ctx.fillStyle = 'rgba(255,255,255,0.1)';               // 바 배경
    ctx.roundRect(x+4, y+18, bw-8, 5, 2);  ctx.fill();
    ctx.fillStyle = cdPct >= 1 ? '#51cf66' : item.color;   // 준비 완료=초록 / 쿨중=item color
    ctx.roundRect(x+4, y+18, (bw-8)*cdPct, 5, 2);  ctx.fill();

    // 상태 텍스트 (우측 정렬, 흰색 55%)
    ctx.fillStyle = 'rgba(255,255,255,0.55)';
    ctx.font = '9px sans-serif';
    ctx.textAlign = 'right';
    ctx.fillText(cdPct >= 1 ? '사용가능' : (Math.ceil(item.st.cd/60) + 's'),
                 x + bw - 4, y + 30);

    y += bh + 5;
  });
}
```

### 11.2 핵심 시각 스펙

| 요소 | 값 |
|---|---|
| 슬롯 박스 | 90 × 34 px, 둥근 radius 5, 배경 `rgba(10,10,30,0.75)`, 테두리 `itemColor` @ alpha 0.53 |
| 상단 라벨 | `[KEY] 이름`, 색 = `itemColor`, 500 weight 10px |
| 쿨다운 바 | 가로 `bw-8 × 5`, 진행도 `1 - (cd/cdMax)` (0→1 차오름), **준비완료 시 초록 `#51cf66`**, 쿨중 `itemColor` |
| 상태 텍스트 | `사용가능` / `3s` (ceil(cd/60) 초 단위), 우측 정렬, 흰 55% |
| 스택 | y=28 시작, 세로로 `bh+5 = 39px` 간격 |
| 위치 | 화면 우측 상단 (`x = W - 90 - 8`) |
| 캐릭터 이름 | 화면 **좌**상단(10, 18), `character.ThemeColor` @ alpha 0.8 |

### 11.3 키 매핑 / 자동 트리거 표시

| 트리거 | 키 라벨 |
|---|---|
| `AbilityTrigger.Manual` | `1` / `2` / `3` — `AbilityData.SlotKey` 그대로 |
| `AbilityTrigger.AutoInterval` | `자동` (Meteor, SpiderDrone) |

### 11.4 쿨다운 바 수식 ↔ Unity 매핑

| v2 | Unity |
|---|---|
| `cdPct = 1 - cd/cdMax` | `fillAmount = 1f - runner.CooldownNormalized` |
| `Math.ceil(cd/60) + 's'` | `Mathf.CeilToInt(runner.CooldownNormalized × Data.CooldownSec) + "s"` |
| `cd <= 0` 체크 | `runner.CooldownNormalized <= 0f` (이때 "사용가능") |

> `IAbilityRunner.CooldownNormalized` 는 이미 [0,1] 정규화 — UI 쪽에서 `AbilityData.CooldownSec` 와 곱해서 초 단위 환산.

### 11.5 아이콘 에셋 (빅터 3종, 준비 완료)

```
Assets/_Game/Sprites/UI/drillcorp_victor_abilities/
├── 64px/   ├── 1_napalm.png  ├── 2_flamethrower.png  └── 3_mine.png
├── 128px/  ├── 1_napalm.png  ├── 2_flamethrower.png  └── 3_mine.png   ← HUD 권장
└── 256px/  ├── 1_napalm.png  ├── 2_flamethrower.png  └── 3_mine.png
```

**HUD 슬롯 크기(≈68~90px)** 기준으로 **128px 해상도 권장** (고DPI 대비). 바인딩 대상은 기존 `AbilityData._icon` (타입 `Sprite`) — 이미 SO에 필드만 있고 비어있음.

**매핑**:
| SO | 아이콘 파일 |
|---|---|
| `Ability_Victor_Napalm.asset` | `.../128px/1_napalm.png` |
| `Ability_Victor_Flame.asset` | `.../128px/2_flamethrower.png` |
| `Ability_Victor_Mine.asset` | `.../128px/3_mine.png` |

> **에디터 자동 바인딩 권장** — 3개 수동 드래그도 가능하지만 `VictorAbilityIconBinder.cs` 같은 간단한 메뉴 스크립트로 처리 (기존 `NapalmVfxBinder` 패턴 확장).

### 11.6 Unity 구현 방안 (UGUI 권장)

기존 `TopBarHud.cs` 패턴 참조 — Canvas + RectTransform + TextMeshPro + Image(bar bg/fill).

#### 파일 구조 (신규)

```
Assets/_Game/Scripts/UI/HUD/
├── AbilityHud.cs              — 상위 컨테이너. AbilitySlotController 찾아 3슬롯 바인딩
└── AbilitySlotUI.cs           — 슬롯 1개의 UI. runner 참조해 매 프레임 갱신

Assets/_Game/Scripts/Editor/
└── AbilityHudSetupEditor.cs   — 메뉴로 Canvas 에 HUD GameObject 자동 생성·레이아웃
```

#### AbilityHud.cs (개요)

```csharp
public class AbilityHud : MonoBehaviour
{
    [SerializeField] private AbilitySlotUI[] _slots;   // 길이 3
    [SerializeField] private TextMeshProUGUI _characterNameLabel;
    [SerializeField] private AbilitySlotController _controller;   // 비우면 FindAnyObjectByType

    void Start() {
        if (_controller == null) _controller = FindAnyObjectByType<AbilitySlotController>();
        var character = _controller?.ResolvedCharacter;
        if (character != null) {
            _characterNameLabel.text = character.DisplayName;
            _characterNameLabel.color = new Color(
                character.ThemeColor.r, character.ThemeColor.g, character.ThemeColor.b, 0.8f);
        }
        for (int i = 0; i < 3; i++) {
            var runner = _controller?.GetRunner(i + 1);
            var data   = character?.GetAbility(i + 1);
            _slots[i].Bind(runner, data, character?.ThemeColor ?? Color.white);
        }
    }

    void Update() { for (int i=0; i<_slots.Length; i++) _slots[i].Refresh(); }
}
```

#### AbilitySlotUI.cs (개요)

```csharp
public class AbilitySlotUI : MonoBehaviour
{
    [SerializeField] private Image _background;   // rgba(10,10,30,0.75)
    [SerializeField] private Image _border;       // itemColor @ 0.53
    [SerializeField] private Image _iconImage;    // AbilityData.Icon (128px)
    [SerializeField] private Image _cooldownBarBg;
    [SerializeField] private Image _cooldownBarFill;   // fillAmount 사용, Type=Filled
    [SerializeField] private TextMeshProUGUI _nameLabel;
    [SerializeField] private TextMeshProUGUI _statusLabel;

    IAbilityRunner _runner;
    AbilityData _data;

    static readonly Color ReadyGreen = new Color(0x51/255f, 0xcf/255f, 0x66/255f);

    public void Bind(IAbilityRunner runner, AbilityData data, Color characterColor) {
        _runner = runner; _data = data;
        gameObject.SetActive(runner != null && data != null);
        if (data == null) return;

        _border.color = WithAlpha(HexColor(data), 0.53f);
        _iconImage.sprite = data.Icon;
        _iconImage.enabled = data.Icon != null;
        string key = data.Trigger == AbilityTrigger.AutoInterval
            ? "자동"
            : data.SlotKey.ToString();
        _nameLabel.text = $"[{key}] {data.DisplayName}";
        _nameLabel.color = HexColor(data);
    }

    public void Refresh() {
        if (_runner == null || _data == null) return;
        float norm = _runner.CooldownNormalized;   // 1=방금사용, 0=준비
        float pct  = 1f - norm;
        _cooldownBarFill.fillAmount = pct;
        bool ready = norm <= 0f;
        _cooldownBarFill.color = ready ? ReadyGreen : HexColor(_data);
        _statusLabel.text = ready
            ? "사용가능"
            : $"{Mathf.CeilToInt(norm * _data.CooldownSec)}s";
    }
}
```

> **어빌리티별 색** (v2 `item.color` → Unity): `#ff6b35` 네이팜/화염, `#ffd700` 지뢰, `#9c6fff` 블랙홀, `#4fc3f7` 충격파/드론, `#51cf66` 채굴드론, `#ff4488` 메테오, `#88ddff` 거미드론. 이 값들은 **AbilityData SO 에 `_themeColor` 필드를 추가**하거나 `_iconEmoji` 옆에 `Color _color` 필드를 신설해 저장 권장 (현재 SO는 색 필드 없음).

### 11.7 v2와의 차이 / 결정 사항

| 항목 | v2 | Unity 포팅 |
|---|---|---|
| 쿨다운 단위 | frame (`/60`) | 초 (그대로) |
| "자동" 표기 | 하드코딩 (meteor/spiderdrone) | `AbilityData.Trigger == AutoInterval` 체크 |
| 어빌리티 색 | 하드코딩 `item.color` | **SO에 `_themeColor` 필드 신설 필요** (또는 CharacterData.ThemeColor 공통 사용) |
| 해금 안 된 슬롯 | 아예 표시 안 함 | `AbilitySlotUI.gameObject.SetActive(runner != null)` (즉, Runner 생성 안 된 슬롯 숨김) |
| 캐릭터 이름 표시 | 좌상단 | **동일** (기존 TopBarHud 좌측과 충돌 여부 체크 필요 — 좌상단 10, 18은 TopBarHud의 HP 슬롯 영역일 가능성) |

> **레이아웃 충돌 주의**: 기존 `TopBarHud` 가 화면 상단 stretch 바를 점유. AbilityHud 는 **우측 상단 세로 스택**(y=28부터)이라 물리적으로 TopBarHud 아래 영역. 캐릭터 이름만 좌상단으로 두면 TopBarHud 와 겹침 → **캐릭터 이름은 우측 첫 슬롯 위에 작게 표시**하거나 TopBarHud 에 통합하는 게 나음.

### 11.8 작업 체크리스트

- [x] **Step 9-1** `Scripts/Data/AbilityData.cs` 에 `_themeColor` 필드 추가 + 9개 SO 색 세팅 (v2 색표 그대로)
- [~] **Step 9-2** ~~`VictorAbilityIconBinder.cs`~~ → **불필요** (사용자가 PNG Sprite 변환 + SO `_icon` 직접 바인딩 완료)
- [x] **Step 9-3** `Scripts/UI/HUD/AbilityHud.cs` / `AbilitySlotUI.cs` 구현 (아이콘 Image 슬롯 포함)
- [x] **Step 9-4** `Scripts/Editor/AbilityHudSetupEditor.cs` — Canvas 에 HUD 자동 생성 메뉴 (TopBarHudSetupEditor 패턴)
- [ ] **Step 9-5** Game 씬에 AbilityHud 배치 (에디터 메뉴로 자동) + 수동 플레이 검증 ← **사용자 작업**
- [x] **Step 9-6** TopBarHud 좌측에 `CharacterSlot` 흡수 (충돌 회피, 일원화)
- [x] **메뉴 트리 정리** — 빅터 전용 메뉴는 `Tools/Drill-Corp/3. 게임 초기 설정/빅터/{1,2}` 서브메뉴로 이동
  - `1. 지뢰 프리펩 생성` (MinePrefabCreator) — 빅터 전용 (지뢰 본체 프리펩)
  - `2. 네이팜 VFX 바인딩` (NapalmVfxBinder) — 빅터 전용 (Napalm SO `_vfxPrefab` 바인딩)
  - **AbilityHudSetupEditor**는 캐릭터 중립이므로 `Drill-Corp/HUD/Build Ability HUD` 공용 위치 (TopBar 메뉴와 동일 카테고리)

### 11.9 v2 원본 스크린샷 기준 해상도

v2 canvas는 700×520 기준. Unity는 1920×1080. **UI 좌표를 비율 변환 필요**: 슬롯 박스 90×34 → Unity 에서는 대략 `180 × 68` 정도 (2배). TextMeshPro 폰트 `font size = 18~20`. 실제 수치는 플레이테스트로 결정.

---

## 7. 참고 문서

- [CharacterAbilitySystem.md](CharacterAbilitySystem.md) §4~§5.3 — Ability 아키텍처 + 빅터 스펙
- [VFX_3D_MigrationPlan.md](VFX_3D_MigrationPlan.md) — 2D→3D VFX 컨벤션(Scale 규칙, AutoDestroyPS)
- [Phase2_Bomb_Plan.md](Phase2_Bomb_Plan.md) — `BombWeapon`/`BombProjectile`/`OverlapSphere` 참고 구현
- [WeaponUnlockUpgradeSystem.md](WeaponUnlockUpgradeSystem.md) — 지뢰가 파급 받는 폭탄 강화 시스템
- `docs/v2.html:1054~1263, 1554, 1724, 1749` — useItem/tick/draw 원본

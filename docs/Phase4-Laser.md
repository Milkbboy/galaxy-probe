# Phase 4 — 레이저 무기 구현

> 상위 문서: [archive/WEAPON_IMPLEMENTATION_PLAN.md](archive/WEAPON_IMPLEMENTATION_PLAN.md) §7
> **상태**: ✅ **구현 완료 / 머지됨** (2026-04-16) — 폴리싱 단계 진입. 현행 시스템: [Sys-Weapon.md](Sys-Weapon.md)
> **원본 기준**: `docs/archive/_v1prototype.html` L166·L188·L269·L281·L293·L294·L307·L313 — `fireLaser` / `laserBeams` / `updateWeaponUI` / `drawCrosshair` / `drawLasers`

## 🎉 구현 결과 (2026-04-16)

### 신규 파일 5개
| 파일 | 역할 |
|------|------|
| `Assets/_Game/Scripts/Weapon/Laser/LaserWeaponData.cs` | SO — `_cooldown=5` / `_beamDuration=6` / `_beamSpeed=1.725` / `_beamRadius=1.0` / `_tickInterval=0.1` / `_stopDistance=0.033` / `_beamPrefab` (`_fireDelay=0` 강제) |
| `Assets/_Game/Scripts/Weapon/Laser/LaserBeam.cs` | 자립형 빔 — XZ 추적, OverlapSphere 틱 데미지, 9개 자식 레이어 갱신 |
| `Assets/_Game/Scripts/Weapon/Laser/LaserWeapon.cs` | WeaponBase 파생 — 자체 Update 루프 + `ShouldFire` 게이팅 + UI 오버라이드 + `CrosshairRingFill` |
| `Assets/_Game/Scripts/Aim/LaserAimRingBinder.cs` | 크로스헤어 쿨 호 바인더 — `CrosshairRingFill` 사용, 색 LaserRed 고정 |
| `Assets/_Game/Scripts/Editor/LaserPrefabCreator.cs` | 메뉴 8번 — 스프라이트·머티리얼·프리펩(9 자식)·SO 일괄 생성 |

### 신규 자산
- `Assets/_Game/Prefabs/Weapons/LaserGlow.png` — 128×128 흰색 라디얼 그라디언트
- `Assets/_Game/Prefabs/Weapons/Laser_LifeArc.mat` — Sprites/Default 머티리얼
- `Assets/_Game/Prefabs/Weapons/LaserBeam.prefab` — 9개 자식 레이어 풀세트
- `Assets/_Game/Data/Weapons/Weapon_LaserBeam.asset` — LaserWeaponData SO

### 빔 프리펩 최종 구조 (root scale ×2)
```
LaserBeam (Root, X+90 회전, scale 2)
├── OuterGlow      LR 링  r+0.13, 굵기 0.10, 빨강 α0.12×pulse
├── RingStroke     LR 링  r+0.05, 굵기 0.03, 분홍 α0.60         ← 데미지 범위 경계 UX
├── Core           SR 채움 r,      빨강 α0.35×pulse
├── CoreStroke     LR 링  r,      굵기 0.025, 빨강 α0.90         ← 본체 윤곽 (프로토 ctx.stroke)
├── Center         SR 채움 r×0.45, 분홍 α0.70
├── CrosshairH     LR 직선 ±cr 가로, 굵기 0.018, 분홍 α0.70      ← 십자(프로토 lb.r+4)
├── CrosshairV     LR 직선 ±cr 세로, 굵기 0.018, 분홍 α0.70
├── CenterParticles ParticleSystem, 분홍 스파클 200/s              ← 프로토 L294 자체 fx
└── LifeArc        LR 호  r+0.2,  굵기 0.04, 진홍 α0.80, 시계방향
```

### 계획서 대비 변경/보강 사항
1. **`WeaponBase` virtual 승격 취소** — 게이팅은 `ShouldFire` 오버라이드만으로 충분 (`FireDelay=0`으로 베이스 1차 게이트 무력화). 베이스 코드 무수정.
2. **빔 수명 페이드 제거** — 사용자 결정으로 `alpha = life/maxLife` 곱셈 제외 (빔이 끝까지 또렷)
3. **빔 반경 0.48 → 1.0** + **root scale ×2** — 시각/플레이감 위해 키움
4. **3가지 fx 추가** (계획서 미포함, 폴리싱 차원으로 합류):
   - **CoreStroke**: Core 외곽 진한 빨간 링 (프로토 alpha 0.9, 빔 윤곽 선명도 핵심)
   - **Crosshair (가로/세로)**: 분홍 십자 (프로토 lb.r+4, 빔 정체성)
   - **CenterParticles**: ParticleSystem 분홍 스파클 (프로토 L294 update 내 매 프레임 2개 파티클)
5. **좌표계 수정** — LifeArc 호 시작 각도 `-π/2` (캔버스 기준) → **`+π/2`** (Unity 탑뷰 XZ 평면 기준). 메모리 `feedback_topdown_proto_conversion.md` 추가됨.

### 단계 5(씬 통합) 상태
- LaserWeapon GameObject 추가 ✅
- LaserAimRingBinder 부착 ✅
- WeaponPanelUI 슬롯 4 등록 ✅
- 런타임 검증 ✅ — 빔 스폰/추적/데미지/소멸/쿨 사이클 확인됨

---

## 0. 구현 목표 요약

마우스 위치에 **붉은 원형 빔**이 자동 스폰되어 6초간 마우스를 느리게 추적하며 0.1초마다 범위 내 벌레에 지속 피해. 빔 수명 종료 시 5초 쿨다운. 메인 무기(저격총)·폭탄·기관총과 병렬 동작.

**목표 수치** (`_.html` 60fps 기준 환산 → `Weapon_Laser.asset`):

| 항목 | 프로토타입 | Unity 환산 | 비고 |
|------|-----------|-----------|------|
| 쿨다운 (CD) | 300프레임 | **5.0s** | 빔 스폰 시 즉시 풀로 세팅 |
| 빔 수명 (Duration) | 360프레임 | **6.0s** | 빔 스폰 시 `life=maxLife` |
| 추적 속도 (Speed) | 1.725px/frame | **1.725 u/s** | ≈ 103.5px/s ÷ 60 (프로토 px→유닛 1:1) |
| 피격 반경 (Range) | 28.8px | **0.48 u** | `lb.r` — 빔 중심 기준 OverlapSphere |
| 틱 데미지 (Damage) | 0.8/tick | **0.8** | tick 1회당 |
| 틱 간격 (TickInterval) | 6프레임 | **0.1s** | DPS = 8 /초 |
| 멈춤 거리 (StopDistance) | 2px | **0.033 u** | 이 거리 이내면 추적 정지 (미세 지글링 방지) |
| 테마색 | `#ff1744` | 진홍색 | 테두리·오버레이·게이지 배경 |
| Active 바 색 | `#ff6090` | 연분홍 | 빔 활성 중 UI 전용 |

**동작 사이클**: 빔 생성(6s 활성) → 빔 소멸 → 쿨다운(5s) → 다음 빔 = **총 11s 주기**, 발사 가동률 ≈ 54%.

**시각 피드백 이중 분리** (§1.5 상세): 프로토 원본은 쿨타임과 수명을 두 개의 공간적으로 분리된 원으로 표시한다.
- 🔴 마우스 크로스헤어 주변 호 = **쿨타임** (Active 시 숨김, Cooling 시 차오름)
- 🔴 빔 자체 주변 호 = **빔 수명** (`life/maxLife`로 줄어듬, 빔과 함께 이동/소멸)
- 🔴 빔 자체 알파 = `life/maxLife`로 페이드 — 수명이 줄수록 빔이 희미해짐

---

## 1. 구현 결정 사항

### 1.1 보조 무기 패턴 (Machine Gun / Bomb과 동일)
- **메인 무기 슬롯 외부에서 자체 구동** — `AimController.EquipWeapon` 경로 거치지 않음
- `LaserWeapon.Update()`에서 매 프레임 `TryFire(_aimController)` 호출
- 메인(저격총)·폭탄·기관총과 병렬 동작
- 레벨업으로 해금될 보조 무기 슬롯 (슬롯 3번 또는 4번 예정)

### 1.2 "단 하나의 활성 빔" 게이트 (프로토 L293 충실 구현)
```js
if(unlocked.laser && laserBeams.length===0) {
    if(laserCD>0) laserCD-=dt;
    if(laserCD<=0 && laserCD>-1) fireLaser();
}
```
- 빔 존재 중에는 **쿨다운 타이머가 흐르지 않음** → 빔 수명(6s) + 쿨다운(5s) 분리됨
- 빔이 소멸하는 순간부터 `laserCD`가 감소
- Unity에서도 동일하게 — `_laserCD` 필드를 Update에서만 감소, 빔 활성 중엔 건드리지 않음

### 1.3 쿨다운 관리 — `WeaponBase._nextFireTime` 미사용
- 저격·기관총은 `_nextFireTime = Time.time + FireDelay` 패턴이지만 레이저는 **빔 수명 + 쿨다운** 두 단계
- `LaserWeapon` 자체에 `_laserCD` / `_activeBeam` 필드 보유, `CanFire`/`CooldownRemaining` 오버라이드
- `BaseData.FireDelay`는 참고만 하고 실제 흐름 제어는 `LaserWeapon` 자체 상태 머신으로

### 1.4 상태 3분기 (UI 통합 공식)
프로토 `updateWeaponUI` L280-281 재현 — 슬롯 바·에임 호·테두리 색이 상태 따라 전환:

| 상태 | 조건 | `BarFillAmount` | `BarColor` | `StateText` | `ShowOverlay` | `BorderColor` |
|------|------|-----------------|-----------|-------------|--------------|--------------|
| **Active** | 빔 존재 | `life/maxLife` (감소) | `#ff6090` 연분홍 | `"5.2s"` (빔 남은수명) | false | `#ff1744` 진홍 |
| **Cooling** | 빔 없음 + `_laserCD>0` | `1 - _laserCD/cd` (차오름) | `#ff1744` 진홍 | `"3.0s"` (쿨) | **true** (카운트다운) | `IdleBorderColor` |
| **Ready** | 빔 없음 + `_laserCD<=0` | `1` 고정 | `ReadyBarColor` 초록 | `"자동발사"` | false | `#ff1744` 진홍 |

### 1.5 ⚠️ 두 개의 독립된 원 (프로토 L307·L313 충실 해석)

**프로토 원본은 쿨타임 호와 수명 호를 공간적으로 분리해서 정보를 이중 표시한다.** 이를 하나로 통합하면 정보 하나가 사라지므로 **분리 유지**.

| 원 | 중심 | 반경 | 진행도 | 프로토 소스 | 구현 위치 |
|----|------|------|--------|-------------|-----------|
| **① 크로스헤어 쿨 호** | **마우스**(에임) | 저격 링 바깥 | `1 - _laserCD/cd` (쿨타임만) | L313 `drawCrosshair` 끝 | 기존 `AimWeaponRing` 재사용 |
| **② 빔 수명 호** | **빔 위치**(빔과 함께 이동) | `beamRadius + 0.2` | `life/maxLife` (수명) | L307 `drawLasers` 끝 | 빔 프리펩 자식 `LineRenderer` |

#### ① 크로스헤어 쿨 호 (`LaserAimRingBinder`)
- `BombAimRingBinder`/`MachineGunAimRingBinder` 패턴 동일
- 쿨중: `FillAmount = 1 - _laserCD/cd` (차오름), 색 `#ff1744`
- **빔 활성 중: `FillAmount = 0`** — 호 숨김 (프로토 L293 `laserCD`가 멈춰있으므로 `lp=0`)
- Ready: `FillAmount = 1` 고정, 색 `#ff1744`
- radiusOffset = **0.4** (저격 0.08, 폭탄 0.2, 기관총 0.3 다음 자리)
- `LaserWeapon`에 슬롯 바와 **분리된** 프로퍼티 `CrosshairRingFill` 노출 (§3.3 표 참조)

#### ② 빔 수명 호 (빔 자식 `LineRenderer`)
- 빔 프리펩 **자식**으로 포함 → 빔과 함께 이동·소멸 (씬에 남지 않음)
- `LineRenderer`로 원 둘레 그림, `positionCount` = 64 × `life/maxLife` 로 호 진행도 표현
- 대안: `AimWeaponRing` 복제 및 "고정 외부 반경" 모드 추가. 다만 `AimWeaponRing`은 `AimController.AimRadius`를 참조하므로 빔 로컬 좌표계에 맞지 않음 → **`LineRenderer` 권장**
- 색 = 진홍 `#ff1744`, 알파 0.8 × `life/maxLife` (빔 자체 알파 페이드와 동조)

### 1.6 적 유무 무관
- `ShouldFire` — 타겟 체크 없음, `_activeBeam==null && _laserCD<=0`만
- `IsHittingTarget` — 빔 활성 중이면 `true` (크로스헤어/테두리 활성 표시용)

### 1.7 빔 시각화 (프로토 L307 충실 구현)
`LaserBeam` 단일 GameObject에 **4겹 SpriteRenderer 동심원** 또는 단일 커스텀 메시로 구현:
- 가장 바깥 (`r+0.13`) — `#ff1744` 12% 알파, 펄스(sin × 0.3)
- 링 스트로크 (`r+0.05`) — `#ff6090` 60% 알파
- 코어 (`r`) — `#ff1744` 35% 알파, 펄스
- 중앙 (`r*0.45`) — `#ffc8d2` 70% 알파
- **수명 호 (`r+0.2`, `LineRenderer` 자식) — §1.5 ②** : `positionCount`를 매 프레임 `life/maxLife`로 재계산
- **빔 자체 `alpha = life/maxLife` 곱 → 소멸 직전 자연스럽게 페이드** (= 프로토 "레이저 양이 줄어드는" 표현의 실체)

### 1.8 기존 Heat 기반 레거시와 공존
현재 리포에 존재하는 다른 패러다임 구현(Phase 3 Weapons 사이클 잔재, commit `60fcc1f`):
- `Scripts/Weapon/Laser/LaserBeamWeapon.cs` (Heat 시스템)
- `Scripts/Weapon/Laser/LaserBeamData.cs` (MaxHeat/OverheatLockTime 등)
- `Scripts/Weapon/Laser/LaserBeamField.cs` (스프라이트 지면 필드)
- `Scripts/Editor/LaserBeamFieldCreator.cs`
- `Prefabs/Weapons/LaserBeamField.prefab`
- `Data/Weapons/Weapon_Laser.asset` (LaserBeamData 타입)

**방침: 전부 보존** — 추후 취합·정리 시 일괄 판단. 신규 구현은 이름을 다르게 잡아 충돌만 회피.

**충돌 회피표**:
| 유형 | 레거시 | 신규 | 충돌 여부 |
|------|--------|------|-----------|
| 클래스 | `LaserBeamWeapon` | `LaserWeapon` | ✅ 다름 |
| 클래스 | `LaserBeamData` | `LaserWeaponData` | ✅ 다름 |
| 클래스 | `LaserBeamField` | `LaserBeam` | ✅ 다름 |
| 네임스페이스 | `DrillCorp.Weapon.Laser` | `DrillCorp.Weapon.Laser` | 공존 OK |
| 프리펩 | `LaserBeamField.prefab` | `LaserBeam.prefab` | ✅ 다름 |
| SO 에셋 | `Weapon_Laser.asset` | **`Weapon_LaserBeam.asset`** | ⚠ 신규 이름 변경 |
| 에디터 | `LaserBeamFieldCreator.cs` | `LaserPrefabCreator.cs` | ✅ 다름 |

레거시 코드는 씬 미참조 상태로 dormant — 컴파일만 통과, 런타임 영향 0. `WeaponGauge`는 `BurstGun/LockOn/Shotgun`에서 계속 사용되므로 당연히 유지.

---

## 2. 파일 계획

### 2.1 신규 파일 (5개)

| 파일 | 역할 |
|------|------|
| `Assets/_Game/Scripts/Weapon/Laser/LaserWeaponData.cs` | SO — cd/dur/speed/range/damage/tickInterval + beam 프리펩 참조 |
| `Assets/_Game/Scripts/Weapon/Laser/LaserWeapon.cs` | WeaponBase 파생 — 자체 Update + 자동 스폰 + 빔 수명/쿨다운 상태 머신 + UI 오버라이드 |
| `Assets/_Game/Scripts/Weapon/Laser/LaserBeam.cs` | 자립형 빔 오브젝트 — XZ 추적 이동, OverlapSphere 틱 데미지, 수명 만료 시 자파괴 |
| `Assets/_Game/Scripts/Aim/LaserAimRingBinder.cs` | 에임 호 — `BarFillAmount` + `BarColor` 푸시 (기관총 바인더 패턴) |
| `Assets/_Game/Scripts/Editor/LaserPrefabCreator.cs` | 에디터 도구 — 메뉴 8번으로 스프라이트 + 프리펩 + 머티리얼 + SO 일괄 생성 |

### 2.2 수정 파일

| 파일 | 변경 내용 |
|------|----------|
| `Assets/_Game/Scripts/Weapon/WeaponSwitcher.cs` | (선택) 슬롯 4번에 `LaserWeapon` 참조 추가. 보조 무기는 Switcher 밖에서 동작하므로 필수 아님. |
| `Assets/_Game/Scripts/Machine/TurretController.cs` | (선택) `Barrel_Laser`(빨간 Cube, 위쪽 배치) — 리팩토링 플랜 트랙 2가 선행되면 그때 함께. 현 Phase 4 범위에선 단독 배럴 추가만. |

> **`WeaponBase.cs`는 수정하지 않는다**. `CanFire`/`CooldownRemaining`/`CooldownProgress`를 virtual로 승격할 필요 없음. 레이저의 실제 게이팅은 `ShouldFire` 오버라이드(`_activeBeam == null && _laserCD <= 0f`)가 전담하고, 베이스 `CanFire`는 `FireDelay=0`일 때 항상 `true`를 돌려주므로 `TryFire`의 1차 게이트를 그냥 통과시킨다. `CanFire`/`CooldownProgress`의 값이 레이저 컨텍스트에서 "의미상 틀리게" 나오지만 외부에서 이 값을 읽는 코드가 존재하지 않음을 확인함(§3.3 참고).

### 2.3 레거시 파일 — 건드리지 않음

§1.8 방침대로 보존. 삭제·리네임 없음. 추후 통합 정리 시 별도 커밋에서 판단.

---

## 3. 클래스 설계

### 3.1 LaserWeaponData.cs

`WeaponData` 상속. 실제 쿨다운은 `_cooldown`, **`FireDelay`는 0으로 설정**(SO 생성 시 에디터에서 강제).
- `FireDelay=0` 이어야 베이스 `TryFire`의 `_nextFireTime = Time.time + FireDelay` 세팅이 무력화되고, 베이스 `CanFire`가 항상 `true`를 돌려 1차 게이트를 통과시킴 → 실제 게이팅은 `ShouldFire` 오버라이드가 전담.
- `LaserPrefabCreator`에서 SO 생성 시 `SetFloat(so, "_fireDelay", 0f)` 명시적으로 박아둠.

```csharp
[Header("Laser")]
[Range(1f, 15f)]  [SerializeField] private float _cooldown = 5f;          // 빔 소멸 후 대기
[Range(0.5f, 20f)][SerializeField] private float _beamDuration = 6f;      // 빔 수명
[Range(0.1f, 10f)][SerializeField] private float _beamSpeed = 1.725f;     // 마우스 추적 속도
[Range(0.05f, 3f)][SerializeField] private float _beamRadius = 0.48f;     // 피격 반경
[Range(0.02f, 1f)][SerializeField] private float _tickInterval = 0.1f;    // 데미지 주기
[Range(0f, 0.5f)] [SerializeField] private float _stopDistance = 0.033f;  // 멈춤 거리
[SerializeField] private GameObject _beamPrefab;

public float Cooldown => _cooldown;
public float BeamDuration => _beamDuration;
public float BeamSpeed => _beamSpeed;
public float BeamRadius => _beamRadius;
public float TickInterval => _tickInterval;
public float StopDistance => _stopDistance;
public GameObject BeamPrefab => _beamPrefab;
```

### 3.2 LaserBeam.cs

**자립형 MonoBehaviour** — 무기 참조 없이 값만 보관.

필드:
```csharp
private float _life, _maxLife, _radius, _damage, _tickInterval, _speed, _stopDistance;
private float _dmgTick;
private int _bugLayer;
private AimController _aim;            // 추적 대상 위치 제공
private readonly Collider[] _hitBuffer = new Collider[64];

// 수명 호 (§1.5 ②) — 빔 자식 LineRenderer (프리펩에서 바인딩)
[SerializeField] private LineRenderer _lifeArc;
[SerializeField] private int _lifeArcSegments = 64;
[SerializeField] private float _lifeArcRadiusOffset = 0.2f;

// 4겹 SpriteRenderer 알파 동기화용 (프리펩에서 바인딩)
[SerializeField] private SpriteRenderer[] _layers;

public float LifeRatio => _maxLife > 0f ? _life / _maxLife : 0f;
```

`Initialize(AimController aim, LaserWeaponData data, int bugLayer)`:
- `_aim/_data` 값 복사, `_life = _maxLife = data.BeamDuration`
- 초기 위치 = `aim.AimPosition` (Y=0 고정), 회전 = 프리펩 회전 보존

`Update()`:
```csharp
_life -= Time.deltaTime;
if (_life <= 0f) { Destroy(gameObject); return; }

// 마우스 추적 (XZ 평면)
Vector3 target = _aim.AimPosition; target.y = transform.position.y;
Vector3 delta = target - transform.position;
float d = new Vector2(delta.x, delta.z).magnitude;
if (d > _stopDistance)
    transform.position += delta.normalized * _speed * Time.deltaTime;

// 틱 데미지
_dmgTick -= Time.deltaTime;
if (_dmgTick <= 0f) {
    _dmgTick = _tickInterval;
    int hit = Physics.OverlapSphereNonAlloc(transform.position, _radius, _hitBuffer, _bugLayer);
    for (int i = 0; i < hit; i++) {
        var d2 = _hitBuffer[i].GetComponentInParent<IDamageable>();
        d2?.TakeDamage(_damage);
    }
}

// 시각: 알파를 life/maxLife로 스케일 (4겹 스프라이트)
float ratio = _life / _maxLife;
UpdateVisualAlpha(ratio);

// 수명 호 (§1.5 ②) — positionCount 동적 조정 + 점 재계산
UpdateLifeArc(ratio);
```

**`UpdateLifeArc(float ratio)` 동작**:
```csharp
int count = Mathf.Max(2, Mathf.RoundToInt(_lifeArcSegments * ratio) + 1);
_lifeArc.positionCount = count;
float r = _radius + _lifeArcRadiusOffset;
float totalAngle = Mathf.PI * 2f * ratio;
float start = -Mathf.PI * 0.5f;  // 12시 방향 시작
for (int i = 0; i < count; i++) {
    float t = (float)i / (count - 1);
    float a = start - totalAngle * t;  // 시계방향 (프로토 L307)
    // 빔 local 좌표계: 빔 자체가 (90,0,0) 회전이므로 local XY 평면이 월드 XZ 평면
    _lifeArc.SetPosition(i, new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f));
}
Color c = new Color(1f, 0.09f, 0.267f, 0.8f * ratio);  // #ff1744 × life ratio
_lifeArc.startColor = c; _lifeArc.endColor = c;
```

**LaserAnimator** (내부 또는 같은 컴포넌트): 펄스(sin×0.3), 알파 페이드, 수명 호 갱신(위 메서드).

### 3.3 LaserWeapon.cs

**자체 구동** (기관총 패턴):
```csharp
[SerializeField] private AimController _aimController;
[SerializeField] private LaserWeaponData _data;

private LaserBeam _activeBeam;
private float _laserCD;   // 0 = 준비 완료

private void Start() {
    if (_aimController == null) _aimController = FindAnyObjectByType<AimController>();
    _baseData = _data;
    _aim = _aimController;
}

private void Update() {
    // 빔 존재 중엔 CD 흐르지 않음 (프로토 L293)
    if (_activeBeam == null && _laserCD > 0f)
        _laserCD = Mathf.Max(0f, _laserCD - Time.deltaTime);

    if (_aimController != null) TryFire(_aimController);
}

protected override bool ShouldFire(AimController aim) =>
    _activeBeam == null && _laserCD <= 0f;

protected override void Fire(AimController aim) {
    // 프리펩 회전 보존 (메모리: feedback_topdown_instantiate.md)
    var obj = Instantiate(
        _data.BeamPrefab,
        aim.AimPosition,
        _data.BeamPrefab.transform.rotation
    );
    _activeBeam = obj.GetComponent<LaserBeam>();
    _activeBeam.Initialize(aim, _data, aim.BugLayer);
    _laserCD = _data.Cooldown;  // 스폰 순간 즉시 풀 세팅 (프로토 L269)
}

// 게이팅은 전적으로 ShouldFire에 위임 — WeaponBase의 CanFire/CooldownRemaining/CooldownProgress는
// 오버라이드 안 함. FireDelay=0이라 베이스 CanFire는 항상 true → TryFire의 1차 게이트 무력화.

private bool IsActive => _activeBeam != null;
private float BeamLifeRatio =>
    _activeBeam != null && _data.BeamDuration > 0f
        ? Mathf.Clamp01(_activeBeam.LifeRatio) : 0f;

// 크로스헤어 쿨 호 전용 — 슬롯 바와 분리 (§1.5 ① 참조)
// Active 시 0 (호 숨김, 프로토 L293 laserCD 고정 → lp=0 재현),
// Cooling 시 `1 - _laserCD/cd`, Ready 시 1
public float CrosshairRingFill =>
    IsActive ? 0f
             : (_data == null ? 1f : Mathf.Clamp01(1f - _laserCD / _data.Cooldown));
```
> `LaserBeam`이 파괴되면 Unity 참조는 null이 되므로 `_activeBeam == null` 체크가 자동 성립 (`?.` 불필요, destroyed Object 비교는 Unity가 처리).

> **주의**: `laserWeapon.CanFire` / `.CooldownProgress` / `.CooldownRemaining`을 외부에서 읽으면 의미상 틀린 값을 받는다(베이스 `_nextFireTime` 기반). 현재 외부 호출부 조사 결과 레이저에 대해 이 값을 읽는 코드는 없음. 향후 공용 API로 이 값이 필요해지면 그때 virtualize 후 override를 추가한다(YAGNI).

**UI 오버라이드 — 슬롯 바(`WeaponSlotUI`)용**:

| 프로퍼티 | 동작 |
|---------|------|
| `BarFillAmount` | Active: `BeamLifeRatio` / Cooling: `1 - _laserCD/cd` / Ready: `1` |
| `BarColor` | Active: `LaserPink #ff6090` / Cooling: `LaserRed #ff1744` / Ready: `ReadyBarColor` |
| `StateText` | Active: `$"{_activeBeam._life:0.0}s"` / Cooling: `$"{_laserCD:0.0}s"` / Ready: `"자동발사"` |
| `BorderColor` | Active 또는 Ready: `LaserRed` / Cooling: `IdleBorderColor` |
| `ShowOverlay` | Cooling 중 `true` |
| `OverlayText` | `$"{_laserCD:0.0}s"` |
| `IsHittingTarget` | `IsActive` (활성 중이면 true, 크로스헤어 색 전환용) |

**크로스헤어 쿨 호(`LaserAimRingBinder`)용 — 슬롯 바와 분리**:

| 프로퍼티 | 동작 |
|---------|------|
| `CrosshairRingFill` | Active: `0` (호 숨김) / Cooling: `1 - _laserCD/cd` / Ready: `1` |

> ⚠️ 슬롯 바 `BarFillAmount`는 빔 **수명**을 표시하지만, 크로스헤어 호는 빔 **쿨**만 표시한다. 프로토는 두 정보를 공간적으로 분리(슬롯 UI ↔ 마우스 주변)해 이중 피드백을 준다.

### 3.4 LaserAimRingBinder.cs

크로스헤어 쿨 호만 담당 (빔 자체의 수명 호는 빔 프리펩 자식 `LineRenderer`가 별도 담당 — §3.5 참조).
```csharp
[RequireComponent(typeof(AimWeaponRing))]
public class LaserAimRingBinder : MonoBehaviour {
    [SerializeField] private LaserWeapon _weapon;
    private AimWeaponRing _ring;

    // 프로토 #ff1744 (laser 테마색)
    private static readonly Color LaserRed = new Color(1f, 0.09f, 0.267f, 1f);

    private void Awake() {
        _ring = GetComponent<AimWeaponRing>();
        if (_weapon == null) _weapon = FindAnyObjectByType<LaserWeapon>();
    }

    private void Update() {
        if (_ring == null) return;
        if (_weapon == null) { _ring.FillAmount = 0f; return; }

        // 슬롯 바(BarFillAmount) 대신 CrosshairRingFill 사용 —
        // Active 중엔 0으로 호가 숨겨지는 프로토 L313 동작 재현
        _ring.FillAmount = _weapon.CrosshairRingFill;
        _ring.SetColor(LaserRed);
    }
}
```

### 3.5 LaserBeam 프리펩 구조

```
LaserBeam (GameObject, LaserBeam 컴포넌트, rotation=(90,0,0))
├── OuterGlow (SpriteRenderer, r+0.13, #ff1744 12%, pulse)
├── RingStroke (SpriteRenderer, r+0.05, #ff6090 60%)
├── Core (SpriteRenderer, r, #ff1744 35%, pulse)
├── Center (SpriteRenderer, r*0.45, #ffc8d2 70%)
└── LifeArc (LineRenderer, r+0.2, useWorldSpace=false, loop=false)
     ├─ positionCount = 65 × (life/maxLife) 로 매 프레임 재계산 (0에 가까워질수록 호가 짧아짐)
     ├─ 각 점 = (cos(a), 0, sin(a)) · (r+0.2), a = -π/2 → -π/2 + 2π·(life/maxLife), 시계방향
     ├─ startColor/endColor = (#ff1744, alpha = 0.8 · life/maxLife)
     └─ widthCurve = 0.04 고정 (두께)
```

- 기본 회전: `(90, 0, 0)` — 탑뷰 XZ 평면 눕힘. `Instantiate` 시 프리펩 회전 반드시 보존 (메모리 `feedback_topdown_instantiate.md`).
- **LifeArc는 빔의 자식** → 빔 이동 시 함께 따라오며, 빔 소멸 시 함께 `Destroy`됨. 씬에 잔존 없음.
- `LineRenderer.useWorldSpace = false` 설정 필수 (빔 local 좌표계에서 원을 그려야 하므로).
- `LineRenderer.alignment = TransformZ` — 탑뷰 (X=90°) 회전에서 XZ 평면에 평평히 깔림.
- `LaserBeam.Update`에서 매 프레임 `RebuildLifeArc()` 호출 (`positionCount` + `SetPositions` + 알파 갱신).

---

## 4. 에디터 / 씬 작업

### 4.1 에디터 메뉴 한 번
`Tools → Drill-Corp → 3. 게임 초기 설정 → 8. 레이저 자산 일괄 생성`
→ 생성:
- `Assets/_Game/Sprites/Weapons/LaserGlow.png` (4겹용 원형 그라디언트)
- `Assets/_Game/Materials/Laser_Core.mat`, `Laser_Glow.mat`, `Laser_LifeArc.mat` (LineRenderer용 vertex-color 머티리얼)
- `Assets/_Game/Prefabs/Weapons/LaserBeam.prefab` (4겹 SR + **LifeArc LineRenderer** 자식 구조)
- `Assets/_Game/Data/Weapons/Weapon_LaserBeam.asset` (LaserWeaponData — 레거시 `Weapon_Laser.asset`과 이름 구분)
  - **`_fireDelay = 0`으로 세팅** — 베이스 `TryFire` 1차 게이트를 무력화 (§2.2 주석 참조)

### 4.1.1 구현 단계와 에디터 스크립트 점진적 작성

`LaserPrefabCreator.cs`는 한 번에 완성하지 않고 **각 구현 단계와 쌍으로 증분 작성**한다. 단계 끝마다 메뉴를 실행해 해당 단계의 산출물이 정상 생성되는지 즉시 검증.

| 단계 | 클래스 산출물 | 에디터 추가 함수 | 메뉴 실행 후 확인 |
|------|--------------|-----------------|------------------|
| **1** | `LaserWeaponData.cs` | `CreateOrUpdateData()` + `SetFloat(so, "_fireDelay", 0f)` | `Weapon_LaserBeam.asset` 생성, `FireDelay=0`, `Cooldown=5` 등 |
| **2** | `LaserBeam.cs` | `CreateBeamSprite()` + `CreateBeamPrefab()` + `AddLifeArc()` | `LaserBeam.prefab` 열어 4겹 SR + 자식 `LifeArc` LineRenderer 구조 확인 |
| **3** | `LaserWeapon.cs` | (에디터 작업 없음 — 씬 배치 단계에서 컴포넌트 추가) | 컴파일 통과 + `Weapon_LaserBeam.asset`의 `_beamPrefab`에 §2 프리펩 참조 자동 세팅 |
| **4** | `LaserAimRingBinder.cs` | (에디터 작업 없음) | 컴파일 통과 |
| **5** | 씬 통합 | (수동) | 런타임 동작 검증 |

→ 각 단계 완료 시 해당 에디터 함수만 추가하면 메뉴 재실행으로 **이전 단계 자산은 보존**하고 새 자산만 덧붙임 (`isNew` 플래그 분기 패턴, `BombPrefabCreator` 참고).

### 4.2 씬 배치
1. 빈 GO `LaserWeapon` 추가 → `LaserWeapon` 컴포넌트 → `Data`에 `Weapon_LaserBeam.asset`, `AimController` 참조 드래그
2. AimController 자식에 `AimWeaponRing` + `LaserAimRingBinder` — `_radiusOffset = 0.4` (크로스헤어 쿨 호 전용, §1.5 ①)
3. `WeaponPanelUI._weapons` 배열에 `LaserWeapon` 추가 (슬롯 4번 권장)
4. 레이저 슬롯의 `WeaponSlotUI` → `Build Default Hierarchy` 재실행 (Overlay 자식 생성 확인)
5. (선택) `TurretController`에 `Barrel_Laser` 자식 추가 — 작은 빨간 Cube(0.15×0.15×0.9), 위쪽 각도

> 참고: 빔 수명 호(§1.5 ②)는 빔 프리펩에 **이미 자식으로 포함**되어 있어 씬 배치 단계에서 별도 작업 없음.

### 4.3 레거시 공존 확인 (삭제 없음)
1. 레거시 `Weapon_Laser.asset`은 그대로 둠 (씬 미참조 상태 유지)
2. 신규 SO는 반드시 `Weapon_LaserBeam.asset`으로 저장 — 파일명 충돌 방지
3. 컴파일 후 두 SO 타입(`LaserBeamData` / `LaserWeaponData`) 공존 확인

---

## 5. 검증 체크리스트

### 기능
- [ ] 장착 중 빔 없고 쿨다운 0 → 마우스 위치에 빔 스폰
- [ ] 빔이 6초간 마우스를 느리게 추적 (1.725 u/s)
- [ ] 빔 중심 0.48 유닛 내 벌레에 0.1초마다 0.8 데미지 (DPS 8)
- [ ] 빔 중심이 마우스 0.033 유닛 이내면 멈춤 (지글링 방지)
- [ ] 빔 수명 6초 만료 시 자파괴 → 쿨다운 5초 시작
- [ ] 쿨다운 중에는 새 빔 스폰 안 함
- [ ] 빔 활성 중에는 `_laserCD` 감소 안 함 (프로토 충실)

### UI 3상태 — 슬롯 바
- [x] Active: 슬롯 바 연분홍(#ff6090), 빔 수명% 감소, 텍스트 `"5.2s"`, 테두리 진홍, 오버레이 없음
- [x] Cooling: 슬롯 바 진홍(#ff1744), 쿨% 차오름, 텍스트 `"3.0s"`, 검은 오버레이 ON
- [x] Ready: 슬롯 바 초록, 100% 고정, 텍스트 `"자동발사"`, 테두리 진홍

### UI 3상태 — 두 개의 원 (§1.5)
- [x] **크로스헤어 쿨 호** (마우스 주변, radiusOffset 0.4): Active 시 **숨김(FillAmount=0)** / Cooling 시 차오름 / Ready 시 100%
- [x] **빔 수명 호** (빔 위치 주변, r+0.2 `LineRenderer`): Active 시 `life/maxLife`로 짧아짐 / 빔 소멸과 함께 사라짐
- [x] ~~빔 자체 4겹 스프라이트 알파가 `life/maxLife`로 페이드~~ → **사용자 결정으로 페이드 제거** (빔이 끝까지 또렷하게 유지)

### 통합
- [x] 저격총·폭탄·기관총과 동시 작동 (서로 간섭 없음)
- [x] 레거시 `LaserBeamWeapon`/`LaserBeamField`/`Weapon_Laser.asset` 공존 상태로 컴파일 에러 0
- [x] 신규 SO는 `Weapon_LaserBeam.asset` 이름으로 생성되어 파일명 충돌 없음
- [x] 프리펩 `Instantiate` 시 회전 보존 (메모리 `feedback_topdown_instantiate.md` 준수)

---

## 6. 폴리싱 To-Do (다음 단계)

### 6.1 시각/청각 폴리싱
- [ ] **사운드**: `sndLaserOn` (빔 스폰 sweep 200→800Hz, 0.3s) / `sndLaserHit` 포팅
- [ ] **빔 트레일 잔상**: 지나간 자리에 파티클 남기기 (현재 CenterParticles로 일부 표현되나 트레일은 별도)
- [ ] **피격 burst** (`WeaponData.HitVfxPrefab`): 데미지 받는 벌레 위치에 작은 분홍 burst (프로토 L294 `burst(...,'#ff6090',2,1.5)`)
- [ ] **CenterParticles 머티리얼 정제**: 현재 Default-ParticleSystem.mat 사용. 글로우 텍스처로 교체 검토.

### 6.2 게임 밸런스/플레이감
- [ ] **빔 데미지 OverlapSphere 반경과 시각 크기 일치 검토** — 현재 root scale ×2로 시각이 데미지 범위(`_radius=1.0`)보다 큼. 통일하거나 의도된 갭 유지 결정.
- [ ] **추적 속도(`_beamSpeed`) 튜닝** — 1.725 u/s가 게임 밸런스 적절한지 플레이 테스트
- [ ] **쿨/수명 비율** — 현재 6s 활성 + 5s 쿨 (가동률 ≈ 54%). 보조 무기로서 너무 강한지/약한지 확인

### 6.3 미구현 / 추후 단계
- **다중 빔**: 성장 시스템으로 동시 2~3개 빔 허용 — 본 Phase 범위 외
- **빔 크기 성장**: 프로토 `ws.laser.range *= 1.2` 업그레이드 — 추후 성장 시스템에서 런타임 배율 처리
- **Turret 다중 배럴 관리**: `SetActiveBarrel(weaponId)` — 리팩토링 트랙 2 (Barrel_Laser 자식 추가 보류 중)
- **레거시 정리**: `LaserBeamWeapon`/`LaserBeamData`/`LaserBeamField` 및 `Weapon_Laser.asset` 삭제 시점 결정

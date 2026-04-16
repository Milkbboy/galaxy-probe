# Phase 4 — 레이저 무기 구현

> 상위 문서: `WEAPON_IMPLEMENTATION_PLAN.md` §7
> **상태**: ⏳ 구현 예정 (2026-04-16 계획)
> **원본 기준**: `docs/_.html` L166·L188·L269·L281·L293·L307·L313 — `fireLaser` / `laserBeams` / `updateWeaponUI` / `drawCrosshair`

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

### 1.5 에임 호 = 슬롯 바 완전 동기화 (플랜 §4.6.3 준수)
- `LaserAimRingBinder` — `BombAimRingBinder`/`MachineGunAimRingBinder` 패턴 동일
- `_ring.FillAmount = _weapon.BarFillAmount` / `_ring.SetColor(_weapon.BarColor)`
- 활성 중에는 호가 **빔 수명**을 따라 감소 (프로토 원본은 쿨다운 진행만 표시하는 허점 있음 — 플랜 §4.6.7 / 호는 수명과 통일)
- radiusOffset = **0.4** (저격 0.08, 폭탄 0.2, 기관총 0.3 다음 자리)

### 1.6 적 유무 무관
- `ShouldFire` — 타겟 체크 없음, `_activeBeam==null && _laserCD<=0`만
- `IsHittingTarget` — 빔 활성 중이면 `true` (크로스헤어/테두리 활성 표시용)

### 1.7 빔 시각화 (프로토 L307 충실 구현)
`LaserBeam` 단일 GameObject에 **4겹 SpriteRenderer 동심원** 또는 단일 커스텀 메시로 구현:
- 가장 바깥 (`r+0.13`) — `#ff1744` 12% 알파, 펄스(sin × 0.3)
- 링 스트로크 (`r+0.05`) — `#ff6090` 60% 알파
- 코어 (`r`) — `#ff1744` 35% 알파, 펄스
- 중앙 (`r*0.45`) — `#ffc8d2` 70% 알파
- 수명 호 (`r+0.2`, counter-clockwise) — 별도 `AimWeaponRing` 재사용 or `LineRenderer`
- 빔 자체 `alpha = life/maxLife` 곱 → 소멸 직전 자연스럽게 페이드

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
| `Assets/_Game/Scripts/Weapon/WeaponBase.cs` | `CanFire`/`CooldownRemaining`/`CooldownProgress`를 `virtual`로 승격 (레이저가 자체 상태로 오버라이드). 현재 `CanFire`는 get-only 프로퍼티 — 가상화하면 기존 파생 무영향. |
| `Assets/_Game/Scripts/Weapon/WeaponSwitcher.cs` | (선택) 슬롯 4번에 `LaserWeapon` 참조 추가. 보조 무기는 Switcher 밖에서 동작하므로 필수 아님. |
| `Assets/_Game/Scripts/Machine/TurretController.cs` | (선택) `Barrel_Laser`(빨간 Cube, 위쪽 배치) — 리팩토링 플랜 트랙 2가 선행되면 그때 함께. 현 Phase 4 범위에선 단독 배럴 추가만. |

### 2.3 레거시 파일 — 건드리지 않음

§1.8 방침대로 보존. 삭제·리네임 없음. 추후 통합 정리 시 별도 커밋에서 판단.

---

## 3. 클래스 설계

### 3.1 LaserWeaponData.cs

`WeaponData` 상속. `FireDelay`는 참고값, 실제 쿨다운은 `_cooldown`.
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
UpdateVisualAlpha(_life / _maxLife);
```

**LaserAnimator** (내부 또는 같은 컴포넌트): 펄스(sin×0.3), 알파 페이드, 수명 호 갱신.

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

// WeaponBase.TryFire가 _nextFireTime을 세팅하려 하지만 우리는 자체 _laserCD 사용 →
// 오버라이드
public override bool CanFire => _activeBeam == null && _laserCD <= 0f;
public new float CooldownRemaining => _laserCD;
public new float CooldownProgress =>
    _data == null ? 1f : Mathf.Clamp01(1f - _laserCD / _data.Cooldown);

private bool IsActive => _activeBeam != null;
private float BeamLifeRatio =>
    _activeBeam != null && _data.BeamDuration > 0f
        ? Mathf.Clamp01(_activeBeam.LifeRatio) : 0f;
```
> `LaserBeam`이 파괴되면 Unity 참조는 null이 되므로 `_activeBeam == null` 체크가 자동 성립 (`?.` 불필요, destroyed Object 비교는 Unity가 처리).

**UI 오버라이드**:

| 프로퍼티 | 동작 |
|---------|------|
| `BarFillAmount` | Active: `BeamLifeRatio` / Cooling: `1 - _laserCD/cd` / Ready: `1` |
| `BarColor` | Active: `LaserPink #ff6090` / Cooling: `LaserRed #ff1744` / Ready: `ReadyBarColor` |
| `StateText` | Active: `$"{_activeBeam._life:0.0}s"` / Cooling: `$"{_laserCD:0.0}s"` / Ready: `"자동발사"` |
| `BorderColor` | Active 또는 Ready: `LaserRed` / Cooling: `IdleBorderColor` |
| `ShowOverlay` | Cooling 중 `true` |
| `OverlayText` | `$"{_laserCD:0.0}s"` |
| `IsHittingTarget` | `IsActive` (활성 중이면 true, 크로스헤어 색 전환용) |

### 3.4 LaserAimRingBinder.cs
```csharp
[RequireComponent(typeof(AimWeaponRing))]
public class LaserAimRingBinder : MonoBehaviour {
    [SerializeField] private LaserWeapon _weapon;
    private AimWeaponRing _ring;

    private void Awake() {
        _ring = GetComponent<AimWeaponRing>();
        if (_weapon == null) _weapon = FindAnyObjectByType<LaserWeapon>();
    }

    private void Update() {
        if (_ring == null || _weapon == null) return;
        _ring.FillAmount = _weapon.BarFillAmount;
        _ring.SetColor(_weapon.BarColor);
    }
}
```

### 3.5 LaserBeam 프리펩 구조

```
LaserBeam (GameObject, LaserBeam 컴포넌트)
├── OuterGlow (SpriteRenderer, r+0.13, #ff1744 12%, pulse)
├── RingStroke (SpriteRenderer, r+0.05, #ff6090 60%)
├── Core (SpriteRenderer, r, #ff1744 35%, pulse)
├── Center (SpriteRenderer, r*0.45, #ffc8d2 70%)
└── LifeArc (AimWeaponRing 재사용 or LineRenderer, r+0.2, life 진행)
```

기본 회전: `(90, 0, 0)` — 탑뷰 XZ 평면 눕힘. `Instantiate` 시 프리펩 회전 반드시 보존.

---

## 4. 에디터 / 씬 작업

### 4.1 에디터 메뉴 한 번
`Tools → Drill-Corp → 3. 게임 초기 설정 → 8. 레이저 자산 일괄 생성`
→ 생성:
- `Assets/_Game/Sprites/Weapons/LaserGlow.png` (4겹용 원형 그라디언트)
- `Assets/_Game/Materials/Laser_Core.mat`, `Laser_Glow.mat`
- `Assets/_Game/Prefabs/Weapons/LaserBeam.prefab` (4겹 자식 구조)
- `Assets/_Game/Data/Weapons/Weapon_LaserBeam.asset` (LaserWeaponData — 레거시 `Weapon_Laser.asset`과 이름 구분)

### 4.2 씬 배치
1. 빈 GO `LaserWeapon` 추가 → `LaserWeapon` 컴포넌트 → `Data`에 `Weapon_LaserBeam.asset`, `AimController` 참조 드래그
2. AimController 자식에 `AimWeaponRing` + `LaserAimRingBinder` — `_radiusOffset = 0.4`
3. `WeaponPanelUI._weapons` 배열에 `LaserWeapon` 추가 (슬롯 4번 권장)
4. 레이저 슬롯의 `WeaponSlotUI` → `Build Default Hierarchy` 재실행 (Overlay 자식 생성 확인)
5. (선택) `TurretController`에 `Barrel_Laser` 자식 추가 — 작은 빨간 Cube(0.15×0.15×0.9), 위쪽 각도

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

### UI 3상태
- [ ] Active: 슬롯 바 연분홍(#ff6090), 빔 수명% 감소, 텍스트 `"5.2s"`, 테두리 진홍, 오버레이 없음
- [ ] Cooling: 슬롯 바 진홍(#ff1744), 쿨% 차오름, 텍스트 `"3.0s"`, 검은 오버레이 ON
- [ ] Ready: 슬롯 바 초록, 100% 고정, 텍스트 `"자동발사"`, 테두리 진홍
- [ ] 에임 호 색·채움이 슬롯 바와 완전 동기화 (3상태 모두)

### 통합
- [ ] 저격총·폭탄·기관총과 동시 작동 (서로 간섭 없음)
- [ ] 레거시 `LaserBeamWeapon`/`LaserBeamField`/`Weapon_Laser.asset` 공존 상태로 컴파일 에러 0
- [ ] 신규 SO는 `Weapon_LaserBeam.asset` 이름으로 생성되어 파일명 충돌 없음
- [ ] 프리펩 `Instantiate` 시 회전 보존 (메모리 `feedback_topdown_instantiate.md` 준수)

---

## 6. 미구현 / 추후

- **빔 트레일 잔상**: 지나간 자리에 파티클 남기기 — 폴리싱
- **다중 빔**: 성장 시스템으로 동시 2~3개 빔 허용 — 본 Phase 범위 외
- **빔 크기 성장**: 프로토 `ws.laser.range *= 1.2` 업그레이드 — 추후 성장 시스템에서 런타임 배율 처리
- **사운드**: `sndLaserOn`/`sndLaserHit` 포팅 — 폴리싱
- **Turret 다중 배럴 관리**: `SetActiveBarrel(weaponId)` — 리팩토링 트랙 2

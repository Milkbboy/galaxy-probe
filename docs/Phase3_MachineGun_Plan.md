# Phase 3 — 기관총 무기 구현

> 상위 문서: `WEAPON_IMPLEMENTATION_PLAN.md` §6
> **상태**: ✅ **구현 완료** (2026-04-16) — Phase 3.5(탄창 pip 행) 포함

## 0. 구현 결과 요약

머신 배럴(Turret FirePoint)에서 파란 탄환이 ±3.4° 산포로 자동 연사 → 탄창 40발 소진 시 5초 리로딩 → 다시 발사. 적 유무 무관 — 마우스 방향으로 계속 발사. 메인 무기(스나이퍼)·폭탄과 병렬 동작.

**최종 데이터 값** (`Weapon_MachineGun.asset`):
- `FireDelay` 0.14s / `Damage` 0.5 / `MaxAmmo` 40 / `ReloadDuration` 5s
- `BulletSpeed` 9 / `BulletLifetime` 1.5s (최대 사거리 13.5유닛)
- `BulletHitRadius` 0.15 / `SpreadAngle` 0.06 rad (±3.4°)
- `LowAmmoThreshold` 8 (이하 시 테두리 빨강 경고)
- `ThemeColor` `#4fc3f7` (밝은 파랑)

**동작 사이클**: 연사 ≈ 5.7초 → 리로딩 5초 → 반복 (사이클당 약 10.7초, 발사 가동률 ≈ 53%)

---

## 1. 구현 결정 사항

### 보조 무기 패턴 (폭탄과 동일)
- **메인 무기 슬롯에 들어가지 않음** — `AimController.EquipWeapon` 흐름 거치지 않음
- `MachineGunWeapon`이 자체 `Update()`에서 매 프레임 `TryFire(_aimController)` 호출
- 메인 무기(스나이퍼)와 병렬 동작 — 레벨업으로 해금되는 보조 무기 컨셉
- 프로토타입 `_.html` L165 `canvas.click → fireBomb()` 와 같은 "글로벌 입력" 패턴

### 발사 위치 (FirePoint Transform)
- `MachineGunWeapon._firePoint` (선택 필드)
- 비어있으면: 머신 중앙(`aim.MachineTransform`)에서 에임 방향
- **할당하면 (Turret 배럴 끝 자식)**: 그 위치에서 `_firePoint.forward`(배럴이 바라보는 +Z) 방향 — 배럴 회전과 시각적으로 일치
- 권장 셋업: `TurretController` Pivot 자식에 빈 GameObject "FirePoint" 추가, localPosition `(0, 0, barrelSize.z)` (배럴 끝점)

### 적 유무 무관 발사
- `ShouldFire`: `aim.HasBugInRange` 체크 **없음** — 항상 발사 가능 (탄 있고 리로딩 아니면)
- `IsHittingTarget`: `CanFire` 반환 — 적 유무 무시
- `BorderColor`: 활성 상태면 항상 GunBlue 강조 (HasTarget 게이트 없음)

---

## 2. 파일 계획

### 2.1 신규 파일 (실제 구현 — 5개)

| 파일 | 역할 |
|------|------|
| `Assets/_Game/Scripts/Weapon/MachineGun/MachineGunData.cs` | SO — 탄창/리로딩/탄속/산포/명중반경 + bullet 프리펩 참조 |
| `Assets/_Game/Scripts/Weapon/MachineGun/MachineGunWeapon.cs` | WeaponBase 파생 — 자체 Update + 자동 연사 + 탄창/리로딩 + UI 오버라이드 |
| `Assets/_Game/Scripts/Weapon/MachineGun/MachineGunBullet.cs` | 자립형 투사체 — XZ 직진, OverlapSphere 충돌, 첫 명중에만 데미지 (관통 없음) |
| `Assets/_Game/Scripts/Aim/MachineGunAimRingBinder.cs` | 에임 호 — `BarFillAmount` + `BarColor` 푸시 (파랑/리로딩 시 빨강) |
| `Assets/_Game/Scripts/Editor/MachineGunPrefabCreator.cs` | 에디터 도구 — 메뉴 7번으로 스프라이트 + 프리펩 + 머티리얼 + SO 일괄 생성 |

### 2.2 수정 파일 (Phase 3.5 — 탄창 pip 행)

| 파일 | 변경 내용 |
|------|----------|
| `Assets/_Game/Scripts/Weapon/WeaponBase.cs` | 가상 프로퍼티 3개 추가: `ShowAmmoRow`, `AmmoCurrent`, `AmmoMax` (모두 기본값) |
| `Assets/_Game/Scripts/UI/Weapon/WeaponSlotUI.cs` | `_ammoRowContainer` 필드, `_pipPool` 동적 생성/색상 갱신, `BuildDefaultHierarchy`에 AmmoRow 자식 자동 생성 (슬롯 높이 90→100) |

---

## 3. 클래스 설계

### 3.1 MachineGunData.cs
`WeaponData` 상속. 추가 필드:
```csharp
[Range(1, 200)]   private int _maxAmmo = 40;
[Range(0.5f, 15f)] private float _reloadDuration = 5f;
[Range(1, 50)]    private int _lowAmmoThreshold = 8;
[Range(1f, 30f)]  private float _bulletSpeed = 9f;
[Range(0.2f, 5f)] private float _bulletLifetime = 1.5f;
[Range(0.05f, 1f)] private float _bulletHitRadius = 0.15f;
[Range(0f, 0.5f)] private float _spreadAngle = 0.06f;
                  private GameObject _bulletPrefab;
```

### 3.2 MachineGunBullet.cs
**자립형 컴포넌트** — 무기 참조 없이 데이터만 보관.

`Initialize(direction, data, bugLayer)`:
- 산포 적용된 정규화 방향 + 데이터/레이어 캡처
- `_velocity = direction * bulletSpeed`, Y=0 강제

`Update()`:
- 수명 만료 → 파괴
- XZ 직진 이동 (`transform.position += _velocity * Time.deltaTime`)
- `Physics.OverlapSphereNonAlloc(pos, _bulletHitRadius, ...)` 매 프레임 충돌 검사
- 가장 가까운 1마리에만 데미지 + Hit VFX (관통 없음)

### 3.3 MachineGunWeapon.cs

**자체 구동** (폭탄 패턴):
```csharp
[SerializeField] private AimController _aimController;
[SerializeField] private Transform _firePoint;

private void Start() {
    if (_aimController == null) _aimController = FindAnyObjectByType<AimController>();
    if (_aimController != null) _aim = _aimController;
}
private void Update() {
    if (_aimController != null) TryFire(_aimController);
}
```

**ShouldFire** — 적 유무 무관:
```csharp
if (_isReloading) {
    if (Time.time >= _reloadEndTime) {
        _isReloading = false;
        _currentAmmo = _data.MaxAmmo;
    }
    return false;
}
return _currentAmmo > 0;
```

**Fire** — FirePoint 우선 + 산포:
```csharp
Vector3 spawnPos = _firePoint != null ? _firePoint.position : aim.MachineTransform.position;
Vector3 baseDir = _firePoint != null ? _firePoint.forward : (aim.AimPosition - spawnPos);
float spread = Random.Range(-_data.SpreadAngle, _data.SpreadAngle);
Vector3 dir = Quaternion.AngleAxis(spread * Mathf.Rad2Deg, Vector3.up) * baseDir.normalized;

// 프리펩 회전 보존 — Quaternion.identity 금지 (메모리 feedback_topdown_instantiate.md)
var obj = Instantiate(_data.BulletPrefab, spawnPos, _data.BulletPrefab.transform.rotation);
obj.GetComponent<MachineGunBullet>()?.Initialize(dir, _data, aim.BugLayer);

_currentAmmo--;
if (_currentAmmo <= 0) {
    _isReloading = true;
    _reloadEndTime = Time.time + _data.ReloadDuration;
}
```

**UI 오버라이드**:
| 프로퍼티 | 동작 |
|---------|------|
| `BarFillAmount` | 평상시 `_currentAmmo/MaxAmmo` (감소) / 리로딩 중 `1 - 남은시간/총시간` (차오름) |
| `BarColor` | 평상시 `GunBlue (#4fc3f7)` / 리로딩 중 `WarningColor` (빨강) |
| `StateText` | `"32발"` / `"리로딩 2.3s"` |
| `BorderColor` | 리로딩 또는 탄≤8 시 `WarningColor` / 활성 시 `GunBlue` / 그 외 `IdleBorderColor` |
| `ShowOverlay` | `_isReloading` |
| `OverlayText` | `"리로딩\n2.3s"` |
| `ShowAmmoRow` | true |
| `AmmoCurrent` | `_currentAmmo` |
| `AmmoMax` | `_data.MaxAmmo` |
| `IsHittingTarget` | `CanFire` (적 유무 무시) |

### 3.4 MachineGunAimRingBinder.cs
`BombAimRingBinder`와 동일 패턴:
```csharp
_ring.FillAmount = _weapon.BarFillAmount;
_ring.SetColor(_weapon.BarColor); // 인스펙터 색 무시, 무기 상태 따라 자동 변경
```

### 3.5 WeaponSlotUI.cs (Phase 3.5 — 탄창 pip 행)

**필드 추가**:
- `_ammoRowContainer` (RectTransform — Build Default Hierarchy가 자동 생성)
- `_pipEmptyColor` (소진된 pip 색)
- `_pipPool` (List<Image> — 런타임 풀)

**`UpdateAmmoRow()`** (매 프레임):
- `ShowAmmoRow=false`면 컨테이너 비활성, 종료
- pip 풀이 `AmmoMax`보다 적으면 추가 생성 (1회만)
- `AmmoMax` 변경 시 균등 배치 재계산
- 색 갱신: `i < AmmoCurrent` → `_weapon.BarColor` / 그 외 → `_pipEmptyColor`

**`BuildDefaultHierarchy` 변경**:
- 슬롯 높이 90 → **100** (pip 행 자리 확보)
- CoolBar(y=4, h=4) 위에 AmmoRow 컨테이너 자동 생성 (y=12, h=4, w=80)
- 기본 비활성 — `ShowAmmoRow=true` 무기 바인딩 시 Update에서 활성화

---

## 4. 에디터 작업

### 4.1 메뉴 한 번
`Tools → Drill-Corp → 3. 게임 초기 설정 → 7. 기관총 자산 일괄 생성`
→ 생성: `MachineGunBulletSprite.png`, `MachineGunBullet.prefab`, `MachineGunTrail_Mat.mat`, `Weapon_MachineGun.asset`

### 4.2 씬 배치
1. 빈 GO 추가 → `MachineGunWeapon` 컴포넌트 → `Data`에 `Weapon_MachineGun.asset`
2. (선택) `Fire Point` 필드에 Turret 배럴 끝 Transform 드래그
3. (선택) AimController 자식에 `AimWeaponRing` + `MachineGunAimRingBinder` (radiusOffset 0.3)
4. `WeaponPanelUI._weapons[2]`에 `MachineGunWeapon` 할당
5. 기관총 슬롯의 `WeaponSlotUI` → "Build Default Hierarchy" 재실행 (AmmoRow + 슬롯 높이 100 적용)

---

## 5. 미구현 / 추후

- **탄피 (Shell Casing) 이펙트**: 발사 시 옆으로 튀는 빈 탄피 — 별도 폴리싱 단계
- **BulletPool**: 현재 매 발사마다 Instantiate. 필요 시 풀링 도입 (40발 × 짧은 수명이라 현재로선 불필요)
- **탄종 다양화**: 관통탄/폭발탄 등 — 후속 성장 시스템에서

---

## 6. 검증 체크리스트

- [x] FirePoint 할당 시 배럴 끝에서 발사
- [x] 0.14s 간격 자동 연사
- [x] ±3.4° 산포 적용
- [x] 탄 명중 시 단일 대상 데미지 (관통 없음)
- [x] 40발 소진 → 5초 리로딩 → 자동 재충전
- [x] 슬롯 바: 평상시 파랑(감소), 리로딩 빨강(차오름)
- [x] 슬롯 텍스트: `"32발"` / `"리로딩 2.3s"`
- [x] 탄 8개 이하 → 슬롯 테두리 빨강
- [x] 리로딩 중 검은 오버레이 + 큰 카운트다운
- [x] 에임 호 색·채움이 슬롯 바와 동기화
- [x] **탄창 pip 40개 — 발사 시 오른쪽부터 회색으로 변함 (Phase 3.5)**
- [x] 적 유무 무관 — 마우스 방향으로 계속 발사
- [x] 메인 무기·폭탄과 병렬 동작
- [x] 모든 Instantiate에서 `prefab.transform.rotation` 보존 (탑뷰 회전 유지)

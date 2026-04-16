# Phase 2 — 폭탄 무기 구현

> 상위 문서: `WEAPON_IMPLEMENTATION_PLAN.md` §5
> **상태**: ✅ **구현 완료** (2026-04-16) — 본 문서는 실제 구현 결과를 반영.

## 0. 구현 결과 요약

좌클릭 → 머신에서 폭탄이 클릭 위치로 비행(주황 트레일) → 도달 시 폭발 burst + AoE 피해. 비행 중 클릭 위치에 반투명 마커 표시. 메인 무기와 병렬로 항상 활성 (슬롯 교체 무관).

**최종 데이터 값** (`Weapon_Bomb.asset`):
- `FireDelay` 5초 / `Damage` 3 / `ExplosionRadius` 1.8
- `Instant` false (비행 모드) / `ProjectileSpeed` 5 / `ProjectileLifetime` 5
- `ThemeColor` `#f4a423` (주황)

**구현 중 결정·발견된 사항** (계획 대비 변경점):
- **`_instant` 토글 추가** — instant=true면 클릭 위치에서 즉시 폭발 (비행 없음). 현재 false.
- **`BarColor` 항상 주황** (계획상 준비 시 초록 → "준비/쿨중 모두 주황"으로 변경)
- **`BombExplosionFx`** 컴포넌트/프리펩 추가 — 폭발 VFX (스프라이트 기반, scale 확장 + 알파 페이드)
- **`BombPrefabCreator`** 에디터 도구 — 메뉴 한 번으로 스프라이트 3개 + 프리펩 3개 + 머티리얼 + SO 자동 생성
- **`AimController.EnsureInfoLabel` 호출을 Awake → Start로 이동** — TMPFontHolder 초기화 순서 보장 (한글 깨짐 방지)
- **트레일 머티리얼은 에셋으로 저장 필수** — 런타임 `new Material()`을 sharedMaterial에 직접 넣으면 프리펩 저장 시 null로 빠져 마젠타 'Missing Material' 색으로 렌더링됨 (디버깅 결과)
- **탑뷰 회전 보존 규칙** — 모든 SpriteRenderer 프리펩 인스턴스화는 `Instantiate(prefab, pos, prefab.transform.rotation)` 사용. `Quaternion.identity`는 프리펩 베이크된 (90,0,0) 회전을 덮어써 카메라 쪽으로 서버림. (메모리 `feedback_topdown_instantiate.md`에 패턴 저장)

---

## 1. Context

프로토타입 `_.html`의 무기 4종 중 **폭탄**을 Unity로 이식한다.

**현재 코드베이스 상태:**
- 기존 무기(Sniper, BurstGun, Shotgun, Laser, LockOn)는 모두 **즉발 영역 피해** 방식 (`OverlapSphere` at aim position).
- 폭탄은 **최초의 투사체(Projectile) 기반 무기**가 된다.
- `WeaponBase`에는 이미 `ShowOverlay`, `OverlayText`, `IsHittingTarget` 훅이 준비되어 있어 **base 변경 없이** 파생만 추가한다.

**핵심 차이점:**
- 자동 발사가 아닌 **수동 클릭** 발사
- 즉발이 아닌 **시간차 폭발** (투사체 비행 → 도달 시 AoE)
- 타겟 게이트(`HasBugInRange`) 없이 어디든 발사 가능
- **무기 교체 슬롯이 아닌 항상 활성 보조 무기** — 메인 무기(저격/기관총 등)와 병렬 동작
  - `AimController.EquipWeapon` / `WeaponSwitcher` 흐름을 **거치지 않음**
  - `BombWeapon`이 자체 `Update()`에서 매 프레임 `TryFire(_aimController)` 호출
  - 프로토타입 `_.html` L165 `canvas.click → fireBomb()` 와 동일 (메인 무기와 무관하게 클릭이 폭탄)

---

## 2. 파일 계획

### 2.1 신규 파일 (실제 구현 — 7개)

| 파일 | 역할 |
|------|------|
| `Assets/_Game/Scripts/Weapon/Bomb/BombData.cs` | ScriptableObject — `_instant` 토글, 폭발 반경, 투사체 속도, 프리펩 참조 4개 |
| `Assets/_Game/Scripts/Weapon/Bomb/BombWeapon.cs` | `WeaponBase` 파생 — 자체 Update + 클릭 발사 + UI 오버라이드 |
| `Assets/_Game/Scripts/Weapon/Bomb/BombProjectile.cs` | 자립형 투사체 — 이동·폭발·AoE 피해 + 착탄 마커 관리. `Detonate(pos, data, layer)` static 헬퍼 노출 |
| `Assets/_Game/Scripts/Weapon/Bomb/BombLandingMarker.cs` | 착탄 위치 표시기 — SpriteRenderer 알파 펄스 |
| `Assets/_Game/Scripts/Weapon/Bomb/BombExplosionFx.cs` | 폭발 VFX — 스케일 ease-out + 알파 페이드 + 자동 파괴 |
| `Assets/_Game/Scripts/Aim/BombAimRingBinder.cs` | 크로스헤어 호 바인딩 — `_weapon.BarColor` 푸시 (인스펙터 색 무시) |
| `Assets/_Game/Scripts/Editor/BombPrefabCreator.cs` | 에디터 도구 — 모든 스프라이트/프리펩/머티리얼/SO 일괄 생성 |

### 2.2 수정 파일 (1개)

| 파일 | 변경 내용 |
|------|----------|
| `Assets/_Game/Scripts/UI/Weapon/WeaponSlotUI.cs` | 오버레이 자식 추가 (`_coolOverlay`, `_overlayText`), `Update`/`RefreshStatic`/`BuildDefaultHierarchy` 확장 |

### 2.3 Unity 에디터 작업 (코드 외)

- `Assets/_Game/Data/Weapons/Weapon_Bomb.asset` 생성
  - `FireDelay=6`, `Damage=3`, `ThemeColor=#f4a423`, `ExplosionRadius=1.8`, `ProjectileSpeed=5`
- `Assets/_Game/Prefabs/Weapons/BombProjectile.prefab` 생성
  - 플레이스홀더 SpriteRenderer + `BombProjectile` 컴포넌트
- `Assets/_Game/Prefabs/Weapons/BombLandingMarker.prefab` 생성
  - Quad (회전 90,0,0 — XZ 평면 누움) + 반투명 주황 머티리얼 (URP Unlit Transparent, alpha≈0.25)
  - Y=0.02 (지면 z-fighting 방지)
  - `BombLandingMarker` 컴포넌트 (선택 — 펄스 효과)
- 씬 작업
  - `BombWeapon` GameObject 추가 + 데이터 할당
  - AimController 자식에 두 번째 `AimWeaponRing` + `BombAimRingBinder` 추가 (radiusOffset=0.2, 주황)
  - `WeaponSwitcher` 슬롯 2번에 폭탄 할당

---

## 3. 클래스 설계

### 3.1 BombData.cs

`WeaponData` 상속. 추가 필드:

```csharp
[Range(0.5f, 5f)]   private float _explosionRadius = 1.8f;
[Range(1f, 20f)]    private float _projectileSpeed = 5f;
[Range(1f, 10f)]    private float _projectileLifetime = 5f;
                    private GameObject _projectilePrefab;
                    private GameObject _explosionVfxPrefab;
[Range(0.1f, 5f)]   private float _explosionVfxLifetime = 1.5f;
                    private GameObject _landingMarkerPrefab; // 착탄 예정 위치 표시
```

`[CreateAssetMenu(fileName = "Weapon_Bomb", menuName = "Drill-Corp/Weapons/Bomb", order = 30)]`

### 3.2 BombProjectile.cs

**자립형 컴포넌트** — 무기 참조 없이 데이터만 저장. 무기 전환 후에도 정상 폭발.

```csharp
public void Initialize(Vector3 targetPos, BombData data, LayerMask bugLayer)
```

`Initialize()`:
- 타겟 위치/데이터/레이어 보관
- 착탄 마커 스폰 (있으면): `Instantiate(_data.LandingMarkerPrefab, _targetPos, Quaternion.Euler(90,0,0))`
  - 스케일을 `ExplosionRadius * 2` 로 설정 → 업그레이드로 반경 변해도 자동 매칭
  - 마커는 **고정 위치** (투사체 자식 아님). `_marker` 필드로 참조 보관

`Update()`:
- 수명 만료 검사 → `Explode()`
- 타겟 방향으로 `_data.ProjectileSpeed * Time.deltaTime` 이동 (XZ 평면, Y=0 고정)
- 거리 ≤ 1프레임 이동량이면 도달 → `Explode()`

`Explode()`:
- `Physics.OverlapSphereNonAlloc(pos, _data.ExplosionRadius, buffer, _bugLayer)`
- 각 collider → `IDamageable.TakeDamage(_data.Damage)`
- 폭발 VFX 스폰 (`_data.ExplosionVfxPrefab`)
- 각 피격 위치에 Hit VFX (`_data.HitVfxPrefab`)
- **착탄 마커 제거**: `if (_marker != null) Destroy(_marker)`
- `Destroy(gameObject)`

`OnDestroy()`:
- 마커가 아직 살아있으면 함께 파괴 (씬 전환/투사체 강제 제거 안전망)

### 3.2.1 BombLandingMarker.cs (선택 효과)

마커 프리펩에 붙는 단순 컴포넌트. **순수 비주얼만 담당** — 데미지/판정 일체 없음.

```csharp
public class BombLandingMarker : MonoBehaviour
{
    [SerializeField] private Renderer _renderer;
    [SerializeField] private float _baseAlpha = 0.25f;
    [SerializeField] private float _pulseAmplitude = 0.15f;
    [SerializeField] private float _pulseSpeed = 1.5f;

    private void Update()
    {
        if (_renderer == null) return;
        float a = _baseAlpha + Mathf.PingPong(Time.time * _pulseSpeed, _pulseAmplitude);
        var c = _renderer.material.color; c.a = a;
        _renderer.material.color = c;
    }
}
```

원치 않으면 컴포넌트 없이 정적 반투명 디스크로만 둬도 충분.

### 3.3 BombWeapon.cs

**A. 자체 구동 — `Update()`에서 매 프레임 `TryFire(_aimController)` 호출**

`AimController.TryFireWeapon`은 `_currentWeapon`(슬롯 1개)에 대해서만 발사 시도.
폭탄은 슬롯에 들어가지 않으므로 그 흐름을 안 탐 → 자체 Update가 필요.

```csharp
[SerializeField] private AimController _aimController;

private void Start()
{
    if (_aimController == null) _aimController = FindAnyObjectByType<AimController>();
    if (_aimController != null) _aim = _aimController; // UI 프로퍼티가 _aim 참조용
}

private void Update()
{
    if (_aimController != null) TryFire(_aimController);
}
```

**B. 수동 발사 — `ShouldFire` 오버라이드**

```csharp
protected override bool ShouldFire(AimController aim)
{
    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        return false;
    return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
}
```

`WeaponBase.TryFire()`의 `CanFire → ShouldFire → Fire → 쿨다운 갱신` 흐름을 그대로 활용. base 변경 없음.

**C. Fire — 투사체 스폰**

```csharp
protected override void Fire(AimController aim)
{
    Vector3 spawnPos = aim.MachineTransform != null ? aim.MachineTransform.position : aim.AimPosition;
    var obj = Instantiate(_data.ProjectilePrefab, spawnPos, Quaternion.identity);
    obj.GetComponent<BombProjectile>()?.Initialize(aim.AimPosition, _data, aim.BugLayer);
}
```

**D. UI 오버라이드**

| 프로퍼티 | 폭탄 동작 |
|---------|----------|
| `StateText` | `CanFire ? "[클릭]" : "{0.0}s"` |
| `BarColor` | `CanFire ? ReadyBarColor(초록) : 주황(#f4a423)` |
| `BorderColor` | `CanFire ? ReadyBarColor(초록) : IdleBorderColor` |
| `ShowOverlay` | `!CanFire` |
| `OverlayText` | base 사용 (`CooldownRemaining` 포맷) |
| `IsHittingTarget` | `CanFire` (수동 발사이므로 준비 시 항상 hit) |

**E. InfoLabel — `TryFire` 오버라이드**

```csharp
public override void TryFire(AimController aim)
{
    base.TryFire(aim);
    aim?.SetInfoText(CanFire ? "클릭→폭탄" : null);
}
public override void OnUnequip()
{
    _aim?.SetInfoText(null);
    base.OnUnequip();
}
```

### 3.4 BombAimRingBinder.cs

`SniperAimRingBinder` 패턴 그대로:

```csharp
[RequireComponent(typeof(AimWeaponRing))]
public class BombAimRingBinder : MonoBehaviour
{
    [SerializeField] private BombWeapon _weapon;
    private AimWeaponRing _ring;

    private void Awake() {
        _ring = GetComponent<AimWeaponRing>();
        if (_weapon == null) _weapon = FindAnyObjectByType<BombWeapon>();
    }
    private void Update() {
        if (_ring == null || _weapon == null) return;
        _ring.FillAmount = _weapon.CooldownProgress;
        bool ready = _weapon.CanFire;
        _ring.SetState(ready, ready); // 폭탄은 준비 시 항상 hit 색
    }
}
```

### 3.5 WeaponSlotUI.cs 수정

**필드 추가:**
```csharp
[SerializeField] private GameObject _coolOverlay;
[SerializeField] private TMP_Text _overlayText;
```

**`Update()` 끝에 추가:**
```csharp
if (_coolOverlay != null)
{
    bool show = _weapon.ShowOverlay;
    if (_coolOverlay.activeSelf != show) _coolOverlay.SetActive(show);
    if (show && _overlayText != null) _overlayText.text = _weapon.OverlayText;
}
```

**`RefreshStatic()` 잠김 분기에 추가:**
```csharp
if (_coolOverlay != null) _coolOverlay.SetActive(false);
```

**`BuildDefaultHierarchy()` 확장:** CoolBar 뒤에 `CoolOverlay` (검은 반투명 Image, 인셋 2px) + 중앙 `OverlayText` (TMP, 16pt) 자동 생성. 기본 비활성.

---

## 4. 구현 순서

| 단계 | 작업 | 의존성 |
|-----|------|--------|
| 1 | `BombData.cs` | 없음 |
| 2 | `BombProjectile.cs` | `BombData`, `IDamageable` |
| 3 | `BombWeapon.cs` | `BombData`, `BombProjectile` |
| 4 | `BombAimRingBinder.cs` | `BombWeapon`, `AimWeaponRing` |
| 5 | `WeaponSlotUI.cs` 수정 | 없음 |
| 6 | Unity 에디터 (에셋/프리펩/씬) | 1~5 완료 |

---

## 5. 잠재 이슈

| 이슈 | 대응 |
|------|------|
| UI 클릭이 발사로 오인 | `EventSystem.IsPointerOverGameObject()` 가드 |
| 투사체 비행 중 무기 교체 | 투사체가 데이터를 복사 보관, 자립 동작 |
| 다발 투사체 (업그레이드 후) | 각 투사체 독립 — 자연스레 처리됨 |
| 기존 슬롯 프리펩에 오버레이 자식 없음 | `_coolOverlay` null 체크로 회귀 없음. 신규는 Build Default Hierarchy로 생성 |

---

## 6. 검증 체크리스트

- [ ] 클릭 시 머신→에임으로 투사체 비행
- [ ] 도달 시 폭발, 반경 1.8 내 벌레 3 데미지
- [ ] 도달 못해도 `ProjectileLifetime` 후 폭발
- [ ] 6초 쿨다운 동안 재클릭 무시
- [ ] 슬롯 텍스트: 준비 "[클릭]" / 쿨중 "{초}s"
- [ ] 슬롯 바: 준비 초록 / 쿨중 주황
- [ ] 슬롯 오버레이: 쿨중에만 검은 덮개 + 큰 초
- [ ] 크로스헤어: 주황 호 + "클릭→폭탄" 텍스트
- [ ] **착탄 마커**: 클릭 즉시 타겟 위치에 반투명 주황 원 표시, 폭발 시 사라짐
- [ ] **착탄 마커 크기**: ExplosionRadius와 정확히 일치 (지름 = 1.8 × 2)
- [ ] **마커 펄스**(선택): 알파가 부드럽게 펄스
- [ ] 마커는 위치 고정 — 투사체 비행 중 마우스 이동에 영향받지 않음
- [ ] 무기 교체(키 1↔2): 쿨다운 상태 보존
- [ ] UI 클릭 시 발사되지 않음
- [ ] 기존 무기(Sniper/Shotgun/Laser/LockOn) 회귀 없음

# Phase 2 — 폭탄 무기 구현 계획

> 상위 문서: `WEAPON_IMPLEMENTATION_PLAN.md` §5
> 본 문서는 실제 코드베이스 상태를 반영한 **세부 실행 계획**이다.

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

---

## 2. 파일 계획

### 2.1 신규 파일 (4개)

| 파일 | 역할 |
|------|------|
| `Assets/_Game/Scripts/Weapon/Bomb/BombData.cs` | ScriptableObject — 폭발 반경/투사체 속도/프리펩 참조 |
| `Assets/_Game/Scripts/Weapon/Bomb/BombWeapon.cs` | `WeaponBase` 파생 — 클릭 발사 + UI 오버라이드 |
| `Assets/_Game/Scripts/Weapon/Bomb/BombProjectile.cs` | 자립형 투사체 — 이동·폭발·AoE 피해 |
| `Assets/_Game/Scripts/Aim/BombAimRingBinder.cs` | 크로스헤어 호 바인딩 (`SniperAimRingBinder` 패턴) |

### 2.2 수정 파일 (1개)

| 파일 | 변경 내용 |
|------|----------|
| `Assets/_Game/Scripts/UI/Weapon/WeaponSlotUI.cs` | 오버레이 자식 추가 (`_coolOverlay`, `_overlayText`), `Update`/`RefreshStatic`/`BuildDefaultHierarchy` 확장 |

### 2.3 Unity 에디터 작업 (코드 외)

- `Assets/_Game/Data/Weapons/Weapon_Bomb.asset` 생성
  - `FireDelay=6`, `Damage=3`, `ThemeColor=#f4a423`, `ExplosionRadius=1.8`, `ProjectileSpeed=5`
- `Assets/_Game/Prefabs/Weapons/BombProjectile.prefab` 생성
  - 플레이스홀더 SpriteRenderer + `BombProjectile` 컴포넌트
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
```

`[CreateAssetMenu(fileName = "Weapon_Bomb", menuName = "Drill-Corp/Weapons/Bomb", order = 30)]`

### 3.2 BombProjectile.cs

**자립형 컴포넌트** — 무기 참조 없이 데이터만 저장. 무기 전환 후에도 정상 폭발.

```csharp
public void Initialize(Vector3 targetPos, BombData data, LayerMask bugLayer)
```

`Update()`:
- 수명 만료 검사 → `Explode()`
- 타겟 방향으로 `_data.ProjectileSpeed * Time.deltaTime` 이동 (XZ 평면, Y=0 고정)
- 거리 ≤ 1프레임 이동량이면 도달 → `Explode()`

`Explode()`:
- `Physics.OverlapSphereNonAlloc(pos, _data.ExplosionRadius, buffer, _bugLayer)`
- 각 collider → `IDamageable.TakeDamage(_data.Damage)`
- 폭발 VFX 스폰 (`_data.ExplosionVfxPrefab`)
- 각 피격 위치에 Hit VFX (`_data.HitVfxPrefab`)
- `Destroy(gameObject)`

### 3.3 BombWeapon.cs

**A. 수동 발사 — `ShouldFire` 오버라이드**

```csharp
protected override bool ShouldFire(AimController aim)
{
    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        return false;
    return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
}
```

`WeaponBase.TryFire()`의 `CanFire → ShouldFire → Fire → 쿨다운 갱신` 흐름을 그대로 활용. base 변경 없음.

**B. Fire — 투사체 스폰**

```csharp
protected override void Fire(AimController aim)
{
    Vector3 spawnPos = aim.MachineTransform != null ? aim.MachineTransform.position : aim.AimPosition;
    var obj = Instantiate(_data.ProjectilePrefab, spawnPos, Quaternion.identity);
    obj.GetComponent<BombProjectile>()?.Initialize(aim.AimPosition, _data, aim.BugLayer);
}
```

**C. UI 오버라이드**

| 프로퍼티 | 폭탄 동작 |
|---------|----------|
| `StateText` | `CanFire ? "[클릭]" : "{0.0}s"` |
| `BarColor` | `CanFire ? ReadyBarColor(초록) : 주황(#f4a423)` |
| `BorderColor` | `CanFire ? ReadyBarColor(초록) : IdleBorderColor` |
| `ShowOverlay` | `!CanFire` |
| `OverlayText` | base 사용 (`CooldownRemaining` 포맷) |
| `IsHittingTarget` | `CanFire` (수동 발사이므로 준비 시 항상 hit) |

**D. InfoLabel — `TryFire` 오버라이드**

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
- [ ] 무기 교체(키 1↔2): 쿨다운 상태 보존
- [ ] UI 클릭 시 발사되지 않음
- [ ] 기존 무기(Sniper/Shotgun/Laser/LockOn) 회귀 없음

# Drill-Corp 리팩토링 마스터 플랜

> **작성일**: 2026-04-16  
> **대상**: 구현자 AI (이 문서를 참조해 바로 착수)  
> **참조**: `WEAPON_IMPLEMENTATION_PLAN.md`, `Architecture.md`

---

## 1. 현상 요약 (What is)

### 1.1 프로젝트 규모
- **131개 CS 파일**, ~14,000 라인 / Unity 6 + URP 탑다운
- 주요 폴더: Aim(3), Bug(10), Machine(3), Weapon(4), UI(8), Core(4), Data(4), Editor(13), OutGame(7), Wave(1), Camera(3), VFX(2)

### 1.2 핵심 클래스 라인 수
| 클래스 | 라인 | 상태 |
|---|---|---|
| BugController | 1143 | 책임 과중 |
| AimController | 319 | Find* 의존 |
| WeaponSlotUI | 273 | Overlay/AmmoRow 미완 |
| MachineController | 214 | 안정 |
| WeaponBase | 167 | 투사체 헬퍼 부재 |
| TurretController | 117 | 신규(untracked), 단일 배럴 |

### 1.3 현 진행 상태
- **Phase 0 공통 기반**: 완료
- **Phase 1 저격총**: 완료 (SniperWeapon, AimWeaponRing, WeaponSlotUI, 크로스헤어)
- **Phase 2~4 (폭탄/기관총/레이저)**: 설계만 (`WEAPON_IMPLEMENTATION_PLAN.md`)
- **TurretController**: 저격 배럴 1개 동작, 다중 배럴 관리 미완

### 1.4 핵심 문제점
1. **WeaponBase 투사체 발사 로직 없음** — Phase 2~4 구현 불가
2. **TurretController 다중 배럴 미지원** — 무기 교체 시 배럴 활성화 불가
3. **WeaponSlotUI Overlay/AmmoRow 자식 없음** — Phase 2~4 UI 블로커
4. **BugController 1143줄** — 스탯/행동/상태/시각 모두 한 파일
5. **FindAnyObjectByType 18곳 / GameObject.Find 2곳** — 명시적 참조 주입 필요
6. **GameEvents 이벤트명 불일치** — OnMachineDamaged vs OnFuelChanged 등

---

## 2. 목표 상태 (To be)

### 2.1 리팩토링 후 구조
```
Assets/_Game/Scripts/
├── Weapon/
│   ├── WeaponBase.cs          ← 투사체 헬퍼 + 슬롯 표현 확장
│   ├── WeaponData.cs          ← ProjectilePrefab 필드 추가
│   ├── IProjectile.cs         ← 신규 인터페이스
│   ├── Proto/SniperWeapon.cs  ← 무변
│   ├── Bomb/
│   │   ├── BombWeapon.cs      ← 신규 (Phase 2)
│   │   ├── BombWeaponData.cs  ← 신규
│   │   └── BombProjectile.cs  ← 신규
│   ├── Gun/
│   │   ├── GunWeapon.cs       ← 신규 (Phase 3)
│   │   ├── GunWeaponData.cs   ← 신규
│   │   └── GunBullet.cs       ← 신규
│   └── Laser/
│       ├── LaserWeapon.cs     ← 신규 (Phase 4)
│       ├── LaserWeaponData.cs ← 신규
│       └── LaserBeam.cs       ← 신규
│
├── Machine/
│   └── TurretController.cs    ← BarrelConfig + SetActiveBarrel()
│
├── UI/Weapon/
│   └── WeaponSlotUI.cs        ← Overlay/AmmoRow 자식 추가
│
├── Bug/
│   ├── BugController.cs       ← 슬리밍 (조율자)
│   ├── BugStatsController.cs  ← 신규 (HP/버프/배율)
│   └── BugBehaviorManager.cs  ← 신규 (행동 5종 관리)
│
├── Aim/
│   └── AimController.cs       ← Find* 제거, Inspector 주입
│
└── Core/
    └── GameEvents.cs           ← 이벤트명 표준화
```

---

## 3. 리팩토링 트랙

---

### 트랙 1: WeaponBase 확장 (투사체 + 슬롯 표현)

**우선순위**: 높음 / **난이도**: 중 / **선행**: 없음 / **예상**: 4~5시간

#### 현재 코드
`Assets/_Game/Scripts/Weapon/WeaponBase.cs`
- L134: `protected abstract void Fire(AimController aim);` — 투사체 발사 수단 없음
- L85~95: `BarFillAmount`, `BarColor`, `StateText` — 저격 전용, 무기별 의미 분기 미지원
- `ShowAmmoRow`, `AmmoCurrent` 프로퍼티 기본값만 존재

#### 문제점
1. Fire() 내에서 월드에 투사체 스폰 불가 (폭탄/총알/레이저 필요)
2. BarFillAmount: 저격/폭탄=쿨(0→1), 기관총=탄창(1→0), 레이저=수명(1→0) — 의미 통일 안 됨
3. AmmoRow, ShowOverlay 프로퍼티 정의만 있고 실제 사용처 없음

#### 목표 설계

```csharp
// === IProjectile.cs (신규) ===
public interface IProjectile
{
    void Initialize(Vector3 direction, float speed, float damage);
}

// === WeaponBase.cs 추가 멤버 ===

// 투사체 발사 헬퍼 (L165 다음 추가)
protected T SpawnProjectile<T>(GameObject prefab, Vector3 origin, Vector3 direction, float speed)
    where T : MonoBehaviour, IProjectile
{
    if (prefab == null) return null;
    var go = Object.Instantiate(prefab, origin, Quaternion.identity);
    var proj = go.GetComponent<T>();
    proj?.Initialize(direction, speed, _baseData.Damage);
    return proj;
}

// 슬롯 표현 확장 (L85~95 근처)
public virtual bool ShowOverlay => false;
public virtual string OverlayText => "";
public virtual bool ShowAmmoRow => false;
public virtual int AmmoCurrent => 0;
public virtual int AmmoMax => 0;
```

#### 작업 단계
- [ ] `IProjectile.cs` 신규 생성 (`Assets/_Game/Scripts/Weapon/`)
- [ ] `WeaponBase.cs` L165 다음: `SpawnProjectile<T>()` 메서드 추가
- [ ] `WeaponBase.cs` L85~95: `ShowOverlay`, `OverlayText`, `ShowAmmoRow`, `AmmoCurrent`, `AmmoMax` virtual 프로퍼티 추가
- [ ] `WeaponData.cs`: `ProjectilePrefab` 필드 추가 (GameObject 참조)
- [ ] 파생 Data 클래스 3개 신규:
  - `BombWeaponData : WeaponData` — Radius, Duration, Speed
  - `GunWeaponData : WeaponData` — MaxAmmo, ReloadDuration, Accuracy
  - `LaserWeaponData : WeaponData` — BeamDuration, Range, TickInterval
- [ ] SniperWeapon 동작 무변 확인

#### 영향 범위
- **수정**: `WeaponBase.cs`, `WeaponData.cs`
- **신규**: `IProjectile.cs`, `BombWeaponData.cs`, `GunWeaponData.cs`, `LaserWeaponData.cs`
- **연쇄**: SniperWeapon 무영향, WeaponSlotUI는 트랙 3에서 읽기 추가

#### 검증
- SniperWeapon 기존 동작 동일
- `SpawnProjectile<BombProjectile>(...)` 컴파일 통과
- BarFillAmount/ShowOverlay 기본값으로 Phase 1 UI 무변

---

### 트랙 2: TurretController 다중 배럴 관리

**우선순위**: 높음 / **난이도**: 중 / **선행**: 없음 / **예상**: 3~4시간

#### 현재 코드
`Assets/_Game/Scripts/Machine/TurretController.cs`
- L75~113: `BuildDefaultHierarchy()`에서 저격 배럴만 생성 (`Barrel_Sniper`)
- L52~72: `Update()`에서 Pivot을 마우스 방향으로 Y축 회전 — 모든 자식이 함께 회전
- L43: `FindAnyObjectByType<AimController>()` — 명시적 할당 권장
- 비활성화/활성화 로직 전무

#### 목표 설계

```csharp
// === 배럴 설정 ===
[System.Serializable]
public class BarrelConfig
{
    public string WeaponId;                     // "Sniper", "Bomb", "Gun", "Laser"
    [SerializeField] public Transform BarrelTransform;
}

// === TurretController.cs 확장 ===
[SerializeField] private List<BarrelConfig> _barrels = new();
private Dictionary<string, Transform> _barrelMap = new();

public void SetActiveBarrel(string weaponId)
{
    foreach (var barrel in _barrels)
        barrel.BarrelTransform?.gameObject.SetActive(barrel.WeaponId == weaponId);
}

public bool HasBarrel(string weaponId) => _barrelMap.ContainsKey(weaponId);
```

**씬 계층 구조:**
```
DrillMachine
└── Turret (TurretController)
    ├── Base
    └── Pivot (Y축 회전)
        ├── Barrel_Sniper  ← Phase 1 (기존)
        ├── Barrel_Bomb    ← Phase 2 (주황)
        ├── Barrel_Gun     ← Phase 3 (하늘)
        └── Barrel_Laser   ← Phase 4 (빨강)
```

#### 작업 단계
- [ ] `BarrelConfig` 클래스 정의 (중첩 또는 분리)
- [ ] `_barrels` List + `_barrelMap` Dictionary 추가
- [ ] `Awake()`에서 Map 구성: `foreach (var cfg in _barrels) _barrelMap[cfg.WeaponId] = cfg.BarrelTransform;`
- [ ] `SetActiveBarrel(string weaponId)` 메서드 추가
- [ ] `HasBarrel(string weaponId)` 메서드 추가
- [ ] `BuildDefaultHierarchy()` 개선: 4개 배럴 동시 생성 + 색상 구분 + `_barrels` 자동 할당
- [ ] `FindAnyObjectByType<AimController>()` → `[SerializeField]` 우선 + Fallback 유지

#### 영향 범위
- **수정**: `TurretController.cs`
- **연쇄**: 무기 장착/교체 로직에서 `turretController.SetActiveBarrel(weaponId)` 호출 필요

#### 검증
- Phase 1: Sniper 배럴만 활성, 나머지 비활성
- `SetActiveBarrel("Bomb")` → Bomb 배럴만 활성 + 회전

---

### 트랙 3: WeaponSlotUI Overlay / AmmoRow 자식 구조

**우선순위**: 높음 / **난이도**: 낮음 / **선행**: 트랙 1 / **예상**: 2~3시간

#### 현재 코드
`Assets/_Game/Scripts/UI/Weapon/WeaponSlotUI.cs`
- L28~42: UI 참조 — `_iconImage`, `_nameText`, `_stateText`, `_coolBarFill`, `_border`, `_lockedOverlay`
- L97~110: `Update()` — 아이콘/이름/상태/쿨바만 렌더
- Overlay(검은 덮개 + 큰 초 텍스트) 없음 → Phase 2 폭탄 블로커
- AmmoRow(pip 행) 없음 → Phase 3 기관총 블로커

#### 목표 설계

```csharp
// === 신규 필드 (L50 다음) ===
[SerializeField] private GameObject _overlayContainer;
[SerializeField] private Image _overlayBackground;
[SerializeField] private TMP_Text _overlayText;
[SerializeField] private GameObject _ammoRowContainer;
[SerializeField] private Image[] _ammoPips;
[SerializeField] private int _maxAmmoPips = 40;

// === Update() 확장 (L110 다음) ===

// Overlay 갱신
if (_overlayContainer != null)
{
    bool show = _weapon.ShowOverlay;
    _overlayContainer.SetActive(show);
    if (show && _overlayText != null)
        _overlayText.text = _weapon.OverlayText;
}

// AmmoRow 갱신
if (_ammoRowContainer != null && _ammoPips != null && _ammoPips.Length > 0)
{
    bool show = _weapon.ShowAmmoRow;
    _ammoRowContainer.SetActive(show);
    if (show)
    {
        int current = _weapon.AmmoCurrent;
        for (int i = 0; i < _ammoPips.Length; i++)
            _ammoPips[i].color = (i < current) ? Color.white : new Color(1, 1, 1, 0.3f);
    }
}
```

**UI 계층:**
```
WeaponSlot (RectTransform)
├── Border / Background / Icon / Name / Level / State / CoolBar  ← 기존
├── Overlay (신규)
│   ├── OverlayBackground (반투명 검정 Image, alpha 0.7)
│   └── OverlayText (TMP, 60pt, 큰 초 텍스트)
└── AmmoRow (신규)
    └── Pip_0~39 (Image x40, HorizontalLayoutGroup)
```

#### 작업 단계
- [ ] 신규 필드 6개 추가 (L50 다음)
- [ ] `Update()` 확장: ShowOverlay/ShowAmmoRow 읽기 + UI 갱신
- [ ] `BuildDefaultHierarchy()` 확장 (L260 다음):
  - Overlay 자식: 검은 반투명 Image + 60pt TMP 텍스트
  - AmmoRow 자식: HorizontalLayoutGroup + Pip Image x40
  - 둘 다 `SetActive(false)` 초기 비활성

#### 영향 범위
- **수정**: `WeaponSlotUI.cs`
- **연쇄**: BombWeapon/GunWeapon/LaserWeapon에서 `ShowOverlay`/`ShowAmmoRow` 오버라이드 필요

#### 검증
- Phase 1 저격: Overlay/AmmoRow 비활성 (기존 동작 무변)
- 수동 `_overlayContainer.SetActive(true)` → 검은 덮개 + 텍스트 표시
- 수동 `_ammoRowContainer.SetActive(true)` → 40개 pip 표시

---

### 트랙 4: AimController 결합도 감소

**우선순위**: 중 / **난이도**: 낮음 / **선행**: 없음 (병렬) / **예상**: 1~2시간

#### 현재 코드
`AimController.cs:192~197`
```csharp
private void EnsureMachineTransform()
{
    if (_machineTransform == null)
    {
        var obj = GameObject.FindGameObjectWithTag("Machine");
        if (obj != null)
            _machineTransform = obj.transform;
    }
}
```

동일 패턴: `DynamicCamera.cs:39`, `DebugCameraUI.cs`

#### 목표 설계
```csharp
// Inspector 할당 우선, Fallback으로만 자동 탐색
[SerializeField] private Transform _machineTransform;

private void EnsureMachineTransform()
{
    if (_machineTransform != null) return;
    var controller = FindAnyObjectByType<MachineController>();
    if (controller != null)
        _machineTransform = controller.transform;
}
```

#### 작업 단계
- [ ] `AimController.cs:192~197` — `FindAnyObjectByType<MachineController>()` 우선으로 변경
- [ ] `DynamicCamera.cs:39` — 동일 패턴 적용
- [ ] `DebugCameraUI.cs` — 동일 패턴 적용
- [ ] 모든 대상 클래스에 `[SerializeField] private Transform _machineTransform` 확인/추가

#### 영향 범위
- **수정**: `AimController.cs`, `DynamicCamera.cs`, `DebugCameraUI.cs`
- **연쇄**: 없음 (순수 내부 리팩토링)

#### 검증
- 게임 시작 → 에임/카메라 초기화 정상
- Inspector 미할당 시 자동 탐색 Fallback 동작

---

### 트랙 5: BugController 책임 분리

**우선순위**: 중 / **난이도**: 높음 / **선행**: 트랙 1~3 권장 / **예상**: 6~8시간

#### 현재 코드
`Assets/_Game/Scripts/Bug/BugController.cs` — 1143줄

**책임 분류:**
- L36~61: **스탯 필드** (HP, 속도, 데미지, 버프 Dictionary, 배율)
- L64~74: **행동 필드** (Movement/Attack/Skill/Passive/Trigger + Conditional 리스트)
- L150~600: **스탯 메서드** (TakeDamage, Heal, ApplyBuff, RemoveBuff, ModifySpeed/Damage)
- L600~900: **행동 메서드** (UpdateMovement, UpdateAttack, UpdateSkills, CheckTriggers)
- L900~1143: **상태/시각** (Flash, SpawnDeathVfx, SetDead)

#### 목표 설계

```csharp
// === BugStatsController.cs (신규, ~200줄) ===
public class BugStatsController
{
    public float CurrentHealth { get; private set; }
    public float MaxHealth { get; private set; }
    public float MoveSpeed => _baseSpeed * _speedMultiplier;
    public float AttackDamage => _baseDamage * _damageMultiplier;

    private Dictionary<object, BuffInfo> _activeBuffs;
    private float _speedMultiplier = 1f, _damageMultiplier = 1f;

    public void Initialize(BugData data) { ... }
    public void TakeDamage(float damage) { ... }
    public void Heal(float amount) { ... }
    public void ApplyBuff(object source, BuffInfo buff) { ... }
    public void RemoveBuff(object source) { ... }
}

// === BugBehaviorManager.cs (신규, ~300줄) ===
public class BugBehaviorManager
{
    private IMovementBehavior _currentMovement, _defaultMovement;
    private List<ConditionalBehavior<IMovementBehavior>> _conditionalMovements;
    private IAttackBehavior _currentAttack, _defaultAttack;
    private List<ConditionalBehavior<IAttackBehavior>> _conditionalAttacks;
    private List<ISkillBehavior> _skills;
    private List<IPassiveBehavior> _passives;
    private List<ITriggerBehavior> _triggers;

    public void Initialize(BugBehaviorData data, BugController owner) { ... }
    public void UpdateBehaviors(Transform target, float dt) { ... }
    public void CheckTriggers() { ... }
}

// === BugController.cs (슬리밍, ~400줄) ===
public class BugController : MonoBehaviour, IDamageable
{
    private BugStatsController _stats;
    private BugBehaviorManager _behaviors;

    public void TakeDamage(float damage) => _stats.TakeDamage(damage);
    // 상태/시각 + 조율 로직만 남김
}
```

#### 작업 단계

**Phase A: 의존도 분석 (1h)**
- [ ] BugController 모든 메서드를 스탯/행동/상태·시각으로 분류
- [ ] 교차 참조 확인 (행동이 스탯을 읽는 지점 목록화)

**Phase B: BugStatsController 추출 (1.5h)**
- [ ] `BugStatsController.cs` 신규 생성
- [ ] L36~61 스탯 필드 이동
- [ ] TakeDamage/Heal/ApplyBuff/RemoveBuff/ModifySpeed/ModifyDamage 이동
- [ ] BugController Awake에서 `_stats = new BugStatsController()` 초기화

**Phase C: BugBehaviorManager 추출 (2h)**
- [ ] `BugBehaviorManager.cs` 신규 생성
- [ ] L64~74 행동 필드 이동
- [ ] Initialize/UpdateMovement/UpdateAttack/UpdateSkills/CheckTriggers 이동
- [ ] BugController Awake에서 `_behaviors = new BugBehaviorManager()` 초기화

**Phase D: BugController 슬리밍 (1.5h)**
- [ ] 추출된 필드/메서드 제거
- [ ] IDamageable.TakeDamage → `_stats.TakeDamage` 위임
- [ ] Update → `_behaviors.UpdateBehaviors()` + `_stats` 상태 체크

**Phase E: 통합 테스트 (1h)**
- [ ] 기존 벌레 스폰/동작/사망 동일 확인
- [ ] 버프/디버프 적용 테스트
- [ ] 행동 전환 (HP% 조건부) 테스트

#### 영향 범위
- **수정**: `BugController.cs`
- **신규**: `BugStatsController.cs`, `BugBehaviorManager.cs`
- **연쇄**: BugController를 참조하는 클래스들 — 대부분 Initialize/TakeDamage만 호출하므로 인터페이스 무변

#### 검증
- 모든 버그 타입(Beetle, Centipede, Fly 등) 스폰·행동·사망 동일
- BugStatsController 단독 테스트 가능 (MonoBehaviour 의존 없음)

---

### 트랙 6: GameEvents 이벤트명 표준화

**우선순위**: 낮음 / **난이도**: 낮음 / **선행**: 없음 (병렬) / **예상**: 1시간

#### 현재 이벤트 목록
`Assets/_Game/Scripts/Core/GameEvents.cs`
```csharp
public static Action<GameState> OnGameStateChanged;
public static Action<float> OnMachineDamaged;
public static Action OnMachineDestroyed;
public static Action<float> OnFuelChanged;
public static Action<int> OnBugKilled;
public static Action<int> OnWaveStarted;
public static Action<int> OnWaveCompleted;
public static Action OnSessionSuccess;
public static Action OnSessionFailed;
public static Action<int> OnCurrencyChanged;
public static Action<int> OnMiningGained;
public static Action<string, int> OnUpgradePurchased;
public static Action<int> OnMachineSelected;
```

#### 변경 맵
| 현재 | 변경 후 | 사유 |
|---|---|---|
| `OnMachineDamaged` | `OnMachineHealthChanged` | 회복도 포함 가능 |
| `OnFuelChanged` | `OnMachineFuelChanged` | 대상 명시 |
| `OnBugKilled` | `OnBugDied` | 자연사 등 포괄 |
| `OnSessionSuccess` | `OnSessionCompleted` | 과거형 일관 |
| `OnMiningGained` | `OnMiningResourceGained` | 명확성 |

**신규 추가 (Phase 2~4용):**
```csharp
public static Action<string> OnWeaponEquipped;
public static Action<string> OnWeaponUnequipped;
```

#### 작업 단계
- [ ] `GameEvents.cs` 이벤트명 5개 변경 + 2개 신규 추가
- [ ] 발행처 Grep → 이름 변경 (GameManager, MachineController, BugController, WaveManager)
- [ ] 구독처 Grep → 이름 변경 (UI 클래스 5~10개)
- [ ] 컴파일 에러 0건 확인

#### 영향 범위
- **수정**: `GameEvents.cs` + 발행/구독처 10~15개 파일
- **리스크**: 낮음 (컴파일 에러로 전부 드러남)

---

## 4. Phase 2~4 구현 의존성 맵

```
[트랙 1: WeaponBase 확장] ──┬──→ [Phase 2: BombWeapon] ──┐
                             │     └─ [트랙 3: Overlay]     │
[트랙 2: TurretController] ─┘                              │
                                                            ├──→ [Phase 3: GunWeapon]
[트랙 4: AimController]  ── 병렬 ──────────────────────────│     └─ [트랙 3: AmmoRow]
[트랙 6: GameEvents]     ── 병렬 ──────────────────────────│
                                                            └──→ [Phase 4: LaserWeapon]

[트랙 5: BugController] ── Phase 2~4 완료 후 ─────────────────→ [완성]
```

### 실행 순서 권장
| 주차 | 작업 | 병렬 가능 |
|---|---|---|
| 1주차 | 트랙 1 + 트랙 2 + 트랙 4 | O |
| 2주차 | 트랙 3 + Phase 2 BombWeapon | - |
| 3주차 | Phase 3 GunWeapon | - |
| 4주차 | Phase 4 LaserWeapon + 트랙 5, 6 | O |

---

## 5. 신규 작업 (리팩토링 외, Phase 구현 전제)

### 5-1. BombProjectile
**위치**: `Assets/_Game/Scripts/Weapon/Bomb/BombProjectile.cs`
```csharp
public class BombProjectile : MonoBehaviour, IProjectile
{
    public void Initialize(Vector3 direction, float speed, float damage) { ... }
    // XZ 평면 이동 → lifetime 후 Explode → OverlapSphere 범위 피해
}
```
프리팹: Sphere(0.2), 주황 머티리얼, SphereCollider

### 5-2. GunBullet
**위치**: `Assets/_Game/Scripts/Weapon/Gun/GunBullet.cs`
```csharp
public class GunBullet : MonoBehaviour, IProjectile
{
    public void Initialize(Vector3 direction, float speed, float damage) { ... }
    // 직선 이동 → OnTriggerEnter 첫 충돌 피해 → Destroy
}
```
프리팹: Capsule(0.05), 하늘 머티리얼, CapsuleCollider(isTrigger)

### 5-3. LaserBeam
**위치**: `Assets/_Game/Scripts/Weapon/Laser/LaserBeam.cs`
```csharp
public class LaserBeam : MonoBehaviour, IProjectile
{
    public void Initialize(Vector3 direction, float speed, float damage) { ... }
    // 에임 추적 이동 → tickInterval마다 OverlapSphere 틱 피해
}
```
프리팹: LineRenderer + 빨강 머티리얼

### 5-4. WeaponSlotUI Overlay/AmmoRow GameObject
- `BuildDefaultHierarchy()` 확장으로 자동 생성 (트랙 3에 포함)
- Overlay: 반투명 검정 Image + 60pt TMP
- AmmoRow: HorizontalLayoutGroup + Pip Image x40

---

## 6. 위험 및 완화

| 위험 | 가능성 | 영향 | 완화 |
|---|---|---|---|
| BugController 분리 후 행동 이상 | 중 | 높음 | 단계별 커밋, 각 단계 후 플레이 테스트 |
| WeaponBase 추상 멤버 추가 시 파생 누락 | 낮음 | 중 | 컴파일 에러로 전수 드러남 |
| TurretController 배럴 활성화 버그 | 낮음 | 중 | SetActiveBarrel() 단위 테스트 |
| GameEvents 이름 변경 시 구독 누락 | 낮음 | 낮음 | 컴파일 에러 + Grep 검증 |
| FindAnyObjectByType 제거 후 씬 순서 문제 | 낮음 | 중 | Fallback 유지 + 에러 로그 |

---

## 7. 구현자 체크리스트

### 사전
- [ ] `WEAPON_IMPLEMENTATION_PLAN.md` 통독 (특히 §4.6 슬롯 UI 전략)
- [ ] 좌표계 확인: X(좌우) / Y(높이) / Z(상하), `Vector3.forward` = 화면 위
- [ ] `CLAUDE.md` 코딩 컨벤션 확인: `_camelCase` / `PascalCase` / `OnEventName`

### 트랙별
- [ ] 트랙 1: IProjectile + SpawnProjectile + 파생 Data 3종
- [ ] 트랙 2: BarrelConfig + SetActiveBarrel + 4배럴 빌드
- [ ] 트랙 3: Overlay/AmmoRow 필드 + Update 확장 + BuildDefaultHierarchy
- [ ] 트랙 4: Find* → Inspector 주입 (3개 파일)
- [ ] 트랙 5: BugStatsController + BugBehaviorManager 추출
- [ ] 트랙 6: GameEvents 5개 이름 변경 + 2개 신규

### 최종
- [ ] Phase 1 저격총 기존 동작 무변
- [ ] 모든 버그 타입 스폰/행동/사망 정상
- [ ] 컴파일 에러 0건
- [ ] 에디터 콘솔 워닝 증가 없음

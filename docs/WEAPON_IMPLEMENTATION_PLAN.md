# 무기 시스템 구현 계획 (수직 슬라이스)

> 목적: `_.html` 프로토타입의 무기 4종(저격총·폭탄·기관총·레이저)을 Unity로 이식하는 **단계별 실행 계획**
> 분석 문서: `docs/AIM_PROTOTYPE.md`
> 방식: 수직 슬라이스 — 각 무기를 **로직 + 좌측 슬롯 + 크로스헤어** 한 세트로 묶어 하나씩 완성

---

## 1. 개발 원칙

1. **무기 하나 = 수직 슬라이스 하나** — 로직/슬롯/크로스헤어 동시 개발
2. **4종 모두 구현된 후에야 실제 사용** — 중간 단계는 테스트용 해금 상태
3. **해금/성장 시스템은 별도 단계** — 이 문서 범위 외
4. **신규 파일로 격리** — `Assets/_Game/Scripts/Weapon/` 폴더에 신규 작성
5. **기존 시스템과 인터페이스 공유** — `IDamageable`, `SimpleBug` 등은 그대로 활용

---

## 2. 폴더 구조 (목표)

```
Assets/_Game/Scripts/
├── Aim/                          ← 기존, AimController 확장
│   └── AimController.cs
├── Weapon/                       ← 신규
│   ├── WeaponBase.cs            (추상)
│   ├── WeaponData.cs            (SO 기반)
│   ├── TurretController.cs      (배럴 회전)
│   ├── Sniper/
│   │   ├── SniperWeapon.cs
│   │   └── SniperWeaponData.cs
│   ├── Bomb/
│   │   ├── BombWeapon.cs
│   │   ├── BombProjectile.cs
│   │   └── BombWeaponData.cs
│   ├── Gun/
│   │   ├── GunWeapon.cs
│   │   ├── GunBullet.cs
│   │   └── GunWeaponData.cs
│   └── Laser/
│       ├── LaserWeapon.cs
│       ├── LaserBeam.cs
│       └── LaserWeaponData.cs
└── UI/Weapon/                    ← 신규
    ├── WeaponPanelUI.cs
    ├── WeaponSlotUI.cs
    └── CrosshairHUD.cs

Assets/_Game/Data/Weapons/        ← 신규
├── Weapon_Sniper.asset
├── Weapon_Bomb.asset
├── Weapon_Gun.asset
└── Weapon_Laser.asset

Assets/_Game/Prefabs/
├── Weapons/                      ← 신규
│   ├── BombProjectile.prefab
│   ├── GunBullet.prefab
│   └── LaserBeam.prefab
└── UI/
    ├── WeaponSlot.prefab
    └── CrosshairHUD.prefab
```

---

## 3. Phase 0 — 공통 기반 구축

무기 1번 들어가기 전 **모든 무기가 공유할 틀**을 먼저 만든다.

### 3-1. AimController
- 마우스 → XZ 평면 월드 좌표 (`Plane(Vector3.up, Vector3.zero).Raycast`)
- `Vector3 AimPos { get; }` 프로퍼티 노출
- `cursor:none` 처리 (Cursor.visible = false)
- 에디터 Gizmo (aim 위치 원)

### 3-2. WeaponBase (추상)
```csharp
public abstract class WeaponBase : MonoBehaviour {
    public abstract string DisplayName { get; }
    public abstract Color ThemeColor { get; }
    public abstract float CooldownRatio { get; }   // 0~1 (0=준비, 1=쿨 꽉)
    public abstract string StateText { get; }       // "대기"/"발사!"/"3.5s"
    public abstract Color BarColor { get; }
    public virtual bool HasAmmo => false;           // 기관총만 override
    public virtual int CurrentAmmo => 0;
    public virtual int MaxAmmo => 0;
    public virtual bool IsReloading => false;
    public bool IsUnlocked { get; set; } = true;    // 기본 해금 (테스트용)
    public abstract void Tick(float dt, Vector3 aimPos);
    public virtual void TryFire(Vector3 aimPos) {}  // 수동 발사용 (폭탄)
}
```

### 3-3. WeaponData (SO 기반)
```csharp
public class WeaponData : ScriptableObject {
    public string DisplayName;
    public Color ThemeColor;
    public Sprite Icon;
    public float Cooldown;
    public float Damage;
    public float Range;
}
```
무기별 확장은 `SniperWeaponData : WeaponData` 형태로 상속.

### 3-4. TurretController (A 방식 배럴)
머신 자식으로 **빈 GameObject "Turret"** 생성:
```
DrillMachine (기존 Cube)
└── Turret (빈, Y축 회전)
    ├── SniperBarrel (보라 세로 Cube, 0.15 × 0.15 × 1.0)
    ├── GunBarrel (하늘 Cube, 아래쪽 배치)
    └── LaserBarrel (빨강 Cube, 위쪽 배치)
```
- `Turret.rotation = Quaternion.LookRotation(aimPos - turret.position)` (XZ만)
- 각 배럴은 `WeaponBase.IsUnlocked == false` 면 `SetActive(false)`

### 3-5. WeaponPanelUI + WeaponSlotUI (빈 틀)
- 좌측 상단에 **Canvas(Screen Space)** 생성
- `WeaponSlotUI` 프리팹: 아이콘/이름/레벨/쿨텍스트/쿨바/오버레이
- 빈 4개 슬롯 배치 (해금은 추후)

### 3-6. CrosshairHUD (중앙 십자선만)
- **World Space Canvas**, AimController 따라다님
- 중앙 십자선 + 작은 중앙 점만 먼저 (무기별 원/호는 각 무기 슬라이스에서)
- 색 전환용 프로퍼티 준비

### Phase 0 완료 기준
- [ ] 마우스 움직이면 크로스헤어가 월드에서 따라감
- [ ] 굴착기 Turret이 마우스 방향으로 회전
- [ ] 좌측 패널에 빈 슬롯 4개 표시
- [ ] 아무 무기도 없어 적은 못 죽지만 **시각 요소 전부 동작**

---

## 4. Phase 1 — 저격총 (가장 단순)

### 4-1. 로직 (`SniperWeapon`) ✅ 완료
- **자동 발사**: 쿨다운 끝나고 범위 내 타겟 있으면 발사
- **범위 내 전체 피격**: `AimController.BugsInRange` 리스트 전체 피해
- **쿨다운은 실제 발사 시에만 소비** (타겟 없으면 쿨 안 돌아감)
- 데이터: `Cooldown=0.4`, `Damage=1`, UseAimRadius=true
- 파일: `Scripts/Weapon/Proto/SniperWeapon.cs`, `SniperWeaponData.cs`
- 에셋: `Data/Weapons/Weapon_Sniper.asset`

### 4-2. 에임 쿨다운 호 ✅ 완료
- **동적 메시 기반** (Canvas 대신 MeshFilter/MeshRenderer)
- **12시부터 시계방향 채움**: 쿨 0 → 호 없음, 쿨 1 → 완전한 원
- **양면 렌더링 + 알파 투명** 자동 설정
- **다중 무기 확장 가능**: 각 무기마다 RadiusOffset 다르게 자식 추가
- **상태별 색 전환**: 쿨다운 중 / 준비+타겟 / 준비+대기
- 파일: `Scripts/Aim/AimWeaponRing.cs`, `SniperAimRingBinder.cs`
- 씬 위치: `Aim/SniperRing`

### 4-3. 좌측 슬롯 (저격총) ✅ 완료
- 아이콘 + 이름 "저격총" + 레벨 "Lv.1"
- 상태 텍스트: **쿨중→"{남은초}s"** / **준비+타겟→"발사!"** / **준비+대기→"대기"**
- 쿨바: 에임 호와 **동기화** — 항상 CooldownProgress 표시 (타겟 무관)
- 쿨바 색: 쿨중 보라 `#e040fb` / 준비 초록 `#51cf66`
- 슬롯 테두리: 준비+타겟 시 보라 강조
- 파일: `Scripts/UI/Weapon/WeaponSlotUI.cs`, `WeaponPanelUI.cs`
- 자동 빌드: `[ContextMenu("Build Default Hierarchy")]` 로 자식 7개 + D2Coding 폰트 + Square_White 스프라이트 원클릭 구성
- 프리팹: `Assets/_Game/Prefabs/UI/WeaponSlot_Sniper.prefab`

### 4-4. 크로스헤어 기타 ⏳ 예정
- **범위 원**: 현재는 기존 크로스헤어 스프라이트 사용 중 (유지)
- **십자선 색**: 타겟 있을 때 보라로 변경 (개선 여지)

### 4-5. TurretController ⏳ 예정
- 저격 배럴만 표시 (다른 배럴은 비활성)

### Phase 1 진행 상황
- [x] 마우스 근처 SimpleBug 자동 처치
- [x] 에임 쿨다운 호가 12시부터 시계방향 회전
- [x] 쿨다운 진행이 에임 호에 실시간 반영
- [x] 슬롯에 "대기/발사!/쿨" 상태가 실시간 반영 (쿨바 에임과 동기화)
- [ ] 굴착기 포탑 배럴이 에임 방향 회전

---

## 4.5. 구현 중 얻은 기술 노트

### 4.5.1 SimpleBug Hitbox 자동 생성
- `SimpleBug.Initialize()`에서 자동으로 SphereCollider(isTrigger) + Bug 레이어 설정
- 프리팹을 수동 수정하지 않아도 무기가 감지 가능
- 옵션: `_autoSetupHitbox`, `_hitboxRadius`

### 4.5.2 Aim GameObject 구조 특성
- Aim 자체가 이미 X축 90° 회전 + Scale 0.5 적용됨 (탑뷰)
- 자식은 Transform 기본값(회전 0, scale 1)으로 두면 부모 회전 자동 상속
- 동적 메시는 **삼각형 감기 방향을 반시계방향**으로 해야 카메라를 향함

### 4.5.3 AimWeaponRing 설계 포인트
- MeshFilter/MeshRenderer 자동 추가 (`[RequireComponent]`)
- URP/Legacy Unlit 쉐이더 순차 탐색 + 양면 렌더링
- `FillAmount` 변화 시에만 메시 재구성 (성능)
- `AimRadius` 실시간 반영 (에임 반경 변해도 따라감)

### 4.5.4 WeaponSlotUI 자동 빌드
- `[ContextMenu("Build Default Hierarchy")]` 지원
- 빈 GameObject + WeaponSlotUI 컴포넌트에서 한 번 실행하면
  Border/Background/Icon/Name/Level/State/CoolBarBg+Fill 자식 자동 생성
- TMP 폰트 D2Coding 자동 적용 (에디터 전용, AssetDatabase 경로)
- CoolBar Image에 `Square_White.png` 자동 할당 (필수 — 아래 4.5.5 참조)
- 기존 자식 clear 후 재생성 — Undo 지원

### 4.5.5 Filled Image는 Sprite 필수
- `Image.Type = Filled` + `Sprite = null` → `fillAmount` 값이 **시각적으로 반영 안 됨**
  (무슨 값을 넣어도 풀로 보이거나 아예 안 보임)
- 해결: Stretch 전체 단색이라도 **아무 Sprite든 할당**해야 함
- 프로젝트 표준: `Assets/_Game/Sprites/UI/Square_White.png` 사용

### 4.5.6 쿨바 = 에임 호 동기화 원칙
- 프로토타입 `_.html`은 "타겟 없으면 바 숨김" 스펙이지만
  Unity에선 **슬롯 바와 에임 호가 같은 쿨타임을 독립적으로 표현**하므로
  불일치가 버그처럼 보임
- Drill-Corp 정책: `BarFillAmount => CooldownProgress` 를 기본값으로 두고
  타겟 게이팅 제거 → 에임 호와 완전 동기화
- 레벨업으로 `FireDelay`가 줄어도 두 UI가 같은 속도로 빨라짐

---

## 4.6 슬롯 UI 확장 전략 (무기별 쿨바 차이)

> 출처: `AIM_PROTOTYPE.md` §10-11 / `_.html` `updateWeaponUI` (L270-282)
> Phase 1은 저격총만 대응 → Phase 2~4 진입 시 무기별로 쿨바/상태/오버레이 거동이 달라 확장 필요

### 4.6.1 무기별 쿨바 거동 요약

| 무기 | 쿨바 의미 | 색상 | 채움 방향 | 특수 요소 |
|---|---|---|---|---|
| **저격총** | 쿨다운 진행 | 초록 ↔ 보라 `#e040fb` | 빈→가득 (왼→오) | 에임 호와 동기화 — 타겟 유무 무관하게 항상 쿨 표시 |
| **폭탄** | 쿨다운 진행 | 초록 ↔ 주황 `#f4a423` | 빈→가득 | **쿨 오버레이** (검은 덮개 + 큰 초) |
| **기관총** | **탄창 잔량** | 하늘 `#4fc3f7` ↔ 빨강 `#ff6b6b` | **가득→빈** (쏠수록 감소) | **탄 pip row**, 탄≤8 테두리 경고, 리로딩 별도 바 |
| **레이저** | **3단계** | 핑크/진빨/초록 | 상태별 상이 | 활성/쿨/준비 3상태, 오버레이 |

### 4.6.2 현재 구현 상태 (Phase 1 완료 기준)

`WeaponSlotUI` v2 — **추상 프로퍼티 기반 렌더러**:
- `WeaponBase.BarFillAmount` / `BarColor` / `StateText` / `BorderColor` 만 읽음 → 무기별 분기 없음
- `ShowOverlay` / `OverlayText` / `ShowAmmoRow` / `AmmoCurrent` / `AmmoMax` 훅은 기본값(false/0) 정의됨 (Phase 2~4에서 실제 연결)
- 저격총은 WeaponBase 기본 구현으로 동작 — 별도 오버라이드 없음

**아직 없는 UI 요소** (Phase 2~4에서 확장):
- [ ] 쿨 오버레이 자식 (검은 덮개 + 큰 초) — Phase 2 폭탄
- [ ] 탄알 pip row 자식 — Phase 3 기관총
- [ ] 역방향 채움 로직 — Phase 3 기관총 탄창
- [ ] 테두리 경고색 / 3단계 상태 — Phase 3~4

### 4.6.3 목표 아키텍처 — `WeaponBase`에 슬롯 표현 추상화

슬롯은 **한 벌의 코드**로 모든 무기를 표시. 무기별 차이는 각 `WeaponBase` 파생이 오버라이드로 담당.

```csharp
public abstract class WeaponBase
{
    // 기존 (Phase 1)
    public virtual float CooldownProgress { get; }
    public virtual bool CanFire { get; }
    public virtual bool HasTarget { get; }
    public string DisplayName { get; }
    public Color ThemeColor { get; }

    // 신규 (Phase 2~4에서 점진 추가)
    // 쿨바는 에임 호와 동일한 CooldownProgress 를 표현 → 부드러운 애니메이션 보장.
    // 레벨업으로 쿨이 줄면 쿨바도 그만큼 빨리 차오름 (같은 속도).
    public virtual float BarFillAmount => CooldownProgress;
    public virtual Color BarColor => CanFire ? readyGreen : ThemeColor;
    public virtual string StateText => CanFire ? (HasTarget ? "발사!" : "대기")
                                                 : $"{CooldownRemaining:0.0}s";
    public virtual Color BorderColor => (CanFire && HasTarget) ? ThemeColor : idleBorder;
    public virtual bool ShowOverlay => false;
    public virtual string OverlayText => $"{CooldownRemaining:0.0}s";
    public virtual bool ShowAmmoRow => false;       // 기관총 전용
    public virtual int AmmoCurrent => 0;
    public virtual int AmmoMax => 0;
}
```

`WeaponSlotUI.Update()`는 위 프로퍼티만 읽음 → 무기별 분기 없음.

### 4.6.4 Phase별 UI 작업 체크리스트

#### Phase 2 — 폭탄 (추가 필요분)

- [ ] `WeaponSlotUI`에 **Overlay 자식** 추가 (검은 반투명 덮개 + 큰 초 TMP)
  - 자동 빌드 로직에도 포함
  - `_weapon.ShowOverlay` true일 때만 활성화
- [ ] `BombWeapon` 오버라이드:
  - `BarColor` → 쿨중 주황 / 준비 초록
  - `StateText` → 준비 시 **"[클릭]"** / 쿨중 "3.5s"
  - `BorderColor` → 준비 시 초록
  - `ShowOverlay` → 쿨중 true

#### Phase 3 — 기관총 (가장 큰 확장)

- [ ] `WeaponSlotUI`에 **AmmoRow 자식** 추가 (pip 40개 Horizontal Layout)
  - `_weapon.ShowAmmoRow` true일 때만 표시
  - pip 색 갱신: 현재 탄 이하는 active, 이상은 empty
- [ ] `GunWeapon` 오버라이드:
  - `BarFillAmount` = `AmmoCurrent / AmmoMax` (**쏠수록 감소** 방향)
  - 리로딩 중엔 `reloadElapsed / reloadDuration` (차오름)
  - `BarColor` → 리로딩 빨강 / 탄 있음 하늘
  - `StateText` → "32발" / "리로딩"
  - `BorderColor` → 탄≤8 빨강 경고 / 리로딩 빨강 / 평상시 기본
  - `ShowOverlay` → 리로딩 중 true, OverlayText "리로딩\n2.1s" (2줄)
  - `ShowAmmoRow` → 항상 true (해금된 경우)

#### Phase 4 — 레이저 (3단계 상태)

- [ ] `LaserWeapon` 오버라이드 — 상태 머신으로 분기:
  ```
  활성(빔 존재)  → BarColor 핑크,   BarFill 남은수명%,  StateText "5.2s"
  쿨중          → BarColor 진빨강, BarFill 쿨진행%,    StateText "3.0s", Overlay ON
  준비          → BarColor 초록,   BarFill 100%,       StateText "자동발사"
  ```
- [ ] 기존 Overlay 자식 재사용 (Phase 2에서 이미 구축됨)

### 4.6.5 리팩터링 타이밍

| 시점 | 작업 | 상태 |
|---|---|---|
| **Phase 1 내 (완료)** | `WeaponBase`에 슬롯 표현 프로퍼티 도입 + `WeaponSlotUI` 추상 렌더러화 | ✅ |
| **Phase 2 진행 중** | Overlay 자식 추가 + `BombWeapon` 오버라이드 | ⏳ |
| **Phase 3 진행 중** | AmmoRow 자식 추가 + `GunWeapon` 오버라이드 | ⏳ |
| **Phase 4 진행 중** | `LaserWeapon` 오버라이드만 (UI는 재사용) | ⏳ |

**저격총은 WeaponBase 기본 구현 그대로 동작** — 모든 프로퍼티가 기본값이라 별도 오버라이드 불필요.

---

## 5. Phase 2 — 폭탄 (수동 발사 + 투사체)

### 5-1. 로직 (`BombWeapon`)
- **클릭 시 발사** (New Input System, `Mouse.current.leftButton.wasPressedThisFrame`)
- `BombProjectile` 프리팹 생성 → 굴착기에서 에임 방향으로 이동
- 목표점 도달 or 수명 만료 시 **폭발**: `OverlapSphere(pos, radius)` 피해
- 데이터: `Cooldown=6s`, `Damage=3`, `Radius=1.8`, `Speed=5`

### 5-2. 좌측 슬롯 (폭탄)
- 상태 텍스트: **준비→"[클릭]"** / **쿨 중→"3.5s"**
- 쿨바 색: `ready(초록)` or `bomb(주황)`
- 쿨 오버레이: 쿨 중일 때 검은 반투명 + 남은 시간

### 5-3. 크로스헤어 (폭탄)
- **폭탄 쿨다운 호** 추가 (반경 `R+0.2`, 주황색)
- 준비 완료 시 십자선 하단에 "클릭→폭탄" 텍스트

### Phase 2 완료 기준
- [ ] 클릭 시 투사체가 굴착기→에임으로 날아가 폭발
- [ ] 폭발 반경 내 벌레 피해
- [ ] 슬롯/크로스헤어에 폭탄 쿨다운 반영
- [ ] "[클릭]" 프롬프트 표시

---

## 6. Phase 3 — 기관총 (탄창 + 리로딩)

### 6-1. 로직 (`GunWeapon`)
- **자동 연사**: `fireCD` 간격으로 발사
- **산포**: 에임 방향 ±0.06 rad 랜덤
- `GunBullet` 프리팹 — 투사체로 날아가 벌레와 충돌 (`OnTriggerEnter`)
- **탄창 소진 시 리로딩**: `reload` 시간 후 `maxAmmo` 복구
- 데이터: `fireCD=0.14s`, `Damage=0.5`, `Speed=9`, `maxAmmo=40`, `Reload=5s`

### 6-2. 좌측 슬롯 (기관총)
- **탄알 pip 행 추가**: 동적 생성 `ammo-pip` Image 40개 (또는 설정값)
- 상태 텍스트: **탄 남음→"32발"** / **리로딩→"리로딩"**
- 쿨바: 리로딩 중은 `reload(빨강)` 진행 %, 평상시는 `gun(하늘)` 탄수 비율
- 테두리: 탄 8개 이하 시 빨강 경고

### 6-3. 크로스헤어 (기관총)
- **탄창 호** 추가 (반경 `R+0.3`, 탄 비율 or 리로딩 진행)

### Phase 3 완료 기준
- [ ] 자동 연사 + 산포 탄환 발사
- [ ] 탄환이 벌레에 닿으면 피해
- [ ] 탄창 0 → 리로딩 → 재충전
- [ ] 슬롯 pip과 크로스헤어 호에 탄수 실시간 반영

---

## 7. Phase 4 — 레이저 (추적 빔 + 지속 피해)

### 7-1. 로직 (`LaserWeapon`)
- **쿨다운 완료 시 자동 스폰**: 마우스 위치에 `LaserBeam` GameObject 생성
- 빔 수명(`dur`) 동안 유지, 매 프레임 마우스 쪽으로 이동 (속도 느림)
- `dmgTick`(0.1초)마다 `OverlapSphere(beamPos, range)` 지속 피해
- 데이터: `Cooldown=5s`, `Duration=6s`, `Speed=1.725`, `Range=0.48`, `Damage=0.8`

### 7-2. 좌측 슬롯 (레이저)
- 3단계 상태:
  - **활성 중**: `laser-active(핑크)` 수명 %, 남은 시간
  - **쿨 중**: `laser(빨강)` 진행 %, 쿨 시간
  - **준비**: `ready(초록)` 100%, "자동발사"

### 7-3. 크로스헤어 (레이저)
- **레이저 쿨다운 호** 추가 (반경 `R+0.4`, 빨강)

### 7-4. 빔 시각 효과 (`LaserBeam`)
- 원형 판(Quad) + 빨강 머티리얼 + 알파 펄스
- Particle 잔상 (선택)

### Phase 4 완료 기준
- [ ] 쿨다운 후 빔 자동 스폰, 마우스 추적
- [ ] 빔 내 벌레 지속 피해
- [ ] 수명 끝나면 소멸, 쿨 재시작
- [ ] 3단계 상태가 슬롯/크로스헤어에 정확히 표시

---

## 8. Phase 5 — 통합 마감

### 8-1. 전체 검증
- [ ] 4종 무기 동시 동작, 서로 간섭 없음
- [ ] 각 무기 해금 플래그 토글 시 슬롯/배럴/크로스헤어 호가 전부 활성/비활성
- [ ] 밸런스 1차 튜닝 (쿨다운, 데미지, 범위)

### 8-2. 폴리싱 (선택)
- [ ] 발사 사운드 4종
- [ ] 히트 VFX (기존 `SimpleVFX` 활용)
- [ ] 탄창 리로딩 사운드

### 8-3. 커밋 단위
- Phase 0 완료 시 커밋 1회
- 각 Phase 1~4 완료 시 커밋 1회 (총 5 커밋)

---

## 9. 이후 단계 (이 문서 범위 외)

무기 4종 완성 후:
1. **해금 시스템**: 킬 카운트 → 무기 해금 오버레이
2. **성장 시스템**: 30킬마다 업그레이드 3선택 오버레이
3. **14종 성장 옵션** 데이터화 (프로토타입 `ALL_GROWTH`)

---

## 10. 데이터 기준값 요약

프로토타입 `BASE` (L166) → Unity 환산:

| 무기 | 필드 | 프로토타입 값 | Unity 시작값 | 단위 |
|---|---|---|---|---|
| 저격총 | cd | 24프레임 | 0.4 | 초 |
| | dmg | 1 | 1 | HP |
| | range | 24px | 0.4 | 월드유닛 |
| 폭탄 | cd | 360프레임 | 6 | 초 |
| | radius | 110px | 1.8 | 월드유닛 |
| | speed | 5px/frame | 5 | 유닛/초 |
| | dmg | 3 | 3 | HP |
| 기관총 | fireCD | 8.4프레임 | 0.14 | 초 |
| | maxAmmo | 40 | 40 | 발 |
| | reload | 300프레임 | 5 | 초 |
| | dmg | 0.5 | 0.5 | HP |
| | speed | 9px/frame | 9 | 유닛/초 |
| 레이저 | cd | 300프레임 | 5 | 초 |
| | dur | 360프레임 | 6 | 초 |
| | speed | 1.725 | 1.725 | 유닛/초 |
| | range | 28.8px | 0.48 | 월드유닛 |
| | dmg | 0.8 | 0.8 | HP (tick당) |

**경고**: 이 값은 **시작점**. Unity에서 플레이하며 반드시 재튜닝.

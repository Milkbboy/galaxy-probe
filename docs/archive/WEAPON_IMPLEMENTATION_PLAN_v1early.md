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

### 4-3. 좌측 슬롯 (저격총) ⏳ 예정
- 아이콘 + 이름 "저격총" + 레벨 "Lv.1"
- 상태 텍스트: **타겟 없음→"대기"** / **타겟+쿨0→"발사!"** / **쿨 중→"0.3s"**
- 쿨바 색: `ready(초록)` or `sniper(보라)`
- 슬롯 테두리: 준비+타겟 시 보라

### 4-4. 크로스헤어 기타 ⏳ 예정
- **범위 원**: 현재는 기존 크로스헤어 스프라이트 사용 중 (유지)
- **십자선 색**: 타겟 있을 때 보라로 변경 (개선 여지)

### 4-5. TurretController ⏳ 예정
- 저격 배럴만 표시 (다른 배럴은 비활성)

### Phase 1 진행 상황
- [x] 마우스 근처 SimpleBug 자동 처치
- [x] 에임 쿨다운 호가 12시부터 시계방향 회전
- [x] 쿨다운 진행이 에임 호에 실시간 반영
- [ ] 슬롯에 "대기/발사!/쿨" 상태가 실시간 반영
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

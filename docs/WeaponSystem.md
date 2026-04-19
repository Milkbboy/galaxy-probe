# 무기 시스템 (Weapon System)

> 최종 갱신: 2026-04-14
> Phase 3 구현 결과

> ## ⚠ 아카이브 알림 (2026-04-19)
>
> 이 문서는 **Phase 3 (v1) 무기 시스템** — Shotgun/BurstGun/LaserBeam/LockOn 4종을 다룹니다.
> **v2 전환 후** 무기 5종 (sniper/bomb/gun/laser/saw)로 재편되었고, 아키텍처도 변경되었습니다:
>
> - `WeaponSwitcher` / `AimController.EquipWeapon` / `_currentWeapon` — **제거됨**
> - 모든 무기 **self-driven** (각자 `Update()`에서 `TryFire` 호출, v2.html 동시 발동 패턴)
> - `AimController`는 에임 데이터만 공급 — 장착 개념 없음
>
> **현행 문서**: [WeaponUnlockUpgradeSystem.md](WeaponUnlockUpgradeSystem.md)
> — §7 회전톱날 (2026-04-19 완료) / §4 무기 강화 / §5 Title→Game 데이터 흐름 / §8 기관총 탄창
>
> 본 문서는 패턴 참조(WeaponBase·BulletPool·Fire 템플릿)용으로 유지됩니다.

## 1. 개요

플레이어(오퍼레이터)가 마우스로 조준하여 자동 발사하는 무기 시스템입니다.

### 핵심 개념

- **자동 발사**: 마우스 호버만으로 에임 범위에 적이 있으면 자동 사격
- **재장전 없음**: FireDelay(발사 간격)로만 속도 제어
- **업그레이드로 FireDelay 단축** (향후 구현)
- **한 번에 하나의 무기만 장착** (테스트는 숫자키로 교체)

### 무기 4종

| 무기 | 스타일 | 특징 |
|------|--------|------|
| **Shotgun** | 탕! ... 탕! | 산탄 6발 + 긴 딜레이 |
| **BurstGun** | 다다다다 | 고속 단발 연사 |
| **LaserBeam** | 지익~~~ | 지속 빔 + Heat 과열 시스템 |
| **LockOn** | 팡!팡!팡! | 타게팅 후 미사일 일제 발사 |

---

## 2. 아키텍처

### 구조

```
AimController (조준 + 크로스헤어 + 범위 체크)
    └─ _currentWeapon.TryFire(aimPos, hasTargetInRange)
           ↓
       WeaponBase (발사 타이밍 관리)
           ├─ ShotgunWeapon
           ├─ BurstGunWeapon
           ├─ LaserBeamWeapon
           └─ LockOnWeapon
                ↓
            BulletPool.Get() → Bullet
```

### 역할 분리

| 클래스 | 역할 |
|--------|------|
| **AimController** | 마우스 추적, 크로스헤어 UI, 범위 내 적 체크 |
| **WeaponBase** | FireDelay 관리, TryFire/Fire 템플릿 |
| **WeaponData** | 스탯 SO (데미지, 쿨다운, 투사체 설정) |
| **BulletPool** | 투사체 Object Pool 싱글톤 |
| **Bullet** | 투사체 이동 + 충돌 판정 + 데미지 |
| **WeaponSwitcher** | 디버그용 숫자키 무기 교체 |

---

## 3. 파일 구조

```
Assets/_Game/Scripts/Weapon/
├── WeaponData.cs          # SO 베이스 (abstract)
├── WeaponBase.cs          # 무기 베이스 (abstract)
├── BulletPool.cs          # 풀 싱글톤
├── PooledBullet.cs        # 풀 마커
├── Bullet.cs              # 투사체 컴포넌트
├── WeaponSwitcher.cs      # 디버그 교체 (1~4키)
│
├── Shotgun/
│   ├── ShotgunData.cs     # PelletCount, SpreadAngle
│   └── ShotgunWeapon.cs
│
├── BurstGun/
│   ├── BurstGunData.cs    # SpreadAngle
│   └── BurstGunWeapon.cs
│
├── Laser/
│   ├── LaserBeamData.cs   # Heat 시스템 필드
│   └── LaserBeamWeapon.cs # Raycast + LineRenderer
│
└── LockOn/
    ├── LockOnData.cs      # MaxTargets, LockRadius
    └── LockOnWeapon.cs    # 일괄 미사일 발사
```

---

## 4. WeaponBase 핵심 로직

```csharp
public abstract class WeaponBase : MonoBehaviour
{
    protected WeaponData _baseData;
    protected float _nextFireTime;

    public bool CanFire => Time.time >= _nextFireTime;

    public virtual void TryFire(Vector3 targetPoint, bool hasTargetInRange)
    {
        if (!CanFire) return;
        if (!ShouldFire(targetPoint, hasTargetInRange)) return;

        Fire(targetPoint);
        _nextFireTime = Time.time + _baseData.FireDelay;
    }

    protected virtual bool ShouldFire(Vector3 target, bool inRange) => inRange;
    protected abstract void Fire(Vector3 targetPoint);  // 파생 구현
}
```

### AimController 연동

```csharp
void Update()
{
    UpdateAimPosition();          // 마우스 월드 좌표
    CheckTargetsInRange();        // Bug 레이어 OverlapSphere
    _currentWeapon?.TryFire(_aimPosition, _hasBugInRange);
}
```

---

## 5. 각 무기 상세

### 5.1 ShotgunWeapon

**핵심 동작**: 한 번 발사 시 N발이 부채꼴로 동시에 나감

```csharp
for (int i = 0; i < pelletCount; i++)
{
    float angle = Lerp(-halfSpread, halfSpread, i / (count-1));
    Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * baseDir;
    SpawnPellet(origin, dir, pelletDamage);
}
```

| 필드 | 권장값 | 설명 |
|------|--------|------|
| FireDelay | 1.0s | 긴 딜레이 (탕! 한 방의 맛) |
| Damage | 40 | 전체 데미지 |
| PelletCount | 6 | 한 번에 나가는 산탄 수 |
| SpreadAngle | 30° | 좌우 총 퍼짐 각도 |
| PelletDamageMultiplier | 0.5 | 펠릿당 실제 데미지 = Damage × 이 값 |

**실제 펠릿당 데미지**: `Damage(40) × 0.5 = 20`
**전체 최대 데미지**: `20 × 6 = 120` (모두 명중 시)

---

### 5.2 BurstGunWeapon

**핵심 동작**: 짧은 딜레이로 단발을 빠르게 난사

```csharp
float jitter = Random.Range(-spread, spread);
dir = Quaternion.AngleAxis(jitter, Vector3.up) * dir;
SpawnBullet(origin, dir, damage);
```

| 필드 | 권장값 | 설명 |
|------|--------|------|
| FireDelay | 0.08s | 매우 짧은 딜레이 |
| Damage | 8 | 낮은 단발 데미지 |
| SpreadAngle | 3° | 미세한 탄퍼짐 |

**DPS**: `8 / 0.08 = 100`

---

### 5.3 LaserBeamWeapon

**특수 동작**: FireDelay = 0, 매 프레임 빔 유지. Heat 시스템으로 과열 관리

```csharp
if (_overheated) { CoolDown(); return; }  // 강제 쿨다운

if (hasTarget)
{
    FireBeam(targetPoint);                 // SphereCast + LineRenderer
    _heat += HeatPerSecond * deltaTime;
    if (_heat >= MaxHeat) _overheated = true;
}
else
{
    CoolDown();  // 쉬는 동안 Heat 감소
}
```

| 필드 | 권장값 | 설명 |
|------|--------|------|
| FireDelay | 0 | 연속 빔이라 발사 간격 없음 |
| Damage | 3 | **Tick당** 데미지 |
| DamageTickInterval | 0.05s | 데미지 주기 (= 20 tick/sec) |
| BeamRadius | 0.2 | 빔 두께 (SphereCast 반경) |
| MaxHeat | 100 | 과열 임계값 |
| HeatPerSecond | 30 | 발사 중 초당 Heat 증가 |
| CoolPerSecond | 20 | 쉴 때 초당 Heat 감소 |
| OverheatLockTime | 1.5s | 과열 후 강제 쿨다운 |

**초당 DPS**: `3 × 20 = 60`
**최대 지속 시간**: `100 / 30 ≈ 3.3초`
**과열 후 완전 복귀**: `1.5초 + (100/20) = 6.5초`

**필수 컴포넌트**: `LineRenderer` (자동 추가)

---

### 5.4 LockOnWeapon

**특수 동작**: 에임 범위 내 적을 수집 → FireDelay마다 일괄 미사일 발사

```csharp
// ShouldFire 오버라이드로 범위 내 적 수집
protected override bool ShouldFire(targetPoint, inRange)
{
    CollectTargets(targetPoint);
    return _currentTargets.Count > 0;
}

// 수집된 타겟마다 미사일 발사
IEnumerator LaunchMissilesCoroutine(targets)
{
    foreach (var t in targets)
    {
        LaunchMissile(t);
        yield return new WaitForSeconds(MissileInterval);
    }
}
```

| 필드 | 권장값 | 설명 |
|------|--------|------|
| FireDelay | 2.0s | 긴 재장전 느낌 |
| Damage | 20 | 미사일 1발당 데미지 |
| MaxTargets | 30 | 한 번에 타게팅할 최대 수 |
| LockRadius | 4 | 락온 감지 반경 |
| MissileInterval | 0.05s | 미사일 간 발사 간격 (시각적 텀) |
| LaunchHeight | 0.5 | 미사일 시작 Y 오프셋 |

---

## 6. BulletPool 시스템

### 동작

```
Get(prefab) → 해당 프리펩 풀에서 꺼냄
              큐 비었으면 Instantiate (PooledBullet 자동 부착)

Return(obj) → 큐로 복귀, SetActive(false)
```

### Bullet.Update()

```csharp
if (Time.time - _spawnTime >= _lifetime) ReturnOrDestroy();

if (Physics.Raycast(position, direction, moveDistance, hitLayer))
{
    damageable?.TakeDamage(damage);
    SpawnHitVfx();
    ReturnOrDestroy();
}
else
{
    position += direction * moveDistance;
}
```

---

## 7. Unity 씬 설정 체크리스트

### 7.1 BulletPool 배치

1. `Create Empty` → 이름: **BulletPool**
2. `Add Component → BulletPool`
3. 설정:

| 필드 | 값 |
|------|-----|
| Pool Root | 비움 (자기 자신 사용) |
| Default Initial Size | 30 |
| Max Active Total | 500 |

---

### 7.2 Bullet 프리펩 만들기

**1. 프리펩 생성**
```
Assets/_Game/Prefabs/ 에서:
Create → 3D Object → Sphere
이름: Bullet_Default
```

**2. 컴포넌트 설정**

| 컴포넌트 | 설정 |
|---------|------|
| Transform Scale | (0.15, 0.15, 0.15) |
| SphereCollider | Is Trigger: true (선택) |
| **Bullet** 스크립트 | Hit VFX Prefab, Hit Layer: Bug |
| Material | 눈에 띄는 색 (노랑, 빨강 등) |

**3. 레이어 설정**
```
Layer: "BugProjectile" 또는 기본
Physics Matrix에서 Bug와 충돌하도록 확인
```

**4. 프리펩 저장 후 Hierarchy 원본 삭제**

---

### 7.3 무기 Data 에셋 생성

**폴더:** `Assets/_Game/Data/Weapons/`

#### Weapon_Shotgun

`Create → Drill-Corp → Weapons → Shotgun`

| 필드 | 값 |
|------|-----|
| Display Name | 샷건 |
| Fire Delay | 1.0 |
| Damage | 40 |
| Range | 8 |
| Bullet Prefab | Bullet_Default |
| Bullet Speed | 30 |
| Bullet Lifetime | 1 |
| Pellet Count | 6 |
| Spread Angle | 30 |
| Pellet Damage Multiplier | 0.5 |

#### Weapon_BurstGun

`Create → Drill-Corp → Weapons → Burst Gun`

| 필드 | 값 |
|------|-----|
| Display Name | 버스트건 |
| Fire Delay | 0.08 |
| Damage | 8 |
| Range | 10 |
| Bullet Prefab | Bullet_Default |
| Bullet Speed | 40 |
| Bullet Lifetime | 1 |
| Spread Angle | 3 |

#### Weapon_Laser

`Create → Drill-Corp → Weapons → Laser Beam`

| 필드 | 값 |
|------|-----|
| Display Name | 레이저 |
| Fire Delay | 0 |
| Damage | 3 (Tick당) |
| Range | 12 |
| Bullet Prefab | (비움) |
| Damage Tick Interval | 0.05 |
| Beam Radius | 0.2 |
| Max Heat | 100 |
| Heat Per Second | 30 |
| Cool Per Second | 20 |
| Overheat Lock Time | 1.5 |

#### Weapon_LockOn

`Create → Drill-Corp → Weapons → Lock On`

| 필드 | 값 |
|------|-----|
| Display Name | 락온 |
| Fire Delay | 2.0 |
| Damage | 20 |
| Range | 10 |
| Bullet Prefab | Bullet_Default (또는 별도 미사일) |
| Bullet Speed | 25 |
| Bullet Lifetime | 3 |
| Max Targets | 30 |
| Lock Radius | 4 |
| Missile Interval | 0.05 |
| Launch Height | 0.5 |

---

### 7.4 씬에 무기 GameObject 배치

**부모 GameObject**
```
Create Empty → 이름: Weapons
```

**각 무기 자식 생성**

| 이름 | 컴포넌트 | Data 할당 |
|------|---------|----------|
| Shotgun | ShotgunWeapon | Weapon_Shotgun |
| BurstGun | BurstGunWeapon | Weapon_BurstGun |
| LaserBeam | LaserBeamWeapon (LineRenderer 자동) | Weapon_Laser |
| LockOn | LockOnWeapon | Weapon_LockOn |

**공통 설정**
- Muzzle: 자기 자신 (또는 별도 발사 위치)
- 시작 시 비활성화 가능 (AimController Initial Weapon으로 하나만 활성)

**LaserBeam 특별 설정**

LineRenderer 컴포넌트:
- Material: 빛나는 머티리얼 (Unlit/Color 빨강 등)
- Width: Start 0.1, End 0.1
- Use World Space: true

---

### 7.5 AimController 설정

기존 Aim/Crosshair GameObject 선택

| 필드 | 값 |
|------|-----|
| Aim Radius | 0.5 (또는 Auto) |
| Auto Calculate Radius | true |
| Bug Layer | Bug |
| **Initial Weapon** | Shotgun 드래그 |
| Crosshair Renderer | (기존) |
| Normal Color | 흰색 |
| Ready Color | 빨강 |

---

### 7.6 WeaponSwitcher 설정

**1. GameObject 생성**
```
Create Empty → 이름: WeaponSwitcher
```

**2. 컴포넌트 + 필드 설정**

| 필드 | 값 |
|------|-----|
| Aim Controller | AimController 드래그 (또는 자동 탐색) |
| Slot 1 | Shotgun 드래그 |
| Slot 2 | BurstGun 드래그 |
| Slot 3 | LaserBeam 드래그 |
| Slot 4 | LockOn 드래그 |
| Show On Screen Label | true |

---

## 8. 최종 Hierarchy 예시

```
Scene
├── Main Camera (DynamicCamera)
├── Machine
├── Aim (AimController)
│   └── Crosshair (SpriteRenderer)
│
├── --- Managers ---
│   ├── GameManager
│   ├── WaveManager
│   ├── BugPool
│   ├── FormationSpawner
│   ├── BulletPool
│   └── WeaponSwitcher
│
├── --- Weapons ---
│   ├── Shotgun (ShotgunWeapon)
│   ├── BurstGun (BurstGunWeapon)
│   ├── LaserBeam (LaserBeamWeapon + LineRenderer)
│   └── LockOn (LockOnWeapon)
│
└── ... (기타)
```

---

## 9. 플레이 테스트

### 기본 확인
1. 재생
2. 마우스 이동 → 크로스헤어 따라오는지
3. **숫자키 1** → 샷건 활성화 (화면 하단에 무기명 표시)
4. Bug 근처로 마우스 이동 → 크로스헤어 빨강 + 발사
5. 숫자키 2/3/4로 다른 무기 교체 확인

### 무기별 체감

| 키 | 무기 | 예상 체감 |
|----|------|----------|
| **1** | Shotgun | 탕! 한 방에 6발 퍼짐, 1초 쿨다운 |
| **2** | BurstGun | 다다다다 고속 연사 |
| **3** | Laser | 빔 지속, 3초 후 과열 → 1.5초 쿨다운 |
| **4** | LockOn | 2초마다 범위 내 적 일제 공격 |

---

## 10. 트러블슈팅

| 증상 | 원인 / 해결 |
|------|-----------|
| 총알 안 나감 | BulletPool 씬에 있는지, Bullet 프리펩 할당 확인 |
| 총알 즉시 사라짐 | Bullet Lifetime 너무 짧음 (1~3초 권장) |
| 데미지 안 들어감 | Bullet Hit Layer가 Bug 레이어 포함인지 |
| 레이저 안 보임 | LineRenderer Material, Width 설정 |
| 레이저 핑크색 | Material 누락, Unlit/Color 머티리얼 생성 필요 |
| 무기 안 바뀜 | WeaponSwitcher Slot 필드, AimController 연결 |
| 과열 안 풀림 | Overheat Lock Time 확인, Heat 0 될 때까지 기다림 |
| LockOn 발사 안 함 | Lock Radius 내에 적 있는지, Target Layer 확인 |

---

## 11. 업그레이드 연결 (향후)

FireDelay가 업그레이드 핵심 스탯:

```
Shotgun FireDelay:
  Lv 0: 1.0s
  Lv 1: 0.9s
  Lv 2: 0.8s
  ...
  최종: 0.5s
```

`WeaponData`의 FireDelay를 런타임에 수정하거나 별도 UpgradeApplier 시스템으로 연결 예정.

---

## 12. 향후 확장

- [ ] **머신 자동포**: 마우스 방향 자동 발사 (오퍼레이터와 독립)
- [ ] **머신 무기 7종**: BlackHole, ChainLightning, ClusterBomb, SonicWave 등
- [ ] **오퍼레이터 × 머신 조합 효과**: 스킬 트리
- [ ] **업그레이드 시스템**: FireDelay, Damage, Range 개별 강화
- [ ] **탄약 시스템**: 기획서엔 없지만 필요시
- [ ] **Hit VFX 풀링**: 현재 Instantiate/Destroy 사용

---

## 13. 참고 문서

- 전체 기획: `DRILL-CORP-PLAN.md`
- Bug Behavior: `BugBehaviorSystemAnalysis.md`
- Formation 시스템: `FormationSystem.md`
- 카메라 시스템: `CameraSystem.md`

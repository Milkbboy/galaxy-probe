# VFX 3D 전환 계획

> 작성일: 2026-04-19 / 구현 적용: 2026-04-19
> 상태: **✅ 1차 구현 완료** — MachineGun / Bomb / Shotgun / Laser(스코치) 적용. LockOn / LandingMarker / LaserBeam 본체는 미착수
> 관련 문서: [WeaponSystem.md](WeaponSystem.md)

## 🚀 빠른 시작 (자동화 스크립트)

전체 Phase를 메뉴 한 번으로 실행할 수 있는 Editor 스크립트 준비됨.

**메뉴 위치**: `Tools → Drill-Corp → VFX 3D Migration → ▶ 전체 실행 (Run All Phases)`

### 자동 처리되는 범위
- ✅ `AutoDestroyPS.cs` (파티클 종료 시 자동 소멸) — 이미 생성됨
- ✅ `Quaternion.identity` → `prefab.transform.rotation` 3곳 교정 — 이미 적용됨
- ✅ PA 기반 4종 One-shot Variant 생성 (`VFX_3D/` 폴더)
  - `Bomb_Explosion_3D` / `Bomb_HitVfx_3D` / `Shotgun_Muzzle_3D` / `MG_HitVfx_3D`
- ✅ 2종 Projectile Visual Variant 생성
  - `MG_BulletVisual_3D` / `Bomb_ProjectileVisual_3D`
- ✅ `MachineGunBullet_3D.prefab` / `BombProjectile_3D.prefab` 재구성 (VisualSocket 패턴)
- ✅ `Weapon_MachineGun` / `Weapon_Bomb` / `Weapon_Shotgun` SO 필드 갱신

### 수동 작업 필요
- ⚠️ **LockOnMarker** — `LockOnMarker.cs`가 `SpriteRenderer` 직접 참조. 먼저 `MeshRenderer + MaterialPropertyBlock` 으로 리팩터 후 Variant 교체
- ⚠️ **BombLandingMarker** — 동일한 구조. SR 의존 스크립트 개조 선행 필요
- ⚠️ **LaserBeam** — 프로토 `_.html` 포팅이라 하이브리드 방식(§Phase 4) 수동 적용

### 롤백
원본 2D 프리펩은 삭제되지 않음. `Weapon_*.asset` SO의 VFX 필드를 원본 프리펩(예: `MachineGunBullet.prefab`)으로 되돌리면 즉시 복구.

### 세부 메뉴 (문제 디버깅용)
스크립트는 전체 실행 외에 단계별 메뉴도 제공:
1. `2. One-shot Variants만 생성`
2. `3. Projectile Visual Variants만 생성`
3. `4. Projectile 프리펩 재구성만`
4. `5. Data SO 갱신만`
5. `6. Laser 스코치만 생성 + SO 연결`
6. `7. Projectile _3D 프리펩 재생성 (삭제 후 재빌드)` — 비주얼 스케일 튜닝 후 재빌드용

---

## 📌 구현 결과 (2026-04-19)

### 생성된 3D 에셋 (총 9개)

#### Variants — `Assets/_Game/Prefabs/Weapons/VFX_3D/`
| 파일명 | PA 소스 | 용도 |
|---|---|---|
| `MG_BulletVisual_3D.prefab` | `Missiles/Sci-Fi/Bullet/BulletBlue.prefab` | MachineGunBullet_3D VisualSocket 자식 |
| `MG_HitVfx_3D.prefab` | `Explosions/Sci-Fi/Bullet/BulletExplosionBlue.prefab` | 머신건 탄환 명중 |
| `Bomb_ProjectileVisual_3D.prefab` | `Missiles/Sci-Fi/Rocket/RocketMissileRed.prefab` | BombProjectile_3D VisualSocket 자식 |
| `Bomb_Explosion_3D.prefab` | `Explosions/Sci-Fi/Rocket/RocketExplosionRed.prefab` | 폭탄 폭발 |
| `Bomb_HitVfx_3D.prefab` | `Explosions/Mini/MiniExploFire.prefab` | 폭탄 개별 피격 |
| `Shotgun_Muzzle_3D.prefab` | `Muzzleflash/Standard/ShotgunMuzzleStandard.prefab` | 샷건 머즐 |
| `Laser_Scorch_3D.prefab` | `Combat/Aura/DamageAura/DamageAuraFire.prefab` | 레이저 지면 그을림 (빔 추적) |

#### 재구성 프리펩 — `Assets/_Game/Prefabs/Weapons/`
| 파일명 | 구조 |
|---|---|
| `MachineGunBullet_3D.prefab` | 루트(MachineGunBullet.cs + Collider) + VisualSocket(scale 2.5, rot -90) + MG_BulletVisual_3D 중첩 |
| `BombProjectile_3D.prefab` | 루트(BombProjectile.cs + Collider) + VisualSocket(scale 1.2, rot -90) + Bomb_ProjectileVisual_3D 중첩 |

### 사용처 매핑 (Data SO 필드별)

**`Weapon_MachineGun.asset`**
| SO 필드 | 참조 프리펩 | 발동 시점 |
|---|---|---|
| `_bulletPrefab` | `MachineGunBullet_3D.prefab` | 자동 연사마다 (FireDelay 0.14s) |
| `_hitVfxPrefab` | `MG_HitVfx_3D.prefab` | 탄환 명중 시 (`MachineGunBullet.cs:91`) |

**`Weapon_Bomb.asset`**
| SO 필드 | 참조 프리펩 | 발동 시점 |
|---|---|---|
| `_projectilePrefab` | `BombProjectile_3D.prefab` | 폭탄 투척 (`BombWeapon.cs:102`) |
| `_explosionVfxPrefab` | `Bomb_Explosion_3D.prefab` | 착탄 폭발 (`BombProjectile.cs:133`) |
| `_hitVfxPrefab` | `Bomb_HitVfx_3D.prefab` | 폭발 범위 내 개별 피격 (`BombProjectile.cs:125`) |

**`Weapon_Shotgun.asset`**
| SO 필드 | 참조 프리펩 | 발동 시점 |
|---|---|---|
| `_muzzleVfxPrefab` | `Shotgun_Muzzle_3D.prefab` | 샷건 발사 (`ShotgunWeapon.cs:39`) |

**`Weapon_LaserBeam.asset`**
| SO 필드 | 참조 프리펩 | 발동 시점 |
|---|---|---|
| `_scorchPrefab` | `Laser_Scorch_3D.prefab` | 레이저 빔 스폰과 동시 (빔 Transform 추적, 6s+2s fade) |

### 신규 커스텀 컴포넌트

| 컴포넌트 | 부착 대상 | 역할 |
|---|---|---|
| `AutoDestroyPS` (`Scripts/VFX/`) | One-shot Variants (MG_HitVfx / Bomb_Explosion / Bomb_HitVfx / Shotgun_Muzzle) | `OnParticleSystemStopped` 콜백으로 자동 Destroy — `stopAction = Callback` 자동 세팅 |
| `LaserScorchDecay` (`Scripts/VFX/`) | Laser_Scorch_3D | (1) 빔 Transform 추적(`LateUpdate`에서 position 복사) (2) 6s 후 방출 정지(자연 소멸 유지) (3) 8s 후 GameObject Destroy |

### 코드 수정 (Phase 1 — 탑뷰 회전 보존 룰 준수)

| 파일 | 변경 |
|---|---|
| `WeaponBase.cs:172` | `Quaternion.identity` → `_baseData.HitVfxPrefab.transform.rotation` |
| `LockOnWeapon.cs:185` | `Quaternion.identity` → `_markerPrefab.transform.rotation` |
| `ShotgunWeapon.cs:39` | `Quaternion.identity` → `_data.MuzzleVfxPrefab.transform.rotation` |
| `LaserWeapon.cs` | `SpawnScorch(spawnPos, obj.transform)` 호출 추가 + `SpawnScorch` 메서드 신설 |
| `LaserWeaponData.cs` | `_scorchPrefab` / `_scorchScaleMultiplier` / `_scorchStopAfter` / `_scorchTotalLifetime` 4개 필드 추가 |

### 미전환 항목 (2D 유지)

| 항목 | 사유 |
|---|---|
| **LaserBeam 본체** | 프로토 `_.html` 포팅 복잡도 — 4겹 LineRenderer + LifeArc 유지, 스코치만 추가 (하이브리드-라이트 방식) |
| **LockOnMarker** | `LockOnMarker.cs`가 `SpriteRenderer` 직접 의존 — `MeshRenderer + MaterialPropertyBlock` 리팩터 선행 필요 |
| **BombLandingMarker** | 동일 (SR 의존 스크립트) |
| **Sniper / BurstGun** | 이펙트 스폰 자체가 없거나 공용 HitVfx만 사용 — 별도 작업 불필요 |

### 발동 흐름 다이어그램

```
[발사 이벤트]
 ├─ MachineGun 연사 → MachineGunBullet_3D (자체 이동+히트)
 │                     └─ 명중 → MG_HitVfx_3D (자동 소멸)
 ├─ Shotgun 발사   → Shotgun_Muzzle_3D (자동 소멸)
 ├─ Bomb 투척      → BombProjectile_3D (포물선 비행)
 │                     ├─ 착탄 → Bomb_Explosion_3D (자동 소멸)
 │                     └─ 개별 명중 → Bomb_HitVfx_3D (자동 소멸)
 ├─ Laser 스폰     → 기존 LaserBeam (2D 링 유지)
 │                     + Laser_Scorch_3D (빔 추적, 6s+2s fade)
 └─ LockOn 표적화  → 기존 LockOnMarker (2D 유지)
```

### 원본 2D 에셋 상태

삭제되지 않음 — SO 참조만 바뀐 상태. 롤백 시 SO 필드를 원본으로 되돌리면 즉시 복구.

---



## 0. 요약

현재 무기 이펙트(폭발·피격·머즐·탄환·레이저·락온)가 전부 **2D `SpriteRenderer` 기반**으로 되어 있음. `Assets/Polygon Arsenal/` (1,400 프리펩, 전부 `ParticleSystem` + Mesh 파티클) 을 활용해 **3D 이펙트로 교체**.

**핵심 설계**:
- 로직과 비주얼을 분리 → **비주얼 소켓(자식 오브젝트)** 패턴
- 좌표계 교정(탑뷰 -90° X 회전)은 소켓에서 한 번만 담당
- 데이터 SO 레퍼런스만 Variant로 갈면 무기 로직 코드 변경 최소화

**예상 작업량**: 14~20시간 / 6단계 Phase

---

## 1. 배경 & 결정

### 1.1 현재 상태 (2D)

| 프리펩 | 렌더러 구성 |
|---|---|
| `BombExplosionFx` | `SpriteRenderer` 1개 |
| `BombLandingMarker` | `SpriteRenderer` + 커스텀 알파 펄스 스크립트 |
| `BombProjectile` | `SpriteRenderer` + `TrailRenderer` |
| `MachineGunBullet` | `SpriteRenderer` + `TrailRenderer` |
| `LockOnMarker` | `SpriteRenderer` + 커스텀 색/알파 스크립트 |
| `LaserBeamField` | `SpriteRenderer` 1개 |
| `LaserBeam` | 4×`LineRenderer` + 2×`SpriteRenderer` + 1×`ParticleSystem` (프로토 포팅) |

### 1.2 동적 생성 지점 (`Instantiate` 호출)

| 위치 | 생성 대상 |
|---|---|
| `WeaponBase.cs:172` | `HitVfxPrefab` (모든 무기 공통 피격) |
| `MachineGunWeapon.cs:148` | `BulletPrefab` |
| `MachineGunBullet.cs:91` | `HitVfxPrefab` |
| `ShotgunWeapon.cs:39` | `MuzzleVfxPrefab` |
| `BombWeapon.cs:102` | `ProjectilePrefab` |
| `BombProjectile.cs:52` | `LandingMarkerPrefab` |
| `BombProjectile.cs:125` | `HitVfxPrefab` |
| `BombProjectile.cs:133` | `ExplosionVfxPrefab` |
| `LaserWeapon.cs:105` | `BeamPrefab` |
| `LaserBeamWeapon.cs:107` | `_fieldPrefab` |
| `LockOnWeapon.cs:185` | `_markerPrefab` |

### 1.3 Polygon Arsenal 팩 구조

- **총 1,400 프리펩** — 전부 `ParticleSystem` 기반
- `m_RenderMode: 4` → Mesh 파티클 (3D 메시 레퍼런스)
- `m_RenderMode: 0/1` → Billboard/Stretched
- 카테고리: `Combat/` (Explosions, Beams, Missiles, Muzzleflash, Surface Impact …)
- 내장 스크립트 5종: `PolygonBeamStatic`, `PolygonRotation`, `PolygonLightFade`, `PolygonLightFlicker`, `PolygonSoundSpawn`

### 1.4 채택 방침

| 항목 | 결정 |
|---|---|
| 기반 리소스 | Polygon Arsenal 프리펩 Variant |
| 좌표 교정 | 소켓(자식 GO) 루트에서 `(-90, 0, 0)` 회전 — 원본 PA 프리펩 무수정 |
| 로직 / 비주얼 | **소켓 패턴으로 완전 분리** (로직은 루트, 비주얼은 자식 소켓) |
| 데이터 SO | VFX 레퍼런스 필드만 Variant로 교체 (코드 무변경) |
| 레거시 2D 자산 | 전 작업 완료 후 정리 (즉시 삭제 X) |

---

## 2. 아키텍처 — 소켓 패턴

### 2.1 패턴 A: 지속형 오브젝트 (총알, 폭탄)

```
MachineGunBullet (루트)
├─ MachineGunBullet.cs      ← 로직 (이동·수명·히트 판정)
├─ SphereCollider            ← 판정
└─ VisualSocket (회전 -90,0,0)   ← 탑뷰 교정
    └─ BulletBlue_Variant (PA)    ← 갈아끼는 부분
        └─ ParticleSystem (트레일 내장)
```

**장점**:
- 로직 코드 무변경 (이동·수명·히트 그대로)
- 비주얼만 Variant 갈아끼우면 룩 완전 교체
- 소켓 회전 때문에 PA 프리펩의 기본 방향(+Y)이 탑뷰 +Z로 자동 맵핑

### 2.2 패턴 B: 일회성 이펙트 (폭발, 피격, 머즐)

```
BombExplosion_Variant (루트, 회전 -90,0,0)
├─ AutoDestroyPS             ← 파티클 종료 시 Destroy
└─ ParticleSystem(들)         ← PA 원본
```

- 자기 자신이 통째로 소멸 → 소켓 불필요
- Data SO의 VFX 레퍼런스만 Variant로 교체하면 끝
- Instantiate 시 반드시 `prefab.transform.rotation` 사용 (기존 `Quaternion.identity` 3곳 교정 대상)

### 2.3 패턴 C: 커스텀 로직이 붙은 마커 (LandingMarker, LockOnMarker)

알파 펄스·색 제어 로직이 컴포넌트에 박혀있어 패턴 A 변형 적용:

```
BombLandingMarker (루트)
├─ BombLandingMarker.cs       ← 알파 펄스 로직 유지 (MeshRenderer 대상으로 수정)
└─ VisualSocket (회전 -90,0,0)
    └─ CircleQuad (MeshRenderer + 기존 BombLandingMarkerCircle.png 재활용)
```

- 기존 `GetComponent<SpriteRenderer>()` → `GetComponentInChildren<MeshRenderer>()` + `MaterialPropertyBlock`
- PNG는 기존 것 재활용 → 룩 변경 최소화 + 구현 간단

### 2.4 공통 유틸리티

**신규 스크립트**: `Assets/_Game/Scripts/VFX/AutoDestroyPS.cs`

```csharp
[RequireComponent(typeof(ParticleSystem))]
public class AutoDestroyPS : MonoBehaviour
{
    void OnParticleSystemStopped() => Destroy(gameObject);
}
```

모든 패턴 B Variant에 부착.

---

## 3. 매핑 테이블 (교체 계획)

| 기존 프리펩 | PA Variant (예정) | 무기 | 패턴 |
|---|---|---|---|
| `BombExplosionFx.prefab` | `RocketExplosionRed_TopView` | Bomb | B |
| `BombProjectile.prefab` (비주얼) | `BulletRed_TopView` (Visual Socket 하위) | Bomb | A |
| `BombLandingMarker.prefab` | MeshRenderer + 기존 PNG 재활용 | Bomb | C |
| `MachineGunBullet.prefab` (비주얼) | `BulletBlue_TopView` (Visual Socket 하위) | MachineGun | A |
| MachineGun `HitVfxPrefab` | `SurfaceImpactMetal_TopView` | MachineGun | B |
| Shotgun `MuzzleVfxPrefab` | `ShotgunMuzzleStandard_TopView` | Shotgun | B |
| `LockOnMarker.prefab` | MeshRenderer + 기존 PNG 재활용 | LockOn | C |
| `LaserBeamField.prefab` (비주얼) | PA Aura 계열 Variant (Visual Socket) | Laser | A |
| `LaserBeam.prefab` (내부 구조) | 하이브리드 — §5 Phase 4 참조 | Laser | 특수 |
| 공통 `HitVfxPrefab` (`WeaponBase`) | `SurfaceImpactMetal_TopView` (공용) | 공통 | B |

**컬러 팔레트** (기존 무기 테마 색 유지):
- MachineGun: Blue (`#4fc3f7`)
- Shotgun: Yellow
- Bomb: Red
- Laser: Purple
- LockOn: Pink

---

## 4. Phase별 실행 계획

### Phase 0 — 선행 결정 (30분)

- [ ] 무기별 컬러 팔레트 확정 (§3 기본안 승인)
- [ ] 스케일 기준 확정: 버그 1마리 ≈ 1 유닛 → PA 프리펩은 0.5~1.5 배율로 조정
- [ ] 빌드 포함 범위: 실사용 ~30개 Variant만. `Polygon Arsenal/Demo/`, `Upgrades/` 빌드 제외 (Variant 참조 안 하면 자동 제외됨)

### Phase 1 — 공통 인프라 (1~2시간)

- [ ] `Assets/_Game/Prefabs/Weapons/VFX_3D/` 폴더 생성
- [ ] `Assets/_Game/Scripts/VFX/AutoDestroyPS.cs` 작성
- [ ] **코드 3곳 좌표 교정** (feedback 메모 `feedback_topdown_instantiate.md` 룰 위반 중):
  - `WeaponBase.cs:172` — `Quaternion.identity` → `HitVfxPrefab.transform.rotation`
  - `LockOnWeapon.cs:185` — `Quaternion.identity` → `_markerPrefab.transform.rotation`
  - `ShotgunWeapon.cs:39` — `Quaternion.identity` → `_data.MuzzleVfxPrefab.transform.rotation`
- [ ] 컴파일 확인 + 기존 씬 정상 동작 확인

### Phase 2 — MachineGun 파일럿 (3~4시간)

목적: 교체 파이프라인 검증. 전체 방식이 잘 도는지 한 무기로 확인 후 나머지 확장.

#### 2-1 Variant 생성 (`VFX_3D/`)
- [ ] `BulletBlue_TopView.prefab` (Muzzle 포함된 Rocket/Bullet 계열 중 선택)
- [ ] `BulletMuzzleBlue_TopView.prefab` (선택 — 머즐 분리하거나 탄환에 내장)
- [ ] `BulletExplosionBlue_TopView.prefab` (HitVfx)
- 각 Variant 루트: 회전 `(-90,0,0)` + `AutoDestroyPS` 부착

#### 2-2 프리펩 재구성
- [ ] `MachineGunBullet.prefab` 변형:
  - `SpriteRenderer`, `TrailRenderer` 컴포넌트 **제거**
  - 자식 `VisualSocket` 추가 → 그 아래 `BulletBlue_TopView` 배치
  - `MachineGunBullet.cs` 내부 SR/Trail 참조 제거 (있으면)

#### 2-3 Data SO 갱신
- [ ] `MachineGunData` SO의 `BulletPrefab`, `HitVfxPrefab` 필드 Variant로 교체

#### 2-4 검증
- [ ] Game 씬 플레이 → 발사·피격·궤적 육안 확인
- [ ] 다중 동시 발사 시 프레임 체크
- [ ] 머즐 플래시 타이밍 확인

### Phase 3 — Bomb + Shotgun + LockOn (4~5시간)

MachineGun 파일럿이 성공하면 동일 파이프라인을 나머지에 적용.

#### 3-1 Bomb (패턴 A + B + C)
- [ ] `BombProjectile.prefab`: `VisualSocket` 구조로 재구성, PA Rocket 계열 Variant 바인딩
- [ ] `BombExplosionFx.prefab`: 완전 교체 → `RocketExplosionRed_TopView` Variant
  - 기존 `BombExplosionFx.cs` **삭제** (SR 의존이라 무용)
- [ ] `BombLandingMarker.prefab`: 패턴 C 적용
  - `BombLandingMarker.cs`의 `SpriteRenderer` 참조 → `MaterialPropertyBlock` 방식으로 수정
  - 자식 Quad + 기존 `BombLandingMarkerCircle.png` 재활용
- [ ] `BombData` SO의 3개 필드 모두 교체

#### 3-2 Shotgun
- [ ] `ShotgunMuzzleStandard_TopView.prefab` Variant 생성
- [ ] `ShotgunData.MuzzleVfxPrefab` 교체

#### 3-3 LockOn
- [ ] `LockOnMarker.prefab`: 패턴 C 적용
- [ ] `LockOnData` 필드 갱신

#### 3-4 WeaponBase 공용 HitVfx
- [ ] 공용 Variant (`SurfaceImpactMetal_TopView`) 생성
- [ ] 무기 데이터에서 개별 / 공용 피격 VFX 정책 확인

### Phase 4 — Laser (6~8시간) **별도 브랜치 권장**

`LaserBeam.cs`는 프로토 `_.html` 포팅 (LineRenderer 4겹 + SR 2겹 + ParticleSystem 수동 제어). 전면 교체 시 LifeArc / 펄스 기획 기능 손실 위험.

#### 방향: 하이브리드 (옵션 C)

- [ ] 빔 본체 (Core/Center/RingStroke/OuterGlow) → PA `PolyBeamStaticPurple` + 파티클 활용
- [ ] **LifeArc는 기존 `LineRenderer` 유지** → 수명 표시 기획 기능 보존
- [ ] `_centerParticles`는 PA 자체 파티클로 대체
- [ ] `LaserBeam.cs` 렌더러 바인딩부 (~40줄) 수정, 알파 제어 로직은 재사용
- [ ] `LaserBeamField.prefab`: Visual Socket + PA Aura Variant (패턴 A)
- [ ] `LaserWeaponData` / `LaserBeamData` SO 갱신

### Phase 5 — 검증 & 튜닝 (1~2시간)

- [ ] 전체 무기를 게임에서 플레이해보고 영상 캡처
- [ ] 동시 발사 상황 스트레스 테스트 (MachineGun + Shotgun + Bomb 동시)
- [ ] 프레임 드롭 확인 (PA 프리펩 내부 Light 컴포넌트가 다수면 제거)
- [ ] PA `PolygonSoundSpawn.cs` 내장 사운드 → 우리 `AudioManager`와 충돌 확인, 필요 시 Variant에서 제거

### Phase 6 — 정리 (1시간)

**삭제 대상** (사용처 없음 확인 후):
- [ ] `Assets/_Game/Prefabs/Weapons/*.png` 중 사용 안 하는 것 (`BombExplosionBurst`, `MachineGunBulletSprite`, `BombProjectileSprite`, `LaserGlow` 등)
- [ ] Trail Materials (`BombTrail_Mat`, `MachineGunTrail_Mat`)
- [ ] 옛 2D 프리펩 (사용처 없는 경우에 한해)
- [ ] `BombExplosionFx.cs` (Phase 3-1에서 삭제 완료됐는지 재확인)

---

## 5. 리스크 & 대응

| 리스크 | 영향도 | 대응 |
|---|---|---|
| PA 프리펩이 탑뷰에서 엉뚱한 방향으로 분출 | 중 | Variant 회전 룰 `(-90,0,0)` + 실기기 테스트로 검증 |
| 스케일 미스매치 (너무 크거나 작음) | 중 | Variant에서 scale 조정 후 확정 |
| PA 내장 Light 때문에 프레임 저하 | 저 | 다중 발사 무기 Variant는 Light 제거 |
| `LaserBeam` 개조 중 기획 기능 손상 | **고** | Phase 4는 별도 브랜치 + 하이브리드 방식 채택 |
| `PolygonSoundSpawn.cs` 사운드 중복 | 저 | Variant에서 컴포넌트 제거 |
| 빌드 용량 증가 | 저 | Variant 기반 의존성 추적 → 미사용 PA 자동 제외 |

---

## 6. 롤백 전략

- **Phase 단위 커밋** — 각 Phase는 독립적으로 `git revert` 가능
- Phase 6 정리 전까지 **기존 2D 프리펩·PNG 그대로 유지** → SO의 VFX 레퍼런스만 되돌리면 즉시 원복
- Laser는 별도 브랜치 작업 → 메인 브랜치 영향 없음

---

## 7. 성공 기준

- [ ] 무기 6종 전부 3D 이펙트로 전환 완료
- [ ] 동시 발사 스트레스 상황에서 60fps 유지
- [ ] 레이저 LifeArc / 펄스 기획 기능 보존
- [ ] 탑뷰 좌표계 룰 위반 없음 (`Quaternion.identity` 사용 0건)
- [ ] CHANGELOG 갱신

---

## 8. 참조

- [WeaponSystem.md](WeaponSystem.md) — 무기 시스템 현황
- [CLAUDE.md](../CLAUDE.md) §탑다운 좌표계 — 회전 룰
- 피드백 메모: `feedback_topdown_instantiate.md` — Instantiate 회전 보존 룰
- Polygon Arsenal 문서: `Assets/Polygon Arsenal/Polygon Arsenal 2.0 - Documentation.txt`

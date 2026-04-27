# Drill-Corp 최적화 현황

> 최종 갱신 2026-04-23 · 이전 이력은 [Sys-Optimization-History.md](Sys-Optimization-History.md) 참조
> 이 문서는 **"지금 이 순간의 최적화 상태"** 만 다룹니다. 왜 그 결정을 했는지·폐기된 접근·측정 원본은 History 문서로.

## 한 줄 요약

프레임 드랍의 주범으로 지목된 **VFX/DamagePopup Instantiate/Destroy 누수**를 **오브젝트 풀링**으로 차단. 개발자 PC HierarchyDumper 측정에서 **50초 세션 GC Δ +2.69 MB** 까지 개선 확인. URP 에셋 비용도 **Shadow Cascade 4→2, Shadowmap 2048→1024, SSAO off** 로 정리. **⚠️ 기획자 PC 재측정은 아직 안 된 상태** — "해결" 은 개발자 PC 기준으로만 확정됐고, 원 보고 환경(RTX 3060 기획자 PC)의 스파이크 75→115ms 가 실제로 감소했는지 CSV 로 검증 필요.

---

## 현재 상태 (2026-04-23 측정)

### 풀링 시스템 가동 중

| 풀 | 대상 | 현재 보유 | 싱글톤 |
|---|---|---:|---|
| **VfxPool** | 피격·사망·폭발·총구 파티클 VFX | 약 583개 | 자동 부트스트랩 |
| **DamagePopupPool** | 머신 피격 / 보석 획득 텍스트 | 약 339개 | 자동 부트스트랩 |
| **BulletPool** | 기관총 탄환 본체 (MachineGunBullet) | 16 prewarm | 자동 부트스트랩 |
| **BugPool** (기존) | 벌레 프리팹 | 300개 (설정값) | Inspector 배치 |

모두 `DontDestroyOnLoad` + `RuntimeInitializeOnLoadMethod(AfterSceneLoad)` 로 씬 배치 없이 동작.

### 최종 측정 수치 (50초 세션)

| 지표 | 풀링 전 | 풀링 후 (포화) | 판단 |
|---|---:|---:|---|
| `GC.GetTotalMemory` Δ | **+30~86 MB** | **+2.69 MB** | 🟢 누수 제거 |
| `ParticleSystem` Δ | +98~504 | **-45** | 🟢 풀 포화 |
| `DamagePopup` Δ | +256 | **0** | 🟢 풀 포화 |
| `TextMeshPro` Δ | +256 | **0** | 🟢 |
| `Mesh` (asset) Δ | +257 | **0** | 🟢 |
| `PooledVfx` Δ | — | **0 ~ +1** | 🟢 |

마지막 측정은 **-27 MB (회수 우세)** 까지 관측 — 벌레 사망으로 MinimapIcon 등 정리되며 순수 감소.

---

## 구현 내역

### 신규 파일

| 파일 | 역할 |
|---|---|
| `Assets/_Game/Scripts/VFX/Pool/VfxPool.cs` | VFX 풀 매니저 (프리팹별 Stack, auto-prewarm 8) |
| `Assets/_Game/Scripts/VFX/Pool/PooledVfx.cs` | VFX 프리팹 부착 컴포넌트 (cullingMode AlwaysSimulate 강제, OnParticleSystemStopped → Return) |
| `Assets/_Game/Scripts/UI/DamagePopupPool.cs` | DamagePopup 풀 매니저 (단일 Stack) |
| `Assets/_Game/Scripts/Weapon/Pool/BulletPool.cs` | 탄환 프리팹별 Stack 풀. 호출자가 명시적 Return |
| `Assets/_Game/Scripts/Editor/VfxPoolAttacher.cs` | VFX 프리팹에 PooledVfx 일괄 부착 메뉴 (선택적) |
| `Assets/_Game/Scripts/Editor/HierarchyDumper.cs` | A/B 스냅샷 + diff 도구 (진단용) |

### 수정된 호출부 (Instantiate → VfxPool.Get)

| 파일 | 라인 | 대상 |
|---|---|---|
| `Bug/Simple/SimpleBug.cs` | 320 | `_hitVfxPrefab`, `_deathVfxPrefab` |
| `Weapon/WeaponBase.cs` | 199 | `_baseData.HitVfxPrefab` |
| `Weapon/MachineGun/MachineGunBullet.cs` | 97 | `_data.HitVfxPrefab` |
| `Weapon/MachineGun/MachineGunWeapon.cs` | 198 | `BulletPrefab` → `BulletPool.Get` |
| `Weapon/MachineGun/MachineGunBullet.cs` | Despawn | `Destroy` → `BulletPool.Return` |
| `Weapon/Bomb/BombProjectile.cs` | 147, 155 | `HitVfxPrefab`, `ExplosionVfxPrefab` |
| `Weapon/Shotgun/ShotgunWeapon.cs` | 39 | `MuzzleVfxPrefab` |
| `UI/DamagePopup.cs` | 전체 | `new GameObject` → `DamagePopupPool.Acquire()` |

---

## 동작 원리

### VFX 풀링 흐름

```
[발사] WeaponBase.SpawnHitVfx(pos)
         ↓
       VfxPool.Get(prefab, pos, rot)
         ├─ 첫 호출: Instantiate × 8 (prewarm) + PooledVfx AddComponent
         └─ 이후: Stack.Pop → SetActive(true)
         ↓
       PooledVfx.OnEnable
         ├─ 파티클 Clear + Play
         └─ 10s 폴백 타이머 세팅 (OnParticleSystemStopped 누락 대비)
         ↓
       파티클 자연 종료
         ↓
       OnParticleSystemStopped → VfxPool.ReturnInternal
         └─ SetActive(false) + Stack.Push
```

### 핵심 설계 결정

| 포인트 | 결정 | 이유 |
|---|---|---|
| cullingMode | Awake 1회 `AlwaysSimulate` 강제 | PauseAndCatchup 이면 화면 밖 VFX 가 멈춰 OnParticleSystemStopped 영원히 안 옴 (이번 세션의 원인) |
| 서브 이미터 stopAction | 서브 이미터는 제외 | Unity 가 "Sub-emitters may not use stop actions" 경고 발생 |
| 프리팹 asset 수정 | 안 함 | 호출 지점 통합 (VfxPool.Get) + 런타임 AddComponent 로 커버. git diff 깔끔 |
| 풀 크기 제한 | **없음** (동적 성장) | 피크 수요만큼 성장 후 안정. 메모리 수 MB 수준이라 문제 없음 |
| 반환 트리거 | `OnParticleSystemStopped` + 10s 폴백 | 신뢰 가능한 조합 |

---

## 남은 작업 — 3갈래로 분류

### 🔴 검증 필요 (가장 중요)

이번 세션의 "해결" 을 **원 보고 환경에서 확정** 하는 작업. 여기를 먼저 해야 아래의 "정당한 보류" 판단도 확신이 생김.

| 항목 | 방법 |
|---|---|
| **기획자 PC 재측정** | PerfRecorder(F9) 로 60~120초 세션 녹화, 풀링 전과 비교 |
| **목표 수치** | p99 75ms → **< 20ms**, max 144ms → **< 30ms**, 스파이크 87회 → **< 10회**, GCUsed 증가 **< 10MB/세션** |
| **CSV 2종 수집** | `baseline` (무기/어빌리티 자동) + `heavy_combat` (어빌리티 적극 사용) |

### 🟡 정당한 보류 (현 시점 우선순위 낮음)

풀링 효과로 긴급도 떨어졌거나, 현재 활성 코드 경로 아닌 항목들. **기획자 PC 재측정 결과 나쁘면 순위 재조정 필요**.

#### 매 프레임 알로케이션 (풀링 후 긴급도 감소)
| 항목 | 파일 | 프레임당 | 상태 |
|---|---|---:|---|
| `AimWeaponRing.RebuildMesh` verts/uvs/tris 재할당 | `Aim/AimWeaponRing.cs:177` | ~16 KB | 3차 §3.3 — "가장 큰 단일 절감" 지목됐지만 풀링 후 힙 안정 |
| `PerfRecorder.OnGUI` GUIStyle 반복 생성 | `Diagnostics/PerfRecorder.cs:427` | ~2 KB | 녹화 중에만 비용. 측정 노이즈 |
| `PerfRecorder.SampleFrame` Samples 무한 누적 | `Diagnostics/PerfRecorder.cs:214` | ~1 MB/세션 | 녹화 중에만 비용 |
| HUD TMP 매 프레임 문자열 (Phase A-1) | `UI/HUD/TopBarHud.cs` 등 4개 | ~4 KB/s | dirty 가드 패턴. 풀링 후 GC 자체가 안정 |

#### 현재 씬 미사용 경로 (BugController 생태계)
| 항목 | 원본 계획 | 비고 |
|---|---|---|
| `BugHpBar` 공유 Sprite | 2차 A0-1 | BugController 전용. 현재 활성 벌레는 SimpleBug |
| `SimpleVFX.PlayBugHit` 핫패스 제거 | 2차 A0-3 | 동일 |
| `MinimapIcon` 풀 복귀 재활용 | 2차 A0-4 | 동일 |
| `BugController.Update` `is` 캐스팅 제거 | 2차 A-3 | 동일 |

#### 스킬 사용 시에만 비용 (어빌리티 경로)
| 항목 | 파일 | 비고 |
|---|---|---|
| 드론/거미 `Physics.OverlapSphere` 프레임 분산 | `Ability/Runners/Drone*.cs` | 2차 B-1. 현재 기본 무기만 측정 |
| 어빌리티 Runner VFX 풀링 | `Ability/Runners/*` | 드론/지뢰/메테오/블랙홀 6곳 — `Instantiate` 잔존 |

### 🟢 미착수 (풀링과 무관, 별도 작업)

진행이 늦어진 건 "풀링으로 체감 개선이 커서 급하지 않아진 것" 뿐. 착수하면 추가 이득 있음.

| 항목 | 대상 | 기대 효과 |
|---|---|---|
| `Gem` 드롭 오브젝트 풀링 | `Pickup/Gem.cs` | 초당 소수, 영향 작음 |
| `WorldUiTicker` 단일 tick (C-1) | HP바/라벨/미니맵 아이콘 LateUpdate 통합 | 벌레 많을 때 의미 |
| Gizmos 가드 (C-2) | `OnDrawGizmosSelected` 에 `#if UNITY_EDITOR` | 위생 |
| 미니맵 갱신 주기 완화 (C-3) | `MinimapCamera` | 매 프레임 렌더 → 격프레임 or 0.1s |

### ✅ 완료 (이번 세션 추가)

| 항목 | 변경 | 파일 |
|---|---|---|
| Shadow Cascade 4→2 | `m_ShadowCascadeCount: 4 → 2` | `Assets/Settings/PC_RPAsset.asset` |
| Shadowmap 해상도 2048→1024 | `m_MainLightShadowmapResolution`, `m_AdditionalLightsShadowmapResolution` | `Assets/Settings/PC_RPAsset.asset` |
| SSAO 비활성화 | `ScreenSpaceAmbientOcclusion.m_Active: 1 → 0` | `Assets/Settings/PC_Renderer.asset` |
| MachineGunBullet 본체 풀링 | `BulletPool` 도입, `Destroy/Instantiate` → `BulletPool.Get/Return`. TrailRenderer Clear 재사용 대응 | `Weapon/Pool/BulletPool.cs`, `Weapon/MachineGun/MachineGunBullet.cs`, `MachineGunWeapon.cs` |
| VFX 프리팹 duration 트리밍 (전체 8종) | 고빈도 3종 + 폭탄·레이저 5종. `lengthInSec` 를 startLifetime 최대값 수준으로 단축. **실측: FX_Bullet_Impact 풀 124→36, 전체 PooledVfx 143→48 (66% 감소)** | `VFX/Prefabs/FX_Bullet_Impact.prefab`, `FX_Bullet_Muzzle.prefab`, `FX_Death_01.prefab`, `FX_Grenade_Impact.prefab`, `FX_Grenade_Muzzle.prefab`, `FX_Grenade_Projectile.prefab`, `FX_Laser_Impact.prefab`, `FX_Laser_Muzzle.prefab` |
| (Bloom / DoF / Motion Blur / Vignette / FilmGrain) | 원래부터 intensity=0 — 변경 불필요 | `Assets/Settings/DefaultVolumeProfile.asset` |


---

## 진단 도구

### `HierarchyDumper` — A/B 스냅샷 비교

**파일**: `Assets/_Game/Scripts/Editor/HierarchyDumper.cs`

플레이 중 특정 시점의 Hierarchy + GC 상태 + 컴포넌트 집계를 텍스트로 저장하고 두 스냅샷 diff 를 뽑는다.

| 단축키 | 동작 |
|---|---|
| `Ctrl+Alt+Shift+Q` | 스냅샷 A 저장 |
| `Ctrl+Alt+Shift+W` | 스냅샷 B 저장 |

메뉴 `Tools/Drill-Corp/Dev/Hierarchy 덤프/diff 생성` 으로 diff 파일 생성. 출력 `HierarchyDumps/diff_AB_*.txt`.

### `PerfRecorder` — 세션 로거

**파일**: `Assets/_Game/Scripts/Diagnostics/PerfRecorder.cs`

| 단축키 | 동작 |
|---|---|
| `F9` | 녹화 시작/정지 |
| `F10` | 라벨 순환 (baseline / wave_fighting / drones_active / heavy_combat) |

출력 `PerfLogs/{label}_{timestamp}.csv` + `_spikes.csv`. 정지 순간 클립보드 복사 + 폴더 오픈.

### `PerfMarkers` — 의심 구간 계측

**파일**: `Assets/_Game/Scripts/Diagnostics/PerfMarkers.cs`

11개 구간에 `ProfilerMarker` 직접 심어둠 (BugController.Update, 드론/거미 Update, HUD Update 등).

### `VfxPoolAttacher` — 에디터 일괄 부착 (선택)

**파일**: `Assets/_Game/Scripts/Editor/VfxPoolAttacher.cs`

메뉴 `Tools/Drill-Corp/Dev/VFX 풀링/모든 VFX 프리팹에 PooledVfx 부착`. 현재는 **필수 아님** — 런타임 AddComponent 로 커버됨. 프리팹 원본에 세팅 남기고 싶을 때만 사용.

---

## 주요 학습 / 판단 근거

### "GC 누수" vs "매 프레임 알로케이션" 구분

- **누수**: 해제 안 돼 Managed Heap 이 선형 증가 → 시간 지날수록 스파이크 악화 → **VFX/DamagePopup 이 이 경우**
- **알로케이션**: 매 프레임 단명 객체 생성 → 일정한 GC 주기 → 프레임당 비용만 큼 → AimWeaponRing/PerfRecorder.OnGUI 가 이 경우

이번 세션은 **누수만 해결**. 알로케이션 쪽은 체감 영향 작아 보류.

### 풀링이 어떤 경우에 적합한가

| 적합 | 부적합 |
|---|---|
| 초당 여러 번 생성 | 세션당 1~2번 생성 |
| 수명 몇 초 이내 | 영구 보존 |
| 동일 프리팹 반복 | 매번 다른 형태 |

**드릴콥 기준**: VFX/DamagePopup/탄환/벌레 → 적합. UI 창/지형/캐릭터 → 부적합.

### 측정 방법론

- **풀 성장 구간 측정 금지** — 풀이 피크 수요까지 채워질 때까지는 Δ 가 계속 증가. 풀 포화 후 측정해야 진짜 누수 여부 확인 가능.
- **측정 시간 50~100초** — 기획자 PC 이슈가 "시간 지날수록 스파이크 증가" 였으므로 이 정도는 돌려야 누수 drift 관측.

---

## 관련 문서

- [Sys-Optimization-History.md](Sys-Optimization-History.md) — 1차~4차 의사결정 연대기 + 원본 측정
- [archive/2026-04_optimization/](archive/2026-04_optimization/) — 원본 세부 문서 (FrameDrop / Optimization 01~04 / 구 Overview)

## 관련 메모리

개인 메모리에 저장된 관련 학습:
- `feedback_unity6_api_deprecations.md` — `FindObjectsByType` SortMode 제거, `GetInstanceID → GetEntityId`
- `feedback_vfx_polygon_arsenal_first.md` — VFX 제작 전 Polygon Arsenal 재활용 먼저
- `feedback_topdown_coordinates.md` — XZ 평면 / Z=화면상하 좌표계 주의

# 최적화 4차 — VFX 풀링 시스템 도입 계획

> 작성 2026-04-23 · 선행 [Optimization_03_ActiveCodePath.md](Optimization_03_ActiveCodePath.md) 의 "파티클 누수" 범인 확정 이후
> 목표: 프리팹 단위 `AutoDestroyPS` 부착 방식에서 **VFX 풀링** 으로 전환해 근본 해결

## 배경 — 여기까지 온 경로

### 1. 1~3차 최적화 요약

- **1차** ([Optimization_01.md](Optimization_01.md)): 프레임 드랍 원인 후보 정리 + `PerfRecorder` / `PerfMarkers` 계측 도구 도입. 개발자 PC 평균 201 fps 로 병목 없음 확인. 기획자 PC 실측 필요.
- **2차** ([Optimization_02_PlannerPCAnalysis.md](Optimization_02_PlannerPCAnalysis.md)): 기획자 PC CSV 실측 → `GCUsed 735MB` · 스파이크 75→115ms 점진 증가 확인. BugHpBar / DamagePopup / SimpleVFX / MinimapIcon 을 누수 후보로 지목.
- **3차** ([Optimization_03_ActiveCodePath.md](Optimization_03_ActiveCodePath.md)): 2차의 타깃이 **현재 Game 씬과 어긋남** 을 발견. BugController 생태계는 풀에 300마리 대기 중이지만 실제 활성은 SimpleBug. `AimWeaponRing` / `PerfRecorder OnGUI` / MG/Laser 가 진짜 핫패스.

### 2. 4차의 출발점 — Hierarchy 덤프로 누수 확정

3차 결과를 더 확인하려고 `HierarchyDumper.cs` 에디터 도구를 만들었다. 플레이 중 일시정지 상태에서 Hierarchy + GC 상태 + 컴포넌트 집계를 텍스트로 덤프하고, 두 시점 A/B 를 비교해 diff 생성.

**측정 결과 (50초간)**:

| 지표 | A (3초) | B (53초) | Δ |
|---|---:|---:|---:|
| `GC.GetTotalMemory` | 640.97 MB | 673.88 MB | **+32.91 MB / 50s = 40 MB/분** |
| `ParticleSystem` 인스턴스 | 63 | 161 | **+98** |
| `ParticleSystemRenderer` | 63 | 161 | +98 |
| `PolygonSoundSpawn` | 20 | 38 | +18 |
| `Texture2D` | 937 | 937 | 0 ✅ |
| `Material` | 192 | 195 | +3 (무시) |
| `AutoDestroyPS` 부착 인스턴스 | 0 | 1 | +1 (대부분 미부착!) |

**결론**: **ParticleSystem 이 시간당 7200개 속도로 누적 중**. 누수의 진짜 범인 확정.

### 3. 범인 확정 — `cullingMode = PauseAndCatchup`

`FX_Bullet_Impact.prefab` 분석:
- 루트 `lengthInSec: 5`, `looping: 0`, `stopAction: 0`
- **`cullingMode: 3` (PauseAndCatchup)** ← **결정적**
- 자식 PS 3개 (Glow/Smoke/Sparks)

**왜 누수인가**:
- `PauseAndCatchup` = 파티클이 **카메라 frustum 밖** 에 있으면 시뮬레이션 일시정지
- 현재 게임: SimpleBug 가 머신 향해 행진 → 대부분 화면 밖에서 생성·이동 → 피격 VFX 도 화면 밖에서 스폰
- 시뮬레이션 정지 → 수명 감소 안 함 → `isPlaying` 계속 true → `OnParticleSystemStopped` 영원히 안 불림
- GameObject 영원히 안 파괴 → **매 피격마다 Heap 에 누적**

### 4. 1차 대응 — `AutoDestroyPS` 강화 + 프리팹 일괄 부착

코드 수정:
- `Assets/_Game/Scripts/VFX/AutoDestroyPS.cs` — 자식 PS 도 stopAction=Callback + cullingMode=AlwaysSimulate 강제 + 10s 폴백 타이머
- `Assets/_Game/Scripts/Bug/Simple/SimpleBug.cs:SpawnScaledVfx` — 타이머 기반 Destroy 대신 `EnsureAutoDestroy(vfx)` 호출 (런타임 AutoDestroyPS 동적 부착)

에디터 도구:
- `Assets/_Game/Scripts/Editor/VfxAutoDestroyAttacher.cs` — `Assets/_Game/VFX/Prefabs/*.prefab` 전체 스캔, AutoDestroyPS 일괄 부착 메뉴
  - `Tools/Drill-Corp/Dev/VFX 정리/모든 VFX 프리팹에 AutoDestroyPS 부착`
  - `Tools/Drill-Corp/Dev/VFX 정리/AutoDestroyPS 누락 프리팹 목록`

**재측정 결과 (47초간)**:

| 지표 | 1차 (AutoDestroyPS 전) | 2차 (AutoDestroyPS 후) |
|---|---:|---:|
| GC.GetTotalMemory Δ / 분 | 40 MB | **110 MB ⬆️ (악화!)** |
| ParticleSystem Δ | +98 | **+237** |
| AutoDestroyPS 인스턴스 | 1 | 89 (+69) |

**왜 악화됐나**:
- 벌레 활성 수가 다름 (이전 22→99, 이번 72→97) — warm-up 된 상태에서 측정됨
- `AutoDestroyPS` 자체도 +69 증가 → **부착됐는데도 Destroy 가 안 됨**
- 원인 추정: cullingMode 런타임 변경이 제대로 적용 안 됐거나, 자식 PS 중 일부가 여전히 정지 중이거나, 콜백 체인 타이밍 문제

## 핵심 결론

**AutoDestroyPS 방식의 한계**:
1. 프리팹마다 수동 부착 필요 → 누락 리스크
2. cullingMode 런타임 변경의 신뢰성 불명확
3. 자식 PS / 서브 이미터 / 트레일 조합에서 종료 감지 불완전
4. 근본적으로 "알아서 죽어줘" 방식은 확률적 실패 가능

**풀링이 근본 해결인 이유**:
1. **Destroy 자체가 없음** → 누수 경로 원천 차단
2. Instantiate 도 초기화만 — 런타임 GC 압박 0
3. 이미 `BugPool` 로 같은 패턴 검증됨 (300마리 벌레 재사용)
4. `cullingMode` 같은 세부 설정 문제 무관 — 재활용이 본질

---

## VFX 풀링 시스템 설계

### 구조

```
VfxPool (싱글톤, DontDestroyOnLoad)
  ├─ Dictionary<GameObject prefab, Stack<GameObject>> _pools
  ├─ Transform _root  (Hierarchy 정리용)
  └─ API:
       Get(prefab, position, rotation, parent=null)  → GameObject
       Return(GameObject instance)                   → void
       Prewarm(prefab, count)                        → void

PooledVfx (프리팹 루트에 부착하는 MonoBehaviour)
  ├─ Awake: 루트+자식 PS 의 cullingMode=AlwaysSimulate, stopAction=Callback 세팅
  ├─ OnParticleSystemStopped: VfxPool.Return(this) 호출
  ├─ 폴백 타이머: 10초 후 강제 Return (안전망)
  └─ OnEnable: ParticleSystem.Clear + Play 재시작
```

### API 사용법

**Before** (`SimpleBug.SpawnScaledVfx`):
```csharp
GameObject vfx = Instantiate(prefab);
vfx.transform.position = _fxSocket != null ? _fxSocket.position : transform.position;
vfx.transform.localScale = Vector3.Scale(vfx.transform.localScale,
    transform.localScale * _vfxScaleMultiplier);
EnsureAutoDestroy(vfx);  // 타이머/콜백 기반 Destroy 예약
```

**After**:
```csharp
Vector3 pos = _fxSocket != null ? _fxSocket.position : transform.position;
GameObject vfx = VfxPool.Get(prefab, pos, Quaternion.identity);
vfx.transform.localScale = Vector3.Scale(vfx.transform.localScale,
    transform.localScale * _vfxScaleMultiplier);
// Destroy 는 PooledVfx 가 자동 처리 → 코드 개입 불필요
```

한 줄만 변경: `Instantiate` → `VfxPool.Get`. `EnsureAutoDestroy` 호출 제거.

### 적용 범위 (우선순위)

| 순위 | 파일 | 경로 | 현재 빈도 | 누수 기여도 |
|---|---|---|---|---|
| **1** | `Bug/Simple/SimpleBug.cs` | `_hitVfxPrefab`, `_deathVfxPrefab` (`SpawnScaledVfx`) | 초당 수~수십 회 | **주범** |
| **2** | `Weapon/WeaponBase.cs:199` | `_baseData.HitVfxPrefab` | 모든 무기 공통 | 높음 |
| **3** | `Weapon/MachineGun/MachineGunBullet.cs:97` | `_data.HitVfxPrefab` | 초당 10회 | 높음 |
| **4** | `Weapon/Bomb/BombProjectile.cs:147, 155` | `HitVfxPrefab`, `ExplosionVfxPrefab` | 저빈도 | 중간 |
| 5 | `Weapon/Shotgun/ShotgunWeapon.cs:39` | `MuzzleVfxPrefab` | 저빈도 | 낮음 |
| 6 | `Bug/Behaviors/Attack/AttackBehaviorBase.cs:94` | `_hitVfxPrefab` (BugController 경로, 현재 Game 씬 미사용) | 0 회 | 없음 |

**1~4 만 적용해도 누수 주범 거의 다 제거** 예상.

### 설계 결정 (확정)

| 결정 | 선택 | 이유 |
|---|---|---|
| Q1 초기 크기 | **동적 성장 + 첫 Get 시 pre-warm 8개** | 완전 정적은 초기 스파이크, 완전 동적은 콜드 미스 |
| Q2 API 스타일 | **Option A — `VfxPool.Get(prefab, pos, rot)` 직접** | 기존 `GameObject` 필드 변경 없음, 가장 단순 |
| Q3 반환 트리거 | **OnParticleSystemStopped + 10s 폴백 타이머** | 이미 `AutoDestroyPS` 에서 검증된 조합 |
| Q4 cullingMode 처리 | **Awake 1회 AlwaysSimulate 강제** | 풀 GameObject 는 재활용되므로 첫 Awake 면 충분 |
| Q5 Hierarchy 배치 | **DontDestroyOnLoad 의 VfxPool 루트 밑** | BugPool 과 동일 패턴, 일관성 |

### 단계별 구현 계획

**1단계 — MVP (다음 세션 우선)**
- 파일 신규 생성:
  - `Assets/_Game/Scripts/VFX/Pool/VfxPool.cs` (싱글톤 매니저)
  - `Assets/_Game/Scripts/VFX/Pool/PooledVfx.cs` (프리팹 부착용)
  - `Assets/_Game/Scripts/Editor/VfxPoolAttacher.cs` (프리팹에 `PooledVfx` 일괄 부착 메뉴)
- `SimpleBug.SpawnScaledVfx` 만 풀링으로 교체
- `AutoDestroyPS` 는 그대로 두기 (다른 경로 호환성)
- 재측정 → 효과 확인

**2단계 — 무기 경로 적용 (1단계 효과 확인 후)**
- `WeaponBase.PlayHitVfx` → `VfxPool.Get`
- `MachineGunBullet.cs:97` → `VfxPool.Get`
- `BombProjectile.cs:147, 155` → `VfxPool.Get`
- `ShotgunWeapon.cs:39` → `VfxPool.Get`
- 재측정

**3단계 — 정리 (선택)**
- 모든 경로 풀링 전환 완료되면:
  - `AutoDestroyPS` 붙은 프리팹에서 컴포넌트 제거 (에디터 메뉴)
  - `VfxAutoDestroyAttacher` 폐기 or deprecate 마킹
  - `EnsureAutoDestroy` 헬퍼 제거

### 예상 효과

| 지표 | 현재 (AutoDestroyPS) | 풀링 후 예상 |
|---|---:|---:|
| `GC.GetTotalMemory` Δ / 1분 | +100 MB | **< 3 MB** |
| `ParticleSystem` Δ / 1분 | +237 | **+0 ~ +20** (풀 성장분만) |
| Instantiate 호출 / 초 | 수 회 | **0 회** (warm-up 이후) |
| Destroy 호출 / 초 | 수 회 | **0 회** |
| `AutoDestroyPS` 누적 | 있음 | 없음 (풀링은 Destroy 안 함) |

---

## 현재 상태 스냅샷 (다음 세션 시작점)

### 완료된 것

1. ✅ **Hierarchy 덤프 도구** (`HierarchyDumper.cs`) — A/B/diff 3단 메뉴, Ctrl+Alt+Shift+Q/W 단축키
2. ✅ **VFX 누수 주범 확정** — cullingMode=PauseAndCatchup + 화면 밖 스폰 조합
3. ✅ **`AutoDestroyPS` 강화** — 자식 PS 처리, cullingMode 강제, 폴백 타이머
4. ✅ **`VfxAutoDestroyAttacher` 에디터 메뉴** — 프리팹 일괄 부착
5. ✅ **`SimpleBug.SpawnScaledVfx` 리팩토링** — EnsureAutoDestroy 사용
6. ✅ **Unity 6 API deprecation 메모리 저장** — `FindObjectsByType` SortMode 제거 등
7. ✅ **`.gitignore`** — `HierarchyDumps/`, `PerfLogs/` 추가

### AutoDestroyPS 방식의 잔존 문제

- 부착했는데도 누수 계속 (+69 AutoDestroyPS 인스턴스 누적)
- 원인 불명확: cullingMode 런타임 변경 신뢰성 의심
- 근본 해결 필요 → 풀링으로 전환

### 미적용 (다음 세션 작업)

**최우선**:
1. `VfxPool.cs` + `PooledVfx.cs` 신규 구현
2. `VfxPoolAttacher.cs` 에디터 메뉴 (프리팹에 `PooledVfx` 부착)
3. `SimpleBug.SpawnScaledVfx` → `VfxPool.Get` 교체
4. 재측정 (`Ctrl+Alt+Shift+Q/W` 로 A/B 스냅샷 → diff)

**2순위 (1단계 효과 확인 후)**:
5. `WeaponBase.PlayHitVfx` 풀링 적용
6. `MachineGunBullet.cs:97` 풀링 적용
7. `BombProjectile.cs:147, 155` 풀링 적용

**다른 최적화 지점 (보류 중)**:
- `AimWeaponRing.RebuildMesh` verts/uvs/tris 필드 캐시 (3차 §3.3, 16KB/frame)
- `PerfRecorder.OnGUI` GUIStyle static-cache (3차 §3.2)
- `PerfRecorder.SampleFrame` Samples 무한 누적 → circular buffer (3차 §3.1)
- HUD TMP dirty 가드 (`TopBarHud`, `AbilityHud` 등)

---

## 참고 문서

- [Optimization_Overview.md](Optimization_Overview.md) — 1+2차 통합 문서 (초심자용 설명 포함)
- [Optimization_01.md](Optimization_01.md) — 1차 원본
- [Optimization_02_PlannerPCAnalysis.md](Optimization_02_PlannerPCAnalysis.md) — 2차 (기획자 PC CSV 분석)
- [Optimization_03_ActiveCodePath.md](Optimization_03_ActiveCodePath.md) — 3차 (현재 씬 핫패스 재조사)
- [FrameDropInvestigation.md](FrameDropInvestigation.md) — 초기 원인 후보 정리

## 관련 도구

- `Assets/_Game/Scripts/Diagnostics/PerfRecorder.cs` — F9 녹화 / F10 라벨
- `Assets/_Game/Scripts/Diagnostics/PerfMarkers.cs` — 11개 의심 구간 계측
- `Assets/_Game/Scripts/Editor/HierarchyDumper.cs` — Hierarchy 덤프 + A/B diff (Ctrl+Alt+Shift+Q/W)
- `Assets/_Game/Scripts/Editor/VfxAutoDestroyAttacher.cs` — AutoDestroyPS 일괄 부착 (풀링 전환 시 deprecated)
- `Assets/_Game/Scripts/VFX/AutoDestroyPS.cs` — 파티클 종료 감지 → 자동 파괴 (풀링 도입 시 대체)

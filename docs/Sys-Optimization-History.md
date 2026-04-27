# Drill-Corp 최적화 이력

> 현재 상태 요약은 [Sys-Optimization.md](Sys-Optimization.md) 참조
> 이 문서는 **의사결정 과정 연대기** — 잘못 짚었던 방향, 왜 그렇게 판단했는지, 어떻게 뒤집혔는지를 보존합니다.
> 원본 상세 문서는 `archive/2026-04_optimization/` 에 그대로 있습니다.

## 세션 일람

| 차수 | 기간 | 상태 | 핵심 |
|---|---|---|---|
| 1차 | 2026-04-22 | 완료 | 계측 도구 도입 (PerfRecorder / PerfMarkers), 개발자 PC baseline 201 fps |
| 2차 | 2026-04-23 | 완료 | 기획자 PC CSV 수령 · GCUsed 735MB / 스파이크 75→115ms 관측 · 누수 차단 Phase A0 신설 |
| 3차 | 2026-04-23 | 완료 | 활성 코드 경로 재조사 · 2차 타깃 일부가 현재 씬에서 비활성 확인 |
| 4차 | 2026-04-23 | 완료 | VFX/DamagePopup 풀링 구현 · 누수 완전 차단 측정 확인 |

---

## 1차 — 프레임 드랍 조사 + 계측 도구 (2026-04-22)

**트리거**: 기획자 PC(RTX 3060) 에디터 플레이 렉 보고. 개발자 PC 에서는 재현 안 됨 → 실측 기반 진단 체계 필요.

**수행**:
- `FrameDropInvestigation.md` 에 원인 후보 정리 (HUD TMP 매 프레임, 월드 UI LateUpdate 러시, 드론 OverlapSphere, VFX 풀링 부재, URP 과설정)
- `PerfRecorder.cs` 구현 — F9/F10 기반 세션 로거, CSV + 스파이크 분리 출력, 클립보드 복사 + 폴더 자동 오픈
- `PerfMarkers.cs` — 의심 구간 11개에 `ProfilerMarker` 심음
- 개발자 PC baseline 47초 녹화 → **평균 201 fps, p99 8.92ms, 병목 없음** → 기준선으로만 사용

**사이드 수정**: Unity 6.4 `GetInstanceID → GetEntityId` 마이그레이션 (`SpiderDroneInstance.cs`). `EntityId` 는 int 캐스팅 금지되어 `HashSet<int> → HashSet<EntityId>` 로 변경.

**판단**: 추측만으로 우선순위 잡는 것 위험. 기획자 PC 실측 CSV 수령 전까지 실제 수정 착수 보류.

**산출물**: `archive/2026-04_optimization/Optimization_01.md`, `archive/2026-04_optimization/FrameDropInvestigation.md`

---

## 2차 — 기획자 PC CSV 분석 (2026-04-23 오전)

**입력**: `PerfLogs/baseline_20260423_063517.csv` — 46.8초, 벌레 0, 라벨 `baseline`

**관측**:
| 지표 | 값 | 해석 |
|---|---|---|
| 평균 FPS | 144.5 | 평상시는 쾌적 |
| MainThread p99 | **75.30 ms** | 하위 1% = 13fps (체감 렉) |
| MainThread max | **144.39 ms** | 최악 7fps |
| 스파이크 빈도 | 87회 / 46.8s ≈ 1.9 Hz | 78프레임마다 1번 |
| 평균 스파이크 크기 | 초반 75ms → 후반 **115ms** | **시간 지날수록 악화** |
| **GCUsed** | **735 MB** | 핵심 신호 |

**핵심 결론**: 관리 힙이 735MB 까지 부풀었고 **스파이크가 시간 지날수록 커진다** → 전형적 **누수 곡선**. 단순 누적으로는 설명 불가 (46.8s × 25KB/frame = 1.1MB 에 불과한데 735MB).

**누수 후보 지목** (코드 리뷰):
| 지점 | 근거 |
|---|---|
| `BugHpBar.CreateSquareSprite` | 벌레 1마리당 `new Texture2D(1,1)` × 2 + `new Sprite` × 2. GC 회수 늦음 |
| `DamagePopup.Create` | `new GameObject + AddComponent<TextMeshPro>` 매 피격 (TMP = 수 KB~수십 KB) |
| `SimpleVFX.PlayBugHit` | `new Gradient + new Burst[]` 매 호출 폴백 경로 |
| `MinimapIcon` 풀 복귀 | `BugController.OnDestroy` 에서 Destroy → 다음 스폰 시 MeshFilter/Renderer 재생성 |

**계획**: **Phase A0 (누수 차단)** 을 Phase A (퀵윈) 앞에 삽입. 누수부터 막아야 그 다음 개선이 효과를 냄.

**남은 의문**: "Memory Profiler 스냅샷으로 735MB 실체 확인" 필요 — 문서에 권장했지만 사용자 환경에서 불가 판단.

**산출물**: `archive/2026-04_optimization/Optimization_02_PlannerPCAnalysis.md`, `archive/2026-04_optimization/Sys-Optimization.md` (1+2차 통합본)

---

## 3차 — 활성 코드 경로 재조사 (2026-04-23 낮)

**트리거**: 2차의 후보들이 전부 **벌레 피격 / 사망 트리거** 경로. 그런데 실측은 "벌레 0 baseline" 세션. **경로 자체가 안 도는데 왜 누수?** 모순 발견.

**방법**: `Game.unity` yaml 의 `m_Script guid` 를 `.cs.meta` 와 매칭 → 현재 씬에 실제 배치된 MonoBehaviour 35종 추출 → 각 Start/Update/LateUpdate 전수 리뷰.

**주요 발견**:

### 3.1 벌레 0 에서도 상시 Update 돌고 있는 핫패스
- `AimWeaponRing.LateUpdate` × 4 → **매 프레임 `new Vector3[130] + new Vector2[130] + new int[384]`** (4개 링 × 4KB × 144fps = 2.3 MB/s 단명 alloc)
- `MachineGunWeapon.Fire` → 적 0 이어도 자동 연사 (`ShouldFire` 가 `_currentAmmo > 0` 만 체크)
- `LaserWeapon.Fire` → 적 0 이어도 쿨다운마다 빔 스폰
- `PerfRecorder.OnGUI` → 매 호출 `new GUIStyle()` (290KB/s 단명 alloc)
- `PerfRecorder.SampleFrame` → `Channel.Samples.Add(double)` 세션 내내 (~2 MB/세션 retained)

### 3.2 2차 타깃 상태 재확인 — **BugController 경로가 현재 씬에 없음**
- `WaveManager`, `MachineStatusUI`, `MiningUI`, `Hp3DBar`, `BugHpBar`, `DamagePopup` 의 guid 를 `Game.unity` + 모든 `.prefab` 에서 grep → **참조 0 또는 소수**
- 즉 **2차의 Phase A0-1 (BugHpBar Sprite 공유), A0-3 (SimpleVFX), A0-4 (MinimapIcon) 는 현재 씬에서 doa** (dead-on-arrival)
- 현재 활성 벌레는 `SimpleBug` (BugController 와 별개)

### 3.3 (이후 뒤집힌 3차 판단) `SimpleBugSpawner` 씬 상태
- yaml 상 `m_IsActive: 1, m_Enabled: 1, _maxBugs: 90` → **baseline 이 정말 "벌레 0"인지 의심**
- 코드 리뷰만으로는 "정말 spawner 꺼두고 측정했는지" 확인 불가 → 런타임 Hierarchy 확인이 답이라고 결론

**결론**: "735MB 단일 범인은 코드 리뷰로 특정 불가. Editor baseline + Incremental GC scan-time 가능성". 우선 `AimWeaponRing` mesh rebuild 캐시가 가장 큰 단일 절감 후보.

**그런데 이 3차 결론이 4차에서 일부 뒤집힘**:
- 3차에서 "`DamagePopup` 은 씬에 없다" 로 분류 → 실제로는 `MachineController.cs:243` 에서 **호출됨** (머신 피격용). grep 시 `.prefab` 참조만 보고 코드 호출 경로는 누락.
- 3차에서 "BugPool 에 `BugController` 300개 있지만 현재 씬에선 inactive" 로 추정 → 4차 HierarchyDumper 로 확인 시 **300개 preload 실재** 하지만 실활성은 0. 3차 판단 부분 정확, 부분 부정확.

**산출물**: `archive/2026-04_optimization/Optimization_03_ActiveCodePath.md`

---

## 4차 — VFX 풀링 구현 (2026-04-23 오후)

**트리거**: 3차의 코드 리뷰 결론이 불확실 → **런타임 실측 도구** 필요.

### 4.1 HierarchyDumper 도입

코드 리뷰로 해결 불가능한 "지금 이 순간 씬에 뭐가 몇 개 있나" 를 덤프하는 에디터 도구 구현:
- Ctrl+Alt+Shift+Q/W → A/B 스냅샷
- 메뉴로 diff 생성 (GC 상태 + 컴포넌트 집계 + Hierarchy Tree)
- 단축키 충돌 피해 `%&#q` / `%&#w` 조합 선택 (1/2 는 Unity 기본 단축키와 충돌)

**사이드 버그**: 플레이 중 컴파일 시 static 필드 `_lastSnapshotA/B` 리셋 → diff 가 "스냅샷 A 가 없습니다" 로 실패. `ResolveSnapshotPath()` 에서 파일 시스템 최신 `*_A.txt` / `*_B.txt` 로 fallback 로직 추가.

### 4.2 첫 측정 — 진짜 누수 범인 확정

50초 플레이 측정:
| 지표 | A→B |
|---|---|
| `GC.GetTotalMemory` | 640 → 673 MB (+32.91 MB / 50s = **40 MB/분**) |
| `ParticleSystem` | 63 → 161 (**+98**) |
| `AutoDestroyPS` | 0 → 1 (거의 부착 안 됨) |

**결론**: ParticleSystem 이 분당 7200개 속도로 누적. **VFX 가 진짜 범인**. 2차의 DamagePopup/BugHpBar 추정은 현재 씬에서 주범 아니고, 진짜는 SimpleBug 피격 VFX.

### 4.3 `FX_Bullet_Impact.prefab` 분석 — 누수 메커니즘 확정

프리팹 직접 확인:
```yaml
main:
  lengthInSec: 5
  looping: 0
  stopAction: 0
  cullingMode: 3   # ← PauseAndCatchup (결정적!)
```

**누수 체인**:
1. `cullingMode = PauseAndCatchup` → 카메라 frustum 밖이면 시뮬레이션 일시정지
2. Drill-Corp 는 SimpleBug 가 머신 향해 행진 → **피격 VFX 대부분 화면 밖에서 스폰**
3. 시뮬레이션 정지 → 수명 감소 안 함 → `isPlaying` 영구 true
4. `OnParticleSystemStopped` 영원히 안 불림 → Destroy 예약된 적 없어서 GameObject 영구 누적

### 4.4 1차 대응 — `AutoDestroyPS` 강화 (실패)

`AutoDestroyPS.cs` 수정:
- 자식 PS 까지 전부 `stopAction=Callback` + `cullingMode=AlwaysSimulate` 강제
- 10s 폴백 타이머 추가
- `VfxAutoDestroyAttacher.cs` 에디터 메뉴로 프리팹 일괄 부착

**재측정 결과 — 오히려 악화**:
| 지표 | 1차 | 2차 |
|---|---:|---:|
| GC/분 | +40 MB | **+110 MB** |
| PS Δ | +98 | **+237** |
| AutoDestroyPS 인스턴스 | 1 | 89 (+69) |

**원인 분석**: 부착됐는데도 Destroy 안 됨. cullingMode 런타임 변경 신뢰성 불명확. 자식 PS 조합에서 종료 감지 불완전.

**판단**: 부착 방식은 **확률적 실패**. 근본 접근 변경 필요.

### 4.5 방향 전환 — 풀링 도입

사용자 제안 "**vfx 풀링 시스템으로 관리하자**" 를 수용. 설계:

```
VfxPool (싱글톤, DontDestroyOnLoad)
  └ Dictionary<GameObject prefab, Stack<GameObject>>
     Get(prefab, pos, rot) → PooledVfx 자동 부착

PooledVfx (프리팹 부착 컴포넌트)
  ├ Awake: cullingMode=AlwaysSimulate + stopAction=Callback 1회 강제
  ├ OnEnable: 파티클 Clear + Play
  ├ OnParticleSystemStopped → VfxPool.Return
  └ 10s 폴백 타이머
```

핵심 결정:
| 선택 | 이유 |
|---|---|
| 동적 성장 + auto-prewarm 8 | 완전 정적은 초기 스파이크, 완전 동적은 콜드 미스 |
| `VfxPool.Get(prefab, pos, rot)` 직접 API | 기존 GameObject 필드 변경 없음 |
| 프리팹 asset 수정 안 함 | 호출 지점 통합 (사용자 제안 따름) + 런타임 AddComponent |

### 4.6 구현 — 1단계 MVP

신규 파일: `VfxPool.cs`, `PooledVfx.cs`, `VfxPoolAttacher.cs`
교체: `SimpleBug.SpawnScaledVfx` → `VfxPool.Get`

**Sub-emitters 경고**: `stopAction=Callback` 을 서브 이미터에도 적용 → Unity 가 "Sub-emitters may not use stop actions" 경고. 서브 이미터 수집 후 제외 로직 추가 (PooledVfx + AutoDestroyPS 둘 다).

### 4.7 2단계 — 무기 경로 확장

사용자 지적: "호출 지점 통합 vs 프리팹 수정" 비교. 결론: **호출 지점 통합이 근본**.

교체:
- `WeaponBase.SpawnHitVfx:199`
- `MachineGunBullet:97`
- `BombProjectile:147, 155`
- `ShotgunWeapon:39`

### 4.8 DamagePopup 풀링 — 3차 판단의 반전

첫 풀 포화 측정에서 **DamagePopup +256** 관측. 3차에서 "씬에 없음" 으로 분류했던 DamagePopup 이 실제로는 `MachineController.cs:243` 에서 호출 — 머신 피격 시 초당 5개 누수.

**신규**: `DamagePopupPool.cs` — 싱글톤 + 단일 Stack. 공개 API (`Create`/`CreateText`) 시그니처 그대로 유지 → 호출부 수정 0.
**수정**: `DamagePopup.cs` — `new GameObject` → `DamagePopupPool.Acquire`, `Destroy(gameObject)` → `DamagePopupPool.Release(this)`.

### 4.9 한글 텍스트 누수 가설 (기각됨)

"Gem 획득 시 `+5 보석`" 한글이 TMP 아틀라스 동적 확장을 유발해 Mesh 누수라고 추정 → 재측정에서 **Gem 7개 획득해도 Mesh Δ = 0** → 가설 기각. Mesh 도 풀링으로 재사용됨.

### 4.10 최종 측정 — 누수 완전 차단

50초 플레이 × 여러 차례 측정:
| 지표 | 풀링 전 | 최종 |
|---|---:|---:|
| GC/50s | +30~86 MB | **+2.69 MB** / 최근 **-27 MB (회수 우세)** |
| PS Δ | +98~504 | **-27** |
| DamagePopup Δ | +256 | **0** |
| Mesh Δ | +257 | **0** |
| PooledVfx Δ | — | **0 ~ +1** |

**확정**: Drill-Corp VFX/DamagePopup 누수 완전 해결.

### 4.11 판단 복기 — 무엇이 잘못 짚였나

| 단계 | 잘못 짚은 것 | 실제 |
|---|---|---|
| 2차 | BugHpBar Texture2D 누수가 주범 | 현재 씬 미사용 경로 |
| 2차 | DamagePopup 은 피격 트리거 (벌레 쪽) | 머신 피격도 호출 |
| 3차 | 코드 리뷰로 범인 특정 불가 → Editor baseline 가능성 | 런타임 덤프로 VFX 확정 가능 |
| 3차 | DamagePopup `.prefab` 참조 없음 = 미사용 | 코드 호출 경로 놓침 |
| 4.4 | AutoDestroyPS 부착 강화로 해결 | 재측정에서 악화 확인 후 풀링으로 전환 |
| 4.9 | 한글 보석 텍스트 = Mesh 누수 원인 | 측정 중 풀 성장 구간이었을 뿐, 포화 후 Δ=0 |

**공통 교훈**:
- **코드 리뷰 + 정적 분석만으로 retained 객체 특정 한계**. 런타임 덤프가 답.
- **풀 성장 구간과 누수 구분** — 풀 포화 전 측정은 신뢰 불가.
- **부착 방식은 확률적 실패 가능** — 재사용 (풀링) 이 근본.

---

## 미완료 항목 추적

1~4차에서 계획됐으나 아직 수행되지 않은 항목을 기록한다. 각 항목은 **왜 보류인가 / 언제 다시 봐야 하나** 를 명시.

### 카테고리 표기
- 🔴 **검증 필요** — 이번 세션의 "해결" 을 확정하는 데 필수
- 🟡 **정당한 보류** — 풀링 효과로 긴급도 떨어졌거나 현재 활성 경로 아님
- 🟢 **미착수** — 풀링과 무관, 별도 작업

### 🔴 검증 필요 (최우선)

| # | 항목 | 원본 계획 | 왜 미완료 | 트리거 조건 |
|---|---|---|---|---|
| V-1 | 기획자 PC 재측정 (RTX 3060) | 2차 §5, 1차 §5.1 | 개발자 PC 측정만 했음 — HierarchyDumper 가 에디터 도구라 동일 환경에서 측정 필요 | **즉시** — 풀링 효과가 원 보고 환경에서도 나타나는지 확정해야 나머지 보류 판단 유효 |
| V-2 | CSV 2종 수집 (`baseline` + `heavy_combat`) | 2차 §5 | 동일 | 기획자 PC 접근 시 |
| V-3 | Memory Profiler 스냅샷 | 2차 §6 | 환경 제약으로 skip 결정 | 위 재측정 결과 불만족 시 재평가 |

**완료 기준**: 기획자 PC 에서 `p99 < 20ms`, `max < 30ms`, `스파이크 < 10회/47s`, `GCUsed 증가 < 10MB/세션`.

### 🟡 정당한 보류 — BugController 생태계 (현재 씬 미사용)

| # | 항목 | 원본 계획 | 파일 | 재개 조건 |
|---|---|---|---|---|
| B-1 | `BugHpBar` 공유 Sprite + 풀 복귀 재활용 | 2차 A0-1 | `Bug/BugHpBar.cs`, `Bug/BugController.cs` | BugController 경로 활성화 (Formation 시스템 도입 등) |
| B-2 | `SimpleVFX` 핫패스 제거 / 풀 | 2차 A0-3 | `VFX/SimpleVFX.cs`, `Bug/BugController.cs` | 동일 |
| B-3 | `MinimapIcon` 풀 복귀 재활용 | 2차 A0-4 | `Bug/BugController.cs:ResetForPool` | 동일 |
| B-4 | `BugController.Update` `is` 캐스팅 제거 | 2차 A-3 | `Bug/BugController.cs:227` | 동일 |

**확인 방법**: BugController 가 실제 활성화됐다면 HierarchyDumper 스냅샷에 `BugController` 카운트가 0 초과. 현 시점 300개 preload 되어 있으나 전부 풀 inactive 상태.

### 🟡 정당한 보류 — 매 프레임 알로케이션

풀링 후 GC 누수가 잡혀 시간 드리프트 해소. 프레임당 단명 할당은 스파이크 빈도·크기에 간접 기여할 뿐.

| # | 항목 | 원본 계획 | 파일 | 프레임당 | 재개 조건 |
|---|---|---|---|---:|---|
| F-1 | `AimWeaponRing.RebuildMesh` verts/uvs/tris 캐시 | 3차 §3.3 | `Aim/AimWeaponRing.cs:177-247` | ~16 KB | V-1 결과 스파이크 여전 |
| F-2 | `PerfRecorder.OnGUI` GUIStyle static-cache | 3차 §3.2 | `Diagnostics/PerfRecorder.cs:427` | ~2 KB | 녹화 중 노이즈가 문제 될 때 |
| F-3 | `PerfRecorder.SampleFrame` circular buffer 전환 | 3차 §3.1 | `Diagnostics/PerfRecorder.cs:214` | ~1 MB/세션 | 동일 |
| F-4 | HUD TMP dirty 가드 (4 파일) | 2차 A-1 | `UI/HUD/TopBarHud.cs`, `MachineStatusUI.cs`, `MiningUI.cs`, `AbilityHud.cs` | ~4 KB/s | V-1 결과 스파이크 여전 |

### 🟡 정당한 보류 — 어빌리티 경로 (스킬 사용 시만 비용)

| # | 항목 | 원본 계획 | 파일 | 재개 조건 |
|---|---|---|---|---|
| A-1 | 드론/거미 `OverlapSphere` 프레임 분산 | 2차 B-1 | `Ability/Runners/DroneInstance.cs:131, 215`, `SpiderDroneInstance.cs:199, 260` | 어빌리티 중심 플레이 시나리오 측정 시 |
| A-2 | 어빌리티 Runner VFX 풀링 | (이번 세션 파생) | `Ability/Runners/DroneRunner.cs:64`, `MeteorRunner.cs:122`, `MineRunner.cs:69`, `MiningDroneRunner.cs:64`, `SpiderDroneRunner.cs:103`, `BlackHoleRunner.cs:170`, `ShockwaveRunner.cs:190`, `MeteorFireZone.cs:80`, `MeteorInstance.cs:106`, `MineInstance.cs:259` | 동일 |

### ✅ 완료 — 독립 작업 (2026-04-23 오후)

| # | 항목 | 변경 | 파일 |
|---|---|---|---|
| M-1 | URP 에셋 튜닝 | Shadow Cascade 4→2, MainLight/AdditionalLights Shadowmap 2048→1024, SSAO `m_Active` 1→0 | `Assets/Settings/PC_RPAsset.asset`, `Assets/Settings/PC_Renderer.asset` |
| M-2 | MachineGunBullet 본체 풀링 | `BulletPool` 싱글톤 도입. `MachineGunWeapon.Fire` 의 `Instantiate` → `BulletPool.Get`. `MachineGunBullet.Despawn` 의 `Destroy` → `BulletPool.Return`. `Initialize` 시그니처에 `GameObject prefabKey` 추가, `_consumed = false` 리셋. TrailRenderer 궤적 잔상 방지 Clear 호출 | `Weapon/Pool/BulletPool.cs` (신규), `Weapon/MachineGun/MachineGunBullet.cs`, `Weapon/MachineGun/MachineGunWeapon.cs` |
| M-7 | VFX 프리팹 PS duration 트리밍 | 전체 8종 프리팹 처리. `lengthInSec` 를 startLifetime 최대값 수준으로 단축 → PS 가 빈 idle 상태로 버티는 시간 제거 → `OnParticleSystemStopped` 빠르게 와서 풀 반환 가속. 실측 검증: FX_Bullet_Impact 풀 크기 **124 → 36** (71% 감소), 전체 PooledVfx **143 → 48** (66% 감소) | 고빈도 3종: `FX_Bullet_Impact`(5→1), `FX_Bullet_Muzzle`(5→1), `FX_Death_01`(5→1.5). 폭탄·레이저 5종: `FX_Grenade_Impact`(2.5→1.5), `FX_Grenade_Muzzle`(5→1), `FX_Grenade_Projectile`(5→0.5), `FX_Laser_Impact`(2.5→1), `FX_Laser_Muzzle`(2.5→1) |

**비고 — Bloom/DoF/Motion Blur/Vignette/FilmGrain**: `DefaultVolumeProfile` 에서 이미 `intensity: 0` 으로 비활성 상태 → 변경 불필요. Volume 컴포넌트는 활성(`active: 1`)이지만 intensity 0 이면 실제 셰이더 패스 영향 거의 없음. 추후 완전 제거 원하면 컴포넌트 override 해제.

**M-2 설계 노트 — BulletPool 이 VfxPool 과 다른 점**:
- **반환 트리거**: VfxPool 은 `OnParticleSystemStopped` 콜백으로 자동 반환. BulletPool 은 탄환 로직(수명/명중)이 명시적으로 `Return` 호출.
- **부착 컴포넌트 없음**: `PooledVfx` 같은 별도 컴포넌트 불필요. `MachineGunBullet` 자체에 `_prefabKey` 만 저장.
- **재사용 시 주의**: 
  - `_consumed = false` 리셋 (Despawn 가드 해제)
  - TrailRenderer 는 `SetActive` 토글 시 points 유지 → `Clear()` 명시 호출로 순간이동 선 방지

### 🟢 미착수 — 독립 작업

| # | 항목 | 원본 계획 | 대상 | 기대 효과 |
|---|---|---|---|---|
| M-3 | `Gem` 드롭 오브젝트 풀링 | — | `Pickup/Gem.cs` | 초당 소수 — 영향 작지만 완전성 측면 |
| M-4 | `WorldUiTicker` 단일 tick | 2차 C-1 | 드론/거미/벌레 개별 LateUpdate 통합 싱글턴 | 월드 UI 수십~수백 개일 때 의미 |
| M-5 | Gizmos 가드 (`#if UNITY_EDITOR`) | 2차 C-2 | `Camera/DynamicCamera.cs`, `BugSpawner.cs` 등 | Scene+Game 동시 띄운 환경 한정 |
| M-6 | 미니맵 갱신 주기 완화 | 2차 C-3 | `UI/Minimap/MinimapCamera.cs` | 매 프레임 → 0.1s 수동 Render |

### 체크리스트 — 다음 세션 시작 시

1. [ ] V-1 (기획자 PC 재측정) 수행 → 결과에 따라 F-1~F-4 우선순위 결정
2. [ ] V-1 결과 본 문서에 덧붙이기
3. [x] ~~🟢 M-1 (URP 튜닝)~~ 완료 (2026-04-23)
4. [x] ~~🟢 M-2 (MachineGunBullet 풀링)~~ 완료 (2026-04-23)
5. [ ] B-1~B-4 는 BugController 경로 활성화 시까지 동결 상태 유지

---

## 폐기된 접근 정리

### 1. `AutoDestroyPS` 프리팹 부착 방식
- 시도: 모든 VFX 프리팹 루트에 `AutoDestroyPS` 컴포넌트 박아 파티클 종료 자동 감지 → Destroy
- 실패: cullingMode 런타임 변경 신뢰성 문제, 자식 PS 조합에서 종료 감지 불완전
- 대체: VfxPool (재사용) + PooledVfx (런타임 AddComponent)
- 코드는 남겨둠: 레거시 비풀링 경로 호환용 (`SimpleBug.SpawnScaledVfx` 의 루트 PS 없는 프리팹 폴백)

### 2. 2차의 Phase A0-1 (BugHpBar 공유 Sprite)
- 현재 씬 미사용 (BugController 경로)
- 해당 경로 활성화 전엔 우선순위 하위

### 3. 2차의 Phase A0-3 (SimpleVFX.PlayBugHit)
- 현재 씬 활성 벌레는 `SimpleBug` — `_hitVfxPrefab` 할당 필수 설계라 `SimpleVFX` 폴백 경로 탐 가능성 낮음

### 4. 3차의 "SimpleBugSpawner 활성 여부" 의문
- 4차 HierarchyDumper 측정에서 SimpleBug 실존 확인 (`SimpleBug: 22→99` 증가 관측) → 스포너 정상 동작. 의문 해소.

### 5. 한글 텍스트 Mesh 누수 가설
- 재측정에서 기각. TMP 는 동일 TextMeshPro 컴포넌트 재사용 시 Mesh 도 재사용함.

---

## 참고 자료

- **원본 상세 문서**: `archive/2026-04_optimization/` 하위
  - `FrameDropInvestigation.md` — 1차 초기 조사
  - `Optimization_01.md` — 1차 계측 도구 도입
  - `Optimization_02_PlannerPCAnalysis.md` — 2차 기획자 CSV 분석
  - `Optimization_03_ActiveCodePath.md` — 3차 활성 경로 재조사
  - `Optimization_04_VfxPoolingPlan.md` — 4차 풀링 설계 (구현 전 시점 문서, 이후 실제 구현은 본 History 에 반영됨)
  - `Sys-Optimization.md` — 구 Overview (1+2차 통합본, 초심자용 설명 포함)

- **측정 데이터**: `HierarchyDumps/archive/*` (사용자 측정 원본 스냅샷)
- **현재 상태**: [Sys-Optimization.md](Sys-Optimization.md)

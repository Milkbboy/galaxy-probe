# 최적화 1차 — 프레임 드랍 조사 및 계측 도구 도입

> 기간 2026-04-22 · 트리거 RTX 3060(기획자 PC) 에디터 플레이 중 렉 보고

## 배경

기획자 PC(RTX 3060) 에서 Unity 에디터로 Game 씬 실행 시 프레임 드랍이 심하다는 보고. 벌레 수가 적은 상황에서도 렉 발생. 개발자 PC 에서는 재현되지 않아 **실측 데이터 기반 진단 체계**가 필요했다.

## 결과 요약

| 산출물 | 상태 |
|---|---|
| 프레임 드랍 원인 후보 조사 + 수정 계획 문서화 | ✅ [FrameDropInvestigation.md](FrameDropInvestigation.md) |
| `PerfRecorder` — 세션 로거 + 클립보드/Explorer 자동화 | ✅ `Assets/_Game/Scripts/Diagnostics/PerfRecorder.cs` |
| `PerfMarkers` — 의심 구간 11개 커스텀 계측 | ✅ `Assets/_Game/Scripts/Diagnostics/PerfMarkers.cs` |
| Unity 6.4 `GetInstanceID` deprecation 해소 | ✅ `SpiderDroneInstance.cs` (EntityId 전환) |
| 개발자 PC baseline 측정 | ✅ 평균 201 FPS (병목 없음) |
| 기획자 PC 실측 | ⏳ 진행 중 |
| Phase A/B/C 실제 수정 | ⏳ 기획자 데이터 분석 후 |

---

## 1. 조사

`Assets/_Game/Scripts/` 전역을 CPU 핫패스/렌더 설정/벌레·웨이브·UI/드론·어빌리티/카메라 다섯 갈래로 탐색. 상세는 [FrameDropInvestigation.md](FrameDropInvestigation.md) 참조. 핵심 재분류 결과:

**상시 비용 의심 (벌레 0 에도 돔)**
- HUD TMP 텍스트 매 프레임 재할당 (`TopBarHud`, `MachineStatusUI`, `MiningUI`, `AbilitySlotUI`)
- 월드 UI LateUpdate 러시 (`Hp3DBar`, `BugLabel`, `MinimapIcon`)

**스케일링 비용 의심**
- 드론/거미 매 프레임 `Physics.OverlapSphere` 2~3회 (거미 자동 소환 10s→3s 단축 이후 누적 가속)
- `BugController.Update` 의 `is CleaveAttack` / `is BeamAttack` 매 프레임 캐스팅

**VFX 스파이크**
- 무기 VFX 풀링 부재, 기관총 연사 시 초당 30+ GameObject Instantiate/Destroy

**URP 과설정**
- MainLight Shadowmap 2048, Cascade 4, SSAO on — 탑다운 오쏘에 과함

초기 조사에서 "무기 Update 의 `Find*`" 를 최우선으로 잡았으나 코드 재확인 결과 모두 `Start()`/`Awake()` 1회 호출로 확인되어 우선순위에서 제외.

---

## 2. 계측 체계

의심 지점을 추측에서 실측으로 전환하기 위해 두 단계 계측 체계 구축.

### 2.1 `PerfRecorder` (`Assets/_Game/Scripts/Diagnostics/PerfRecorder.cs`)

Unity 6 `ProfilerRecorder` API 기반 세션 로거. 씬 설정 불필요 (`RuntimeInitializeOnLoadMethod` 로 자동 부트스트랩).

**조작**
| 키 | 동작 |
|---|---|
| `F9` | 녹화 시작/정지 토글 |
| `F10` | 라벨 순환 (`baseline` → `wave_fighting` → `drones_active` → `heavy_combat`) |

**출력**
- `PerfLogs/{label}_{timestamp}.csv` — 채널별 avg / max / p50 / p95 / p99
- `PerfLogs/{label}_{timestamp}_spikes.csv` — `_spikeThresholdMs`(기본 33.3ms) 초과 프레임 스냅샷
- 동시에 CSV 전문을 시스템 클립보드 복사 + Windows Explorer 로 폴더 자동 오픈

**기본 캡처 채널 (16 + α, `.Valid` 자동 선별)**
- CPU: `Main Thread`, `CPU Main Thread Frame Time`, `Render Thread`
- Render: `Batches`, `SetPass`, `DrawCalls`, `Vertices`, `Triangles`, `ShadowCasters`
- Memory/GC: `GC Used/Reserved`, `GC Allocated In Frame`, `System Used`, `Total Reserved`

**HUD 위치**: 기본 BottomRight. Inspector 에서 `_overlayCorner` 로 네 모서리 선택 가능 (기획자 PC 에서 TopBarHud·미니맵과 겹치는 이슈로 결정).

**`.gitignore`**: `PerfLogs/` 제외 추가.

### 2.2 `PerfMarkers` (`Assets/_Game/Scripts/Diagnostics/PerfMarkers.cs`)

조사에서 의심한 11개 구간에 `ProfilerMarker` 직접 계측. `PerfRecorder` 가 자동 캡처.

| 마커 | 대상 파일 | 대상 구간 |
|---|---|---|
| `DrillCorp.BugController.Update` | `Bug/BugController.cs` | Update 전체 (`_isDead` early-return 이후) |
| `DrillCorp.BugLabel.LateUpdate` | `Bug/BugLabel.cs` | LateUpdate 전체 |
| `DrillCorp.Drone.Update` | `Ability/Runners/DroneInstance.cs` | Update 전체 |
| `DrillCorp.Drone.OverlapSphere` | `Ability/Runners/DroneInstance.cs` | `Physics.OverlapSphereNonAlloc` 호출만 |
| `DrillCorp.Spider.Update` | `Ability/Runners/SpiderDroneInstance.cs` | Update 전체 |
| `DrillCorp.Spider.OverlapSphere` | `Ability/Runners/SpiderDroneInstance.cs` | `Physics.OverlapSphereNonAlloc` 호출만 |
| `DrillCorp.TopBarHud.Update` | `UI/HUD/TopBarHud.cs` | Update 전체 |
| `DrillCorp.MachineStatusUI.Update` | `UI/MachineStatusUI.cs` | Update 전체 |
| `DrillCorp.MiningUI.Update` | `UI/MiningUI.cs` | Update 전체 |
| `DrillCorp.AbilityHud.Update` | `UI/HUD/AbilityHud.cs` | Update 전체 (3슬롯 Refresh 포함) |
| `DrillCorp.Hp3DBar.LateUpdate` | `UI/Hp3DBar.cs` | LateUpdate 전체 |

각 마커는 한 프레임 동안 전체 인스턴스의 누적 실행 시간(ms)을 기록 → 벌레 50마리의 Bug_Update 는 50회 호출 합계로 집계.

---

## 3. 사이드 수정

조사 중 발견한 컴파일러 경고 해소. 별도 커밋 후보.

**Unity 6.4 `GetInstanceID` → `GetEntityId` 마이그레이션** (`SpiderDroneInstance.cs`)
- Unity 공식 가이드: *"EntityId cannot be represented by a single integer, developers should avoid casting to integers"*
- `HashSet<int>` → `HashSet<EntityId>`, `int _lockedTargetInstanceId` → `EntityId _lockedTargetId`
- `c.GetInstanceID()` → `c.GetEntityId()`, `!= 0` → `!= default`
- 다른 파일에서 `GetInstanceID` 사용 0건 확인.

---

## 4. 개발자 PC baseline 측정

Game 씬 47초 녹화 (벌레·무기·드론 활성화 안 함).

| 지표 | 값 |
|---|---|
| 평균 FPS | **201.5** |
| MainThread avg | 4.95 ms |
| MainThread p95 | 7.27 ms |
| MainThread p99 | 8.92 ms |
| MainThread max | 399.02 ms (초기 hitch) |
| GC Alloc/frame avg | 22 KB |
| GC Reserved | 871 MB (안정) |

**결론**: 개발자 PC 에서는 정상 플레이 구간 병목 없음. 스파이크 3건 전부 플레이 시작 2~3초 구간에 집중 (셰이더 워밍업 / 첫 VFX 로드 / 첫 번째 프리팹 Instantiate 추정). 이후 47초 동안 33.3ms 초과 프레임 0건.

**시사점**: 기획자 PC 에서만 발생하는 렉은 GPU(섀도우·SSAO) 또는 CPU 차이에 의한 것. 하드웨어 차이 확인을 위해 기획자 PC 실측 필요.

**마커 이름 이슈**: 일부 내장 카운터(`Batches Count`, `Draw Calls Count`, `Render Thread`) 가 `.Valid` 에서 걸림. Unity 6.4 에서 내부 명칭 변경 가능성. 후속 조사 필요하지만 긴급도 낮음.

---

## 5. 남은 작업

### 5.1 기획자 PC 실측 (블로커)

`git pull` 후 Game 씬 플레이 → F9 녹화 → 실제 렉 상황 60초 재현 → F9 정지 → 클립보드에 복사된 CSV 를 채팅으로 회신. 가능하면 `baseline` 과 `heavy_combat` 두 라벨로 각각 1회.

### 5.2 데이터 기반 우선순위 재조정

수집한 CSV 에서 `MainThread` 와 11개 커스텀 마커의 p95/p99 를 대조해 [FrameDropInvestigation.md §7](FrameDropInvestigation.md) Phase A/B/C 순서를 실측 순서로 재정렬.

### 5.3 Phase A 수정 적용 (후속 커밋)

실측 결과가 의심과 일치하면 퀵윈부터 순차 적용.

- **A-1** HUD TMP dirty 가드 (`TopBarHud`, `MachineStatusUI`, `MiningUI`, `AbilitySlotUI`)
- **A-2** URP 에셋 튜닝 (`PC_RPAsset`, `PC_Renderer`, Volume profile)
- **A-3** `BugController` `is` 캐스팅 제거 (공격 전환 시점 캐싱)

각 스텝 후 같은 시나리오 재녹화 → 해당 마커 감소 확인 → 다음 스텝. 수치가 안 줄면 원인을 잘못 짚은 것이므로 재조사.

### 5.4 Phase B (실측 결과에 따라 조건부)

- **B-1** 드론/거미 OverlapSphere 프레임 분산 (0.15s 간격 + 캐시)
- **B-2** VFX/탄환 풀 (기관총 머즐·탄환·임팩트 우선)

---

## 6. 이번 작업의 커밋 단위 제안

PR 1개로 묶어도 됨. 전부 계측 · 도구 · 사이드 수정만이라 런타임 동작 변경 없음 (마커는 성능 영향 미미).

```
[계측 도입]
  +  Assets/_Game/Scripts/Diagnostics/PerfRecorder.cs
  +  Assets/_Game/Scripts/Diagnostics/PerfMarkers.cs
  M  Assets/_Game/Scripts/Bug/BugController.cs          (마커 1)
  M  Assets/_Game/Scripts/Bug/BugLabel.cs               (마커 1)
  M  Assets/_Game/Scripts/Ability/Runners/DroneInstance.cs       (마커 2)
  M  Assets/_Game/Scripts/Ability/Runners/SpiderDroneInstance.cs (마커 2 + EntityId)
  M  Assets/_Game/Scripts/UI/HUD/TopBarHud.cs           (마커 1)
  M  Assets/_Game/Scripts/UI/HUD/AbilityHud.cs          (마커 1)
  M  Assets/_Game/Scripts/UI/MachineStatusUI.cs         (마커 1)
  M  Assets/_Game/Scripts/UI/MiningUI.cs                (마커 1)
  M  Assets/_Game/Scripts/UI/Hp3DBar.cs                 (마커 1)
  M  .gitignore                                         (PerfLogs/)

[문서]
  +  docs/FrameDropInvestigation.md
  +  docs/Optimization_01.md
  M  docs/README.md                                     (인덱스)
```

## 7. 체크리스트 (다음 대화 시작 시점)

- [ ] 기획자 PC CSV 2종 수령
- [ ] `MainThread` vs 커스텀 마커 합산 diff → 미계측 영역 잔여 비용 확인
- [ ] Phase A/B/C 우선순위 재조정 결과 본 문서에 덧붙이기
- [ ] Phase A 착수 (스텝당 1 커밋, 스텝 후 재녹화 비교)

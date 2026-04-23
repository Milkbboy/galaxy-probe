# Changelog

모든 주요 변경 사항을 기록합니다.

형식: [Keep a Changelog](https://keepachangelog.com/ko/1.0.0/)

---

## [Unreleased] - 2026-04-23 — SimpleBug 전면 교체 Phase A~D + C-2 진행

> 상세: `docs/SimpleBugSheet.md` (SSoT 임시 문서), 프로젝트 메모리 `project_simplebug_migration.md`
> 맥락: 레거시 `BugData`·`BugBehaviorData`(Movement/Attack/Passive/Skill/Trigger) + `WaveManager`(SpawnGroup/Formation 기반) 시스템은 이미 `SimpleBug` 계열로 런타임 교체된 dead code 상태. 이번 작업으로 시트 연동 + 레거시 제거 일괄 진행.

### Added
- **`SpawnConfigData.cs`** (신규 SO) — Spawn/Tunnel/Area 전역 폴백값. 시트 없음, 인스펙터 직편집. `SpawnConfig.asset` × 1 생성.
- **`SimpleWaveData.cs`** (신규 SO) — 웨이브별 파라미터 오버라이드 테이블. `KillTarget` + `-1` sentinel 5개 + `bool TunnelEnabled`. Resolve 헬퍼가 SpawnConfig 폴백 해석. `Wave_01~05.asset` × 5 생성.
- **`SimpleWaveManager.cs`** — 킬 기반 전환 매니저. `GameEvents.OnBugScoreEarned` 구독, 누적 점수가 `KillTarget` 도달 시 다음 웨이브 `Configure()` 주입. `KillTarget<=0` = 전환 없음(마지막 웨이브는 세션 끝까지 유지).
- **`SimpleBugSpawner.Configure(wave, cfg)`** / **`TunnelEventManager.Configure(wave, cfg)`** — 웨이브 진입 시 런타임 파라미터 주입 API.
- **`SimpleWaveAssetSetup.cs`** (에디터 유틸) — `SpawnConfig.asset` + `Wave_01~05.asset` 자동 생성 + Game 씬에 `SimpleWaveManager` GameObject 자동 바인딩.
- **`docs/SimpleBugSheet.md`** — 시트 스키마 + SO 정의 + 런타임 흐름 + 기획자 튜닝 워크플로우 + Phase C 이후 문서 정리 계획 (Phase E에서 기존 문서로 흡수 후 삭제 예정).
- **`docs/_review/SimpleBugData.csv|tsv`, `WaveData.csv|tsv`, `README.md`** — 시트 초기 데이터 + 붙여넣기 가이드.

### Changed
- **`GoogleSheetsImporter.cs`** 전면 개편
  - `ImportSimpleBugDataAsync()` 신설: SO 내부 `BugName` 필드 매칭(파일명 오타 `SimpleBug_Elit.asset` 수용), 빈 셀 기존값 보존(`GetFloatOrKeep`/`GetIntOrKeep`), `Prefab` 필드 절대 덮어쓰지 않음, `TintHex` 파싱(`ColorUtility.TryParseHtmlString`).
  - `ImportWaveDataAsync()` 새 스키마 재작성: `WaveNumber` 매칭, `KillTarget` 포함 모든 오버라이드는 빈 셀→기존 유지, `-1` 명시 → -1 기록(런타임 Resolve에서 해석), `TunnelEnabled` bool 파싱.
  - 레거시 제거: `SHEET_BUG_DATA`/`SHEET_WAVE_SPAWN_GROUPS` 상수, `ImportBugDataAsync`, `ImportWaveDataAsync` 레거시, 11개 Behavior 헬퍼(`EnsureBehaviorFolders`/`LoadBehaviorCache`/`CreateOrUpdateBugBehaviorData`/`FindOrCreateMovement/AttackSO`/`ParseAndCreatePassives/Skills/Triggers`/`SetSOListProperty`/`SetSerializedEnumField`), `SpawnGroupData` 내부 클래스, `using DrillCorp.Bug.Behaviors.Data`.
  - `_previewTabNames` = `{ SimpleBugData, WaveData, MachineData, UpgradeData }`, `ImportAllData` 순서 `SimpleBug → Machine → Upgrade → Wave`.
- **`SawWeapon.cs`** — `BugController.ApplySlow` → `SimpleBug.ApplySlow`로 타입 교체. **기존에는 SimpleBug에 톱날 Slow가 안 걸렸던 버그 자동 수정.**
- **`DebugManager.cs`** — `KillAllBugs`가 `Bug.BugController` → `Bug.Simple.SimpleBug` 대상으로.
- **`PerfMarkers.cs` / `PerfRecorder.cs`** — `BugController_Update` marker → `SimpleBug_Update`로 이름 변경 (런타임 영향 없음, 이름 정리).

### 웨이브 전환 트리거 재설계 (시간 → 킬)
- 초기 설계는 `WaveDuration` 기반 시간 전환이었으나, **세션 종료는 채굴 완료(승리) / 머신 HP 0(패배)로만**이라는 컨셉 확인 후 v2.html 원본 로직(`waveKills>=waveKillTarget`) 이식.
- 웨이브는 난이도 곡선일 뿐 게임 종료와 무관. 마지막 웨이브(`KillTarget=-1`)는 세션 끝까지 파라미터 유지.
- 시트 값: Wave 1=15, Wave 2=25, Wave 3=40, Wave 4=60, Wave 5=-1. Score는 SimpleBugData.Score 그대로 (Normal=1, Elite=5, Swift=0.5).

### In Progress — Phase C (레거시 제거)
- ✅ C-1/C-2 완료: 비-제거 파일의 레거시 참조 끊기 (SawWeapon/DebugManager/PerfMarkers/PerfRecorder + BugController.cs 선제 수정).
- 🔜 C-3: Game 씬에서 `FormationSpawner` orphan GameObject 삭제 (사용자 수동).
- 🔜 C-4~C-6: 레거시 런타임 코드/데이터 일괄 삭제 + 최종 컴파일·Play 재검증.
- ⏳ Phase E: 문서 정리 (DataStructure.md/GoogleSheetsGuide.md 개편, Behavior/Formation 문서 archive 이동, SimpleBugSheet.md 흡수 후 삭제).

---

## [Unreleased] - 2026-04-21 (2) — Phase 7 지누스 어빌리티 (드론 포탑 / 채굴 드론 / 드론 거미)

> 근거: `docs/v2.html:1042~1053, 1057~1060, 1081~1086, 1156~1164, 1185~1214, 1296~1303, 1643~1646`
> 상세: [Phase7_JinusAbility_Plan.md](Phase7_JinusAbility_Plan.md)
> 맥락: Phase 5·6 에서 만든 어빌리티 인프라 위에 지누스 3종을 **배치형 유닛(HP·AI 자체 보유)** 으로 구현. 빅터(즉시 장판)·사라(즉시 효과) 와 성격이 다른 유닛 중심 어빌리티.

### Added — 지누스 Runner 3종

- **`DroneRunner.cs` + `DroneInstance.cs`** — 수동 배치 드론 포탑. 20초 쿨, 최대 5기. 사거리 10 내 최근접 벌레 OverlapSphereNonAlloc 조준, yaw 회전 후 0.5초 주기로 DroneBullet 발사(±5° 산포). 벌레 접촉 시 HP 감소(30 dps/마리), 0 이하면 파괴 + burst. 사거리 링(`AbilityRangeDecal.BuildRing`) + 3D HP 바(Hp3DBar).
- **`MiningDroneRunner.cs` + `MiningDroneInstance.cs`** — 수동 배치 채굴 버프. 30초 쿨, 10초 지속, 최대 1기. 매 프레임 `MachineController.AddBonusMining(5 * dt)` 호출로 초당 +5 채굴. 1초 주기로 10% 확률 `GameEvents.OnGemCollected(1)` 발행. 런타임 디스크 body(Cylinder) + 원호 타이머 + "Ns" 라벨.
- **`SpiderDroneRunner.cs` + `SpiderDroneInstance.cs`** — **AutoInterval** 10초마다 자동 소환, 최대 3기. 머신 ±2 유닛 랜덤 위치 스폰. 사거리 12 내 타겟 추격(속도 18 u/s), 멜리 반경 1.2 도달 시 정지 + 접촉 피해 3 dps. 타겟 없으면 머신 주위 선회(orbit base 6 + sin 진폭 2). HP 40 자연감쇠 0.3/s. 런타임 primitive Sphere body + 3D HP 바.

### Added — 지누스 드론 공용 부품

- **`DroneBullet.cs`** — 경량 투사체(드론 포탑 전용). `MachineGunData` 의존 없이 `Initialize(dir, speed, damage, lifetime, bugLayer)` 로 상태 주입. XZ 직진 + OverlapSphereNonAlloc 첫 명중에 `IDamageable.TakeDamage` → Destroy. 수명 만료는 폭발 VFX 없이 소멸.
- **`AbilityContext.Machine`** 필드 신규 — `MachineController` 참조 주입. 채굴 드론이 외부에서 채굴량 증가. `AbilitySlotController.BuildContext()` 에서 `FindAnyObjectByType<MachineController>()` 로 자동 해결.
- **`MachineController.AddBonusMining(float)`** 공개 API — 외부 소스가 `_miningAccumulator` 에 누적. 정수화·이벤트 발행은 기존 `Mining()` 경로 재사용 (SSoT 원칙).

### Added — 3D 월드 UI 공용 컴포넌트

- **`Hp3DBar.cs`** — primitive Cube 2개(배경+Fill) 기반 월드 공간 HP 바. `Create(target, offset, size)` + `SetHealth(ratio)` + `SetColors(full, low, threshold)`. Fill 가로 축소 왼쪽 정렬, 30% 이하 빨강 전환. 런타임 머티리얼(URP/Unlit) + `shadowCastingMode=Off`. 드론 포탑(2×0.22×0.3) / 거미(1.2×0.15×0.2) 양쪽 사용.
- **`MiningDroneTimer3D.cs`** — 채굴 드론 수명 시각화. LineRenderer 원호(12시 시작 시계방향 수축) + TextMeshPro 3D 라벨("Ns"). `SetProgress(0~1)` + `SetSeconds(int)`. `LineAlignment.View` 로 탑뷰 카메라 billboard (TransformZ 는 vertical plane 으로 서버리는 이슈 있음). v2.html:1643~1646 포팅.

### Added — 에디터 자동화

- **`Scripts/Editor/DronePrefabCreator.cs`** (메뉴 `Tools/Drill-Corp/3. 게임 초기 설정/10. 지누스 드론 프리펩 생성`) — 4개 프리펩 일괄 생성:
  - `DroneBullet.prefab` — `DroneBullet` 컴포넌트 + Polygon Arsenal `BulletGreen` 자식 VFX
  - `DroneInstance.prefab` — `DroneInstance` + Body(GlowPowerupBigGreen) + `_bulletPrefab` 자동 바인딩
  - `MiningDroneInstance.prefab` — `MiningDroneInstance` + Body(CrystalGrowthGreen)
  - `SpiderDroneInstance.prefab` — `SpiderDroneInstance` + Body(SparkleOrbGreen) · 탄 없음
  - 각 `Ability_Jinus_*.asset._vfxPrefab` 자동 할당
- **`Scripts/Editor/PolygonArsenalFbxFixer.cs`** (메뉴 `Tools/Drill-Corp/4. 서드파티 유틸/Polygon Arsenal FBX 머티리얼 경로 수정`) — Polygon Arsenal 153개 FBX 의 deprecated `MaterialLocation.External → InPrefab` 일괄 변경. `AssetDatabase.StartAssetEditing` 배치 처리. `!= InPrefab` 조건으로 obsolete enum 값 직접 참조 회피.

### Changed — AbilityData 지누스 SO 3종

- `Ability_Jinus_Drone._themeColor` → `#51cf66` (기존 사라 블루 `#4fc3f7` 수정)
- `Ability_Jinus_SpiderDrone._themeColor` → `#51cf66` (기존 하늘색 수정)
- HUD 테마 통일 — 캐릭터 컬러 일관성.

### Changed — 드론 거미 근접 공격 전환 (v2 체감 우선)

v2 원본은 탄을 쏘지만 공격적 접근 + 0거리 발사로 플레이 체감이 "달라붙어 뜯는 근접". Unity 포팅은 체감 우선 — `DroneBullet` 의존 제거, `_meleeRadius=1.2` + `_meleeDps=3` 접촉 피해로 전환. 멜리 범위 도달 시 이동 정지 (통과 방지).

### Changed — 드론 거미 타겟 예약 (AI 개선)

단순 "최근접 벌레" 선택 시 3기가 같은 벌레에 몰려 1기처럼 보이는 문제. `static HashSet<int> _claimedTargets` 공유 예약 세트 도입 — 다른 거미가 추적 중인 벌레는 skip, 미예약 없으면 fallback. 타겟 락으로 사거리 이탈/사망 전까지 재선택 안 함(thrashing 방지). `DestroySelf`/`OnDestroy` 에서 예약 해제.

### Fixed — CS0618 deprecated API 경고

- `Object.FindFirstObjectByType<T>()` → `FindAnyObjectByType<T>()` 4건 (AbilityHudSetupEditor, TopBarHudSetupEditor ×2, ResultPanelSetupEditor)
- `TextureImporter.spritesheet` 참조 제거 (AbilitySlotBorderCreator — 실제 사용 안 됐음)
- `ModelImporterMaterialLocation.External` 직접 비교 회피 (`!= InPrefab` 조건)
- 사용자 피드백 반영 — 이미 CLAUDE.md 에 경고된 사항을 Phase 5·6 에서 반복 위반한 점 `feedback_unity_deprecated_apis.md` 메모리에 영구 기록.

---

## [Unreleased] - 2026-04-21 — Phase 6 사라 어빌리티 (블랙홀 / 충격파 / 메테오)

> 근거: `docs/v2.html:994~1000, 1055, 1061~1080, 1146~1184, 1265~1295`
> 상세: [Phase6_SaraAbility_Plan.md](Phase6_SaraAbility_Plan.md)
> 맥락: Phase 5 빅터 어빌리티 인프라(`AbilityContext`, `IAbilityRunner`, `AbilitySlotController`, `AbilityRangeDecal`, `AbilityDecalMeshBuilder`) 위에 사라 3종 구현.

### Added — 사라 Runner 3종

- **`BlackHoleRunner.cs`** — 마우스 위치에 중력 존 1개 지속 생성. 반경 18 내 SimpleBug 를 중심으로 `PullSpeedPerSec=5.4f` 로 끌어당김. `InnerCutoff=0.4f` 로 딱붙기 방지. 동시 1개 제한(v2 `!bh.active` 가드). VortexPortalPurple wrapper 에 `Quaternion.Euler(90,0,0)` 로 탑뷰 평면화. 바닥 데칼은 `BuildCircle(1)` + `localScale` 스케일링 방식(Mesh 재빌드 회피).
- **`ShockwaveRunner.cs`** — 머신 중심에서 순간성(약 0.43초) 확장 링. `ExpandSpeedPerSec=84f`로 반경 36 까지 확장 → 링 통과한 SimpleBug 1회 히트 (`HashSet<Collider>` 중복 방지), `PushDistance=8f` 순간 변위 + `ApplySlow(0.5f, 3f)`. `BuildRing(0.85, 1.0)` Mesh 1회만 생성 후 `localScale` 확장. LightningWaveBlue 임팩트 VFX.
- **`MeteorRunner.cs`** — AutoInterval `_autoIntervalSec=10`. 머신 기준 반경 3~15 랜덤 착지 위치 + **XZ 오프셋 14 유닛**으로 탑뷰에서 **대각선 낙하 궤적**(~38°). `FallSpeed=12f`, `FallHeight=18f`. `CooldownNormalized = 1 - _autoTimer/AutoIntervalSec` (HUD 차오름).
- **`MeteorInstance.cs`** — 낙하체 MonoBehaviour. `startPos → targetPos` 직선 이동, `LookRotation(_fallDir)` 로 낙하 방향 바라봄(트레일 자연스러움). 도착 시 폭발 VFX(FireNovaYellow) + `MeteorFireZone` new GameObject 생성 후 자기 Destroy.
- **`MeteorFireZone.cs`** — 착지 후 15초 지속 원형 화염. 0.1s 틱마다 `OverlapSphereNonAlloc(radius=5.5)` → `IDamageable.TakeDamage(0.5)`. FloorTrapMolten 자식 PS 전부 `main.loop = true` 강제(네이팜 타일링 패턴 재사용). 바닥 데칼은 `BuildCircle(1)` + localScale 주황.

### Added — AbilityData 라이브 튜닝 훅

- **`AbilityData.cs`** — `_vfxScale` 필드 신설 (기본 1.0, VFX 크기 배율). 판정/데칼은 영향 없고 VFX 만 조절.
- **`AbilityData.OnValidated` 이벤트** — `#if UNITY_EDITOR` 로 빌드 제외. `OnValidate()` 에서 발행. Runner 가 구독해 활성 존의 판정 반경/VFX 스케일 실시간 갱신. `BlackHoleZone.ApplyLiveTuning` / `ShockwaveRing.ApplyLiveTuning` 구현.

### Added — SimpleBug 슬로우 기능

- **`SimpleBug.cs`** — `ApplySlow(strength, durationSec)` 공개 메서드 (BugController 와 동일 시그니처). `_slowStrength`/`_slowTimer` 필드 + Update 에서 이동속도 `(1 - _slowStrength)` 배율 적용.
- **슬로우 시각 표시 (전략 3: 체인 임팩트 + 틴트)** — 90마리 동시 슬로우 시 프레임 드랍 이슈 해결.
  - `_slowVfxPrefab`(ChainedFrost) 은 슬로우 시작 **순간 0.5초만** 재생 후 `Destroy(wrapper, _slowVfxDuration)` 자동 정리.
  - 지속 표시는 **머티리얼 틴트** — Renderer 배열 캐싱 + 기본 색 저장 + `_BaseColor`/`_Color` 에 `_slowTint` (`#4fc3f7 α=0.55`) Lerp. 슬로우 해제 시 `ClearSlowTint` 복원. Unity 머티리얼 인스턴스 1회만 생성.
  - `_slowVfxScale=3f` 로 벌레 localScale 상쇄 + 월드 기준 일정 크기.
- **사망/풀 반환 시 정리** — `TakeDamage` 치명타 분기에서 `ClearSlowVfxImmediate` 호출.

### Added — 에디터 자동화

- **`Scripts/Editor/MeteorPrefabCreator.cs`** (메뉴 `Tools/Drill-Corp/3. 게임 초기 설정/사라/1. 메테오 프리펩 생성`) — `MeteorInstance.prefab` 자동 생성: Body(Sphere + `MeteorBody.mat` 빨강 Unlit) + ChargeAura(AuraChargeRed, 90°X 회전) + MeteorInstance 컴포넌트. `_impactVfxPrefab=FireNovaYellow` / `_fireZoneVfxPrefab=FloorTrapMolten` 필드 자동 바인딩. `Ability_Sara_Meteor.asset._vfxPrefab` 자동 할당. **`AssetDatabase.CreateAsset` 으로 `MeteorBody.mat` 에셋 저장** — `new Material()` 만으로는 프리펩 직렬화 안 되는 이슈 해결.

### Modified — 인프라

- **`AbilitySlotController.cs`** — switch 분기 3줄 추가 (BlackHole/Shockwave/Meteor → 각 Runner 생성). Drone/MiningDrone/SpiderDrone 은 Phase 7 대기.
- **`docs/README.md`** — Phase별 구현 계획 섹션 추가 (Phase 2~6 링크).

### 주요 의사 결정

| # | 결정 | 근거 |
|---|---|---|
| 1 | 사라 먼저 (Jinus 는 Phase 7) | 기존 Runner 패턴 재사용 용이 — Jinus 는 드론 AI/발사체 추가 작업 큼 |
| 2 | 블랙홀 흡인은 `Transform.position += delta` direct 조작 | BugController 안 씀(현 프로젝트 SimpleBug 전용). 다음 프레임 이동 로직에서 자연 복귀 |
| 3 | 메테오 첫 발동은 v2 원본 그대로 (10초 대기) | UX 안정성 |
| 4 | `_vfxBaseRadius` 역방향 → `_vfxScale` 직관적 배율 | 인스펙터에서 값 키우면 커지는 게 자연스러움 |
| 5 | 슬로우 VFX 는 체인 유지 + 부하 해결 = 0.5초 임팩트 + 틴트 | "체인 연출감" 요구 + 대량 벌레 동시 적용 시 프레임 안정 양립 |
| 6 | 충격파 VFX 는 FrostNova → LightningWaveBlue | "Wave" 의미 일치 + 사라 테마 청록 |

### Deferred

- `SaraVfxBinder.cs` 통합 메뉴 (Jinus VfxBinder 와 함께 작업)
- 충격파 VFX "얼음이 팍" 스타일 재검토 (SpikeIce / MiniExploFrost / IceExplosion 후보 정리됨)
- SimpleBug 스폰 반경 조정 — 사용자 피드백(너무 멀리 스폰)
- 에디터 전용 코드 마킹 리팩토링 ([Refactor_EditorOnlyCode_Plan.md](Refactor_EditorOnlyCode_Plan.md))

---

## [Unreleased] - 2026-04-21 — 빅터 어빌리티 폴리싱 (범위 표시 / 지뢰 연출 / HUD 테두리)

> 근거: `docs/v2.html` + 인게임 피드백
> 맥락: [Phase5 Victor 어빌리티](Phase5_VictorAbility_Plan.md) 구현 이후, 인게임에서 **공격 범위가 안 보인다·지뢰가 밋밋하다·HUD 배경이 테마색으로 덮인다**는 플레이 피드백을 해결.

### Added — 공통 바닥 데칼 유틸

- **`Scripts/Ability/AbilityRangeDecal.cs`** — 어빌리티 바닥 범위 데칼 MonoBehaviour. 탑뷰(XZ 평면) 기반. 페이드인 → 펄스 → 페이드아웃 파이프라인. `SpriteRenderer`와 `MeshRenderer` 둘 다 지원. `SetupMesh(Mesh)` 호출 시 **`Sprites/Default` 셰이더 런타임 머티리얼** 생성(흰 × `_Color` 곱셈 방식이라 색 주입 안전).
- **`Scripts/Ability/AbilityDecalMeshBuilder.cs`** — XZ 평면 Mesh 빌더 3종.
  - `BuildRectangle(halfWidth, length)` — 네이팜 직사각형
  - `BuildRing(innerRadius, outerRadius)` — 지뢰 예고 링
  - `BuildCircle(radius)` / `BuildSector(halfAngleRad, range)` — 향후 원형/부채꼴 용도
  - Unity 기본 앞면 = CW (법선 방향에서) 규칙에 맞춰 탑뷰(+Y 시점) CW winding 적용. 메쉬 자체가 XZ 평면이라 `Quaternion.Euler(90,0,0)` 불필요.

### Modified — 어빌리티별 범위 시각화

- **`NapalmRunner.cs`** — `NapalmZone` 생성 시 wrapper와 동일한 회전/위치에 **직사각형 범위 데칼** 부착. `halfW × length` 크기, 주황 틴트. Dispose 시 데칼도 페이드아웃 후 자기 자신 Destroy.
- **`FlameRunner.cs`** — `SpawnVfx`에서 **부채꼴 범위 데칼** 생성(`Angle` 반각 × `Range` 길이). 머신 자식이 아닌 `VfxParent`(월드 루트)에 붙여 머신 회전·스케일 영향 제거. 매 프레임 `UpdateVfx`에서 월드 position + LookRotation 갱신.
- **`MineInstance.cs`** — 대폭 개편
  - **감지/폭발 반경 분리**: `_detectionRadius`(벌레 접촉용, 0.7m) ≠ 폭발 반경(`BombWeapon.EffectiveRadius × 0.5` = 1.5m). v2 원본의 `bug.sz+14` 접촉 감지 → 광역 AoE 폭발 흐름 복원.
  - **폭발 예고 링**: 폭발 반경 크기의 `BuildRing` Mesh. armed 전 옅은 주황(`#ff9919`) → armed 후 또렷한 빨강(`#ff3326`).
  - **armed idle pulse**: arm 완료 후에도 본체 스케일 느린 pingpong(`_armedPulseMin` 0.92, `_armedPulseSpeed` 3) 추가.
  - **중앙 점**: armed 시 활성화될 `_centerDotObject` GameObject 참조 필드. `Initialize`에서 `SetActive(false)`, `SwitchToArmed`에서 `SetActive(true)`. v2.html의 "준비 완료 = 빨간 점 점등" 연출 포팅.
  - **폭발 VFX 교체**: `MiniExploFire` → **`GrenadeExplosionRed`** (Polygon Arsenal / Combat / Explosions / Sci-Fi / Grenade). "펑!" 느낌 강화. BombWeapon VFX 공유 옵션은 의도적 탈락 — 지뢰와 폭탄 무기를 시각적으로 구분.
  - **Explode**: 링을 detach + Dispose → 잠깐 잔상으로 폭발 영역 강조.
  - **OnDrawGizmosSelected**: 감지 반경(노랑/빨강) + 폭발 반경(주황) 둘 다 표시.

### Added — 에디터 툴

- **`Scripts/Editor/AbilitySlotBorderCreator.cs`** (메뉴 `Tools/Drill-Corp/3. 게임 초기 설정/UI/어빌리티 슬롯 테두리 스프라이트 생성`) — 16×16 흰 테두리 1px + 투명 내부 PNG 생성 → `TextureImporter` 자동 세팅(Sprite Single, PPU 100, FilterMode Point, 9-slice Border (4,4,4,4)). 출력: `Assets/_Game/Prefabs/UI/AbilitySlotBorder.png`.
  - 씬의 AbilitySlotUI 3개 `_border Image`에 수동 할당 + `Image.Type = Sliced` 필요 (스크립트가 강제하지 않음)

### Modified — 에디터 툴

- **`Scripts/Editor/MinePrefabCreator.cs`** — 3가지 업데이트
  - `ExplosionPath`: `MiniExploFire` → `GrenadeExplosionRed`
  - `CenterDot` 자식 추가: `GlowPowerupSmallRed` 복제, `SetActive(false)`로 시작, `_centerDotObject` 슬롯 자동 바인딩
  - `ExplosionBaseRadius`: 2 → 1.5 (지뢰 기본 폭발 반경과 동일 → 스케일 배수=1, 원본 그대로 표시)

### Modified — UI

- **`Scripts/UI/HUD/AbilitySlotUI.cs`** — `_border.color` 알파 0.53 → **0.9** (v2 원본보다 진하게. 테마색 테두리가 또렷해짐). 스프라이트가 미할당된 씬 상태를 주석으로 명시.

### Fixed — 이번 페이즈에서 해결한 이슈

- **(네이팜/화염) 범위가 안 보임** → 바닥 데칼 Mesh 부착으로 해결. 부채꼴 Mesh winding이 초기에 뒤집혀 안 보였던 건 CW/CCW 재검증 후 `{0, i+1, i+2}`로 확정.
- **(화염) 데칼이 머신 자식이면 안 보임** → 머신의 rotation/scale을 그대로 상속받아 Mesh가 눕지 않음. `VfxParent` 루트로 이전하고 매 프레임 월드 좌표 직접 갱신.
- **(지뢰) 폭발 범위 = 감지 범위였음** → 접촉(0.7m) vs AoE(1.5m)로 분리.
- **(지뢰) 중앙 점이 흰 플래시처럼 거대함** → 초기 Mesh + URP Unlit Transparent 조합에서 셰이더 키워드 세팅 불완전으로 흰 덩어리로 렌더. 접근 방식 전환: **Polygon Arsenal `GlowPowerupSmallRed` 프리펩 재활용** + `GameObject` 토글 방식으로 교체.
- **(HUD) 어빌리티 슬롯 배경이 테마색으로 덮임** → `_border Image`가 스프라이트 미할당 + Simple 타입이라 **테두리가 아니라 풀 색 패널**로 렌더되고 있었음. 9-slice 테두리 스프라이트 에셋을 생성해 할당하는 방식으로 전환.

### Docs / Policy

- **`CLAUDE.md`** — "VFX 제작 정책" 섹션 추가. 새 VFX 만들기 전 `Assets/Polygon Arsenal/Prefabs/` 먼저 검색. 주요 카테고리 경로 명시.
- **유저 메모리**: `feedback_vfx_polygon_arsenal_first.md` — VFX 우선순위 정책 기록.

### 의사결정 로그

- **Mesh 직접 제작 vs Polygon Arsenal 재활용**: 지뢰 중앙 점에서 처음엔 `BuildCircle` Mesh로 만들었다가 흰 플래시 이슈 발생. 사용자 피드백으로 Polygon Arsenal에서 `GlowPowerupSmallRed` 찾아 교체. 이후 정책화: **파티클/글로우류는 Polygon Arsenal 먼저 검색**.
- **런타임 생성 vs 에디터 에셋**: 슬롯 테두리 스프라이트를 처음엔 `AbilitySlotUI`에서 런타임 Texture2D 생성했다가 에디터 툴 방식으로 전환. 이유: 프로젝트의 다른 UI 스프라이트(`BombLandingMarkerCircle.png` 등)가 모두 에셋 파일로 존재 — 일관성 + Unity 에디터에서 직접 튜닝 가능.

---

## [Unreleased] - 2026-04-20 (3) — 빅터 어빌리티 3종 Game 구현

> 문서: [Phase5_VictorAbility_Plan.md](Phase5_VictorAbility_Plan.md)

v2.html 의 빅터(중장비 전문가) 3 어빌리티(네이팜·화염방사기·폭발지뢰)를 3D VFX 와 함께 Game 씬에서 동작하도록 구현. 캐릭터/어빌리티 SO + Registry 는 기존 완료 상태였고 본 페이즈는 **런타임 실행 레이어 + Polygon Arsenal 프리펩 바인딩**.

### Added — Ability 런타임 스캐폴딩

- **`Scripts/Ability/AbilityContext.cs`** — Runner 초기화 참조 번들 (Machine/Aim/BombWeapon/VfxParent/BugLayer)
- **`Scripts/Ability/IAbilityRunner.cs`** — Type / Initialize / Tick / TryUse / CooldownNormalized 계약
- **`Scripts/Ability/AbilitySlotController.cs`** — Game 씬에 1개 배치. `CharacterRegistry + PlayerData.SelectedCharacterId` 로 CharacterData 해결 → `UnlockedAbilities` 필터 → 3슬롯 Runner 인스턴스화. `Keyboard.current.digit1/2/3Key` (New Input System) 입력 바인딩. 단독 실행 대비 `_characterOverride` / `_ignoreUnlockGate` 옵션

### Added — Victor 3 Runner

- **`Scripts/Ability/Runners/NapalmRunner.cs`** — 회전된 직사각형 OBB 지속 장판. `Physics.OverlapBoxNonAlloc` 0.1s 주기 틱(0.5 dmg). VFX는 OilFireRed 를 길이축으로 N개 타일링(wrapper GO + 자식 타일). FloorTrapMolten `looping=false` 문제로 인스턴스화 직후 자식 ParticleSystem 전부 `main.loop = true` 강제. v2.html:1054-1056, 1005-1041, 1554 포팅
- **`Scripts/Ability/Runners/FlameRunner.cs`** — 5초 지속 부채꼴 dps(10.8). 매 프레임 `aim.AimPosition - machine.position` XZ 방향 재계산 + VFX rotation 갱신. `Vector3.Angle` 로 `_angle`(rad→deg) 반각 필터. v2.html:1087, 1215-1241, 1749 포팅
- **`Scripts/Ability/Runners/MineRunner.cs`** + **`MineInstance.cs`** — 최대 5개 배치. armTimer 0.5s 동안 스케일 pingpong 점멸, 이후 `Physics.OverlapSphere(1.4)` 탐지. 폭발 반경/데미지는 **BombWeapon 실효값에 배율 적용** (`EffectiveRadius×0.5`, bug `×1.5`, 보스 `×2` — v2.html:1259 준수). v2.html:1092-1099, 1243-1263 포팅

### Modified

- **`Scripts/Weapon/Bomb/BombWeapon.cs`** — `EffectiveDamage` / `EffectiveRadius` **public getter 2줄** 추가. MineRunner가 폭탄 강화 반영 실효값을 읽어 씀 (SSoT 유지, 중복 계산 방지)
- **`Data/Abilities/Ability_Victor_Napalm.asset`** — `_range` 42 → 4 (v2 픽셀 → Unity 유닛 튜닝, Flame 의 180→18 비율과 일치)

### Added — 에디터 자동화

- **`Scripts/Editor/MinePrefabCreator.cs`** (메뉴 `Tools/Drill-Corp/3. 게임 초기 설정/7. 빅터 지뢰 프리펩 생성`) — `MineInstance.prefab` 자동 생성(루트 + MineInstance + Body=GlowZoneRed **90°X 회전(탑뷰 바닥 평면화)** + scale 0.5). MineInstance 의 `_bodyTransform` / `_explosionPrefab` / `_explosionPrefabBaseRadius` 필드 자동 바인딩. `Ability_Victor_Mine.asset._vfxPrefab` 자동 할당
- **`Scripts/Editor/NapalmVfxBinder.cs`** (메뉴 `.../8. 빅터 네이팜 VFX 바인딩`) — `Ability_Victor_Napalm.asset._vfxPrefab` 에 `OilFireRed.prefab` 자동 할당 (타일링 방식 채택)

### Fixed — 구현 중 발견한 이슈

- **네이팜 사이즈 과대** — SO `_range: 42` 가 v2 픽셀 단위 그대로였음 → 4 로 재튜닝
- **네이팜 VFX 3초 후 사라짐** — FloorTrapMolten 이 `looping=false` authoring. Runner 가 ParticleSystem.main.loop 강제
- **네이팜 정사각형 모양** — 단일 FloorTrapMolten + 비등방 스케일이 90°X-회전된 자식 mesh 때문에 shear 발생 → OilFireRed 타일링 N개로 전환
- **지뢰 세로로 섬** — GlowZoneRed 가 XY 평면 authoring. Body 자식에 `Euler(90, 0, 0)` 로 바닥 눕힘

### Tuning 상수 (NapalmRunner)

- `LengthToHalfWidthRatio = 10f` (length = halfW × 10. 탑뷰 프레임에 맞게 v2 20:1 에서 축소)
- `TileSpacingMultiplier = 1f` (타일 간격 = halfW = 4 유닛, 총 10 타일)
- `TileScaleMultiplier = 1f` (타일 스케일 = halfW)
- `DamageTickInterval = 0.1f` (v2 6 frame)

### Assets (준비 완료, UI 구현 시 바인딩)

- **`Sprites/UI/drillcorp_victor_abilities/{64,128,256}px/`** — 빅터 3 아이콘 PNG (1_napalm / 2_flamethrower / 3_mine), 3 해상도. HUD에는 128px 권장

### Deferred

- 인게임 어빌리티 HUD (쿨다운 바/키 표시/아이콘) — **§11 에 별도 구현 계획 작성. 아이콘 에셋은 준비 완료, `AbilityData._icon` 에 바인딩만 남음**
- Sara/Jinus 6 어빌리티 (BlackHole ~ SpiderDrone)
- BossController 실구현 후 MineInstance.IsBoss 교체 (현재 `CompareTag("Boss")` fallback)
- AbilityData 에 `_useSfx` 는 있지만 Runner가 AudioManager 호출 안 함
- AbilitySlotController 씬 배치 자동화(Setup Editor 통합)

---

## [Unreleased] - 2026-04-20 (2) — Game HUD + 세션 정산 + Range 업그레이드 v2 포팅

> 커밋 `cb5c63c`, `a0eefb1`.

v2.html 원본과 동일한 인게임 HUD·세션 정산·범위 업그레이드 체계 완성.

### Removed

- **연료 시스템 완전 제거** — `MachineController._maxFuel / _currentFuel / _fuelConsumeRate / ConsumeFuel() / AddFuel() / IsFuelEmpty`, `MachineData.MaxFuel/FuelConsumeRate`, `GameEvents.OnFuelChanged`, `MachineStatusUI` 연료바, `DebugManager` [F] 무한 연료 토글, `UpgradeType.MaxFuel/FuelEfficiency` enum 값, 각종 에디터(`UISetupEditor`/`DataSetupEditor`/`TitleSceneSetupEditor`/`GoogleSheetsImporter`) 연료 참조
- `Upgrade_MaxFuel.asset`, `Upgrade_FuelEfficiency.asset` 에셋은 이미 프로젝트에 없었음 (정리 불필요)

### Added — Session Currency & Settlement

- **`MachineController._sessionOre`** (float 내부 누적) + `SessionOre` / `SessionGems` / `SessionKills` 프로퍼티. v2 공식: `Mining()`이 tick마다 `mined × 0.5` 누적 + `OnBugScoreEarned` 구독이 `score × 0.5` 누적
- **`GameEvents.OnBugScoreEarned(float)`** — 벌레 사망 시 `SimpleBug.TakeDamage`가 `_data.Score` 발행 (세션 광석 보너스용)
- **`GameEvents.OnSessionOreChanged(int)` / `OnSessionGemsChanged(int)`** — HUD 실시간 갱신용
- **정산 로직 교체** — `MachineController.CheckSessionEnd`:
  - 승리(`IsMiningTargetReached`): `AddOre(sessionOre)` + `AddGems(sessionGems)` 전액
  - 패배(`IsDead`): `AddOre(sessionOre × 0.5)` + `AddGems(sessionGems × 0.5)` 절반
  - 나가기: 정산 없이 `LoadTitleScene` (v2 원본)
- **승리 조건 단일화** — 연료 소진 분기 제거, `IsMiningTargetReached`만

### Added — TopBarHud (v2 #ig-topbar 포팅)

- **`Scripts/UI/HUD/TopBarHud.cs`** — 상단바 통합 컴포넌트. 5 슬롯(체력/채굴/처치/광석/보석) + 나가기 버튼. MachineController·GameEvents 구독해 실시간 갱신. 체력 슬롯은 매 프레임 갱신(타이밍 이슈 대비)
- **`Scripts/Editor/TopBarHudSetupEditor.cs`** — 메뉴 `Drill-Corp/HUD/Build TopBar`. Canvas 상단 stretch 바 생성 + 아이콘(`06_gold`, `01_diamond`) 자동 바인딩. 기존 `MachineStatusUI`/`CurrencyHud` 자동 비활성화 + 미니맵 y offset `-20 → -84` + `EventSystem` 없으면 `InputSystemUIInputModule` 동반 자동 생성
- 광석/보석 슬롯은 **세션 값**만 표시 (누적 보유량 아님). 채굴 슬롯 `mined / target`은 주황 강조
- 나가기 버튼 = v2 원본과 동일: 정산 없이 Title 복귀

### Added — Result Popup (v2 #resultPanel 포팅)

- **`Scripts/UI/SessionResultUI.cs`** (통합 재작성) — Success/Failed 분리 패널 대신 단일 딤 배경 + 중앙 다이얼로그. 제목색·아이콘·부제·보상 수치 동적 전환. 승리=금색(#ffd700) + `채굴 완료!`, 패배=빨강(#ff6b6b) + `채굴 실패`
- **지연 타이밍** — 승리 500ms / 패배 200ms 후 팝업 (v2 endGame). `StartCoroutine + WaitForSeconds`
- **보상 행 아이콘** — `06_gold` + `01_diamond` 24×24. `CreateRewardRow` 헬퍼로 HubPanel TopBar와 스타일 통일
- **버튼 2개** — `업그레이드 하기` → `LoadTitleScene`, `다시 도전` → `RestartSession`
- **결과 아이콘 팩 신규** — `Assets/_Game/Sprites/UI/drillcorp_result_icons/{128,256,512}px/01_mining_success.png` / `02_mining_failure.png` (별도 팩)
- **`Scripts/Editor/ResultPanelSetupEditor.cs`** — 메뉴 `Drill-Corp/HUD/Build Result Panel`. 레거시 SuccessPanel/FailedPanel **완전 삭제** + 단일 ResultPanel 재생성. `SessionResultUI` 컴포넌트는 Canvas에 부착(루트 비활성 문제 방지) + 자식 Dialog 참조
- **핵심 함정 (학습)** — ResultPanel 루트를 SetActive(false)로 두면 같은 오브젝트의 MonoBehaviour는 `OnEnable`이 호출되지 않아 이벤트 구독 끊김. 컨트롤러는 **항상 활성인 Canvas**에 부착해야 함

### Changed — Gem System (v2 2종 보석)

- **`Gem.Create(pos, sprite, value, tint)`** — value/color 파라미터 추가. 채집 시 `OnGemCollected(_value)` 발행 (1 또는 5). **SpriteSize 0.7 → 1.2**로 확대 (사용자 피드백: 너무 작아서 안 보임). 링 반경·두께 비례 확대. 진행 링 색상도 `_tint` 기반 (엘리트는 금색 링)
- **`Gem.Collect()`** — `DataManager.AddGems(1)` **직접 호출 제거**. v2와 동일하게 세션 종료 시 일괄 정산. 팝업은 `+{value} 보석`
- **`GemDropSpawner`** — 엘리트 분기에서 `Gem.Create(pos, ..., value=5, color=#ffd700)`, 일반은 `value=1, color=#aadfff`

### Added — Range Upgrade (v2 range 업그레이드 체계)

- **`AimController.SetRangeMultiplier(float)` API 신규** — `_rangeMultiplier`, `_baseAimRadius`, `_baseCrosshairScale` 필드. `ApplyRangeMultiplier()`가 판정 반경(`_aimRadius = _base × mul`) + 크로스헤어 스프라이트 스케일 동시 적용. 모든 `AimWeaponRing`이 `AimRadius + _radiusOffset` 공식이므로 호 4개(Sniper/Bomb/Gun/Laser)가 자동 따라감 — offset이 상수라 겹침 없음
- **`SniperWeapon.RefreshEffectiveStats`** — `WeaponUpgradeStat.Range` 조회 → `_aimController.SetRangeMultiplier(rangeMul)` 호출. v2 공식 `1 + lv × 0.2` 그대로. `WeaponUpgrade_Sniper_Range.asset`은 이미 존재했으나 런타임 미연결 상태였음
- **`LaserWeapon._effectiveRadius`** 필드 추가. `RefreshEffectiveStats`에서 Range 배율을 `BeamRadius`에 곱함. `LaserBeam.Initialize` 시그니처 확장(`effectiveDamage + effectiveRadius` 오버로드 추가, 기존 오버로드는 새 버전 호출로 위임). 스코치 프리펩 크기도 확장 반경 반영
- **레이저는 저격총 에임에 종속되지 않음** — `_beamRadius=1.0` (독립) + 자체 Range 배율. v2의 `ws.laser.range=28.8`이 `ws.sniper.range=24`와 별개인 구조 재현
- **`LaserBeamWeapon.cs` 레거시 경로도 Damage/Range/Cooldown 구독 일관화** — Game 씬에선 `LaserWeapon`만 쓰이지만 혼동 방지

### Fixed

- **UpgradeType enum 순서 재배열 버그** — 연료 제거로 `MaxFuel/FuelEfficiency` 2값을 중간에서 삭제 → 뒤 값들 정수가 2씩 밀림. 기존 SO 에셋이 옛 정수값 보관해 잘못된 타입으로 매칭됨(`Upgrade_MiningTarget`=11 → 실제 enum 11은 `GemCollectSpeed`라 적용 안 됨). enum에 명시적 정수값 부여 + 에셋 4개 정수값 정정 (MiningRate 5→3, MiningTarget 11→9, GemDrop 12→10, GemSpeed 13→11). 재발 방지 주석 추가
- **HubController 목표 라벨 미갱신** — `OnUpgradePurchased` 미구독이라 업그레이드 직후 TopBar 목표량/드랍/속도 라벨 안 바뀜. 이벤트 구독 + `RefreshTargetLabel` 연결
- **DataManager.StoreSessionResult 이중 적립** — 내부 `AddOre/AddGems` 제거. 광석=MachineController, 보석=세션 누적에서 각각 적립하도록 책임 분리. StoreSessionResult는 기록/통계 전용

### Changed — Title 초기 화면

- **HubPanel 기본 진입** — `TitleUI.Start()`가 `ShowMainPanel` 대신 `ShowHubPanel` 호출. Hub가 메인 화면, MainPanel은 폴백으로만 남김
- **Hub TopBar 재편** — `BackButton` 제거(대응 MainPanel 없음), `OptionsButton` / `QuitButton` 신규. `OptionsUI` 뒤로가기는 `ShowHubPanel`로 복귀
- **HubPanel 첫 프레임 겹침 버그 방지** — `HubController.OnEnable`에서 `StartCoroutine(ForceRebuildNextFrame)` → `LayoutRebuilder.ForceRebuildLayoutImmediate` 호출. CSF+VLG 중첩으로 CharacterSelectSubPanel이 TopBar 덮는 현상 방지
- **CharacterSelectSubPanel 고정 높이 180** — `V2HubCanvasSetupEditor`에서 `forcedHeight` 부여

### Changed — DebugManager 위치

- 디버그 단축키 OnGUI 창을 **우측 상단 → 우측 하단**으로 이동 (`Screen.height - height - 10f`). 높이 150 → 130 (연료 토글 항목 제거로 축소)

---

## [Unreleased] - 2026-04-20 — 문서 일괄 갱신

최근 v2 작업(Hub→Game 연결, 보석 드랍/채집)을 모든 핵심 문서에 반영.

### Docs

- `README.md` — 헤더 v2 진행 상황 갱신, WeaponSystem 참조를 v2(`WeaponUnlockUpgradeSystem.md`)와 레거시 두 줄로 분리
- `Architecture.md` — Core 계층에 `UpgradeManager`/`WeaponUpgradeManager`/`CharacterRegistry` 명시, InGame에 `Pickup`(Gem/GemDropSpawner)·`CurrencyHud`(MiningUI/GemCounterUI)·`AimRingBinder` 추가, GameEvents 시그니처 v2 반영, §6.1 Pickup 시스템 신규
- `DataStructure.md` — BugData `_isElite`, MachineData `BaseMiningTarget` 필드 추가. UpgradeData v2 6종 (`mine_speed`/`mine_target`/`excavator_hp`/`excavator_armor`/`gem_drop`/`gem_speed`) 비용 schedule 명시. CharacterData/WeaponUpgradeData/AbilityData 계층 추가
- `WeaponUnlockUpgradeSystem.md §11` — 인게임 적용 현황 신규: 해금 필터(`TryDisableIfLocked`), AimRingBinder 자동 숨김, 4종 강화(Saw 패턴), `EffectiveFireDelay` virtual, 투사체 effective 오버로드, 파일 매핑 표
- `CharacterAbilitySystem.md §2.5` — `CharacterRegistry` 싱글턴 + `MachineController.ApplySelectedCharacter()` 흐름

---

## [Unreleased] - 2026-04-19 (4) — 보석 드랍/채집 + 인게임 재화 HUD

### Added

- **`Scripts/Pickup/Gem.cs`** — 월드 보석 오브젝트. `Create(pos, sprite)` 팩토리로 프로그램 스폰 (프리펩 불필요). SpriteRenderer(`01_diamond.png`, X=90° 회전해 지면에 누움) + 자식 LineRenderer 진행 링. XZ 거리 체크로 마우스 호버 2초(gem_speed 보정) → 채집 시 `DataManager.AddGems(1)` + 팝업 + 자파괴
- **`Scripts/Pickup/GemDropSpawner.cs`** — 씬 싱글턴. `GameEvents.OnBugDied` 구독, 일반 5%+`gem_drop` 강화 / 엘리트 100% 드랍. `_gemSprite` 인스펙터 필드 (HUD 에디터가 `01_diamond` 자동 바인딩)
- **`Scripts/UI/GemCounterUI.cs`** — 세션 보석 HUD 카운터. `OnGemCollected` 구독, MiningUI의 펀치 애니 동일
- **`Scripts/Editor/InGameCurrencyHudSetupEditor.cs`** — `Tools > Drill-Corp > 3. 게임 초기 설정 > 3. 광석·보석 HUD 추가` 메뉴. 우상단에 `[05_iron]광석` + `[01_diamond]보석` 2행 + `GemDropSpawner` GameObject 자동 생성/스프라이트 바인딩. 재실행 시 기존 중복 컴포넌트 제거 (idempotent)
- **`GameEvents.OnBugDied(Vector3, bool)`** — 벌레 사망 위치 + 엘리트 여부. 드랍 스포너 전용 (기존 `OnBugKilled(int)`는 WaveManager/AudioManager가 쓰므로 유지)
- **`GameEvents.OnGemCollected(int)`** — 채집 1회당 invoke, HUD 누적용
- **`BugData._isElite`** bool 필드 — 기본 false, 엘리트 지정 SO 예정

### Changed

- **`BugController.Die()`** — `OnBugKilled` 직후 `OnBugDied(transform.position, _bugData.IsElite)` 동반 발사
- **`SessionResultUI`** — 세션 중 `_sessionGems` 누적 → Success/Failed 패널에 `"채굴량: N / 보석: N"` 병기. 실패 패널엔 `"광석 획득 불가 / 보석: N 획득"`으로 즉시 적립 정책 명시
- **인게임 HUD 배치** — 좌상단(미니맵과 겹침) → **우상단 (-20, -20)**
- **광석 HUD 아이콘** — `06_gold.png` → `05_iron.png` (v2 팩 "철광석", 범용 광석 느낌). `05_iron.png.meta`의 `spriteMode: 2(Multiple)` → `1(Single)` 수정해 `LoadAssetAtPath<Sprite>` 정상화

### Policy

- 보석 적립은 **즉시** (`DataManager.AddGems(1)`) — 세션 실패해도 유지. 2초 호버 채집 노력의 UX 보전. `SessionResult.GemGained`는 표시 집계용으로만 사용

### Docs

- `V2_IntegrationPlan.md §8` — 보석 드랍/채집 ✅, Hub→Game 연결 서브테이블 추가
- `GemMiningSystem.md §10` — 구현 현황 섹션 신규, 초안 설계와의 차이 표

---

## [Unreleased] - 2026-04-19 (3) — Hub 강화·해금·캐릭터를 Game에 반영

> 커밋 `7c5e37e`.

### Added

- **`Scripts/OutGame/CharacterRegistry.cs`** (신규 싱글턴) — 3 캐릭터 SO 중앙 등록소. `Find(characterId)`로 Game 씬에서 `DefaultMachine` 조회. `V2HubCanvasSetupEditor.EnsureCharacterRegistry()`가 자동 생성/할당
- **`WeaponBase.TryDisableIfLocked()`** — 미해금 무기 GameObject를 `SetActive(false)`. 5종 무기 `Start()` 첫 줄에서 가드
- **`WeaponBase.EffectiveFireDelay` virtual** — Cooldown 강화가 `_baseData.FireDelay`에 자동 반영. `TryFire()`/`CooldownProgress`가 이 값을 사용
- **`MachineController.MiningTarget` / `IsMiningTargetReached`** 게터 — mineTarget 승리 조건 작업을 위한 사전 노출

### Changed

- **`MachineController.Awake`** — `ApplySelectedCharacter()` (CharacterRegistry → DefaultMachine 주입) + `ApplyUpgradeBonuses()` (MaxHealth/MiningRate/MiningTarget/Armor 누적) 추가. v2 armor(받는 피해 감소율 0~1)는 기존 머신 armor와 **별도 누적** (legacy `armor/(armor+100)` 커브 유지)
- **`SniperWeapon` / `BombWeapon` / `MachineGunWeapon` / `LaserWeapon`** — Saw 패턴 복사. `RefreshEffectiveStats()` + `GameEvents.OnWeaponUpgraded` 구독. 투사체(`BombProjectile`/`MachineGunBullet`/`LaserBeam`) `Initialize`에 `effectiveDamage`/`effectiveRadius` 오버로드 추가해 강화 값 전달
- **AimRingBinder 4종** (Sniper/Bomb/Gun/Laser) — Update 첫 프레임에 `_weapon == null || !activeInHierarchy`면 바인더 GameObject 자체 비활성 → 호 영구 숨김
- **무기 SO `_weaponId` 채움** — `Weapon_Sniper(sniper)` / `Weapon_Bomb(bomb)` / `Weapon_MachineGun(gun)` / `Weapon_LaserBeam(laser)`. Sniper는 `_unlockedByDefault=1`

### Removed

- 레거시 무기 SO 정리 — `Weapon_BurstGun` / `Weapon_Laser`(오래된 LaserBeamData) / `Weapon_LockOn` / `Weapon_Shotgun` + meta
- `Assets/Polygon Arsenal/Upgrades/...meta` 잔존물 제거

---

## [Unreleased] - 2026-04-19 (2)

### Added — 회전톱날 무기 (v2 §saw 포팅)

- **`SawWeaponData` / `SawWeapon`** (`Scripts/Weapon/Saw/`) — 머신 중심에서 마우스 방향 궤도(`OrbitRadius=7.2`) 위에 떠있으며 블레이드 반경(`BladeRadius=1.8`) 내 Bug에 **0.1초 tick 데미지(0.15) + 슬로우(30%/2s)**. v2.html `tickSaw`/`drawSawPipe` 파라미터 그대로 이식
- **`SpinSpeed=24 rad/sec`** — v2.html 1109줄 `sawAngle += spinSpeed * dt * 5` 의 `×5` 배율 반영. 문서 초안 `4.8 rad/sec`은 배율을 놓친 값이므로 정정 ([WeaponUnlockUpgradeSystem.md §7.2](WeaponUnlockUpgradeSystem.md) 경고 참조)
- **`BugController.ApplySlow(strength, duration)`** — `_slowStrength`·`_slowTimer` + `MoveSpeed` getter에 `(1 - _slowStrength)` 곱. 더 강한 슬로우만 덮어쓰고 지속시간은 max. 충격파 어빌리티도 재사용 예정
- **`SawBladePrefabBuilder.cs`** — 10톱니+허브+볼트 **절차적 메시 자동 생성** (`#77ee77` teeth / `#666` hub / `#bbb` bolt, v2 색상). URP Lit 머티리얼 3종 + Mesh 에셋 3종 + 프리펩 1종 생성 → `Weapon_Saw.asset._bladeVisualPrefab` 자동 바인딩
- **`WeaponPanelSawSetup.cs`** — Game 씬에 `SawWeapon` GameObject 생성 + `WeaponSlot_Sniper.prefab` 복제→`WeaponSlot_Saw` 리네임 + `WeaponPanelUI._slots/._weapons` 배열 확장
- **★ Saw 풀셋업 원클릭 메뉴** — 에셋 생성 → 프리펩 빌드 → 씬 바인딩 3단계 자동화 (`Tools > Drill-Corp > Weapons > ★ Saw 풀셋업`)
- **`V2DataSetupEditor.CreateSawWeaponDataMenu`** — `Weapon_Saw.asset` 생성 (req=Laser, 40💎, 보라 `#e03ff8`)
- **`Sprites/UI/09_saw.png`** — 슬롯 아이콘

### Changed — v2 동시 발동 아키텍처 전환

v2.html은 해금된 모든 무기가 매 프레임 병렬 발동 (`tickSniper/tickBomb/tickGun/tickLaser/tickSaw`). Unity 단일-장착 모델을 이 패턴에 정렬:

- **`AimController` 단순화** — `_currentWeapon`·`_initialWeapon`·`EquipWeapon(WeaponBase)`·`CurrentWeapon`·`CooldownProgress`·`IsReady` 제거. 에임 위치·범위·머신 참조·`BugsInRange` 제공만 담당. `SuppressAimBugDetection` 분기 제거 (항상 범위 내 벌레 수집)
- **`SniperWeapon` self-driven 전환** — Bomb·Laser·Gun이 이미 쓰던 `Update() → TryFire(_aimController)` 패턴 동일 적용. `_startDelay=0.3s`로 씬 로딩 직후 즉발 방지
- **`SawWeapon` self-driven** — 동일 패턴. `OnEquip/OnUnequip` 오버라이드 제거, 블레이드 시각은 `Start`에서 스폰 / `OnDestroy`에서 정리

### Removed

- **`WeaponSwitcher.cs`** — 단일 무기 슬롯 교체(디버그용 1~4키) 폐기. 모든 무기 동시 발동이라 슬롯 개념 불필요
- **`AimChargeUI.cs`** — 단일 무기 쿨 표시 UI. 각 무기의 `AimWeaponRing`·게이지로 대체됨

### Docs

- `V2_IntegrationPlan.md §8` — 회전톱날 ✅, 동시 발동 전환 기록
- `WeaponUnlockUpgradeSystem.md §7` — 구현 완료, SpinSpeed 정정, 파일 매핑 추가
- `WeaponSystem.md` — Phase 3 아카이브 배너 (현행은 WeaponUnlockUpgradeSystem.md)
- `Architecture.md` — 무기 시스템 5종 + self-driven 패턴 반영

---

## [Unreleased] - 2026-04-19

### Changed — 무기 SFX 라운드로빈 + 3D 프로젝타일 방향 수정

- **AudioManager**: 무기 4종(`_sfxMachineGunFire` / `_sfxSniperFire` / `_sfxBombLaunch` / `_sfxBombExplosion`) 필드를 `AudioClip` → `AudioClip[]` 배열로 변경. `PickVariant(clips, ref idx)`로 매 발사마다 다음 변주 재생 (라운드로빈, 무기별 독립 인덱스). 레이저 빔은 loop 재생이라 단일 유지
- **Game.unity**: 각 배열에 Sci-Fi 팩 5변주씩 바인딩 — `Rifle_Shoot_1~5` / `Pistol_Shoot_Big_Size_1~5` / `Cannon_Shoot1_Electrified_1~5` / `ARCADE_Retro_Explosion_Big_1~5` / `Electric_Weapon_Fire_Loop_1` (레이저)
- **3D 프로젝타일 회전**: `MachineGunWeapon.cs:148` / `BombWeapon.cs:102` — `Instantiate(..., prefab.transform.rotation)` → `Quaternion.LookRotation(dir, Vector3.up)`. 2D 스프라이트 시절 "프리펩 회전 보존" 주석이 3D 전환 후에도 그대로 남아 탄이 발사 방향을 안 보던 문제 수정

### Added — 벌레 피격/사망 VFX 분리

- **`BugController._hitVfxPrefab` 필드** — 벌레별 커스텀 피격 VFX. 비어있으면 기존 `SimpleVFX.PlayBugHit` 폴백
- **`SimpleBug._hitVfxPrefab` / `_deathVfxPrefab` 필드** — 레거시 SimpleBug는 VFX 훅 자체가 없었음. `TakeDamage` / 사망 시점에 각각 재생
- **벌레 프리펩 27개 바인딩** — Bug_Beetle/Centipede/Fly + Bug_Test_* 21개 + SimpleBug 3개에 `FX_Bullet_Impact`(피격)·`FX_Death_01`(사망) 설정
- **VFX 크기 벌레 스케일 연동** — `_vfxScaleMultiplier` 필드(default 2). `SpawnScaledVfx`에서 `vfx.localScale = prefab authored × bug.transform.localScale × multiplier`. `Instantiate(prefab)` 후 position만 설정해 authored 회전 보존 (기존 `Quaternion.identity`가 -90 X 회전을 덮어쓰던 문제 해결)
- **SimpleBug 프리펩에 FX_Socket 자식 추가** — SimpleBug_Normal/Swift/Elite에 빈 Transform 자식 `FX_Socket`을 YAML로 삽입 + `_fxSocket` 필드 자동 바인딩. 디자이너가 Inspector에서 VFX 스폰 위치를 조정 가능

### Removed — 무기측 히트 VFX 해제

- `Weapon_Bomb` / `BurstGun` / `Laser` / `LaserBeam` / `LockOn` / `MachineGun` / `Shotgun` / `Sniper`.asset의 `_hitVfxPrefab` → `{fileID: 0}`. 기존에 무기→벌레 충돌 시 `FX_Death_01 Variant`가 터져 벌레 사망 VFX와 시각적으로 혼동되던 문제 제거. 이제 피격 VFX는 벌레측 단일 훅에서만 재생

### Fixed
- 치명타 시 피격 VFX + 사망 VFX가 같은 프레임에 이중 출력되던 버그 — `BugController.TakeDamage` / `SimpleBug.TakeDamage`에서 HP 확인 후 **둘 중 하나만** 재생하도록 분기

---

## [Unreleased] - 2026-04-18

### Added — v2 Hub UI 통합 완료
- **HubPanel** (Title 씬): 5개 서브패널 + TopBar 자동 생성 에디터
  - `Scripts/Editor/V2HubCanvasSetupEditor.cs`: `Tools → Drill-Corp → 3. 게임 초기 설정 → Title → 3. v2 Hub Canvas 추가` 메뉴로 캔버스·SO 연결까지 자동화. `EnsureUpgradeManagerLinks()`가 `Assets/_Game/Data/Upgrades` 폴더의 모든 SO를 매니저에 자동 연결
  - `Scripts/Editor/V2DataSetupEditor.cs`: 3 캐릭터·9 어빌리티·15 무기 강화 SO를 한 번에 생성
  - 좌·중·우 3컬럼 마소너리(1:1:1, `flexibleWidth: 1` + `preferredWidth: 0`로 컬럼 균등 분배)

- **5개 Sub-Panel UI** (`Scripts/OutGame/`)
  - `HubController.cs`: TopBar 광석/보석 표시 + 치트(+1000)/초기화(2단계 확인)/뒤로/채굴 시작
  - `CharacterSelectUI.cs`: 빅터/사라/지누스 카드 클릭 선택, 첫 OnEnable 시 `LayoutRebuilder.ForceRebuildLayoutImmediate`로 중첩 CSF 1프레임 지연 회피
  - `WeaponShopUI.cs`: 5종 무기 카드(2열 마소너리 Col1/Col2). **patch-pattern**: 첫 OnEnable에 1회 빌드, 이후 이벤트(`OnOreChanged`/`OnGemsChanged`/`OnWeaponUnlocked`/`OnWeaponUpgraded`)는 텍스트·색상·interactable만 패치. 잠김↔해금 전환 시에만 카드 Body 재구성
  - `AbilityShopUI.cs`: 선택 캐릭터의 3 어빌리티만 필터 표시. `OnCharacterSelected` 시 옛 카드 `SetActive(false)` 후 Destroy → 같은 프레임 새 카드와 안 섞이게
  - `ExcavatorUpgradeUI.cs`: 굴착기 강화 4종(`UpgradeType.MaxHealth/Armor/MiningRate/MiningTarget`) 단행 카드, Name·Effect·Lv·Cost 4컬럼
  - `GemUpgradeUI.cs`: 보석 채집 2종(`GemDropRate/GemCollectSpeed`) 동일 패턴
  - `StatDisplayUI.cs`: 9행 실시간 합산(광석·보석·캐릭터·최대체력·받는피해·채굴속도·목표채굴량·보석출현확률·채집속도)

- **데이터 모델 확장**
  - `Scripts/Core/DataManager.cs`: `Ore`/`Gems`/`SelectedCharacterId`/`UnlockedWeapons`/`UnlockedAbilities`/`LastSessionResult` 추가, `SpendOre`/`SpendGems`/`TryUnlockWeapon`/`TryUnlockAbility`/`SelectCharacter` API
  - `Scripts/Core/GameEvents.cs`: `OnOreChanged`/`OnGemsChanged`/`OnWeaponUpgraded`/`OnWeaponUnlocked`/`OnAbilityUnlocked`/`OnCharacterSelected`
  - `Scripts/Data/CharacterData.cs`, `AbilityData.cs`, `WeaponUpgradeData.cs` (신규)
  - `Scripts/OutGame/WeaponUpgradeManager.cs`: 무기별 누적 보너스(`GetBonus(weaponId, stat)`) — PlayerPrefs 영속
  - `Scripts/Data/UpgradeData.cs`: `_oreCostSchedule`/`_gemCostSchedule` 명시 배열 추가 (v2 핸드튜닝 비용)
  - `Scripts/OutGame/UpgradeManager.cs`: `TryUpgrade`/`CanUpgrade` 이중 재화(광석+보석) 처리, 보석 차감 실패 시 광석 환불

### Changed — Upgrade SO를 v2.html 스펙으로 정렬
- `Upgrade_MiningRate.asset`: ID `mining_rate` → **`mine_speed`**, schedule `[80,160,280,440,640]`광석, 효과 `초당 +2`
- `Upgrade_MiningTarget.asset`: schedule `[100,200,350,550,800]`광석, 효과 `목표량 +50`
- `Upgrade_MaxHealth.asset`: ID `max_health` → **`excavator_hp`**, schedule `[60,130,230,370,540]`광석, 효과 `+30 HP`
- `Upgrade_Armor.asset`: ID `armor` → **`excavator_armor`**, maxLv 10→3, schedule `[150,300,500]`광석, 효과 `-15% 받는 피해`
- `Upgrade_GemDrop.asset`: schedule `[15,30,50,75,105]`보석, 효과 `+2% 등장 확률`
- `Upgrade_GemSpeed.asset`: schedule `[10,22,38,58,82]`보석, 효과 `+20% 채집 속도`

### Removed — v2.html에 없는 옛 강화 SO
- `Upgrade_AttackDamage.asset`, `Upgrade_AttackSpeed.asset`, `Upgrade_FuelEfficiency.asset` (무기별 강화는 `WeaponUpgradeData`로 분리됨)
- `Scripts/Editor/DataSetupEditor.cs`의 위 3종 생성 블록 제거

### Fixed
- 중첩 ContentSizeFitter 1프레임 지연 → `AddVerticalItemContainer`의 VLG `childControlHeight = true`로 변경, 1프레임 내 안정 수렴
- `WeaponShopUI` 전체 destroy/recreate로 인한 레이아웃 폭주 → patch-pattern 도입
- `AbilityShopUI` 캐릭터 전환 시 옛 카드와 새 카드 6장 동시 존재 → `SetActive(false)` 즉시 레이아웃 제외
- `StatDisplayUI` 베이스값 더블카운트 (예: HP 230 = 100 + 100 + 30) → SO `_baseValue=0` 통일, UI에서만 베이스 추가

---

## [Unreleased] - 2026-04-17

### Added
- **사운드 시스템 (Phase 5)**: `AudioManager` 싱글톤 + 외부 WAV/OGG 기반 SFX 재생
  - `Assets/_Game/Scripts/Audio/AudioManager.cs`: 8종 SFX 클립 필드 + 클립별 볼륨 슬라이더(0~2, 부스트 가능) + 디버그 Enable 토글(단독 테스트)
  - AudioSource 구조: 공용 풀(8개, PlayOneShot 라운드로빈) + 기관총 전용(Stop→Play 연사 겹침 방지) + 레이저 전용(`loop=true`, 빔 수명 동안 지속)
  - `GameEvents` 자동 구독: `OnBugKilled` → BugDeath, `OnMachineDamaged` → MachineDamaged(150ms 쓰로틀)
  - `PlayOneShot` 동일 클립 30ms 가드 — 한 프레임 다중 호출 시 겹침 방지
  - 트리거 통합: `MachineGunWeapon`, `SniperWeapon`, `BombWeapon`, `BombProjectile`, `LaserWeapon`(StartLaserBeamLoop), `LaserBeam.OnDestroy`(StopLaserBeam), `BugBase`/`BugController`/`SimpleBug`의 `TakeDamage`
  - `SimpleBug.TakeDamage`에 `PlayBugHit` + `GameEvents.OnBugKilled` 훅 추가 (프로토 스타일 벌레가 이벤트를 발행하지 않아 사운드가 나지 않던 버그 수정)
  - `OptionsUI` SFX 슬라이더 → `AudioManager.SetSfxVolume` 실연결
  - 상세: `docs/SoundSystem.md`

- **AudioTrimWindow 에디터 툴**: 파형 뷰어 + 드래그 핸들로 AudioClip 구간 편집
  - `Tools → Drill-Corp → Audio → Trim AudioClip`
  - `AudioClip.GetData` 기반 peak-per-column 렌더링을 `Texture2D`에 직접 픽셀 기록 (내부 API 미사용)
  - 드래그 핸들 4종: Start / End / FadeIn / FadeOut — 숫자 필드와 양방향 바인딩
  - Zoom: `View Start/End` 숫자 + `Fit All` / `Fit Sel` 버튼
  - Preview: 원본 에셋을 Start 위치부터 재생, (End-Start)초 후 자동 Stop (Unity 6의 `PlayPreviewClip` 런타임 클립 무음 이슈 우회)
  - Save: Trim + 선형 페이드 + forceMono 다운믹스 → 16-bit PCM WAV (`_Short` 접미사)
  - Preset 버튼: "첫 0.3s (MachineGun 1-shot)" 원클릭 세팅

### Removed
- **SfxSynth.cs** 제거 — 프로토(_.html) Web Audio API 합성 방식을 이식했으나, 외부 파일 기반이 실무에서 더 유연하다고 판단. 관련 `_useSynthGunShot`/`_synthGunShotClip` 필드·분기 정리.

## [Unreleased] - 2026-04-16

### Added
- **기관총 무기 (Phase 3 + 3.5)**: 자동 연사 + 산포 + 탄창/리로딩 + 탄창 pip 행 UI
  - `MachineGunData` (SO): MaxAmmo 40, ReloadDuration 5s, FireDelay 0.14s, BulletSpeed 9, SpreadAngle ±3.4°
  - `MachineGunWeapon`: WeaponBase 파생, **자체 Update 루프** (폭탄과 동일 보조 무기 패턴 — 메인 무기 슬롯과 병렬). `_firePoint` 옵션 필드로 Turret 배럴 끝에서 `_firePoint.forward` 방향 발사 (배럴 회전과 시각 일치). 적 유무 무관 — 마우스 방향으로 계속 연사
  - `MachineGunBullet`: 자립형 투사체, XZ 직진, 매 프레임 OverlapSphere 충돌, 첫 명중 1마리에만 데미지 (관통 없음)
  - `MachineGunAimRingBinder`: 에임 호에 `BarFillAmount` + `BarColor` 푸시 — 평상시 파랑(탄 잔량), 리로딩 빨강(차오름)
  - `MachineGunPrefabCreator` (에디터 도구): `Tools → Drill-Corp → 3. 게임 초기 설정 → 7. 기관총 자산 일괄 생성` 메뉴로 스프라이트 + 프리펩 + 트레일 머티리얼 + SO 자동 생성
  - **탄창 pip 행 (Phase 3.5)**:
    - `WeaponBase`에 `ShowAmmoRow`/`AmmoCurrent`/`AmmoMax` 가상 프로퍼티 추가
    - `WeaponSlotUI`에 동적 pip 풀 — 슬롯 하단 가는 행에 40개 작은 사각형, 발사 시 오른쪽부터 회색으로 전환
    - 슬롯 높이 90→100 (`BuildDefaultHierarchy` 재실행 필요)
  - 상세 문서: `docs/Phase3_MachineGun_Plan.md`

- **폭탄 무기 (Phase 2)**: 좌클릭 수동 발사 + 투사체 비행 + AoE 폭발
  - `BombData` (SO): `_instant` 토글로 비행 모드/즉시 폭발 모드 전환
  - `BombWeapon`: `WeaponBase` 파생, **자체 Update 루프** — 메인 무기 슬롯과 무관하게 항상 활성 (프로토타입의 `canvas.click → fireBomb()`와 동일)
  - `BombProjectile`: 자립형 투사체 (스폰 시 데이터 캡처, 무기 전환 후에도 동작), `Detonate(pos, data, layer)` static 헬퍼
  - `BombLandingMarker`: 비행 중 클릭 위치에 반투명 주황 원 표시 (펄스 알파)
  - `BombExplosionFx`: 스프라이트 기반 폭발 burst (scale ease-out + 알파 페이드 + 자동 파괴) + TrailRenderer 비행 잔상
  - `BombAimRingBinder`: 크로스헤어 호에 `_weapon.BarColor` 푸시 — 인스펙터 색 무시
  - `BombPrefabCreator` (에디터 도구): `Tools → Drill-Corp → 3. 게임 초기 설정 → 6. 폭탄 자산 일괄 생성` 메뉴 한 번으로 스프라이트 3종(투사체/마커/폭발) + 프리펩 3종 + 트레일 머티리얼 + SO 자동 생성
  - `WeaponSlotUI` 확장: `_coolOverlay` + `_overlayText` 자식 (검은 반투명 + 큰 초) — 쿨다운 중 표시. `Build Default Hierarchy`에 자동 생성 로직 포함
  - 상세 문서: `docs/Phase2_Bomb_Plan.md`

### Fixed
- **`AimController.EnsureInfoLabel` 호출 순서** — `Awake → Start`로 이동. `TMPFontHolder.Awake()`보다 먼저 실행되면 D2Coding 폰트 미초기화 상태에서 라벨이 만들어져 한글 미지원 LiberationSans로 fallback (\uD074 등 미지원 경고). Start는 모든 Awake 후에 실행되므로 폰트 초기화 보장.
- **`SniperWeapon.ThemeColor`** 중복 정의 제거 — `WeaponBase`가 이미 동일 프로퍼티 제공 (CS0108 경고).

## [Unreleased] - 2026-04-15

### Added
- **미니맵 시스템 (Phase 4)**: 화면 좌상단 실시간 미니맵
  - `MinimapCamera`: 머신 상공에서 -Y를 내려다보는 Orthographic 세컨드 카메라 + RenderTexture 출력
  - `MinimapUI`: RawImage에 RenderTexture 바인딩
  - `MinimapIcon`: 월드 루트에 생성되어 target을 따라다니는 아이콘 (자식이 아니라 월드 루트 — 부모 회전/스케일 영향 차단, `BugHpBar` 패턴)
  - 머신(파랑 사각형) + 벌레(빨강 원) 자동 표시
  - 메시/머티리얼/셰이더 static 캐싱으로 수백 마리 동시 표시 지원 (SRP Batcher 친화적)
  - 풀링 대응: `BugController.OnEnable/OnDisable/OnDestroy`에서 아이콘 활성/파괴 동기화
  - 디버그 UI(DebugManager, DebugCameraUI) 우상단으로 이동하여 미니맵 자리 확보
  - 상세 문서: `docs/MinimapSystem.md`

## [Unreleased] - 2026-04-13

### Added
- **카메라 시스템 (Nuclear Throne 방식)**: 마우스 쪽으로 카메라가 따라가는 방식
  - `CameraSettingsData` ScriptableObject: 파라미터 관리 ([Range] 속성)
  - `DynamicCamera`: Position Lerp 기반 카메라 제어
  - `DebugCameraUI`: F1 토글 디버그 UI (런타임 슬라이더, Save to Asset)
  - Gizmo 시각화 (MaxOffset 범위, 현재 타겟 위치)
  - 상세 문서: `docs/CameraSystem.md`

- **무기 시스템 (Phase 3)**: 오퍼레이터 주무기 4종 구현
  - `WeaponBase` + `WeaponData` 추상 베이스
  - `BulletPool` + `Bullet`: 투사체 Object Pool
  - `ShotgunWeapon`: 산탄 6발 + 긴 딜레이
  - `BurstGunWeapon`: 고속 단발 연사
  - `LaserBeamWeapon`: Raycast 빔 + Heat 과열 시스템
  - `LockOnWeapon`: 범위 타게팅 + 일괄 미사일
  - `WeaponSwitcher`: 디버그용 숫자키 교체 (1~4)
  - `AimController` 리팩토링: 무기 위임 방식
  - 상세 문서: `docs/WeaponSystem.md`

- **Formation(군집) 시스템**: Phase 2 구현
  - `BugPool` + `PooledBug` + `BugPoolConfig`: Object Pooling (600+ 동시 스폰 지원)
  - `FormationData` SO: Cluster/Line/Swarm 3가지 진형 타입
  - `FormationGroup` + `FormationMember`: 리더 이동 + 멤버 오프셋 추적
  - `FormationSpawner`: 외곽 스폰 + 진형 조립
  - `FormationOffsetCalculator`: 진형 패턴 계산 (static)
  - `BugManager` + `OffscreenVisibilityTracker`: Update 분산 준비
  - Phase 1/2/3 자동 전환 (머신 거리 기반 제어권 이양)
  - 상세 문서: `docs/FormationSystem.md`

### Changed
- `BugController`: Formation 연동 (MovementExternallyControlled 플래그, Pool 복귀, ResetForPool)
- `WaveData`: FormationSpawnEntry 추가 (레거시 SpawnGroup과 공존)
- `WaveManager`: FormationSpawner 참조 + Formation 스폰 코루틴

### Fixed
- NaN 에러: FormationSpawner가 BugPool에서 Get한 Bug를 Initialize하지 않던 문제
- HP바 NaN 방어: `UpdateHpBar`에 `_maxHealth > 0` 체크 추가
- 리더 제어권 누락: FormationGroup.Setup에서 리더 자동 제어권 획득
- 멤버 회전 동기화: UpdateMembers에 리더 rotation Slerp 적용

---

## [0.2.0] - 2024-03-26

### Added
- **데스 이펙트 시스템**: Bug 사망 시 FX_Socket 위치에 VFX 재생
  - `BugBase.PlayDeathVfx()` 메서드 추가
  - `_deathVfxPrefab` 필드로 프리팹 설정
  - ParticleSystem 재생 완료 후 자동 파괴
- **피격 깜빡임 효과**: Bug 피격 시 흰색 플래시
  - `BugBase.PlayHitFlash()` 메서드 추가
  - `_hitFlashDuration` 필드로 지속 시간 설정 (기본 0.1초)
  - MaterialPropertyBlock 사용으로 인스턴스별 독립 처리
- **FX_Socket**: 적 프리팹에 VFX 출력 위치 소켓 추가 (아트팀)
- **FX_Death_01**: 데스 이펙트 프리팹 추가 (아트팀)

### Changed
- `BugBase.cs`: VFX 관련 필드 및 메서드 추가
  - `_fxSocket`, `_deathVfxPrefab`, `_hitFlashDuration` 필드
  - `CacheRenderers()`, `FindFxSocket()` 초기화 메서드
  - `PlayHitFlash()`, `HitFlashCoroutine()`, `PlayDeathVfx()` 메서드

### Files Changed
- `Assets/_Game/Scripts/Bug/BugBase.cs`
- `Assets/_Game/Prefabs/Bugs/Bug_Beetle.prefab` (FX_Socket 추가)
- `Assets/_Game/Prefabs/Bugs/Bug_Fly.prefab` (FX_Socket 추가)
- `Assets/_Game/Prefabs/Bugs/Bug_Centipede.prefab` (FX_Socket 추가)
- `Assets/_Game/VFX/Prefabs/FX_Death_01.prefab` (신규)

---

## [0.1.1] - 2024-03-26

### Changed
- **BugHealthBar → BugHpBar 리팩토링**: 네이밍 통일
  - 클래스명, 프리팹명, 변수명 모두 HpBar로 변경
- **HP바 스프라이트 수정**: `Square_White_World.png` (PPU 4) 사용
  - 월드 스페이스에서 적절한 크기로 표시
- **BugData.HpBarOffset 추가**: 벌레별 HP바 위치 커스터마이징

### Fixed
- HP바가 너무 작게 표시되던 문제 수정
- HP바 Fill/Background 위치 분리 문제 수정

### Files Changed
- `Assets/_Game/Scripts/Bug/BugHpBar.cs` (renamed from BugHealthBar)
- `Assets/_Game/Scripts/Bug/BugBase.cs`
- `Assets/_Game/Scripts/Data/BugData.cs`
- `Assets/_Game/Scripts/Editor/BugPrefabEditor.cs`
- `Assets/_Game/Prefabs/UI/BugHpBar.prefab` (renamed)

---

## [0.1.0] - 2024-03-25

### Added
- **코어 시스템**: GameManager, DataManager, GameEvents
- **머신 시스템**: MachineController, MachineData
- **벌레 시스템**: BugBase, BeetleBug, FlyBug, CentipedeBug
- **웨이브 시스템**: WaveManager, WaveData, SpawnGroup
- **조준 시스템**: AimController (마우스 커서 조준/사격)
- **UI 시스템**: HP바, 연료바, 웨이브 표시
- **데이터 시스템**: ScriptableObject 기반 (BugData, WaveData, MachineData, UpgradeData)

### Files Added
- `Assets/_Game/Scripts/Core/` (GameManager, DataManager, GameEvents)
- `Assets/_Game/Scripts/Bug/` (BugBase, 벌레 종류별)
- `Assets/_Game/Scripts/Machine/` (MachineController)
- `Assets/_Game/Scripts/Wave/` (WaveManager)
- `Assets/_Game/Scripts/Aim/` (AimController)
- `Assets/_Game/Scripts/Data/` (ScriptableObject 정의)
- `Assets/_Game/Data/` (ScriptableObject 에셋)

---

## 버전 규칙

- **Major (X.0.0)**: 대규모 기능 추가, 호환성 변경
- **Minor (0.X.0)**: 새로운 기능 추가
- **Patch (0.0.X)**: 버그 수정, 작은 개선

## 태그 설명

- `Added`: 새로운 기능
- `Changed`: 기존 기능 변경
- `Deprecated`: 곧 삭제될 기능
- `Removed`: 삭제된 기능
- `Fixed`: 버그 수정
- `Security`: 보안 관련

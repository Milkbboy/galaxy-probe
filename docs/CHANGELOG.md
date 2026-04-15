# Changelog

모든 주요 변경 사항을 기록합니다.

형식: [Keep a Changelog](https://keepachangelog.com/ko/1.0.0/)

---

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

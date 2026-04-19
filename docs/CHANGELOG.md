# Changelog

모든 주요 변경 사항을 기록합니다.

형식: [Keep a Changelog](https://keepachangelog.com/ko/1.0.0/)

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

# Changelog

모든 주요 변경 사항을 기록합니다.

형식: [Keep a Changelog](https://keepachangelog.com/ko/1.0.0/)

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

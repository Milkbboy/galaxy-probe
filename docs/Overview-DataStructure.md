# 게임 데이터 구조 (Data Structure)

기획자가 Google Sheets에서 튜닝 가능한 게임 데이터 정의서. 시트 컬럼·입력 방법·Import 절차는 [Data-SheetsGuide.md](Data-SheetsGuide.md) 참조.

> 최종 갱신: 2026-04-23 — SimpleBug 전면 교체 반영. 구 `BugData`/`BugBehaviorData`/`WaveData(SpawnGroup)` 체계 삭제 (`docs/archive/` 이동).

---

## 데이터 계층 구조

```
Google Sheets                  ScriptableObject                  Scene Components
─────────────                  ────────────────                  ────────────────
SimpleBugData 시트   ─────▶   SimpleBug_*.asset × 3     ─────▶  SimpleBugSpawner
                              (숫자 필드만 덮어쓰기,            TunnelEventManager
                               Prefab·VFX는 수동 연결 유지)

WaveData 시트        ─────▶   Wave_NN.asset × N          ─────▶  SimpleWaveManager
                              (SimpleWaveData 타입)              (Spawner/Tunnel에 웨이브별 주입)

(시트 없음)                    SpawnConfig.asset × 1      ─────▶  SimpleWaveManager
                              (인스펙터 직편집)                   (전역 폴백값 공급)

MachineData 시트     ─────▶   Machine_*.asset
UpgradeData 시트     ─────▶   Upgrade_*.asset              (v2 이중 재화 — 광석+보석)
WeaponData 시트      ─────▶   Weapon_*.asset               (5종 — sniper/bomb/gun/laser/saw)
WeaponUpgradeData 시트 ───▶   WeaponUpgrade_*.asset        (15종 — 무기당 3개)
CharacterData 시트   ─────▶   Character_*.asset            (3종 — jinus/sara/victor)
AbilityData 시트     ─────▶   Ability_*.asset              (9종 — 캐릭터당 3개)
```

| 계층 | ScriptableObject | 시트 | 역할 |
|---|---|---|---|
| **Bug 스탯** | `SimpleBugData` | `SimpleBugData` | HP·Speed·Size·Score·Tint + 웨이브 스케일링 |
| **Wave 오버라이드** | `SimpleWaveData` | `WaveData` | 웨이브별 Spawner/Tunnel 파라미터 오버라이드 |
| **Spawn 폴백** | `SpawnConfigData` | **없음** | 시트에 없는 값 + 오버라이드 디폴트 |
| **Machine** | `MachineData` | `MachineData` | 머신 HP·채굴률·공격 스탯 + BaseMiningTarget |
| **Upgrade** | `UpgradeData` | `UpgradeData` | 굴착기 6종 영구 강화 (이중 재화 비용) |
| **Weapon** | `WeaponData` (서브타입 5) | `WeaponData` | 무기 5종 베이스 스탯 — 공통 + ExtraStats 한 셀 압축 |
| **WeaponUpgrade** | `WeaponUpgradeData` | `WeaponUpgradeData` | 무기별 강화 15항목 (Damage/Range/Cooldown/Ammo/Reload/Radius/Slow) |
| **Character** | `CharacterData` | `CharacterData` | 3 캐릭터 + 기본 머신 + 어빌리티 3종 묶음 |
| **Ability** | `AbilityData` | `AbilityData` | 캐릭터 어빌리티 9종 (cooldown/damage/range 등) |

---

## 파일 구조

```
Assets/_Game/Data/
├── Bugs/
│   ├── SimpleBug_Normal.asset       # 일반 — HP 2, Speed 0.5, Size 0.4
│   ├── SimpleBug_Elit.asset         # 엘리트 (파일명 오타 그대로 유지 — BugName 필드로 매칭)
│   └── SimpleBug_Swift.asset        # 스위프트 — HP 1, Speed 3, 땅굴 전용
│
├── Waves/
│   ├── Wave_01.asset ~ Wave_05.asset  # SimpleWaveData 타입
│
├── SpawnConfig.asset                  # 단일 인스턴스 (시트 없음)
│
├── Machines/
│   └── Machine_Default.asset
│
├── Upgrades/
│   └── Upgrade_*.asset                # excavator_hp/armor, mine_speed/target, gem_drop/speed
│
├── Characters/                        # v2
├── Abilities/                         # v2
└── WeaponUpgrades/                    # v2
```

---

## 1. SimpleBugData

벌레 종류별 기본 스탯 + 웨이브 스케일링. 현재 3종(Normal/Elite/Swift) 고정이지만 시트에서 행 추가로 확장 가능.

### 필드

| 필드 | 타입 | 설명 |
|---|---|---|
| `BugName` | string | 식별용. 시트 컬럼과 동일. Import 매칭 기준 (파일명 무관) |
| `Kind` | enum | `Normal` / `Elite` / `Swift` — Spawner·Tunnel의 역할 분기 |
| `BaseHp` | float | 웨이브 1 기준 HP |
| `HpPerWave` | float | 웨이브당 HP 증가 (`floor(wave × HpPerWave)` 가산) |
| `BaseSpeed` | float | 웨이브 1 기준 이동 속도 (유닛/초) |
| `SpeedPerWave` | float | 웨이브당 속도 증가 |
| `SpeedRandom` | float | 스폰 시 속도 +랜덤 `[0, 값)` |
| `Size` | float | 스케일 (`localScale` 균등 배율) |
| `Score` | float | 처치 시 `OnBugScoreEarned` 발행값 (웨이브 전환 트리거) |
| `Tint` | Color | 미니맵 아이콘 색. 시트 `TintHex` 컬럼(`#RRGGBB`) 파싱 |
| `Prefab` | GameObject | **시트 무관** — 인스펙터에서 수동 연결. Import가 덮어쓰지 않음 |

### 스케일링 헬퍼

```csharp
public float GetHp(int wave)    => BaseHp + Mathf.Floor(wave * HpPerWave);
public float GetSpeed(int wave) => BaseSpeed + wave * SpeedPerWave + Random.Range(0f, SpeedRandom);
```

### 현재 값

| BugName | Kind | BaseHp | HpPerWave | BaseSpeed | SpeedPerWave | SpeedRandom | Size | Score | TintHex |
|---|---|---|---|---|---|---|---|---|---|
| Normal | Normal | 2 | 0.5 | 0.5 | 0.06 | 0.15 | 0.4 | 1 | #51CF66 |
| Elite | Elite | 10 | 0.5 | 0.35 | 0.04 | 0.15 | 0.8 | 5 | #FFD700 |
| Swift | Swift | 1 | 0 | 3 | 0 | 0 | 0.2 | 0.5 | #DEDEFF |

> 파일명 `SimpleBug_Elit.asset` 은 오타이나 `BugName="Elite"` 필드 기준 매칭이라 Import 시 문제 없음. rename이 필요하면 meta GUID를 유지한 채 별도 작업.

---

## 2. SimpleWaveData

웨이브 진입 시 Spawner/Tunnel에 주입되는 파라미터 **오버라이드 테이블**. `-1` 또는 빈 필드는 `SpawnConfig` 폴백값 사용. `0` 은 "명시적 값(대개 비활성)"으로 존중.

### 필드

| 필드 | 타입 | sentinel | 설명 |
|---|---|---|---|
| `WaveNumber` | int | — | 웨이브 번호 (1부터) |
| `WaveName` | string | — | 표시용 |
| `KillTarget` | float | `-1`·`0` = 전환 없음 | 누적 처치 점수 도달 시 다음 웨이브 전환. 마지막 웨이브는 `-1` |
| `NormalSpawnInterval` | float | `-1` = 폴백 | 일반 벌레 스폰 주기(초) |
| `EliteSpawnInterval` | float | `-1`·`0` = 비활성 **(폴백 아님)** | 엘리트 주기(초). 초기 웨이브 엘리트 끄는 용도라 폴백 안 타게 설계 |
| `MaxBugs` | int | `-1` = 폴백 | 동시 생존 상한 |
| `TunnelEnabled` | bool | — | 이 웨이브부터 땅굴 이벤트 활성. `false` 면 뒤 두 필드 무시 |
| `TunnelEventInterval` | float | `-1` = 폴백 | 땅굴 주기(초) |
| `SwiftPerTunnel` | int | `-1` = 폴백 | 한 땅굴당 Swift 수 |

### Resolve 헬퍼 (`SpawnConfig` 주입)

```csharp
ResolveNormalSpawnInterval(cfg) => NormalSpawnInterval >= 0 ? NormalSpawnInterval : cfg.DefaultNormalSpawnInterval;
ResolveEliteSpawnInterval(cfg)  => EliteSpawnInterval  > 0 ? EliteSpawnInterval  : 0f;  // 예외: 0 리턴=비활성
ResolveMaxBugs(cfg)             => MaxBugs             >= 0 ? MaxBugs            : cfg.DefaultMaxBugs;
ResolveTunnelEventInterval(cfg) => TunnelEventInterval >= 0 ? TunnelEventInterval : cfg.DefaultTunnelEventInterval;
ResolveSwiftPerTunnel(cfg)      => SwiftPerTunnel      >= 0 ? SwiftPerTunnel      : cfg.DefaultSwiftPerTunnel;
```

### 현재 값

| WaveNumber | WaveName | KillTarget | Normal | Elite | MaxBugs | Tunnel | TunnelInt | Swift |
|---|---|---|---|---|---|---|---|---|
| 1 | 시작 | 15 | 0.12 | -1 | 50 | FALSE | -1 | -1 |
| 2 | 가속 | 25 | 0.1 | -1 | 70 | FALSE | -1 | -1 |
| 3 | 땅굴 출현 | 40 | 0.083 | 15 | 90 | TRUE | 15 | 10 |
| 4 | 러시 | 60 | 0.07 | 12 | 110 | TRUE | 12 | 12 |
| 5 | 최종 | -1 | 0.06 | 10 | 130 | TRUE | 10 | 15 |

**KillTarget 의미**: `Normal=1 + Elite=5 + Swift=0.5` 누적 점수. Wave 1의 15점 ≈ Normal 15마리 또는 Elite 3마리.

**세션 종료와 무관**: 웨이브는 난이도 곡선일 뿐. 세션은 **채굴 완료(승리)** / **머신 HP 0(패배)** 로만 종료. 마지막 웨이브 `KillTarget=-1` 은 "세션 끝까지 파라미터 유지".

---

## 3. SpawnConfigData

**시트에 없는 전역 폴백값**. 인스펙터 직편집 전용 (튜닝 빈도 낮음). `SpawnConfig.asset` 단일 인스턴스.

| 그룹 | 필드 | 기본값 | 설명 |
|---|---|---|---|
| Spawn 기본 | `DefaultNormalSpawnInterval` | 0.083 | Wave의 `-1` 대체 |
| | `DefaultEliteSpawnInterval` | 15 | (WaveData `-1/0` 는 비활성으로 해석되므로 실질 미사용) |
| | `DefaultMaxBugs` | 90 | |
| Tunnel 기본 | `TunnelGameTimeStart` | 30 | **게임 시작 후 N초 지나야 땅굴 발생** (`TunnelEnabled=true` 웨이브라도 대기) |
| | `DefaultTunnelEventInterval` | 15 | |
| | `DefaultSwiftPerTunnel` | 10 | |
| | `TunnelSpawnInterval` | 0.2 | 한 땅굴 내 Swift 생성 간격 |
| 스폰 영역 | `AutoRadius` | true | 카메라 Orthographic 크기로 자동 반경 |
| | `ManualRadius` | 15 | AutoRadius=false 시 사용 |
| | `NormalMargin` | 0.4 | 일반 스폰 반경 추가 여유 |
| | `EliteMargin` | 0.5 | 엘리트 스폰 반경 추가 여유 |
| | `EdgeMargin` | 0.4 | 땅굴 위치 화면 가장자리 안쪽 여유 |
| | `SpawnJitter` | 0.15 | 땅굴 지점 주변 Swift 랜덤 오프셋 |

---

## 4. 런타임 흐름

```
Start
  └─ SimpleWaveManager.StartWave(0)
       ├─ _waves[0] 읽음
       ├─ SimpleBugSpawner.Configure(wave, spawnConfig)
       │    └─ _spawnInterval = Resolve…, _eliteInterval, _maxBugs, _wave 갱신
       └─ TunnelEventManager.Configure(wave, spawnConfig)
            └─ _autoRun = TunnelEnabled, _eventInterval, _swiftPerTunnel, _gameTimeStart 갱신

Update
  ├─ Spawner 자체 타이머 (_spawnInterval 주기로 SpawnNormal, _eliteInterval 주기로 SpawnElite)
  └─ Tunnel 자체 타이머 (_gameTimeStart 지난 후 _eventInterval 주기로 StartTunnelEvent → Swift N마리)

GameEvents.OnBugScoreEarned(score)  // SimpleBug.TakeDamage 사망 시 발행
  └─ SimpleWaveManager._waveScoreAccum += score
       └─ if _waveScoreAccum >= KillTarget → StartWave(index+1)
            └─ 같은 Configure() 재호출 → Spawner._wave 증가 → SimpleBugData.GetHp/Speed 자동 스케일
```

**핵심 원칙**
- Spawner/Tunnel 은 자체 타이머·스폰 루프를 계속 돌리고, `SimpleWaveManager` 는 **파라미터 주입자** 역할만.
- 웨이브 전환 = **Configure() 재호출**. 기존 생존 벌레는 유지되고, 새 스폰부터 새 파라미터 적용.
- Elite/Swift 는 Spawner·Tunnel 이 내부에 `SimpleBugData` 참조를 하드코딩 (Normal/Elite 는 Spawner 인스펙터, Swift 는 TunnelEventManager 인스펙터).

---

## 5. MachineData

플레이어가 지키는 채굴 머신의 스탯. 시트 `MachineData`. v2 는 단일 머신 (`Machine_Default`) — 캐릭터 선택은 별도 `CharacterData` 에서 담당.

| 필드 | 타입 | 기본값 | 설명 |
|---|---|---|---|
| `MachineId` | int | 1 | 고유 ID |
| `MachineName` | string | "Default" | 머신 이름 |
| `MaxHealth` | float | 100 | 최대 체력 (v2.html 베이스) |
| `HealthRegen` | float | 0 | 초당 체력 회복 |
| `Armor` | float | 0 | legacy 공식 `armor/(armor+100)` |
| `MiningRate` | float | 5 | 초당 채굴량 (v2.html `baseMineRate=5`) |
| `MiningBonus` | float | 0 | 채굴 보너스(%) |
| `BaseMiningTarget` (v2) | float | 100 | **세션 승리 목표 채굴량**. `mine_target` 업그레이드로 +50/lv |
| `BaseGemDropRate` (v2) | float | 0.05 | 일반 벌레 기본 보석 드랍 확률. `gem_drop` 업그레이드 %p 가산 (v2.html `0.05 + gemDropBonus`) |

> 구 필드 정리:
> - `MaxFuel`/`FuelConsumeRate` — v2 승리 조건이 연료 소진 → `BaseMiningTarget` 달성으로 전환되며 삭제
> - `AttackDamage`/`AttackCooldown`/`AttackRange`/`CritChance`/`CritMultiplier` — v2 에서 머신은 공격 안 함 (무기가 self-driven). 2026-04-24 일괄 삭제

---

## 6. UpgradeData

아웃게임 영구 강화. **이중 재화** (광석 + 보석). 시트 `UpgradeData`.

| 필드 | 타입 | 설명 |
|---|---|---|
| `UpgradeId` | string | `excavator_hp`, `mine_target` 등 |
| `DisplayName` | string | |
| `UpgradeType` | enum | `MaxHealth`/`Armor`/`MiningRate`/`MiningTarget`/`GemDropRate`/`GemCollectSpeed` |
| `MaxLevel` | int | 3 또는 5 |
| `BaseValue` | float | 기본 값 (보통 0) |
| `ValuePerLevel` | float | 레벨당 증가 |
| `IsPercentage` | bool | true면 % 적용 |
| `BaseCost` | int | 1레벨 **광석** 비용 |
| `BaseCostGem` (v2) | int | 1레벨 **보석** 비용 (gem_drop/speed 전용) |
| `CostMultiplier` | float | 비용 증가율 (Schedule 비어있을 때) |
| `OreCostSchedule` (v2) | int[] | 레벨별 광석 비용 배열 |
| `GemCostSchedule` (v2) | int[] | 레벨별 보석 비용 배열 |

### v2 현행 6종

| Type | UpgradeId | MaxLv | ValuePerLevel | % | 비용 schedule |
|---|---|---|---|---|---|
| MaxHealth | excavator_hp | 5 | +30 | | 광석 [60,130,230,370,540] |
| Armor | excavator_armor | 3 | +0.15 (받는 피해 감소율) | ✓ | 광석 [150,300,500] |
| MiningRate | mine_speed | 5 | +2 (초당 채굴) | | 광석 [80,160,280,440,640] |
| MiningTarget (v2) | mine_target | 5 | +50 (목표량) | | 광석 [100,200,350,550,800] |
| GemDropRate (v2) | gem_drop | 5 | +0.02 (확률 %p) | | **보석** [15,30,50,75,105] |
| GemCollectSpeed (v2) | gem_speed | 5 | +0.20 (배율) | ✓ | **보석** [10,22,38,58,82] |

> 무기별 강화는 별도 `WeaponUpgradeData` (15항목, 5무기 × 3). 상세: [Sys-Weapon.md](Sys-Weapon.md).

---

## 7. Weapon / WeaponUpgrade / Character / Ability

설계 의도 + 시트 컬럼 정의는 다음 문서 참조:

- [Sys-Weapon.md](Sys-Weapon.md) — 무기 5종 + 해금 체인 + 강화 15항목 설계
- [Sys-Character.md](Sys-Character.md) — 캐릭터 3 · 어빌리티 9 설계
- [Sys-Gem.md](Sys-Gem.md) — 보석 드랍·채집 + 이중 재화
- [Data-SheetsGuide.md §5~§8](Data-SheetsGuide.md#5-weapondata-시트) — Weapon/WeaponUpgrade/Character/Ability 시트 스키마 (단일 SSoT)

---

## 참고

- 시트 입력·Import 절차: [Data-SheetsGuide.md](Data-SheetsGuide.md)
- 기획자 전달용 압축 가이드: [Data-PlannerGuide.md](Data-PlannerGuide.md)
- Bug/Wave 설계 근거 및 프로토타입 포팅 맥락: 커밋 `9b32067` (SimpleBug 전면 교체) + `docs/Overview-Changelog.md` 의 2026-04-23 섹션
- 구 BugBehavior/Formation 시스템 문서: `docs/archive/{BugBehaviorSystem,BugBehaviorPatterns,FormationSystem}.md`

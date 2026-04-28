# Google Sheets 데이터 관리 가이드

게임 데이터(벌레·웨이브·머신·업그레이드·무기·무기강화·캐릭터·어빌리티)는 Google Sheets 에서 관리하고, Unity Editor 메뉴 `Tools / Drill-Corp / 4. 데이터 Import / Google Sheets Importer` 로 가져와 ScriptableObject 로 변환합니다.

**스프레드시트 URL**: https://docs.google.com/spreadsheets/d/1hwgQ4IF-gQqVSX4xS_uqeKIPWUDy2NR4bC-OWmZQO_E/edit

> 최종 갱신: 2026-04-26 — Weapon/WeaponUpgrade/Character/Ability 4탭 통합 완료. 구 `GoogleSheetsGuide_v2Addendum.md` 흡수.
>
> **기획자 전달용 압축 가이드**: [Data-PlannerGuide.md](Data-PlannerGuide.md)

---

## 시트 구조 (전체 9탭)

| § | 시트 이름 | 행 수 | 설명 | 대응 SO |
|---|---|---|---|---|
| §1 | `SimpleBugData` | 3 | 벌레 종류별 스탯 | `SimpleBugData` (Normal/Elite/Swift) |
| §2 | `WaveData` | 5 | 웨이브별 Spawner/Tunnel 오버라이드 | `SimpleWaveData` (Wave_01~05) |
| §3 | `MachineData` | 1 | 채굴 머신 스탯 | `MachineData` (Default) |
| §4 | `UpgradeData` | 6 | 굴착기 영구 강화 (이중 재화) | `UpgradeData` |
| §5 | `WeaponData` | 5 | 무기 5종 베이스 스탯 (ExtraStats 한 셀 압축) | `WeaponData` 서브타입 |
| §6 | `WeaponUpgradeData` | 15 | 무기별 강화 (Damage/Range/Cooldown 등) | `WeaponUpgradeData` |
| §7 | `CharacterData` | 3 | 캐릭터 3종 + 머신·어빌리티 묶음 | `CharacterData` |
| §8 | `AbilityData` | 9 | 캐릭터 어빌리티 (cooldown/damage/range) | `AbilityData` |
| §9 | `BossData` | 1 | 거미 보스 튜닝 (HP·movement·telegraph) | `BossData` (Boss_Spider) |

> 구 시트 `BugData`, `WaveSpawnGroups`, `BugBehaviors`, `MovementData`, `AttackData`, `PassiveData`, `SkillData`, `TriggerData` 는 2026-04-23 SimpleBug 전면 교체로 **삭제**. Importer 도 참조하지 않음. 시트에 남아있으면 `_legacy_` prefix 로 rename 하거나 삭제.

---

## 1. SimpleBugData 시트

벌레 종류별 기본 스탯 + 웨이브 스케일링.

### 컬럼

| 컬럼 | 타입 | 필수 | 설명 | 예시 |
|---|---|---|---|---|
| **BugName** | string | O | 식별용. Import 매칭 기준 (파일명 무관, SO 내부 필드) | Normal, Elite, Swift |
| **Kind** | enum | O | `Normal` / `Elite` / `Swift` (대소문자 무시) | Normal |
| **BaseHp** | float | O | 웨이브 1 기준 HP | 2, 10 |
| **HpPerWave** | float | O | 웨이브당 HP 증가 (`floor(wave × HpPerWave)` 가산) | 0.5, 0 |
| **BaseSpeed** | float | O | 웨이브 1 기준 이동 속도 (유닛/초) | 0.5, 3 |
| **SpeedPerWave** | float | O | 웨이브당 속도 증가 | 0.06, 0 |
| **SpeedRandom** | float | | 스폰 시 속도 +랜덤 `[0, 값)` | 0.15 |
| **Size** | float | O | 스케일 (localScale 균등 배율) | 0.4, 0.8, 0.2 |
| **Score** | float | O | 처치 시 보상 (WaveData `KillTarget` 에 누적되는 값) | 1, 5, 0.5 |
| **TintHex** | string | | 미니맵 아이콘 색. `#RRGGBB` 또는 `#RRGGBBAA` | #51CF66, #FFD700 |

**Prefab 컬럼 없음** — Unity 인스펙터에서 대상 SO를 열어 `Prefab` 필드에 수동 연결. Import가 `Prefab` 및 런타임 바인딩 필드를 절대 덮어쓰지 않음.

**빈 셀 규칙**: 비어있으면 **기존 SO 값 보존**. 명시적 0이 필요하면 `0` 을 입력 (예: Swift 의 HpPerWave 는 `0` 명시).

### BugName ↔ 파일명 매핑

Import 는 파일명을 무시하고 SO 내부 `BugName` 필드로 매칭.
- 기존 `SimpleBug_Elit.asset` (오타) 같은 경우도 `BugName="Elite"` 필드로 계속 매칭됨 — 파일 rename 불필요.
- 새 BugName 을 시트에 추가하면 Import가 `Assets/_Game/Data/Bugs/SimpleBug_<BugName>.asset` 으로 자동 생성. Prefab 필드는 수동 연결.

### 현재 값

| BugName | Kind | BaseHp | HpPerWave | BaseSpeed | SpeedPerWave | SpeedRandom | Size | Score | TintHex |
|---|---|---|---|---|---|---|---|---|---|
| Normal | Normal | 2 | 0.5 | 0.5 | 0.06 | 0.15 | 0.4 | 1 | #51CF66 |
| Elite | Elite | 10 | 0.5 | 0.35 | 0.04 | 0.15 | 0.8 | 5 | #FFD700 |
| Swift | Swift | 1 | 0 | 3 | 0 | 0 | 0.2 | 0.5 | #DEDEFF |

---

## 2. WaveData 시트

웨이브별 **오버라이드**. 빈 셀/`-1` 은 `SpawnConfig.asset` 의 폴백값 사용. `0` 은 "명시적 값"으로 존중.

### 컬럼

| 컬럼 | 타입 | 필수 | 설명 | 예시 |
|---|---|---|---|---|
| **WaveNumber** | int | O | 웨이브 번호 (1부터) | 1, 2, 3 |
| **WaveName** | string | | 표시용 | 시작, 가속, 땅굴 출현 |
| **KillTarget** | float | | 이 웨이브에서 누적해야 할 처치 점수. 도달 시 다음 웨이브. `-1` / `0` = 전환 없음 (마지막 웨이브 유지) | 15, 25, -1 |
| **NormalSpawnInterval** | float | | 일반 벌레 스폰 주기(초) | 0.083 |
| **EliteSpawnInterval** | float | | 엘리트 주기(초). `-1` 또는 `0` = **엘리트 비활성** (폴백 아닌 예외) | 15, -1 |
| **MaxBugs** | int | | 동시 생존 상한 | 90 |
| **TunnelEnabled** | bool | | 이 웨이브부터 땅굴 이벤트 활성 | TRUE, FALSE |
| **TunnelEventInterval** | float | | 땅굴 주기(초) | 15 |
| **SwiftPerTunnel** | int | | 한 땅굴당 Swift 수 | 10 |

### Sentinel 규칙

- **빈 셀** → SpawnConfig 폴백값 사용 (= "오버라이드 없음")
- **`-1`** → 빈 셀과 동일 (폴백)
- **`0`** → 명시적 0. `MaxBugs=0` 이면 "상한 없음"이 아니라 "0마리 유지"
- **예외 — EliteSpawnInterval**: `-1`·`0` 모두 **엘리트 비활성**으로 해석. 초기 웨이브에서 엘리트를 끄는 용도라 폴백을 타지 않음
- **예외 — KillTarget**: `-1`·`0` 모두 **전환 없음 = 이 웨이브를 세션 끝까지 유지**. 마지막 웨이브 지정 방식
- **TunnelEnabled=FALSE** 이면 `TunnelEventInterval`·`SwiftPerTunnel` 무시

### KillTarget 의미 (웨이브 전환 트리거)

`Normal=1 + Elite=5 + Swift=0.5` 처치 시 누적 (SimpleBugData.Score 컬럼 기반). 예: Wave 1 `KillTarget=15` ≈ Normal 15마리 or Elite 3마리.

**세션 종료와 무관**: 웨이브는 난이도 곡선일 뿐. 세션은 **채굴 완료(승리)** / **머신 HP 0(패배)** 로만 종료. 마지막 웨이브 `KillTarget=-1` → 전환 없음, 세션 끝까지 스폰 계속.

### 현재 값

| WaveNumber | WaveName | KillTarget | NormalSpawnInterval | EliteSpawnInterval | MaxBugs | TunnelEnabled | TunnelEventInterval | SwiftPerTunnel |
|---|---|---|---|---|---|---|---|---|
| 1 | 시작 | 15 | 0.12 | -1 | 50 | FALSE | -1 | -1 |
| 2 | 가속 | 25 | 0.1 | -1 | 70 | FALSE | -1 | -1 |
| 3 | 땅굴 출현 | 40 | 0.083 | 15 | 90 | TRUE | 15 | 10 |
| 4 | 러시 | 60 | 0.07 | 12 | 110 | TRUE | 12 | 12 |
| 5 | 최종 | -1 | 0.06 | 10 | 130 | TRUE | 10 | 15 |

> Wave 3 `TunnelEnabled=TRUE` 로 전환돼도 **게임 시작 후 `TunnelGameTimeStart`(기본 30s) 가 지나야** 실제 땅굴 발생. `SpawnConfig.asset` 필드.

### SpawnConfig (시트 없음)

`Assets/_Game/Data/SpawnConfig.asset` 는 **시트에 없는 전역 폴백값**. 인스펙터 직편집. 튜닝 빈도 낮아 시트에서 뺌. 필드 상세는 [Overview-DataStructure.md §3](Overview-DataStructure.md#3-spawnconfigdata).

---

## 3. MachineData 시트

채굴 머신 스탯. [Overview-DataStructure.md §5](Overview-DataStructure.md#5-machinedata) 참조. v2 는 단일 머신 (Default 1행). 컬럼:

`MachineId`, `MachineName`, `Description`, `MaxHealth`, `HealthRegen`, `Armor`, `MiningRate` (v2: 5), `MiningBonus`, `BaseMiningTarget` (v2: 100), `BaseGemDropRate` (v2: 0.05).

> 삭제된 구 컬럼 — 시트에 남아있으면 제거:
> - `MaxFuel`, `FuelConsumeRate` (연료 → mineTarget 전환)
> - `AttackDamage`, `AttackCooldown`, `AttackRange`, `CritChance`, `CritMultiplier` (v2 는 머신이 공격 안 함 — 무기 self-driven)

---

## 4. UpgradeData 시트

영구 강화. 이중 재화(광석+보석). [Overview-DataStructure.md §6](Overview-DataStructure.md#6-upgradedata) 참조. 컬럼:

`UpgradeId`, `DisplayName`, `Description`, `UpgradeType`, `MaxLevel`, `BaseValue`, `ValuePerLevel`, `IsPercentage`, `CurrencyType` (v2 — Ore/Gem/Both), `BaseCostOre` (v2 — 광석, 구 `BaseCost` rename), `BaseCostGem` (v2 — 보석), `CostMultiplier`, `OreCostSchedule` (v2 — 파이프 구분 배열), `GemCostSchedule` (v2).

### v2 활성 6종

| UpgradeId | UpgradeType | MaxLevel | ValuePerLevel | IsPercentage | 비용 |
|---|---|---|---|---|---|
| `excavator_hp` | MaxHealth | 5 | 30 | FALSE | 광석 [60,130,230,370,540] |
| `excavator_armor` | Armor | 3 | 0.15 | TRUE | 광석 [150,300,500] |
| `mine_speed` | MiningRate | 5 | 2 | FALSE | 광석 [80,160,280,440,640] |
| `mine_target` | MiningTarget | 5 | 50 | FALSE | 광석 [100,200,350,550,800] |
| `gem_drop` | GemDropRate | 5 | 0.02 | FALSE | **보석** [15,30,50,75,105] |
| `gem_speed` | GemCollectSpeed | 5 | 0.20 | TRUE | **보석** [10,22,38,58,82] |

---

## 5. WeaponData 시트

무기 5종의 베이스 스탯. v2 무기 시스템: [Sys-Weapon.md](Sys-Weapon.md). 컬럼:

`WeaponId`, `DisplayName`, `Description`, `ThemeColorHex`, `UnlockedByDefault`, `UnlockGemCost`, `RequiredWeaponId`, `FireDelay`, `Damage`, `HitVfxLifetime`, `ExtraStats`.

### `ExtraStats` — 무기별 고유 스탯 한 셀에 압축

5개 무기가 각자 다른 서브클래스(SniperWeaponData/BombData/...)를 갖고 있어 고유 필드가 다름. 시트 컬럼 폭증을 막기 위해 **한 셀에 `key:value|key:value` 형식**으로 묶음.

```
sniper  → useAimRadius:true|customRange:0.4
bomb    → explosionRadius:3|instant:false|projectileSpeed:10|...
gun     → maxAmmo:40|reloadDuration:5|spreadAngle:0.06|...
laser   → cooldown:5|beamDuration:10|beamRadius:1|tickInterval:0.1|...
saw     → orbitRadius:7.2|bladeRadius:1.8|spinSpeed:4.8|slowFactor:0.3|slowDuration:2
```

임포터(`GoogleSheetsImporter._allowedExtraKeys`) 에 무기별 허용 키 화이트리스트가 박혀 있어 **타 무기 키나 오타는 LogError**. 새 필드 추가 시 SO 클래스 + 화이트리스트 둘 다 갱신 필요.

### 시트화 안 되는 필드

prefab/sprite/SO 참조 (`_icon`, `_hitVfxPrefab`, `_bulletPrefab`, `_landingMarkerPrefab`, ...) 는 Unity 에서 직접 바인딩. `RequiredWeapon` 만 예외 — 시트에 `RequiredWeaponId` (문자열) 로 표현, 임포터가 SO 참조로 재해석.

### 무기 5종 (현재 값)

| WeaponId | UnlockedByDefault | UnlockGemCost | RequiredWeaponId | FireDelay | Damage |
|---|---|---|---|---|---|
| `sniper` | TRUE | 0 | | 0.5 | 1 |
| `bomb` | FALSE | 0 | | 5 | 3 |
| `gun` | FALSE | 0 | | 0.14 | 0.5 |
| `laser` | FALSE | 0 | | 0 | 0.8 |
| `saw` | FALSE | 40 | gun | 0 | 0.15 |

> 신규 무기는 시트만으로 추가 불가 — Unity 에서 SO 인스턴스(서브클래스 선택)를 먼저 만든 뒤 시트 import 로 값 갱신.

---

## 6. WeaponUpgradeData 시트

무기별 강화 15종. 모두 `WeaponUpgradeData` 단일 클래스. 컬럼:

`UpgradeId`, `WeaponId`, `DisplayName`, `TargetStat`, `MaxLevel`, `ValuePerLevel`, `IsPercentage`, `Operation`, `BaseCostOre`, `BaseCostGem`, `OreCostMultiplier`, `GemCostMultiplier`, `ManualCostsOre`, `ManualCostsGem`.

### Enum 값

- `TargetStat`: `Damage` / `Range` / `Cooldown` / `AmmoBonus` / `ReloadTime` / `Radius` / `SlowBonus`
- `Operation`: `Add` (덧셈) / `Multiply` (곱셈, ValuePerLevel 음수 = 감소)

### 비용 — 공식 vs 수동

기본은 공식 — `BaseCost × Multiplier^level`. 레벨별로 정확한 비용을 명시하려면 `ManualCostsOre` / `ManualCostsGem` 에 파이프 배열 입력 (둘 다 같은 길이). 빈 칸이면 공식 사용.

```
ManualCostsOre = 40|90|180|360|720
ManualCostsGem =  2| 5| 10| 18| 30
→ 1렙 (40, 2), 2렙 (90, 5), ...
```

### 무기별 강화 3종씩 (현재 값)

| 무기 | 강화 ID | TargetStat | MaxLv | Δ/lv | Operation | BaseCost (Ore, Gem) |
|---|---|---|---|---|---|---|
| sniper | `sniper_dmg` | Damage | 5 | +25% | Multiply | 40 / 2 |
| sniper | `sniper_range` | Range | 3 | +20% | Multiply | 55 / 3 |
| sniper | `sniper_cd` | Cooldown | 4 | -20% | Multiply | 45 / 2 |
| bomb | `bomb_dmg` | Damage | 4 | +30% | Multiply | 60 / 4 |
| bomb | `bomb_radius` | Radius | 4 | +20% | Multiply | 55 / 3 |
| bomb | `bomb_cd` | Cooldown | 4 | -15% | Multiply | 45 / 3 |
| gun | `gun_dmg` | Damage | 5 | +25% | Multiply | 70 / 5 |
| gun | `gun_ammo` | AmmoBonus | 4 | +10 | Add | 55 / 4 |
| gun | `gun_reload` | ReloadTime | 4 | -20% | Multiply | 50 / 3 |
| laser | `laser_dmg` | Damage | 5 | +25% | Multiply | 85 / 6 |
| laser | `laser_range` | Range | 4 | +20% | Multiply | 70 / 5 |
| laser | `laser_cd` | Cooldown | 4 | -15% | Multiply | 60 / 4 |
| saw | `saw_dmg` | Damage | 5 | +20% | Multiply | 85 / 7 |
| saw | `saw_radius` | Radius | 4 | +25% | Multiply | 80 / 6 |
| saw | `saw_slow` | SlowBonus | 3 | +0.2 | Add | 95 / 8 |

---

## 7. CharacterData 시트

플레이어 캐릭터 3종. v2 시스템: [Sys-Character.md](Sys-Character.md). 컬럼:

`CharacterId`, `DisplayName`, `Title`, `Description`, `ThemeColorHex`, `DefaultMachineName`, `Ability1Id`, `Ability2Id`, `Ability3Id`.

### 시트화 안 되는 필드

`Portrait` (Sprite ref) 만 Unity 직접 바인딩.

### 외부 SO 참조 — 문자열로 표현

| 시트 컬럼 | 타겟 | 임포터 동작 |
|---|---|---|
| `DefaultMachineName` | `MachineData._machineName` | 매칭되는 `MachineData` SO 를 `_defaultMachine` 에 바인딩 |
| `Ability1Id` ~ `Ability3Id` | `AbilityData._abilityId` | 슬롯 1~3 에 순서대로 바인딩 |

> **임포트 순서 주의** — `Import All Data` 는 `AbilityData → CharacterData` 순으로 호출해 어빌리티 cache 가 캐릭터 임포트 시점엔 이미 채워져 있도록 보장. 시트별 버튼으로 `CharacterData` 만 단독 import 할 때 어빌리티가 누락되어 있으면 경고 로그 출력 + null 처리.

### 캐릭터 3종 (현재 값)

| CharacterId | DisplayName | Title | Color | DefaultMachine | Ability 슬롯 1·2·3 |
|---|---|---|---|---|---|
| `jinus` | 지누스 | 채굴 전문가 | #51CF66 | Default | drone / mining_drone / spider_drone |
| `sara` | 사라 | 방어 전문가 | #4FC3F7 | Default | blackhole / shockwave / meteor |
| `victor` | 빅터 | 중장비 전문가 | #F4A423 | Default | napalm / flame / mine |

---

## 8. AbilityData 시트

캐릭터 어빌리티 9종 (캐릭터 3 × 슬롯 3). 모두 `AbilityData` 단일 클래스. 컬럼:

`AbilityId`, `CharacterId`, `DisplayName`, `Description`, `IconEmoji`, `ThemeColorHex`, `SlotKey`, `AbilityType`, `Trigger`, `CooldownSec`, `DurationSec`, `AutoIntervalSec`, `Damage`, `Range`, `Angle`, `MaxInstances`, `UnlockGemCost`, `RequiredAbilityId`, `VfxScale`.

### Enum 값

- `AbilityType`: `Napalm` / `Flame` / `Mine` / `BlackHole` / `Shockwave` / `Meteor` / `Drone` / `MiningDrone` / `SpiderDrone`
- `Trigger`: `Manual` (키 1/2/3) / `AutoInterval` (`AutoIntervalSec` 마다 자동 발동)

### 필드 의미

- `CooldownSec` — Manual 트리거 발동 후 쿨다운
- `DurationSec` — 지속 효과 길이 (블랙홀 10초, 메테오 15초). 0 이면 즉발
- `AutoIntervalSec` — `AutoInterval` 트리거에서 다음 발동까지 간격
- `Damage`/`Range`/`Angle` — 어빌리티 타입별 해석 다름 (Damage=틱 또는 1회, Angle=부채꼴 라디안 반각)
- `MaxInstances` — 동시 배치 최대 (지뢰 5, 거미드론 3, 메테오 999)
- `RequiredAbilityId` — 선행 해금 어빌리티. 빈 칸이면 즉시 해금 가능

### 시트화 안 되는 필드

`_icon` (Sprite), `_vfxPrefab`, `_useSfx` 만 Unity 직접 바인딩.

### 어빌리티 9종 (현재 값)

| AbilityId | Char | Slot | Type | Trigger | CD | Dur | Auto | Dmg | Range | Max | Required |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `jinus_drone` | jinus | 1 | Drone | Manual | 2 | 0 | 0 | 0.8 | 10 | 5 | |
| `jinus_mining_drone` | jinus | 2 | MiningDrone | Manual | 2 | 10 | 0 | 0 | 0 | 1 | jinus_drone |
| `jinus_spider_drone` | jinus | 3 | SpiderDrone | AutoInterval | 0 | 0 | 3 | 0 | 12 | 3 | jinus_mining_drone |
| `sara_blackhole` | sara | 1 | BlackHole | Manual | 30 | 10 | 0 | 0 | 15 | 1 | |
| `sara_shockwave` | sara | 2 | Shockwave | Manual | 1 | 0 | 0 | 0 | 36 | 1 | sara_blackhole |
| `sara_meteor` | sara | 3 | Meteor | AutoInterval | 0 | 15 | 10 | 0.5 | 5.5 | 999 | sara_shockwave |
| `victor_napalm` | victor | 1 | Napalm | Manual | 40 | 20 | 0 | 0.5 | 4 | 1 | |
| `victor_flame` | victor | 2 | Flame | Manual | 20 | 5 | 0 | 10.8 | 18 | 1 | victor_napalm |
| `victor_mine` | victor | 3 | Mine | Manual | 10 | 0 | 0 | 0 | 0 | 5 | victor_napalm |

---

## 9. BossData 시트

거미 보스 튜닝 1행. v2 시스템: [Sys-Boss.md](Sys-Boss.md). 컬럼:

`BossId`, `DisplayName`, `MaxHp`, `ContactDamagePerSecond`, `ContactRange`, `KillThreshold`,
`WalkDuration`, `WalkRadius`, `WalkSpeed`, `IdleDuration`, `PerchJitter`, `JumpDurationMin`, `JumpDurationMax`,
`AttackDuration`, `AttackSpawnFraction`, `ChildCountPerLanding`, `ChildSpawnJitter`,
`TelegraphCooldownCycles`, `TelegraphDuration`, `InterruptHitsRequired`, `PounceRadiusMultiplier`, `PounceImpactDamage`, `FlinchDuration`, `TelegraphScalePulse`, `TelegraphPulseFreq`, `HpBarLowThreshold`.

### 시트화 안 되는 필드

`Prefab` 참조 (Animator/Avatar/Mesh/Material), `_childBugData` (SimpleBugData SO), VFX 프리팹, `_fxSocket` Transform — 모두 Unity 인스펙터에서 직접 바인딩.

### BossId ↔ 파일명 매핑

Importer 는 SO 내부 `BossId` 필드로 매칭 (파일명 무관). 새 BossId 추가 시 `Boss_<BossId>.asset` 자동 생성.

### 현재 값

| BossId | DisplayName | MaxHp | KillThreshold | WalkDuration | IdleDuration | TelegraphCooldownCycles | InterruptHitsRequired | PounceImpactDamage |
|---|---|---|---|---|---|---|---|---|
| `spider` | 거미 보스 | 500 | 250 | 1.5 | 2.0 | 2 | 8 | 50 |

전체 25개 컬럼 + 기본값: [archive/_review_initial_sheet_data/BossData.csv](archive/_review_initial_sheet_data/BossData.csv) 참조.

### 필드 의미 요약

- **Stats** — `MaxHp`/`ContactDamagePerSecond`/`ContactRange` (머신 접촉 피해 반경)
- **Spawn** — `KillThreshold` (이 점수 누적 시 등장)
- **Movement** — Walk/Idle 시간 + 점프 거리(Jitter/Min/Max) + 속도
- **Attack** — 공격 모션 길이 + 새끼 소환 타이밍 (`AttackSpawnFraction` 0~1)
- **Telegraph** — N사이클마다 발동 압박 패턴, 인터럽트 hit 수, Pounce 임팩트 데미지
- **HP Bar** — 빨강 전환 임계 비율

상세는 [Sys-Boss.md](Sys-Boss.md) §3.

---

## 10. Unity 에서 Import

### 열기

메뉴 `Tools > Drill-Corp > 4. 데이터 Import > Google Sheets Importer`.

### 인증

창이 열리면 `Assets/_Game/Data/Credentials/google-credentials.json` 기반으로 자동 인증. "✓ 인증됨" 표시 확인.

### 프리뷰 (선택)

`Load Preview` → 각 시트 탭 데이터 확인. 문제 있으면 시트에서 수정 후 다시 Load.

### Import 실행

- `Import All Data` — 8개 시트 순서대로 (SimpleBug → Machine → Upgrade → Wave → Weapon → WeaponUpgrade → Ability → Character)
- 시트별 버튼 — `SimpleBugData`, `WaveData`, `MachineData`, `UpgradeData`, `WeaponData`, `WeaponUpgradeData`, `CharacterData`, `AbilityData`

### 결과 경로

- `Assets/_Game/Data/Bugs/SimpleBug_<BugName>.asset`
- `Assets/_Game/Data/Waves/Wave_<NN>.asset`
- `Assets/_Game/Data/Machines/Machine_<Name>.asset`
- `Assets/_Game/Data/Upgrades/Upgrade_<Id>.asset`

### 보존 정책

- `Prefab`, `VFX`, Tint 커스텀 등 **런타임 바인딩 필드는 Import 시 덮어쓰지 않음**. 수치만 갱신.
- `BaseHp`/`BaseSpeed` 같은 필수 수치 컬럼이 비면 기존 값 유지. 명시적 0 필요하면 `0` 입력.

---

## 6. 기획자 튜닝 워크플로우

### 일상 튜닝 (수치만)

1. Google Sheets 에서 셀 수정 (예: Normal `BaseHp` 2 → 3)
2. Unity `Tools / Drill-Corp / 4. 데이터 Import / Google Sheets Importer` → `Import All Data`
3. Play — 즉시 반영

### 새 벌레 종류 추가

1. 시트 `SimpleBugData` 탭에 새 행 추가 (예: `BugName=Bomber`)
2. Unity 에서 프리펩 준비 (Simple/ 아래 패턴 참고)
3. Import 실행 → 신규 `SimpleBug_Bomber.asset` 자동 생성 + 수치 주입
4. 새로 생긴 SO 열어서 `Prefab` 필드 수동 바인딩
5. Spawner 또는 Tunnel 의 어느 슬롯에 배정할지 결정 후 참조 연결

### 웨이브 밸런스 조정

1. `WaveData` 탭에서 수치 수정 (`KillTarget` / `NormalSpawnInterval` 등)
2. 폴백으로 되돌리려면 셀을 비우거나 `-1` 입력
3. Import → Play

### 하지 말아야 할 것

- **Unity 인스펙터에서 수치 직접 변경** → 다음 Import 에 덮어씀. 수치는 **시트에서만** 편집.
- **시트에 Prefab/VFX 컬럼 추가** → Import 가 SO 필드를 건드리지 않는 정책 위배.
- **`SpawnConfig.asset` 의 `TunnelGameTimeStart`/`TunnelSpawnInterval` 등을 시트로 빼기** → 튜닝 빈도 낮아 시트 남용. 필요해지면 그때 WaveData 컬럼 추가.

---

## 7. 주의사항

### 반드시 지켜야 할 규칙

1. **시트 이름 변경 금지** — `SimpleBugData`, `WaveData`, `MachineData`, `UpgradeData` 정확히 일치
2. **헤더 행 유지** — 첫 번째 행은 컬럼 이름 (삭제/수정 금지)
3. **컬럼 이름 대소문자 구분**
4. **데이터 중간 빈 행 주의** — 그 이후 데이터가 무시될 수 있음 (첫 셀이 비면 해당 행 스킵)

### 데이터 타입별 입력

| 타입 | 입력 방법 | 예시 |
|---|---|---|
| 정수 | 숫자만 | 1, 10, 100 |
| 실수 | 소수점 사용 가능 | 1.5, 0.083 |
| 문자열 | 텍스트 그대로 | Normal, 땅굴 출현 |
| 불리언 | TRUE / FALSE | TRUE |
| 색상 | `#RRGGBB` 또는 `#RRGGBBAA` | #51CF66 |

### 문제 해결

| 증상 | 원인 | 해결 |
|---|---|---|
| "인증 실패" | credentials 파일 없음 | `Assets/_Game/Data/Credentials/google-credentials.json` 경로 확인 |
| "데이터가 안 나옴" | 시트 이름 불일치 | 시트 탭 이름을 정확히 (`SimpleBugData` 등) |
| 일부 컬럼 누락 | 컬럼 이름 오타 | 대소문자·공백 확인 |
| Import 후 Prefab 비어있음 | (의도된 동작) | SO 열어서 Prefab 수동 바인딩 |
| Import 후 수치 0 | 셀 서식 오류 | 숫자 서식으로 재설정 |
| `WaveData` EliteSpawnInterval=-1 인데 엘리트 나옴 | 기대 동작 아님 | 해당 웨이브 인스펙터 확인 후 `0` 로 명시 |
| 새 BugName 추가했는데 생성 안 됨 | 시트 첫 셀(BugName) 비었거나 헤더 불일치 | 로그 확인 후 헤더·값 재확인 |

---

## 8. 변경 이력

| 날짜 | 변경 |
|---|---|
| 2024-01 | 최초 작성 |
| 2026-04-06 | BugData 시트에 행동 컬럼 추가 (Movement/Attack/Passive/Skill/Trigger) |
| 2026-04-23 | **SimpleBug 전면 교체** — `BugData`/`WaveSpawnGroups`/`BugBehaviors` 삭제, `SimpleBugData`/`WaveData` (신 스키마) 신설. 웨이브 전환을 시간 → 킬 점수 기반으로 변경 |

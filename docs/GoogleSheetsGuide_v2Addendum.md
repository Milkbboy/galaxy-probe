# 구글시트 스키마 확장 (v2 부록)

> 최종 갱신: 2026-04-17
> 근거 프로토타입: `docs/v2.html`
> 상위 문서: [V2_IntegrationPlan.md](V2_IntegrationPlan.md)
> 기반 가이드: [GoogleSheetsGuide.md](GoogleSheetsGuide.md) (기존 5시트)

## 0. 이 문서의 역할

v2 아웃게임 통합을 위해 기존 스프레드시트에 **추가·확장**할 시트 스키마를 정의.
기획자가 시트에서 직접 캐릭터/무기/어빌리티를 튜닝할 수 있도록 한다.

### 전체 시트 현황

| 시트 이름 | 상태 |
|---|---|
| `BugData` | ✅ 기존 유지 |
| `WaveData` | ✅ 기존 유지 |
| `WaveSpawnGroups` | ✅ 기존 유지 |
| `MachineData` | 🔄 **1컬럼 추가** (`BaseMiningTarget`) |
| `UpgradeData` | 🔄 **2컬럼 추가** (`BaseCostGem`) + 타입 3종 추가 |
| `CharacterData` | ✨ **신규** |
| `WeaponData` | ✨ **신규** (기존 Weapon SO의 SSoT) |
| `WeaponUpgradeData` | ✨ **신규** |
| `AbilityData` | ✨ **신규** |

---

## 1. MachineData (기존, 1컬럼 추가)

기존 스키마에 다음 컬럼 1개 추가. 다른 컬럼 변경 없음.

| 컬럼명 | 타입 | 필수 | 설명 | 예시 |
|---|---|---|---|---|
| **BaseMiningTarget** | 실수 | O | 세션 승리를 위한 기본 채굴량 | 100 |

### 예시

| MachineId | MachineName | MaxHealth | MaxFuel | MiningRate | **BaseMiningTarget** | … |
|---|---|---|---|---|---|---|
| 1 | Default | 100 | 60 | 10 | **100** | … |
| 2 | Heavy | 150 | 45 | 8 | **80** | … |
| 3 | Speed | 80 | 90 | 14 | **140** | … |

---

## 2. UpgradeData (기존, 확장)

### 2.1 컬럼 추가

| 컬럼명 | 타입 | 필수 | 설명 | 예시 |
|---|---|---|---|---|
| **BaseCostOre** | 정수 | △ | **기존 `BaseCost`를 리네임** | 100 |
| **BaseCostGem** | 정수 | | 보석 비용 (0이면 광석만) | 0, 15, 30 |

> **마이그레이션**: 기존 시트의 `BaseCost` 컬럼명을 `BaseCostOre`로 변경. `BaseCostGem` 컬럼을 새로 추가하고 기본값 0. GoogleSheetsImporter는 `BaseCost`도 레거시로 읽어서 `BaseCostOre`에 매핑.

### 2.2 UpgradeType 신규 값 3종

| Type | 설명 | 권장 ValuePerLevel | IsPercentage |
|---|---|---|---|
| **MiningTarget** | 세션 목표 채굴량 증가 | +50 | false |
| **GemDropRate** | 보석 드랍 확률 증가 | +0.02 | false (0~1 소수) |
| **GemCollectSpeed** | 보석 채집 속도 증가 | +0.20 | true |

### 2.3 v2 권장 UpgradeData 행

| UpgradeId | DisplayName | UpgradeType | MaxLevel | ValuePerLevel | IsPercentage | BaseCostOre | BaseCostGem | CostMultiplier |
|---|---|---|---|---|---|---|---|---|
| `mine_speed` | 채굴 속도 향상 | MiningRate | 5 | 2 | false | 80 | 0 | 2.0 |
| `mine_target` | 목표량 확장 | MiningTarget | 5 | 50 | false | 100 | 0 | 2.0 |
| `excavator_hp` | 굴착기 내구도 | MaxHealth | 5 | 30 | false | 60 | 0 | 2.2 |
| `excavator_armor` | 장갑 강화 | Armor | 3 | 0.15 | true | 150 | 0 | 2.0 |
| `gem_drop` | 보석 출현 확률 | GemDropRate | 5 | 0.02 | false | 0 | 15 | 2.0 |
| `gem_speed` | 보석 채집 속도 | GemCollectSpeed | 5 | 0.20 | true | 0 | 10 | 2.2 |

> v2의 수동 비용 표(예: `mine_speed`는 80 / 160 / 280 / 440 / 640)에 맞추려면 `CostMultiplier`를 살짝 튜닝하거나, 필요 시 `ManualCosts` 컬럼을 별도 구성. 초안은 공식 비용 사용.

---

## 3. CharacterData (신규)

### 3.1 컬럼 구조

| 컬럼명 | 타입 | 필수 | 설명 | 예시 |
|---|---|---|---|---|
| **CharacterId** | 문자열 | O | 고유 ID (소문자, 영문) | victor / sara / jinus |
| **DisplayName** | 문자열 | O | 표시 이름 | 빅터 |
| **Title** | 문자열 | | 칭호 | 중장비 전문가 |
| **Description** | 문자열 | | 캐릭터 설명 | 네이팜·화염방사기·지뢰로 화력을 극대화 |
| **ColorHex** | 문자열 | O | 테마 컬러 #RRGGBB | #F4A423 |
| **DefaultMachineId** | 정수 | O | 기본 머신 참조 (MachineData.MachineId) | 1 |

### 3.2 예시

| CharacterId | DisplayName | Title | Description | ColorHex | DefaultMachineId |
|---|---|---|---|---|---|
| victor | 빅터 | 중장비 전문가 | 네이팜·화염방사기·지뢰로 화력을 극대화 | #F4A423 | 1 |
| sara | 사라 | 방어 전문가 | 블랙홀·충격파·반중력 메테오로 전장을 제어 | #4FC3F7 | 2 |
| jinus | 지누스 | 채굴 전문가 | 드론 포탑·채굴 드론·드론 거미로 자원 장악 | #51CF66 | 3 |

### 3.3 Import 결과

`Assets/_Game/Data/Characters/Character_Victor.asset` 등 생성.
어빌리티 연결은 `AbilityData` Import 이후 자동으로 `CharacterData.Abilities[]`를 채움 (AbilityData에 CharacterId + SlotKey로).

---

## 4. WeaponData (신규 — 기존 Weapon SO의 SSoT)

> 현재는 `Weapon_Sniper.asset` 등 Unity SO로 직접 편집 중. 시트로 이관하여 밸런스 일관성 확보.

### 4.1 컬럼 구조

| 컬럼명 | 타입 | 필수 | 설명 | 예시 |
|---|---|---|---|---|
| **WeaponId** | 문자열 | O | 고유 ID | sniper, bomb, gun, laser, saw |
| **DisplayName** | 문자열 | O | 표시 이름 | 저격총 |
| **Description** | 문자열 | | 설명 | 범위 내 자동 저격 |
| **BaseDamage** | 실수 | O | 기본 데미지 | 1.0 |
| **BaseCooldownSec** | 실수 | O | 기본 발사 간격 (초) | 0.4 (v2 24 frames) |
| **BaseRange** | 실수 | | 사거리 | 24 |
| **BaseRadius** | 실수 | | 폭발/블레이드 반경 | 110 (bomb) / 18 (saw) |
| **BaseAmmo** | 정수 | | 탄창 (gun만) | 40 |
| **BaseReloadSec** | 실수 | | 재장전 (gun만, 초) | 5.0 |
| **BaseDurationSec** | 실수 | | 발사 지속 (laser 6초 / saw 계속) | 6.0 |
| **BaseSlow** | 실수 | | 슬로우 강도 (saw만) | 0.3 |
| **UnlockedByDefault** | 불리언 | O | 기본 해금 여부 | TRUE (sniper만) / FALSE |
| **UnlockGemCost** | 정수 | | 해금 비용 | 0 / 30 / 20 / 40 / 40 |
| **UnlockRequiredWeaponId** | 문자열 | | 해금 선행 무기 | (빈) / sniper / bomb / gun / laser |

### 4.2 예시

| WeaponId | DisplayName | BaseDamage | BaseCooldownSec | BaseRange | BaseRadius | BaseAmmo | BaseReloadSec | BaseDurationSec | BaseSlow | UnlockedByDefault | UnlockGemCost | UnlockRequiredWeaponId |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| sniper | 저격총 | 1.0 | 0.4 | 24 | 0 | 0 | 0 | 0 | 0 | TRUE | 0 | |
| bomb | 폭탄 | 3.0 | 6.0 | 0 | 110 | 0 | 0 | 0 | 0 | FALSE | 30 | |
| gun | 기관총 | 0.5 | 0.14 | 0 | 0 | 40 | 5.0 | 0 | 0 | FALSE | 20 | bomb |
| laser | 레이저 | 0.8 | 5.0 | 28.8 | 0 | 0 | 0 | 6.0 | 0 | FALSE | 40 | gun |
| saw | 회전톱날 | 0.15 | 0 | 0 | 18 | 0 | 0 | 0 | 0.3 | FALSE | 40 | laser |

> 프레임 단위 스탯을 **초 단위로 변환**해서 입력. v2 값은 `docs/v2.html` 675줄 `BASE` 객체 참조.

### 4.3 Import 규칙

WeaponId별로 해당 Unity SO 타입에 매핑:

| WeaponId | SO 타입 |
|---|---|
| sniper | `SniperWeaponData` (또는 기존 `LockOnData` 재활용) |
| bomb | `BombWeaponData` |
| gun | `GunWeaponData` (기존 `BurstGunData` 재활용 가능) |
| laser | `LaserBeamData` |
| saw | `SawWeaponData` |

Import 시 `Assets/_Game/Data/Weapons/Weapon_<WeaponId>.asset`에 반영.

---

## 5. WeaponUpgradeData (신규)

### 5.1 컬럼 구조

| 컬럼명 | 타입 | 필수 | 설명 | 예시 |
|---|---|---|---|---|
| **UpgradeId** | 문자열 | O | 고유 ID | sniper_dmg |
| **WeaponId** | 문자열 | O | 대상 무기 | sniper |
| **DisplayName** | 문자열 | O | 표시 이름 | 저격총 데미지 |
| **TargetStat** | enum | O | Damage / Range / Cooldown / AmmoBonus / ReloadTime / Radius / SlowBonus |
| **MaxLevel** | 정수 | O | 최대 레벨 | 5 |
| **ValuePerLevel** | 실수 | O | 레벨당 증가량 | 0.25 |
| **IsPercentage** | 불리언 | | 퍼센트 적용 여부 | TRUE |
| **Operation** | enum | | Add / Multiply | Multiply |
| **BaseCostOre** | 정수 | O | 1레벨 광석 비용 | 40 |
| **BaseCostGem** | 정수 | | 1레벨 보석 비용 | 2 |
| **OreCostMultiplier** | 실수 | | 레벨당 비용 배율 | 2.0 |
| **GemCostMultiplier** | 실수 | | 레벨당 비용 배율 | 2.0 |

### 5.2 예시 (v2 전체 15행)

| UpgradeId | WeaponId | DisplayName | TargetStat | MaxLevel | ValuePerLevel | IsPercentage | Operation | BaseCostOre | BaseCostGem | OreCostMultiplier | GemCostMultiplier |
|---|---|---|---|---|---|---|---|---|---|---|---|
| sniper_dmg | sniper | 저격총 데미지 | Damage | 5 | 0.25 | TRUE | Multiply | 40 | 2 | 2.0 | 2.0 |
| sniper_range | sniper | 저격총 범위 | Range | 3 | 0.20 | TRUE | Multiply | 55 | 3 | 2.1 | 2.0 |
| sniper_cd | sniper | 저격총 연사 | Cooldown | 4 | -0.20 | TRUE | Multiply | 45 | 2 | 2.1 | 2.2 |
| bomb_dmg | bomb | 폭탄 데미지 | Damage | 4 | 0.30 | TRUE | Multiply | 60 | 4 | 2.0 | 2.0 |
| bomb_radius | bomb | 폭탄 범위 | Radius | 4 | 0.20 | TRUE | Multiply | 55 | 3 | 2.1 | 2.0 |
| bomb_cd | bomb | 폭탄 쿨타임 | Cooldown | 4 | -0.15 | TRUE | Multiply | 45 | 3 | 2.0 | 2.0 |
| gun_dmg | gun | 기관총 데미지 | Damage | 5 | 0.25 | TRUE | Multiply | 70 | 5 | 2.0 | 2.0 |
| gun_ammo | gun | 탄창 증가 | AmmoBonus | 4 | 10 | FALSE | Add | 55 | 4 | 2.1 | 2.0 |
| gun_reload | gun | 재장전 단축 | ReloadTime | 4 | -0.20 | TRUE | Multiply | 50 | 3 | 2.1 | 2.2 |
| laser_dmg | laser | 레이저 데미지 | Damage | 5 | 0.25 | TRUE | Multiply | 85 | 6 | 2.0 | 2.0 |
| laser_range | laser | 레이저 범위 | Range | 4 | 0.20 | TRUE | Multiply | 70 | 5 | 2.1 | 2.0 |
| laser_cd | laser | 레이저 쿨타임 | Cooldown | 4 | -0.15 | TRUE | Multiply | 60 | 4 | 2.1 | 2.2 |
| saw_dmg | saw | 톱날 데미지 | Damage | 5 | 0.20 | TRUE | Multiply | 85 | 7 | 2.0 | 2.0 |
| saw_radius | saw | 톱날 사거리 | Radius | 4 | 0.25 | TRUE | Multiply | 80 | 6 | 2.1 | 2.0 |
| saw_slow | saw | 슬로우 강화 | SlowBonus | 3 | 0.20 | FALSE | Add | 95 | 8 | 2.1 | 2.0 |

> **Cooldown 업그레이드는 `ValuePerLevel`을 음수**로. 예: `-0.20` → 레벨당 20% 감소. 계산: `effectiveCooldown = base × (1 + ValuePerLevel × level)`.
> **SlowBonus는 누적 덧셈**이며 `SawWeapon` 구현에서 `min(0.9, baseSlow + slowBonus)`로 상한 적용.

---

## 6. AbilityData (신규)

### 6.1 컬럼 구조

| 컬럼명 | 타입 | 필수 | 설명 | 예시 |
|---|---|---|---|---|
| **AbilityId** | 문자열 | O | 고유 ID | victor_napalm |
| **CharacterId** | 문자열 | O | 소유 캐릭터 | victor |
| **DisplayName** | 문자열 | O | 표시 이름 | 네이팜 탄 |
| **IconEmoji** | 문자열 | | 이모지 아이콘 | 🔥 |
| **SlotKey** | 정수 | O | 인게임 키 1/2/3 | 1 |
| **AbilityType** | enum | O | 실행 로직 분기 (Napalm / Flame / Mine / BlackHole / Shockwave / Meteor / Drone / MiningDrone / SpiderDrone) | Napalm |
| **Trigger** | enum | | Manual / AutoInterval | Manual |
| **CooldownSec** | 실수 | O | 쿨다운 (초) | 40 |
| **DurationSec** | 실수 | | 지속 시간 (초) | 20 |
| **AutoIntervalSec** | 실수 | | 자동 발동 주기 (초) | 0 / 10 |
| **Damage** | 실수 | | 데미지 (틱 또는 1회) | 0.5 |
| **Range** | 실수 | | 범위/반경/사거리 | 42 |
| **Angle** | 실수 | | 부채꼴 각도 (rad) | 0.35 |
| **MaxInstances** | 정수 | | 동시 배치 최대 수 | 1 / 3 / 5 |
| **UnlockGemCost** | 정수 | O | 해금 비용 | 30 |
| **RequiredAbilityId** | 문자열 | | 선행 어빌리티 | (빈) / victor_napalm |

### 6.2 예시 (9행)

| AbilityId | CharacterId | DisplayName | Emoji | SlotKey | AbilityType | Trigger | CooldownSec | DurationSec | AutoIntervalSec | Damage | Range | Angle | MaxInstances | UnlockGemCost | RequiredAbilityId |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| victor_napalm | victor | 네이팜 탄 | 🔥 | 1 | Napalm | Manual | 40 | 20 | 0 | 0.5 | 42 | 0 | 1 | 30 | |
| victor_flame | victor | 화염방사기 | 🔆 | 2 | Flame | Manual | 20 | 5 | 0 | 10.8 | 18 | 0.35 | 1 | 30 | victor_napalm |
| victor_mine | victor | 폭발지뢰 | 💣 | 3 | Mine | Manual | 10 | 0 | 0 | 0 | 0 | 0 | 5 | 30 | victor_napalm |
| sara_blackhole | sara | 블랙홀 | 🌀 | 1 | BlackHole | Manual | 30 | 10 | 0 | 0 | 18 | 0 | 1 | 30 | |
| sara_shockwave | sara | 충격파 | 💥 | 2 | Shockwave | Manual | 50 | 0 | 0 | 0 | 36 | 0 | 1 | 30 | sara_blackhole |
| sara_meteor | sara | 반중력 메테오 | ☄ | 3 | Meteor | AutoInterval | 0 | 15 | 10 | 0.5 | 5.5 | 0 | 999 | 30 | sara_shockwave |
| jinus_drone | jinus | 드론 포탑 | 🚁 | 1 | Drone | Manual | 20 | 0 | 0 | 0.8 | 10 | 0 | 5 | 30 | |
| jinus_mining_drone | jinus | 채굴 드론 | ⛏ | 2 | MiningDrone | Manual | 30 | 10 | 0 | 0 | 0 | 0 | 1 | 30 | jinus_drone |
| jinus_spider_drone | jinus | 드론 거미 | 🕷 | 3 | SpiderDrone | AutoInterval | 0 | 0 | 10 | 0 | 12 | 0 | 3 | 30 | jinus_mining_drone |

> 모든 거리는 **유닛 스케일**(1 유닛 = v2의 10 픽셀로 환산했다면 조정 필요). 초안은 v2 픽셀값을 그대로 넣고 플레이테스트로 조정.

### 6.3 Damage는 "단위 시간당"인가 "1회"인가

| AbilityType | Damage 해석 |
|---|---|
| Napalm, Meteor | 0.1초 틱당 데미지 |
| Flame | **초당 데미지** (v2 0.18/frame × 60) |
| Mine | 단발 폭발 (Range 0이면 폭탄 무기의 `radius × 0.5` 자동 사용) |
| BlackHole, Shockwave | 데미지 없음 (CC) |
| Drone, SpiderDrone | 발사체 1발당 데미지 |
| MiningDrone | 데미지 없음 (자원 생성) |

---

## 7. Import 실행 순서

기존 `Import All Data` 버튼에 다음 순서 보장:

1. **BugData** (행동 SO 생성)
2. **MachineData** (신규 컬럼 포함)
3. **CharacterData** (DefaultMachineId로 머신 참조)
4. **WeaponData** (기본 해금 상태)
5. **WeaponUpgradeData** (WeaponId로 무기 참조)
6. **AbilityData** (CharacterId로 캐릭터 참조)
7. **UpgradeData** (재화 필드 확장 포함)
8. **WaveData** + **WaveSpawnGroups**

의존성 순서를 따르면 참조 깨짐 방지.

### 7.1 링크 후처리

Import 완료 후 두 단계 후처리:

1. **CharacterData.Abilities[]**를 `AbilityData.CharacterId + SlotKey`로 자동 채움
2. **WeaponData.RequiredWeapon** SO 참조를 `UnlockRequiredWeaponId`로 자동 링크

→ `GoogleSheetsImporter`에 `PostProcessLinks()` 단계 추가.

---

## 8. 신규 Enum (Importer 파싱)

### 8.1 AbilityType

```
Napalm, Flame, Mine, BlackHole, Shockwave, Meteor, Drone, MiningDrone, SpiderDrone
```

### 8.2 AbilityTrigger

```
Manual, AutoInterval
```

### 8.3 WeaponUpgradeStat

```
Damage, Range, Cooldown, AmmoBonus, ReloadTime, Radius, SlowBonus
```

### 8.4 WeaponUpgradeOp

```
Add, Multiply
```

파싱 시 대소문자 무시 권장 (`"manual"` / `"Manual"` 둘 다 허용).

---

## 9. 튜닝 워크플로우

1. **시트에서 숫자만 수정** (예: `sniper_dmg`의 ValuePerLevel을 0.25 → 0.30)
2. Unity에서 `Tools > Drill-Corp > Google Sheets Importer` → `Import All`
3. Play 버튼으로 즉시 확인 (SO 에셋이 갱신되어 반영됨)
4. 만족스러우면 시트 그대로 유지, 아니면 1단계로 복귀

> **주의**: 수동으로 SO 에셋을 편집한 내용은 Import 시 덮어쓰임. 밸런스 관련 수정은 반드시 시트에서.

---

## 10. 변경 이력

| 날짜 | 내용 |
|---|---|
| 2026-04-17 | 초안 작성 (v2.html 기반). CharacterData/WeaponData/WeaponUpgradeData/AbilityData 신규 + UpgradeData 이중 재화 확장 |

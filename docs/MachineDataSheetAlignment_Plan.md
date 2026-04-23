# MachineData / 머신 강화 UpgradeData 시트 정렬 계획

> 작성: 2026-04-23
> 상위 문서: [GoogleSheetsGuide.md](GoogleSheetsGuide.md) · [GoogleSheetsGuide_v2Addendum.md](GoogleSheetsGuide_v2Addendum.md) · [DataStructure.md](DataStructure.md)
> 배경 프로토타입: `docs/v2.html` §굴착기 강화

---

## 1. 배경

v2 스펙은 **머신 스탯(MachineData)** + **머신 관련 강화 6종(UpgradeData)** 을 정의한다. 현재 Unity 에셋·Importer·시트 3층 사이에 불일치가 쌓여있어, 기획자가 시트에서 튜닝해도 게임에 반영되지 않는 상태.

v2.html 원본 `calcStats()` (421~454줄) 기준 6종:

| UpgradeId | 효과 | MaxLv | ValuePerLevel | IsPercentage | 비용 schedule |
|---|---|---|---|---|---|
| `excavator_hp` | 최대 체력 +30/lv | 5 | 30 | false | 광석 [60,130,230,370,540] |
| `excavator_armor` | 받는 피해 -15%/lv | 3 | 0.15 | true | 광석 [150,300,500] |
| `mine_speed` | 초당 채굴 +2/lv | 5 | 2 | false | 광석 [80,160,280,440,640] |
| `mine_target` | 목표 채굴량 +50/lv | 5 | 50 | false | 광석 [100,200,350,550,800] |
| `gem_drop` | 보석 드랍 확률 +2%p/lv | 5 | 0.02 | false | **보석** [15,30,50,75,105] |
| `gem_speed` | 보석 채집 속도 +20%/lv | 5 | 0.20 | true | **보석** [10,22,38,58,82] |

---

## 2. 현황 — 불일치 매핑

### 2.1 MachineData

| 층 | 상태 |
|---|---|
| **SO 클래스** (`MachineData.cs`) | ✅ v2 필드 반영 — `_baseMiningTarget` 존재. `_maxFuel`/`_fuelConsumeRate` 제거됨 |
| **에셋** (`Machine_Default/Heavy/Speed.asset`) | ✅ `_baseMiningTarget` 값 들어있음 (Default=100, 나머지 확인 필요) |
| **Importer** (`GoogleSheetsImporter.ImportMachineDataAsync`) | ❌ **`BaseMiningTarget` 컬럼 파싱 없음** — 시트에 컬럼 있어도 무시 |
| **시트 `MachineData` 탭** | ❓ 직접 확인 필요. v2Addendum §1 권장: `BaseMiningTarget` 1컬럼 추가. `MaxFuel` 컬럼은 읽히지 않으므로 두거나 제거 무관 |

### 2.2 UpgradeData

| 층 | 상태 |
|---|---|
| **SO 클래스** (`UpgradeData.cs`) | ✅ v2 필드 반영 — `BaseCostGem`, `OreCostSchedule`, `GemCostSchedule` 존재 추정. 타입 enum에 `MiningTarget`/`GemDropRate`/`GemCollectSpeed` 추가됨 |
| **에셋** (`Assets/_Game/Data/Upgrades/`) | ⚠️ **혼재 상태 16개** — 구 v1 레거시 11개 + v2 5개. `excavator_armor` 에셋 누락 |
| **Importer** (`ImportUpgradeDataAsync`) | ❌ 구 컬럼만 파싱 (`BaseCost` 단일, `BaseCostGem` / `OreCostSchedule` / `GemCostSchedule` 미지원). `UpgradeType` 파싱은 Enum.TryParse 로 v2 타입도 처리 가능 |
| **시트 `UpgradeData` 탭** | ❓ 직접 확인 필요. 레거시 에셋 11개가 잔존하는 것으로 보아 **구 v1 스키마**로 추정 |
| **UpgradeManager 씬 바인딩** | ❓ 인스펙터 `_availableUpgrades` 리스트에 어떤 에셋이 바인딩돼 있는지 확인 필요 |

### 2.3 현재 에셋 목록 상세

**구 v1 레거시 (삭제 대상 11개)**
`Upgrade_max_health.asset` (`max_health`), `Upgrade_Armor.asset` (`armor`), `Upgrade_max_fuel.asset`, `Upgrade_fuel_efficiency.asset`, `Upgrade_mining_rate.asset`, `Upgrade_health_regen.asset`, `Upgrade_attack_damage.asset`, `Upgrade_attack_range.asset`, `Upgrade_attack_speed.asset`, `Upgrade_crit_chance.asset`, `Upgrade_crit_damage.asset`

**v2 5종 (보존)**
`Upgrade_MaxHealth.asset` (`excavator_hp`), `Upgrade_MiningRate.asset` (`mine_speed`), `Upgrade_MiningTarget.asset` (`mine_target`), `Upgrade_GemDrop.asset` (`gem_drop`), `Upgrade_GemSpeed.asset` (`gem_speed`)

**누락 (신규 생성)**
`Upgrade_ExcavatorArmor.asset` (`excavator_armor`)

---

## 3. 목표 상태

완료 시점에 달성할 일관성:

1. **시트 `MachineData` 탭**: v2Addendum §1 스키마 — `BaseMiningTarget` 컬럼 포함. 3행 (Default/Heavy/Speed).
2. **시트 `UpgradeData` 탭**: v2Addendum §2.3 스키마 — v2 6종 (excavator_hp/armor, mine_speed/target, gem_drop/speed) + `BaseCostGem`/`OreCostSchedule`/`GemCostSchedule` 컬럼.
3. **Importer**: 위 두 시트의 모든 v2 컬럼 파싱. 기존 구 컬럼(`BaseCost`)은 fallback 지원하되 점진 폐기.
4. **에셋**: `Machines/` 3개 그대로. `Upgrades/` 폴더 v2 6개만 존재 (레거시 11개 삭제, `excavator_armor` 신규).
5. **UpgradeManager**: 씬 `_availableUpgrades` 가 v2 6개만 바인딩.
6. **ExcavatorUpgradeUI / GemUpgradeUI**: 각각 4종 / 2종 바인딩 확인.

---

## 4. 작업 단계

4단계로 나눠 각 단계 끝나면 Unity 컴파일·Play 재검증.

### Phase M-1 — Importer + SO 코드 확장 (코드 수정)

목적: 시트에서 v2 컬럼을 읽을 수 있게 먼저 만듦. 이 단계가 없으면 시트 업데이트해도 반영 안 됨.

**`Assets/_Game/Scripts/Data/UpgradeData.cs`**

- `CurrencyType` enum 신설:
  ```csharp
  public enum CurrencyType { Ore, Gem, Both }
  [SerializeField] private CurrencyType _currencyType = CurrencyType.Ore;
  public CurrencyType Currency => _currencyType;
  ```
- `GetCostsForLevel(int currentLevel)` 로직 갱신 — `CurrencyType` 이 authoritative:
  ```csharp
  int ore = (_currencyType == CurrencyType.Gem) ? 0 : GetCostForLevel(currentLevel);
  int gem = (_currencyType == CurrencyType.Ore) ? 0 : GetGemCostForLevel(currentLevel);
  return (ore, gem);
  ```

**`Assets/_Game/Scripts/Editor/GoogleSheetsImporter.cs`**

- `ImportMachineDataAsync`:
  - `_baseMiningTarget` 필드 `SetSerializedField(so, "_baseMiningTarget", GetFloatValue(row, headers, "BaseMiningTarget", 100f))` 추가
- `ImportUpgradeDataAsync`:
  - `_currencyType` enum 파싱 — `Enum.TryParse<UpgradeData.CurrencyType>(str, true, out var ct)` / `typeProp.enumValueIndex = (int)ct`
  - `_baseCost` 는 `BaseCostOre` 우선 → fallback `BaseCost` (하위 호환)
  - `_baseCostGem` 필드 `SetSerializedField(so, "_baseCostGem", GetIntValue(row, headers, "BaseCostGem", 0))` 추가
  - `_oreCostSchedule` / `_gemCostSchedule` 배열 파싱 헬퍼 신설 — 셀 형식: `"60|130|230|370|540"` (파이프 구분, 쉼표 회피 — CSV 파싱 충돌 방지). `SetSerializedIntArray(so, fieldName, value.Split('|'))`.
  - `UpgradeType` 파싱은 이미 `Enum.TryParse` 사용 중이라 `MiningTarget`/`GemDropRate`/`GemCollectSpeed` 자동 처리.

**`Assets/_Game/Scripts/OutGame/UpgradeManager.cs` 또는 `ExcavatorUpgradeUI`/`GemUpgradeUI`**

- 구매 검증 로직 — `CurrencyType == Both` 일 때 광석·보석 **양쪽 모두 충분해야** 구매 가능. 한쪽만 부족해도 실패.
- 가격 표시 로직 — `Ore` → "광석 N", `Gem` → "보석 N", `Both` → "광석 N + 보석 M".

### Phase M-2 — 시트 편집 (웹)

**MachineData 탭**
- 헤더 끝에 `BaseMiningTarget` 컬럼 추가 (기존 `CritMultiplier` 뒤가 자연스러움).
- 3행 값 입력: Default=100, Heavy=80, Speed=140 (v2Addendum §1 예시).

**UpgradeData 탭 전면 교체**
- 구 11행 전부 삭제 (or `_legacy_` rename).
- 헤더를 다음으로 교체:
  ```
  UpgradeId, DisplayName, Description, UpgradeType, MaxLevel, BaseValue, ValuePerLevel, IsPercentage, CurrencyType, BaseCostOre, BaseCostGem, CostMultiplier, OreCostSchedule, GemCostSchedule
  ```
- `CurrencyType` 값: `Ore` / `Gem` / `Both`. 구매 시 차감·가격 표시의 authoritative flag.
- 6행 입력 (붙여넣기 데이터는 `docs/_review/UpgradeData.tsv` 참조):
  | UpgradeId | UpgradeType | MaxLv | Value/Lv | IsPct | **CurrencyType** | BaseCostOre | BaseCostGem | OreCostSchedule | GemCostSchedule |
  |---|---|---|---|---|---|---|---|---|---|
  | `excavator_hp` | MaxHealth | 5 | 30 | FALSE | Ore | 60 | 0 | `60\|130\|230\|370\|540` | |
  | `excavator_armor` | Armor | 3 | 0.15 | TRUE | Ore | 150 | 0 | `150\|300\|500` | |
  | `mine_speed` | MiningRate | 5 | 2 | FALSE | Ore | 80 | 0 | `80\|160\|280\|440\|640` | |
  | `mine_target` | MiningTarget | 5 | 50 | FALSE | Ore | 100 | 0 | `100\|200\|350\|550\|800` | |
  | `gem_drop` | GemDropRate | 5 | 0.02 | FALSE | Gem | 0 | 15 | | `15\|30\|50\|75\|105` |
  | `gem_speed` | GemCollectSpeed | 5 | 0.20 | TRUE | Gem | 0 | 10 | | `10\|22\|38\|58\|82` |

> `Both` 예시 (가상): `CurrencyType=Both, BaseCostOre=200, BaseCostGem=50, OreCostSchedule=200|400|700, GemCostSchedule=50|100|200` — 광석과 보석을 동시에 차감. 한쪽만 부족해도 구매 실패.

> 구 컬럼 `BaseCost` 이름 그대로 두고 값만 넣어도 M-1의 fallback 덕분에 동작은 하나, 혼동 방지 위해 `BaseCostOre`로 rename 권장.

### Phase M-3 — 에셋 정리 + Import (Unity)

1. Unity 에디터에서 **레거시 11개 Upgrade 에셋 삭제**:
   - `Upgrade_max_health`, `Upgrade_Armor`, `Upgrade_max_fuel`, `Upgrade_fuel_efficiency`, `Upgrade_mining_rate`, `Upgrade_health_regen`, `Upgrade_attack_damage`, `Upgrade_attack_range`, `Upgrade_attack_speed`, `Upgrade_crit_chance`, `Upgrade_crit_damage`
   - 삭제 전 `UpgradeManager._availableUpgrades` 리스트 + `ExcavatorUpgradeUI._availableUpgrades` + `GemUpgradeUI._availableUpgrades` 인스펙터에서 참조 끊기.
2. `Tools / Drill-Corp / 4. 데이터 Import / Google Sheets Importer` → `Import All Data`.
3. 결과 확인:
   - `Machine_Default/Heavy/Speed.asset` 의 `_baseMiningTarget` 이 시트값으로 갱신됐는지.
   - `Upgrades/` 에 v2 6개만 남는지. `excavator_armor` 신규 생성 확인 (`Upgrade_excavator_armor.asset` 또는 같은 UpgradeId로 찾아지는 경로).
4. 시트 파일명 규칙상 `Upgrade_<UpgradeId>.asset` 형태로 생성됨 — 기존 PascalCase 파일명(`Upgrade_MaxHealth` 등)과 충돌 가능성 있음. Importer 매칭 기준을 **UpgradeId 기반**으로 확인 (현재 `AssetDatabase.LoadAssetAtPath(savePath + "Upgrade_" + upgradeId + ".asset")` 로 구현돼 있으므로 시트 `excavator_hp` 는 신규 `Upgrade_excavator_hp.asset` 을 만들고 기존 `Upgrade_MaxHealth.asset` 은 고아로 남음 → 고아된 구 PascalCase 에셋도 수동 삭제).

### Phase M-4 — 씬 바인딩 + Play 검증

1. Title 씬에서 `UpgradeManager` 컴포넌트 열고 `_availableUpgrades` 리스트를 v2 6개로 재바인딩.
2. `ExcavatorUpgradeUI._availableUpgrades`: `excavator_hp`, `excavator_armor`, `mine_speed`, `mine_target` (4개).
3. `GemUpgradeUI._availableUpgrades`: `gem_drop`, `gem_speed` (2개).
4. 씬 저장.
5. Play 테스트:
   - Title Hub 에서 굴착기 강화 패널 4종 / 보석 강화 패널 2종 표시 확인.
   - 레벨 업 시 비용 스케줄 (60→130→230→370→540 등) 맞는지.
   - Game 씬 진입 시 `MachineController` 가 `BaseMiningTarget + mine_target*50` 반영된 목표 채굴량으로 시작하는지.

---

## 5. 영향 범위

| 컴포넌트 | 영향 |
|---|---|
| `GoogleSheetsImporter.cs` | Machine/Upgrade 메서드 내부 필드 파싱 추가. 시그니처 불변 |
| `MachineData.cs`, `UpgradeData.cs` | 코드 수정 없음 (필드 이미 v2 반영됨). 단, 실제 필드명 재확인 필요 |
| `Assets/_Game/Data/Machines/` | 값만 Import 갱신. 에셋 추가/삭제 없음 |
| `Assets/_Game/Data/Upgrades/` | 11개 삭제 + 6개 재생성 (파일명 규칙 변화 가능) |
| `UpgradeManager.cs` | 코드 변경 없음. 씬 인스펙터 리바인딩만 |
| `ExcavatorUpgradeUI.cs`, `GemUpgradeUI.cs` | 코드 변경 없음. 리스트 재바인딩 |
| `MachineController.cs` | 영향 없음 (`BaseMiningTarget` 이미 사용 중) |

**씬 파일 (`Title.unity`, `Game.unity`)**: `UpgradeManager._availableUpgrades` 가 변경되므로 diff 나옴.

---

## 6. 주의사항

- **기획자 타이밍**: M-2 시트 편집을 M-1 Importer 코드 수정 **후에** 하는 것이 안전. 반대 순서로 하면 시트 저장 후 Import 돌렸을 때 `BaseMiningTarget` 무시돼 "왜 안 바뀌지?" 로 혼란.
- **BaseCost 리네임**: 기존 `BaseCost` 를 시트에서 `BaseCostOre` 로 바꾸면 타 시스템이 참조하는지 확인. Importer fallback 설계는 선택이나, 리네임 후 기존 이름 지우지 않는 "2개 유지" 는 지양 — SSoT 깨짐.
- **Schedule 배열 vs Multiplier**: 시트에 `OreCostSchedule` 을 채운 행은 `CostMultiplier` 를 무시 (or 우선 사용). Importer 파싱 시 정책 명확화 필요.
- **레거시 Upgrade 에셋 고아 방지**: M-3 에셋 삭제 전에 **참조 검색** (씬/프리펩에서). `AssetDatabase.FindReferences` 또는 Editor 콘솔의 "Find References In Scene". Missing Script 경고 방지.
- **PascalCase vs snake_case 파일명**: 현행 `Upgrade_MaxHealth.asset` (PascalCase) vs Importer가 만들 `Upgrade_excavator_hp.asset` (UpgradeId 기반). Import 후 PascalCase 쪽은 고아 상태로 남으므로 수동 삭제. GUID 참조가 있으면 (씬 등) 끊어짐 주의.

---

## 7. 체크리스트

**Phase M-1 (SO + Importer + Manager)**
- [ ] `UpgradeData.cs` 필드 명 확인 (`_baseCost`, `_baseCostGem`, `_oreCostSchedule`, `_gemCostSchedule` — 실제 이름 grep)
- [ ] `UpgradeData.cs` 에 `CurrencyType` enum(`Ore`/`Gem`/`Both`) + `_currencyType` 필드 신설, `GetCostsForLevel()` 이 CurrencyType 으로 Ore/Gem 게이팅
- [ ] `ImportMachineDataAsync` 에 `_baseMiningTarget` 파싱 추가
- [ ] `ImportUpgradeDataAsync` 에 `CurrencyType` enum 파싱 + `_baseCostGem`, `_oreCostSchedule`, `_gemCostSchedule` 파싱 추가 (+ `BaseCostOre` → `_baseCost` 우선, `BaseCost` fallback)
- [ ] `SetSerializedIntArray` 헬퍼 신설 or 기존 헬퍼 활용 (파이프 `|` 분할)
- [ ] `UpgradeManager.TryPurchase()` (또는 구매 로직 위치) — `Both` 시 광석·보석 양쪽 모두 충분한지 검증
- [ ] `ExcavatorUpgradeUI` / `GemUpgradeUI` 의 가격 표시 로직 — `Ore` → "광석 N", `Gem` → "보석 N", `Both` → "광석 N + 보석 M"
- [ ] Unity 컴파일 통과

**Phase M-2 (시트 웹)**
- [ ] `MachineData` 탭에 `BaseMiningTarget` 컬럼 추가, 3행 값 입력 (100/80/140)
- [ ] `UpgradeData` 탭 헤더 교체, 구 11행 삭제, v2 6행 입력
- [ ] `BaseCost` → `BaseCostOre` rename 여부 결정

**Phase M-3 (에셋 + Import)**
- [ ] Scene 인스펙터에서 레거시 Upgrade 참조 모두 제거
- [ ] 레거시 11개 `.asset` 삭제 (+ meta)
- [ ] `Import All Data` 실행
- [ ] 생성된 v2 6개 에셋 값 검증 (BaseCostGem, Schedule 배열 포함)
- [ ] 구 PascalCase 고아 에셋 (`Upgrade_MaxHealth/MiningRate/MiningTarget/GemDrop/GemSpeed`) 삭제

**Phase M-4 (씬 + Play)**
- [ ] `UpgradeManager._availableUpgrades` 재바인딩 (Title 씬)
- [ ] `ExcavatorUpgradeUI._availableUpgrades` 4개 재바인딩
- [ ] `GemUpgradeUI._availableUpgrades` 2개 재바인딩
- [ ] Title 씬 + Game 씬 저장
- [ ] Play 테스트 — Hub 강화 UI, Game 씬 MiningTarget 반영

**완료 후**
- [ ] CHANGELOG 업데이트
- [ ] `GoogleSheetsGuide.md` §4 (UpgradeData 섹션) + `GoogleSheetsGuide_v2Addendum.md` 에 실제 최종 스키마 반영
- [ ] 이 계획 문서 (`MachineDataSheetAlignment_Plan.md`) 를 `docs/archive/` 로 이동 or 삭제

# Drill-Corp 데이터 시트 입력 초안 (아카이브)

> 작성: 2026-04-23 · 아카이브 이동: 2026-04-27 (시트 통합 완료 후)
> 용도: Google Sheets 8 탭에 붙여넣은 **초기 데이터** 보존본. 시트가 SSoT 가 된 후 참고용.
> 스키마 문서: [Data-SheetsGuide.md](../../Data-SheetsGuide.md) · [Overview-DataStructure.md](../../Overview-DataStructure.md)

---

## 파일 목록

| 파일 | 대상 탭 | 스키마 섹션 |
|---|---|---|
| `SimpleBugData.csv` / `.tsv` | `SimpleBugData` | [Data-SheetsGuide.md §1](../../Data-SheetsGuide.md#1-simplebugdata-시트) |
| `WaveData.csv` / `.tsv`      | `WaveData`      | [Data-SheetsGuide.md §2](../../Data-SheetsGuide.md#2-wavedata-시트) |
| `MachineData.tsv`            | `MachineData`   | [Data-SheetsGuide.md §3](../../Data-SheetsGuide.md#3-machinedata-시트) |
| `UpgradeData.tsv`            | `UpgradeData`   | [Data-SheetsGuide.md §4](../../Data-SheetsGuide.md#4-upgradedata-시트) |
| `WeaponData.csv`             | `WeaponData`    | [Data-SheetsGuide.md §5](../../Data-SheetsGuide.md#5-weapondata-시트) — ExtraStats 한 셀 압축 (`key:value\|key:value`) |
| `WeaponUpgradeData.csv`      | `WeaponUpgradeData` | [Data-SheetsGuide.md §6](../../Data-SheetsGuide.md#6-weaponupgradedata-시트) — 무기 강화 15종 |
| `CharacterData.csv`          | `CharacterData` | [Data-SheetsGuide.md §7](../../Data-SheetsGuide.md#7-characterdata-시트) — 캐릭터 3종, MachineId/Ability1·2·3Id 로 SO 참조 |
| `AbilityData.csv`            | `AbilityData`   | [Data-SheetsGuide.md §8](../../Data-SheetsGuide.md#8-abilitydata-시트) — 어빌리티 9종, Cooldown/Damage/Range 등 밸런스 |
| `BossData.csv`               | `BossData`      | [Data-SheetsGuide.md §9](../../Data-SheetsGuide.md#9-bossdata-시트) — 거미 보스 1행, HP/movement/attack/telegraph |

> 모든 v2 컬럼(`BaseMiningTarget` / `BaseCostOre` / `BaseCostGem` / `OreCostSchedule` / `GemCostSchedule` / `WeaponData.ExtraStats` / `Character/Ability` 신규 등) Importer 파싱 완료 (커밋 `e26b3ef` 까지). 시트에 붙여넣고 Import 누르면 됨. 과거 정렬 계획은 `docs/archive/MachineDataSheetAlignment_Plan.md` 참조.

모든 파일은 **1행은 헤더 + 2행부터 실제 데이터**. Google Sheets에 이미 헤더 행이 만들어져 있으면 본문(2행 이후)만 붙여넣으면 됨.

---

## Google Sheets에 붙여넣는 방법

### TSV 파일 (`.tsv` — 탭 구분, 추천)

1. `.tsv` 파일을 **VS Code 등 텍스트 에디터**로 연다
2. 헤더 포함 전체 선택 (Ctrl+A) → Ctrl+C
3. Google Sheets 해당 탭에서 `A1` 셀 클릭 → `Ctrl+Shift+V` (서식 없이 붙여넣기)
4. **탭 구분이므로 자동으로 셀 분할됨** — 별도 설정 불필요. 스키마 열이 많은 `UpgradeData` (13컬럼) 에 특히 편리
5. 숫자·불리언이 자동 서식으로 인식되는지 확인 (오른쪽 정렬 = 숫자, 가운데 정렬 = 불리언)

### CSV 파일 (`.csv` — 쉼표 구분)

1. 텍스트 에디터로 열어 데이터 선택 → 복사
2. Sheets `A1` → `Ctrl+Shift+V`
3. 붙여넣기 직후 "텍스트를 열로 분할" 메뉴 뜨면 `쉼표` 선택

### 방법 B — 파일 가져오기

1. Google Sheets 상단 `파일 → 가져오기 → 업로드`
2. 파일 드래그 업로드
3. "가져오기 위치" → `현재 시트 교체` 또는 `새 시트 만들기`
4. 구분자: TSV면 `탭`, CSV면 `쉼표`

### 배열 컬럼 주의 (`OreCostSchedule`, `GemCostSchedule`)

`UpgradeData.tsv` 의 스케줄 컬럼은 **파이프(`|`) 구분 문자열** 1셀:
- 예: `60|130|230|370|540` ← 이대로 한 셀에 들어가야 함. 쉼표를 쓰지 않는 이유는 CSV 파싱과 충돌하기 때문
- Sheets가 파이프를 보고 열 분할하지 않으니 안전. 만약 숫자만 있는 셀로 잘못 해석되면 셀 앞에 작은따옴표 추가 (`'60|130|230|370|540`)

### 숫자가 문자열로 들어갔을 때

셀 왼쪽 위에 초록 삼각형이 뜨거나 값이 왼쪽 정렬되면 문자열입니다.
- 해당 열 전체 선택 → `서식 → 숫자 → 숫자` 적용
- 또는 `데이터 → 열 분할 텍스트` 한 번 실행

---

## 확인 포인트 (붙여넣은 뒤)

### SimpleBugData

- [ ] 3행이 `Normal / Elite / Swift` 순서로 들어갔는지
- [ ] `BaseHp`, `BaseSpeed` 등 숫자 컬럼이 **숫자 서식**인지 (셀 오른쪽 정렬 확인)
- [ ] `TintHex` 컬럼이 `#` 포함한 **문자열 서식**인지 (`#51CF66` 그대로)
  - 만약 구글 시트가 `#`을 수식 시작으로 오해하면 셀 앞에 작은따옴표(`'`) 붙여 `'#51CF66`로 입력

### WaveData

- [ ] 5행(Wave 1~5) 들어갔는지
- [ ] 헤더가 `KillTarget`인지 (이전 `WaveDuration` 아님)
- [ ] `TunnelEnabled` 컬럼이 `TRUE` / `FALSE` 대문자로 들어갔는지 (`참`/`거짓`이 아니라)

### MachineData

- [ ] **1행(Default) 만** 들어갔는지 — v2 는 단일 머신 (Heavy/Speed 는 v1 레거시, 삭제)
- [ ] `BaseMiningTarget` 컬럼이 `MiningBonus` 뒤, `BaseGemDropRate` 앞
- [ ] **`BaseGemDropRate`** 컬럼 신규 — 0.05 (v2.html `0.05 + gemDropBonus` 베이스라인)
- [ ] `MiningRate=5` (v2.html `baseMineRate=5` 일치). 구 10 이 아님
- [ ] 구 컬럼 `MaxFuel` / `FuelConsumeRate` 가 시트에 남아있다면 삭제 (Importer가 읽지 않음)

### UpgradeData

- [ ] 6행 (excavator_hp/armor, mine_speed/target, gem_drop/speed) 들어갔는지
- [ ] 구 v1 11행 (max_health, max_fuel, attack_damage, crit_*, fuel_* 등) 전부 삭제됐는지
- [ ] `UpgradeType` 컬럼에 v2 신규 타입 3종 (`MiningTarget`, `GemDropRate`, `GemCollectSpeed`) 이 철자 정확히 들어갔는지
- [ ] **`CurrencyType`** 컬럼 — `Ore` / `Gem` / `Both` 중 하나. excavator_*, mine_* 는 `Ore`, gem_* 는 `Gem`. 이 값이 구매 차감의 authoritative flag (BaseCostOre/Gem 에 값이 있어도 CurrencyType 에 포함 안 된 재화는 무시)
- [ ] `OreCostSchedule` / `GemCostSchedule` 가 `60|130|230|370|540` 처럼 **한 셀에 파이프 구분 문자열**로 들어갔는지
- [ ] `IsPercentage` 컬럼 — gem_drop 은 **FALSE** (누적 %p 가산), gem_speed 는 **TRUE** (배율)
- [ ] `-1` 값이 숫자로 들어갔는지 (음수 대쉬 깨짐 주의)
- [ ] `WaveName` 한글 깨짐 없는지

### WeaponData

- [ ] 5행 (sniper / bomb / gun / laser / saw) 들어갔는지
- [ ] `ThemeColorHex` 가 `#RRGGBB` 형태 — Sheets 가 `#` 을 수식 시작으로 오해하면 셀 앞에 `'` 추가
- [ ] `RequiredWeaponId` 가 다른 행의 `WeaponId` 값과 일치 (현재는 saw → gun 만 사용)
- [ ] `ExtraStats` 셀이 한 컬럼에 `key:value|key:value` 형식으로 들어갔는지 — 절대 분할되지 않게
- [ ] `UnlockedByDefault` 가 `TRUE`/`FALSE` 대문자

### WeaponUpgradeData

- [ ] 15행 (gun/sniper/bomb/laser/saw 각 3종) 들어갔는지
- [ ] `TargetStat` 철자 (`Damage`/`Range`/`Cooldown`/`AmmoBonus`/`ReloadTime`/`Radius`/`SlowBonus`)
- [ ] `Operation` 철자 (`Add` / `Multiply`)
- [ ] `ValuePerLevel` — Cooldown/ReloadTime 강화는 음수 (`-0.20` = 20% 단축)
- [ ] `ManualCostsOre` / `ManualCostsGem` 빈 칸이면 공식 자동 사용. 채우려면 둘 다 같은 길이 파이프 배열

### CharacterData

- [ ] 3행 (jinus / sara / victor) 들어갔는지
- [ ] `DefaultMachineName` 이 `MachineData._machineName` 과 일치 — v2 는 모두 `Default`
- [ ] `Ability1Id` / `Ability2Id` / `Ability3Id` 가 `AbilityData._abilityId` 와 일치 + `AbilityData._slotKey` 와 슬롯 번호 일치 (예: `Ability1Id=jinus_drone` → `jinus_drone._slotKey=1`)
- [ ] **Import 순서** — `Import All Data` 는 자동으로 Ability → Character 순. 시트별 단독 import 시 AbilityData 가 먼저 갱신돼 있어야 캐릭터의 어빌리티 SO 참조가 정상 바인딩

### AbilityData

- [ ] 9행 (캐릭터 3 × 슬롯 3) 들어갔는지
- [ ] `AbilityType` 철자 (`Napalm`/`Flame`/`Mine`/`BlackHole`/`Shockwave`/`Meteor`/`Drone`/`MiningDrone`/`SpiderDrone`)
- [ ] `Trigger` 철자 (`Manual` / `AutoInterval`)
- [ ] `IconEmoji` 가 한 셀에 이모지 1개 (구글 시트가 이모지 정상 렌더링)
- [ ] `RequiredAbilityId` 가 비었거나 다른 행의 `AbilityId` 와 일치 (다른 캐릭터 어빌리티 referencing 가능하지만 권장 X)
- [ ] `Damage`/`Range`/`Angle` 의미 — `AbilityType` 마다 다르게 해석되니 [Sys-Character.md](../../Sys-Character.md) 참조

---

## 값 해석 참고

### SimpleBugData.csv

현재 값은 `Assets/_Game/Data/Bugs/SimpleBug_Normal/Elit/Swift.asset`의 **현행 필드값 그대로**.
기획자가 튜닝할 출발점이지 최종값이 아님.

| BugName | 의도 |
|---|---|
| Normal | 기본 몸빵 벌레. 웨이브당 HP/속도 꾸준히 증가. |
| Elite  | 15초마다 한 마리. HP 10, 큰 덩치, 느리지만 맷집 담당. |
| Swift  | 땅굴에서 쏟아지는 극속 벌레. HP 1이지만 속도 3 유닛/초. |

### WaveData.csv

- **전환 트리거**: **벌레 처치 점수 누적**이 `KillTarget`에 도달하면 다음 웨이브로 전환 (시간 아님).
  Score는 SimpleBugData.Score 그대로 (Normal=1, Elite=5, Swift=0.5).
- **세션 종료와 무관**: 웨이브는 난이도 곡선일 뿐. 세션은 채굴 완료(승리) / 머신 HP 0(패배)로만 끝남.
- **Wave 1·2**: 엘리트 `-1` + TunnelEnabled=FALSE → 둘 다 비활성.
- **Wave 3**: 땅굴 이벤트 시작. 단, `SpawnConfig.TunnelGameTimeStart=30s` 때문에
  게임 시작 후 30초가 지나야 실제 땅굴 발생.
- **Wave 5**: `KillTarget=-1` → 전환 없이 이 파라미터로 세션 끝까지 유지.

`-1`은 일반적으로 "이 웨이브에서 오버라이드 없음 = SpawnConfig 기본값 사용".
`0`은 "명시적으로 비활성" (예: `EliteSpawnInterval=0`이면 엘리트 스폰 중단).
**예외** — EliteSpawnInterval의 `-1`은 폴백이 아닌 "비활성"으로 해석. KillTarget의 `-1`도 "전환 없음"으로 해석.

---

## 다음 단계

1. 위 두 CSV를 Google Sheets에 붙여넣기
2. 셀 서식 검증 (숫자/문자열/불리언)
3. Unity `Tools / Drill-Corp / 4. 데이터 Import / Google Sheets Importer` → `Import All Data`
4. 값 튜닝은 시트에서 수정 후 재Import — 인스펙터 직편집 금지 (덮어쓰기 됨)

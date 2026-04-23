# Drill-Corp 데이터 시트 입력 초안

> 작성: 2026-04-23
> 용도: Google Sheets `SimpleBugData` / `WaveData` 탭에 붙여넣을 **초기 데이터**.
> 라이프사이클: 시트 입력 완료되면 폴더 삭제 또는 `docs/archive/` 이동 가능.
> 스키마 문서: [GoogleSheetsGuide.md](../GoogleSheetsGuide.md) · [DataStructure.md](../DataStructure.md)

---

## 파일 목록

| 파일 | 대상 탭 | 스키마 섹션 |
|---|---|---|
| `SimpleBugData.csv` / `.tsv` | `SimpleBugData` | [GoogleSheetsGuide.md §1](../GoogleSheetsGuide.md#1-simplebugdata-시트) |
| `WaveData.csv` / `.tsv`      | `WaveData`      | [GoogleSheetsGuide.md §2](../GoogleSheetsGuide.md#2-wavedata-시트) |
| `MachineData.tsv`            | `MachineData`   | [GoogleSheetsGuide.md §3](../GoogleSheetsGuide.md#3-machinedata-시트) + [v2Addendum §1](../GoogleSheetsGuide_v2Addendum.md#1-machinedata-기존-1컬럼-추가) |
| `UpgradeData.tsv`            | `UpgradeData`   | [GoogleSheetsGuide.md §4](../GoogleSheetsGuide.md#4-upgradedata-시트) + [v2Addendum §2](../GoogleSheetsGuide_v2Addendum.md#2-upgradedata-기존-확장) + [MachineDataSheetAlignment_Plan.md](../MachineDataSheetAlignment_Plan.md) |

> `MachineData.tsv` / `UpgradeData.tsv` 는 Importer가 v2 컬럼(`BaseMiningTarget` / `BaseCostOre` / `BaseCostGem` / `OreCostSchedule` / `GemCostSchedule`) 을 파싱하도록 **Phase M-1 코드 확장이 끝난 뒤** 붙여넣기. 그 전에 Import 돌리면 신 컬럼이 무시됨. 상세 순서는 `MachineDataSheetAlignment_Plan.md` 참조.

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

- [ ] 3행(Default/Heavy/Speed) 들어갔는지
- [ ] `BaseMiningTarget` 컬럼이 마지막이 아니라 중간에 있는지 (`MiningBonus` 뒤, `AttackDamage` 앞)
- [ ] 구 컬럼 `MaxFuel` / `FuelConsumeRate` 가 시트에 남아있다면 삭제 (Importer가 읽지 않음)

### UpgradeData

- [ ] 6행 (excavator_hp/armor, mine_speed/target, gem_drop/speed) 들어갔는지
- [ ] 구 v1 11행 (max_health, max_fuel, attack_damage, crit_*, fuel_* 등) 전부 삭제됐는지
- [ ] `UpgradeType` 컬럼에 v2 신규 타입 3종 (`MiningTarget`, `GemDropRate`, `GemCollectSpeed`) 이 철자 정확히 들어갔는지
- [ ] `OreCostSchedule` / `GemCostSchedule` 가 `60|130|230|370|540` 처럼 **한 셀에 파이프 구분 문자열**로 들어갔는지
- [ ] `IsPercentage` 컬럼 — gem_drop 은 **FALSE** (누적 %p 가산), gem_speed 는 **TRUE** (배율)
- [ ] `-1` 값이 숫자로 들어갔는지 (음수 대쉬 깨짐 주의)
- [ ] `WaveName` 한글 깨짐 없는지

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

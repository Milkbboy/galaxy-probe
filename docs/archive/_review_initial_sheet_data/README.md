# Drill-Corp 데이터 시트 입력 초안 (아카이브)

> 작성: 2026-04-23 · 최종 갱신: 2026-05-05 (Aim/Gem 추가, 12탭)
> 용도: Google Sheets 12 탭에 붙여넣은 **초기 데이터** 보존본. 시트가 SSoT 가 된 후 참고용.
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
| `SpawnConfigData.csv`        | `SpawnConfigData` | [Data-SheetsGuide.md §10](../../Data-SheetsGuide.md#10-spawnconfigdata-시트) — 전역 스폰 폴백 1행, SpawnShape/Margin 등 |
| `AimData.csv`                | `AimData`       | [Data-SheetsGuide.md §11](../../Data-SheetsGuide.md#11-aimdata-시트) — 에임 크기·자동 계산·크로스헤어 배율 1행 |
| `GemData.csv`                | `GemData`       | [Data-SheetsGuide.md §12](../../Data-SheetsGuide.md#12-gemdata-시트) — 보석 크기·픽업 반경/시간·진행 링 1행 |

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

# Google Sheets 데이터 관리 가이드

게임 데이터(벌레, 웨이브, 머신, 업그레이드)는 Google Sheets에서 관리하고, Unity Editor 메뉴 `Tools / Drill-Corp / 4. 데이터 Import / Google Sheets Importer` 로 가져와 ScriptableObject 로 변환합니다.

**스프레드시트 URL**: https://docs.google.com/spreadsheets/d/1hwgQ4IF-gQqVSX4xS_uqeKIPWUDy2NR4bC-OWmZQO_E/edit

> 최종 갱신: 2026-04-23 — SimpleBug 전면 교체 반영. v2 신규 시트(Character/Ability/Weapon/WeaponUpgrade) 는 [GoogleSheetsGuide_v2Addendum.md](GoogleSheetsGuide_v2Addendum.md) 참조.

---

## 시트 구조

| 시트 이름 | 설명 | 대응 SO |
|---|---|---|
| `SimpleBugData` | 벌레 종류별 스탯 | `SimpleBugData` (Normal/Elite/Swift) |
| `WaveData` | 웨이브별 Spawner/Tunnel 오버라이드 | `SimpleWaveData` (Wave_01~N) |
| `MachineData` | 채굴 머신 스탯 | `MachineData` |
| `UpgradeData` | 굴착기 영구 강화 6종 (이중 재화) | `UpgradeData` |
| `Characters` / `Abilities` / `Weapons` / `WeaponUpgrades` (v2) | — | [v2Addendum](GoogleSheetsGuide_v2Addendum.md) |

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

`Assets/_Game/Data/SpawnConfig.asset` 는 **시트에 없는 전역 폴백값**. 인스펙터 직편집. 튜닝 빈도 낮아 시트에서 뺌. 필드 상세는 [DataStructure.md §3](DataStructure.md#3-spawnconfigdata).

---

## 3. MachineData 시트

채굴 머신 스탯. [DataStructure.md §5](DataStructure.md#5-machinedata) 참조. 컬럼:

`MachineId`, `MachineName`, `Description`, `MaxHealth`, `HealthRegen`, `Armor`, `MiningRate`, `MiningBonus`, `BaseMiningTarget` (v2 — 세션 승리 목표), `AttackDamage`, `AttackCooldown`, `AttackRange`, `CritChance`, `CritMultiplier`.

> 구 `MaxFuel`/`FuelConsumeRate` 는 v2 승리 조건 전환(연료 → mineTarget)으로 삭제됨. 시트에 남아있으면 제거.

---

## 4. UpgradeData 시트

영구 강화. 이중 재화(광석+보석). [DataStructure.md §6](DataStructure.md#6-upgradedata) 참조. 컬럼:

`UpgradeId`, `DisplayName`, `Description`, `UpgradeType`, `MaxLevel`, `BaseValue`, `ValuePerLevel`, `IsPercentage`, `BaseCost` (광석), `BaseCostGem` (v2 — 보석), `CostMultiplier`, `OreCostSchedule` (v2 — 레벨별 배열), `GemCostSchedule` (v2).

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

## 5. Unity 에서 Import

### 열기

메뉴 `Tools > Drill-Corp > 4. 데이터 Import > Google Sheets Importer`.

### 인증

창이 열리면 `Assets/_Game/Data/Credentials/google-credentials.json` 기반으로 자동 인증. "✓ 인증됨" 표시 확인.

### 프리뷰 (선택)

`Load Preview` → 각 시트 탭 데이터 확인. 문제 있으면 시트에서 수정 후 다시 Load.

### Import 실행

- `Import All Data` — 4개 시트 순서대로 (SimpleBug → Machine → Upgrade → Wave)
- 시트별 버튼 — `SimpleBugData`, `WaveData`, `MachineData`, `UpgradeData`

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

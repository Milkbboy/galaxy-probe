# SimpleBug 데이터 스키마 (작업용 중간 문서)

> 작성일: 2026-04-23
> 용도: SimpleBug 전면 교체에 따른 **시트/SO 스키마 확정안**.
> 라이프사이클: 이 문서는 Phase C 완료 시점에 `GoogleSheetsGuide.md` / `DataStructure.md`로 흡수되고 **삭제**됩니다.

---

## 0. 배경

기존 `BugData` + `BugBehaviorData`(Movement/Attack/Passive/Skill/Trigger) + `WaveData(SpawnGroups)` 시스템은 `SimpleBugSpawner` + `TunnelEventManager` + `SimpleBug`로 전면 교체되었고, 레거시 데이터는 씬에서 더 이상 참조되지 않습니다. Importer의 BugData/WaveData 경로도 dead code 상태입니다.

본 문서는 SimpleBug 시스템을 Google Sheets로 기획자가 튜닝할 수 있게 하기 위한 **신규 데이터 계약**입니다.

---

## 1. 데이터 3계층 — 시트 / SO / 씬

```
Google Sheets                  ScriptableObject                  Scene Components
─────────────                  ────────────────                  ────────────────
SimpleBugData 시트   ─────▶   SimpleBug_*.asset × 3     ─────▶  SimpleBugSpawner
                              (숫자 필드만 덮어쓰기,            TunnelEventManager
                               Prefab은 수동 연결 유지)          (Data 참조 주입)

WaveData 시트        ─────▶   Wave_NN.asset × N          ─────▶  SimpleWaveManager
                                                                 (Spawner/Tunnel에 웨이브 진입 시 주입)

(시트 없음)                    SpawnConfig.asset × 1      ─────▶  SimpleWaveManager
                              (인스펙터 직편집)                   (폴백 기본값 공급)
```

- **시트 → SO**: `GoogleSheetsImporter`가 Import 시 SO의 숫자/문자 필드만 덮어씀. Prefab/VFX 참조 필드는 건드리지 않음.
- **SO → 씬**: Spawner/TunnelEvent/SimpleWaveManager가 인스펙터에서 SO를 바인딩.
- **SpawnConfig**는 시트 없음 — 한 번 세팅하면 자주 바뀌지 않는 전역 폴백값용.

---

## 2. 시트 스키마

### 2.1 `SimpleBugData` 시트 (신규, 기존 `BugData` 시트 대체)

벌레 종류별 기본 스탯 + 웨이브 스케일링. 행은 지금 3종(Normal/Elite/Swift) 고정이지만 확장 가능.

| 컬럼 | 타입 | 필수 | 설명 | 예시 |
|---|---|---|---|---|
| **BugName** | string | O | 식별용. SO 파일명 매핑 규칙은 2.1.1절 참조 | Normal, Elite, Swift |
| **Kind** | enum | O | `Normal` / `Elite` / `Swift` (대소문자 무시) | Normal |
| **BaseHp** | float | O | 웨이브 1 기준 HP | 2, 10 |
| **HpPerWave** | float | O | 웨이브당 HP 증가 (`floor(wave × HpPerWave)` 가산) | 0.5, 0 |
| **BaseSpeed** | float | O | 웨이브 1 기준 이동 속도 (유닛/초) | 0.5, 3 |
| **SpeedPerWave** | float | O | 웨이브당 속도 증가 | 0.06, 0 |
| **SpeedRandom** | float | | 스폰 시 속도 +랜덤 `[0, 값)` | 0.15 |
| **Size** | float | O | 스케일 (localScale 균등 배율) | 0.4, 0.8, 0.2 |
| **Score** | float | O | 처치 시 보상 (`OnBugScoreEarned` 발행값) | 1, 5, 0.5 |
| **TintHex** | string | | 미니맵 아이콘 색 + (필요 시) 틴트. `#RRGGBB` 또는 `#RRGGBBAA` | #51CF66, #FFD700 |

**비어있는 필드의 기본값**: Importer가 "빈 셀 → 기존 SO 값 보존" 규칙으로 처리. SpeedRandom/TintHex 같은 선택 컬럼을 지워도 이전 값이 유지됨. 반대로 BaseHp/BaseSpeed처럼 필수 컬럼이 비면 이전 값이 남으므로 **명시적 0이 필요하면 `0`을 써야 함** (Swift의 HpPerWave 처럼).

**Prefab 처리**: 시트에 Prefab 컬럼 **없음**. 유니티 인스펙터에서 대상 SO를 열어 `Prefab` 필드에 수동 연결. Import는 `Prefab`·런타임 바인딩 필드를 절대 덮어쓰지 않음.

#### 2.1.1 BugName ↔ 에셋 파일명 매핑 규칙

기존 에셋 파일명이 **`SimpleBug_Elit`(오타)** 로 되어 있어 단순 `SimpleBug_<BugName>` 규약과 충돌. 처리 옵션 둘:

- **(A) Import 매칭 기준을 "SO 내부 `BugName` 필드"로** — 파일명 무시, `AssetDatabase.FindAssets("t:SimpleBugData")`로 전체 로드 후 각 SO의 BugName 필드와 시트 BugName을 대조. 새 종류는 `Assets/_Game/Data/Bugs/SimpleBug_<BugName>.asset`으로 신규 생성. **추천** — 파일명 오타에 강건하고 기획자가 시트에서 수정 못 하는 상태를 방지.
- (B) `SimpleBug_Elit.asset`을 `SimpleBug_Elite.asset`으로 **사전 rename** 후 파일명 기반 매핑. 깔끔하지만 git history·참조(프리펩·씬 GUID는 무관하나 파일명은 바뀜) 주의.

> Phase B 진입 시 (A) 확정 권장.

#### 2.1.2 예시 데이터 (현행 SO 값 그대로 전재)

| BugName | Kind | BaseHp | HpPerWave | BaseSpeed | SpeedPerWave | SpeedRandom | Size | Score | TintHex |
|---|---|---|---|---|---|---|---|---|---|
| Normal | Normal | 2 | 0.5 | 0.5 | 0.06 | 0.15 | 0.4 | 1 | #51CF66 |
| Elite | Elite | 10 | 0.5 | 0.35 | 0.04 | 0.15 | 0.8 | 5 | #FFD700 |
| Swift | Swift | 1 | 0 | 3 | 0 | 0 | 0.2 | 0.5 | #DEDEFF |

> 위 값은 `Assets/_Game/Data/Bugs/SimpleBug_Normal/Elit/Swift.asset`의 현재 필드를 그대로 옮긴 것. Tint RGB → Hex 변환도 기존 값 기준 (Normal `(0.318, 0.812, 0.4)` ≈ `#51CF66`, Elite `(1, 0.84, 0)` = `#FFD700`, Swift `(0.87, 0.87, 1)` ≈ `#DEDEFF`).

---

### 2.2 `WaveData` 시트 (개편 — 기존 WaveSpawnGroups 삭제, 컬럼 전면 교체)

웨이브 진행 시 스포너·땅굴의 파라미터를 오버라이드. 빈 셀은 `SpawnConfig` 폴백값 사용.

**중요 — 세션 종료와 무관**: 웨이브 시스템은 "시간대별 난이도 곡선"일 뿐. 세션은 **채굴 완료**(승리) 또는 **머신 HP 0**(패배)로만 종료. 마지막 웨이브에 도달해도 세션은 계속되고 벌레는 계속 스폰되어야 함. 따라서 "다음 웨이브 없음"은 "마지막 웨이브 파라미터 유지"를 의미.

**웨이브 전환 트리거**: **벌레 처치 점수 누적**이 `KillTarget`에 도달하면 다음 웨이브로 전환 (v2.html 원본 로직). Score는 `SimpleBugData.Score` 컬럼 그대로 사용 — Normal=1, Elite=5, Swift=0.5. 시간 기반 아님.

| 컬럼 | 타입 | 필수 | 설명 | 예시 |
|---|---|---|---|---|
| **WaveNumber** | int | O | 웨이브 번호 (1부터) | 1, 2, 3 |
| **WaveName** | string | | 표시용 | 시작, 증가, 땅굴 출현 |
| **KillTarget** | float | | 이 웨이브에서 누적해야 할 처치 점수. 도달 시 다음 웨이브로 전환. `-1` 또는 `0`이면 "전환 없음 = 세션 끝까지 유지" (보통 마지막 웨이브) | 15, 25, -1 |
| **NormalSpawnInterval** | float | | 일반 벌레 스폰 주기 (초) | 0.083 |
| **EliteSpawnInterval** | float | | 엘리트 스폰 주기 (초). `-1` 또는 `0`이면 엘리트 비활성 (이 컬럼만 예외적으로 -1이 폴백 아님) | 15, 10 |
| **MaxBugs** | int | | 동시 생존 상한 | 90 |
| **TunnelEnabled** | bool | | 이 웨이브부터 땅굴 이벤트 활성 | TRUE, FALSE |
| **TunnelEventInterval** | float | | 땅굴 주기 (초) | 15 |
| **SwiftPerTunnel** | int | | 한 땅굴당 Swift 수 | 10 |

**비어있는 셀 규칙 (Numeric)**
- `-1` 또는 빈 셀 → SpawnConfig 기본값 사용 (= "오버라이드 없음").
- `0`은 "명시적 0" 의미로 존중. 예: `MaxBugs=0` → 상한 없음 아니라 0마리 유지.
- **예외 — EliteSpawnInterval**: `-1`·`0` 모두 "엘리트 비활성"으로 해석 (폴백 아님). 시트 작성 의도상 Wave 초기에 엘리트를 끄는 용도이므로 SpawnConfig 폴백을 타지 않음.
- **예외 — KillTarget**: `-1`·`0` 또는 빈 셀 모두 "전환 없음 = 이 웨이브를 세션 끝까지 유지"로 해석. 마지막 웨이브 지정 방식.
- `TunnelEnabled`: 빈 셀 → `false` 취급. 이 값이 `false`면 TunnelEventInterval·SwiftPerTunnel은 어떤 값이든 무시됨.
- 수치 오버라이드가 필요 없는 웨이브는 `WaveNumber` + `WaveName` + `KillTarget`만 채워도 됨 (나머지는 SpawnConfig 폴백).

#### 예시 데이터

| WaveNumber | WaveName | KillTarget | NormalSpawnInterval | EliteSpawnInterval | MaxBugs | TunnelEnabled | TunnelEventInterval | SwiftPerTunnel |
|---|---|---|---|---|---|---|---|---|
| 1 | 시작 | 15 | 0.12 | -1 | 50 | FALSE | -1 | -1 |
| 2 | 가속 | 25 | 0.1 | -1 | 70 | FALSE | -1 | -1 |
| 3 | 땅굴 출현 | 40 | 0.083 | 15 | 90 | TRUE | 15 | 10 |
| 4 | 러시 | 60 | 0.07 | 12 | 110 | TRUE | 12 | 12 |
| 5 | 최종 | -1 | 0.06 | 10 | 130 | TRUE | 10 | 15 |

- `KillTarget`은 **누적 점수** (Normal 1 + Elite 5 + Swift 0.5). Wave 1 목표 15점 ≈ Normal 15마리 또는 Elite 3마리.
- Wave 5의 `KillTarget=-1` → 전환 없음. 세션 끝(채굴 완료)까지 이 파라미터로 계속 스폰.
- `EliteSpawnInterval=-1` 로 둔 1·2웨이브는 엘리트 비활성 (폴백 아닌 예외 규칙).
- Wave 3에서 TunnelEnabled가 처음 true가 되면서 `TunnelGameTimeStart` 대기는 별도 — **게임 시작 후 30초가 지나야** 실제 땅굴 발생.

---

### 2.3 기존 시트 유지/개편 요약

| 시트 | 상태 | 비고 |
|---|---|---|
| `BugData` | ❌ **삭제** | SimpleBugData로 대체 |
| `WaveSpawnGroups` | ❌ **삭제** | Formation + SpawnGroup 개념 모두 제거 |
| `WaveData` | 🔄 **컬럼 전면 교체** | 본 문서 2.2절 스키마로 재정의 |
| `SimpleBugData` | ✨ **신규** | 본 문서 2.1절 |
| `MachineData` | ✅ 유지 | 별건 (v2 Addendum에 BaseMiningTarget 1컬럼 추가 예정) |
| `UpgradeData` | ✅ 유지 | 별건 (v2 Addendum에 Gem 컬럼 추가 예정) |

---

## 3. ScriptableObject 정의

### 3.1 `SimpleBugData` (기존 클래스 유지)

기존 `Assets/_Game/Scripts/Bug/Simple/SimpleBugData.cs` 구조 그대로 사용. Importer가 `[SerializeField]` 필드를 직접 덮어쓰기 위해 **public 필드 → `[SerializeField] private` + 프로퍼티**로 일관되게 리팩토링 필요할 수 있음 (현재는 public 필드인데 SerializedObject 경로로 쓰려면 이름 규약 맞춰야).

**작업 시 주의**: 리팩토링하면 기존 3개 에셋의 YAML 필드명이 달라져 값 날아감. `[FormerlySerializedAs]` 속성으로 마이그레이션 필요.

**또는 최소 변경안**: public 필드를 그대로 두고 Importer에서 `FindProperty(fieldName)`으로 접근 (SerializedObject는 public 필드도 찾음). 이쪽이 안전.

### 3.2 `SpawnConfigData` (신규 SO)

```
Assets/_Game/Scripts/Data/SpawnConfigData.cs
Assets/_Game/Data/SpawnConfig.asset  (단일 인스턴스)
```

**필드**
- `[Header("Spawn Defaults")]`
  - `float DefaultNormalSpawnInterval = 0.083f`
  - `float DefaultEliteSpawnInterval = 15f`
  - `int DefaultMaxBugs = 90`
- `[Header("Tunnel Defaults")]`
  - `float TunnelGameTimeStart = 30f` — 게임 시작 후 이 시간 지나야 땅굴 활성 (`TunnelEnabled=true` 웨이브 진입한 경우에도 이 값 미만이면 대기)
  - `float DefaultTunnelEventInterval = 15f`
  - `int DefaultSwiftPerTunnel = 10`
  - `float TunnelSpawnInterval = 0.2f` — 한 땅굴 내 Swift 생성 간격 (튜닝 빈도 낮아 전역 상수)
- `[Header("Spawn Area")]`
  - `bool AutoRadius = true`
  - `float ManualRadius = 15f`
  - `float NormalMargin = 0.4f`
  - `float EliteMargin = 0.5f`
  - `float EdgeMargin = 0.4f`
  - `float SpawnJitter = 0.15f`

`[CreateAssetMenu(fileName = "SpawnConfig", menuName = "Drill-Corp/Spawn Config", order = 2)]`

### 3.3 `WaveData` (기존 클래스 전면 교체)

기존 `Assets/_Game/Scripts/Data/WaveData.cs`는 SpawnGroup/FormationSpawn 기반이므로 **필드 전면 교체**. 기존 `Wave_*.asset`은 스키마 불일치로 재생성 필요 (Phase C-4에서 삭제, C-5에서 새로 생성).

**필드**
- `int WaveNumber`
- `string WaveName`
- `float WaveDuration` (0 = 수동/무한)
- Override 필드 (–1 = 폴백 사용)
  - `float NormalSpawnInterval = -1f`
  - `float EliteSpawnInterval = -1f`
  - `int MaxBugs = -1`
  - `bool TunnelEnabled = false`
  - `float TunnelEventInterval = -1f`
  - `int SwiftPerTunnel = -1`

**속성**: 각 오버라이드 getter가 `value >= 0 ? value : fallback.DefaultXxx` 형태로 해석. 폴백 로직은 `SimpleWaveManager`가 담당.

---

## 4. 런타임 흐름

```
Start
  └─ SimpleWaveManager.StartWave(1)
       ├─ WaveData[0] 읽음
       ├─ SimpleBugSpawner.Configure(waveData, spawnConfig, wave=1)
       │    └─ _spawnInterval / _eliteInterval / _maxBugs / _wave 갱신
       └─ TunnelEventManager.Configure(waveData, spawnConfig)
            └─ _autoRun = TunnelEnabled, _eventInterval, _swiftPerTunnel 갱신

Update (WaveDuration 경과 또는 수동 트리거)
  └─ SimpleWaveManager.StartWave(N+1)
       └─ 동일한 Configure 재호출
```

- Spawner/Tunnel은 여전히 자체 타이머·스폰 루프를 돌리고, 매니저는 **파라미터 주입자** 역할만.
- `_wave` 정수는 SimpleBugData의 HP/Speed 스케일링에 이미 들어가므로 웨이브 전환 시 Spawner의 Wave 프로퍼티만 갱신해도 새 스폰부터 자동 반영됨.

---

## 5. Import 규칙 변경 요약

### 5.1 `GoogleSheetsImporter.cs` 수정점

**제거**
- 상수 `SHEET_BUG_DATA`, `SHEET_WAVE_SPAWN_GROUPS`
- 메서드 `ImportBugData(Async)`, `ImportWaveData(Async)`의 기존 구현
- 헬퍼 전부: `CreateOrUpdateBugBehaviorData`, `FindOrCreateMovementSO`, `FindOrCreateAttackSO`, `ParseAndCreatePassives`, `ParseAndCreateSkills`, `ParseAndCreateTriggers`, `EnsureBehaviorFolders`, `LoadBehaviorCache`, `SetSOListProperty`
- `using DrillCorp.Bug.Behaviors.Data;`

**추가**
- 상수 `SHEET_SIMPLE_BUG_DATA = "SimpleBugData"`
- `ImportSimpleBugDataAsync()` — 2.1절 컬럼 파싱, `SimpleBugData` SO 필드만 덮어쓰기, Prefab 필드 손대지 않음
- `ImportWaveDataAsync()` 새 구현 — 2.2절 컬럼 파싱, `WaveData` SO에 오버라이드 값 기록 (–1 처리 포함)
- TintHex 파싱 헬퍼 (`#RRGGBB` → `Color`)

**갱신**
- `_previewTabNames` 배열: `{ "SimpleBugData", "WaveData", "MachineData", "UpgradeData" }`
- `ImportAllData()` 순서: `SimpleBug → Machine → Upgrade → Wave`
- Import 버튼 UI 텍스트 재배치

### 5.2 빈 셀 처리 헬퍼 신설

`GetFloatOrKeep(so, fieldName, row, headers, column)`: 셀이 비었으면 SO의 기존 값을 보존, 채워져 있으면 덮어쓰기. Prefab 필드 외에도 색상/런타임 바인딩 값이 실수로 초기화되는 사고 방지.

---

## 6. 씬 세팅 변경점 (Phase C-5)

- **`SimpleWaveManager` GameObject 추가** — `_waveDataList`(WaveData[]), `_spawnConfig`(SpawnConfigData), `_spawner`, `_tunnel` 참조 바인딩.
- **`SimpleBugSpawner`, `TunnelEventManager`** — 현재 인스펙터에 하드코딩된 숫자 필드들이 오버라이드 가능한 공개 API(`Configure(WaveData, SpawnConfigData)`)를 갖도록 수정. 에디터 인스펙터의 기존 필드는 제거 or 읽기 전용 표시.
- **신규 에셋**: `Assets/_Game/Data/SpawnConfig.asset` + `Assets/_Game/Data/Waves/Wave_01~05.asset` 재생성.

---

## 7. 기획자 튜닝 워크플로우 (예정)

Phase C 완료 후 작동할 루프:

1. **수치만 바꾸는 경우** (일상 튜닝)
   - Google Sheets 에서 셀 수정 (예: Normal `BaseHp` 2 → 3)
   - Unity `Tools / Drill-Corp / 4. 데이터 Import / Google Sheets Importer` → `Import All Data`
   - Play — 즉시 반영 (SO 덮어쓰기)
2. **새 벌레 종류를 추가하는 경우**
   - 시트 `SimpleBugData` 탭에 새 행 추가 (BugName 예: `Bomber`)
   - 유니티에서 Prefab 준비 → Import 실행 (신규 SO 자동 생성)
   - Import 직후 새로 생긴 `SimpleBug_Bomber.asset` 열어서 `Prefab` 필드 수동 바인딩
   - Spawner/Tunnel 어디에서 사용할지 결정 후 참조 연결
3. **웨이브 밸런스 조정**
   - `WaveData` 탭에서 오버라이드 수치 수정
   - 폴백으로 되돌리려면 셀을 비우거나 `-1` 입력
   - Import → Play

### 하지 말아야 할 것

- Unity 인스펙터에서 직접 수치 바꾸기 → 다음 Import에 덮어쓰여 날아감. 수치는 **항상 시트에서만** 편집.
- Prefab 필드를 시트에 노출하려는 시도 → 본 문서 2.1절 Prefab 처리 규칙을 깨뜨림.
- `SpawnConfig.asset`의 `TunnelGameTimeStart`, `TunnelSpawnInterval` 등을 시트로 빼는 것 → 튜닝 빈도가 낮아 시트 남용. 필요해지면 그때 2.2절 컬럼 추가.

---

## 8. 체크리스트 (Phase A 완료 기준)

**완료됨**
- [x] 본 문서 작성 — 스키마 초안 확정
- [x] 기존 `SimpleBug_Normal/Elit/Swift.asset` 현재값을 2.1.2절 예시 데이터로 전재
- [x] BugName ↔ 파일명 매핑 규칙 명시 (2.1.1절, 옵션 A 추천)
- [x] 빈 셀 / `-1` / `0`의 의미 명확화 (2.1절, 2.2절)

**남은 작업 (코드 수정은 하지 않음)**
- [ ] 기획자 리뷰 1회 — 컬럼명/기본값/TintHex 값 납득 여부
- [ ] Google Sheets에 탭 구조 적용
  - [ ] `SimpleBugData` 탭 신규 — 2.1.2절 예시 3행 초기 입력
  - [ ] `WaveData` 탭 컬럼 전면 교체 — 2.2절 예시 5행 초기 입력
  - [ ] `BugData` 탭 삭제 또는 `_legacy_BugData`로 rename (Import 안 건드림 보장)
  - [ ] `WaveSpawnGroups` 탭 삭제 또는 `_legacy_` rename
- [ ] 리뷰 결과 반영해 본 문서 갱신

확정되면 Phase B(코드 변경)로 진입.

---

## 9. 이후 문서 정리 예정

본 문서는 Phase C 완료 시점에 아래로 흡수·정리됩니다.

- 2절(시트 스키마) → `docs/GoogleSheetsGuide.md` 전면 개편 본문
- 3절(SO 정의) → `docs/DataStructure.md` 전면 개편 본문
- 4절(런타임 흐름) → `docs/DataStructure.md` 또는 `docs/BugSystem.md`(신규) 어느 쪽으로 갈지는 Phase C-5에서 결정
- 7절(기획자 워크플로우) → `docs/GoogleSheetsGuide.md` 말미에 합류
- 기존 `BugBehaviorSystem.md`, `BugBehaviorPatterns.md`, `FormationSystem.md` → `docs/archive/` 루트로 이동
- 본 문서 자체는 흡수 후 **삭제**

# 1단계 - 코어 시스템 개발 로그

## 개요
Drill-Corp 게임의 핵심 시스템 구축. 게임 상태 관리, 이벤트 시스템, 데이터 저장/로드 기능 구현.

---

## 타임라인

| 시간 | 작업 내용 |
|------|-----------|
| 2026-03-18 | 1단계 코어 시스템 구현 시작 |
| └ | 폴더 구조 생성: `Assets/_Game/Scripts/Core/` |
| └ | GameManager.cs 작성 - 싱글톤, 게임 상태 관리 |
| └ | GameEvents.cs 작성 - 전역 이벤트 정의 |
| └ | DataManager.cs 작성 - JSON 저장/로드 |
| └ | Roadmap.md 업데이트 - 1단계 완료 체크 |

---

## 생성된 파일

| 파일명 | 경로 | 역할 |
|--------|------|------|
| GameManager.cs | `Assets/_Game/Scripts/Core/` | 게임 상태 관리, 씬 전환 |
| GameEvents.cs | `Assets/_Game/Scripts/Core/` | 전역 이벤트 정의 |
| DataManager.cs | `Assets/_Game/Scripts/Core/` | JSON 저장/로드, 재화 관리 |

---

## GameManager.cs

### 역할
- 싱글톤 패턴으로 전역 접근
- 게임 상태(State) 관리
- 씬 전환 처리
- 세션 시작/종료 로직

### 게임 상태 (GameState)
```csharp
public enum GameState
{
    Title,          // 타이틀 화면
    Playing,        // 인게임 플레이 중
    Paused,         // 일시정지
    SessionSuccess, // 세션 성공 (연료 소진)
    SessionFailed   // 세션 실패 (머신 파괴)
}
```

### 주요 메서드
| 메서드 | 설명 |
|--------|------|
| `ChangeState(GameState)` | 상태 변경 + 이벤트 발행 |
| `StartSession()` | Playing 상태로 전환 |
| `PauseGame()` | 일시정지 (TimeScale = 0) |
| `ResumeGame()` | 재개 (TimeScale = 1) |
| `SessionSuccess()` | 세션 성공 처리 |
| `SessionFailed()` | 세션 실패 처리 |
| `LoadScene(string)` | 씬 로드 |
| `RestartSession()` | 현재 씬 재시작 |

### 사용 예시
```csharp
// 게임 시작
GameManager.Instance.StartSession();

// 상태 확인
if (GameManager.Instance.CurrentState == GameState.Playing)
{
    // 플레이 중 로직
}

// 세션 종료
GameManager.Instance.SessionSuccess();
```

---

## GameEvents.cs

### 역할
- C# Action 기반 전역 이벤트 시스템
- 컴포넌트 간 느슨한 결합(Loose Coupling) 제공

### 이벤트 목록

| 이벤트 | 파라미터 | 발생 시점 |
|--------|----------|-----------|
| `OnGameStateChanged` | `GameState` | 게임 상태 변경 시 |
| `OnMachineDamaged` | `float` (데미지) | 머신 피격 시 |
| `OnMachineDestroyed` | - | 머신 HP 0 |
| `OnFuelChanged` | `float` (현재량) | 연료 변경 시 |
| `OnBugKilled` | `int` (벌레 ID) | 벌레 처치 시 |
| `OnWaveStarted` | `int` (웨이브 번호) | 웨이브 시작 |
| `OnWaveCompleted` | `int` (웨이브 번호) | 웨이브 완료 |
| `OnSessionSuccess` | - | 세션 성공 |
| `OnSessionFailed` | - | 세션 실패 |
| `OnCurrencyChanged` | `int` (현재 재화) | 재화 변경 시 |
| `OnMiningGained` | `int` (획득량) | 채굴 획득 시 |

### 사용 예시
```csharp
// 이벤트 구독
void OnEnable()
{
    GameEvents.OnMachineDamaged += HandleDamage;
}

void OnDisable()
{
    GameEvents.OnMachineDamaged -= HandleDamage;
}

void HandleDamage(float damage)
{
    // UI 업데이트 등
}

// 이벤트 발행
GameEvents.OnBugKilled?.Invoke(bugId);
```

---

## DataManager.cs

### 역할
- 플레이어 데이터 관리
- JSON 파일로 저장/로드
- 재화 시스템

### 저장 위치
- `Application.persistentDataPath/playerdata.json`
- Windows: `C:\Users\{User}\AppData\LocalLow\{Company}\{Product}\`

### PlayerData 구조
```csharp
[Serializable]
public class PlayerData
{
    public int Currency;           // 보유 재화
    public int MachineLevel;       // 머신 강화 레벨
    public int TotalSessionsPlayed; // 총 플레이 횟수
    public int TotalBugsKilled;    // 총 처치 벌레 수
}
```

### 주요 메서드
| 메서드 | 설명 |
|--------|------|
| `Save()` | JSON 파일로 저장 |
| `Load()` | JSON 파일에서 로드 |
| `AddCurrency(int)` | 재화 추가 |
| `SpendCurrency(int)` | 재화 사용 (부족 시 false) |
| `ResetData()` | 데이터 초기화 |

### 사용 예시
```csharp
// 재화 추가
DataManager.Instance.AddCurrency(100);

// 재화 사용
if (DataManager.Instance.SpendCurrency(50))
{
    // 구매 성공
}

// 현재 재화 확인
int currency = DataManager.Instance.Data.Currency;
```

---

## Unity 설정 가이드

### 1. 빈 GameObject 생성
- Hierarchy > Create Empty
- 이름: `Managers`

### 2. 컴포넌트 추가
- Add Component > GameManager
- Add Component > DataManager

### 3. DontDestroyOnLoad
- 두 매니저 모두 자동으로 씬 전환 시 유지됨

---

## 다음 단계
2단계 - 인게임 세션
- MachineController (HP/FUEL 시스템)
- BugBase, BugAI (벌레 시스템)
- AimController (사격 시스템)
- WaveManager (웨이브 관리)

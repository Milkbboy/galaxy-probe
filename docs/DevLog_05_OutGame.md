# 5단계 - 아웃게임 개발 로그

## 개요
타이틀 화면, 강화 시스템, 머신 선택, 옵션 설정 등 게임 세션 외부 시스템 구현.

---

## 타임라인

| 작업 내용 |
|-----------|
| 5단계 아웃게임 구현 시작 |
| UpgradeData.cs ScriptableObject 작성 |
| UpgradeManager.cs 싱글톤 작성 |
| TitleUI.cs 타이틀 화면 UI |
| UpgradeUI.cs, UpgradeItemUI.cs 강화 UI |
| MachineSelectUI.cs, MachineItemUI.cs 머신 선택 UI |
| OptionsUI.cs 옵션 설정 UI |
| GameManager.cs 씬 전환 기능 추가 |
| DataSetupEditor.cs 업그레이드 에셋 생성 추가 |
| GameEvents.cs 업그레이드/머신 선택 이벤트 추가 |

---

## 생성된 파일

| 파일명 | 경로 | 역할 |
|--------|------|------|
| UpgradeData.cs | `Scripts/Data/` | 강화 항목 ScriptableObject |
| UpgradeManager.cs | `Scripts/OutGame/` | 강화 시스템 관리 |
| TitleUI.cs | `Scripts/OutGame/` | 타이틀 화면 UI |
| UpgradeUI.cs | `Scripts/OutGame/` | 강화 패널 UI |
| UpgradeItemUI.cs | `Scripts/OutGame/` | 개별 강화 항목 UI |
| MachineSelectUI.cs | `Scripts/OutGame/` | 머신 선택 패널 |
| MachineItemUI.cs | `Scripts/OutGame/` | 개별 머신 항목 UI |
| OptionsUI.cs | `Scripts/OutGame/` | 옵션 설정 UI |

---

## UpgradeData.cs

### 역할
- 강화 항목별 설정 정의
- 레벨에 따른 값 계산
- 비용 계산

### UpgradeType 종류
```csharp
public enum UpgradeType
{
    MaxHealth,       // 최대 체력
    Armor,           // 방어력
    HealthRegen,     // 체력 재생
    MaxFuel,         // 최대 연료
    FuelEfficiency,  // 연료 효율
    MiningRate,      // 채굴량
    AttackDamage,    // 공격력
    AttackSpeed,     // 공격 속도
    AttackRange,     // 공격 범위
    CritChance,      // 치명타 확률
    CritDamage       // 치명타 배율
}
```

### 주요 메서드
```csharp
// 특정 레벨에서의 값
float GetValueAtLevel(int level)
    => baseValue + (valuePerLevel * level)

// 다음 레벨 업그레이드 비용
int GetCostForLevel(int currentLevel)
    => baseCost * (costMultiplier ^ currentLevel)

// 표시용 문자열
string GetValueString(int level)
    => isPercentage ? "+{value}%" : "+{value}"
```

---

## UpgradeManager.cs

### 역할
- 강화 레벨 관리 (저장/로드)
- 업그레이드 처리 (비용 확인/차감)
- 타입별 총 보너스 계산

### 주요 메서드
```csharp
// 업그레이드 시도
bool TryUpgrade(string upgradeId)

// 레벨 조회
int GetUpgradeLevel(string upgradeId)

// 타입별 총 보너스
float GetTotalBonus(UpgradeType type)

// 업그레이드 가능 여부
bool CanUpgrade(string upgradeId)
```

### 저장 방식
- PlayerPrefs에 JSON으로 저장
- 키: "Upgrades"
- 형식: `{ States: [{ UpgradeId, Level }, ...] }`

---

## TitleUI.cs

### 역할
- 타이틀 화면 메인 메뉴
- 패널 전환 관리

### 버튼 기능
| 버튼 | 기능 |
|------|------|
| Start | GameScene 로드 |
| Upgrade | 강화 패널 표시 |
| Machine | 머신 선택 패널 표시 |
| Options | 옵션 패널 표시 |
| Quit | 게임 종료 |

### 패널 구성
```
TitleUI
├── MainPanel (메인 메뉴)
├── UpgradePanel (강화)
├── MachineSelectPanel (머신 선택)
└── OptionsPanel (옵션)
```

---

## UpgradeUI / UpgradeItemUI

### UpgradeUI
- 강화 목록 관리
- UpgradeManager에서 데이터 로드
- 동적 아이템 생성

### UpgradeItemUI
- 개별 강화 항목 표시
- 레벨, 현재 값, 다음 값, 비용 표시
- 구매 버튼 처리

### UI 구성
```
UpgradeItemUI
├── Icon (강화 아이콘)
├── Name (강화 이름)
├── Description (설명)
├── Level (Lv. 3 / 10)
├── Value (+15 → +20)
├── Cost (500)
└── UpgradeButton
```

---

## MachineSelectUI / MachineItemUI

### MachineSelectUI
- 머신 목록 표시
- 선택한 머신 정보 표시
- 선택 저장 (PlayerPrefs)

### MachineItemUI
- 개별 머신 카드
- 선택 상태 표시
- 클릭으로 선택

### 선택 저장
```csharp
// 키: "SelectedMachine"
// 값: MachineId (int)
PlayerPrefs.SetInt("SelectedMachine", machineId);
```

---

## OptionsUI.cs

### 오디오 설정
| 설정 | 키 | 기본값 |
|------|-----|--------|
| Master Volume | MasterVolume | 1.0 |
| BGM Volume | BGMVolume | 1.0 |
| SFX Volume | SFXVolume | 1.0 |

### 그래픽 설정
- Resolution Dropdown
- Fullscreen Toggle
- Quality Dropdown (QualitySettings)

### 언어 설정
- Language Dropdown (한국어, English)
- 키: "Language"

### 데이터 초기화
- Reset Data 버튼
- PlayerPrefs.DeleteAll()
- UpgradeManager.ResetAllUpgrades()

---

## GameManager 씬 전환

### 추가된 메서드
```csharp
// 타이틀 씬 로드
void LoadTitleScene()

// 게임 씬 로드
void LoadGameScene()

// 타이틀로 돌아가기
void ReturnToTitle()
```

### 머신 선택 저장
```csharp
public int SelectedMachineId
{
    get => _selectedMachineId;
    set {
        _selectedMachineId = value;
        PlayerPrefs.SetInt("SelectedMachine", value);
    }
}
```

---

## GameEvents 추가

```csharp
// 강화 구매 시
Action<string, int> OnUpgradePurchased;  // upgradeId, newLevel

// 머신 선택 시
Action<int> OnMachineSelected;  // machineId
```

---

## 자동 생성 업그레이드 에셋

`Tools > Drill-Corp > Setup Data Assets` 실행 시:

```
Assets/_Game/Data/Upgrades/
├── Upgrade_MaxHealth.asset
├── Upgrade_Armor.asset
├── Upgrade_MiningRate.asset
├── Upgrade_AttackDamage.asset
├── Upgrade_AttackSpeed.asset
└── Upgrade_FuelEfficiency.asset
```

### 업그레이드 초기값
| 업그레이드 | 레벨당 값 | 최대 | 기본 비용 |
|------------|-----------|------|-----------|
| Max HP | +10 | 10 | 100 |
| Armor | +5 | 10 | 150 |
| Mining | +5% | 10 | 100 |
| Damage | +5% | 10 | 120 |
| Attack Speed | +5% | 10 | 120 |
| Fuel Efficiency | +3% | 10 | 80 |

---

## Unity 설정 가이드

### TitleScene 생성
1. File > New Scene
2. 저장: `Scenes/TitleScene`
3. Build Settings에 추가

### TitleUI 설정
1. Canvas 생성
2. TitleUI 컴포넌트 추가
3. 패널들 생성 및 연결:
   - MainPanel
   - UpgradePanel (UpgradeUI)
   - MachineSelectPanel (MachineSelectUI)
   - OptionsPanel (OptionsUI)

### UpgradeManager 설정
1. 빈 GameObject 생성: "UpgradeManager"
2. UpgradeManager 컴포넌트 추가
3. Available Upgrades에 Upgrade_*.asset 연결

### MachineSelectUI 설정
1. Available Machines에 Machine_*.asset 연결
2. Machine Item Prefab 설정

---

## 씬 전환 플로우

```
TitleScene
    │
    ├── [Start] ──→ GameScene
    │                   │
    │                   ├── [Success/Fail] ──→ SessionResultUI
    │                   │                           │
    │                   └── [Return to Title] ──────┘
    │
    └── [Quit] ──→ Application.Quit()
```

---

## 다음 단계 (확장 가능)

### 무기 시스템
- WeaponData ScriptableObject
- 무기 선택 UI
- 무기별 발사 패턴

### 캐릭터 시스템
- CharacterData ScriptableObject
- 캐릭터별 특수 능력
- 잠금 해제 시스템

### 상점 시스템
- 재화로 아이템 구매
- 무기/캐릭터 해금

### 업적 시스템
- 조건 달성 시 보상
- 업적 UI

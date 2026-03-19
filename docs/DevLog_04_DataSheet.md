# 4단계 - 데이터 시트 개발 로그

## 개요
ScriptableObject 기반의 데이터 시스템 구축. 벌레, 웨이브, 머신 데이터를 외부 에셋으로 분리하여 밸런스 조정이 용이하도록 설계.

---

## 타임라인

| 시간 | 작업 내용 |
|------|-----------|
| 2026-03-19 | 4단계 데이터 시트 구현 시작 |
| └ | BugData.cs ScriptableObject 작성 |
| └ | WaveData.cs ScriptableObject 작성 |
| └ | MachineData.cs ScriptableObject 작성 |
| └ | DataSetupEditor.cs 에디터 스크립트 작성 |
| └ | BugBase.cs 수정 - BugData 연동 |
| └ | MachineController.cs 수정 - MachineData 연동 |
| └ | WaveManager.cs 수정 - WaveData 연동 |
| └ | BugSpawner.cs 수정 - SpawnBugFromData 추가 |

---

## 생성된 파일

| 파일명 | 경로 | 역할 |
|--------|------|------|
| BugData.cs | `Scripts/Data/` | 벌레 스탯 데이터 |
| WaveData.cs | `Scripts/Data/` | 웨이브 구성 데이터 |
| MachineData.cs | `Scripts/Data/` | 머신 스탯 데이터 |
| DataSetupEditor.cs | `Scripts/Editor/` | 데이터 에셋 자동 생성 |

---

## BugData.cs

### 역할
- 개별 벌레의 스탯을 정의하는 ScriptableObject
- Inspector에서 쉽게 수정 가능

### 필드 구성
```
[Identification]
- Bug Id (int)
- Bug Name (string)
- Description (string)

[Stats]
- Max Health (float) - 최대 체력
- Move Speed (float) - 이동 속도
- Attack Damage (float) - 공격력
- Attack Cooldown (float) - 공격 쿨다운
- Attack Range (float) - 공격 범위

[Visuals]
- Prefab (GameObject) - 벌레 프리팹
- Tint Color (Color) - 틴트 색상
- Scale (float) - 크기 배율

[Rewards]
- Currency Reward (int) - 처치 시 재화
- Drop Chance (float) - 드랍 확률
```

### 사용법
```csharp
// Assets > Create > Drill-Corp > Bug Data
[CreateAssetMenu(fileName = "Bug_New", menuName = "Drill-Corp/Bug Data")]
```

---

## WaveData.cs

### 역할
- 웨이브별 스폰 구성 정의
- 난이도 배율 지원

### 필드 구성
```
[Wave Info]
- Wave Number (int)
- Wave Name (string)

[Spawn Settings]
- Spawn Groups (SpawnGroup[])
- Wave Duration (float)
- Delay Before Next Wave (float)

[Difficulty Scaling]
- Health Multiplier (float) - 체력 배율
- Damage Multiplier (float) - 공격력 배율
- Speed Multiplier (float) - 속도 배율
```

### SpawnGroup 구성
```csharp
[Serializable]
public class SpawnGroup
{
    public BugData BugData;      // 스폰할 벌레 데이터
    public int Count;            // 스폰 수
    public float StartDelay;     // 시작 지연
    public float SpawnInterval;  // 스폰 간격
    public bool RandomPosition;  // 랜덤 위치 여부
}
```

---

## MachineData.cs

### 역할
- 채굴 머신의 스탯 정의
- 강화 시스템 확장 가능

### 필드 구성
```
[Identification]
- Machine Id (int)
- Machine Name (string)
- Description (string)

[Health]
- Max Health (float) - 최대 체력
- Health Regen (float) - 체력 재생
- Armor (float) - 방어력 (데미지 감소)

[Fuel]
- Max Fuel (float) - 최대 연료
- Fuel Consume Rate (float) - 연료 소모율

[Mining]
- Mining Rate (float) - 기본 채굴량
- Mining Bonus (float) - 채굴 보너스 (%)

[Weapon]
- Attack Damage (float) - 공격력
- Attack Cooldown (float) - 공격 쿨다운
- Attack Range (float) - 공격 범위
- Crit Chance (float) - 크리티컬 확률
- Crit Multiplier (float) - 크리티컬 배율
```

### Armor 계산 공식
```csharp
// Armor 10 = 10% 데미지 감소
float reduction = armor / (armor + 100f);
float actualDamage = rawDamage * (1f - reduction);
```

---

## DataSetupEditor.cs

### 역할
- 에디터 메뉴에서 데이터 에셋 자동 생성
- 초기 밸런스 값 설정

### 사용법
```
Unity 메뉴: Tools > Drill-Corp > Setup Data Assets
```

### 자동 생성 구조
```
Assets/_Game/Data/
├── Bugs/
│   ├── Bug_Beetle.asset
│   ├── Bug_Fly.asset
│   ├── Bug_Centipede.asset
│   ├── Bug_Spider.asset
│   └── Bug_Wasp.asset
├── Waves/
│   ├── Wave_01.asset
│   ├── Wave_02.asset
│   ├── Wave_03.asset
│   ├── Wave_04.asset
│   └── Wave_05.asset
└── Machines/
    ├── Machine_Default.asset
    ├── Machine_Heavy.asset
    └── Machine_Speed.asset
```

---

## 밸런스 초기값

### 벌레 스탯
| 벌레 | 체력 | 속도 | 공격력 | 쿨다운 | 범위 | 보상 |
|------|------|------|--------|--------|------|------|
| Beetle | 15 | 2.0 | 5 | 1.0s | 1.0 | 1 |
| Fly | 8 | 4.0 | 3 | 0.5s | 1.0 | 1 |
| Centipede | 40 | 1.0 | 10 | 2.0s | 1.5 | 3 |
| Spider | 20 | 2.5 | 7 | 1.2s | 1.2 | 2 |
| Wasp | 12 | 3.0 | 12 | 1.5s | 1.0 | 2 |

### 웨이브 구성
| 웨이브 | 이름 | 벌레 구성 | 체력배율 |
|--------|------|-----------|----------|
| 1 | First Contact | Beetle x5 | 1.0x |
| 2 | Swarm Incoming | Beetle x5, Fly x3 | 1.0x |
| 3 | Heavy Hitters | Beetle x4, Centipede x2 | 1.1x |
| 4 | Speed Rush | Fly x8, Spider x3 | 1.1x |
| 5 | Final Stand | Beetle x6, Wasp x4, Centipede x3 | 1.2x |

### 머신 스탯
| 머신 | 체력 | 연료 | 채굴 | 공격력 | 쿨다운 | 특성 |
|------|------|------|------|--------|--------|------|
| Default | 100 | 60s | 10 | 20 | 0.5s | 기본 |
| Heavy | 150 | 50s | 8 | 25 | 0.7s | 방어력 15 |
| Speed | 80 | 45s | 15 | 15 | 0.3s | 빠른 채굴/사격 |

---

## 기존 스크립트 연동

### BugBase.cs 수정
```csharp
[Header("Data")]
[SerializeField] protected BugData _bugData;

protected virtual void ApplyBugData()
{
    if (_bugData != null)
    {
        _bugId = _bugData.BugId;
        _maxHealth = _bugData.MaxHealth;
        // ... 나머지 필드
    }
}

// 스포너에서 사용하는 초기화
public void Initialize(BugData data, float healthMult, float damageMult, float speedMult)
{
    _bugData = data;
    _maxHealth = data.MaxHealth * healthMult;
    // ... 배율 적용
}
```

### MachineController.cs 수정
```csharp
[Header("Data")]
[SerializeField] private MachineData _machineData;

private void ApplyMachineData()
{
    if (_machineData != null)
    {
        _maxHealth = _machineData.MaxHealth;
        _armor = _machineData.Armor;
        // ... 나머지 필드
    }
}

// Armor 데미지 감소 적용
public void TakeDamage(float damage)
{
    float actualDamage = CalculateDamageReceived(damage);
    _currentHealth -= actualDamage;
}
```

### WaveManager.cs 수정
```csharp
[Header("Wave Data (ScriptableObject)")]
[SerializeField] private List<Data.WaveData> _waveDataAssets;
[SerializeField] private bool _useScriptableObjects = true;

// ScriptableObject 기반 웨이브 스폰
private IEnumerator SpawnWaveFromDataCoroutine(Data.WaveData waveData)
{
    foreach (var group in waveData.SpawnGroups)
    {
        _bugSpawner.SpawnBugFromData(group.BugData,
            waveData.HealthMultiplier,
            waveData.DamageMultiplier,
            waveData.SpeedMultiplier);
    }
}
```

### BugSpawner.cs 수정
```csharp
// BugData로 스폰하는 새 메서드
public BugBase SpawnBugFromData(BugData bugData, float healthMult, float damageMult, float speedMult)
{
    GameObject prefab = bugData.Prefab ?? GetFallbackPrefab(bugData.BugId);
    var bug = Instantiate(prefab, spawnPos, Quaternion.identity);
    bug.Initialize(bugData, healthMult, damageMult, speedMult);
    return bug;
}
```

---

## Unity 설정 가이드

### 1. 데이터 에셋 생성
```
1. Unity 메뉴: Tools > Drill-Corp > Setup Data Assets
2. Assets/_Game/Data/ 폴더에 에셋 생성됨
3. Inspector에서 값 조정
```

### 2. WaveManager 설정
```
1. WaveManager 컴포넌트 선택
2. Use ScriptableObjects 체크
3. Wave Data Assets에 Wave_01 ~ Wave_05 연결
```

### 3. Machine 설정
```
1. MachineController 컴포넌트 선택
2. Machine Data에 Machine_Default.asset 연결
```

### 4. Bug Prefab 연결
```
1. 각 Bug_*.asset 파일 선택
2. Prefab 필드에 해당 프리팹 연결
3. 또는 BugSpawner의 기존 프리팹 사용 (자동 매핑)
```

---

## 확장 가이드

### 새 벌레 추가
1. `Assets > Create > Drill-Corp > Bug Data`
2. 스탯 설정
3. 프리팹 연결
4. Wave 에셋에서 SpawnGroup에 추가

### 새 웨이브 추가
1. `Assets > Create > Drill-Corp > Wave Data`
2. SpawnGroups 구성
3. WaveManager의 리스트에 추가

### 밸런스 조정
- Inspector에서 직접 수정
- Play Mode에서도 변경 가능 (저장됨)
- 에셋 복제하여 여러 버전 테스트

---

## 다음 단계
5단계 - 아웃게임
- 타이틀 화면
- 강화 시스템 (DataManager + MachineData 연동)
- 캐릭터/무기 선택
- 옵션 설정

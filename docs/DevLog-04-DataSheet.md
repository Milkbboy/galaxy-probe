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

## Bug Behavior System (2026-04)

### 개요
벌레의 이동, 공격, 패시브, 스킬, 트리거 행동을 ScriptableObject로 분리하여 조합 가능하게 설계.

### 생성된 SO 타입

| SO 타입 | 경로 | 역할 |
|---------|------|------|
| MovementBehaviorData | `BugBehaviors/Movement/` | 이동 패턴 (Linear, Hover, Burst 등) |
| AttackBehaviorData | `BugBehaviors/Attack/` | 공격 타입 (Melee, Projectile, Cleave 등) |
| PassiveBehaviorData | `BugBehaviors/Passive/` | 패시브 효과 (Armor, Shield, Regen 등) |
| SkillBehaviorData | `BugBehaviors/Skill/` | 스킬 (Nova, Spawn, BuffAlly 등) |
| TriggerBehaviorData | `BugBehaviors/Trigger/` | 트리거 (Enrage, ExplodeOnDeath 등) |
| BugBehaviorData | `BugBehaviors/` | 위 행동들의 조합 Set |

### 샘플 생성기
`BugBehaviorSampleCreator.cs`에서 모든 샘플 SO를 자동 생성.

```
Unity 메뉴: Tools > Drill-Corp > 1. 버그 설정 > 행동 (Behavior) > 샘플 전체 생성
```

---

## SO 생성 시 발생한 문제와 해결

### 문제 현상
에디터 스크립트에서 ScriptableObject를 생성하고 `SetPrivateField()`로 값을 설정할 때:
- **일부 SO에만 값이 설정됨** (불규칙)
- **첫 번째 SO에 값이 설정되지 않음** (패턴 발견)
- 에러나 경고 로그 없음

### 원인 분석

#### 1. Unity AssetDatabase 동기화 문제
```csharp
// 문제 코드: 생성 직후 바로 값 설정
var asset = CreateAsset<MovementBehaviorData>(folder, "Movement_Linear");
SetPrivateField(asset, "_type", MovementType.Linear);  // 첫번째는 실패
SetPrivateField(asset, "_displayName", "직선 이동");   // 실패

var asset2 = CreateAsset<MovementBehaviorData>(folder, "Movement_Hover");
SetPrivateField(asset2, "_type", MovementType.Hover);  // 성공
```

`AssetDatabase.CreateAsset()` 호출 직후에는 Unity 에디터 내부적으로 에셋이 완전히 등록되지 않은 상태.
`SerializedObject`나 `SerializedProperty`로 값을 설정해도 첫 번째 에셋에는 적용되지 않음.

#### 2. SetDirty만으로는 부족
```csharp
// EditorUtility.SetDirty()는 "변경됨" 표시만 할 뿐
// 실제 디스크 저장은 SaveAssets() 호출 시점에 발생
EditorUtility.SetDirty(asset);
```

#### 3. SO 간 참조 문제
BugBehaviorData가 MovementBehaviorData를 참조할 때:
```csharp
// Movement SO 생성
CreateMovementSamples();
SaveAllAssets();

// BugBehavior 생성 시 Movement 로드 → null 반환 가능
var linearMovement = AssetDatabase.LoadAssetAtPath<MovementBehaviorData>(path);
// Unity가 아직 에셋을 인식하지 못한 상태
```

### 해결 방법: 3단계 분리 패턴

**핵심 원칙:** 에셋 생성과 값 설정을 분리하고, 중간에 `SaveAssets()` + `Refresh()` 호출

```csharp
public static void CreateMovementSamples()
{
    // ========== 1단계: 빈 에셋 전부 생성 ==========
    CreateAsset<MovementBehaviorData>(folder, "Movement_Linear");
    CreateAsset<MovementBehaviorData>(folder, "Movement_Hover");
    CreateAsset<MovementBehaviorData>(folder, "Movement_Burst");
    // ... 나머지 에셋들

    // ========== 2단계: 저장 및 새로고침 ==========
    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();
    // 이 시점에 Unity가 모든 에셋을 인식

    // ========== 3단계: 에셋 다시 로드 후 값 설정 ==========
    var linear = AssetDatabase.LoadAssetAtPath<MovementBehaviorData>($"{folder}/Movement_Linear.asset");
    SetPrivateField(linear, "_type", MovementType.Linear);
    SetPrivateField(linear, "_displayName", "직선 이동");
    // ... 나머지 값 설정

    SaveAllAssets();
}
```

### SO 참조 설정 시 추가 주의사항

BugBehaviorData처럼 다른 SO를 참조하는 경우:

```csharp
public static void CreateBugBehaviorSetSamples()
{
    // 1단계: 빈 BugBehavior 에셋 먼저 생성
    var beetle = CreateAsset<BugBehaviorData>(folder, "BugBehavior_Beetle");
    var fly = CreateAsset<BugBehaviorData>(folder, "BugBehavior_Fly");

    // 2단계: 저장 및 새로고침
    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();

    // 3단계: 참조할 에셋 로드 (이미 존재하는 Movement/Attack SO)
    var linearMovement = LoadAssetWithLog<MovementBehaviorData>(BasePath + "/Movement/Movement_Linear.asset");
    var meleeAttack = LoadAssetWithLog<AttackBehaviorData>(BasePath + "/Attack/Attack_Melee.asset");

    // 4단계: 생성한 에셋 다시 로드 후 참조 설정
    beetle = AssetDatabase.LoadAssetAtPath<BugBehaviorData>($"{folder}/BugBehavior_Beetle.asset");
    SetPrivateField(beetle, "_defaultMovement", linearMovement);
    SetPrivateField(beetle, "_defaultAttack", meleeAttack);

    SaveAllAssets();
}
```

### CreateAsset 메서드 개선

기존 에셋이 있을 때 덮어쓰기 문제도 해결:

```csharp
private static T CreateAsset<T>(string folder, string name, bool overwrite = true) where T : ScriptableObject
{
    string path = $"{folder}/{name}.asset";

    T existing = AssetDatabase.LoadAssetAtPath<T>(path);
    if (existing != null)
    {
        if (overwrite)
        {
            // 기존 에셋 삭제 후 Refresh
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.Refresh();
        }
        else
        {
            return existing;
        }
    }

    T asset = ScriptableObject.CreateInstance<T>();
    AssetDatabase.CreateAsset(asset, path);
    AssetDatabase.SaveAssets();  // 즉시 저장
    return asset;
}
```

### 전체 생성 순서 (CreateAllSamples)

```csharp
public static void CreateAllSamples()
{
    CreateFolders();

    // 1단계: 기본 행동 SO 생성 (Movement, Attack, Passive, Skill, Trigger)
    CreateMovementSamples();
    CreateAttackSamples();
    CreatePassiveSamples();
    CreateSkillSamples();
    CreateTriggerSamples();

    // 저장 및 새로고침 (참조 가능하게)
    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();
    AssetDatabase.ReleaseCachedFileHandles();

    // 2단계: BugBehavior Set 생성 (위에서 만든 SO 참조)
    CreateBugBehaviorSetSamples();
    CreateTestBugBehaviorSamples();

    // 최종 저장
    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();
}
```

### 교훈 요약

| 문제 | 원인 | 해결 |
|------|------|------|
| 첫 번째 SO에 값 미적용 | CreateAsset 직후 Unity 내부 동기화 미완료 | 생성과 값 설정 분리, 중간에 SaveAssets+Refresh |
| SO 참조가 null | 참조 대상 SO가 아직 인식 안됨 | 참조 대상 SO 먼저 생성+저장 후 로드 |
| 기존 SO 덮어쓰기 안됨 | 기존 에셋 로드 후 리턴만 함 | DeleteAsset 후 새로 생성 |
| 에러/경고 없음 | Unity가 조용히 실패 | LoadAssetWithLog로 null 체크 로그 추가 |

### 핵심 원칙
> **Unity 에디터 스크립트에서 SO 생성 시:**
> 1. 빈 에셋 먼저 전부 생성
> 2. `SaveAssets()` + `Refresh()`로 동기화
> 3. 에셋 다시 로드 후 값 설정
> 4. 다시 `SaveAssets()`로 저장

---

## 다음 단계
5단계 - 아웃게임
- 타이틀 화면
- 강화 시스템 (DataManager + MachineData 연동)
- 캐릭터/무기 선택
- 옵션 설정

# 데이터 구조

## ScriptableObject 파일 위치
```
Assets/_Game/Data/
├── Bugs/
│   ├── Bug_Beetle.asset
│   ├── Bug_Fly.asset
│   └── Bug_Centipede.asset
├── Waves/
│   ├── Wave_01.asset
│   ├── Wave_02.asset
│   └── Wave_03.asset
└── Machine/
    └── MachineData.asset
```

---

## BugData (ScriptableObject)

```csharp
[CreateAssetMenu(menuName = "DrillCorp/BugData")]
public class BugData : ScriptableObject {
    public string bugType;
    public int hp;
    public float moveSpeed;
    public int attackDamage;
    public float attackRange;
    public float attackInterval;  // 공격 간격(초)
    public int spawnWeight;       // 스폰 가중치 (높을수록 많이 나옴)
    public int resourceDrop;      // 처치 시 자원 드롭
    public GameObject prefab;
}
```

| 필드 | 타입 | Beetle | Fly | Centipede(보스) |
|------|------|--------|-----|-----------------|
| hp | int | 30 | 15 | 300 |
| moveSpeed | float | 2.0 | 3.5 | 1.0 |
| attackDamage | int | 5 | 3 | 20 |
| attackRange | float | 1.5 | 4.0 | 2.0 |
| attackInterval | float | 1.0 | 1.5 | 2.0 |
| spawnWeight | int | 10 | 8 | 0 (보스 웨이브만) |
| resourceDrop | int | 3 | 1 | 30 |

---

## MachineData (ScriptableObject)

```csharp
[CreateAssetMenu(menuName = "DrillCorp/MachineData")]
public class MachineData : ScriptableObject {
    public int maxHp;
    public float maxFuel;
    public float fuelDecreasePerSec;  // 초당 연료 감소량
    public float drillSpeed;          // 채굴 속도 배율
    public float resourcePerSec;      // 초당 자원 채굴량
}
```

| 필드 | 타입 | 초기값 |
|------|------|--------|
| maxHp | int | 500 |
| maxFuel | float | 100 |
| fuelDecreasePerSec | float | 0.5 |
| drillSpeed | float | 1.0 |
| resourcePerSec | float | 2.0 |

---

## WaveData (ScriptableObject)

```csharp
[CreateAssetMenu(menuName = "DrillCorp/WaveData")]
public class WaveData : ScriptableObject {
    public int waveNumber;
    public float spawnInterval;     // 스폰 간격(초)
    public int maxBugsAtOnce;       // 동시 최대 벌레 수
    public List<BugData> bugTypes;  // 이번 웨이브 등장 벌레
    public bool isBossWave;
}
```

| 웨이브 | spawnInterval | maxBugsAtOnce | 등장 벌레 | 보스 |
|--------|--------------|---------------|-----------|------|
| 1 | 3.0 | 5 | Beetle | ❌ |
| 2 | 2.5 | 8 | Beetle, Fly | ❌ |
| 3 | 2.0 | 10 | Beetle, Fly | ❌ |
| 4 | 1.5 | 12 | Beetle, Fly | ❌ |
| 5 | 2.0 | 5 | Centipede | ✅ |

---

## DataManager 저장 데이터 (JSON)

아웃게임 데이터는 JSON으로 저장/로드

```json
{
  "currency": 0,
  "upgrades": {
    "machineHp": 0,
    "machineFuel": 0,
    "drillSpeed": 0
  },
  "unlockedCharacters": ["default"],
  "selectedCharacter": "default"
}
```
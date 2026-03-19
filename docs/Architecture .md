# 아키텍처 패턴

## 전체 구조

```
┌─────────────────────────────────┐
│          GameManager            │  ← 씬 전환, 게임 상태
│          DataManager            │  ← 저장/로드
│          AudioManager           │  ← 사운드
└────────────┬────────────────────┘
             │ 이벤트로 통신
┌────────────▼────────────────────┐
│  InGame                         │
│  ├── MachineController          │  ← HP/FUEL
│  ├── BugSpawner                 │  ← 스폰
│  ├── BugAI                      │  ← 이동/공격
│  ├── AimController              │  ← 사격
│  └── WaveManager                │  ← 웨이브
└─────────────────────────────────┘
             │
┌────────────▼────────────────────┐
│  ScriptableObject Data          │
│  ├── BugData                    │
│  ├── WaveData                   │
│  └── MachineData                │
└─────────────────────────────────┘
```

---

## 1. 싱글톤 (전역 매니저)

전역 접근이 필요한 시스템에 사용

```csharp
GameManager.Instance.StartSession();
DataManager.Instance.AddCurrency(100);
```

- `GameManager` : 씬 전환, 게임 상태 (Playing, Success, Fail)
- `DataManager` : 재화, 강화 수치, 캐릭터 데이터 저장/로드
- `AudioManager` : 사운드 관리

---

## 2. 이벤트 시스템 (오브젝트 간 통신)

직접 참조 대신 이벤트로 결합도를 낮춤

```csharp
// GameEvents.cs - 이벤트 목록
public static class GameEvents {
    public static Action<int> OnMachineDamaged;
    public static Action OnBugDied;
    public static Action OnSessionSuccess;
    public static Action OnSessionFail;
}

// 발행
GameEvents.OnMachineDamaged?.Invoke(10);

// 구독
GameEvents.OnMachineDamaged += HandleMachineDamaged;
```

이벤트 흐름 예시:
```
벌레가 머신 공격
  → GameEvents.OnMachineDamaged 발생
    → MachineController (HP 감소)
    → UIManager (HP바 업데이트)
    → GameManager (HP 0 체크 → 세션 실패)
```

---

## 3. ScriptableObject (수치 데이터)

코드 수정 없이 인스펙터에서 밸런스 튜닝 가능

```csharp
[CreateAssetMenu(menuName = "DrillCorp/BugData")]
public class BugData : ScriptableObject {
    public int hp;
    public float moveSpeed;
    public int attackDamage;
}
```

---

## 4. OOP 적용

### 상속 - 벌레 종류별 분리
```csharp
public class BugBase : MonoBehaviour, IDamageable {
    public BugData data;
    public virtual void Attack() { }
    public virtual void Die() { }
}

public class BeetleBug : BugBase {
    public override void Attack() { /* 근접 공격 */ }
}

public class FlyBug : BugBase {
    public override void Attack() { /* 원거리 공격 */ }
}

public class CentipedeBug : BugBase {
    public override void Attack() { /* 범위 공격 (보스) */ }
}
```

### 인터페이스 - 피격 가능한 오브젝트
```csharp
public interface IDamageable {
    void TakeDamage(int damage);
}

// 사격 시스템에서 IDamageable만 알면 됨
hit.GetComponent<IDamageable>()?.TakeDamage(damage);
```

### 캡슐화 - 데이터 보호
```csharp
public class MachineController : MonoBehaviour {
    private float _currentFuel;
    public float CurrentFuel => _currentFuel;  // 읽기만 허용

    public void ConsumeFuel(float amount) {
        _currentFuel -= amount;
        if (_currentFuel <= 0) OnFuelEmpty();
    }
}
```

### 다형성 - 통합 관리
```csharp
// BugBase 타입으로 모든 벌레 통합 관리
List<BugBase> bugs = new List<BugBase>();
// Beetle이든 Fly든 다 담김
```
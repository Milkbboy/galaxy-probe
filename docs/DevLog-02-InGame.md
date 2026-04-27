# 2단계 - 인게임 세션 개발 로그

## 개요
Drill-Corp 게임의 인게임 세션 시스템 구축. 머신, 벌레, 사격, 웨이브 시스템 구현.

---

## 타임라인

| 시간 | 작업 내용 |
|------|-----------|
| 2026-03-18 | 2단계 인게임 세션 구현 시작 |
| └ | 폴더 구조 생성: Machine, Bug, Aim, Wave |
| └ | IDamageable.cs 작성 - 데미지 인터페이스 |
| └ | MachineController.cs 작성 - HP/FUEL/채굴 시스템 |
| └ | BugBase.cs 작성 - 벌레 기본 클래스 |
| └ | BeetleBug.cs 작성 - 딱정벌레 (근접) |
| └ | FlyBug.cs 작성 - 파리 (비행, 호버링) |
| └ | CentipedeBug.cs 작성 - 지네 (돌진) |
| └ | BugSpawner.cs 작성 - 벌레 스폰 시스템 |
| └ | AimController.cs 작성 - 마우스 조준/충전/사격 |
| └ | WaveManager.cs 작성 - 웨이브 순서 제어 |
| └ | **3D 탑다운 전환** - 모든 스크립트 XZ 평면 기준으로 수정 |
| └ | BugSpawner.cs - XZ 평면 원형 스폰으로 변경 |
| └ | BugBase.cs - XZ 평면 이동, Y축 회전으로 변경 |
| └ | FlyBug.cs - Y축 호버링 (높이)으로 변경 |
| └ | CentipedeBug.cs - XZ 평면 이동으로 변경 |
| └ | AimController.cs - Raycast 바닥 평면 감지, Physics.OverlapSphere 사용 |

---

## 생성된 파일

| 파일명 | 경로 | 역할 |
|--------|------|------|
| IDamageable.cs | `Scripts/Machine/` | 데미지 인터페이스 |
| MachineController.cs | `Scripts/Machine/` | 머신 HP/FUEL/채굴 |
| BugBase.cs | `Scripts/Bug/` | 벌레 기본 클래스 |
| BeetleBug.cs | `Scripts/Bug/` | 딱정벌레 |
| FlyBug.cs | `Scripts/Bug/` | 파리 |
| CentipedeBug.cs | `Scripts/Bug/` | 지네 |
| BugSpawner.cs | `Scripts/Bug/` | 벌레 스폰 |
| AimController.cs | `Scripts/Aim/` | 조준/사격 시스템 |
| WaveManager.cs | `Scripts/Wave/` | 웨이브 관리 |

---

## IDamageable.cs

### 역할
- 데미지를 받을 수 있는 오브젝트의 공통 인터페이스
- 머신과 벌레 모두 구현

### 인터페이스 정의
```csharp
public interface IDamageable
{
    float CurrentHealth { get; }
    float MaxHealth { get; }
    bool IsDead { get; }

    void TakeDamage(float damage);
    void Heal(float amount);
}
```

---

## MachineController.cs

### 역할
- 화면 중앙 채굴 머신 제어
- HP/FUEL 시스템
- 채굴량 계산
- 세션 종료 조건 체크

### 주요 프로퍼티
| 프로퍼티 | 타입 | 설명 |
|----------|------|------|
| `CurrentHealth` | float | 현재 HP |
| `MaxHealth` | float | 최대 HP |
| `CurrentFuel` | float | 현재 연료 |
| `MaxFuel` | float | 최대 연료 |
| `TotalMined` | int | 총 채굴량 |
| `IsDead` | bool | HP 0 여부 |
| `IsFuelEmpty` | bool | 연료 0 여부 |

### Inspector 설정값
```
Max Health: 100
Max Fuel: 60 (초)
Fuel Consume Rate: 1 (초당)
Mining Rate: 1
```

### 세션 종료 조건
- **HP 0**: 세션 실패 → `GameManager.SessionFailed()`
- **연료 0**: 세션 성공 → `GameManager.SessionSuccess()` + 재화 지급

---

## BugBase.cs (추상 클래스)

### 역할
- 모든 벌레의 기본 클래스
- 이동, 공격, 데미지 처리 공통 로직

### 주요 기능
| 메서드 | 설명 |
|--------|------|
| `MoveToTarget()` | 머신을 향해 이동 |
| `TryAttack()` | 공격 범위 내 시 공격 |
| `TakeDamage()` | 피격 처리 |
| `Die()` | 사망 처리 + 이벤트 발행 |
| `Initialize()` | 스탯 초기화 (스포너에서 호출) |

### Inspector 설정값
```
Max Health: 10
Move Speed: 2
Attack Damage: 5
Attack Cooldown: 1초
```

---

## 벌레 종류

### BeetleBug (딱정벌레)
- 기본 근접 공격 벌레
- 느리지만 체력이 높음
- 공격 범위: 1.2

### FlyBug (파리)
- 빠른 비행 벌레
- 호버링 효과 (상하 부유)
- 빠르지만 체력 낮음
- 공격 범위: 0.8

### CentipedeBug (지네)
- 대형 벌레
- 일정 거리 이내 돌진 (속도 2배)
- 높은 체력, 높은 데미지
- 공격 범위: 1.5

---

## BugSpawner.cs

### 역할
- 벌레 프리팹 관리
- 화면 바깥 원형 범위에서 스폰

### Inspector 설정
```csharp
[SerializeField] private float _spawnRadius = 10f;
[SerializeField] private Transform _centerPoint;
[SerializeField] private List<BugSpawnData> _bugDataList;
```

### 사용 예시
```csharp
// 단일 스폰
bugSpawner.SpawnBug(BugType.Beetle);

// 다수 스폰
bugSpawner.SpawnBugs(BugType.Fly, 5);
```

---

## AimController.cs

### 역할
- 마우스 커서 추적
- 0.5초 충전 시스템
- 범위 내 벌레 데미지 처리

### 조작법
1. 마우스로 조준
2. 좌클릭 홀드 → 충전 시작
3. 0.5초 후 완충 (색상 변화)
4. 버튼 떼면 발사

### 충전 상태별 색상
| 상태 | 색상 |
|------|------|
| 기본 | 흰색 |
| 충전 중 | 흰색 → 노란색 |
| 완충 | 빨간색 |

### Inspector 설정
```
Aim Radius: 0.5 (공격 범위)
Charge Time: 0.5초
Damage: 10
Bug Layer: 벌레 레이어 설정 필요
```

---

## WaveManager.cs

### 역할
- 웨이브 순서 제어
- 벌레 스폰 타이밍 관리
- 웨이브 완료 체크

### 웨이브 데이터 구조
```csharp
[Serializable]
public class WaveEntry
{
    public BugType BugType;
    public int Count;
    public float SpawnDelay = 0.5f;
}

[Serializable]
public class WaveData
{
    public string WaveName;
    public List<WaveEntry> Entries;
    public float DelayBeforeNextWave = 3f;
}
```

### 웨이브 흐름
1. 웨이브 시작 → `OnWaveStarted` 이벤트
2. 벌레 순차 스폰 (SpawnDelay 간격)
3. 모든 벌레 처치 대기
4. 웨이브 완료 → `OnWaveCompleted` 이벤트
5. 딜레이 후 다음 웨이브 자동 시작

---

## Unity 설정 가이드

### 0. 카메라 설정 (3D 탑다운)
1. Main Camera 선택
2. Transform 설정:
   - Position: (0, 10, 0)
   - Rotation: (90, 0, 0) - 아래를 향함
3. Camera 컴포넌트:
   - Projection: **Orthographic**
   - Size: **10** (넓게 보려면 값 증가)

| Size | 보이는 범위 (세로) |
|------|-------------------|
| 5 | 10유닛 |
| 10 | 20유닛 |
| 15 | 30유닛 |

### 1. Machine 설정
1. 3D Object > Cylinder 또는 Cube 생성 → 이름: `Machine`
2. Position: (0, 0, 0)
3. MachineController 컴포넌트 추가
4. Collider 자동 포함됨

### 2. Bug 프리팹 생성
1. 3D Object > Cube 또는 Sphere 생성 (Scale: 0.5)
2. 해당 스크립트 컴포넌트 추가 (BeetleBug, FlyBug, CentipedeBug)
3. **Collider** 자동 포함 (3D)
4. Layer: `Bug` 설정
5. Prefabs 폴더에 저장

### 3. BugSpawner 설정
1. 빈 GameObject 생성 → 이름: `BugSpawner`
2. BugSpawner 컴포넌트 추가
3. **Center Point: Machine Transform 연결**
4. Spawn Radius: 10
5. Bug Data List에 프리팹 등록

### 4. AimController 설정
1. 3D Object > Sphere 생성 (Scale: 0.3) → 이름: `Aim`
2. AimController 컴포넌트 추가
3. **Bug Layer 마스크 설정**
4. Crosshair Renderer에 MeshRenderer 연결

### 5. WaveManager 설정
1. 빈 GameObject 생성 → 이름: `WaveManager`
2. WaveManager 컴포넌트 추가
3. Bug Spawner 연결
4. Waves 리스트에 웨이브 데이터 설정

### 6. 레이어 설정
- Edit > Project Settings > Tags and Layers
- Layer 추가: `Bug`
- 모든 벌레 프리팹에 Bug 레이어 적용

---

## 3D 탑다운 좌표계

```
Y (높이/카메라)
│
│   ☐ 카메라 (위에서 아래로)
│   ↓
└───────── X
    ╲
     ╲ Z
      ╲
       (바닥 = XZ 평면, Y=0)
```

- **바닥**: XZ 평면 (Y=0)
- **높이**: Y축
- **카메라**: Y축 위에서 아래로 내려다봄
- **이동**: XZ 평면에서만
- **회전**: Y축 기준 (Quaternion.Euler(0, angle, 0))

---

## 다음 단계
3단계 - UI
- HP바 / FUEL바
- 에임 충전 게이지
- 채굴량 표시
- 세션 결과 화면

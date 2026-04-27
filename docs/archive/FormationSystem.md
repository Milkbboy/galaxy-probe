# Formation(군집) 시스템

> 최종 갱신: 2026-04-13
> Phase 2 구현 결과

## 1. 개요

**Nuclear Throne 스타일의 탑다운 디펜스**에서 600~1000마리 벌레 떼를 효율적으로 처리하기 위한 군집 시스템입니다.

### 핵심 개념

```
Wave → Formation → Leader + Members
              └─ Bug 여러 마리가 진형을 이루어 이동
```

- **개별 Bug Behavior 시스템과 공존** (충돌 없음)
- **머신과의 거리에 따라 제어권 전환** (Formation ↔ 개별 Behavior)
- **Object Pooling으로 600+ 동시 스폰 지원**

---

## 2. 계층 구조

```
WaveManager
  └─ FormationSpawner.Spawn(FormationData)
        ├─ BugPool.Get() × (1 + N)
        ├─ FormationGroup 생성
        │   ├─ Leader (이동 주체)
        │   └─ Members[] (리더 오프셋 추적)
        └─ 각 Bug에 FormationMember 부착
```

| 계층 | 역할 | 관련 파일 |
|------|------|----------|
| **WaveManager** | 웨이브별 Formation 스폰 타이밍 제어 | WaveManager.cs |
| **FormationSpawner** | 외곽 스폰 위치 계산 + 진형 조립 | FormationSpawner.cs |
| **FormationGroup** | 리더 이동 + 멤버 동기화 | FormationGroup.cs |
| **FormationMember** | 로컬 오프셋 유지 + Phase 판정 | FormationMember.cs |
| **BugPool** | Object Pooling 싱글톤 | BugPool.cs |

---

## 3. 파일 구조

```
Assets/_Game/Scripts/Bug/
├── Pool/
│   ├── BugPool.cs               # 싱글톤 풀 관리자
│   ├── BugPoolConfig.cs         # SO: 풀 설정
│   └── PooledBug.cs             # 풀 마커 컴포넌트
│
├── Formation/
│   ├── FormationData.cs         # SO: 진형 설정
│   ├── FormationGroup.cs        # 군집 관리자
│   ├── FormationMember.cs       # 멤버 마커
│   ├── FormationSpawner.cs      # 스폰 담당
│   └── FormationOffsetCalculator.cs  # 진형 패턴 계산 (static)
│
├── BugController.cs             # (수정) Formation 연동
├── BugManager.cs                # Update 분산 처리 (준비됨)
└── OffscreenVisibilityTracker.cs  # 가시성 캐싱
```

---

## 4. 진형 종류

| Type | 배치 | 특징 |
|------|------|------|
| **Cluster** | 원형 링 확장 | 밀집 뭉텅이 (기본) |
| **Line** | 리더 뒤로 여러 줄 | 돌파형 |
| **Swarm** | 반경 내 랜덤 + Jitter | 느슨한 군집 |

### FormationData SO 필드

```
FormationType        : Cluster / Line / Swarm
FormationSize        : Small (15~35) / Medium (40~90) / Large (120~200)
MinMembers/MaxMembers: 실제 생성 수량 범위
Spacing              : 멤버 간 최소 거리
FormationRadius      : 진형 크기
Jitter               : 랜덤 흔들림 (Swarm)
LeaderBugData        : 리더 종류
Members[]            : 멤버 BugData + 비율
SpeedMultiplier      : 진형 이동 속도 배율
```

---

## 5. 리더(Leader) 역할

**리더 = 진형의 기준점 + 이동 담당**

```
[리더] 머신 방향 이동 계산 → 1회
  ↓
[멤버] 리더 위치/회전 기준 오프셋 유지 → 가벼운 Lerp
```

### 왜 리더가 필요한가?

- 600마리가 각자 머신 거리 계산 = 연산 낭비
- 리더 하나만 계산 → 멤버는 오프셋 따라가기
- 진형 유지 자연스러움

### 리더 배치 패턴별

```
Cluster:          Line:           Swarm:
    M M           [L]             M    M
  M [L] M         M M M         M [L]   M
    M M           M M M              M
```

---

## 6. Phase 전환 시스템

머신과의 거리에 따라 **제어권이 자동으로 이양**됩니다.

| Phase | 거리 조건 | 제어 주체 | Lerp |
|-------|----------|----------|------|
| **Phase 1** | > 12f | Formation (100%) | 1.0 |
| **Phase 2** | 6f ~ 12f | Formation (느슨) | 0.4 |
| **Phase 3** | < 6f | 개별 BugController | - |

### 동작 흐름

```
Phase 1 (먼 거리):
  - Formation이 위치/회전 완전 제어
  - BugController: Movement 비활성, 공격/스킬만 동작

Phase 2 (중간):
  - Formation이 40% 영향력만
  - 살짝 흐트러지기 시작

Phase 3 (근접):
  - Formation 이탈
  - BugController.SetMovementExternallyControlled(false)
  - 개별 Behavior가 이어받음 (달라붙어 공격)
```

---

## 7. BugController 연동

### 추가된 필드/메서드

```csharp
private bool _movementExternallyControlled;
public bool MovementExternallyControlled => _movementExternallyControlled;
public void SetMovementExternallyControlled(bool controlled);
```

### Update 분기

```csharp
if (!_movementExternallyControlled)
{
    _currentMovement?.UpdateMovement(_target);  // 기존 로직
}
// 공격/스킬/패시브는 항상 동작
```

### Pool 복귀

```csharp
private void Die()
{
    // ... VFX, 이벤트, 정리 ...

    var pooled = GetComponent<PooledBug>();
    if (pooled != null && pooled.IsPooled)
    {
        ResetForPool();           // 상태 초기화
        pooled.ReturnToPool();    // 풀로 복귀
    }
    else
    {
        Destroy(gameObject);      // 기존 방식
    }
}
```

---

## 8. BugPool 시스템

### 개념

```
Awake() → Config 기반 각 BugData별 InitialSize만큼 Instantiate (비활성)
Get(bugData) → 큐에서 꺼내 활성화
Return(obj) → 비활성화 후 큐로 복귀
```

### BugPoolConfig 구성 예시

| BugData | InitialSize | 비고 |
|---------|-------------|------|
| Beetle | 200 | 기본 잡몹 |
| Fly | 150 | 비행 잡몹 |
| Tank | 30 | 방어형 |
| Spitter | 50 | 원거리 |
| Bomber | 50 | 자폭 |
| Elite | 10 | 엘리트 |
| Orbiter | 30 | 공전형 |
| Healer | 10 | 힐러 |

**Max Active Total**: 1000 (전체 동시 활성 상한)

### AllowGrow 옵션

- `true`: 부족 시 자동 Instantiate (안전)
- `false`: InitialSize 고정 (성능 예측 가능, 테스트 후 권장)

---

## 9. WaveData 확장

기존 `SpawnGroup[]`과 **공존**하며 `FormationSpawnEntry[]` 추가.

### FormationSpawnEntry

```
FormationData: Formation 설정 참조
Count        : 이 Formation을 몇 번 스폰
StartDelay   : 웨이브 시작 후 첫 스폰 대기
SpawnInterval: Formation 간 간격
```

### 웨이브 스폰 흐름

```
WaveStart
├── FormationSpawn (병렬 코루틴)
│   ├── Formation A × 2 (15초 간격)
│   └── Formation B × 3 (20초 간격)
└── 레거시 SpawnGroup (순차)
    └── 개별 Bug 스폰
```

---

## 10. 최적화 준비 (BugManager)

600+ Bug Update를 여러 프레임에 분산 처리하는 **구조만 준비**된 상태.
실제 BugController 이전은 별도 단계.

```
BugManager.Update()
├── BugsPerFrame(80)만큼 순차 처리
├── 카메라 밖 Bug 50%만 처리 (OffscreenTickRatio)
└── Ring Buffer로 다음 프레임 이어서

600마리 / 80 = 약 8프레임마다 1회 Tick
```

---

## 11. 씬 설정 체크리스트

### 필수 컴포넌트 배치

| GameObject | 컴포넌트 | 필수 필드 |
|-----------|---------|----------|
| **BugPool** | BugPool | Config (BugPoolConfig.asset) |
| **FormationSpawner** | FormationSpawner | Machine Target (머신 Transform) |
| **WaveManager** | WaveManager | FormationSpawner, WaveDataAssets |
| **Machine** | - | Tag: "Machine" (자동 탐색용) |

### 필수 에셋

- `Assets/_Game/Data/BugPoolConfig.asset`
- `Assets/_Game/Data/Formation_*.asset` (진형별)
- WaveData에 `FormationSpawns` 배열 설정

### 권장 파라미터

| 항목 | 값 | 이유 |
|------|-----|------|
| FormationSpawner.spawnRadius | 18~25 | 화면 밖에서 스폰되어 보임 |
| Phase1To2Distance | 12 | Phase 전환 시점 |
| Phase2To3Distance | 6 | 머신 근접 판정 |

---

## 12. 알려진 이슈 / 미구현

### 완료 ✅

- [x] Leader/Member 오프셋 시스템
- [x] Phase 1/2/3 자동 전환
- [x] BugPool + Return 흐름
- [x] BugController 제어권 이양
- [x] WaveManager Formation 스폰
- [x] 회전 동기화 (멤버도 머신 방향)

### 미구현 ⏳

- [ ] **리더 사망 처리**: 현재 리더 죽으면 Formation이 멈춤 (새 리더 선정 or 해체 필요)
- [ ] **투사체 Object Pool**: Bug Projectile이 Instantiate/Destroy 사용 중
- [ ] **VFX Pool**: Hit VFX 등
- [ ] **BugManager 실제 통합**: 현재 인터페이스만 준비됨
- [ ] **GPU Instancing**: 머티리얼 설정 필요

---

## 13. 주요 버그 수정 이력

### NaN 에러 (2026-04-13)

**증상**: `transform.localScale ... NaN`

**원인**: FormationSpawner가 BugPool에서 Get한 Bug를 `Initialize()` 호출 없이 사용
→ `_maxHealth = 0` → `_currentHealth / _maxHealth = NaN`

**수정**:
- `FormationSpawner.InitializeBug()` 추가
- `BugController.UpdateHpBar()`에 `_maxHealth > 0` 방어 체크

### 리더 제어권 누락 (2026-04-13)

**증상**: 리더가 팍 하고 이동 or 중간에 멈춤

**원인**: `FormationGroup.AddMember()`는 멤버만 제어권 획득, 리더 제외

**수정**: `FormationGroup.Setup()`에서 리더도 자동으로 `SetMovementExternallyControlled(true)`

### 회전 누락

**증상**: 멤버가 머신 방향 회전 안 함

**원인**: `UpdateMembers()`에 회전 로직 없음

**수정**: 리더 rotation을 모든 멤버에 Slerp 적용

---

## 14. 참고 문서

- 전체 기획: `Overview-Plan.md`
- Bug Behavior: `BugBehaviorSystemAnalysis.md`
- 카메라 시스템: `Sys-Camera.md`

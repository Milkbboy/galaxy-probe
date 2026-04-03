# Bug 행동 시스템 분석 보고서

## 1. 폴더/파일 구조

```
Assets/_Game/Scripts/Bug/Behaviors/
├── IBehavior.cs                              # 인터페이스 정의 (5개)
├── BehaviorCondition.cs                      # 조건부 행동 처리
│
├── Data/
│   ├── BugBehaviorData.cs                   # 통합 행동 데이터 SO
│   ├── MovementBehaviorData.cs              # 이동 타입/데이터 정의
│   ├── AttackBehaviorData.cs                # 공격 타입/데이터 정의
│   ├── PassiveBehaviorData.cs               # 패시브 타입/데이터 정의
│   ├── SkillBehaviorData.cs                 # 스킬 타입/데이터 정의
│   └── TriggerBehaviorData.cs               # 트리거 타입/데이터 정의
│
├── Movement/
│   ├── MovementBehaviorBase.cs              # Base 클래스 + Factory
│   ├── LinearMovement.cs                    # 직선 이동
│   ├── HoverMovement.cs                     # 공중 부유
│   ├── BurstMovement.cs                     # 멈췄다 돌진
│   ├── RangedMovement.cs                    # 사거리 유지 + 좌우 이동
│   ├── RetreatMovement.cs                   # 후퇴
│   ├── SlowStartMovement.cs                 # 점진 가속
│   ├── OrbitMovement.cs                     # 타겟 주위 선회
│   └── TeleportMovement.cs                  # 순간이동
│
├── Attack/
│   ├── AttackBehaviorBase.cs                # Base 클래스 + Factory
│   ├── MeleeAttack.cs                       # 근접 공격
│   ├── ProjectileAttack.cs                  # 원거리 투사체
│   ├── CleaveAttack.cs                      # 부채꼴 범위 공격
│   ├── SpreadAttack.cs                      # 다발 발사
│   ├── BeamAttack.cs                        # 지속 레이저
│   └── BugProjectile.cs                     # 투사체 컴포넌트
│
├── Passive/
│   ├── PassiveBehaviorBase.cs               # Base 클래스 + Factory
│   ├── ArmorPassive.cs                      # 데미지 감소
│   ├── DodgePassive.cs                      # 확률 회피
│   ├── ShieldPassive.cs                     # 데미지 흡수 + 재생
│   ├── RegenPassive.cs                      # 체력 재생
│   ├── PoisonAttackPassive.cs               # 독 공격
│   └── BurrowPassive.cs                     # 땅속 숨기
│
├── Skill/
│   ├── SkillBehaviorBase.cs                 # Base 클래스 + Factory
│   ├── NovaSkill.cs                         # 전방향 폭발
│   ├── SpawnSkill.cs                        # 졸개 소환
│   ├── BuffAllySkill.cs                     # 아군 강화
│   └── HealAllySkill.cs                     # 아군 회복
│
└── Trigger/
    ├── TriggerBehaviorBase.cs               # Base 클래스 + Factory
    ├── EnrageTrigger.cs                     # 광폭화
    ├── ExplodeOnDeathTrigger.cs             # 사망 시 폭발
    └── PanicBurrowTrigger.cs                # HP 낮을 때 숨기
```

**총 40개의 C# 파일**

---

## 2. 인터페이스 계층 (IBehavior.cs)

모든 행동은 다음 5가지 인터페이스 중 하나를 구현:

| 인터페이스 | 목적 | 핵심 메서드 |
|-----------|------|-----------|
| `IBehavior` | 기본 인터페이스 | `Initialize(BugController)`, `Cleanup()` |
| `IMovementBehavior` | 매 프레임 이동 | `UpdateMovement(Transform)`, `SpeedMultiplier` |
| `IAttackBehavior` | 쿨다운 기반 공격 | `TryAttack(Transform)`, `AttackRange`, `DamageMultiplier` |
| `ISkillBehavior` | 쿨다운 기반 스킬 | `TryUse(Transform)`, `UpdateCooldown(float)`, `IsReady` |
| `IPassiveBehavior` | 상시 적용 효과 | `UpdatePassive(float)`, `ProcessIncomingDamage()`, `ProcessOutgoingDamage()` |
| `ITriggerBehavior` | 조건 발동 | `CheckAndTrigger()`, `OnDeath()`, `HasTriggered`, `TriggerOnDeath` |

---

## 3. Base 클래스 패턴 분석

### 3.1 공통 패턴

모든 Base 클래스가 공유하는 구조:

```csharp
public abstract class [Category]BehaviorBase : I[Category]Behavior
{
    protected BugController _bug;

    public virtual void Initialize(BugController bug)
    {
        _bug = bug;
    }

    public virtual void Cleanup()
    {
        _bug = null;
    }

    // Factory 메서드
    public static [Category]BehaviorBase Create([Type]Type type, ...)
    {
        switch(type) { ... }
    }
}
```

### 3.2 Base 클래스별 특징

#### MovementBehaviorBase
- **공유 메서드:**
  - `GetMoveSpeed()` - 버프 적용된 이동 속도
  - `GetDirectionToTarget(Transform)` - XZ 평면 정규화 방향
  - `RotateTowards(Vector3)` - Y축 기준 회전
- **Factory:** 8가지 타입 생성

#### AttackBehaviorBase
- **쿨다운 기반** 공격 처리
- **공유 메서드:**
  - `GetDamage()` - 버프 적용된 공격력
  - `DealDamage(Transform, float)` - 패시브 연동 + IDamageable 호출
  - `PlayHitVfx(Vector3)` - VFX 재생
- **Factory:** 6가지 타입 생성
- **흐름:** 쿨다운 체크 → PerformAttack() → OnAttackPerformed 이벤트

#### PassiveBehaviorBase
- **가장 단순** (대부분 가상/기본 구현)
- **기본 구현:**
  - `UpdatePassive()`: 빈 구현
  - `ProcessIncomingDamage()`: 데미지 그대로 반환
  - `ProcessOutgoingDamage()`: 빈 구현
- **Factory:** 6가지 타입 생성

#### SkillBehaviorBase
- **쿨다운 기반** 스킬 처리
- **공유 데이터:** `_cooldown`, `_currentCooldown`, `_range`
- **공유 메서드:**
  - `UpdateCooldown(float)` - 매 프레임 쿨다운 감소
  - `IsReady` - 쿨다운 완료 여부
- **Factory:** 4가지 타입 생성

#### TriggerBehaviorBase
- **조건 발동** 기반
- **플래그:** `_hasTriggered` (1회성), `_triggerOnDeath` (사망 시 발동)
- **공유 메서드:**
  - `Trigger()` - HasTriggered 체크 후 OnTriggered() 호출
  - `OnDeath()` - 사망 시 호출
- **Factory:** 3가지 타입 생성

---

## 4. 구현체 목록

### 4.1 Movement (8종)

| 클래스 | 동작 | 파라미터 |
|--------|------|---------|
| LinearMovement | 직진 | - |
| HoverMovement | XZ 이동 + Y 부유 | param1=높이, param2=주기 |
| BurstMovement | 대기 → 돌진 | param1=대기시간, param2=속도배율 |
| RangedMovement | 사거리 유지 + 좌우 | param1=거리, param2=횡이동배율 |
| RetreatMovement | 후퇴 | param1=지속시간, param2=속도배율 |
| SlowStartMovement | 점진 가속 | param1=시작속도비율, param2=도달시간 |
| OrbitMovement | 타겟 주위 공전 | param1=반경, param2=각속도 |
| TeleportMovement | 순간이동 | param1=쿨다운, param2=거리 |

### 4.2 Attack (5종 + 투사체)

| 클래스 | 동작 | 파라미터 |
|--------|------|---------|
| MeleeAttack | 즉발 데미지 | - |
| ProjectileAttack | 투사체 발사 | param1=속도 |
| CleaveAttack | 부채꼴 범위 | param1=각도 |
| SpreadAttack | 다발 발사 | param1=발수, param2=각도 |
| BeamAttack | 지속 레이저 | param1=지속시간, param2=틱간격 |

### 4.3 Passive (6종)

| 클래스 | 효과 | 동작 방식 |
|--------|------|----------|
| ArmorPassive | 데미지 감소 | ProcessIncomingDamage |
| DodgePassive | 확률 회피 | ProcessIncomingDamage |
| ShieldPassive | 흡수 + 재생 | ProcessIncomingDamage + UpdatePassive |
| RegenPassive | 체력 재생 | UpdatePassive |
| PoisonAttackPassive | 독 적용 | ProcessOutgoingDamage |
| BurrowPassive | 땅속 숨기 | UpdatePassive (상태 머신) |

### 4.4 Skill (4종)

| 클래스 | 효과 | 특징 |
|--------|------|------|
| NovaSkill | 전방향 폭발 | OverlapSphere, 범위 표시 |
| SpawnSkill | 졸개 소환 | Prefab 인스턴스화 |
| BuffAllySkill | 아군 강화 | Aura 방식, 지속 업데이트 |
| HealAllySkill | 아군 회복 | Aura 방식, 주기적 회복 |

### 4.5 Trigger (3종)

| 클래스 | 조건 | 효과 |
|--------|------|------|
| EnrageTrigger | HP ≤ N% | 공격력/이속 증가 |
| ExplodeOnDeathTrigger | 사망 | 폭발 데미지 |
| PanicBurrowTrigger | HP ≤ N% + 피격 | BurrowPassive 발동 |

---

## 5. 데이터 흐름

### 5.1 ScriptableObject 구조

```
[Category]BehaviorData (SO)
├── _type: [Category]Type (enum)
├── _param1, _param2: float
├── _effectPrefab: GameObject
├── _projectilePrefab: GameObject (Attack만)
└── _stringParam: string (Spawn 등)
```

### 5.2 통합 BugBehaviorData

```csharp
BugBehaviorData (SO)
├── _defaultMovement: MovementBehaviorData
├── _defaultAttack: AttackBehaviorData
├── _conditionalMovements: List<ConditionalMovementData>
├── _conditionalAttacks: List<ConditionalAttackData>
├── _passives: List<PassiveBehaviorData>
├── _skills: List<SkillBehaviorData>
├── _triggers: List<TriggerBehaviorData>
└── _runtimeData: RuntimeBehaviorSet (Google Sheets용)
```

### 5.3 초기화 흐름

```
BugController.Start()
├── ApplyBugData() ─────────────────── 스탯 로드 (HP, 공격력, 이속)
├── InitializeBehaviors()
│   ├── BehaviorData 없음? ────────── SetupDefaultBehaviors() (Linear + Melee)
│   ├── UseRuntimeData? ───────────── InitializeFromRuntimeData()
│   └── SO 사용 ───────────────────── InitializeFromScriptableObjects()
│       ├── Movement: Create() → Initialize()
│       ├── Attack: Create() → Initialize()
│       ├── Passives[]: Create() → Initialize()
│       ├── Skills[]: Create() → Initialize()
│       └── Triggers[]: Create() → Initialize()
├── FindTarget()
└── CreateHpBar()
```

---

## 6. BugController 연동

### 6.1 Update 흐름

```
Update()
├── AliveTime 증가
├── JustAttacked 타이머 감소
├── Passive 업데이트 (전체 순회)
├── Burrow 상태 체크
│   └── Burrow 중 아니면:
│       ├── 조건부 행동 전환 체크
│       ├── Movement.UpdateMovement()
│       ├── Attack 처리 (범위 체크 → TryAttack)
│       └── Skill 업데이트 (쿨다운 → TryUse)
├── Trigger 체크 (CheckAndTrigger)
└── AllyJustDied 리셋
```

### 6.2 데미지 처리

```
TakeDamage(damage)
├── 무적/사망 체크
├── HitCount++
├── foreach Passive: damage = ProcessIncomingDamage(damage)
├── HP -= damage
├── UpdateHpBar()
├── HitFlash + VFX
└── HP ≤ 0 → Die()
```

### 6.3 조건부 행동 전환

```
UpdateConditionalBehaviors()
├── foreach ConditionalMovement:
│   └── if Condition.Evaluate() → currentMovement = behavior
└── foreach ConditionalAttack:
    └── if Condition.Evaluate() → currentAttack = behavior
```

**조건 타입:** `HP<`, `HP>`, `Distance<`, `Distance>`, `AfterAttack`, `HitCount>`, `Time>`, `AllyDead`

---

## 7. 발견된 문제점

### 7.1 구조적 문제

| 문제 | 현상 | 영향 |
|------|------|------|
| Factory 분산 | 각 Base에 Create() 메서드 | 새 행동 추가 시 Base 수정 필요 |
| 조건부 Attack 미구현 | InitializeFromScriptableObjects에서 누락 | 조건부 공격 전환 불가 |
| 런타임 초기화 미완성 | Skills/Triggers TODO 상태 | Google Sheets 연동 불완전 |
| Duration 미사용 | ConditionalBehavior.Duration 필드 존재 | 일시적 상태 전환 불가 |

### 7.2 성능 문제

| 문제 | 현상 | 개선안 |
|------|------|--------|
| Passive 전체 순회 | 매 프레임 모든 Passive UpdatePassive() | 업데이트 필요한 것만 분리 |
| 조건 매 프레임 평가 | 모든 조건 매 프레임 Evaluate() | 상태 변경 시에만 평가 |
| OverlapSphere 비용 | Nova 등에서 매번 새 배열 생성 | NonAlloc 버전 사용 |

### 7.3 설계 이슈

| 문제 | 현상 | 개선안 |
|------|------|--------|
| Burrow 상태 복잡 | CanBurrow 플래그로 상태 판정 | 명시적 State enum |
| 다중 조건 미지원 | 단일 Condition만 가능 | AND/OR 복합 조건 |
| 우선순위 불명확 | 첫 번째 매칭 조건 선택 | Priority 필드 추가 |
| Buff 시스템 중복 | Behavior 배율 vs Controller 버프 | 통합 BuffSystem |
| 이벤트 부족 | OnAttackPerformed만 존재 | 행동별 이벤트 추가 |

### 7.4 코드 품질

| 문제 | 예시 | 개선안 |
|------|------|--------|
| 매직 넘버 | `_justAttackedTimer = 0.5f` | const 정의 |
| 문자열 조건 | `"HP<30"` 파싱 | 타입 세이프 빌더 |
| null 체크 중복 | 각 메서드마다 반복 | Guard 클래스 |

---

## 8. 개선 권장사항

### 우선순위 높음

1. **조건부 Attack 초기화 완성**
   - `InitializeFromScriptableObjects()`에 ConditionalAttacks 처리 추가
   - 위치: `BugController.cs:330` 부근

2. **런타임 초기화 완성**
   - `InitializeFromRuntimeData()`에 Skills/Triggers 처리 추가
   - 위치: `BugController.cs:481` (TODO 주석)

3. **Factory 중앙화**
   - `BehaviorFactory` 클래스 생성
   - 모든 Create() 메서드를 한 곳으로 이동

### 우선순위 중간

4. **성능 최적화**
   - UpdatePassive 필요한 Passive만 별도 리스트 관리
   - 조건 평가 캐싱 (상태 변경 시에만)

5. **상태 시스템 명확화**
   - `BugState` enum 추가 (Normal, Burrowed, Stunned 등)
   - Burrow 상태를 BugController에서 직접 관리

6. **다중 조건 지원**
   - `ConditionGroup` 클래스 추가 (AND/OR 연산)
   - Priority 필드로 우선순위 명시

### 우선순위 낮음

7. **이벤트 시스템 확장**
   - `OnPassiveActivated`, `OnSkillUsed`, `OnTriggered` 추가

8. **Buff 시스템 통합**
   - `BuffManager` 클래스로 모든 버프 관리

9. **코드 정리**
   - 매직 넘버 → const
   - 문자열 조건 → 빌더 패턴

---

## 9. 리팩토링 작업 목록

### Phase 1: 버그 수정
- [ ] 조건부 Attack 초기화 코드 추가
- [ ] Skills/Triggers 런타임 초기화 완성

### Phase 2: 구조 개선
- [ ] BehaviorFactory 클래스 생성
- [ ] 각 Base의 Create()를 Factory로 이동
- [ ] BugState enum 추가

### Phase 3: 성능 최적화
- [ ] Passive 업데이트 필터링
- [ ] 조건 평가 캐싱
- [ ] OverlapSphere → NonAlloc

### Phase 4: 기능 확장
- [ ] 다중 조건 (ConditionGroup)
- [ ] 조건 우선순위 (Priority)
- [ ] ConditionalBehavior Duration 구현

### Phase 5: 코드 품질
- [ ] 매직 넘버 상수화
- [ ] 이벤트 시스템 확장
- [ ] Buff 시스템 통합

# Bug 행동 시스템 분석 보고서

> 최종 갱신: 2026-04-06

## 1. 개요

Bug 행동 시스템은 **데이터 기반 행동 조합**을 통해 다양한 적 유형을 생성하는 시스템입니다.

### 핵심 개념

![데이터 계층 구조](image/데이터%20계층%20구조.png)

| 계층 | 역할 | ScriptableObject |
|------|------|------------------|
| **Wave** | 스폰 타이밍/버그 정의 | WaveData |
| **Bug** | 체력, 공격력, 이속 등 스탯 | BugData |
| **BugBehavior** | 행동 조합 (어떻게 움직이고 공격할지) | BugBehaviorData |

---

## 2. 폴더/파일 구조

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

## 3. 인터페이스 계층 (IBehavior.cs)

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

## 4. Base 클래스 패턴 분석

### 4.1 공통 패턴

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

### 4.2 Base 클래스별 특징

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

## 5. 구현체 목록

### 5.1 Movement (8종)

| 클래스 | 동작 | 파라미터 |
|--------|------|---------|
| LinearMovement | 직진 + 옵션 (Strafe/Orbit/Retreat) | param1=옵션, param2=값 |
| HoverMovement | XZ 이동 + Y 부유 + Strafe | param1=높이, param2=주기 |
| BurstMovement | 대기 → 돌진 | param1=대기시간, param2=속도배율 |
| RangedMovement | 사거리 유지 + 좌우 | param1=거리, param2=횡이동배율 |
| RetreatMovement | 후퇴 | param1=지속시간, param2=속도배율 |
| SlowStartMovement | 점진 가속 | param1=시작속도비율, param2=도달시간 |
| OrbitMovement | 타겟 주위 공전 | param1=반경, param2=각속도 |
| TeleportMovement | 순간이동 | param1=쿨다운, param2=거리 |

### 5.2 Attack (5종 + 투사체)

| 클래스 | 동작 | 파라미터 |
|--------|------|---------|
| MeleeAttack | 즉발 데미지 | - |
| ProjectileAttack | 투사체 발사 | param1=속도 |
| CleaveAttack | 부채꼴 범위 | param1=각도 |
| SpreadAttack | 다발 발사 | param1=발수, param2=각도 |
| BeamAttack | 지속 레이저 | param1=지속시간, param2=틱간격 |

### 5.3 Passive (6종)

| 클래스 | 효과 | 동작 방식 |
|--------|------|----------|
| ArmorPassive | 데미지 감소 | ProcessIncomingDamage |
| DodgePassive | 확률 회피 | ProcessIncomingDamage |
| ShieldPassive | 흡수 + 재생 | ProcessIncomingDamage + UpdatePassive |
| RegenPassive | 체력 재생 | UpdatePassive |
| PoisonAttackPassive | 독 적용 | ProcessOutgoingDamage |
| BurrowPassive | 땅속 숨기 | UpdatePassive (상태 머신) |

### 5.4 Skill (4종)

| 클래스 | 효과 | 특징 |
|--------|------|------|
| NovaSkill | 전방향 폭발 | OverlapSphere, 범위 표시 |
| SpawnSkill | 졸개 소환 | Prefab 인스턴스화 |
| BuffAllySkill | 아군 강화 | Aura 방식, 지속 업데이트 |
| HealAllySkill | 아군 회복 | Aura 방식, 주기적 회복 |

### 5.5 Trigger (3종)

| 클래스 | 조건 | 효과 |
|--------|------|------|
| EnrageTrigger | HP ≤ N% | 공격력/이속 증가 |
| ExplodeOnDeathTrigger | 사망 | 폭발 데미지 |
| PanicBurrowTrigger | HP ≤ N% + 피격 | BurrowPassive 발동 |

---

## 6. 데이터 흐름

### 6.1 ScriptableObject 구조

```
[Category]BehaviorData (SO)
├── _type: [Category]Type (enum)
├── _displayName: string
├── _param1, _param2: float
├── _effectPrefab: GameObject
├── _projectilePrefab: GameObject (Attack만)
└── _stringParam: string (Spawn 등)
```

### 6.2 통합 BugBehaviorData

```csharp
BugBehaviorData (SO)
├── _defaultMovement: MovementBehaviorData
├── _defaultAttack: AttackBehaviorData
├── _conditionalMovements: List<ConditionalMovementData>
├── _conditionalAttacks: List<ConditionalAttackData>
├── _passives: List<PassiveBehaviorData>
├── _skills: List<SkillBehaviorData>
└── _triggers: List<TriggerBehaviorData>
```

### 6.3 초기화 흐름

```
BugController.Start()
├── ApplyBugData() ─────────────────── 스탯 로드 (HP, 공격력, 이속)
├── InitializeBehaviors()
│   ├── BehaviorData 없음? ────────── SetupDefaultBehaviors() (Linear + Melee)
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

## 7. BugController 연동

### 7.1 Update 흐름

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

### 7.2 데미지 처리

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

### 7.3 조건부 행동 전환

```
UpdateConditionalBehaviors()
├── foreach ConditionalMovement:
│   └── if Condition.Evaluate() → currentMovement = behavior
└── foreach ConditionalAttack:
    └── if Condition.Evaluate() → currentAttack = behavior
```

**조건 타입:** `HP<`, `HP>`, `Distance<`, `Distance>`, `AfterAttack`, `HitCount>`, `Time>`, `AllyDead`

---

## 8. 샘플 데이터

### 8.1 BugBehavior 프리셋 (5종)

| SO 이름 | 컨셉 | Movement | Attack |
|---------|------|----------|--------|
| BugBehavior_Beetle | 돌격형 | Linear | Melee |
| BugBehavior_Fly | 부유형 | Hover | Melee |
| BugBehavior_Tank | 방어형 | Linear | Melee |
| BugBehavior_Spitter | 원거리형 | Ranged | Projectile |
| BugBehavior_Bomber | 자폭형 | Linear | Melee + ExplodeOnDeath |

### 8.2 테스트용 조합 (현재 5종)

| SO 이름 | 테스트 목적 |
|---------|------------|
| BugBehavior_Test_Orbit | OrbitMovement 검증 |
| BugBehavior_Test_OrbitPoison | Orbit + PoisonAttack 조합 |
| BugBehavior_Test_Spread | SpreadAttack 검증 |
| BugBehavior_Test_Explode | ExplodeOnDeath 검증 |
| BugBehavior_Test_Regen | RegenPassive 검증 |

---

## 9. 에디터 도구

### 9.1 BugBehaviorSampleCreator

메뉴: `DrillCorp > Bug Behaviors > Create All Samples`

샘플 SO를 자동 생성하는 에디터 도구.

**생성 항목:**
- Movement SO (8종)
- Attack SO (5종)
- Passive SO (6종)
- Skill SO (4종)
- Trigger SO (3종)
- BugBehavior 조합 (5종 + 테스트용)

**주의사항:**
Unity AssetDatabase 타이밍 이슈로 인해 **3단계 패턴** 적용:
1. 빈 에셋 생성
2. SaveAssets + Refresh
3. 다시 로드 후 값 설정

---

## 10. 향후 개선 방향

### 완료됨 (Phase 1-4)

- [x] 조건부 Attack 초기화 완성
- [x] SO 생성 안정화 (3단계 패턴)
- [x] LinearMovement 옵션 추가 (Strafe/Orbit/Retreat)
- [x] HoverMovement Strafe 옵션
- [x] BuffAllySkill, HealAllySkill 구현
- [x] BeamAttack 구현
- [x] Google Sheets 연동 (Phase 4)
  - BugData 시트에 행동 컬럼 추가
  - Import 시 BugBehaviorData SO 자동 생성
  - BugData에 BehaviorData 필드 추가

### 고려 중

- [ ] BehaviorFactory 중앙화
- [ ] BugState enum 추가 (Normal, Burrowed, Stunned 등)
- [ ] 다중 조건 지원 (AND/OR)
- [ ] 이벤트 시스템 확장
- [ ] Phase 5: 기존 BugBase 코드 제거

---

## 11. 참고 문서

- 개발 계획: `docs/BugBehaviorDevelopmentPlan.md`
- 데이터 시트 로그: `docs/DevLog-04-DataSheet.md`
- 데이터 구조: `docs/DATA_STRUCTURE.md`

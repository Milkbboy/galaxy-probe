# Bug 행동 시스템

> 최종 갱신: 2026-04-15 (BugBehaviorSystemAnalysis + BugBehaviorDevelopmentPlan 통합)

## 1. 개요
상속 기반 `BugBase`를 조합 기반 `BugController`로 교체하는 중. 공존 상태로 점진 전환.

**계층 구조**

| 계층 | 역할 | SO |
|------|------|----|
| Wave | 스폰 타이밍/버그 선택 | WaveData |
| Bug | 스탯(HP, 공격, 이속 등) | BugData |
| BugBehavior | 이동/공격/스킬/패시브/트리거 조합 | BugBehaviorData |

![데이터 계층 구조](image/데이터%20계층%20구조.png)

---

## 2. 폴더 구조 (`Assets/_Game/Scripts/Bug/Behaviors/`)

```
IBehavior.cs              # 5개 인터페이스
BehaviorCondition.cs      # 조건 파싱/평가
Data/                     # SO 정의 (Movement/Attack/Skill/Passive/Trigger/BugBehavior)
Movement/ Attack/ Skill/ Passive/ Trigger/   # Base + Factory + 구현체
```
총 **40개 C# 파일**.

---

## 3. 인터페이스

| 인터페이스 | 목적 | 핵심 메서드 |
|---|---|---|
| `IBehavior` | 공통 | `Initialize(BugController)`, `Cleanup()` |
| `IMovementBehavior` | 매 프레임 이동 | `UpdateMovement(Transform)`, `SpeedMultiplier` |
| `IAttackBehavior` | 쿨다운 공격 | `TryAttack(Transform)`, `AttackRange`, `DamageMultiplier` |
| `ISkillBehavior` | 쿨다운 스킬 | `TryUse`, `UpdateCooldown`, `IsReady` |
| `IPassiveBehavior` | 상시 효과 | `UpdatePassive`, `ProcessIncomingDamage`, `ProcessOutgoingDamage` |
| `ITriggerBehavior` | 조건 발동 | `CheckAndTrigger`, `OnDeath`, `HasTriggered` |

---

## 4. Base 클래스 패턴

```csharp
public abstract class [Category]BehaviorBase : I[Category]Behavior
{
    protected BugController _bug;
    public virtual void Initialize(BugController bug) => _bug = bug;
    public virtual void Cleanup() => _bug = null;
    public static [Category]BehaviorBase Create([Type] type, ...) { switch(type){...} }
}
```

- **Movement**: `GetMoveSpeed`, `GetDirectionToTarget`(XZ 정규화), `RotateTowards`(Y축)
- **Attack**: 쿨다운 체크 → `PerformAttack` → `OnAttackPerformed` 이벤트, `DealDamage`가 패시브의 `ProcessOutgoing` 연동
- **Skill**: `_cooldown`, `_currentCooldown`, `IsReady`
- **Trigger**: `_hasTriggered`(1회성), `_triggerOnDeath`
- **Passive**: 기본 구현은 passthrough

---

## 5. 구현체 (18개)

### Movement (8)
| 클래스 | 동작 | 파라미터 |
|---|---|---|
| Linear | 직진 + 옵션(Strafe/Orbit/Retreat) | p1=옵션, p2=값 |
| Hover | XZ 이동 + Y 부유 + Strafe | p1=높이, p2=주기 |
| Burst | 대기 → 돌진 | p1=대기, p2=배율 |
| Ranged | 사거리 유지 + 좌우 | p1=거리, p2=횡이동 |
| Retreat | 후퇴 | p1=지속, p2=속도배율 |
| SlowStart | 점진 가속 | p1=시작비율, p2=도달시간 |
| Orbit | 타겟 공전 | p1=반경, p2=각속도 |
| Teleport | 순간이동 | p1=쿨다운, p2=거리 |

### Attack (5 + 투사체)
| Melee / Projectile(p1=속도) / Cleave(p1=각도) / Spread(p1=발수, p2=각도) / Beam(p1=지속, p2=틱) |

### Passive (6)
| Armor / Dodge / Shield(흡수+재생) / Regen / PoisonAttack(ProcessOutgoing) / Burrow(무적+투명, 상태머신) |

### Skill (4)
| Nova(OverlapSphere 폭발) / Spawn(졸개 소환) / BuffAlly(Aura ATK/SPD) / HealAlly(Aura 주기 회복) |

### Trigger (3)
| Enrage(HP≤N%, 공격/이속↑) / ExplodeOnDeath / PanicBurrow(HP≤N% + 피격, 쿨다운 반복) |

**제외 결정**: HomingAttack / SlowSkill / StunSkill (머신 고정이라 무의미), Lifesteal/Reflect/Fast/Transform/SplitOnDeath/Revive (현재 불필요)

---

## 6. 데이터 흐름

### SO 구조
```
[Category]BehaviorData
├ _type, _displayName
├ _param1, _param2
├ _effectPrefab (+ _effectPrefab2 for Burrow)
├ _projectilePrefab (Attack)
└ _stringParam (Spawn 등)

BugBehaviorData
├ _defaultMovement / _defaultAttack
├ _conditionalMovements[] / _conditionalAttacks[]
└ _passives[] / _skills[] / _triggers[]
```

### 초기화
```
BugController.Start()
├ ApplyBugData()                       # 스탯 로드
├ InitializeBehaviors()
│  ├ SO 없음 → SetupDefaultBehaviors() (Linear+Melee)
│  └ SO 있음 → Movement/Attack/Passives/Skills/Triggers 순차 Create+Initialize
├ FindTarget()
└ CreateHpBar()
```

### Update
```
AliveTime++ / JustAttacked--
→ Passive.UpdatePassive 전체 순회
→ Burrow 상태면 skip, 아니면:
   - 조건부 전환 체크
   - Movement.UpdateMovement
   - 범위 체크 → Attack.TryAttack
   - Skill.UpdateCooldown → TryUse
→ Trigger.CheckAndTrigger
```

### 데미지
```
TakeDamage → 무적/사망 체크 → HitCount++
→ foreach Passive: damage = ProcessIncomingDamage(damage)
→ HP -= damage → UI/VFX → HP≤0이면 Die()
```

### 조건부 전환
조건 타입: `HP<`, `HP>`, `Distance<`, `Distance>`, `AfterAttack`, `HitCount>`, `Time>`, `AllyDead`

---

## 7. 샘플 데이터

**프리셋 5종**: Beetle(돌격), Fly(부유), Tank(방어), Spitter(원거리), Bomber(자폭)
**테스트 5종**: Test_Orbit, Test_OrbitPoison, Test_Spread, Test_Explode, Test_Regen

---

## 8. 에디터 도구
`DrillCorp > Bug Behaviors > Create All Samples` — 모든 샘플 SO 자동 생성.
Unity AssetDatabase 타이밍 이슈로 **3단계 패턴**: 빈 에셋 → SaveAssets+Refresh → 재로드 후 값 설정.

---

## 9. Phase 진행 이력

| Phase | 목표 | 상태 |
|---|---|---|
| 0 | 기반 (인터페이스/SO/조건/BugController) | ✅ |
| 1 | 기본 행동 (Linear/Hover/Burst, Melee/Projectile, Armor/Dodge) | ✅ |
| 2 | 확장 (Retreat/SlowStart/Orbit, Cleave/Spread, Nova/Spawn, Shield/Regen/Poison, Enrage/ExplodeOnDeath) | ✅ |
| 3 | 고급 (Teleport, Beam, BuffAlly/HealAlly, Burrow+PanicBurrow) | ✅ |
| 4 | Google Sheets 연동 (BugData 시트에 행동 컬럼 추가, Import 시 SO 자동 생성) | ✅ 2026-04-03 |
| 5 | BugBase 계열 완전 제거 | ⬜ |

**Phase 4 Import 규칙 요약**
- `MovementType: Hover`, `MovementParam1: 0.5` → `HoverMovement` SO
- `Passives: Armor:5, Dodge:20` / `Triggers: ExplodeOnDeath:10:2`
- 출력: `Assets/_Game/Data/BugBehaviors/Imported/` (`BugBehavior_*`, `Movement_*`, `Attack_*`, ...)
- 시트 문법 상세는 `GoogleSheetsGuide.md` 참조

**Phase 5 체크리스트**
- [ ] 모든 Bug 프리펩을 BugController로 교체
- [ ] BugSpawner의 BugBase 경로 제거
- [ ] `BugBase.cs`, `BeetleBug.cs`, `FlyBug.cs`, `CentipedeBug.cs` 삭제

---

## 10. 향후 개선 고려
- BehaviorFactory 중앙화
- BugState enum (Normal/Burrowed/Stunned)
- 다중 조건 지원 (AND/OR)
- 이벤트 시스템 확장

---

## 참고
- 기획자 가이드: `BugBehaviorPatterns.md`
- 시트 문법: `GoogleSheetsGuide.md`
- 데이터 구조: `DataStructure.md`
- 변경 이력: `CHANGELOG.md`

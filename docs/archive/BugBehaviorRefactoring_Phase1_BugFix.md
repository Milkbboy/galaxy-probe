# Phase 1: 버그 수정 계획

## 개요

BugController의 초기화 코드에서 누락된 부분을 수정합니다.

---

## 버그 1: 조건부 Attack 초기화 누락

### 현상
`InitializeFromScriptableObjects()`에서 ConditionalMovements는 처리하지만, **ConditionalAttacks는 처리하지 않음**.

### 위치
`BugController.cs` 357줄 (`_currentAttack = _defaultAttack;` 이후)

### 현재 코드
```csharp
// Attack
if (_behaviorData.DefaultAttack != null)
{
    var atkData = _behaviorData.DefaultAttack;
    _defaultAttack = AttackBehaviorBase.Create(...);
    _defaultAttack?.Initialize(this);
}
else
{
    _defaultAttack = new MeleeAttack();
    _defaultAttack.Initialize(this);
}
_currentAttack = _defaultAttack;

// ← 여기에 Conditional Attacks 처리 없음!

// Passives
for (int i = 0; i < _behaviorData.Passives.Count; i++)
```

### 수정 내용
ConditionalMovements와 동일한 패턴으로 ConditionalAttacks 초기화 추가:

```csharp
_currentAttack = _defaultAttack;

// Conditional Attacks 추가
foreach (var condAtk in _behaviorData.ConditionalAttacks)
{
    if (condAtk.Behavior == null) continue;

    var behavior = AttackBehaviorBase.Create(
        condAtk.Behavior.Type,
        condAtk.Behavior.Param1,
        condAtk.Behavior.Param2,
        condAtk.Behavior.ProjectilePrefab,
        condAtk.Behavior.HitVfxPrefab
    );
    behavior?.Initialize(this);

    _conditionalAttacks.Add(new ConditionalBehavior<IAttackBehavior>
    {
        Condition = BehaviorCondition.Parse(condAtk.Condition),
        Behavior = behavior
    });
}

// Passives
```

### 영향
- BugBehaviorData에 설정된 조건부 공격이 실제로 동작하게 됨
- 예: HP < 30%일 때 Melee → Cleave 전환 가능

---

## 버그 2: 런타임 Skills/Triggers 초기화 누락

### 현상
`InitializeFromRuntimeData()`에서 Skills와 Triggers 초기화가 TODO로 남아있음.

### 위치
`BugController.cs` 489줄

### 현재 코드
```csharp
// Passives
foreach (var passiveData in data.Passives)
{
    var passive = PassiveBehaviorBase.Create(passiveData.Type, passiveData.Param1, passiveData.Param2);
    if (passive != null)
    {
        passive.Initialize(this);
        _passives.Add(passive);
    }
}

// TODO: Skills, Triggers 초기화 (Phase 2)
```

### 수정 내용
Skills와 Triggers 초기화 코드 추가:

```csharp
// Passives
foreach (var passiveData in data.Passives)
{
    var passive = PassiveBehaviorBase.Create(passiveData.Type, passiveData.Param1, passiveData.Param2);
    if (passive != null)
    {
        passive.Initialize(this);
        _passives.Add(passive);
    }
}

// Skills
foreach (var skillData in data.Skills)
{
    var skill = SkillBehaviorBase.Create(
        skillData.Type,
        skillData.Cooldown,
        skillData.Param1,
        skillData.Param2,
        null,  // spawnPrefab (런타임에서는 미지원)
        null   // effectPrefab (런타임에서는 미지원)
    );
    if (skill != null)
    {
        skill.Initialize(this);
        _skills.Add(skill);
    }
}

// Triggers
foreach (var triggerData in data.Triggers)
{
    var trigger = TriggerBehaviorBase.Create(
        triggerData.Type,
        triggerData.Param1,
        triggerData.Param2,
        triggerData.Param3,
        null  // effectPrefab (런타임에서는 미지원)
    );
    if (trigger != null)
    {
        trigger.Initialize(this);
        _triggers.Add(trigger);
    }
}
```

### 영향
- Google Sheets에서 Import된 데이터로 Skills/Triggers 사용 가능
- 단, Prefab 참조는 런타임 데이터에서 지원 불가 (null 전달)

---

## 작업 순서

### Step 1: 조건부 Attack 초기화 추가
1. `BugController.cs` 열기
2. `_currentAttack = _defaultAttack;` 줄 찾기 (357줄 부근)
3. 그 아래에 ConditionalAttacks 초기화 코드 추가
4. ConditionalMovements 코드와 동일한 패턴 사용

### Step 2: 런타임 Skills 초기화 추가
1. `// TODO: Skills, Triggers 초기화` 주석 찾기 (489줄 부근)
2. TODO 주석 삭제
3. Skills 초기화 코드 추가

### Step 3: 런타임 Triggers 초기화 추가
1. Skills 초기화 코드 아래에 Triggers 초기화 코드 추가

### Step 4: 테스트
1. BugBehaviorData에 ConditionalAttack 추가하여 테스트
2. 조건 충족 시 공격 패턴 전환 확인

---

## 체크리스트

- [x] 조건부 Attack 초기화 코드 추가
- ~~[ ] 런타임 Skills 초기화 코드 추가~~ (불필요 - SO 방식 사용)
- ~~[ ] 런타임 Triggers 초기화 코드 추가~~ (불필요 - SO 방식 사용)
- ~~[ ] TODO 주석 제거~~ (불필요 - SO 방식 사용)
- [ ] 컴파일 확인
- [ ] 테스트 (선택)
- [ ] 커밋

## 비고

Phase 4 Google Sheets Import는 **ScriptableObject 생성 방식**으로 진행 예정.
`InitializeFromRuntimeData()` 관련 코드는 사용하지 않으므로 Bug 2 수정 불필요.

# Bug Behavior Import 가이드

Google Sheets에서 BugData를 Import할 때 Behavior SO가 자동 생성되는 과정을 설명합니다.

---

## 개요

![Import 흐름도](image/Import%20흐름도.png)

---

## Import 흐름

### 1단계: 시트 데이터 읽기

```
BugData 시트 1행 예시:
| BugId | BugName | MaxHealth | ... | MovementType | AttackType | Passives | Triggers |
|-------|---------|-----------|-----|--------------|------------|----------|----------|
| 3     | Tank    | 40        | ... | Linear       | Melee      | Armor:5  |          |
```

### 2단계: BugData SO 생성

```
Assets/_Game/Data/Bugs/Bug_Tank.asset
├── _bugId: 3
├── _bugName: "Tank"
├── _maxHealth: 40
├── ...
└── _behaviorData: → BugBehavior_Tank.asset (참조)
```

### 3단계: Movement SO 확인/생성

```
MovementType = "Linear"
MovementParam1 = 0
MovementParam2 = 0

1. 기존 캐시 확인: Assets/_Game/Data/BugBehaviors/Movement/Movement_Linear.asset
   → 있으면 재사용

2. 없으면 새로 생성: Assets/_Game/Data/BugBehaviors/Imported/Movement_Linear_0_0.asset
   ├── _type: MovementType.Linear
   ├── _displayName: "Linear (Imported)"
   ├── _param1: 0
   └── _param2: 0
```

### 4단계: Attack SO 확인/생성

```
AttackType = "Melee"
AttackRange = 1.5
AttackParam1 = 0

1. 기존 캐시 확인: Assets/_Game/Data/BugBehaviors/Attack/Attack_Melee.asset
   → 있으면 재사용

2. 없으면 새로 생성: Assets/_Game/Data/BugBehaviors/Imported/Attack_Melee_1.5.asset
```

### 5단계: Passive SO 파싱/생성

```
Passives = "Armor:5"

파싱 결과:
├── Type: PassiveType.Armor
├── Param1: 5
└── Param2: 0

1. 기존 캐시 확인: Assets/_Game/Data/BugBehaviors/Passive/Passive_Armor.asset
   → 있으면 재사용

2. 없으면 새로 생성: Assets/_Game/Data/BugBehaviors/Imported/Passive_Armor_5_0.asset
```

### 6단계: BugBehaviorData SO 생성

```
Assets/_Game/Data/BugBehaviors/Imported/BugBehavior_Tank.asset
├── _defaultMovement: → Movement_Linear.asset
├── _defaultAttack: → Attack_Melee.asset
├── _passives: [Passive_Armor.asset]
├── _skills: []
└── _triggers: []
```

### 7단계: BugData에 연결

```
Bug_Tank.asset
└── _behaviorData: → BugBehavior_Tank.asset
```

---

## 생성되는 파일 구조

```
Assets/_Game/Data/
├── Bugs/
│   ├── Bug_Beetle.asset      ← BugData (스탯 + BehaviorData 참조)
│   ├── Bug_Fly.asset
│   ├── Bug_Tank.asset
│   └── ...
│
└── BugBehaviors/
    ├── Movement/             ← 수동 생성된 SO (재사용)
    │   ├── Movement_Linear.asset
    │   ├── Movement_Hover.asset
    │   └── ...
    ├── Attack/
    │   ├── Attack_Melee.asset
    │   ├── Attack_Projectile.asset
    │   └── ...
    ├── Passive/
    │   └── ...
    ├── Skill/
    │   └── ...
    ├── Trigger/
    │   └── ...
    │
    └── Imported/             ← Import 시 자동 생성
        ├── BugBehavior_Beetle.asset
        ├── BugBehavior_Tank.asset
        ├── Movement_Orbit_3_90.asset    ← 기존에 없던 조합
        ├── Passive_Armor_5_0.asset
        └── ...
```

---

## SO 재사용 규칙

### 기존 SO 재사용 (캐시)

| 폴더 | 검색 기준 | 예시 |
|------|-----------|------|
| Movement/ | 타입명 | `Movement_Linear` → Linear 타입 |
| Attack/ | 타입명 | `Attack_Melee` → Melee 타입 |
| Passive/ | 타입명 | `Passive_Armor` → Armor 타입 |
| Skill/ | 타입명 | `Skill_Nova` → Nova 타입 |
| Trigger/ | 타입명 | `Trigger_Enrage` → Enrage 타입 |

### 새 SO 생성 조건

기존 캐시에 없으면 `Imported/` 폴더에 새로 생성:

```
파일명 규칙:
- Movement: Movement_[Type]_[Param1]_[Param2].asset
- Attack: Attack_[Type]_[Range].asset
- Passive: Passive_[Type]_[Param1]_[Param2].asset
- Skill: Skill_[Type]_[Cooldown].asset
- Trigger: Trigger_[Type]_[Param1]_[Param2].asset
```

---

## 문자열 파싱 규칙

### Passives

```
형식: Type:Param1:Param2
복수: Type:P1:P2, Type:P1:P2

예시:
- "Armor:5"           → ArmorPassive(param1=5)
- "Shield:20:2"       → ShieldPassive(param1=20, param2=2)
- "Armor:5, Dodge:30" → ArmorPassive(5) + DodgePassive(30)
```

| 타입 | Param1 | Param2 | 예시 |
|------|--------|--------|------|
| Armor | 감소량 | - | `Armor:5` |
| Dodge | 회피% | - | `Dodge:30` |
| Shield | 최대량 | 재생속도 | `Shield:20:2` |
| Regen | 초당회복 | - | `Regen:2` |
| PoisonAttack | 지속시간 | 틱데미지 | `PoisonAttack:3:5` |

### Skills

```
형식: Type:Cooldown:Param1:Param2
특수: Spawn:Cooldown:BugName:Count

예시:
- "Nova:5:10:3"        → NovaSkill(cd=5, damage=10, range=3)
- "HealAlly:6:10:4"    → HealAllySkill(cd=6, heal=10, range=4)
- "Spawn:8:Beetle:2"   → SpawnSkill(cd=8, bug="Beetle", count=2)
```

| 타입 | Cooldown | Param1 | Param2 | 예시 |
|------|----------|--------|--------|------|
| Nova | 쿨다운 | 데미지 | 범위 | `Nova:5:10:3` |
| BuffAlly | 쿨다운 | 버프량% | 범위 | `BuffAlly:10:50:4` |
| HealAlly | 쿨다운 | 회복량 | 범위 | `HealAlly:6:10:4` |
| Spawn | 쿨다운 | BugName | 수량 | `Spawn:8:Beetle:2` |

### Triggers

```
형식: Type:Param1:Param2
특수: SplitOnDeath:BugName:Count

예시:
- "Enrage:30:50"           → EnrageTrigger(hp%=30, buff%=50)
- "ExplodeOnDeath:10:2"    → ExplodeOnDeathTrigger(damage=10, radius=2)
- "SplitOnDeath:Mini:3"    → SplitOnDeathTrigger(bug="Mini", count=3)
```

| 타입 | Param1 | Param2 | 예시 |
|------|--------|--------|------|
| Enrage | HP% | 버프% | `Enrage:30:50` |
| ExplodeOnDeath | 데미지 | 반경 | `ExplodeOnDeath:10:2` |
| SplitOnDeath | BugName | 수량 | `SplitOnDeath:Mini:3` |
| PanicBurrow | HP% | 쿨다운 | `PanicBurrow:50:5` |

---

## 예시: Elite 벌레 Import

### 시트 데이터

```
| BugId | BugName | MaxHealth | MovementType | AttackType | AttackParam1 | Passives | Triggers |
|-------|---------|-----------|--------------|------------|--------------|----------|----------|
| 6     | Elite   | 50        | Linear       | Cleave     | 90           | Shield:20:2 | Enrage:30:50 |
```

### 생성되는 SO

```
1. Bug_Elite.asset (BugData)
   └── _behaviorData: → BugBehavior_Elite.asset

2. BugBehavior_Elite.asset (BugBehaviorData)
   ├── _defaultMovement: → Movement_Linear.asset (기존 재사용)
   ├── _defaultAttack: → Attack_Cleave_2.asset (새로 생성 또는 기존)
   ├── _passives: [Passive_Shield_20_2.asset]
   ├── _skills: []
   └── _triggers: [Trigger_Enrage_30_50.asset]

3. Passive_Shield_20_2.asset (없으면 새로 생성)
   ├── _type: PassiveType.Shield
   ├── _param1: 20
   └── _param2: 2

4. Trigger_Enrage_30_50.asset (없으면 새로 생성)
   ├── _type: TriggerType.Enrage
   ├── _param1: 30
   └── _param2: 50
```

---

## 주의사항

### 1. 기존 SO 우선 사용

`Movement/`, `Attack/` 등 폴더에 이미 있는 SO가 있으면 **타입명만 일치해도 재사용**됩니다.
파라미터가 다르더라도 기존 SO를 사용하므로, 정밀한 제어가 필요하면 수동으로 SO를 생성하세요.

### 2. Imported 폴더 정리

반복 Import 시 `Imported/` 폴더에 SO가 계속 쌓일 수 있습니다.
테스트 후에는 불필요한 SO를 정리하세요.

### 3. 빈 값 처리

행동 컬럼이 모두 비어있으면 BugBehaviorData가 생성되지 않습니다.
MovementType 또는 AttackType 중 하나라도 값이 있어야 생성됩니다.

---

## 관련 코드

- `GoogleSheetsImporter.cs` - Import 로직
- `BugData.cs` - BehaviorData 필드
- `BugBehaviorData.cs` - 행동 조합 SO
- `MovementBehaviorData.cs` 등 - 개별 행동 SO

---

*최종 갱신: 2026-04-06*

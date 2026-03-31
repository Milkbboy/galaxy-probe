# Bug 행동 시스템 개발 계획

## 개요

기존 상속 기반 BugBase 시스템을 컴포넌트 조합 기반 BugController 시스템으로 교체합니다.
점진적으로 교체하여 개발 중에도 게임이 동작하도록 합니다.

---

## Phase 구조

| Phase | 목표 | 상태 |
|-------|------|------|
| Phase 0 | 기반 구조 | ✅ 완료 |
| Phase 1 | 기본 행동 | ✅ 완료 |
| Phase 2 | 확장 행동 | ⬜ 미진행 |
| Phase 3 | 고급 행동 | ⬜ 미진행 |
| Phase 4 | Google Sheets 연동 | ⬜ 미진행 |
| Phase 5 | 기존 코드 제거 | ⬜ 미진행 |

---

## Phase 0: 기반 구조 ✅

### 목표
- 행동 시스템의 뼈대 구축
- 기존 BugBase와 공존 가능한 구조

### 완료 항목
- [x] 인터페이스 정의
  - `IMovementBehavior` - 이동
  - `IAttackBehavior` - 기본 공격
  - `ISkillBehavior` - 스킬 (쿨다운 기반)
  - `IPassiveBehavior` - 패시브 (상시 적용)
  - `ITriggerBehavior` - 트리거 (조건 발동)

- [x] ScriptableObject 데이터 클래스
  - `MovementBehaviorData`
  - `AttackBehaviorData`
  - `SkillBehaviorData`
  - `PassiveBehaviorData`
  - `TriggerBehaviorData`
  - `BugBehaviorData` (5가지 조합)

- [x] 조건 시스템
  - `BehaviorCondition` - 조건 파싱 및 평가
  - HP, Distance, AfterAttack, HitCount, Time 등

- [x] BugController
  - 새로운 행동 조합 기반 컨트롤러
  - 기존 BugBase와 별도로 동작

### 생성된 파일
```
Assets/_Game/Scripts/Bug/
├── BugController.cs
└── Behaviors/
    ├── IBehavior.cs
    ├── BehaviorCondition.cs
    └── Data/
        ├── MovementBehaviorData.cs
        ├── AttackBehaviorData.cs
        ├── SkillBehaviorData.cs
        ├── PassiveBehaviorData.cs
        ├── TriggerBehaviorData.cs
        └── BugBehaviorData.cs
```

---

## Phase 1: 기본 행동 ✅

### 목표
- 가장 기본적인 벌레 구현 가능 (Beetle, Fly 수준)

### 완료 항목

#### Movement
- [x] `LinearMovement` - 직선 이동
- [x] `HoverMovement` - 공중 부유 (위아래 떠다님)
- [x] `BurstMovement` - 돌진 (멈췄다 빠르게)

#### BasicAttack
- [x] `MeleeAttack` - 근접 공격
- [x] `ProjectileAttack` - 원거리 투사체
- [x] `BugProjectile` - 투사체 컴포넌트

#### Passives
- [x] `ArmorPassive` - 데미지 감소
- [x] `DodgePassive` - 확률 회피

### 생성된 파일
```
Assets/_Game/Scripts/Bug/Behaviors/
├── Movement/
│   ├── MovementBehaviorBase.cs
│   ├── LinearMovement.cs
│   ├── HoverMovement.cs
│   └── BurstMovement.cs
├── Attack/
│   ├── AttackBehaviorBase.cs
│   ├── MeleeAttack.cs
│   ├── ProjectileAttack.cs
│   └── BugProjectile.cs
└── Passive/
    ├── PassiveBehaviorBase.cs
    ├── ArmorPassive.cs
    └── DodgePassive.cs
```

---

## Phase 2: 확장 행동 ⬜

### 목표
- 다양한 벌레 타입 구현 가능 (Spitter, Bomber, Tank, Hive)

### 구현 항목

#### Movement
- [ ] `RetreatMovement` - 공격 후 후퇴
- [ ] `SlowStartMovement` - 점점 가속
- [ ] `OrbitMovement` - 타겟 주위 선회

#### BasicAttack
- [ ] `CleaveAttack` - 부채꼴 범위 공격
- [ ] `SpreadAttack` - 다발 발사

#### Skills
- [ ] `SkillBehaviorBase` - 스킬 기본 클래스
- [ ] `SpawnSkill` - 졸개 소환
- [ ] `NovaSkill` - 전방향 폭발

#### Passives
- [ ] `ShieldPassive` - 데미지 흡수 (재생)
- [ ] `RegenPassive` - 체력 재생
- [ ] `PoisonAttackPassive` - 독 공격

#### Triggers
- [ ] `TriggerBehaviorBase` - 트리거 기본 클래스
- [ ] `EnrageTrigger` - HP 낮으면 공격력 증가
- [ ] `ExplodeOnDeathTrigger` - 사망 시 폭발

### 예상 파일
```
Assets/_Game/Scripts/Bug/Behaviors/
├── Movement/
│   ├── RetreatMovement.cs
│   ├── SlowStartMovement.cs
│   └── OrbitMovement.cs
├── Attack/
│   ├── CleaveAttack.cs
│   └── SpreadAttack.cs
├── Skill/
│   ├── SkillBehaviorBase.cs
│   ├── SpawnSkill.cs
│   └── NovaSkill.cs
├── Passive/
│   ├── ShieldPassive.cs
│   ├── RegenPassive.cs
│   └── PoisonAttackPassive.cs
└── Trigger/
    ├── TriggerBehaviorBase.cs
    ├── EnrageTrigger.cs
    └── ExplodeOnDeathTrigger.cs
```

---

## Phase 3: 고급 행동 ⬜

### 목표
- 보스급 벌레 구현 가능 (Queen)

### 구현 항목

#### Movement
- [ ] `TeleportMovement` - 순간이동
- [ ] `BurrowMovement` - 땅속 이동 (무적)
- [ ] `DiveMovement` - 급강하

#### BasicAttack
- [ ] `HomingAttack` - 유도 투사체
- [ ] `BeamAttack` - 지속 레이저

#### Skills
- [ ] `BuffAllySkill` - 아군 강화
- [ ] `HealAllySkill` - 아군 회복
- [ ] `SlowSkill` - 감속
- [ ] `StunSkill` - 기절

#### Passives
- [ ] `LifestealPassive` - 흡혈
- [ ] `ReflectPassive` - 반사
- [ ] `FastPassive` - 이속 증가

#### Triggers
- [ ] `TransformTrigger` - 2페이즈 변신
- [ ] `SplitOnDeathTrigger` - 사망 시 분열
- [ ] `ReviveTrigger` - 부활

---

## Phase 4: Google Sheets 연동 ⬜

### 목표
- 기획자가 시트에서 벌레 행동 조합 가능

### 구현 항목
- [ ] `GoogleSheetsImporter` 확장
  - BugBehaviors 시트 파싱
  - ConditionalBehaviors 시트 파싱
  - 문자열 → RuntimeBehaviorSet 변환

- [ ] Import 시 자동 조합
  - `Movement: Hover:0.5:2` → HoverMovement(0.5, 2)
  - `Passives: Armor:10, Dodge:20` → ArmorPassive(10), DodgePassive(20)

### 시트 구조
```
BugBehaviors 시트:
| BugId | Movement | BasicAttack | Skills | Passives | Triggers |

ConditionalBehaviors 시트:
| BugId | Category | Default | Condition | SwitchTo |
```

---

## Phase 5: 기존 코드 제거 ⬜

### 목표
- BugBase 시스템 완전 제거
- 모든 벌레가 BugController 사용

### 구현 항목
- [ ] 모든 Bug 프리펩을 BugController로 교체
- [ ] BugSpawner에서 BugBase 코드 제거
- [ ] BugBase.cs, BeetleBug.cs, FlyBug.cs, CentipedeBug.cs 삭제

### 주의사항
- Phase 2~4 완료 후 진행
- 모든 기존 벌레 동작 확인 후 제거

---

## 현재 상태

### 완료
- Phase 0: 기반 구조 ✅
- Phase 1: 기본 행동 ✅

### 다음 작업
1. ~~BugSpawner가 BugController 지원하도록 수정~~ ✅
2. 테스트용 프리펩 생성 (Unity Editor에서 BugController 컴포넌트 추가)
3. 인게임 테스트

### 공존 상태
```
BugBase (기존)     ← 기존 프리펩들 사용 중
BugController (신규) ← 새 프리펩에서 사용 가능
```

---

## 변경 이력

| 날짜 | 내용 |
|------|------|
| 2024-XX-XX | Phase 0~1 완료, 문서 작성 |

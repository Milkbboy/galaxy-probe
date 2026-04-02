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
| Phase 2 | 확장 행동 | ✅ 완료 |
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

## Phase 2: 확장 행동 ✅

### 목표
- 다양한 벌레 타입 구현 가능 (Spitter, Bomber, Tank, Hive)

### 완료 항목

#### Movement
- [x] `RetreatMovement` - 공격 후 후퇴
- [x] `SlowStartMovement` - 점점 가속
- [x] `OrbitMovement` - 타겟 주위 선회

#### BasicAttack
- [x] `CleaveAttack` - 부채꼴 범위 공격 (LineRenderer 범위 표시)
- [x] `SpreadAttack` - 다발 발사

#### Skills
- [x] `SkillBehaviorBase` - 스킬 기본 클래스
- [x] `SpawnSkill` - 졸개 소환 (측면/후방 배치)
- [x] `NovaSkill` - 전방향 폭발 (Mesh 범위 표시)

#### Passives
- [x] `ShieldPassive` - 데미지 흡수 (재생)
- [x] `RegenPassive` - 체력 재생
- [x] `PoisonAttackPassive` - 독 공격 (PoisonEffect 컴포넌트)

#### Triggers
- [x] `TriggerBehaviorBase` - 트리거 기본 클래스
- [x] `EnrageTrigger` - HP 낮으면 공격력/이속 증가
- [x] `ExplodeOnDeathTrigger` - 사망 시 폭발 (SimpleVFX.PlayExplosion)

### 추가 기능
- [x] 패시브의 `ProcessOutgoingDamage` 연결 (독 효과 등)
- [x] `SkillBehaviorData`에 `spawnPrefab` 필드 추가
- [x] `effectPrefab` 활용 연결
- [x] BugController에서 Skills/Triggers 초기화 코드 완성
- [x] `SimpleVFX.PlayExplosion` 추가

### 생성된 파일
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

## Phase 3: 고급 행동 🔄

### 목표
- 보스급 벌레 구현 가능 (Queen)

### 구현 항목

#### Movement
- [x] `TeleportMovement` - 순간이동 (쿨다운 기반, 느린 이동 + 순간이동)
- [ ] `DiveMovement` - 급강하

#### Passives (추가)
- [x] `BurrowPassive` - 땅속 숨기 (무적 + 투명화)
  - 원래 Movement로 기획했으나 Passive + Trigger 조합으로 변경
  - param1 = 숨어있는 시간 (초)
  - param2 = 애니메이션 시간 (초)
  - effectPrefab = 숨을 때 VFX
  - effectPrefab2 = 나올 때 VFX

#### Triggers (추가)
- [x] `PanicBurrowTrigger` - HP 낮을 때 피격 시 Burrow 발동
  - param1 = HP 임계값 % (기본 50%)
  - param2 = 쿨다운 (기본 5초)
  - 반복 발동 가능 (쿨다운 후 재발동)

#### BasicAttack
- ~~[ ] `HomingAttack` - 유도 투사체~~ (제외: 머신이 고정이라 유도 의미 없음)
- [x] `BeamAttack` - 지속 레이저
  - param1 = 지속시간 (초)
  - param2 = 틱간격 (초)
  - projectilePrefab = 빔 VFX (없으면 붉은색 LineRenderer 폴백)

#### Skills
- [x] `BuffAllySkill` - 아군 강화 (Aura 방식, 범위 내 버프)
  - param1 = 범위, param2 = 공격력 배율, cooldown = 이속 배율
  - Physics.OverlapSphereNonAlloc으로 성능 최적화
  - 황금색 Cylinder 범위 표시
  - 버프 받는 버그에 텍스트 표시 (ATK/SPD 배율)
- [x] `HealAllySkill` - 아군 회복 (Aura 방식, 주기적 회복)
  - param1 = 범위, param2 = 회복량, cooldown = 회복 주기
  - 녹색 Cylinder 범위 표시
- ~~[ ] `SlowSkill` - 감속~~ (제외: 머신이 고정이라 감속 대상 없음)
- ~~[ ] `StunSkill` - 기절~~ (제외: 머신이 고정이라 기절 효과 무의미)

#### Passives (미구현)
- [ ] `LifestealPassive` - 흡혈
- [ ] `ReflectPassive` - 반사
- [ ] `FastPassive` - 이속 증가

#### Triggers (미구현)
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
- Phase 2: 확장 행동 ✅

### 다음 작업
1. Phase 3: 고급 행동 (보스급 벌레)
   - TeleportMovement, BurrowMovement
   - HomingAttack, BeamAttack
   - BuffAllySkill, SlowSkill
   - TransformTrigger, SplitOnDeathTrigger

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
| 2024-XX-XX | Phase 2 완료: 확장 행동 (Movement 3종, Attack 2종, Skill 2종, Passive 3종, Trigger 2종) |
| 2024-XX-XX | Phase 3 진행: TeleportMovement 완료 |
| 2024-XX-XX | Burrow 시스템 설계 변경: Movement → Passive + Trigger 조합으로 변경. BurrowPassive, PanicBurrowTrigger 완료 |
| 2024-XX-XX | BeamAttack 구현 완료. HomingAttack 제외 결정 (머신 고정이라 유도 의미 없음) |
| 2024-XX-XX | BuffAllySkill, HealAllySkill 완료 (Aura 방식). SlowSkill, StunSkill 제외 결정 (머신 고정이라 디버프 대상 없음) |

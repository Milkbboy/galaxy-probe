# 게임 데이터 구조 (Data Structure)

기획자가 Google Sheets에서 설정 가능한 게임 데이터 정의서입니다.

> 최종 갱신: 2026-04-20

---

## 데이터 계층 구조

![데이터 계층 구조](image/데이터%20계층%20구조.png)

| 계층 | ScriptableObject | 역할 |
|------|------------------|------|
| **Wave** | WaveData | 하나의 게임 세션 정의 |
| **Bug 스탯** | BugData | 체력, 공격력, 이속 + 엘리트 플래그 |
| **Bug 행동** | BugBehaviorData | 어떻게 움직이고 공격할지 |
| **Machine** | MachineData | 플레이어 머신 스탯 + BaseMiningTarget |
| **Character** (v2) | CharacterData | 3 캐릭터, DefaultMachine + 어빌리티 3종 묶음 |
| **Upgrade** | UpgradeData | 굴착기 6종 영구 강화 (이중 재화 비용) |
| **WeaponUpgrade** (v2) | WeaponUpgradeData | 무기별 강화 항목 (Damage/Range/Cooldown/Ammo/Reload/Radius/Slow) |
| **Ability** (v2) | AbilityData | 캐릭터 어빌리티 9종 (런타임 실행기 미구현) |

---

## 파일 구조

```
Assets/_Game/Data/
├── Bugs/                           # BugData (스탯)
│   ├── Bug_Beetle.asset
│   ├── Bug_Fly.asset
│   └── Bug_Test_*.asset           # 테스트용
│
├── BugBehaviors/                   # 행동 데이터
│   ├── BugBehavior_Beetle.asset   # 행동 조합 (Movement + Attack + ...)
│   ├── BugBehavior_Fly.asset
│   ├── Movement/                  # 이동 방식
│   │   ├── Movement_Linear.asset
│   │   ├── Movement_Hover.asset
│   │   └── Movement_Orbit.asset
│   ├── Attack/                    # 공격 방식
│   │   ├── Attack_Melee.asset
│   │   ├── Attack_Projectile.asset
│   │   └── Attack_Cleave.asset
│   ├── Passive/                   # 패시브 능력
│   │   ├── Passive_Armor.asset
│   │   └── Passive_Shield.asset
│   ├── Skill/                     # 스킬
│   │   ├── Skill_Nova.asset
│   │   └── Skill_BuffAlly.asset
│   └── Trigger/                   # 조건부 발동
│       ├── Trigger_Enrage.asset
│       └── Trigger_ExplodeOnDeath.asset
│
├── Waves/
│   ├── Wave_01.asset
│   └── Wave_02.asset
│
├── Machines/
│   └── Machine_Default.asset
│
└── Upgrades/
    └── Upgrade_*.asset
```

---

## 1. BugData (벌레 스탯)

벌레의 **기본 능력치**를 정의합니다. 행동 방식은 BugBehaviorData에서 별도 설정.

### 필드 정의

| 컬럼 | 타입 | 기본값 | 설명 | 기획 가이드 |
|------|------|--------|------|-------------|
| **BugId** | int | - | 고유 식별자 | 1, 2, 3... 순차적으로 부여 |
| **BugName** | string | - | 벌레 이름 | "딱정벌레", "파리" 등 |
| **MaxHealth** | float | 10 | 최대 체력 | 일반 5~20, 보스 100+ |
| **MoveSpeed** | float | 2 | 이동 속도 | 1=느림, 2=보통, 4=빠름 |
| **AttackDamage** | float | 5 | 1회 공격 데미지 | 머신 MaxHealth(100) 기준 |
| **AttackCooldown** | float | 1 | 공격 간격 (초) | DPS = Damage / Cooldown |
| **Scale** | float | 1 | 크기 배율 | 0.5=절반, 2=2배 |
| **CurrencyReward** | int | 1 | 처치 보상 | 강한 적=높은 보상 |
| **BehaviorData** | SO 참조 | - | 행동 데이터 | BugBehaviorData 연결 |
| **IsElite** (v2) | bool | false | 엘리트 플래그 | true면 보석 100% 드랍 (일반은 5%+gem_drop 강화) |

### 밸런스 예시

| 벌레 | 체력 | 속도 | 데미지 | 쿨다운 | DPS | 행동 |
|------|------|------|--------|--------|-----|------|
| Beetle | 20 | 1.5 | 8 | 1.5 | 5.3 | Linear + Melee |
| Fly | 8 | 4 | 3 | 0.5 | 6 | Hover + Melee |
| Spitter | 15 | 2 | 5 | 1.2 | 4.2 | Ranged + Projectile |
| Tank | 40 | 1 | 10 | 2 | 5 | Linear + Melee + Armor |
| Bomber | 12 | 3 | 5 | 1 | 5 | Linear + ExplodeOnDeath |

---

## 2. BugBehaviorData (행동 조합)

벌레가 **어떻게 행동하는지** 정의합니다. 하나의 BugBehaviorData에 여러 행동을 조합.

### 구조

```
BugBehaviorData
├── DefaultMovement ────── 기본 이동 (1개, 필수)
├── DefaultAttack ──────── 기본 공격 (1개, 필수)
├── ConditionalMovements ─ 조건부 이동 전환 (0~N개)
├── ConditionalAttacks ─── 조건부 공격 전환 (0~N개)
├── Passives[] ─────────── 패시브 능력 (0~N개)
├── Skills[] ───────────── 스킬 (0~N개)
└── Triggers[] ─────────── 조건부 발동 (0~N개)
```

### 2.1 Movement (이동 방식)

| Type | 동작 | Param1 | Param2 |
|------|------|--------|--------|
| **Linear** | 타겟으로 직진 | 옵션 (0=기본, 1=Strafe, 2=Orbit, 3=Retreat) | 옵션값 |
| **Hover** | 부유하며 접근 + Strafe | 부유 높이 | 부유 주기 |
| **Burst** | 대기 후 돌진 | 대기 시간 | 속도 배율 |
| **Ranged** | 사거리 유지 + 좌우 이동 | 유지 거리 | 횡이동 배율 |
| **Orbit** | 타겟 주위 공전 | 공전 반경 | 각속도 |
| **Retreat** | 후퇴 | 후퇴 시간 | 속도 배율 |
| **SlowStart** | 점진 가속 | 시작 속도비 | 도달 시간 |
| **Teleport** | 순간이동 | 쿨다운 | 이동 거리 |

### 2.2 Attack (공격 방식)

| Type | 동작 | Param1 | Param2 | Range |
|------|------|--------|--------|-------|
| **Melee** | 근접 즉발 | - | - | 1.5 |
| **Projectile** | 투사체 발사 | 투사체 속도 | - | 5 |
| **Cleave** | 부채꼴 범위 | 각도 | - | 2 |
| **Spread** | 다발 발사 | 발사 수 | 확산 각도 | 5 |
| **Beam** | 지속 레이저 | 지속 시간 | 틱 간격 | 6 |

### 2.3 Passive (패시브 능력)

| Type | 효과 | Param1 | Param2 |
|------|------|--------|--------|
| **Armor** | 데미지 감소 | 감소량 (고정값) | - |
| **Dodge** | 확률 회피 | 회피 확률 (%) | - |
| **Shield** | 보호막 흡수 + 재생 | 최대 보호막 | 재생 속도 |
| **Regen** | 체력 재생 | 초당 회복량 | - |
| **PoisonAttack** | 독 공격 | 초당 데미지 | 지속 시간 |
| **Burrow** | 땅속 숨기 | 숨는 시간 | 쿨다운 |

### 2.4 Skill (스킬)

| Type | 효과 | Param1 | Range | Cooldown |
|------|------|--------|-------|----------|
| **Nova** | 전방향 폭발 | 데미지 | 3 | 5 |
| **Spawn** | 졸개 소환 | 소환 수 | - | 8 |
| **BuffAlly** | 아군 강화 | 버프량 (%) | 4 | 10 |
| **HealAlly** | 아군 회복 | 회복량 | 4 | 6 |

### 2.5 Trigger (조건부 발동)

| Type | 발동 조건 | Param1 | Param2 |
|------|----------|--------|--------|
| **Enrage** | HP ≤ N% | HP 임계값 (%) | 버프량 (%) |
| **ExplodeOnDeath** | 사망 시 | 폭발 데미지 | 폭발 반경 |
| **SplitOnDeath** | 사망 시 분열 | 분열 수 | HP 비율 |
| **PanicBurrow** | HP ≤ N% + 피격 | HP 임계값 | - |

### 행동 조합 예시

| 프리셋 | Movement | Attack | Passive | Trigger |
|--------|----------|--------|---------|---------|
| **Beetle** (돌격형) | Linear | Melee | - | - |
| **Fly** (부유형) | Hover | Melee | - | - |
| **Tank** (방어형) | Linear | Melee | Armor | - |
| **Spitter** (원거리) | Ranged | Projectile | - | - |
| **Bomber** (자폭형) | Linear | Melee | - | ExplodeOnDeath |
| **Elite** (엘리트) | Linear | Cleave | Shield | Enrage |

---

## 3. WaveData (웨이브 데이터)

각 웨이브의 난이도와 스폰 패턴을 정의합니다.

### 기본 설정

| 컬럼 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| **WaveNumber** | int | - | 웨이브 순서 |
| **WaveName** | string | - | 웨이브 이름 |
| **WaveDuration** | float | 60 | 웨이브 지속 시간 (초) |
| **HealthMultiplier** | float | 1 | 체력 배율 |
| **DamageMultiplier** | float | 1 | 공격력 배율 |
| **SpeedMultiplier** | float | 1 | 속도 배율 |

### SpawnGroup (스폰 그룹)

| 컬럼 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| **BugData** | SO 참조 | - | 스폰할 벌레 데이터 |
| **Count** | int | 5 | 스폰 수량 |
| **StartDelay** | float | 0 | 웨이브 시작 후 대기 (초) |
| **SpawnInterval** | float | 1 | 스폰 간격 (초) |

### 웨이브 설계 예시

```
Wave 1: 입문 (30초)
├─ Group 1: Beetle x5, 간격 2초
└─ 총 5마리

Wave 2: 혼합 (45초)
├─ Group 1: Beetle x5, 간격 1.5초, 시작 0초
├─ Group 2: Fly x5, 간격 1초, 시작 10초
└─ 총 10마리

Wave 3: 원거리 (45초)
├─ Group 1: Fly x8, 간격 0.8초
├─ Group 2: Spitter x3, 간격 3초, 시작 5초
└─ 총 11마리

Wave 5: 보스 (60초)
├─ Group 1: Fly x10, 간격 1초
├─ Group 2: Beetle x5, 시작 10초
├─ Group 3: Tank x1, 시작 30초 (미니보스)
└─ 총 16마리
```

---

## 4. MachineData (채굴 머신 데이터)

플레이어가 지키는 채굴 머신의 스탯입니다.

### 필드 정의

| 컬럼 | 타입 | 기본값 | 설명 |
|------|------|--------|------|
| **MaxHealth** | float | 100 | 최대 체력 |
| **Armor** | float | 0 | 방어력 (legacy `armor/(armor+100)` 곡선) |
| **MaxFuel** | float | 60 | 최대 연료 (v2에선 세션 타임아웃 용도) |
| **MiningRate** | float | 10 | 초당 채굴량 |
| **BaseMiningTarget** (v2) | float | 100 | 세션 승리 목표 채굴량 (mineTarget 강화로 +50/lv) |
| **AttackDamage** | float | 20 | 플레이어 공격력 |
| **AttackCooldown** | float | 0.5 | 공격 쿨다운 |
| **AttackRange** | float | 3 | 공격 사거리 |

### 머신 타입 예시

| 머신 | 체력 | 방어력 | 연료 | 공격력 | 컨셉 |
|------|------|--------|------|--------|------|
| Default | 100 | 0 | 60 | 20 | 균형형 |
| Heavy | 150 | 20 | 45 | 15 | 방어형 |
| Speed | 80 | 0 | 90 | 25 | 공격형 |

---

## 5. UpgradeData (업그레이드 데이터)

영구 강화 시스템입니다. 아웃게임에서 재화로 구매.

### 필드 정의

| 컬럼 | 타입 | 설명 |
|------|------|------|
| **UpgradeId** | string | 고유 ID (예: `mine_speed`, `excavator_hp`) |
| **DisplayName** | string | 표시 이름 |
| **MaxLevel** | int | 최대 레벨 |
| **ValuePerLevel** | float | 레벨당 증가 |
| **IsPercentage** | bool | % 적용 여부 |
| **BaseCost** | int | 1레벨 광석 비용 (레거시 `BaseCostOre` 별칭) |
| **BaseCostGem** (v2) | int | 1레벨 보석 비용 (gem_drop/gem_speed에 사용) |
| **CostMultiplier** | float | 비용 증가율 |
| **OreCostSchedule** (v2) | int[] | 레벨별 광석 비용 명시 배열 (비어있으면 multiplier 사용) |
| **GemCostSchedule** (v2) | int[] | 레벨별 보석 비용 명시 배열 |

### UpgradeType 목록 (v2 현행, 6종 활성)

| Type | UpgradeId | MaxLv | ValuePerLevel | IsPercentage | 비용 schedule |
|------|-----------|-------|----|----|----|
| MaxHealth | excavator_hp | 5 | +30 | false | 광석 [60,130,230,370,540] |
| Armor | excavator_armor | 3 | +0.15 (받는 피해 감소율) | true | 광석 [150,300,500] |
| MiningRate | mine_speed | 5 | +2 (초당 채굴) | false | 광석 [80,160,280,440,640] |
| MiningTarget (v2) | mine_target | 5 | +50 (목표량) | false | 광석 [100,200,350,550,800] |
| GemDropRate (v2) | gem_drop | 5 | +0.02 (확률 %p) | false | **보석** [15,30,50,75,105] |
| GemCollectSpeed (v2) | gem_speed | 5 | +0.20 (배율) | true | **보석** [10,22,38,58,82] |

> 무기별 강화는 `WeaponUpgradeData` SO로 분리(15항목). Sniper/Bomb/Gun/Laser/Saw 각 3종. 상세: `WeaponUnlockUpgradeSystem.md`.

---

## Google Sheets 시트 구조

| 시트 이름 | 설명 |
|----------|------|
| **BugData** | 벌레 스탯 |
| **BugBehaviors** | 행동 조합 프리셋 |
| **MovementData** | 이동 방식 정의 |
| **AttackData** | 공격 방식 정의 |
| **PassiveData** | 패시브 정의 |
| **SkillData** | 스킬 정의 |
| **TriggerData** | 트리거 정의 |
| **WaveData** | 웨이브 기본 설정 |
| **WaveSpawnGroups** | 스폰 그룹 |
| **MachineData** | 머신 스탯 |
| **UpgradeData** | 업그레이드 설정 |

### 데이터 연결 관계

```
BugData ←─────────────────┐
    ↑                     │
    │ (BehaviorData 참조) │
    │                     │
BugBehaviorData ──────────┼── MovementData (참조)
                          ├── AttackData (참조)
                          ├── PassiveData[] (참조)
                          ├── SkillData[] (참조)
                          └── TriggerData[] (참조)

WaveSpawnGroups ─── BugData (참조)
       ↑
       │ (WaveNumber로 연결)
WaveData
```

---

## 밸런스 가이드라인

### DPS 계산
```
플레이어 DPS = AttackDamage / AttackCooldown
적 EHP = MaxHealth × HealthMultiplier
적 처치 시간 = 적 EHP / 플레이어 DPS
```

### 방어력 공식
```
실제 데미지 = 원본 데미지 × (1 - Armor / (Armor + 100))

예시:
- Armor 0: 100% 데미지
- Armor 20: 83.3% 데미지
- Armor 50: 66.7% 데미지
```

### 난이도 곡선

| 웨이브 | 체력 배율 | 공격력 배율 | 총 적 수 | 난이도 |
|--------|----------|------------|---------|--------|
| 1 | 1.0 | 1.0 | 5 | 입문 |
| 2 | 1.0 | 1.0 | 10 | 쉬움 |
| 3 | 1.1 | 1.0 | 13 | 보통 |
| 4 | 1.2 | 1.1 | 15 | 어려움 |
| 5 | 1.3 | 1.2 | 16 | 보스 |

---

## 참고 문서

- 행동 시스템 상세: `docs/BugBehaviorSystemAnalysis.md`
- 개발 계획: `docs/BugBehaviorDevelopmentPlan.md`
- 아키텍처: `docs/Architecture.md`

---

*마지막 업데이트: 2026-04-06*

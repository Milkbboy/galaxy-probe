# Bug 행동 패턴 가이드

## 이 문서는?

벌레(Bug)의 행동을 Google Sheets에서 설정하는 방법을 설명합니다.
프로그래머 없이도 다양한 벌레를 만들 수 있습니다.

---

## 빠른 시작

### 벌레는 5가지 행동으로 구성됩니다

| 행동 | 설명 | 예시 |
|------|------|------|
| **Movement** | 어떻게 이동하나요? | 직선, 공중 부유, 돌진 |
| **BasicAttack** | 평타는 뭔가요? | 근접, 원거리, 범위 |
| **Skills** | 특수 기술이 있나요? | 소환, 폭발, 버프 |
| **Passives** | 항상 가진 특성은? | 방어력, 회피, 독 공격 |
| **Triggers** | 특정 상황에 발동하는 것은? | 피 30% 이하면 광폭화 |

### 가장 간단한 벌레 예시

**Beetle** - 그냥 걸어와서 때리는 벌레
```
Movement: Linear
BasicAttack: Melee
Skills: (비워두기)
Passives: (비워두기)
Triggers: (비워두기)
```

---

## Google Sheets 작성법

### 시트 1: BugData

벌레의 기본 스탯을 입력합니다.

| BugId | BugName | MaxHealth | MoveSpeed | AttackDamage | AttackCooldown |
|-------|---------|-----------|-----------|--------------|----------------|
| 1 | Beetle | 10 | 2 | 5 | 1 |
| 2 | Fly | 8 | 4 | 3 | 0.5 |

- **BugId**: 고유 번호 (중복 불가)
- **BugName**: 영문 이름
- **MaxHealth**: 체력
- **MoveSpeed**: 이동 속도
- **AttackDamage**: 공격력
- **AttackCooldown**: 공격 간격 (초)

### 시트 2: BugBehaviors

벌레의 행동을 조합합니다.

| BugId | Movement | BasicAttack | Skills | Passives | Triggers |
|-------|----------|-------------|--------|----------|----------|
| 1 | Linear | Melee | | | |
| 2 | Hover | Melee | | Dodge:20 | |

---

## 작성 문법

### 기본 규칙

```
타입명              → 기본값 사용
타입명:숫자          → 숫자 파라미터 지정
타입명:숫자:숫자     → 여러 파라미터 지정
값1, 값2            → 여러 개 조합 (콤마로 구분)
```

### 예시

| 작성법 | 의미 |
|--------|------|
| `Linear` | 직선 이동 |
| `Dodge:20` | 20% 확률로 회피 |
| `Spawn:10:Beetle:2` | 10초마다 Beetle 2마리 소환 |
| `Armor:5, Regen:2` | 방어력 5 + 초당 2 회복 |

---

## Movement (이동 방식)

벌레가 어떻게 움직이는지 설정합니다.

### 목록

| 이름 | 작성법 | 설명 |
|------|--------|------|
| 직선 | `Linear` | 타겟을 향해 직선으로 이동 |
| 공중 부유 | `Hover` | 위아래로 떠다니며 이동 |
| | `Hover:높이:주기` | 예: `Hover:0.5:2` (0.5 높이, 2초 주기) |
| 돌진 | `Burst` | 잠시 멈췄다가 빠르게 돌진 |
| | `Burst:대기:속도배율` | 예: `Burst:2:3` (2초 대기, 3배속 돌진) |
| 후퇴 | `Retreat:시간` | 공격 후 뒤로 물러남. 예: `Retreat:2` |
| 느린 시작 | `SlowStart` | 천천히 시작해서 점점 빨라짐 |
| 선회 | `Orbit:반경` | 타겟 주위를 돌며 접근. 예: `Orbit:5` |
| 순간이동 | `Teleport:쿨타임` | 순간이동. 예: `Teleport:5` |
| 급강하 | `Dive` | 높이 떴다가 타겟에게 급강하 |

---

## BasicAttack (기본 공격)

평타 방식을 설정합니다.

### 목록

| 이름 | 작성법 | 설명 |
|------|--------|------|
| 근접 | `Melee` | 가까이 가서 한 대 때림 |
| 범위 근접 | `Cleave:각도` | 부채꼴로 휩쓸기. 예: `Cleave:90` (90도) |
| 원거리 | `Projectile` | 투사체 1발 발사 |
| | `Projectile:속도` | 예: `Projectile:10` |
| 다발 발사 | `Spread:발수:각도` | 여러 발 발사. 예: `Spread:5:60` (5발, 60도) |
| ~~유도탄~~ | ~~`Homing:속도:회전`~~ | ~~따라가는 투사체~~ (제외: 머신이 고정이라 유도 의미 없음) |
| 레이저 | `Beam:시간` | 지속 레이저. 예: `Beam:2` (2초) |
| 포물선 | `Lob` | 장애물 넘어가는 투사체 |

---

## Skills (스킬)

쿨타임마다 사용하는 특수 기술입니다. **여러 개 가능** (콤마로 구분)

### 목록

| 이름 | 작성법 | 설명 |
|------|--------|------|
| **공격** | | |
| 전방향 폭발 | `Nova:쿨타임` | 주변 모두 공격. 예: `Nova:15` |
| 차지 공격 | `Charge:쿨타임:배율` | 강한 일격. 예: `Charge:8:3` (3배 데미지) |
| 돌진 공격 | `Lunge:쿨타임:거리` | 돌진 공격. 예: `Lunge:5:4` |
| **소환** | | |
| 졸개 소환 | `Spawn:쿨타임:종류:수량` | 예: `Spawn:10:Beetle:2` |
| **버프/디버프** | | |
| 아군 강화 | `BuffAlly:쿨타임:배율` | 예: `BuffAlly:20:1.2` (1.2배) |
| 아군 회복 | `HealAlly:쿨타임:양` | 예: `HealAlly:15:20` |
| 자기 강화 | `SelfBuff:쿨타임:지속:배율` | 예: `SelfBuff:10:5:1.5` |
| 감속 | `Slow:쿨타임:지속:비율` | 예: `Slow:10:3:0.5` (50% 감속) |
| 기절 | `Stun:쿨타임:지속` | 예: `Stun:12:1.5` |
| 중독 | `Poison:쿨타임:지속:데미지` | 예: `Poison:8:5:3` |

### 작성 예시

```
Skills: Spawn:10:Beetle:2, Nova:15
```
→ 10초마다 Beetle 2마리 소환 + 15초마다 전방향 폭발

---

## Passives (패시브)

항상 적용되는 특성입니다. **여러 개 가능** (콤마로 구분)

### 목록

| 이름 | 작성법 | 설명 |
|------|--------|------|
| **방어** | | |
| 방어력 | `Armor:수치` | 받는 데미지 감소. 예: `Armor:5` |
| 보호막 | `Shield:양:재생쿨` | 데미지 흡수. 예: `Shield:50:30` |
| 체력 재생 | `Regen:초당` | 예: `Regen:2` (초당 2 회복) |
| 회피 | `Dodge:확률` | 예: `Dodge:20` (20% 회피) |
| 반사 | `Reflect:비율` | 예: `Reflect:30` (30% 반사) |
| **공격** | | |
| 흡혈 | `Lifesteal:비율` | 예: `Lifesteal:10` (10% 흡혈) |
| 치명타 | `CritChance:확률:배율` | 예: `CritChance:15:2` |
| 독 공격 | `PoisonAttack:지속:데미지` | 예: `PoisonAttack:3:5` |
| **이동** | | |
| 빠른 이동 | `Fast:배율` | 예: `Fast:1.5` (1.5배 빠름) |
| **특수** | | |
| 땅속 숨기 | `Burrow:시간:애니시간` | 피격 시 땅속에 숨음 (무적). 예: `Burrow:2:0.3` |

### 작성 예시

```
Passives: Armor:10, Regen:2, Dodge:15
```
→ 방어력 10 + 초당 2 회복 + 15% 회피

---

## Triggers (트리거)

특정 조건에서 발동합니다. **여러 개 가능** (콤마로 구분)

### 목록

| 이름 | 작성법 | 발동 조건 | 효과 |
|------|--------|-----------|------|
| 광폭화 | `Enrage:HP%:배율` | HP가 N% 이하 | 공격력 증가 |
| | 예: `Enrage:30:2` | HP 30% 이하 | 공격력 2배 |
| 최후의 저항 | `LastStand:HP%:배율` | HP가 N% 이하 | 방어력 증가 |
| | 예: `LastStand:20:1.5` | HP 20% 이하 | 방어 1.5배 |
| 갑옷 파괴 | `ArmorBreak:횟수` | N번 피격 | 방어력 제거 |
| | 예: `ArmorBreak:5` | 5번 맞으면 | Armor 효과 사라짐 |
| 변신 | `Transform:HP%` | HP가 N% 이하 | 2페이즈로 변신 |
| | 예: `Transform:30` | HP 30% 이하 | 강해짐 |
| 자폭 | `ExplodeOnDeath:범위:데미지` | 사망 시 | 폭발 |
| | 예: `ExplodeOnDeath:3:50` | 죽으면 | 범위 3, 50 데미지 |
| 분열 | `SplitOnDeath:종류:수량` | 사망 시 | 작은 적으로 분열 |
| | 예: `SplitOnDeath:MiniBeetle:3` | 죽으면 | 3마리로 분열 |
| 부활 | `Revive:HP%` | 사망 시 | 한 번 부활 |
| | 예: `Revive:50` | 죽으면 | 50% 체력으로 부활 |
| 장판 | `DropHazard:범위:지속:데미지` | 사망 시 | 위험 지역 생성 |
| 성장 | `Grow:시간:배율` | 시간 경과 | 점점 강해짐 |
| | 예: `Grow:30:1.5` | 30초마다 | 1.5배 성장 |
| 공포 도피 | `PanicBurrow:HP%:쿨타임` | HP 이하 + 피격 | 땅속 숨기 |
| | 예: `PanicBurrow:50:5` | HP 50% 이하 + 피격 | Burrow 발동 |

### 작성 예시

```
Triggers: Enrage:30:2, ExplodeOnDeath:3:50
```
→ HP 30% 이하면 공격력 2배 + 죽으면 폭발

### Burrow + PanicBurrow 조합

땅속에 숨는 행동은 **Passive + Trigger 조합**으로 구현합니다:

```
Passives: Burrow:2:0.3
Triggers: PanicBurrow:50:5
```
→ HP 50% 이하일 때 피격 시 땅속에 2초간 숨음 (무적)
→ 쿨타임 5초 후 재발동 가능

**주의**: Burrow는 반드시 Passives에 추가해야 PanicBurrow가 작동합니다.

---

## 조건부 행동 (ConditionalBehaviors 시트)

"평소엔 A, 특정 상황엔 B"를 설정합니다.

### 시트 구조

| BugId | Category | Default | Condition | SwitchTo |
|-------|----------|---------|-----------|----------|
| 3 | Movement | Linear | AfterAttack | Retreat:2 |
| 3 | BasicAttack | Projectile | Distance<2 | Melee |

### 읽는 법

위 예시를 해석하면:
- BugId 3번 벌레의 **이동**: 평소엔 `Linear`, 공격 후엔 `Retreat:2`
- BugId 3번 벌레의 **공격**: 평소엔 `Projectile`, 거리 2 미만이면 `Melee`

### 사용 가능한 조건 (Condition)

| 조건 | 의미 | 예시 |
|------|------|------|
| `HP<숫자` | 체력이 N% 미만 | `HP<30` |
| `HP>숫자` | 체력이 N% 초과 | `HP>50` |
| `Distance<숫자` | 거리가 N 미만 | `Distance<3` |
| `Distance>숫자` | 거리가 N 초과 | `Distance>5` |
| `AfterAttack` | 공격 직후 | `AfterAttack` |
| `HitCount>숫자` | N번 이상 맞았을 때 | `HitCount>5` |
| `Time>숫자` | 스폰 후 N초 경과 | `Time>10` |

---

## 완성 예시

### 예시 1: Beetle (기본형)

**BugData:**
| BugId | BugName | MaxHealth | MoveSpeed | AttackDamage | AttackCooldown |
|-------|---------|-----------|-----------|--------------|----------------|
| 1 | Beetle | 10 | 2 | 5 | 1 |

**BugBehaviors:**
| BugId | Movement | BasicAttack | Skills | Passives | Triggers |
|-------|----------|-------------|--------|----------|----------|
| 1 | Linear | Melee | | | |

**결과:** 직선으로 걸어와서 근접 공격

---

### 예시 2: Fly (회피형)

**BugData:**
| BugId | BugName | MaxHealth | MoveSpeed | AttackDamage | AttackCooldown |
|-------|---------|-----------|-----------|--------------|----------------|
| 2 | Fly | 8 | 4 | 3 | 0.5 |

**BugBehaviors:**
| BugId | Movement | BasicAttack | Skills | Passives | Triggers |
|-------|----------|-------------|--------|----------|----------|
| 2 | Hover:0.3:2 | Melee | | Dodge:20 | |

**결과:** 위아래로 떠다니며 접근, 공격의 20%를 회피

---

### 예시 3: Spitter (원거리)

**BugData:**
| BugId | BugName | MaxHealth | MoveSpeed | AttackDamage | AttackCooldown |
|-------|---------|-----------|-----------|--------------|----------------|
| 3 | Spitter | 12 | 2 | 8 | 1.5 |

**BugBehaviors:**
| BugId | Movement | BasicAttack | Skills | Passives | Triggers |
|-------|----------|-------------|--------|----------|----------|
| 3 | Linear | Projectile | | PoisonAttack:3:5 | |

**ConditionalBehaviors:**
| BugId | Category | Default | Condition | SwitchTo |
|-------|----------|---------|-----------|----------|
| 3 | Movement | Linear | AfterAttack | Retreat:2 |
| 3 | BasicAttack | Projectile | Distance<2 | Melee |

**결과:**
- 원거리에서 독 투사체 발사 (맞으면 3초간 5 데미지)
- 공격 후 2초간 후퇴
- 거리 2 미만이면 근접 공격으로 전환

---

### 예시 4: Bomber (자폭형)

**BugData:**
| BugId | BugName | MaxHealth | MoveSpeed | AttackDamage | AttackCooldown |
|-------|---------|-----------|-----------|--------------|----------------|
| 4 | Bomber | 5 | 3 | 0 | 999 |

**BugBehaviors:**
| BugId | Movement | BasicAttack | Skills | Passives | Triggers |
|-------|----------|-------------|--------|----------|----------|
| 4 | Burst:1:3 | | | Fast:1.5 | ExplodeOnDeath:3:50 |

**결과:**
- 1초 멈췄다가 3배속으로 돌진
- 이동속도 1.5배
- 죽으면 범위 3에 50 데미지 폭발

---

### 예시 5: Tank (탱커)

**BugData:**
| BugId | BugName | MaxHealth | MoveSpeed | AttackDamage | AttackCooldown |
|-------|---------|-----------|-----------|--------------|----------------|
| 5 | Tank | 50 | 1 | 15 | 2 |

**BugBehaviors:**
| BugId | Movement | BasicAttack | Skills | Passives | Triggers |
|-------|----------|-------------|--------|----------|----------|
| 5 | SlowStart | Cleave:90 | | Armor:10, Regen:2 | Enrage:30:2, ArmorBreak:5 |

**결과:**
- 천천히 시작해서 점점 빨라짐
- 90도 범위 공격
- 방어력 10, 초당 2 회복
- 5번 맞으면 방어력 사라짐
- HP 30% 이하면 공격력 2배

---

### 예시 6: Hive (소환형)

**BugData:**
| BugId | BugName | MaxHealth | MoveSpeed | AttackDamage | AttackCooldown |
|-------|---------|-----------|-----------|--------------|----------------|
| 6 | Hive | 30 | 1.5 | 5 | 1 |

**BugBehaviors:**
| BugId | Movement | BasicAttack | Skills | Passives | Triggers |
|-------|----------|-------------|--------|----------|----------|
| 6 | Linear | Melee | Spawn:15:Beetle:3 | Shield:30:20 | |

**결과:**
- 15초마다 Beetle 3마리 소환
- 30 보호막 (20초마다 재생)

---

### 예시 7: Queen (보스)

**BugData:**
| BugId | BugName | MaxHealth | MoveSpeed | AttackDamage | AttackCooldown |
|-------|---------|-----------|-----------|--------------|----------------|
| 7 | Queen | 200 | 1 | 20 | 1 |

**BugBehaviors:**
| BugId | Movement | BasicAttack | Skills | Passives | Triggers |
|-------|----------|-------------|--------|----------|----------|
| 7 | Orbit:5 | Spread:5:60 | Spawn:10:Beetle:2, Nova:15, BuffAlly:20:1.2 | Shield:100:30, Regen:5 | Transform:30 |

**ConditionalBehaviors:**
| BugId | Category | Default | Condition | SwitchTo |
|-------|----------|---------|-----------|----------|
| 7 | Movement | Orbit | HP<50 | Teleport:5 |

**결과:**
- 반경 5로 주위를 돌며 5발 발사 (60도 범위)
- 10초마다 Beetle 2마리 소환
- 15초마다 전방향 폭발
- 20초마다 주변 아군 1.2배 강화
- 100 보호막 (30초마다 재생), 초당 5 회복
- HP 50% 이하면 5초마다 순간이동
- HP 30% 이하면 2페이즈 변신

---

## 자주 묻는 질문

### Q: Skills, Passives, Triggers 칸을 비워둬도 되나요?
**A:** 네, 비워두면 해당 기능이 없는 벌레가 됩니다.

### Q: 여러 개 넣을 때 순서가 중요한가요?
**A:** 아니요, 순서는 상관없습니다. 콤마로 구분만 하면 됩니다.

### Q: 오타가 있으면 어떻게 되나요?
**A:** Import 시 오류가 나거나 해당 기능이 무시됩니다. 콘솔에서 경고를 확인하세요.

### Q: 숫자 단위가 뭔가요?
- **시간**: 초 (seconds)
- **거리**: Unity 단위 (약 1m)
- **퍼센트**: 0~100 숫자로 입력 (예: 20 = 20%)
- **배율**: 1.0 = 기본값 (예: 1.5 = 1.5배)

### Q: 새로운 행동을 추가하고 싶어요
**A:** 프로그래머에게 요청하세요. 새 ScriptableObject 타입을 만들어야 합니다.

---

## 변경 이력

| 날짜 | 변경 내용 |
|------|-----------|
| 2024-01-XX | 최초 작성 |
| 2024-XX-XX | Burrow를 Movement에서 Passive로 변경, PanicBurrow 트리거 추가 |

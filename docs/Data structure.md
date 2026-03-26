# 게임 데이터 구조 (Data Structure)

기획자가 Google Sheets에서 설정 가능한 게임 데이터 정의서입니다.

---

## 파일 구조

```
Assets/_Game/Data/
├── Bugs/
│   ├── Bug_Beetle.asset
│   ├── Bug_Fly.asset
│   ├── Bug_Centipede.asset
│   ├── Bug_Spider.asset
│   └── Bug_Wasp.asset
├── Waves/
│   ├── Wave_01.asset
│   ├── Wave_02.asset
│   ├── Wave_03.asset
│   ├── Wave_04.asset
│   └── Wave_05.asset
├── Machines/
│   ├── Machine_Default.asset
│   ├── Machine_Heavy.asset
│   └── Machine_Speed.asset
├── Upgrades/
│   ├── Upgrade_MaxHealth.asset
│   ├── Upgrade_Armor.asset
│   ├── Upgrade_MiningRate.asset
│   ├── Upgrade_AttackDamage.asset
│   ├── Upgrade_AttackSpeed.asset
│   └── Upgrade_FuelEfficiency.asset
└── Credentials/
    └── google-credentials.json (git 제외)
```

---

## 1. BugData (벌레 데이터)

벌레 몬스터의 모든 스탯을 정의합니다.

### 필드 정의

| 컬럼 | 타입 | 기본값 | 설명 | 기획 가이드 |
|------|------|--------|------|-------------|
| **BugId** | int | - | 고유 식별자 | 1, 2, 3... 순차적으로 부여. 코드에서 참조용 |
| **BugName** | string | - | 벌레 이름 | "딱정벌레", "파리", "지네" 등 |
| **Description** | string | - | 설명 텍스트 | UI 표시용. "느리지만 단단한 갑충" |
| **MaxHealth** | float | 10 | 최대 체력 | 높을수록 오래 버팀. 일반 5~20, 보스 100+ |
| **MoveSpeed** | float | 2 | 이동 속도 (유닛/초) | 1=느림, 2=보통, 4=빠름. 머신까지 도달 시간에 영향 |
| **AttackDamage** | float | 5 | 1회 공격 데미지 | 머신 MaxHealth(100) 기준으로 밸런싱 |
| **AttackCooldown** | float | 1 | 공격 간격 (초) | DPS = Damage / Cooldown. 0.5=빠름, 2=느림 |
| **AttackRange** | float | 1 | 공격 범위 (유닛) | 근접=1, 원거리=3+ |
| **TintColor** | Color | 흰색 | 스프라이트 색상 | R,G,B,A (0~1). 같은 프리팹 다른 색상 가능 |
| **Scale** | float | 1 | 크기 배율 | 0.5=절반, 2=2배. 엘리트/보스 차별화 |
| **HpBarOffsetX** | float | 0 | HP바 X 오프셋 | 좌우 위치 조정 |
| **HpBarOffsetY** | float | 0.1 | HP바 Y 오프셋 | 높이 조정 (지면 위) |
| **HpBarOffsetZ** | float | 0.8 | HP바 Z 오프셋 | 앞뒤 위치 (탑다운에서 위쪽) |
| **CurrencyReward** | int | 1 | 처치 보상 재화 | 강한 적=높은 보상. 인게임 경제 밸런스 핵심 |
| **DropChance** | float | 1 | 아이템 드롭 확률 | 0~1. 1=100%, 0.1=10% |

### 밸런스 예시

| 벌레 | 체력 | 속도 | 데미지 | 쿨다운 | DPS | 보상 | 특징 |
|------|------|------|--------|--------|-----|------|------|
| Beetle | 20 | 1.5 | 8 | 1.5 | 5.3 | 2 | 느리고 단단함 |
| Fly | 8 | 4 | 3 | 0.5 | 6 | 1 | 빠르고 약함 |
| Centipede | 30 | 2 | 12 | 2 | 6 | 3 | 돌진 특수능력 |
| Spider | 15 | 2.5 | 6 | 1 | 6 | 2 | 균형형 |
| Wasp | 10 | 5 | 4 | 0.3 | 13.3 | 2 | 초고속 공격 |

### DPS 계산 공식
```
DPS (초당 데미지) = AttackDamage / AttackCooldown
```

---

## 2. WaveData (웨이브 데이터)

각 웨이브의 난이도와 진행을 정의합니다.

### 기본 설정

| 컬럼 | 타입 | 기본값 | 설명 | 기획 가이드 |
|------|------|--------|------|-------------|
| **WaveNumber** | int | - | 웨이브 순서 | 1, 2, 3... 게임 진행 순서 |
| **WaveName** | string | - | 웨이브 이름 | "첫 번째 습격", "지네의 돌격" 등 |
| **WaveDuration** | float | 60 | 웨이브 지속 시간 (초) | 스폰 완료 후 대기 시간 포함 |
| **DelayBeforeNextWave** | float | 3 | 다음 웨이브 전 휴식 (초) | 플레이어 준비 시간 |
| **HealthMultiplier** | float | 1 | 체력 배율 | 1.5 = 모든 적 체력 50% 증가 |
| **DamageMultiplier** | float | 1 | 공격력 배율 | 후반 웨이브 난이도 조절용 |
| **SpeedMultiplier** | float | 1 | 속도 배율 | 1.2 = 20% 빠르게 |

### SpawnGroup (스폰 그룹)

한 웨이브에 여러 그룹 설정 가능. 다양한 스폰 패턴 구현.

| 컬럼 | 타입 | 기본값 | 설명 | 기획 가이드 |
|------|------|--------|------|-------------|
| **BugId** | int | - | 스폰할 벌레 ID | BugData의 BugId 참조 |
| **Count** | int | 5 | 스폰 수량 | 총 몇 마리 스폰할지 |
| **StartDelay** | float | 0 | 웨이브 시작 후 대기 (초) | 0=즉시, 10=10초 후 시작 |
| **SpawnInterval** | float | 1 | 스폰 간격 (초) | 0.5=빠르게 몰려옴, 3=천천히 |
| **RandomPosition** | bool | true | 랜덤 스폰 위치 | true=무작위, false=순차적 |

### WaveData 예시 값

| WaveNumber | WaveName | WaveDuration | DelayBeforeNext | HealthMult | DamageMult | SpeedMult |
|------------|----------|--------------|-----------------|------------|------------|-----------|
| 1 | 첫 번째 습격 | 30 | 3 | 1.0 | 1.0 | 1.0 |
| 2 | 혼합 공세 | 45 | 3 | 1.0 | 1.0 | 1.0 |
| 3 | 파리 떼 습격 | 45 | 3 | 1.1 | 1.0 | 1.0 |
| 4 | 강화된 적들 | 50 | 3 | 1.2 | 1.1 | 1.0 |
| 5 | 지네의 돌격 | 60 | 5 | 1.3 | 1.2 | 1.1 |

### WaveSpawnGroups 예시 값

| WaveNumber | GroupIndex | BugId | BugName | Count | StartDelay | SpawnInterval | RandomPosition |
|------------|------------|-------|---------|-------|------------|---------------|----------------|
| 1 | 1 | 1 | Beetle | 5 | 0 | 2.0 | true |
| 2 | 1 | 1 | Beetle | 5 | 0 | 1.5 | true |
| 2 | 2 | 2 | Fly | 5 | 10 | 1.0 | true |
| 3 | 1 | 2 | Fly | 10 | 0 | 0.5 | true |
| 3 | 2 | 1 | Beetle | 3 | 5 | 3.0 | true |
| 4 | 1 | 1 | Beetle | 5 | 0 | 1.5 | true |
| 4 | 2 | 2 | Fly | 8 | 5 | 0.8 | true |
| 4 | 3 | 4 | Spider | 2 | 15 | 5.0 | true |
| 5 | 1 | 2 | Fly | 10 | 0 | 1.0 | true |
| 5 | 2 | 1 | Beetle | 5 | 10 | 2.0 | true |
| 5 | 3 | 3 | Centipede | 1 | 20 | 0 | false |

### 웨이브 설계 다이어그램

```
Wave 1: 입문 (30초)
├─ Group 1: Beetle x5, 간격 2초
└─ 총 5마리, 예상 클리어 10초

Wave 2: 혼합 (45초)
├─ Group 1: Beetle x5, 간격 1.5초, 시작 0초
├─ Group 2: Fly x5, 간격 1초, 시작 10초
└─ 총 10마리, 혼합 전투

Wave 3: 러시 (45초)
├─ Group 1: Fly x10, 간격 0.5초 (빠른 러시)
├─ Group 2: Beetle x3, 간격 3초, 시작 5초 (탱커)
└─ 총 13마리, 물량 압박

Wave 4: 강화 (50초)
├─ Group 1: Beetle x5, 간격 1.5초
├─ Group 2: Fly x8, 간격 0.8초, 시작 5초
├─ Group 3: Spider x2, 간격 5초, 시작 15초
└─ 총 15마리, 3종 혼합

Wave 5: 보스 (60초)
├─ Group 1: Fly x10, 간격 1초 (잡몹 선행)
├─ Group 2: Beetle x5, 간격 2초, 시작 10초
├─ Group 3: Centipede x1, 시작 20초 (보스 등장)
└─ 총 16마리, 보스전
```

### 난이도 곡선 요약

| 웨이브 | 체력 배율 | 공격력 배율 | 속도 배율 | 총 적 수 | 난이도 |
|--------|----------|------------|----------|---------|--------|
| 1 | 1.0 | 1.0 | 1.0 | 5 | 입문 |
| 2 | 1.0 | 1.0 | 1.0 | 10 | 쉬움 |
| 3 | 1.1 | 1.0 | 1.0 | 13 | 보통 |
| 4 | 1.2 | 1.1 | 1.0 | 15 | 어려움 |
| 5 | 1.3 | 1.2 | 1.1 | 16 | 보스 |

---

## 3. MachineData (채굴 머신 데이터)

플레이어가 지키는 채굴 머신의 스탯입니다.

### 필드 정의

| 컬럼 | 타입 | 기본값 | 설명 | 기획 가이드 |
|------|------|--------|------|-------------|
| **MachineId** | int | - | 고유 ID | 머신 종류 구분 |
| **MachineName** | string | - | 머신 이름 | "기본형", "중장갑", "고속형" |
| **Description** | string | - | 설명 | "균형 잡힌 기본 채굴기" |
| **MaxHealth** | float | 100 | 최대 체력 | 0이 되면 게임 오버 |
| **HealthRegen** | float | 0 | 초당 체력 회복 | 0=회복 없음, 1=초당 1 회복 |
| **Armor** | float | 0 | 방어력 | 데미지 감소율: armor/(armor+100) |
| **MaxFuel** | float | 60 | 최대 연료 (=세션 시간) | 60=1분, 180=3분 세션 |
| **FuelConsumeRate** | float | 1 | 초당 연료 소비 | 1=실시간, 0.5=2배 오래 지속 |
| **MiningRate** | float | 10 | 초당 채굴량 | 세션 보상에 직결 |
| **MiningBonus** | float | 0 | 채굴 보너스 (%) | 0.1=10% 추가 채굴 |
| **AttackDamage** | float | 20 | 플레이어 공격력 | 마우스 클릭 시 데미지 |
| **AttackCooldown** | float | 0.5 | 공격 쿨다운 (초) | 낮을수록 빠른 연사 |
| **AttackRange** | float | 3 | 공격 사거리 | 화면 내 타격 범위 |
| **CritChance** | float | 0 | 치명타 확률 | 0~1. 0.1=10% 확률 |
| **CritMultiplier** | float | 1.5 | 치명타 배율 | 1.5=150% 데미지 |

### 머신 타입 예시

| 머신 | 체력 | 방어력 | 연료 | 공격력 | 쿨다운 | 채굴 | 컨셉 |
|------|------|--------|------|--------|--------|------|------|
| Default | 100 | 0 | 60 | 20 | 0.5 | 10 | 균형형 |
| Heavy | 150 | 20 | 45 | 15 | 0.7 | 8 | 방어형 |
| Speed | 80 | 0 | 90 | 25 | 0.3 | 15 | 공격형 |

### 방어력 공식
```
실제 받는 데미지 = 원본 데미지 × (1 - Armor / (Armor + 100))

예시:
- Armor 0: 100% 데미지
- Armor 10: 90.9% 데미지 (9.1% 감소)
- Armor 20: 83.3% 데미지 (16.7% 감소)
- Armor 50: 66.7% 데미지 (33.3% 감소)
- Armor 100: 50% 데미지
```

---

## 4. UpgradeData (업그레이드 데이터)

영구 강화 시스템입니다. 아웃게임에서 재화로 구매.

### 필드 정의

| 컬럼 | 타입 | 기본값 | 설명 | 기획 가이드 |
|------|------|--------|------|-------------|
| **UpgradeId** | string | - | 고유 ID | "max_health", "attack_damage" |
| **DisplayName** | string | - | 표시 이름 | "최대 체력", "공격력" |
| **Description** | string | - | 설명 | "머신의 최대 체력을 증가시킵니다" |
| **UpgradeType** | enum | - | 적용 대상 | 아래 표 참조 |
| **MaxLevel** | int | 10 | 최대 레벨 | 성장 한계 |
| **BaseValue** | float | 0 | 0레벨 보너스 | 보통 0 (강화 전 보너스 없음) |
| **ValuePerLevel** | float | 1 | 레벨당 증가 | 레벨 5 = BaseValue + 5×ValuePerLevel |
| **IsPercentage** | bool | false | % 적용 여부 | true면 곱연산, false면 합연산 |
| **BaseCost** | int | 100 | 1레벨 비용 | 첫 강화 가격 |
| **CostMultiplier** | float | 1.5 | 비용 증가율 | 레벨N 비용 = BaseCost × (Multiplier^N) |

### UpgradeType 목록

| Type | 설명 | 권장 ValuePerLevel | IsPercentage |
|------|------|-------------------|--------------|
| MaxHealth | 최대 체력 | +10 | false |
| Armor | 방어력 | +5 | false |
| HealthRegen | 체력 재생 | +0.5 | false |
| MaxFuel | 최대 연료 | +10 | false |
| FuelEfficiency | 연료 효율 | +5 | true (%) |
| MiningRate | 채굴 속도 | +2 | false |
| AttackDamage | 공격력 | +5 | false |
| AttackSpeed | 공격 속도 | +10 | true (%) |
| AttackRange | 공격 범위 | +0.5 | false |
| CritChance | 치명타 확률 | +3 | true (%) |
| CritDamage | 치명타 배율 | +10 | true (%) |

### 비용 곡선 예시

BaseCost=100, CostMultiplier=1.5 기준:

| 레벨 | 비용 | 누적 비용 |
|------|------|----------|
| 1 | 100 | 100 |
| 2 | 150 | 250 |
| 3 | 225 | 475 |
| 4 | 338 | 813 |
| 5 | 506 | 1,319 |
| 6 | 759 | 2,078 |
| 7 | 1,139 | 3,217 |
| 8 | 1,709 | 4,926 |
| 9 | 2,563 | 7,489 |
| 10 | 3,844 | 11,333 |

### 강화값 공식
```
현재 강화값 = BaseValue + (ValuePerLevel × 현재 레벨)

예시 (MaxHealth: BaseValue=0, ValuePerLevel=10):
- 레벨 0: +0 체력
- 레벨 5: +50 체력
- 레벨 10: +100 체력
```

---

## Google Sheets 시트 구조

Google Sheets에서 데이터 관리 시 권장 구조:

| 시트 이름 | 설명 |
|----------|------|
| **BugData** | 벌레 스탯 (1행 = 1벌레) |
| **WaveData** | 웨이브 기본 설정 (1행 = 1웨이브) |
| **WaveSpawnGroups** | 스폰 그룹 (WaveNumber로 연결) |
| **MachineData** | 채굴 머신 스탯 (1행 = 1머신) |
| **UpgradeData** | 업그레이드 설정 (1행 = 1업그레이드) |

### 시트 연결 관계

```
BugData (BugId) ←──┐
                   │
WaveSpawnGroups ───┘ (BugId로 참조)
       ↑
       │ (WaveNumber로 연결)
       │
WaveData (WaveNumber)
```

---

## 밸런스 가이드라인

### 세션 목표 시간
- 일반 세션: 60초 (Wave 1~5)
- 긴 세션: 180초 (Wave 1~15)

### 난이도 곡선
- 초반 (Wave 1~2): 적응 구간, 느린 스폰
- 중반 (Wave 3~4): 난이도 상승, 혼합 스폰
- 후반 (Wave 5+): 러시 또는 보스

### 경제 밸런스
- 세션당 기대 수입 = Σ(적 수 × CurrencyReward)
- 업그레이드 비용은 3~5세션 플레이로 1레벨 구매 가능하게

### DPS vs EHP (유효 체력)
```
플레이어 DPS = AttackDamage / AttackCooldown
적 EHP = MaxHealth × HealthMultiplier
적 처치 시간 = 적 EHP / 플레이어 DPS
```

---

*마지막 업데이트: 2024*

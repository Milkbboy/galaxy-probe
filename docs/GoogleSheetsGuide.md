# Google Sheets 데이터 관리 가이드

## 개요

게임 데이터(벌레, 웨이브, 머신, 업그레이드)는 Google Sheets에서 관리하고, Unity Editor에서 Import하여 ScriptableObject로 변환합니다.

**스프레드시트 URL**: https://docs.google.com/spreadsheets/d/1hwgQ4IF-gQqVSX4xS_uqeKIPWUDy2NR4bC-OWmZQO_E/edit

---

## 시트 구조

스프레드시트에는 **5개의 시트 탭**이 필요합니다. 시트 이름은 정확히 일치해야 합니다.

| 시트 이름 | 설명 |
|-----------|------|
| `BugData` | 벌레 종류별 스탯 |
| `WaveData` | 웨이브 기본 설정 |
| `WaveSpawnGroups` | 웨이브별 스폰 그룹 |
| `MachineData` | 채굴 머신 스탯 |
| `UpgradeData` | 업그레이드 항목 |

---

## 1. BugData 시트

벌레 종류별 기본 스탯을 정의합니다.

### 컬럼 구조

| 컬럼명 | 타입 | 필수 | 설명 | 예시 |
|--------|------|------|------|------|
| BugId | 정수 | O | 고유 ID | 1, 2, 3 |
| BugName | 문자열 | O | 벌레 이름 (영문) | Beetle, Ant, Spider |
| Description | 문자열 | | 설명 | 기본 근접 벌레 |
| MaxHealth | 실수 | O | 최대 체력 | 10, 25.5 |
| MoveSpeed | 실수 | O | 이동 속도 | 2.0, 3.5 |
| AttackDamage | 실수 | O | 공격력 | 5, 10 |
| AttackCooldown | 실수 | O | 공격 쿨다운 (초) | 1.0, 0.5 |
| AttackRange | 실수 | O | 공격 사거리 | 1.0, 2.5 |
| Scale | 실수 | | 크기 배율 (기본 1.0) | 1.0, 1.5 |
| CurrencyReward | 정수 | | 처치 시 보상 | 1, 5 |
| DropChance | 실수 | | 드롭 확률 (0~1) | 1.0, 0.5 |
| HpBarOffsetX | 실수 | | HP바 X 오프셋 | 0 |
| HpBarOffsetY | 실수 | | HP바 Y 오프셋 | 0.1 |
| HpBarOffsetZ | 실수 | | HP바 Z 오프셋 | 0.8 |

### 예시 데이터

| BugId | BugName | Description | MaxHealth | MoveSpeed | AttackDamage | AttackCooldown | AttackRange | Scale | CurrencyReward |
|-------|---------|-------------|-----------|-----------|--------------|----------------|-------------|-------|----------------|
| 1 | Beetle | 기본 근접 벌레 | 10 | 2 | 5 | 1 | 1 | 1 | 1 |
| 2 | Ant | 빠른 소형 벌레 | 5 | 4 | 3 | 0.5 | 0.8 | 0.7 | 1 |
| 3 | Spider | 원거리 공격 벌레 | 15 | 1.5 | 8 | 2 | 3 | 1.2 | 3 |

---

## 2. WaveData 시트

각 웨이브의 기본 설정을 정의합니다.

### 컬럼 구조

| 컬럼명 | 타입 | 필수 | 설명 | 예시 |
|--------|------|------|------|------|
| WaveNumber | 정수 | O | 웨이브 번호 | 1, 2, 3 |
| WaveName | 문자열 | | 웨이브 이름 | Wave 1, 보스 웨이브 |
| WaveDuration | 실수 | O | 웨이브 지속 시간 (초) | 60, 90 |
| DelayBeforeNextWave | 실수 | | 다음 웨이브까지 대기 (초) | 3, 5 |
| HealthMultiplier | 실수 | | 체력 배율 | 1.0, 1.2 |
| DamageMultiplier | 실수 | | 공격력 배율 | 1.0, 1.1 |
| SpeedMultiplier | 실수 | | 속도 배율 | 1.0, 1.05 |

### 예시 데이터

| WaveNumber | WaveName | WaveDuration | DelayBeforeNextWave | HealthMultiplier | DamageMultiplier | SpeedMultiplier |
|------------|----------|--------------|---------------------|------------------|------------------|-----------------|
| 1 | 시작 | 60 | 3 | 1.0 | 1.0 | 1.0 |
| 2 | 증가 | 60 | 3 | 1.2 | 1.1 | 1.0 |
| 3 | 러시 | 45 | 5 | 1.0 | 1.0 | 1.3 |

---

## 3. WaveSpawnGroups 시트

각 웨이브에서 어떤 벌레를 몇 마리 스폰할지 정의합니다. 한 웨이브에 여러 스폰 그룹을 가질 수 있습니다.

### 컬럼 구조

| 컬럼명 | 타입 | 필수 | 설명 | 예시 |
|--------|------|------|------|------|
| WaveNumber | 정수 | O | 소속 웨이브 번호 | 1, 2 |
| BugId | 정수 | O | 스폰할 벌레 ID (BugData 참조) | 1, 2 |
| Count | 정수 | O | 스폰 수량 | 5, 10 |
| StartDelay | 실수 | | 웨이브 시작 후 스폰 시작까지 대기 (초) | 0, 5 |
| SpawnInterval | 실수 | O | 스폰 간격 (초) | 1.0, 0.5 |
| RandomPosition | 불리언 | | 랜덤 위치 스폰 여부 | TRUE, FALSE |

### 예시 데이터

| WaveNumber | BugId | Count | StartDelay | SpawnInterval | RandomPosition |
|------------|-------|-------|------------|---------------|----------------|
| 1 | 1 | 5 | 0 | 2 | TRUE |
| 1 | 2 | 3 | 10 | 1 | TRUE |
| 2 | 1 | 8 | 0 | 1.5 | TRUE |
| 2 | 2 | 5 | 5 | 1 | TRUE |
| 2 | 3 | 2 | 20 | 3 | TRUE |

> **참고**: WaveNumber가 같은 행들은 같은 웨이브에서 동시에/순차적으로 스폰됩니다.

---

## 4. MachineData 시트

채굴 머신의 스탯을 정의합니다.

### 컬럼 구조

| 컬럼명 | 타입 | 필수 | 설명 | 예시 |
|--------|------|------|------|------|
| MachineId | 정수 | O | 고유 ID | 1 |
| MachineName | 문자열 | O | 머신 이름 | Default, Advanced |
| Description | 문자열 | | 설명 | 기본 채굴 머신 |
| MaxHealth | 실수 | O | 최대 체력 | 100, 150 |
| HealthRegen | 실수 | | 초당 체력 회복 | 0, 1 |
| Armor | 실수 | | 방어력 (데미지 감소) | 0, 5 |
| MaxFuel | 실수 | O | 최대 연료 | 60, 90 |
| FuelConsumeRate | 실수 | O | 초당 연료 소모량 | 1.0, 0.8 |
| MiningRate | 실수 | O | 초당 채굴량 | 10, 15 |
| MiningBonus | 실수 | | 채굴 보너스 (%) | 0, 10 |
| AttackDamage | 실수 | O | 공격력 | 20, 30 |
| AttackCooldown | 실수 | O | 공격 쿨다운 (초) | 0.5, 0.3 |
| AttackRange | 실수 | O | 공격 사거리 | 3, 4 |
| CritChance | 실수 | | 치명타 확률 (0~1) | 0, 0.1 |
| CritMultiplier | 실수 | | 치명타 배율 | 1.5, 2.0 |

### 예시 데이터

| MachineId | MachineName | MaxHealth | MaxFuel | FuelConsumeRate | MiningRate | AttackDamage | AttackCooldown | AttackRange |
|-----------|-------------|-----------|---------|-----------------|------------|--------------|----------------|-------------|
| 1 | Default | 100 | 60 | 1 | 10 | 20 | 0.5 | 3 |

---

## 5. UpgradeData 시트

업그레이드 항목을 정의합니다.

### 컬럼 구조

| 컬럼명 | 타입 | 필수 | 설명 | 예시 |
|--------|------|------|------|------|
| UpgradeId | 문자열 | O | 고유 ID | max_health, attack_damage |
| DisplayName | 문자열 | O | 표시 이름 | 최대 체력, 공격력 |
| Description | 문자열 | | 설명 | 머신의 최대 체력 증가 |
| UpgradeType | 문자열 | O | 업그레이드 타입 | MaxHealth, AttackDamage |
| MaxLevel | 정수 | O | 최대 레벨 | 10, 20 |
| BaseValue | 실수 | | 기본 값 | 0 |
| ValuePerLevel | 실수 | O | 레벨당 증가 값 | 10, 5 |
| IsPercentage | 불리언 | | 퍼센트 적용 여부 | FALSE, TRUE |
| BaseCost | 정수 | O | 기본 비용 | 100 |
| CostMultiplier | 실수 | | 레벨당 비용 배율 | 1.5, 1.3 |

### UpgradeType 값

| 타입 | 설명 |
|------|------|
| MaxHealth | 최대 체력 |
| HealthRegen | 체력 재생 |
| Armor | 방어력 |
| MaxFuel | 최대 연료 |
| FuelEfficiency | 연료 효율 |
| MiningRate | 채굴 속도 |
| MiningBonus | 채굴 보너스 |
| AttackDamage | 공격력 |
| AttackSpeed | 공격 속도 |
| AttackRange | 공격 사거리 |
| CritChance | 치명타 확률 |
| CritDamage | 치명타 데미지 |

### 예시 데이터

| UpgradeId | DisplayName | UpgradeType | MaxLevel | ValuePerLevel | IsPercentage | BaseCost | CostMultiplier |
|-----------|-------------|-------------|----------|---------------|--------------|----------|----------------|
| max_health | 최대 체력 | MaxHealth | 10 | 10 | FALSE | 100 | 1.5 |
| attack_damage | 공격력 | AttackDamage | 10 | 5 | FALSE | 100 | 1.5 |
| crit_chance | 치명타 확률 | CritChance | 5 | 0.05 | FALSE | 200 | 1.8 |

---

## Unity에서 Import 하기

### 1단계: Google Sheets Importer 열기

메뉴에서 `Tools > Drill-Corp > Google Sheets Importer` 선택

### 2단계: 인증 확인

- 창이 열리면 자동으로 인증됩니다
- "인증됨" 표시가 나타나는지 확인

### 3단계: 데이터 미리보기 (선택)

- `Load Preview` 버튼 클릭
- 각 시트 탭을 선택하여 데이터 확인
- 문제가 있으면 Google Sheets에서 수정

### 4단계: Import 실행

- `Import All Data`: 모든 시트 한번에 Import
- 개별 버튼: 특정 시트만 Import (BugData, WaveData 등)

### 5단계: 결과 확인

Import된 ScriptableObject 위치:
- `Assets/_Game/Data/Bugs/` - Bug_[이름].asset
- `Assets/_Game/Data/Waves/` - Wave_[번호].asset
- `Assets/_Game/Data/Machines/` - Machine_[이름].asset
- `Assets/_Game/Data/Upgrades/` - Upgrade_[ID].asset

---

## 주의사항

### 반드시 지켜야 할 규칙

1. **시트 이름 변경 금지** - 시트 탭 이름은 정확히 일치해야 함
2. **헤더 행 유지** - 첫 번째 행은 컬럼 이름 (삭제/수정 금지)
3. **컬럼 이름 정확히 입력** - 대소문자 구분함
4. **빈 행 주의** - 데이터 중간에 빈 행이 있으면 그 이후 데이터가 무시될 수 있음

### 데이터 타입별 입력 방법

| 타입 | 입력 방법 | 예시 |
|------|-----------|------|
| 정수 | 숫자만 | 1, 10, 100 |
| 실수 | 소수점 사용 가능 | 1.5, 0.8, 10.0 |
| 문자열 | 텍스트 그대로 | Beetle, 기본 벌레 |
| 불리언 | TRUE 또는 FALSE | TRUE, FALSE |

### 문제 해결

| 증상 | 원인 | 해결 방법 |
|------|------|-----------|
| 인증 실패 | credentials 파일 없음 | 프로그래머에게 문의 |
| 데이터가 안 나옴 | 시트 이름 불일치 | 시트 탭 이름 확인 |
| 일부 컬럼 누락 | 컬럼 이름 오타 | 컬럼 이름 대소문자 확인 |
| Import 후 값이 0 | 숫자 형식 오류 | 셀 서식이 숫자인지 확인 |

---

## 변경 이력

| 날짜 | 변경 내용 |
|------|-----------|
| 2024-01-XX | 최초 작성 |

# Drill-Corp 기획 문서 가이드

Google Sheets 작업자를 위한 문서 안내입니다.

---

## 문서 읽는 순서

### 1단계: 전체 구조 파악

**[Data structure.md](Data%20structure.md)**

![데이터 계층 구조](image/데이터%20계층%20구조.png)

- Wave → Bug → BugBehavior 계층 구조
- 각 데이터 타입별 필드 정의
- 밸런스 가이드라인

---

### 2단계: 행동 시스템 이해

**[BugBehaviorSystemAnalysis.md](BugBehaviorSystemAnalysis.md)**

행동 타입별 상세 파라미터:

| 카테고리 | 타입 예시 | 파라미터 |
|----------|----------|----------|
| Movement | Linear, Hover, Orbit | param1, param2 |
| Attack | Melee, Projectile, Cleave | range, param1 |
| Passive | Armor, Shield, Regen | param1, param2 |
| Skill | Nova, Spawn, BuffAlly | cooldown, param1, range |
| Trigger | Enrage, ExplodeOnDeath | param1, param2 |

---

### 3단계: 시트 작성법

**[GoogleSheetsGuide.md](GoogleSheetsGuide.md)**

- BugData 시트 컬럼 구조
- 행동 문자열 작성 형식
- 실제 예시 데이터 8종

```
예시:
- Passives: "Armor:5" 또는 "Shield:20:2, Dodge:30"
- Triggers: "Enrage:30:50" 또는 "ExplodeOnDeath:15:3"
- Skills: "Nova:5:10:3" 또는 "Spawn:8:Beetle:2"
```

---

### 4단계: Import 동작 이해 (선택)

**[BugBehaviorImportGuide.md](BugBehaviorImportGuide.md)**

![Import 흐름도](image/Import%20흐름도.png)

- Google Sheets → Unity SO 변환 과정
- 기존 SO 재사용 규칙
- 파일 생성 위치

---

## 빠른 참조

### Movement 타입

| Type | 동작 |
|------|------|
| Linear | 직진 |
| Hover | 부유 + 접근 |
| Burst | 대기 → 돌진 |
| Ranged | 사거리 유지 |
| Orbit | 타겟 주위 공전 |

### Attack 타입

| Type | 동작 |
|------|------|
| Melee | 근접 즉발 |
| Projectile | 투사체 발사 |
| Cleave | 부채꼴 범위 |
| Spread | 다발 발사 |
| Beam | 지속 레이저 |

### Passive 문자열

```
Armor:5           → 데미지 5 감소
Dodge:30          → 30% 회피
Shield:20:2       → 보호막 20, 초당 2 재생
Regen:3           → 초당 3 회복
PoisonAttack:3:5  → 3초간 초당 5 독 데미지
```

### Trigger 문자열

```
Enrage:30:50        → HP 30% 이하 시 50% 강화
ExplodeOnDeath:10:2 → 사망 시 데미지 10, 반경 2 폭발
```

### Skill 문자열

```
Nova:5:10:3       → 쿨다운 5초, 데미지 10, 범위 3
BuffAlly:10:50:4  → 쿨다운 10초, 50% 버프, 범위 4
Spawn:8:Beetle:2  → 쿨다운 8초, Beetle 2마리 소환
```

---

## 문서 목록

| 문서 | 설명 |
|------|------|
| [Data structure.md](Data%20structure.md) | 전체 데이터 구조 |
| [BugBehaviorSystemAnalysis.md](BugBehaviorSystemAnalysis.md) | 행동 시스템 상세 |
| [GoogleSheetsGuide.md](GoogleSheetsGuide.md) | 시트 작성 가이드 |
| [BugBehaviorImportGuide.md](BugBehaviorImportGuide.md) | Import 과정 설명 |
| [BugBehaviorDevelopmentPlan.md](BugBehaviorDevelopmentPlan.md) | 개발 계획 (개발자용) |

---

## 다이어그램 원본

draw.io 편집용 파일: [diagrams/BugBehaviorSystem.drawio](diagrams/BugBehaviorSystem.drawio)

---

*최종 갱신: 2026-04-06*

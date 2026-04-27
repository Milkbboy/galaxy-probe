# 버그 스폰 시스템 — 프로토타입 분석

> 출처: `docs/_.html` (굴착기 디펜스 웹 프로토타입)
> 목적: Unity Drill-Corp 프로젝트에 이식하기 위한 사양 정리

---

## 1. 개요

프로토타입에는 **4종의 벌레 스폰 경로**가 존재한다.

| # | 종류 | 트리거 | 함수 |
|---|---|---|---|
| 1 | 일반 벌레 (Normal) | 스폰 타이머 | `spawnBug()` |
| 2 | 엘리트 (Elite, 황금) | 15초 주기 타이머 | `spawnElite()` |
| 3 | 스위프트 (Swift, 땅굴) | 땅굴 이벤트에서 12틱마다 1마리 | `spawnSwiftFromTunnel()` |
| 4 | 땅굴 이벤트 자체 | 게임 30초 경과 후 15초마다 | `startTunnelEvent()` |

---

## 2. 공통 스폰 좌표 규칙

### 2-1. 일반/엘리트: 화면 밖 원형 둘레
```
a = random(0, 2π)
r = sqrt(CX² + CY²) + margin   // 화면 대각선 반경 + 여유
spawnPos = (CX + cos(a) * r, CY + sin(a) * r)
```
- 화면 중심(`CX, CY`)에서 **화면 밖으로 margin만큼 떨어진 원** 위의 랜덤 각도.
- `margin`: 일반 = 24px, 엘리트 = 30px.
- **Unity 매핑**: 카메라 직교 뷰에서 `orthographicSize` 기반으로 화면 대각선 거리 계산. 좌표계는 **XZ 평면** (Y=0 고정).

### 2-2. 땅굴(Tunnel): 화면 내부 4변 중 한 지점
```
side = random(0..3)  // 0=상, 1=우, 2=하, 3=좌
if side=0: (random(0, W), 0)
if side=1: (W, random(0, H))
if side=2: (random(0, W), H)
if side=3: (0, random(0, H))
clampedX = clamp(x, 20, W-20)
clampedY = clamp(y, 20, H-20)
```
- 화면 가장자리에서 20px 안쪽으로 클램프된 지점에 **땅굴 마커 생성**.
- 이후 땅굴에서 스위프트가 주변 ±8px 랜덤 오프셋으로 출현.

---

## 3. 스폰 파라미터 상세

### 3-1. 일반 벌레 `spawnBug()` (L250)
| 속성 | 값 |
|---|---|
| 최대 동시 수 | `maxBugs` (UI 슬라이더, 기본 90) |
| 기본 속도 | `0.5 + wave * 0.06 + random(0, 0.15)` |
| 최대 HP | `2 + floor(wave * 0.5)` |
| 크기 | `9 + random(0, 4)` |
| 타입 | 30% 확률로 `type=1`(붉은 벌레), 나머지 `type=0`(초록) |
| 점수 | 1 |

**스폰 간격**: `spawnInterval` (UI 슬라이더, 기본 5프레임)

### 3-2. 엘리트 `spawnElite()` (L251)
| 속성 | 일반 대비 |
|---|---|
| 기본 속도 | `0.35 + wave * 0.04` (약간 느림) |
| HP | 일반의 **5배** |
| 크기 | 일반의 **2배** |
| 타입 | `type=2`, `elite=true` |
| 점수 | 5 |

**스폰 주기**: `ELITE_INTERVAL = 900` 프레임 (= 15초 @ 60fps)

### 3-3. 스위프트 `spawnSwiftFromTunnel(tx, ty)` (L261)
| 속성 | 특징 |
|---|---|
| 속도 | 일반의 **6배** (`SWIFT_SPEED_MULT = 6`) |
| HP | 일반의 **0.5배** (최소 1) |
| 크기 | 일반의 **0.5배** |
| 타입 | `type=3`, `swift=true` |
| 점수 | 0.5 |
| 스폰 위치 | 땅굴 지점 ± 8px 랜덤 |

---

## 4. 이벤트 타이밍 로직

### 4-1. 전역 타이머 (L191-194)
```js
gameTime         // 누적 프레임, init 시 0
eliteTimer       // 0 → ELITE_INTERVAL → 0 → ...
eventTimer       // 땅굴 이벤트 타이머
GAME_TIME_START = 1800    // 30초 (이후 땅굴 시작)
EVENT_INTERVAL  = 900     // 15초 주기
ELITE_INTERVAL  = 900     // 15초 주기
TUNNEL_SPAWN_INTERVAL = 12  // 땅굴에서 0.2초마다 1마리
```

### 4-2. 업데이트 루프 (L283-289)
```
update(dt):
  gameTime += dt

  eliteTimer -= dt
  if eliteTimer <= 0:
    spawnElite()
    eliteTimer = ELITE_INTERVAL

  if gameTime >= GAME_TIME_START:
    eventTimer -= dt
    if eventTimer <= 0:
      startTunnelEvent()
      eventTimer = EVENT_INTERVAL

  // 땅굴 큐 진행
  for tq in tunnelQueue:
    tq.tickTimer -= dt
    if tq.tickTimer <= 0:
      spawnSwiftFromTunnel(tq.x, tq.y)
      tq.remaining--
      tq.tickTimer = TUNNEL_SPAWN_INTERVAL
      if tq.remaining <= 0:
        remove tq

  spawnTimer -= dt
  if spawnTimer <= 0:
    spawnBug()
    spawnTimer = spawnInterval
```

---

## 5. 땅굴 이벤트 구조 (L252-260)

### 5-1. 데이터
```js
tunnelQueue = [
  { x, y, remaining: 10, tickTimer: 0 }
]
warnings = [
  { text: '◈ 땅굴 침공 !', subtext: '극속 하얀 벌레 10마리',
    life: 150, maxLife: 150, tx, ty }
]
```

### 5-2. 연출
1. 땅굴 지점에 **파티클 12개 방사형 분출** (보라빛, 30-50프레임)
2. **경고 배너** 상단 표시 (150프레임 = 2.5초)
3. 2.5초 경고 중에도 스위프트는 즉시 스폰 시작
4. 10마리 전부 스폰되면 `tunnelQueue`에서 제거

### 5-3. 사운드
`sndTunnelWarning()`: 1200Hz → 900Hz → 600Hz 하강 3연속 톤

---

## 6. Unity 이식 매핑

### 6-1. 기존 자산 현황
프로젝트에 이미 아래 시스템이 구축되어 있음:
- `BugSpawner.cs` — 기본 랜덤 원형 스폰
- `BugPool.cs` / `PooledBug.cs` — 오브젝트 풀링
- `FormationSpawner.cs` / `FormationData.cs` — 편대 스폰
- `BugData` (ScriptableObject) — 종류별 데이터
- `BugController` + Behaviors — AI/이동/공격 모듈화

### 6-2. 이식 전략

**프로토타입 구조 → Unity 컴포넌트 매핑**

| 프로토타입 | Unity 구현 방향 |
|---|---|
| `spawnBug()` | `BugSpawner` + `BugData(Normal)` |
| `spawnElite()` | `BugData(Elite)` — HP/속도/크기 배수는 SO 필드로 |
| 땅굴 이벤트 | 신규 `TunnelEventManager` |
| 스위프트 | `BugData(Swift)` + 땅굴 전용 스폰 경로 |
| 전역 타이머 | `WaveManager` 또는 `GameManager` 소관 |
| 경고 배너 | `UIManager`에 `ShowWarning(text, sub, duration)` |

### 6-3. 좌표계 주의사항

**프로토타입은 2D (x, y), Unity는 XZ 평면 탑다운**이므로 반드시 변환:

| 프로토타입 | Unity |
|---|---|
| `mouseX, mouseY` | `aimPos.x, aimPos.z` |
| 방향 계산 `atan2(dy, dx)` | `Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z))` |
| 거리 계산 `hypot(dx, dy)` | `Vector3.Distance(a, b)` (단, Y는 0으로 맞춰야 함) |
| "화면 위로" | `Vector3(0, 0, +1)` (Z축 양방향) |

### 6-4. 스폰 반경 계산 (Unity)

프로토타입은 `sqrt(CX² + CY²)` = 화면 대각선 반.
Unity 탑다운 직교 카메라에서:
```csharp
float halfH = Camera.main.orthographicSize;
float halfW = halfH * Camera.main.aspect;
float spawnRadius = Mathf.Sqrt(halfW * halfW + halfH * halfH) + margin;
```

---

## 7. 이식 단계 (권장 순서)

### Step 1 — 일반 벌레 기본 스폰
- [ ] 화면 밖 원형 랜덤 스폰 좌표 계산 (XZ 평면)
- [ ] `maxBugs` 제한 적용
- [ ] `spawnInterval` 기반 타이머 스폰
- [ ] 웨이브별 HP/속도 스케일링 공식 이식

### Step 2 — 엘리트 벌레
- [ ] `BugData_Elite` SO 작성 (배수 필드)
- [ ] 15초 주기 엘리트 스폰 타이머
- [ ] 시각적 구분 (황금색 머티리얼)

### Step 3 — 땅굴 이벤트 (선행: 웨이브/게임타임 시스템)
- [ ] `TunnelEventManager` 작성
- [ ] 화면 내부 4변 스폰 좌표 산출
- [ ] 경고 배너 UI (2.5초)
- [ ] 땅굴 VFX (파티클 12방향)
- [ ] 0.2초 간격으로 스위프트 10마리 스폰

### Step 4 — 스위프트 벌레
- [ ] `BugData_Swift` SO (속도×6, HP×0.5, 크기×0.5)
- [ ] 땅굴 전용 스폰 경로 연결

### Step 5 — 파라미터 외부화
- [ ] `SpawnConfig` SO로 상수(ELITE_INTERVAL, GAME_TIME_START 등) 빼기
- [ ] 인스펙터에서 튜닝 가능하도록

---

## 8. 프로토타입 상수 참조표

```
GAME_TIME_START        = 1800  (30초 @ 60fps)
EVENT_INTERVAL         = 900   (15초)
ELITE_INTERVAL         = 900   (15초)
TUNNEL_SPAWN_INTERVAL  = 12    (0.2초)
SWIFT_SPEED_MULT       = 6

기본 스폰 간격         = 5 프레임 (slider 기본값)
기본 최대 동시 수      = 90
일반 속도 계수         = 0.5 + wave*0.06 + rand(0, 0.15)
일반 HP                = 2 + floor(wave*0.5)
엘리트 HP 배수         = 5
엘리트 크기 배수       = 2
엘리트 점수            = 5
스위프트 점수          = 0.5
땅굴당 스위프트 수     = 10
```

---

## 9. 다음 단계

이 문서 확정 후:
1. `Step 1` 먼저 구현 — 기존 `BugSpawner.cs`와 병합 또는 교체 판단
2. `docs/Overview-Plan.md`에 이식 일정 반영
3. 무기/에임은 벌레 스폰이 안정된 후 진행

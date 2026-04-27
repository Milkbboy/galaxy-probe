# 거미 보스 시스템 (Spider Boss)

> 최종 갱신: 2026-04-27 — v2 거미 보스 풀 포팅 + 자연스러운 행동 사이클(점프→walk→idle)
> 근거 프로토타입: [V2-prototype.html](V2-prototype.html) line 715~880, 1462~1495

## 1. 개요

v2 원본의 거미 보스를 Unity 6 탑다운 좌표계로 포팅. 6각 perch 자리를 점프하며 머신을 압박하고 착지 시 새끼 거미를 소환한다. 처치 시 **즉시 게임 클리어** (mineTarget 미달이어도 승리).

### 등장 트리거

| 경로 | 조건 |
|---|---|
| **자동 등장** | 세션 누적 처치 점수 ≥ `_killThreshold` (기본 250) |
| **수동 등장** | TopBarHud 의 "🕷 보스 소환" 디버그 버튼 → `BossSpawnManager.ForceSpawn()` |

세션 1회만 등장 (`_spawned` 플래그). v2 원본 `BOSS_KILL_THRESHOLD=700`을 우리 시트 규모(웨이브 KillTarget 합계 ~140)에 맞춰 250으로 조정.

---

## 2. 행동 사이클

```
[등장] → Idle (머신 응시·정지)
            ↓
        Jumping (0.67초 포물선 점프, 다른 perch 로)
            ↓
        착지 (perch + jitter) → 새끼 거미 3마리 소환
            ↓
        Walking (perch 주변 어슬렁 1.5초)
            ↓
        Idle (2초 정지)
            ↓
        다시 Jumping ...
```

### 상태 머신 — `BossState`

| 상태 | Animator Speed | 동작 |
|---|---|---|
| `Idle` | 0 | 머신 응시·정지, `_idleDuration` 후 Jumping |
| `Walking` | 1 | perch 중심 `_walkRadius` 안 랜덤 어슬렁, `_walkDuration` 후 Idle |
| `Jumping` | 1 | 포물선 보간 (XZ Lerp + Y sin), 0.67초, 착지 후 Walking |

### 6각 Perch

머신 중심 60° 간격 6개 자리, 반경 `PERCH_RADIUS=15` (v2 200px → Unity 15m).

```
       ●        각도: 60°/120°/180°/240°/300°/360°
   ●       ●
       🤖
   ●       ●
       ●
```

매 점프마다 **현재 perch 제외 랜덤** 다른 자리 선택. 도착 위치에 `_perchJitter` (기본 1.5m) 만큼 XZ 랜덤 오프셋 → 같은 perch라도 매번 다른 자리에 떨어짐.

### 점프 물리

- **Duration**: 0.67초 (v2 jumpT += 0.025 → 40 frame @ 60fps)
- **포물선 높이**: `JUMP_HEIGHT = 3.6f` (v2 sin × 90px → Unity 3.6m)
- 탑다운 좌표계 — Y(높이)축으로 솟구침, XZ 평면 보간

```csharp
Vector3 flat = Vector3.Lerp(_jumpFrom, _jumpTo, _jumpT);
float hop = Mathf.Sin(_jumpT * Mathf.PI) * JUMP_HEIGHT;
transform.position = new Vector3(flat.x, _jumpFrom.y + hop, flat.z);
```

---

## 3. 스탯 / 데이터

### Boss 본체

| 필드 | 기본값 | v2 원본 | 의미 |
|---|---|---|---|
| `_maxHp` | 500 | BOSS_HP_BASE=500 | 최대 HP |
| `_contactDamagePerSecond` | 30 | bug.atk*60 | 머신 접촉 시 초당 피해 |
| `_contactRange` | 1.2 | sz+8 | 접촉 피해 반경 |

> 점프 중엔 공중이라 접촉 피해 X (`UpdateContactDamage` IsJumping 체크).

### Movement 튜닝 (인스펙터)

| 필드 | 기본값 | 효과 |
|---|---|---|
| `_walkDuration` | 1.5초 | 0이면 walk 생략 (즉시 Idle) |
| `_walkRadius` | 2.5m | perch 중심 walk 가능 반경 |
| `_walkSpeed` | 2.0 m/s | 어슬렁 속도 |
| `_idleDuration` | 2초 | walk 종료 후 정지 시간 |
| `_perchJitter` | 1.5m | 점프 도착 jitter 반경 |

### 새끼 거미 (Boss Child)

| 필드 | 값 |
|---|---|
| `_childBugData` | `SimpleBug_BossChild.asset` (Swift 베이스 + IsBossChild=true) |
| `_childCountPerLanding` | 3마리 |
| `_childSpawnJitter` | 1.5m (착지 위치 랜덤 오프셋) |

`SimpleBugData.IsBossChild=true` 인 벌레는 **OnBugDied 이벤트 발행 자체를 스킵** → GemDropSpawner가 분기 진입 못 해 보석 드랍 0%. v2 원본 `dropChance bossChild?0:...` 동등 동작.

---

## 4. UI / 시각 효과

### HP 3D 바

머리 위 3D 바 (`Hp3DBar` 재활용). 보라(가득) → 빨강(낮음) 그라데이션, 25% 임계.
보스 본체와 함께 페이드. 처치 시 즉시 Destroy.

### Hit / Death VFX

| VFX | 위치 | 트리거 |
|---|---|---|
| Hit | `_fxSocket` (자식 Transform) | TakeDamage 시 (0.1초 throttle 로 연사 부하 방지) |
| Death | `_fxSocket` | HP 0 도달 시 1회 |

`_fxSocket` 패턴이 핵심 — VFX 가 거미 자식 Transform 위치에 그대로 따라가, 거미 회전/점프와 무관하게 항상 정확한 위치에 뜸. Polygon Arsenal 보라 계열 권장 (`SwordImpactPurple` / `RocketExplosionStandard` / `MagicExplosionPurple` / `DarkExplosion`).

VFX 본체는 거미 자식이 아니라 `VfxPool` (`_poolRoot`) 자식 — 거미 회전 영향 X, world 고정.

### 보스 등장 경고 UI

화면 중앙 페이드 인/아웃 패널 (`BossWarningUI`):
- 검보라 반투명 박스 + 보라 테두리
- "보스 등장!" (보라 굵은 64pt) + "거미 보스 — 착지 시 새끼 소환" (흰색 28pt)
- `GameEvents.OnBossSpawned` 구독 → 0.3s fade in / 2.5s hold / 0.5s fade out
- D2Coding 폰트 (한글 글리프 보장) — D2CodingBold 는 한글 빠져있어 fontStyle=Bold 로 처리
- 자동 셋업: `Drill-Corp/HUD/Build Boss Warning` 메뉴

### TopBarHud 보스 소환 버튼

상단 HUD 우측 "나가기" 옆에 디버그 버튼. `BossSpawnManager.ForceSpawn()` 호출.

---

## 5. 클리어 분기 (D-2)

`SpiderBoss.TakeDamage` 에서 HP 0 도달 시:

```csharp
GameEvents.OnBossKilled?.Invoke();
Destroy(gameObject, 4f);   // Death 클립 4초 재생 후
```

`MachineController` 가 `OnBossKilled` 구독:

```csharp
private void OnBossKilled() => _bossKilled = true;

// CheckSessionEnd 안에서
if (IsMiningTargetReached || _bossKilled) {
    // 승리 정산 (전액 적립)
    GameManager.Instance?.SessionSuccess();
}
```

**v2 동등 동작**: mineTarget 미달이어도 보스 처치 = 즉시 승리. `_bossKilled` 플래그는 `InitializeSession` 에서 false 로 초기화.

---

## 6. 파일 구조

```
Assets/_Game/
├── Scripts/
│   ├── Boss/
│   │   ├── SpiderBoss.cs          본체 (상태 머신, 점프, walk, HP, VFX)
│   │   └── BossSpawnManager.cs    누적 점수 트리거 + 수동 ForceSpawn
│   ├── UI/HUD/
│   │   └── BossWarningUI.cs       페이드 경고 패널
│   ├── Editor/
│   │   └── BossWarningSetupEditor.cs   메뉴: Drill-Corp/HUD/Build Boss Warning
│   ├── Bug/Simple/
│   │   ├── SimpleBugData.cs       IsBossChild 필드 추가
│   │   └── SimpleBug.cs           IsBossChild → OnBugDied 발행 스킵
│   ├── Core/GameEvents.cs         OnBossSpawned / OnBossKilled
│   └── Machine/MachineController.cs   _bossKilled 분기
│
├── Prefabs/
│   ├── Boss/SpiderBoss.prefab            Animator + Avatar + Collider(Bug 레이어) + FxSocket
│   └── Bugs/Simple/SimpleBug_BossChild.prefab
│
├── Data/Bugs/
│   └── SimpleBug_BossChild.asset    Swift 베이스 + IsBossChild=true
│
├── Animations/
│   └── SpiderBoss.controller        Idle / Walk / Attack / Death (4 state)
│
├── Models/
│   ├── SpiderAnimation.fbx          외부 에셋 — 110본 + 3 take (Walk/Attack/Death)
│   ├── M_SpiderBoss.mat
│   └── Textures/SpiderBoss_Diffuse.png
```

---

## 7. v2 원본 차이점

| 항목 | v2 원본 | Unity 포팅 | 이유 |
|---|---|---|---|
| KillThreshold | 700 (60fps frame 단위) | 250 (KillTarget 합계 비례) | 시트 규모 차이 |
| 점프 perch jitter | 없음 (정확히 perch) | 1.5m 랜덤 | 자연스러움 ↑ |
| 착지 후 walk | 없음 (즉시 정지) | 1.5초 어슬렁 | 거미스러운 행동 |
| 보스 데미지 배율 | 무기마다 1.0~3.0× | 1.0× 일괄 | Unity 통합 우선, 밸런스 후속 |
| 새끼 거미 type | type:4 (전용) | SimpleBug + IsBossChild 플래그 | 시스템 재활용 |
| Death 모션 후 endGame | setTimeout(2000ms) | OnBossKilled 발행 즉시, 본체 4초 후 Destroy | Unity 이벤트 흐름 |

---

## 8. 디버그 / 튜닝

### Scene 뷰 Gizmo (보스 GameObject 선택 시)

- 🟣 진한 보라 점 6개 = 정확한 perch 자리
- 🟣 옅은 보라 원 = perch jitter 반경
- 🟠 옅은 주황 원 = walk 가능 반경
- 🟡 노란 점 + 원 = FxSocket 위치 (VFX 스폰 지점)

### 자주 조정하는 것

| 증상 | 조정 |
|---|---|
| 거미가 너무 부산 | `_walkDuration` ↓ 또는 `_walkSpeed` ↓ |
| 거미가 정적 | `_walkRadius` ↑ 또는 `_walkDuration` ↑ |
| 점프 빈도 ↑ | `_idleDuration` ↓ + `_walkDuration` ↓ |
| 매번 같은 자리 | `_perchJitter` ↑ |
| 너무 흩어짐 | `_perchJitter` ↓ + `_walkRadius` ↓ |
| 보스 너무 약함 | `_maxHp` ↑ (500 → 750) |

### 빠른 보스 등장 (테스트)

`BossSpawnManager._killThreshold` 를 5~10으로 낮추면 일반 벌레 몇 마리만 잡아도 등장. 또는 HUD "🕷 보스 소환" 버튼.

---

## 9. 관련 문서

- [V2-prototype.html](V2-prototype.html) — 원본 line 820~883 (보스 시스템), 715~717 (상수), 1462~1495 (drawBoss)
- [Sys-Weapon.md](Sys-Weapon.md) — 무기측 보스 데미지 배율 (TODO, 현재 1× 일괄)
- [Sys-Gem.md](Sys-Gem.md) — IsBossChild → 보석 드랍 0% 분기
- [Overview-Architecture.md](Overview-Architecture.md) — Boss 계층 추가 위치

# 보석 & 채굴 시스템

> 최종 갱신: 2026-04-20 (2) (보석 2종 v2 규격 + 세션 정산 정책 전환 — §11 참조)
> 근거 프로토타입: `docs/v2.html`
> 상위 문서: [V2-IntegrationPlan.md](V2-IntegrationPlan.md)

> ⚠️ §2~§7의 예시 코드는 **초안 설계**. 실제 구현 파일·차이점은 **§10 구현 현황**을 먼저 확인.

## 1. 개요

v2에서 도입된 세 가지 변화:

1. **이중 재화** — 광석(ore) + 보석(gem)으로 분리
2. **보석 드랍/채집** — 벌레 처치 시 확률 드랍, 마우스 호버 2초로 수집
3. **mineTarget 승리 조건** — "연료 소진"이 아닌 **"채굴량 도달"로 세션 성공**

### 재화 흐름

```
인게임
├─ 채굴 진행 ─────────┐
│   mineRate / sec     │
├─ 벌레 처치 ─────────┤→  sessionOre  (광석, 처치 score의 0.5배 + 채굴량의 0.5배)
│   (score × 0.5)      │
│                       │
└─ 벌레 드랍 보석 ─────→ sessionGems (호버 2초 채집)
    일반: 5% / 1개
    엘리트: 100% / 5개

세션 종료 (승리: 100% / 실패: 50%)
  → PlayerData.Ore += sessionOre
  → PlayerData.Gems += sessionGems

아웃게임
├─ 광석 사용: 굴착기 강화, 무기 강화(혼합)
└─ 보석 사용: 무기 해금, 어빌리티 해금, 보석 채집 스킬, 무기 강화(혼합)
```

---

## 2. 인게임 보석 드랍 시스템

### 2.1 드랍 확률

v2 `killBug()` 참조:

```
dropChance =
  bossChild ? 0 :
  elite     ? 1.0 :
  (0.05 + gemDropBonus)

gemDropBonus = gem_drop_level * 0.02  // 최대 5렙 → +10% (총 15%)
```

### 2.2 보석 스폰

```csharp
public class BugController : MonoBehaviour
{
    void OnDeath()
    {
        // 기존 burst, 광석 적립 …

        float dropChance = _data.IsBossChild ? 0f
                         : _data.IsElite     ? 1f
                         : 0.05f + UpgradeManager.GetBonus(UpgradeType.GemDropRate);

        if (Random.value < dropChance)
        {
            int value = _data.IsElite ? 5 : 1;
            GemDropSpawner.Instance.SpawnGem(transform.position, value);
        }
    }
}
```

### 2.3 GemDrop 컴포넌트

```csharp
public class GemDrop : MonoBehaviour
{
    public int Value = 1;                 // 1 (일반) / 5 (엘리트)
    public float Lifetime = 10f;          // 10초 후 사라짐 (v2: 600 frames)

    float _hoverTime;                     // 호버 누적
    float _lifeTimer;
    bool _collected;

    public float HoverProgress => _hoverTime / CollectDuration;

    float CollectDuration
    {
        // 기본 2초, gem_speed로 단축
        get
        {
            float mult = 1f + UpgradeManager.GetBonus(UpgradeType.GemCollectSpeed);
            return 2f / mult;
        }
    }

    void Update()
    {
        _lifeTimer += Time.deltaTime;
        if (_lifeTimer >= Lifetime) { Destroy(gameObject); return; }

        // 마우스 커서와의 거리 (XZ 평면)
        Vector3 aim = AimController.AimWorldPoint;
        float dist = Vector3.Distance(
            new Vector3(aim.x, 0, aim.z),
            new Vector3(transform.position.x, 0, transform.position.z));

        if (dist < CollectRadius)
        {
            _hoverTime += Time.deltaTime;
            if (_hoverTime >= CollectDuration && !_collected)
            {
                _collected = true;
                SessionState.AddGems(Value);
                SpawnPopup();
                Destroy(gameObject);
            }
        }
        else
        {
            _hoverTime = 0f;  // 멀어지면 리셋
        }
    }
}
```

### 2.4 좌표계 주의

> v2의 `Math.hypot(gem.x-mouseX, gem.y-mouseY)` → Unity에서는 **XZ 평면 거리**:
> ```csharp
> Vector3 delta = aimPoint - gem.transform.position;
> delta.y = 0;
> float dist = delta.magnitude;
> ```
> Y 성분을 제거하지 않으면 카메라 높이 때문에 거리가 왜곡됨.
> 참조: `CLAUDE.md` 좌표계 규칙.

### 2.5 보석 비주얼

- 프리펩: `Assets/_Game/Prefabs/GemDrop.prefab`
  - 마름모 스프라이트 (일반: 하늘색, 엘리트: 금색)
  - 회전 애니메이션 (`Time.time * 0.003` rad/frame ≈ 0.18 rad/sec)
  - 월드 UI 캔버스에 "+N" 라벨 (호버 중 채집 링)

**호버 채집 링** (월드 공간):
- 반경 `16` 유닛에서 `-π/2` → `-π/2 + 2π × HoverProgress` 호
- 골드 컬러, LineRenderer 또는 Canvas UI Image

### 2.6 채집기와 마우스 경합

v2에는 `gem.collectedBy` 필드가 있어 **채굴 드론이 점유한 보석은 마우스로 채집 불가**. 현재 적용할 필요는 없으나, 채굴드론(`jinus_mining_drone`) 구현 시 재도입 고려.

---

## 3. PlayerData 재화 분리

### 3.1 마이그레이션

기존 `PlayerData.Currency` 필드 그대로 두고, 로드 시점에 자동 변환:

```csharp
public class PlayerData
{
    // === 하위호환 ===
    [Obsolete("Use Ore")] public long Currency;

    // === v2 ===
    public long Ore;
    public long Gems;

    public void MigrateIfNeeded()
    {
        if (Currency > 0 && Ore == 0)
        {
            Ore = Currency;   // 기존 잔고를 광석으로 이전
            Currency = 0;
        }
    }
}
```

`DataManager.LoadData()` 직후 `_playerData.MigrateIfNeeded()` 호출.

### 3.2 SessionState (인게임 누적)

```csharp
public static class SessionState
{
    public static float SessionOre;    // float (채굴량 소수점 누적)
    public static int SessionGems;
    public static int SessionKills;

    public static void AddMining(float amount)
    {
        SessionOre += amount * 0.5f;   // v2: sessionOre += mineRate/60 × 0.5
        MineAmt += amount;
    }

    public static void AddKillReward(float score)
    {
        SessionOre += score * 0.5f;    // v2: sessionOre += score × 0.5
        SessionKills += Mathf.RoundToInt(score);
    }

    public static void AddGems(int value) => SessionGems += value;

    public static void Reset()
    {
        SessionOre = 0; SessionGems = 0; SessionKills = 0; MineAmt = 0;
    }
}
```

### 3.3 세션 종료 정산

```csharp
public void EndSession(bool isWin)
{
    int oreGain = Mathf.FloorToInt(SessionState.SessionOre * (isWin ? 1f : 0.5f));
    int gemGain = Mathf.FloorToInt(SessionState.SessionGems * (isWin ? 1f : 0.5f));

    DataManager.PlayerData.Ore += oreGain;
    DataManager.PlayerData.Gems += gemGain;
    DataManager.PlayerData.TotalSessionsPlayed++;
    DataManager.SaveData();

    GameEvents.OnSessionEnded?.Invoke(isWin, oreGain, gemGain);

    StartCoroutine(ReturnToTitleWithDelay(isWin ? 0.5f : 0.2f));
}
```

---

## 4. mineTarget 승리 조건

### 4.1 컨셉 변경

| 기존 | v2 |
|---|---|
| 연료 소진 = 세션 종료 | 채굴량 도달 = **승리** |
| MaxFuel로 세션 시간 조절 | mineTarget으로 목표치 조절 |
| 실패 조건: HP 0 | 실패 조건: HP 0 (동일) |

### 4.2 MachineData 필드 추가

```csharp
public class MachineData : ScriptableObject
{
    // === 기존 ===
    public float MaxHealth;
    public float MaxFuel;
    public float FuelConsumeRate;
    public float MiningRate;
    // …

    // === v2 ===
    public float BaseMiningTarget = 100f;  // 기본 목표량
}
```

> 연료(Fuel)는 **제거하지 않음**. 연료 = 세션 타임아웃으로 남기거나, 채굴드론 가동시간 등으로 재활용. 초안에서는 연료를 **무한**(소모 없음)으로 두고 mineTarget만 체크.

### 4.3 MiningTargetTracker

```csharp
public class MiningTargetTracker : MonoBehaviour
{
    [SerializeField] MachineData _machineData;

    float _current;
    float _target;
    float _miningRate;

    void Start()
    {
        var st = PlayerStats.Current();
        _target = _machineData.BaseMiningTarget +
                  UpgradeManager.GetBonus(UpgradeType.MiningTarget);  // +50/lv
        _miningRate = _machineData.MiningRate +
                      UpgradeManager.GetBonus(UpgradeType.MiningRate); // +2/lv

        SessionState.Reset();
        SessionState.MineTarget = _target;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        float delta = _miningRate * dt;

        _current += delta;
        SessionState.MineAmt = _current;
        SessionState.AddMining(delta);  // 광석도 동시 적립

        if (_current >= _target)
        {
            SessionManager.EndSession(isWin: true);
            enabled = false;
        }
    }
}
```

### 4.4 UI

기존 `MiningUI` 확장:

```
MiningUI
├── MineValueText    ("42 / 150")
├── MineProgressBar  (fill = current / target)
└── MineTargetText   (호버 시 목표량 설명)
```

월드 공간에도 표시 (v2 `drawMineProgress` 참조):
- 머신 주위 원형 링, 진행도만큼 호 채움
- 텍스트 `"42/150"` 머신 아래쪽에

---

## 5. 이중 재화 UI 갱신

### 5.1 TopBar (HubPanel 상단)

v2의 `og-header` 참조:

```
TopBar
├── Title          "굴착기 디펜스"
├── TargetLabel    "목표: 150 채굴 · 보석 드랍 7% (1.6초 수집)"
├── OreDisplay     "광석 [120]🪨"
├── GemDisplay     "보석 [8]💎"
├── CheatButton    "+100" (개발 빌드만)
├── ResetButton    "초기화" + 2단계 확인
└── StartButton    "▶ 채굴 시작"
```

### 5.2 재화 업데이트 이벤트

```csharp
public static class GameEvents
{
    public static event Action<long, long> OnCurrencyChanged;  // (ore, gems)
}

// PlayerData setter 또는 명시적 메서드에서 호출
public void AddCurrency(long ore, long gems)
{
    Ore += ore;
    Gems += gems;
    GameEvents.OnCurrencyChanged?.Invoke(Ore, Gems);
}
```

UI는 이벤트 구독하여 갱신. `renderOutgame()`처럼 전체 재렌더 불필요.

---

## 6. 결과 화면 (ResultOverlay)

Title 씬으로 복귀 시 세션 결과 표시 (v2 `resultPanel` 매핑):

```
ResultOverlay
├── TitleText       "⛏ 채굴 완료!" / "💥 채굴 실패"
├── SubText         "목표 채굴량 150을 달성했습니다!"
├── OreGainText     "+42 🪨"
├── GemGainText     "+3 💎"
├── UpgradeButton   → HubPanel로 이동
└── RetryButton     → Game 씬 즉시 재로드
```

### 6.1 Game → Title 전환 시 결과 전달

세션 종료 시 `PlayerData`에 `LastSessionResult` 캐시:

```csharp
[Serializable]
public class SessionResult
{
    public bool IsWin;
    public int OreGained;
    public int GemGained;
    public int KillCount;
    public long TimestampTicks;  // 표시 후 소비
}

public class PlayerData
{
    public SessionResult LastSessionResult;
    public bool HasUnconsumedResult =>
        LastSessionResult != null && LastSessionResult.TimestampTicks > 0;
}
```

Title 씬 `Start()`:

```csharp
if (DataManager.PlayerData.HasUnconsumedResult)
{
    ResultOverlay.Show(DataManager.PlayerData.LastSessionResult);
    DataManager.PlayerData.LastSessionResult = null;
    DataManager.SaveData();
}
```

---

## 7. 보석 채집 업그레이드 (UpgradeData 추가)

[Data-SheetsGuide.md §4](Data-SheetsGuide.md#4-upgradedata-시트) 참조.

### 7.1 UpgradeData 2개 추가

| UpgradeId | Type | MaxLv | ValuePerLevel | IsPercentage | BaseCost |
|---|---|---|---|---|---|
| `gem_drop` | GemDropRate | 5 | 0.02 | true | 15 Gems |
| `gem_speed` | GemCollectSpeed | 5 | 0.20 | true | 10 Gems |

### 7.2 UpgradeType enum 확장

```csharp
public enum UpgradeType
{
    // 기존 …
    MaxHealth, Armor, MiningRate, AttackDamage, AttackSpeed, FuelEfficiency,

    // v2 신규
    MiningTarget,      // 목표 채굴량 (+50/lv, Add)
    GemDropRate,       // 보석 드랍 확률 (+2%/lv, Add)
    GemCollectSpeed,   // 보석 채집 속도 (+20%/lv, Mul)
}
```

### 7.3 UpgradeData 비용에 Gem 필드 추가

```csharp
public class UpgradeData : ScriptableObject
{
    // 기존 BaseCost (광석) 유지
    public int BaseCostOre;       // ← BaseCost 리네임
    public int BaseCostGem;       // 신규
    public float CostMultiplier;

    public int GetCostOre(int level)
        => Mathf.RoundToInt(BaseCostOre * Mathf.Pow(CostMultiplier, level));
    public int GetCostGem(int level)
        => Mathf.RoundToInt(BaseCostGem * Mathf.Pow(CostMultiplier, level));
}
```

기존 에셋은 `BaseCostOre = 기존 BaseCost`, `BaseCostGem = 0`으로 자동 마이그레이션.

---

## 8. 튜닝 예시 (v2 기본값)

| 레벨 | `gem_drop` 확률 | `gem_speed` 채집 시간 | `mine_target` 목표 |
|---|---|---|---|
| 0 | 5% | 2.00초 | 100 |
| 1 | 7% | 1.67초 | 150 |
| 2 | 9% | 1.43초 | 200 |
| 3 | 11% | 1.25초 | 250 |
| 4 | 13% | 1.11초 | 300 |
| 5 | 15% | 1.00초 | 350 |

> 엘리트 드랍은 항상 100% / 5개 — 채집 압박을 높이는 장치.

---

## 9. 참고 문서

- [V2-IntegrationPlan.md](V2-IntegrationPlan.md) — 총론
- [Sys-Character.md](Sys-Character.md) — 채굴 드론(jinus_mining_drone)이 보석 드랍에 관여
- [Data-SheetsGuide.md §4](Data-SheetsGuide.md#4-upgradedata-시트) — UpgradeData 이중 재화 스키마
- `docs/v2.html` 926~944줄 — `killBug()` 드랍 로직
- `docs/v2.html` 1341~1371줄 — gem 호버 채집 로직
- `CLAUDE.md` 좌표계 섹션 — XZ 평면 거리 계산

---

## 10. 구현 현황 (2026-04-19)

### 드랍 & 채집 — ✅

| 파일 | 역할 |
|---|---|
| `Scripts/Pickup/Gem.cs` | 월드 보석 오브젝트. `Create(pos, sprite)` 팩토리로 프로그램 스폰 (프리펩 불필요). SpriteRenderer(`01_diamond.png`) + 자식 LineRenderer 진행 링. Update에서 `AimController.AimPosition`과 XZ 거리 체크 → 호버 2초(`gem_speed` 보정) → `Collect()`에서 `DataManager.AddGems(1)` + `OnGemCollected` 발사 + 팝업 + 자파괴 |
| `Scripts/Pickup/GemDropSpawner.cs` | 씬 싱글턴. `GameEvents.OnBugDied` 구독. 일반 5%+`gem_drop`, 엘리트 100%. 인스펙터 `_gemSprite` 필드(HUD 에디터가 `01_diamond` 자동 할당) |
| `Scripts/UI/GemCounterUI.cs` | HUD 보석 카운터. `OnGemCollected` 구독, 세션 누적. MiningUI의 펀치 애니 동일 |
| `Scripts/UI/SessionResultUI.cs` | 세션 종료 UI에 `"보석: N"` 병기 (성공/실패 모두) |
| `Scripts/Editor/InGameCurrencyHudSetupEditor.cs` | `Tools > Drill-Corp > 3. 게임 초기 설정 > 3. 광석·보석 HUD 추가` 메뉴. 우상단에 **[05_iron]**광석(MiningUI) + **[01_diamond]**보석(GemCounterUI) 2행 + `GemDropSpawner` GameObject 자동 생성/스프라이트 바인딩 |

### 이벤트 스키마

```csharp
// Scripts/Core/GameEvents.cs
public static Action<Vector3, bool> OnBugDied;   // (위치, 엘리트?) — 드랍 스포너 전용
public static Action<int> OnGemCollected;        // HUD 누적용, invoke당 1회 +amount
```

`BugController.Die()` (line 971)에서 `OnBugKilled`와 함께 `OnBugDied(transform.position, _bugData.IsElite)` 발사.

### BugData 확장

`_isElite` bool 필드 추가(기본 false). 필요한 SO만 `IsElite=true` 체크 (현재 자동 세팅 없음 — 향후 Beetle·Sword 등 특정 종을 엘리트로 지정 예정).

### 초안 설계와의 차이 (2026-04-19 기준 — §11에서 대부분 해소)

| 항목 | §2~§7 설계 | 실제 구현 | 이유 |
|---|---|---|---|
| 프리펩 | `Assets/_Game/Prefabs/GemDrop.prefab` | **프로그램 생성** (프리펩 없음) | 간단·빠른 반복. 필요시 프리펩화 가능 |
| 수명 | 10초 후 사라짐 | 무제한 (세션 종료 시 자동 파괴) | 단순화. 압박감 필요하면 `_lifetime` 추가 |
| 채집기 경합 | `gem.collectedBy` 드론 점유 필드 | 미적용 | 채굴드론 어빌리티 구현 시 재검토 |

---

## 11. 2026-04-20 (2) 업데이트 — v2 원본 규격 포팅

### 보석 2종 체계

| 타입 | 색상 | Value | 드랍 조건 | 드랍 확률 |
|---|---|---|---|---|
| 일반 | #aadfff 파랑 | +1 | 일반 벌레 | 5% + `gem_drop` 강화 (+2%/lv) |
| 엘리트 | #ffd700 금색 | +5 | 엘리트 벌레(`SimpleBugData.Kind == Elite`) | 100% 고정 |

구현:

```csharp
// Scripts/Pickup/GemDropSpawner.cs
private void HandleBugDied(Vector3 position, bool isElite)
{
    float chance = isElite ? _eliteDropChance : _baseDropChance + GetUpgradeBonus();
    if (Random.value > chance) return;
    if (isElite) Gem.Create(position, _gemSprite, EliteGemValue, EliteGemColor);
    else         Gem.Create(position, _gemSprite, NormalGemValue, NormalGemColor);
}
```

`Gem.Create(pos, sprite, value, tint)` 시그니처로 파라미터화. 진행 링 색상도 tint 기반(엘리트는 금색 링).

### 보석 크기 확대

- `SpriteSize 0.7 → 1.2` (1.7배). 사용자 피드백("너무 작아서 안 보임") 반영
- 링 반경 0.45 → 0.6, 두께 0.05 → 0.07 비례 확대

### 채집 정산 정책 전환 (v2 일치)

이전엔 "채집 즉시 `DataManager.AddGems` 호출"로 세션 실패해도 보석 유지. **2026-04-20 (2)부터는 v2 원본 규칙으로 전환**:

| 이벤트 | 처리 |
|---|---|
| `Gem.Collect()` | `GameEvents.OnGemCollected(value)` 발행만. `DataManager.AddGems` 호출 **없음** |
| `MachineController.OnGemCollected` | `_sessionGemsCollected += amount` + `OnSessionGemsChanged` 발행 (HUD 갱신용) |
| 세션 승리 | `DataManager.AddGems(sessionGems)` 전액 |
| 세션 패배 | `DataManager.AddGems(sessionGems × 0.5)` 50% |
| 나가기 | 정산 없이 Title 복귀 (보상 0) |

### 관련 파일

| 파일 | 변경 |
|---|---|
| `Scripts/Pickup/Gem.cs` | `_value`, `_tint` 필드 + `Create` 파라미터화. 크기 확대. 링 색상 tint 기반. `AddGems` 호출 제거 |
| `Scripts/Pickup/GemDropSpawner.cs` | 엘리트/일반 분기에서 `value`+`tint` 지정 |
| `Scripts/Machine/MachineController.cs` | `_sessionGemsCollected` + `OnSessionGemsChanged` 발행. `CheckSessionEnd`에서 정산 |
| `Scripts/UI/HUD/TopBarHud.cs` | `OnSessionGemsChanged` 구독, 세션 누적 표시 |
| `Scripts/UI/SessionResultUI.cs` | 승리=전액, 패배=50% 보상 표시 |

### 남은 작업

- 엘리트 벌레 SimpleBugData 이미 존재 (`SimpleBug_Elit.asset`) — 드랍 분기는 `_data.Kind == Elite`로 동작 중
- `ResultOverlay` Title 복귀 UI 분리 — 현재 Game 씬 ResultPanel이 승리/패배 모두 처리하며 Title 복귀는 버튼 클릭으로만


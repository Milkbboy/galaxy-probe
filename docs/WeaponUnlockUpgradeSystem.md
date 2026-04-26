# 무기 해금 & 강화 시스템

> 최종 갱신: 2026-04-20 (2) (저격총·레이저 Range 업그레이드 v2 포팅 완료, §12 참조)
> 근거 프로토타입: `docs/v2.html`
> 상위 문서: [V2_IntegrationPlan.md](V2_IntegrationPlan.md)
> 관련 문서: [WeaponSystem.md](WeaponSystem.md) (기존 무기 아키텍처)

> ⚠️ §3~§8의 코드 예시는 **초안 설계**. 실제 구현 매핑·차이점은 **§11 인게임 적용 현황**부터 확인.

## 1. 개요

v2에서 도입된 두 개념:

1. **무기 해금 체인** — 5종 중 저격총만 기본, 나머지 4개는 **보석으로 순차 해금**
2. **무기별 강화 3종** — 각 무기마다 3개 강화 항목 (광석+보석 혼합)

그리고 **회전톱날(`saw`)**이 신규 무기로 추가.

### 무기 5종

| ID | 이름 | 해금 비용 | req | 발사 |
|---|---|---|---|---|
| `sniper` | 저격총 | 기본 | — | 범위 내 자동 저격 |
| `bomb` | 폭탄 | 30💎 | — | **마우스 클릭 수동** |
| `gun` | 기관총 | 20💎 | `bomb` | 자동 연사 + 탄창 |
| `laser` | 레이저 | 40💎 | `gun` | 자동 주기 빔 |
| `saw` | 회전톱날 | 40💎 | `laser` | 마우스 따라 자동 회전 |

> **현재 프로젝트에는 `LockOn` / `Shotgun` / `BurstGun`이 있지만 v2에는 없음.** 이들은 보류(삭제하지 않고 Disabled) 또는 캐릭터 특수 무기로 전환 검토. 초안에는 5종만 활성화.

---

## 2. 파일 구조 변경

```
Assets/_Game/Scripts/Weapon/
├── WeaponData.cs             # (확장) 해금 메타 필드 추가
├── WeaponBase.cs             # (유지) 강화 보너스 조회만 추가
├── WeaponUpgradeData.cs      # (신규) 강화 항목 SO
├── WeaponUpgradeManager.cs   # (신규) 무기별 강화 레벨 저장·보너스 계산
│
├── Sniper/   …  (신규 또는 기존 LockOnData에서 변환)
├── Bomb/     …
├── Gun/      …  (기존 BurstGun 재활용)
├── Laser/    …  (기존 LaserBeam 재활용)
└── Saw/                      # 신규
    ├── SawWeaponData.cs
    └── SawWeapon.cs
```

---

## 3. 무기 해금

### 3.1 WeaponData 확장

```csharp
public abstract class WeaponData : ScriptableObject
{
    // === 기존 ===
    public string DisplayName;
    public float FireDelay;
    public float Damage;
    public float Range;
    // …

    // === 신규 (해금 메타) ===
    public string WeaponId;                 // "sniper" / "bomb" / …
    public bool UnlockedByDefault;          // true = sniper만
    public int UnlockGemCost = 0;           // 기본 해금은 0
    public WeaponData RequiredWeapon;       // req 체인
}
```

### 3.2 PlayerData 해금 상태

[CharacterAbilitySystem.md §2](CharacterAbilitySystem.md#playerdata-확장)에 명시된 대로:

```csharp
public HashSet<string> UnlockedWeapons = new() { "sniper" };  // 초기값
```

### 3.3 해금 로직 (WeaponUpgradeManager)

```csharp
public bool TryUnlockWeapon(WeaponData data)
{
    if (data.UnlockedByDefault) return false;
    if (PlayerData.UnlockedWeapons.Contains(data.WeaponId)) return false;

    if (data.RequiredWeapon != null &&
        !PlayerData.UnlockedWeapons.Contains(data.RequiredWeapon.WeaponId))
        return false;

    if (PlayerData.Gems < data.UnlockGemCost) return false;

    PlayerData.Gems -= data.UnlockGemCost;
    PlayerData.UnlockedWeapons.Add(data.WeaponId);
    DataManager.SaveData();
    GameEvents.OnWeaponUnlocked?.Invoke(data);
    return true;
}
```

### 3.4 게임 씬 적용

`WeaponSwitcher` 또는 Game 씬의 무기 컨테이너에서:

```csharp
foreach (var weapon in _allWeapons)
{
    bool unlocked = PlayerData.UnlockedWeapons.Contains(weapon.Data.WeaponId);
    weapon.gameObject.SetActive(unlocked);
}
```

슬롯 UI에서도 해금되지 않은 무기는 자물쇠 아이콘으로 표시.

---

## 4. 무기별 강화

### 4.1 강화 항목 (총 15개)

| WeaponId | UpgradeId | 스탯 | MaxLv | 레벨당 | 방향 |
|---|---|---|---|---|---|
| sniper | sniper_dmg | Damage | 5 | +25% | Mul |
| sniper | sniper_range | Range | 3 | +20% | Mul |
| sniper | sniper_cd | Cooldown | 4 | −20% | Mul(음수) |
| bomb | bomb_dmg | Damage | 4 | +30% | Mul |
| bomb | bomb_radius | Radius | 4 | +20% | Mul |
| bomb | bomb_cd | Cooldown | 4 | −15% | Mul(음수) |
| gun | gun_dmg | Damage | 5 | +25% | Mul |
| gun | gun_ammo | AmmoBonus | 4 | +10발 | Add |
| gun | gun_reload | ReloadTime | 4 | −20% | Mul(음수) |
| laser | laser_dmg | Damage | 5 | +25% | Mul |
| laser | laser_range | Range | 4 | +20% | Mul |
| laser | laser_cd | Cooldown | 4 | −15% | Mul(음수) |
| saw | saw_dmg | Damage | 5 | +20% | Mul |
| saw | saw_radius | Radius | 4 | +25% | Mul |
| saw | saw_slow | SlowBonus | 3 | +20%p | Add(최대 90%) |

### 4.2 WeaponUpgradeData.cs

```csharp
[CreateAssetMenu(menuName = "Drill-Corp/Weapon Upgrade")]
public class WeaponUpgradeData : ScriptableObject
{
    public string UpgradeId;                // "sniper_dmg"
    public string WeaponId;                 // "sniper"
    public string DisplayName;              // "저격총 데미지"
    public Sprite Icon;

    public WeaponUpgradeStat TargetStat;    // 아래 enum
    public int MaxLevel = 5;
    public float ValuePerLevel = 0.25f;     // 0.25 = +25%
    public bool IsPercentage = true;
    public WeaponUpgradeOp Operation = WeaponUpgradeOp.Multiply;

    [Header("비용 — 공식 방식")]
    public int BaseCostOre = 40;
    public int BaseCostGem = 2;
    public float OreCostMultiplier = 2f;
    public float GemCostMultiplier = 2f;

    [Header("비용 — 수동 방식 (선택)")]
    public List<CostTuple> ManualCosts;     // 있으면 공식 무시

    [Serializable] public struct CostTuple { public int Ore; public int Gem; }
}

public enum WeaponUpgradeStat
{
    Damage, Range, Cooldown, AmmoBonus, ReloadTime, Radius, SlowBonus,
}

public enum WeaponUpgradeOp { Add, Multiply }
```

> **비용 방식 선택**: v2는 레벨별 명시(`costs:[{o:40,g:2},{o:90,g:4},...]`)를 썼고 **비선형 증가**. Unity에서는 공식(`base * mult^lv`)이 단순하지만 v2 그대로 복원하려면 `ManualCosts`를 채우면 됨.
> → 초안은 **공식 방식**으로 채우고, 밸런스가 이상하면 `ManualCosts`로 덮어쓰기.

### 4.3 WeaponUpgradeManager

```csharp
public class WeaponUpgradeManager : MonoBehaviour
{
    [SerializeField] List<WeaponUpgradeData> _allUpgrades;
    Dictionary<string, int> _levels;  // UpgradeId → level

    public int GetLevel(string upgradeId)
        => _levels.TryGetValue(upgradeId, out var lv) ? lv : 0;

    public float GetBonus(string weaponId, WeaponUpgradeStat stat)
    {
        float add = 0f, mul = 1f;
        foreach (var u in _allUpgrades)
        {
            if (u.WeaponId != weaponId || u.TargetStat != stat) continue;
            int lv = GetLevel(u.UpgradeId);
            if (lv <= 0) continue;

            if (u.Operation == WeaponUpgradeOp.Add)
                add += u.ValuePerLevel * lv;
            else // Multiply
                mul *= 1f + u.ValuePerLevel * lv;
        }
        return (add, mul);  // 실제로는 (float add, float mul) 튜플 반환
    }

    public (int ore, int gem) GetCost(WeaponUpgradeData u)
    {
        int lv = GetLevel(u.UpgradeId);
        if (lv >= u.MaxLevel) return (0, 0);

        if (u.ManualCosts != null && lv < u.ManualCosts.Count)
            return (u.ManualCosts[lv].Ore, u.ManualCosts[lv].Gem);

        int ore = Mathf.RoundToInt(u.BaseCostOre * Mathf.Pow(u.OreCostMultiplier, lv));
        int gem = Mathf.RoundToInt(u.BaseCostGem * Mathf.Pow(u.GemCostMultiplier, lv));
        return (ore, gem);
    }

    public bool TryBuy(WeaponUpgradeData u)
    {
        var (ore, gem) = GetCost(u);
        if (ore == 0 && gem == 0) return false;  // maxed

        if (PlayerData.Ore < ore || PlayerData.Gems < gem) return false;

        PlayerData.Ore -= ore;
        PlayerData.Gems -= gem;
        _levels[u.UpgradeId] = GetLevel(u.UpgradeId) + 1;
        DataManager.SaveData();
        GameEvents.OnWeaponUpgraded?.Invoke(u);
        return true;
    }
}
```

### 4.4 WeaponBase 연동

각 무기 클래스가 자신의 스탯을 읽을 때 `WeaponUpgradeManager.GetBonus()` 호출:

```csharp
public abstract class WeaponBase : MonoBehaviour
{
    protected WeaponData _baseData;

    protected float EffectiveDamage
    {
        get
        {
            var (add, mul) = UpgradeManager.GetBonus(_baseData.WeaponId, WeaponUpgradeStat.Damage);
            return (_baseData.Damage + add) * mul;
        }
    }

    protected float EffectiveCooldown
    {
        get
        {
            // Cooldown 감소 업그레이드는 음수 % → ValuePerLevel = -0.2
            var (_, mul) = UpgradeManager.GetBonus(_baseData.WeaponId, WeaponUpgradeStat.Cooldown);
            return _baseData.FireDelay * Mathf.Max(0.1f, mul);  // 최소 0.1x 바닥
        }
    }
}
```

> **캐시 권장**: 매 프레임 호출 시 딕셔너리 조회 비용. 업그레이드 이벤트 발생 시 `_cachedDamage = …` 식으로 캐시 후 재계산. 실제 캐시 패턴은 [§5.4](#54-weaponbase-캐시--이벤트-구독)에 있음.

---

## 5. Title → Game 영속성 & 전파

### 5.1 원칙

> **PlayerData가 모든 영속 데이터의 단일 진실 소스(SSoT)**.
> `WeaponUpgradeManager`, `AbilitySlotController`, `UpgradeManager`(굴착기)는 **PlayerData의 뷰**일 뿐.
> 씬 전환 시 관리자 GameObject를 넘기지 않고, 새 씬에서 다시 `PlayerData`를 읽어 자기 상태를 복원.

이 방식의 장점:
- `DontDestroyOnLoad` 관리자가 늘어나며 생기는 싱글톤 혼란 회피
- Title 씬 재진입 / Game 씬 재로드 모두 동일하게 작동
- 기존 `UpgradeManager`(굴착기 강화)와 동일 패턴 유지

### 5.2 PlayerData 확장

```csharp
[Serializable]
public class PlayerData
{
    // ── 재화 ──
    public long Ore;
    public long Gems;

    // ── 캐릭터 / 해금 ──
    public string SelectedCharacterId = "victor";
    public List<string> UnlockedWeapons   = new() { "sniper" };
    public List<string> UnlockedAbilities = new();

    // ── 강화 레벨 ──
    // Unity JsonUtility는 Dictionary 직렬화 불가 → List<LevelEntry> 사용
    public List<LevelEntry> WeaponUpgradeLevels = new();   // "sniper_dmg" → 3
    public List<LevelEntry> ExcavatorUpgradeLevels = new(); // 기존 UpgradeManager용

    // ── 직전 세션 결과 ──
    public SessionResult LastSessionResult;

    // ── 런타임 캐시 (직렬화 제외) ──
    [NonSerialized] Dictionary<string, int> _weaponLevelMap;
    [NonSerialized] Dictionary<string, int> _excavatorLevelMap;

    public int GetWeaponUpgradeLevel(string id)
    {
        EnsureWeaponMap();
        return _weaponLevelMap.TryGetValue(id, out var lv) ? lv : 0;
    }

    public void SetWeaponUpgradeLevel(string id, int lv)
    {
        EnsureWeaponMap();
        _weaponLevelMap[id] = lv;
        var entry = WeaponUpgradeLevels.Find(e => e.Id == id);
        if (entry != null) entry.Level = lv;
        else WeaponUpgradeLevels.Add(new LevelEntry { Id = id, Level = lv });
    }

    void EnsureWeaponMap()
    {
        if (_weaponLevelMap != null) return;
        _weaponLevelMap = new Dictionary<string, int>();
        foreach (var e in WeaponUpgradeLevels) _weaponLevelMap[e.Id] = e.Level;
    }
}

[Serializable]
public class LevelEntry
{
    public string Id;
    public int Level;
}
```

> `ISerializationCallbackReceiver`를 써도 되지만 위 패턴이 더 단순. 읽을 때 지연 생성(`EnsureWeaponMap`)하고 쓸 때 리스트·맵을 같이 갱신.

### 5.3 WeaponUpgradeManager 싱글톤

**씬마다 배치** + PlayerData 조회. DontDestroyOnLoad 안 씀.

```csharp
public class WeaponUpgradeManager : MonoBehaviour
{
    public static WeaponUpgradeManager Instance { get; private set; }

    [SerializeField] List<WeaponUpgradeData> _allUpgrades;

    void Awake()
    {
        // 씬 간 공유가 아니라 씬마다 하나씩. 중복되면 새것 우선.
        Instance = this;
    }

    public int GetLevel(string upgradeId)
        => DataManager.PlayerData.GetWeaponUpgradeLevel(upgradeId);

    public WeaponUpgradeData FindUpgrade(string upgradeId)
        => _allUpgrades.Find(u => u.UpgradeId == upgradeId);

    public bool TryBuy(WeaponUpgradeData u)
    {
        var (ore, gem) = GetCost(u);
        if (DataManager.PlayerData.Ore < ore) return false;
        if (DataManager.PlayerData.Gems < gem) return false;

        DataManager.PlayerData.Ore -= ore;
        DataManager.PlayerData.Gems -= gem;
        DataManager.PlayerData.SetWeaponUpgradeLevel(
            u.UpgradeId, GetLevel(u.UpgradeId) + 1);
        DataManager.SaveData();

        GameEvents.OnWeaponUpgraded?.Invoke(u.UpgradeId);
        GameEvents.OnCurrencyChanged?.Invoke(
            DataManager.PlayerData.Ore, DataManager.PlayerData.Gems);
        return true;
    }

    // GetBonus, GetCost 는 §4.3 그대로
}
```

Title·Game 씬 각각에 `WeaponUpgradeManager` GameObject 1개. `Instance`는 현재 씬의 것을 가리킴. 데이터는 씬과 무관하게 `PlayerData`에 있음.

### 5.4 WeaponBase 캐시 + 이벤트 구독

```csharp
public abstract class WeaponBase : MonoBehaviour
{
    protected WeaponData _baseData;

    // 캐시
    float _cachedDamage;
    float _cachedCooldown;
    float _cachedRange;
    // …

    void OnEnable()
    {
        RefreshCachedStats();
        GameEvents.OnWeaponUpgraded += OnUpgradedAny;
    }

    void OnDisable()
    {
        GameEvents.OnWeaponUpgraded -= OnUpgradedAny;
    }

    void OnUpgradedAny(string upgradeId)
    {
        var u = WeaponUpgradeManager.Instance?.FindUpgrade(upgradeId);
        if (u != null && u.WeaponId == _baseData.WeaponId)
            RefreshCachedStats();
    }

    void RefreshCachedStats()
    {
        var mgr = WeaponUpgradeManager.Instance;
        if (mgr == null) return;

        var (dmgAdd, dmgMul) = mgr.GetBonus(_baseData.WeaponId, WeaponUpgradeStat.Damage);
        _cachedDamage = (_baseData.Damage + dmgAdd) * dmgMul;

        var (_, cdMul) = mgr.GetBonus(_baseData.WeaponId, WeaponUpgradeStat.Cooldown);
        _cachedCooldown = _baseData.FireDelay * Mathf.Max(0.1f, cdMul);

        var (_, rMul) = mgr.GetBonus(_baseData.WeaponId, WeaponUpgradeStat.Range);
        _cachedRange = _baseData.Range * rMul;
    }

    // 발사 시 _cachedDamage / _cachedCooldown / _cachedRange 사용
}
```

> Game 씬은 강화 UI가 없으므로 `OnWeaponUpgraded`가 세션 중 발행될 일은 실제로 없다. 이벤트 구독은 **안전망**(디버그 치트로 인게임 강화 허용 시 대비) + Title 씬 내에서 `WeaponUpgradeManager.Instance`를 쓰는 미리보기용.

### 5.5 전체 데이터 흐름

```
┌──────────── Title 씬 ─────────────┐         ┌─── PlayerData (JSON) ───┐
│                                   │         │                         │
│  UpgradeUI 버튼 클릭              │         │  Ore: 250               │
│    ↓                              │         │  Gems: 42               │
│  WeaponUpgradeManager.TryBuy(u)   │ ─────→  │  WeaponUpgradeLevels:   │
│    • PlayerData.Ore -= cost       │  쓰기    │    sniper_dmg: 3        │
│    • SetWeaponUpgradeLevel(+1)    │         │    bomb_radius: 2       │
│    • DataManager.SaveData()       │         │  UnlockedWeapons:       │
│    • OnWeaponUpgraded 발행         │         │    [sniper, bomb, gun]  │
│    ↓                              │         │                         │
│  UpgradeUI 재렌더                 │         └─────────┬───────────────┘
│                                   │                   │
└────────────┬──────────────────────┘                   │ 읽기
             │ "채굴 시작"                              │
             │ SceneManager.LoadScene("Game")            │
             ▼                                           │
┌──────────── Game 씬 ──────────────┐                   │
│                                   │                   │
│  WeaponUpgradeManager.Awake()     │ ←─────────────────┘
│    (_allUpgrades SO는 씬에 있음,  │
│     레벨은 PlayerData에서 조회)    │
│                                   │
│  WeaponBase.OnEnable()            │
│    → RefreshCachedStats()         │
│    → _cachedDamage 반영           │
│                                   │
│  발사 루프                        │
│    → _cachedDamage / Cooldown     │
│    → 강화 효과 실시간 반영         │
│                                   │
└───────────────────────────────────┘
```

### 5.6 같은 패턴 재사용

동일한 "PlayerData = SSoT" 흐름을 다음 시스템이 공유:

| 시스템 | PlayerData 필드 | 관리자 |
|---|---|---|
| 무기 해금 | `UnlockedWeapons` | `WeaponUpgradeManager.TryUnlockWeapon` |
| 무기 강화 | `WeaponUpgradeLevels` | `WeaponUpgradeManager.TryBuy` |
| 어빌리티 해금 | `UnlockedAbilities` | `AbilityShopManager.TryUnlock` (신규) |
| 굴착기 강화 | `ExcavatorUpgradeLevels` | 기존 `UpgradeManager` |
| 캐릭터 선택 | `SelectedCharacterId` | `CharacterSelectUI` |
| 재화 | `Ore`, `Gems` | 위 모든 매니저의 `TryBuy` |

모두 `DataManager.SaveData()` 하나로 저장, 씬 전환 시 `PlayerData` 재조회로 복원.

### 5.7 세션 보상과의 상호작용

Game 씬 내에서는 `SessionState.SessionOre/SessionGems`에만 누적 (PlayerData 직접 수정 X).
세션 종료 시점에 한 번에 `PlayerData`로 커밋:

```csharp
// SessionManager.EndSession()
DataManager.PlayerData.Ore += sessionOreFinal;
DataManager.PlayerData.Gems += sessionGemsFinal;
DataManager.SaveData();
```

이렇게 하면 세션 도중 강제 종료·에러에도 직전 저장 상태로 복원되며, 강화 레벨이 **인게임 재화 변동과 섞이지 않음**.

---

## 6. 무기 상점 UI (WeaponShopSubPanel)

v2의 `weapon-card` 구조와 동일:

```
WeaponCard (5개)
├── Top
│   ├── Icon (28×28)
│   ├── NameText
│   └── StatusText   ("활성화" / "미해금")
└── Body
    ├─ (미해금) UnlockButton  ("폭탄 해금 — 30💎")
    └─ (해금됨) 3× UpgradeRow
        ├── NameText
        ├── LevelText    (2/5)
        └── CostText     (40🪨 2💎)  또는  "완료"
```

카드 테두리: 해금 상태면 캐릭터 컬러 대신 보라 (`#4a1890`).

---

## 7. 회전톱날 무기 (구현 완료 2026-04-19)

### 7.1 컨셉

머신 주위를 **마우스 방향으로** 자동 회전하는 톱날. 벌레에 닿으면 데미지 + 슬로우 디버프.

### 7.2 v2 파라미터 (60fps 기준)

| 속성 | 값 | Unity 변환 |
|---|---|---|
| 톱날 궤도 반경 | 72 유닛 | `OrbitRadius = 7.2f` |
| 톱날 블레이드 반경 | 18 유닛 | `BladeRadius = 1.8f` |
| 데미지 | 0.15 / tick | `WeaponData.Damage = 0.15f` (tick당) |
| 슬로우 강도 | 30% (기본) | `SlowFactor = 0.3f` |
| 슬로우 지속 | 2초 (120 frames) | `SlowDuration = 2f` |
| 회전 속도 | 0.08 rad/frame × `*5` 배율 | `SpinSpeed = 24 rad/sec` ⚠ |
| 데미지 주기 | 0.1초 (6 frames) | `DamageTickInterval = 0.1f` |

> ⚠ **SpinSpeed 주의**: v2.html 1109줄 `sawAngle += ws.saw.spinSpeed * dt * 5` — 기본값 0.08에 추가 **×5 배율**이 곱해져 있음. 60fps 기준 실제 = `0.08 × 5 × 60 = 24 rad/sec`. 문서 초안의 `4.8 rad/sec`은 이 배율을 놓친 계산이므로 **SpinSpeed = 24**로 설정해야 v2 체감과 일치.

### 7.3 SawWeaponData.cs

```csharp
[CreateAssetMenu(menuName = "Drill-Corp/Weapons/Saw")]
public class SawWeaponData : WeaponData
{
    public float OrbitRadius = 7.2f;        // 머신~톱날 궤도 거리
    public float BladeRadius = 1.8f;        // 블레이드 충돌 반경
    public float SpinSpeed = 4.8f;          // rad/sec (블레이드 자체 회전)
    public float DamagePerTick = 0.15f;
    public float DamageTickInterval = 0.1f;
    public float SlowFactor = 0.3f;
    public float SlowDuration = 2f;
    public GameObject BladeVisualPrefab;    // 톱날 스프라이트/메시
}
```

### 7.4 SawWeapon.cs (요약)

```csharp
public class SawWeapon : WeaponBase
{
    SawWeaponData _data;
    Transform _bladeVisual;
    float _tickTimer;
    float _bladeSpinAngle;

    protected override void Fire(Vector3 aim) { /* 즉발 아님 — Update에서 처리 */ }

    void Update()
    {
        if (!IsActive) return;

        // 1) 궤도 위치: 머신 → 마우스 방향
        Vector3 fromMachine = (aim - machinePos);
        fromMachine.y = 0;  // XZ 평면 (CLAUDE.md 좌표계)
        Vector3 orbitPos = machinePos + fromMachine.normalized * _data.OrbitRadius;

        // 2) 블레이드 회전 (시각만)
        _bladeSpinAngle += _data.SpinSpeed * Time.deltaTime;
        _bladeVisual.position = orbitPos;
        _bladeVisual.rotation = Quaternion.Euler(90, 0, _bladeSpinAngle * Mathf.Rad2Deg);
        // ↑ 스프라이트가 Y축으로 눕혀있을 경우. CLAUDE.md 좌표계 섹션 참조.

        // 3) 데미지 틱
        _tickTimer -= Time.deltaTime;
        if (_tickTimer <= 0f)
        {
            _tickTimer = _data.DamageTickInterval;
            var effBlade = _data.BladeRadius * UpgradeMul("Radius");
            var effDmg   = _data.DamagePerTick * UpgradeMul("Damage");
            var effSlow  = Mathf.Min(0.9f, _data.SlowFactor + UpgradeAdd("SlowBonus"));

            var hits = Physics.OverlapSphere(orbitPos, effBlade, _bugLayer);
            foreach (var h in hits)
            {
                var bug = h.GetComponent<BugController>();
                bug?.TakeDamage(effDmg);
                bug?.ApplySlow(effSlow, _data.SlowDuration);
            }
        }
    }
}
```

### 7.5 톱날 프리펩 주의사항

> **`Instantiate(bladePrefab, pos, Quaternion.identity)` 금지.**
> 원본 프리펩에 스프라이트가 이미 탑다운 각도(X=90°)로 누워있다면, 그 회전을 보존:
> ```csharp
> var blade = Instantiate(bladePrefab, pos, bladePrefab.transform.rotation);
> ```
> 이후 프레임별 회전은 프리펩 회전과 블레이드 각도를 합성 (`rotation * Quaternion.Euler(0, 0, spinAngle)`).
> 참조: `CLAUDE.md` 좌표계 규칙, 기존 `BugHpBar.cs`.

### 7.6 BugController.ApplySlow

기존에 없으면 신규 추가:

```csharp
public class BugController : MonoBehaviour
{
    float _slowStrength;     // 0~1
    float _slowTimer;

    public void ApplySlow(float strength, float durationSec)
    {
        if (strength > _slowStrength)  // 더 강한 슬로우만 덮어씀
        {
            _slowStrength = strength;
        }
        _slowTimer = Mathf.Max(_slowTimer, durationSec);  // 지속시간은 max
    }

    void Update()
    {
        if (_slowTimer > 0)
        {
            _slowTimer -= Time.deltaTime;
            if (_slowTimer <= 0) _slowStrength = 0;
        }
        float speedMul = 1f - _slowStrength;
        // MovementBehavior에 speedMul 전달
    }
}
```

> 충격파 어빌리티도 이 `ApplySlow`를 재사용 (50% / 3초).

### 7.7 실제 구현 (2026-04-19)

초안 §7.3~7.6 대부분이 그대로 반영됐으며, 아래 항목만 결정 변경:

| 항목 | 초안 | 실제 | 이유 |
|---|---|---|---|
| 데미지 필드 | `DamagePerTick = 0.15f` 별도 필드 | `WeaponData.Damage` 재활용 | 레이저가 이미 `Damage`를 tick당 값으로 쓰는 패턴과 통일 |
| 발사 구동 | 초안 미정 | **self-driven** (자체 `Update`에서 `TryFire`) | v2 동시 발동 아키텍처 전환(`AimController.EquipWeapon` 경로 제거) |
| 블레이드 시각 | 외부 프리펩 바인딩 | **자동 빌더 에디터** (`SawBladePrefabBuilder`) | 10톱니+허브+볼트 절차적 메시 — 외부 에셋 불필요 |
| SpinSpeed 기본값 | `4.8` | `24` | v2 `*5` 배율 반영 (§7.2 경고 참조) |

**파일 매핑**

| 역할 | 경로 |
|---|---|
| SO 정의 | `Assets/_Game/Scripts/Weapon/Saw/SawWeaponData.cs` |
| 런타임 | `Assets/_Game/Scripts/Weapon/Saw/SawWeapon.cs` |
| SO 에셋 | `Assets/_Game/Data/Weapons/Weapon_Saw.asset` |
| 블레이드 프리펩 빌더 | `Assets/_Game/Scripts/Editor/SawBladePrefabBuilder.cs` |
| 씬 셋업 에디터 | `Assets/_Game/Scripts/Editor/WeaponPanelSawSetup.cs` |
| SO 생성 메뉴 | `V2DataSetupEditor.CreateSawWeaponDataMenu` |
| 블레이드 프리펩 | `Assets/_Game/Prefabs/Weapons/SawBlade.prefab` |

**에디터 메뉴 체인** (풀셋업 1-click):
```
Tools > Drill-Corp > Weapons > ★ Saw 풀셋업 (에셋 + 프리펩 + 씬)
  └─ 1. Weapon_Saw.asset 생성 (V2DataSetupEditor)
  └─ 2. SawBlade.prefab + 메시·머티리얼 3종 (SawBladePrefabBuilder)
  └─ 3. 씬에 SawWeapon + WeaponSlot_Saw 추가 (WeaponPanelSawSetup)
```

**v2와 남은 격차** (의도적 스킵):
- 팔/파이프 시각 (머신→블레이드 회색 막대) — 탑다운 3D에서 시각적 이득 낮음
- 외곽 슬로우 링 (`br+5` 파란 링) — 프리펩 확장으로 추후 가능
- 보스 2배 데미지 — 보스 시스템 미구현, 도입 시 추가

---

## 8. 기관총 탄창 / 리로딩

v2에서는 기관총만 탄창 개념이 있음:

| 속성 | 값 |
|---|---|
| 기본 탄창 | 40발 |
| gun_ammo 레벨당 | +10발 |
| 재장전 시간 | 5초 (300 frames) |
| gun_reload 레벨당 | −20% |

현재 프로젝트의 `MachineGun` 구현은 **이미 탄창/리로딩 구현됨** (`baa43e6` 커밋). 강화 적용만 추가:

```csharp
void OnAmmoUpgraded()
{
    var (add, _) = UpgradeManager.GetBonus("gun", WeaponUpgradeStat.AmmoBonus);
    _maxAmmo = _data.BaseMaxAmmo + (int)add;
}

void OnReloadUpgraded()
{
    var (_, mul) = UpgradeManager.GetBonus("gun", WeaponUpgradeStat.ReloadTime);
    _reloadDuration = _data.BaseReloadDuration * mul;
}
```

---

## 9. 무기별 튜닝 포인트

| 무기 | 주 강화 | 체감 |
|---|---|---|
| sniper | dmg > cd > range | 기본 저격, 꾸준한 DPS 상승 |
| bomb | radius > dmg > cd | 범위 확대가 수동 조준 부담 완화 |
| gun | dmg = ammo > reload | ammo 확장이 리로드 빈도 감소 |
| laser | range > dmg > cd | 레이저는 **자동 발동**, 범위가 핵심 |
| saw | radius > slow > dmg | slow는 어빌리티 콤보 (블랙홀/충격파)와 시너지 |

---

## 10. 참고 문서

- [V2_IntegrationPlan.md](V2_IntegrationPlan.md) — 총론·우선순위
- [WeaponSystem.md](WeaponSystem.md) — 기존 무기 아키텍처 (WeaponBase, AimController)
- [CharacterAbilitySystem.md](CharacterAbilitySystem.md) — 폭탄 강화가 지뢰 어빌리티에 파급
- [GoogleSheetsGuide.md §5~§6](GoogleSheetsGuide.md#5-weapondata-시트) — WeaponData/WeaponUpgradeData 시트 스키마
- [PlannerSheetGuide.md](PlannerSheetGuide.md) — 기획자 전달용 압축 가이드

---

## 11. 인게임 적용 현황 (2026-04-20)

### 11.1 무기 해금 필터 — ✅

`WeaponBase.TryDisableIfLocked()` (`Scripts/Weapon/WeaponBase.cs`):

```csharp
protected bool TryDisableIfLocked()
{
    if (_baseData == null || string.IsNullOrEmpty(_baseData.WeaponId)) return false;
    var dm = DrillCorp.Core.DataManager.Instance;
    if (dm?.Data == null) return false;
    if (!dm.Data.HasWeapon(_baseData.WeaponId))
    {
        gameObject.SetActive(false);
        return true;
    }
    return false;
}
```

5종 무기(Sniper/Bomb/MachineGun/Laser/Saw) 모두 `Start()` 첫 줄에서 호출 → 미해금이면 GO 자체 비활성. 레거시 무기(Shotgun/BurstGun/LockOn)는 `WeaponId`가 비어있어 pass-through.

**4 무기 SO에 WeaponId 채움** (Hub WeaponShopUI ID와 일치):
| SO | WeaponId | UnlockedByDefault |
|---|---|---|
| `Weapon_Sniper.asset` | `sniper` | ✅ |
| `Weapon_Bomb.asset` | `bomb` | ❌ (30💎) |
| `Weapon_MachineGun.asset` | `gun` | ❌ (20💎) |
| `Weapon_LaserBeam.asset` (← `LaserWeaponData`) | `laser` | ❌ (40💎) |
| `Weapon_Saw.asset` | `saw` | ❌ (40💎) |

> ⚠️ `Weapon_Laser.asset`(LaserBeamData, 레거시)과 `Weapon_LaserBeam.asset`(LaserWeaponData, 실제 사용)이 **이름이 반대**. v2 플레이용은 `Weapon_LaserBeam`.

### 11.2 AimRingBinder 자동 숨김 — ✅

4종 바인더(`SniperAimRingBinder`/`BombAimRingBinder`/`MachineGunAimRingBinder`/`LaserAimRingBinder`)가 첫 Update에서:

```csharp
if (_weapon == null || !_weapon.gameObject.activeInHierarchy)
{
    gameObject.SetActive(false);  // 바인더 GO 자체 끄면 호 영구 숨김
    return;
}
```

미해금 무기는 호도 안 그려짐 — 인스펙터 직렬화된 참조가 살아있어도 (Unity SetActive(false)는 C# 참조 살림) `activeInHierarchy` 체크로 정확히 감지.

### 11.3 4종 무기 강화 적용 — ✅ (Saw 패턴 복사)

5종 모두 동일한 `RefreshEffectiveStats()` + `OnWeaponUpgraded` 구독:

```csharp
private void OnEnable() {
    GameEvents.OnWeaponUpgraded += OnWeaponUpgradedAny;
    RefreshEffectiveStats();
}

private void OnWeaponUpgradedAny(string upgradeId) {
    if (string.IsNullOrEmpty(upgradeId)) { RefreshEffectiveStats(); return; }
    var u = WeaponUpgradeManager.Instance?.FindUpgrade(upgradeId);
    if (u != null && u.WeaponId == _data.WeaponId) RefreshEffectiveStats();
}

private void RefreshEffectiveStats() {
    var (_, dmgMul) = mgr.GetBonus(_data.WeaponId, WeaponUpgradeStat.Damage);
    _effectiveDamage = _data.Damage * dmgMul;
    // ... 기타 스탯
}
```

**무기별 적용 스탯**:
| 무기 | Damage | Cooldown | Range | Radius | Slow | Ammo | Reload |
|---|---|---|---|---|---|---|---|
| Sniper | ✅ | ✅ | ⏸️ (AimController 통합 필요) | — | — | — | — |
| Bomb | ✅ | ✅ | — | ✅ | — | — | — |
| MachineGun | ✅ | — | — | — | — | ✅ | ✅ |
| Laser | ✅ | ✅ (`_laserCD` 직접) | ⏸️ | — | — | — | — |
| Saw | ✅ | (쿨 없음) | — | ✅ | ✅ | — | — |

> Range 강화는 AimController의 검색 반경 통합이 필요해 보류. 다른 스탯은 모두 활성.

### 11.4 EffectiveFireDelay virtual

`WeaponBase`에 추가:

```csharp
protected virtual float EffectiveFireDelay
    => _baseData != null ? _baseData.FireDelay : 0f;

public virtual void TryFire(AimController aim) {
    // ... existing
    float delay = EffectiveFireDelay;
    if (delay > 0f) _nextFireTime = Time.time + delay;
}
```

Sniper/Bomb는 `_data.FireDelay * _effectiveFireDelayMul` 오버라이드로 Cooldown 강화 자동 반영. Laser는 `_laserCD = _effectiveCooldown` 직접 사용 (별도 쿨 변수).

### 11.5 투사체 effective 오버로드

투사체가 `_data.Damage`/`_data.ExplosionRadius`를 직접 읽지 않도록 Initialize에 추가 인자:

```csharp
// BombProjectile
public void Initialize(Vector3 targetPos, BombData data, LayerMask bugLayer)
    => Initialize(targetPos, data, bugLayer, data.Damage, data.ExplosionRadius);
public void Initialize(Vector3 targetPos, BombData data, LayerMask bugLayer,
                       float effectiveDamage, float effectiveRadius) { ... }
```

`MachineGunBullet.Initialize(... float effectiveDamage)`, `LaserBeam.Initialize(... float effectiveDamage)` 동일 패턴. 기본 오버로드는 _data 값으로 호출 → 호환성 유지.

### 11.6 파일 매핑 (전체 무기 v2 적용)

| 무기 | Weapon 클래스 | Data SO | Projectile | Binder |
|---|---|---|---|---|
| Sniper | `Weapon/Proto/SniperWeapon.cs` | `Weapon_Sniper.asset` | (없음, 즉발) | `Aim/SniperAimRingBinder.cs` |
| Bomb | `Weapon/Bomb/BombWeapon.cs` | `Weapon_Bomb.asset` | `BombProjectile.cs` | `Aim/BombAimRingBinder.cs` |
| Gun | `Weapon/MachineGun/MachineGunWeapon.cs` | `Weapon_MachineGun.asset` | `MachineGunBullet.cs` | `Aim/MachineGunAimRingBinder.cs` |
| Laser | `Weapon/Laser/LaserWeapon.cs` | `Weapon_LaserBeam.asset` | `LaserBeam.cs` | `Aim/LaserAimRingBinder.cs` |
| Saw | `Weapon/Saw/SawWeapon.cs` | `Weapon_Saw.asset` | (블레이드 visual prefab) | (없음) |

### 11.7 남은 작업

- **레거시 무기 SO 정리** — `Weapon_Laser.asset`(LaserBeamData)는 사용처 없음
- `docs/v2.html` 675~1000줄 — `BASE` 스탯 / `ws` 적용 / 무기 발사 구현 원본

---

## 12. 2026-04-20 (2) 업데이트 — Range 업그레이드 v2 포팅

저격총·레이저의 Range(범위) 업그레이드를 v2 원본과 동일하게 구현.

### 12.1 저격총 Range → 에임 반경 동적 확장

v2 원본: `ws.sniper.range`가 **에임 원의 반경**. Range 업그레이드는 이 값에 1 + lv×0.2를 곱해 에임 크기 자체를 확대. 링(쿨다운 호)들도 `r+5`, `r+11` 상대 offset이라 자동 따라감.

**우리 구현**:

```csharp
// Scripts/Aim/AimController.cs
private float _rangeMultiplier = 1f;
private float _baseAimRadius;
private Vector3 _baseCrosshairScale;

public void SetRangeMultiplier(float multiplier)
{
    multiplier = Mathf.Max(0.1f, multiplier);
    if (Mathf.Approximately(_rangeMultiplier, multiplier)) return;
    _rangeMultiplier = multiplier;
    ApplyRangeMultiplier();
}

private void ApplyRangeMultiplier()
{
    _aimRadius = _baseAimRadius * _rangeMultiplier;
    if (_crosshairRenderer != null)
        _crosshairRenderer.transform.localScale = _baseCrosshairScale * _rangeMultiplier;
}
```

`CalculateAimRadius`에서 `_baseAimRadius`/`_baseCrosshairScale`을 저장해 배율 적용 시 원본 값에 곱셈.

**SniperWeapon 연결**:

```csharp
// Scripts/Weapon/Proto/SniperWeapon.cs — RefreshEffectiveStats
(_, rangeMul) = mgr.GetBonus(_data.WeaponId, WeaponUpgradeStat.Range);
if (_aimController != null)
    _aimController.SetRangeMultiplier(rangeMul);
```

결과:
- Sniper range lv 1 구매 → 에임 반경 1.2배 + 크로스헤어 스프라이트 1.2배 스케일
- 모든 `AimWeaponRing`이 `AimRadius + _radiusOffset` 공식이므로 Sniper/Bomb/Gun/Laser 호 4개 **자동 확장** (offset은 상수라 겹침 없음)
- `BugsInRange` 판정 반경도 같이 확장 → 실제 사격 범위 증가

### 12.2 레이저 Range → 빔 반경 확장 (독립)

v2 원본: `ws.laser.range=28.8`이 `ws.sniper.range=24`와 **별개 독립 변수**. 레이저 range 업그레이드는 빔 반경만 확장, 에임과 무관.

**우리 구현**:

```csharp
// Scripts/Weapon/Laser/LaserWeapon.cs
private float _effectiveRadius;

private void RefreshEffectiveStats()
{
    // ...
    (_, rangeMul) = mgr.GetBonus(_data.WeaponId, WeaponUpgradeStat.Range);
    _effectiveRadius = _data.BeamRadius * Mathf.Max(0.1f, rangeMul);
}

// Fire()에서
_activeBeam.Initialize(aim, _data, aim.BugLayer, _effectiveDamage, _effectiveRadius);
```

`LaserBeam.Initialize`에 **effectiveRadius 오버로드 추가**. 기존 오버로드 2개는 새 버전에 위임 (호환성 유지):

```csharp
// Scripts/Weapon/Laser/LaserBeam.cs
public void Initialize(AimController aim, LaserWeaponData data, LayerMask bugLayer)
    => Initialize(aim, data, bugLayer, data.Damage, data.BeamRadius);
public void Initialize(AimController aim, LaserWeaponData data, LayerMask bugLayer, float effectiveDamage)
    => Initialize(aim, data, bugLayer, effectiveDamage, data.BeamRadius);
public void Initialize(AimController aim, LaserWeaponData data, LayerMask bugLayer,
                       float effectiveDamage, float effectiveRadius)
{
    _radius = effectiveRadius;  // 업그레이드 반영
    // 시각 레이어(Core/Ring/Glow/Crosshair/파티클) 모두 _radius 기반 → 자동 확장
    // OverlapSphere 피격 판정도 _radius 기반 → 자동 확장
}
```

Scorch 프리펩도 `_effectiveRadius × 2 × scaleMul`로 크기 조정.

### 12.3 무기별 범위 독립성

| 무기 | 범위 소스 | Range 업그레이드 대상 | 에임 종속? |
|---|---|---|---|
| Sniper | `aim.AimRadius` (에임 원) | **에임 반경 자체** (`RangeMultiplier`) | 에임이 곧 저격총 |
| Bomb | `_explosionRadius` 독립 | `WeaponUpgradeStat.Radius` | ❌ 완전 독립 |
| BurstGun | 최근접 1마리 | (범위 개념 없음) | ❌ |
| Laser | `_beamRadius=1.0` 독립 | `_effectiveRadius = _beamRadius × rangeMul` | ❌ 완전 독립 |
| Saw | `_orbitRadius`/`_bladeRadius` 독립 | `WeaponUpgradeStat.Radius` | ❌ 완전 독립 |

저격총만 에임과 동일시 되고, 나머지는 자기 범위를 독립적으로 관리.

### 12.4 버그 수정 — `Weapon_Laser.asset` vs `Weapon_LaserBeam.asset`

Game 씬에 실제 부착된 것은 **`LaserWeapon + LaserWeaponData`** (에셋 `Weapon_LaserBeam.asset`). `LaserBeamWeapon + LaserBeamData`는 레거시 경로로 사용 안 됨. 두 경로 모두 Damage/Range/Cooldown 구독을 일관화 했으나 실제 반영되는 건 `LaserWeapon`만.

### 12.5 호 겹침 방지

현재 Game 씬의 AimWeaponRing 4개 `_radiusOffset`:

| 무기 | offset |
|---|---|
| Sniper | 2.0 |
| Bomb | 2.7 |
| Gun | 3.5 |
| Laser | 4.3 |

계단식 차등. 에임 반경이 커지면 모든 링이 `+offset` 상수만큼 바깥에 위치하므로 **구조적으로 겹치지 않음** (v2 `r+5/r+11` 원리와 동일).

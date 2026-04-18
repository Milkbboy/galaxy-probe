# 캐릭터 & 어빌리티 시스템

> 최종 갱신: 2026-04-17
> 근거 프로토타입: `docs/v2.html`
> 상위 문서: [V2_IntegrationPlan.md](V2_IntegrationPlan.md)

## 1. 개요

기존 `MachineSelect`(3머신, 스탯만 차별화)를 **캐릭터(3종)** 컨셉으로 전환. 캐릭터는 **고유 어빌리티 3개**로 차별화되며, 어빌리티는 인게임에서 키 **1·2·3**으로 발동한다.

> **머신 데이터는 남긴다.** 캐릭터마다 기본 머신(DefaultMachineId)이 연결되며, MachineData SO는 그대로 유지. 캐릭터는 "머신 + 어빌리티 프리셋"의 조합체.

### 캐릭터 3종

| ID | 이름 | 컨셉 | 컬러 | 어빌리티 1→2→3 |
|---|---|---|---|---|
| `victor` | 빅터 | 중장비 전문가 | `#f4a423` | 네이팜 → 화염방사기 → 폭발지뢰 |
| `sara` | 사라 | 방어 전문가 | `#4fc3f7` | 블랙홀 → 충격파 → 반중력 메테오 |
| `jinus` | 지누스 | 채굴 전문가 | `#51cf66` | 드론 포탑 → 채굴 드론 → 드론 거미 |

> 각 캐릭터의 2·3번 어빌리티는 1번이 해금된 후에만 해금 가능 (req 체인, 각 30💎).

---

## 2. CharacterData SO

### 파일 위치

```
Assets/_Game/Scripts/Data/CharacterData.cs
Assets/_Game/Data/Characters/Character_Victor.asset
                              Character_Sara.asset
                              Character_Jinus.asset
```

### 스크립트

```csharp
[CreateAssetMenu(menuName = "Drill-Corp/Character Data")]
public class CharacterData : ScriptableObject
{
    public string CharacterId;              // "victor" / "sara" / "jinus"
    public string DisplayName;              // "빅터"
    public string Title;                    // "중장비 전문가"
    [TextArea] public string Description;
    public Color ThemeColor = Color.white;
    public MachineData DefaultMachine;      // 기존 SO 재사용
    public AbilityData[] Abilities = new AbilityData[3];  // 1·2·3번 슬롯
}
```

### 에셋 필드 예시

| Field | Victor | Sara | Jinus |
|---|---|---|---|
| CharacterId | victor | sara | jinus |
| DisplayName | 빅터 | 사라 | 지누스 |
| Title | 중장비 전문가 | 방어 전문가 | 채굴 전문가 |
| ThemeColor | #F4A423 | #4FC3F7 | #51CF66 |
| DefaultMachine | Machine_Default | Machine_Heavy | Machine_Speed |
| Abilities[0] | Ability_Napalm | Ability_BlackHole | Ability_Drone |
| Abilities[1] | Ability_Flame | Ability_Shockwave | Ability_MiningDrone |
| Abilities[2] | Ability_Mine | Ability_Meteor | Ability_SpiderDrone |

### PlayerData 통합 스키마

PlayerData는 **모든 영속 데이터의 단일 진실 소스(SSoT)**. 어빌리티 해금 · 무기 해금/강화 · 굴착기 강화 · 재화 · 캐릭터 선택이 **하나의 `playerdata.json`에 저장**되며, 씬 전환 시 관리자가 이 데이터를 재조회하여 자기 상태를 복원한다. 전체 데이터 흐름은 [WeaponUnlockUpgradeSystem.md §5](WeaponUnlockUpgradeSystem.md#5-title--game-영속성--전파) 참조.

```csharp
[Serializable]
public class PlayerData
{
    // ── 재화 ──
    public long Ore;                                 // 기존 Currency → Ore
    public long Gems;                                // 신규

    // ── 캐릭터 / 해금 ──
    public string SelectedCharacterId = "victor";    // 기존 SelectedMachineId 대체
    public List<string> UnlockedWeapons   = new() { "sniper" };
    public List<string> UnlockedAbilities = new();   // "victor_napalm" 등

    // ── 강화 레벨 (Unity JsonUtility Dictionary 미지원 → List<LevelEntry>) ──
    public List<LevelEntry> WeaponUpgradeLevels    = new();
    public List<LevelEntry> ExcavatorUpgradeLevels = new();  // 기존 UpgradeManager

    // ── 통계 · 세션 결과 ──
    public int TotalSessionsPlayed;
    public int TotalBugsKilled;
    public SessionResult LastSessionResult;

    // ── 런타임 캐시 (직렬화 제외) ──
    [NonSerialized] Dictionary<string, int> _weaponLevelMap;
    [NonSerialized] Dictionary<string, int> _excavatorLevelMap;

    public int GetWeaponUpgradeLevel(string id)   { /* §5.2 구현 */ }
    public void SetWeaponUpgradeLevel(string id, int lv) { /* §5.2 */ }
}
```

> **HashSet이 아닌 List를 쓰는 이유**: `JsonUtility`는 `HashSet<T>` 직렬화 불가. Add 시 `Contains` 체크 O(N)이지만 해금 개수가 수십 개 미만이라 무관.

> **하위 호환**: 기존 `Currency` 값을 `Ore`로 자동 마이그레이션하는 로직을 `DataManager.LoadData()` 초기에 한 번 실행. 상세는 [GemMiningSystem.md §3.1](GemMiningSystem.md#31-마이그레이션).

### 어빌리티 해금/상태 저장

- **해금 여부**만 영속: `UnlockedAbilities` List에 `AbilityId` 문자열 보관.
- **런타임 쿨다운·배치 상태**는 `AbilitySlotController` 내부 변수로만 관리 (세션 종료 시 폐기).
- Game 씬 진입 시 `AbilitySlotController.Start()`가 `PlayerData.UnlockedAbilities`를 읽어 3개 슬롯의 `IAbilityRunner`를 인스턴스화.

---

## 3. CharacterSelectUI (기존 MachineSelectUI 확장)

기존 `MachineSelectUI.cs` / `MachineItemUI.cs`를 **그대로 리네임**해서 활용:

```
MachineSelectUI    → CharacterSelectUI
MachineItemUI      → CharacterItemUI
```

### UI 구성

```
CharacterSelectSubPanel
├── Scroll View (Horizontal)
│   └── Content
│       ├── CharacterItem (Victor)
│       ├── CharacterItem (Sara)
│       └── CharacterItem (Jinus)
└── SelectedInfoPanel
    ├── NameText        (컬러 적용)
    ├── TitleText
    ├── DescriptionText
    └── AbilityPreview (3개 아이콘 + 해금 여부 뱃지)
```

### 선택 저장

```csharp
// 기존 PlayerPrefs.SetInt("SelectedMachine", id) 대신
_playerData.SelectedCharacterId = character.CharacterId;
DataManager.SaveData();

GameEvents.OnCharacterSelected?.Invoke(character);
```

### 머신 자동 적용

캐릭터 선택 시 `character.DefaultMachine`이 Game 씬 로드 시 자동 스폰. 따로 머신 선택 UI는 숨김 (또는 개발 빌드에서만 노출).

---

## 4. AbilityData SO + IAbilityRunner

### 4.1 구조

```
Assets/_Game/Scripts/Ability/
├── AbilityData.cs            # SO 베이스
├── AbilityType.cs            # enum
├── IAbilityRunner.cs         # 실행 인터페이스
├── AbilitySlotController.cs  # 키 1/2/3 → Runner 매핑
│
├── Runners/                  # 어빌리티별 구현 9종
│   ├── NapalmRunner.cs
│   ├── FlameRunner.cs
│   ├── MineRunner.cs
│   ├── BlackHoleRunner.cs
│   ├── ShockwaveRunner.cs
│   ├── MeteorRunner.cs
│   ├── DroneRunner.cs
│   ├── MiningDroneRunner.cs
│   └── SpiderDroneRunner.cs
```

### 4.2 AbilityData.cs

```csharp
[CreateAssetMenu(menuName = "Drill-Corp/Ability Data")]
public class AbilityData : ScriptableObject
{
    public string AbilityId;                // "victor_napalm"
    public string CharacterId;              // "victor"
    public string DisplayName;              // "네이팜 탄"
    public string IconEmoji = "🔥";
    public Sprite Icon;

    public int SlotKey = 1;                 // 1 / 2 / 3
    public AbilityType Type;                // 실행 로직 분기
    public AbilityTrigger Trigger = AbilityTrigger.Manual; // Manual / AutoInterval

    [Header("공통 타이밍")]
    public float CooldownSec = 20f;
    public float DurationSec = 0f;          // 지속형만 (화염방사기·블랙홀 등)
    public float AutoIntervalSec = 0f;      // AutoInterval 트리거만

    [Header("공격/범위")]
    public float Damage = 0.5f;             // 틱 또는 1회
    public float Range = 0f;                // 반경 / 부채꼴 길이 / 드론 사거리
    public float Angle = 0f;                // 부채꼴 각도 (화염방사기)
    public int MaxInstances = 1;            // 동시 배치 수 (지뢰·거미드론)

    [Header("해금")]
    public int UnlockGemCost = 30;
    public AbilityData RequiredAbility;     // req 체인 (null = 캐릭터 최초 어빌리티)

    [Header("시각효과")]
    public GameObject VfxPrefab;
    public AudioClip UseSfx;
}

public enum AbilityType
{
    Napalm, Flame, Mine,           // Victor
    BlackHole, Shockwave, Meteor,  // Sara
    Drone, MiningDrone, SpiderDrone // Jinus
}

public enum AbilityTrigger { Manual, AutoInterval }
```

### 4.3 IAbilityRunner

```csharp
public interface IAbilityRunner
{
    AbilityType Type { get; }
    void Initialize(AbilityData data, AbilityContext ctx);
    void Tick(float dt);             // 매 프레임 (쿨다운 감소, 자동 발동, 지속 효과)
    bool TryUse(Vector3 aimPoint);   // 수동 발동 (키 1/2/3)
    float CooldownNormalized { get; }// UI 표시용 (0=사용가능, 1=방금 사용)
}

public class AbilityContext
{
    public Transform MachineTransform;
    public BugManager BugManager;
    public AimController Aim;
    public Transform VfxParent;
}
```

### 4.4 AbilitySlotController

Game 씬에 배치. 캐릭터 선택을 읽어 3개 Runner를 인스턴스화.

```csharp
public class AbilitySlotController : MonoBehaviour
{
    [SerializeField] CharacterData _character;  // null이면 PlayerData에서 자동 로드
    [SerializeField] InputActionReference _slot1, _slot2, _slot3;

    IAbilityRunner[] _runners = new IAbilityRunner[3];

    void Start()
    {
        var charId = _character?.CharacterId ?? DataManager.PlayerData.SelectedCharacterId;
        var character = DataManager.GetCharacter(charId);

        for (int i = 0; i < 3; i++)
        {
            var data = character.Abilities[i];
            if (!DataManager.PlayerData.UnlockedAbilities.Contains(data.AbilityId))
                continue;  // 해금 안 된 슬롯

            _runners[i] = CreateRunner(data.Type);
            _runners[i].Initialize(data, MakeContext());
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < 3; i++) _runners[i]?.Tick(dt);

        if (_slot1.action.WasPressedThisFrame()) _runners[0]?.TryUse(AimPoint);
        if (_slot2.action.WasPressedThisFrame()) _runners[1]?.TryUse(AimPoint);
        if (_slot3.action.WasPressedThisFrame()) _runners[2]?.TryUse(AimPoint);
    }

    IAbilityRunner CreateRunner(AbilityType t) => t switch
    {
        AbilityType.Napalm      => new NapalmRunner(),
        AbilityType.Flame       => new FlameRunner(),
        AbilityType.Mine        => new MineRunner(),
        AbilityType.BlackHole   => new BlackHoleRunner(),
        AbilityType.Shockwave   => new ShockwaveRunner(),
        AbilityType.Meteor      => new MeteorRunner(),
        AbilityType.Drone       => new DroneRunner(),
        AbilityType.MiningDrone => new MiningDroneRunner(),
        AbilityType.SpiderDrone => new SpiderDroneRunner(),
        _ => null,
    };
}
```

> **New Input System 필수**. `UnityEngine.Input` 레거시 금지 (CLAUDE.md 규칙).

---

## 5. 어빌리티별 상세 명세

v2.html의 숫자를 그대로 가져옴. 프레임 단위는 60fps 기준으로 **초 단위로 변환**.

### 5.1 Victor — 네이팜 탄 (`victor_napalm`)

| 속성 | 값 |
|---|---|
| 타입 | Manual, 부채꼴 화염 지대 |
| 쿨다운 | 40초 (2400 frames) |
| 지속 | 20초 (1200 frames) |
| 범위 | 길이 = 대각선, 좌우 폭 42 유닛 |
| 데미지 | 틱당 0.5, 0.1초 주기 (v2: 6 frames) |
| req | 없음 (캐릭터 최초) |
| 해금 | 30💎 |

**동작**: 마우스 방향으로 긴 부채꼴 화염 지대 생성, 안에 들어온 벌레는 틱 데미지.

### 5.2 Victor — 화염방사기 (`victor_flame`)

| 속성 | 값 |
|---|---|
| 타입 | Manual, 부채꼴 지속 |
| 쿨다운 | 20초 |
| 지속 | 5초 |
| 범위 | 길이 180, 각도 ±20° (0.35 rad) |
| 데미지 | 0.18 / frame (초당 약 10.8) |
| req | `victor_napalm` |

**동작**: 활성화 후 5초간 매 프레임 마우스 방향 부채꼴에 데미지. 이동·무기 동시 사용 가능.

### 5.3 Victor — 폭발지뢰 (`victor_mine`)

| 속성 | 값 |
|---|---|
| 타입 | Manual, 배치형 |
| 쿨다운 | 10초 |
| 최대 수 | 5개 |
| 활성화 지연 | 0.5초 (30 frames) |
| 데미지 | `bomb.dmg × 1.5`, 반경 = `bomb.radius × 0.5` |
| req | `victor_napalm` |

**동작**: 마우스 위치에 지뢰 설치, 0.5초 후 활성화. 벌레 근접(반경 14) 시 폭발. 폭탄 무기 반경·데미지 배율 적용 — **폭탄 무기 강화가 지뢰에도 영향**.

### 5.4 Sara — 블랙홀 (`sara_blackhole`)

| 속성 | 값 |
|---|---|
| 타입 | Manual, 중력 지속 |
| 쿨다운 | 30초 (1800 frames) |
| 지속 | 10초 (600 frames) |
| 범위 | 당기기 반경 180 |
| 흡인력 | 0.9 / frame |
| req | 없음 (캐릭터 최초) |

**동작**: 마우스 위치에 블랙홀 생성. 반경 내 벌레를 중심으로 끌어당김. 데미지 없음 (CC 전용).

### 5.5 Sara — 충격파 (`sara_shockwave`)

| 속성 | 값 |
|---|---|
| 타입 | Manual, 확장 링 |
| 쿨다운 | 50초 (3000 frames) |
| 최대 반경 | 360 |
| 확장 속도 | 14 / frame |
| 밀쳐내기 | 80 유닛 |
| 슬로우 | 50%, 3초 |
| req | `sara_blackhole` |

**동작**: 중심에서 링이 퍼져나가며 닿는 벌레를 밖으로 밀치고 슬로우. 한 벌레는 한 번만 맞음.

### 5.6 Sara — 반중력 메테오 (`sara_meteor`)

| 속성 | 값 |
|---|---|
| 타입 | **AutoInterval** |
| 자동 주기 | 10초 (600 frames) |
| 쿨다운 | — (자동) |
| 착지 폭발 반경 | 55 |
| 화염 지대 지속 | 15초 (900 frames) |
| 틱 데미지 | 0.5 |
| req | `sara_shockwave` |

**동작**: 10초마다 랜덤 위치에 운석 낙하. 착지 시 원형 화염 지대 15초 유지.

### 5.7 Jinus — 드론 포탑 (`jinus_drone`)

| 속성 | 값 |
|---|---|
| 타입 | Manual, 배치 유닛 |
| 쿨다운 | 20초 (1200 frames) |
| 최대 수 | 5개 |
| HP | 30 |
| 사거리 | 100 |
| 발사 쿨 | 0.5초 (30 frames) |
| req | 없음 (캐릭터 최초) |

**동작**: 마우스 위치에 드론 배치. 사거리 내 가장 가까운 벌레에 자동 사격. 접촉한 벌레에 의해 HP 감소 → 파괴.

### 5.8 Jinus — 채굴 드론 (`jinus_mining_drone`)

| 속성 | 값 |
|---|---|
| 타입 | Manual, 자원 생성형 |
| 쿨다운 | 30초 (1800 frames) |
| 지속 | 10초 (600 frames) |
| 채굴 속도 | +5 / sec (mineAmt 증가) |
| 보석 확률 | 초당 10% |
| req | `jinus_drone` |

**동작**: 마우스 위치에 채굴 드론 배치. 10초간 초당 5 채굴 + 10% 확률로 보석 드랍(세션 적립).

### 5.9 Jinus — 드론 거미 (`jinus_spider_drone`)

| 속성 | 값 |
|---|---|
| 타입 | **AutoInterval** |
| 자동 주기 | 10초 (600 frames) |
| 최대 수 | 3기 |
| HP | 40 |
| 사거리 | 120 |
| 발사 쿨 | 0.42초 (25 frames) |
| req | `jinus_mining_drone` |

**동작**: 10초마다 머신 근처에 소환. 가장 가까운 벌레 추적 후 발사. 타겟 없으면 머신 주위 선회. HP 감소 시 파괴.

---

## 6. 어빌리티 해금 UI (AbilityShopSubPanel)

### 구성

```
AbilityShopSubPanel
├── CharacterFilter  (현재 선택된 캐릭터 자동, 수동 전환 X)
├── AbilityNode 1   (req 없음, 기본 해금 가능)
├── AbilityNode 2   (req 1번)
└── AbilityNode 3   (req 2번 또는 1번)
```

### AbilityNodeUI 상태

| 상태 | 조건 | 표시 |
|---|---|---|
| `Locked` | req 미해금 | 회색, "잠김" |
| `Available` | req 해금 + gem 부족 | 반투명, "30💎" |
| `Affordable` | req 해금 + gem 충분 | 캐릭터 컬러 강조 + 클릭 가능 |
| `Owned` | 이미 해금 | 골드 테두리, "보유" |

### 구매 로직

```csharp
public bool TryUnlockAbility(AbilityData data)
{
    if (PlayerData.UnlockedAbilities.Contains(data.AbilityId)) return false;
    if (data.RequiredAbility != null &&
        !PlayerData.UnlockedAbilities.Contains(data.RequiredAbility.AbilityId))
        return false;
    if (PlayerData.Gems < data.UnlockGemCost) return false;

    PlayerData.Gems -= data.UnlockGemCost;
    PlayerData.UnlockedAbilities.Add(data.AbilityId);
    DataManager.SaveData();
    GameEvents.OnAbilityUnlocked?.Invoke(data);
    return true;
}
```

---

## 7. 인게임 어빌리티 UI

Game 씬의 화면 우상단에 어빌리티 슬롯 3개 표시 (v2의 `drawItemUI` 참조).

```
SlotUI
├── KeyLabel     ([1], [2], [3] 또는 [자동])
├── NameText     ("네이팜", "블랙홀"...)
├── CooldownBar  (가로 바, CooldownNormalized 0~1)
└── StatusText   ("사용가능" / "5.2s")
```

캐릭터 컬러로 테두리 표시. 해금되지 않은 슬롯은 숨김 또는 비활성.

---

## 8. 밸런스 튜닝 포인트

| 문제 | 조정 대상 |
|---|---|
| Victor 어빌리티가 약함 | `Damage`, 화염방사기 `DurationSec` |
| Sara 블랙홀이 너무 강함 | 지속 `DurationSec` 축소 |
| Jinus 채굴드론이 P2W | 보석 확률 `0.1` 축소, 쿨다운 연장 |
| 메테오 자동 발동 너무 잦음 | `AutoIntervalSec` 증가 |

v2 수치는 **60fps 기준 프레임 단위**로 설계되어 있어서, Unity의 초 단위로 나눌 때 `/60`. 초안 밸런스는 그대로 옮긴 뒤 플레이테스트 후 조정.

---

## 9. 참고 문서

- [V2_IntegrationPlan.md](V2_IntegrationPlan.md) — 총론·우선순위
- [WeaponUnlockUpgradeSystem.md](WeaponUnlockUpgradeSystem.md) — 무기 강화가 일부 어빌리티(지뢰)에 파급
- [GoogleSheetsGuide_v2Addendum.md](GoogleSheetsGuide_v2Addendum.md) — CharacterData/AbilityData 시트 스키마
- `docs/v2.html` 1054~1215줄 — `useItem()` 및 각 어빌리티 tick 구현 원본

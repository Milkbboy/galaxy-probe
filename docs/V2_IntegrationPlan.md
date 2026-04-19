# v2.html 아웃게임 통합 계획서

> 최종 갱신: 2026-04-17
> 근거 프로토타입: `docs/v2.html` (1851줄)

## 0. 이 문서의 역할

`docs/v2.html` 프로토타입에 새로 추가된 **아웃게임 고도화 요소**를 현재 Unity 프로젝트에 어떻게 반영할지 결정한 계획서. 개별 시스템 설계는 각 하위 문서에서 다루며, 이 문서는 **총론·의사결정·우선순위**를 담는다.

### 관련 문서

| 주제 | 문서 |
|---|---|
| 캐릭터 + 어빌리티 9종 | [CharacterAbilitySystem.md](CharacterAbilitySystem.md) |
| 무기 해금/강화 + 회전톱날 | [WeaponUnlockUpgradeSystem.md](WeaponUnlockUpgradeSystem.md) |
| 보석 드랍·채집, mineTarget 승리 | [GemMiningSystem.md](GemMiningSystem.md) |
| 구글시트 신규 시트 스키마 | [GoogleSheetsGuide_v2Addendum.md](GoogleSheetsGuide_v2Addendum.md) |

---

## 1. v2.html이 바꾼 것

### 아웃게임 (로비) 5개 패널

```
og-body (3열 그리드)
├── 캐릭터 선택        (상단 전체 폭, 3종 카드)
├── 굴착기 강화        (광석만, 4항목)
├── 무기 & 강화        (5종 카드, 해금 버튼 + 3항목 강화)
├── 고유 장비          (캐릭터별 3항목, req 체인)
├── 보석 채집          (2항목)
└── 현재 스탯          (합산 표시)
```

### 인게임 추가 요소

| 요소 | 설명 |
|---|---|
| **보석 드랍** | 벌레 처치 시 기본 5% 드랍, 엘리트 100% / value 5 |
| **보석 채집** | 마우스 호버 2초 (gem_speed로 단축) |
| **mineTarget 승리** | "연료 소진"이 아닌 **"채굴량 도달"로 승리** |
| **회전톱날 무기** | 마우스 방향 자동 회전 + 슬로우 디버프 |
| **캐릭터별 어빌리티** | 키 1/2/3, 캐릭터당 3종 고유 어빌리티 |

### 이중 재화

| 재화 | 획득 | 용도 |
|---|---|---|
| 🪨 **광석 (ore)** | 채굴 진행 + 벌레 처치 비례 | 굴착기 강화, 무기 강화(광석+보석 혼합) |
| 💎 **보석 (gem)** | 벌레 드랍 채집 | 무기 해금, 어빌리티 해금, 보석 채집 스킬 |

---

## 2. 현재 프로젝트 vs v2 갭

| 항목 | 현재 | v2 | 조치 |
|---|---|---|---|
| 씬 구조 | Title + Game 분리 | outgame/ingame DOM 토글 | ✅ 이미 분리. Title 씬 확장만 |
| 재화 | `Currency` 단일 | ore + gem 이중 | ❌ **PlayerData 분리 필요** |
| 머신/캐릭터 | MachineSelectUI (스탯만) | 캐릭터 (어빌리티 차별화) | 🔄 컨셉 전환 |
| 영구 강화 | 6항목 (단일 재화) | 30+항목 (혼합·req 체인) | ❌ 확장 |
| 무기 | 8개 SO, **항상 사용 가능** | 5개, 순차 해금 + 강화 | ❌ 해금/강화 구조 추가 |
| 회전톱날 | 미구현 | 신규 무기 | ❌ 신규 |
| 보석 드랍/채집 | 없음 | 호버 2초 채집 | ❌ 신규 |
| 승리 조건 | 연료 0 | mineTarget 도달 | 🔄 로직 변경 |
| 캐릭터 어빌리티 | 없음 | 9종 (BlackHole/Napalm/…) | ❌ 신규 시스템 |
| Google Sheets Importer | 5시트 자동 | — | ✅ 신규 시트만 추가 |

---

## 3. 씬 구성 판단 — **Title 씬 확장**

### 결정

> 새 씬을 만들지 않는다. **기존 `Title.unity` 안에 허브 패널을 확장**한다.

### 근거

1. `TitleUI.cs`가 이미 패널 토글 구조 (`MainPanel` / `UpgradePanel` / `MachineSelectPanel` / `OptionsPanel`)로 v2의 outgame SPA와 동일한 구조
2. v2의 3열 그리드 = Title의 패널 그리드로 그대로 매핑됨
3. `UpgradeManager`, `OptionsUI`, 머신 선택 데이터 흐름을 재활용 가능
4. Build에 등록되는 씬을 1개(Title)로 유지하면 씬 전환·로딩 비용 최소

### Title 씬 허브 패널 재구성

```
TitleUI
├── MainPanel                        (유지 — 시작/옵션/종료 버튼)
├── ▼ HubPanel (신규 메인 허브)
│   ├── TopBar                       (ore·gem·치트·초기화·채굴 시작)
│   ├── CharacterSelectSubPanel      (기존 MachineSelectUI 확장/교체)
│   ├── ExcavatorUpgradeSubPanel     (기존 UpgradePanel 그대로)
│   ├── WeaponShopSubPanel           (신규)
│   ├── AbilityShopSubPanel          (신규, 캐릭터 필터)
│   ├── GemUpgradeSubPanel           (신규)
│   └── StatDisplaySubPanel          (신규, 합산 표시)
├── OptionsPanel                     (유지)
└── ResultOverlay                    (신규 — 직전 세션 결과 진입 시 표시)
```

### Game 씬은 건드리지 않는다 (4가지만 추가)

1. `GemDrop` / `GemPickup` 컴포넌트
2. `MiningTargetTracker` (기존 FuelGauge 옆에 mineTarget 게이지)
3. `AbilitySlotController` (캐릭터별 키 1/2/3)
4. `SawWeapon` (회전톱날 신규 무기 클래스)

세션 종료 시: `GameManager.ReturnToTitle()` → Title 씬의 `ResultOverlay`가 `PlayerData`에서 직전 세션 보상을 읽어 표시 (v2의 resultPanel과 동일 흐름).

---

## 4. 작업 우선순위

| # | 작업 | 영향 | 문서 |
|---|---|---|---|
| 1 | `PlayerData.Currency` → `Ore` + `Gem` 분리 | DataManager, SessionResultUI | [GemMiningSystem.md §5](GemMiningSystem.md) |
| 2 | `UpgradeData` 이중 재화 필드 추가 + 신규 항목 (`mine_target`, `gem_drop`, `gem_speed`) | UpgradeManager 확장 | [GoogleSheetsGuide_v2Addendum.md §2](GoogleSheetsGuide_v2Addendum.md) |
| 3 | `WeaponUnlock` 상태 `PlayerData`에 추가 | bool[] 또는 HashSet | [WeaponUnlockUpgradeSystem.md §3](WeaponUnlockUpgradeSystem.md) |
| 4 | `WeaponUpgradeData` SO + UpgradeManager의 무기별 누적 보너스 | WeaponBase가 조회 | [WeaponUnlockUpgradeSystem.md §4](WeaponUnlockUpgradeSystem.md) |
| 5 | `CharacterData` SO + `CharacterSelectUI` (MachineSelect 리네임/확장) | UI 거의 그대로 | [CharacterAbilitySystem.md §2](CharacterAbilitySystem.md) |
| 6 | `AbilityData` SO + `IAbilityRunner` 9종 구현 | **가장 무거움**. 시각효과+로직 | [CharacterAbilitySystem.md §4](CharacterAbilitySystem.md) |
| 7 | 인게임 보석 드랍/채집 | BugController.OnDeath + GemPickup | [GemMiningSystem.md §2](GemMiningSystem.md) |
| 8 | `mineTarget` 승리 조건 | MachineController·SessionManager | [GemMiningSystem.md §4](GemMiningSystem.md) |
| 9 | 회전톱날 무기 | WeaponBase 상속 신규 클래스 | [WeaponUnlockUpgradeSystem.md §6](WeaponUnlockUpgradeSystem.md) |
| 10 | `GoogleSheetsImporter` 확장 (4시트) | 기존 패턴 복사 | [GoogleSheetsGuide_v2Addendum.md §6](GoogleSheetsGuide_v2Addendum.md) |

---

## 5. 좌표계·프리펩 변환 규칙 (v2 → Unity)

> `docs/v2.html`은 **2D 캔버스 (Y축 = 화면 상하)**, Unity는 **탑다운 3D (Z축 = 화면 상하)**.
> v2 코드의 sin/cos/각도를 **그대로 카피하지 말 것**. 다음 규칙으로 변환.

### 축 매핑

| v2 (2D) | Unity (탑다운) |
|---|---|
| `x` (좌우) | `x` (그대로) |
| `y` (상하) | **`z`** |
| `-y` (위쪽) | **`+z`** |
| 마우스 → 중심 각도 `atan2(dy, dx)` | `atan2(dz, dx)` — 부호 주의 |
| 화면상 "위쪽" | `Vector3.forward` (**`Vector3.up` 아님**) |

### 프리펩 회전

v2의 `rotate(angle)` + 스프라이트 렌더링은 2D 시스템 자연스럽게 Z축 회전이지만, 탑다운 Unity에서 스프라이트는 보통 X=90°로 누워 있음.

- **스프라이트 프리펩 인스턴스화 시 `Quaternion.identity` 금지**
- 프리펩의 원본 회전 (`prefab.transform.rotation`)을 보존: `Instantiate(prefab, pos, prefab.transform.rotation)`
- 방향이 있는 프리펩(총알 등)은 **프리펩 회전 × 방향 회전**으로 합성

### 월드 UI 오프셋

- 높이 조금 + 화면 아래로 띄움: `new Vector3(0, 0.1f, -0.8f)` 또는 위쪽: `(0, 0.1f, 0.8f)`
- `LateUpdate`에서 `transform.position = target.position + offset`
- 회전 고정: `Quaternion.Euler(90, 0, 0)` 또는 `Quaternion.identity`

참조 구현: `Assets/_Game/Scripts/Bug/BugHpBar.cs`, 프로젝트 `CLAUDE.md` 좌표계 섹션.

---

## 6. v2 스탯 배율 공식 (현재 스탯 계산)

v2 `calcStats()`에서 모든 업그레이드를 합산. Unity에서는 `UpgradeManager.GetTotalBonus(UpgradeType)`와 동일 개념:

| 패턴 | 예시 | Unity 처리 |
|---|---|---|
| `base + lv * amount` | `maxHp = 100 + lv*30` | `IsPercentage=false`, 덧셈 누적 |
| `1 + lv * percent` (덧셈 %) | `sniper_dmg = 1 + lv*0.25` | `IsPercentage=true`, 최종 `base * (1 + bonusSum)` |
| `1 - lv * percent` (감소 %) | `sniper_cd = 1 - lv*0.20` | `IsPercentage=true`, `Operation=Sub` |

### 신규 추가할 UpgradeType (v2 기준)

```csharp
public enum UpgradeType
{
    // === 기존 ===
    MaxHealth, Armor, MiningRate, AttackDamage, AttackSpeed, FuelEfficiency,

    // === v2 신규 (굴착기) ===
    MiningTarget,         // 목표 채굴량 (100 + lv*50)

    // === v2 신규 (보석 채집) ===
    GemDropRate,          // 드랍 확률 (+lv*2%)
    GemCollectSpeed,      // 채집 속도 (+lv*20%)

    // === v2 신규 (무기별 15항목은 WeaponUpgradeData로 분리, Enum 안 씀) ===
}
```

**무기별 강화**는 `WeaponUpgradeData` SO로 분리. UpgradeType 열거형에 섞지 않음. (→ [WeaponUnlockUpgradeSystem.md](WeaponUnlockUpgradeSystem.md))

---

## 7. 치트/초기화 UI

v2 프로토타입에는 편의성을 위한 치트·초기화 버튼이 있음:

| 버튼 | 기능 | 비고 |
|---|---|---|
| 치트 +100 | ore +100, gem +100 | 개발 빌드에서만 노출 |
| 초기화 | ore=0, gem=0, skills={} | 2단계 확인 (확인/취소) |

Unity에서는 `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 가드로 노출 여부 제어 권장.

---

## 8. 구현 현황 (2026-04-19)

### Hub UI — 100% 완료

| # | 작업 | 상태 | 파일 |
|---|---|---|---|
| 1 | `PlayerData.Currency` → `Ore` + `Gem` 분리 | ✅ | `Scripts/Core/DataManager.cs` |
| 2 | `UpgradeData` 이중 재화 + 신규 항목 + costSchedule | ✅ | `Scripts/Data/UpgradeData.cs`, `Upgrades/*.asset` (6종) |
| 3 | `WeaponUnlock` 상태 `PlayerData`에 추가 | ✅ | `DataManager.UnlockedWeapons` |
| 4 | `WeaponUpgradeData` SO + `WeaponUpgradeManager` | ✅ | `Scripts/Data/WeaponUpgradeData.cs`, `Scripts/OutGame/WeaponUpgradeManager.cs` |
| 5 | `CharacterData` SO + `CharacterSelectUI` | ✅ | `Scripts/Data/CharacterData.cs`, `Scripts/OutGame/CharacterSelectUI.cs` |

### Hub UI 추가 구현 (계획서 너머)

| 컴포넌트 | 역할 |
|---|---|
| `HubController.cs` | TopBar 광석/보석 + 치트(+1000)/초기화/뒤로/채굴 시작 |
| `WeaponShopUI.cs` | 5종 무기 카드 (patch-pattern, 2열 마소너리) |
| `AbilityShopUI.cs` | 선택 캐릭터의 3 어빌리티만 필터 표시 |
| `ExcavatorUpgradeUI.cs` | 굴착기 강화 4종 (Name·Effect·Lv·Cost) |
| `GemUpgradeUI.cs` | 보석 채집 2종 |
| `StatDisplayUI.cs` | 9행 실시간 합산 |
| `V2HubCanvasSetupEditor.cs` | 한 번 클릭으로 Hub 전체 자동 생성 |
| `V2DataSetupEditor.cs` | 3 캐릭터·9 어빌리티·15 무기 강화 SO 일괄 생성 |

### 인게임 연결 — 진행 중

| # | 작업 | 상태 | 비고 |
|---|---|---|---|
| 6 | `AbilityData` SO + `IAbilityRunner` 9종 | ❌ | SO만 완료, 런타임 실행기 0/9 |
| 7 | 보석 드랍/채집 (`GemDrop`/`GemPickup`) | ❌ | StatDisplayUI에 합산값 표시되지만 인게임 미연결 |
| 8 | `mineTarget` 승리 조건 | ❌ | 데이터만 있음, MachineController 미연결 |
| 9 | 회전톱날(`SawWeapon`) | ✅ | `Scripts/Weapon/Saw/SawWeapon.cs` + 블레이드 프리펩 빌더 + WeaponPanel 슬롯. 상세 [WeaponUnlockUpgradeSystem.md §7](WeaponUnlockUpgradeSystem.md) |
| 10 | `GoogleSheetsImporter` 4시트 확장 | ❌ | 우선순위 낮음 |

### v2 동시 발동 아키텍처 전환 (2026-04-19)

회전톱날 구현과 함께 단일-장착 모델을 **v2 동시 발동**으로 전환:
- `WeaponSwitcher.cs` / `AimChargeUI.cs` 제거
- `AimController`에서 `_currentWeapon`·`EquipWeapon`·`CooldownProgress`·`IsReady` 제거 — 에임 위치·범위·머신 참조만 제공
- 모든 무기(Sniper/Bomb/Gun/Laser/Saw)가 자체 `Update()`에서 `TryFire(_aimController)` 호출 — v2.html의 `tickSniper/tickBomb/…tickSaw` 병렬 실행과 동일
- 해금 무기 전체가 매 프레임 병렬 동작 (슬롯 전환 없음)

### v2.html 비교: SO 값 정렬

| ID | 비용 schedule | maxLv | 효과 |
|---|---|---|---|
| `mine_speed` | 광석 [80,160,280,440,640] | 5 | 초당 채굴 +2 |
| `mine_target` | 광석 [100,200,350,550,800] | 5 | 목표량 +50 |
| `excavator_hp` | 광석 [60,130,230,370,540] | 5 | 최대 체력 +30 |
| `excavator_armor` | 광석 [150,300,500] | 3 | 받는 피해 -15% |
| `gem_drop` | 보석 [15,30,50,75,105] | 5 | 보석 등장 +2% |
| `gem_speed` | 보석 [10,22,38,58,82] | 5 | 채집 속도 +20% |

---

## 9. 변경 이력

| 날짜 | 내용 |
|---|---|
| 2026-04-17 | 초안 작성 (v2.html 분석 기반) |
| 2026-04-18 | Hub UI 5개 패널 + 데이터 v2 정렬 완료, 인게임 연결만 남음 |
| 2026-04-19 | 회전톱날 인게임 구현 + v2 동시 발동 아키텍처 전환 (WeaponSwitcher 제거) |

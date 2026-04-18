# v2 Hub UI 구현 트러블슈팅 타임라인

> 작성: 2026-04-18
> 범위: v2.html 프로토타입 → Unity Hub Canvas 이식 (Title 씬 HubPanel)
> 관련: [V2_IntegrationPlan.md](V2_IntegrationPlan.md), [v2.html](v2.html)

uGUI 다단 레이아웃을 동적으로 빌드하면서 만난 문제와 해결책을 시간순으로 정리. 같은 패턴이 다른 패널에서 재발할 가능성이 높아 재사용 가능한 규칙 형태로 정리한다.

---

## 1. 첫 진입 시 캐릭터 선택 배경이 작게 잡힘

### 증상
`CharacterSelectSubPanel`이 처음 열릴 때 배경 박스가 카드 3장 영역보다 작게 그려짐. 카드를 한 번 클릭(임의 입력)하면 정상 크기로 부풀어 오름.

스크린샷: `screenshot/upgrade_00.png` (작음) → `upgrade_01.png` (정상)

### 원인
4단 ContentSizeFitter 캐스케이드:

```
Card(CSF) → Content(CSF) → SubPanel(CSF) → Column(CSF)
```

TMP 메시 빌드와 LayoutRebuilder가 같은 프레임에 끝나지 않음. 부모 CSF는 자식의 미완성 preferredHeight(보통 0 또는 잘못된 값)를 그대로 읽고 자기 크기를 계산. 다음 입력이 들어오는 순간 LayoutRebuilder가 다시 돌면서 정상 크기로 복구됨.

### 해결
`CharacterSelectUI.OnEnable`에서 코루틴으로 leaf-first 강제 리빌드:

```csharp
private IEnumerator ForceRebuildNextFrame()
{
    yield return null;
    yield return new WaitForEndOfFrame();
    var rt = transform as RectTransform;
    var rts = rt.GetComponentsInChildren<RectTransform>(true);
    for (int i = rts.Length - 1; i >= 0; i--)  // 역순 = leaf-first
        LayoutRebuilder.ForceRebuildLayoutImmediate(rts[i]);
}
```

**핵심**: 자식부터(역순) 강제 리빌드해야 부모가 올바른 preferred 값을 읽는다.

참조: `Assets/_Game/Scripts/OutGame/CharacterSelectUI.cs`

---

## 2. 3컬럼 너비가 동일하지 않음 (가운데만 큼)

### 증상
`BodyScrollView`의 좌/중/우 컬럼에 `flexibleWidth = 1:1:1`로 줬는데 가운데(WeaponShop) 컬럼만 눈에 띄게 폭이 큼.

### 원인
컬럼 안의 자식 GridLayoutGroup(WeaponShopSubPanel 초기 구현)의 `cellSize.x × N` 합계가 컬럼의 `LayoutElement.preferredWidth`로 누수됨. HLG는 "preferredWidth가 명시된 자식이 우선, 남은 공간을 flexibleWidth 비율로 분배"하는 규칙이라 가운데 컬럼이 더 큰 preferredWidth를 차지하게 됨.

### 해결
`CreateColumn` LayoutElement에 `preferredWidth = 0` **명시**:

```csharp
var le = col.AddComponent<LayoutElement>();
le.flexibleWidth = flexibleWidth;
le.minWidth = 200;
le.preferredWidth = 0;  // 자식 누수 차단 — 누락 시 -1(자동) 그대로 누수됨
```

**핵심**: HLG 균등분배 컬럼은 `flexibleWidth=1` + `preferredWidth=0` + `minWidth=N` 3종 세트가 표준.

참조: `V2HubCanvasSetupEditor.cs::CreateColumn` (446줄), `CreateWeaponCardColumn` (498줄)

---

## 3. 무기 카드가 컬럼 배경을 넘어감 (GridAutoCellWidth 타이밍)

### 증상
WeaponShopSubPanel을 GridLayoutGroup + GridAutoCellWidth(런타임에 cellSize 계산하는 헬퍼)로 짰을 때, 카드(저격총·폭탄 등) 내용이 컬럼 배경 밖으로 삐져나옴.

스크린샷: `screenshot/upgrade_03.png`

### 원인
`GridAutoCellWidth.OnRectTransformDimensionsChange`에서 부모 너비를 읽어 cellSize 갱신 — 그런데 부모 너비 자체가 첫 프레임에 임시값(0 또는 1920)으로 잡혀 있어 cellSize 계산이 어긋남. 게다가 GLG가 부모 폭을 모를 때 자기 contentSize를 임의로 잡아 위로 누수(원인 #2와 동일 메커니즘).

### 해결
GridLayoutGroup 접근 자체를 폐기. WeaponShopSubPanel을 **HLG(2열) + 각 컬럼 VLG** 구조로 재설계:

```
WeaponShopSubPanel/Content (HLG, childForceExpandWidth=true)
  ├── Col1 (VLG + CSF, flexibleWidth=1, preferredWidth=0)
  └── Col2 (VLG + CSF, flexibleWidth=1, preferredWidth=0)
```

카드는 단순히 컬럼 너비를 그대로 받아 자동으로 맞춰짐. WeaponShopUI가 런타임에 5장 카드를 두 컬럼에 교차 분배.

참조: `V2HubCanvasSetupEditor.cs::CreateWeaponShopSubPanel`, `CreateWeaponCardColumn`

---

## 4. 입력 한 번마다 무기 카드가 들쭉날쭉

### 증상
통화 변경, 업그레이드 구매, 캐릭터 변경 등 어떤 이벤트가 들어와도 WeaponShop의 카드 배치·정렬·배경 사이즈가 한 프레임 출렁임. 사용자 표현: "UI 입력이 있을 때마다 무기 & 강화 아이템들이 들쭉날쭉 변하고 배경 사이즈도 다시 변경되고 정렬도 이상해짐."

### 원인
초기 구현이 이벤트 핸들러 안에서 카드 5장을 전부 `Destroy` → `Instantiate`로 다시 만들었음. 1프레임 동안 빈 컨테이너가 노출 → HLG가 빈 폭으로 한 번 정렬 → 새 카드 5장이 추가되면서 다시 정렬. 이 깜빡임이 이벤트마다 반복됨.

### 해결
**Patch-pattern**으로 전환. 데이터 변경 ≠ GameObject 변경.

1. inner view 클래스로 컨트롤 참조 캐싱:
   ```csharp
   class CardView { public TMP_Text Name; public Image Bg; public Button UnlockBtn; ... }
   class UpgradeRowView { public TMP_Text Cost; public Button BuyBtn; ... }
   ```
2. `_builtOnce` 가드 + `BuildOnce()` — 카드 5장과 행을 단 한 번만 생성, 컨트롤을 view에 저장.
3. 이벤트 핸들러는 `UpdateAll()`만 호출 — 텍스트·색상·`button.interactable`만 패치, GameObject 생성/파괴 금지.
4. 잠금↔해제 같은 구조 전환은 Body 서브컨테이너만 교체 (`BodyIsUnlocked` 플래그 비교 후).

**핵심**: 이벤트 기반 UI는 항상 "build once + cached views". 데이터가 바뀌어도 GameObject 트리는 정적으로 유지.

참조: `Assets/_Game/Scripts/OutGame/WeaponShopUI.cs` (CardView, UpgradeRowView, BuildOnce, UpdateAll)

---

## 5. AbilityShopSubPanel이 아예 표시되지 않음

### 증상
`AbilityShopUI`를 부착했는데 `AbilityShopSubPanel`이 화면에 안 나타남. 인스펙터로 봐도 SubPanel은 활성, Content도 활성인데 그려지지 않음.

### 원인
`AddVerticalItemContainer`(빈 Content에 VLG+CSF를 부착하는 헬퍼)가 초기에 `childControlHeight = false`였음. 자식 카드의 CSF가 아무리 preferredHeight를 계산해도 부모 VLG가 sizeDelta를 갱신하지 않음 → Content 자체 높이 0 → CSF가 0을 위로 보고 → SubPanel 높이 0 → 안 보임.

### 해결
`childControlHeight = true` + `childForceExpandHeight = false`:

```csharp
static void AddVerticalItemContainer(GameObject content)
{
    var vl = content.AddComponent<VerticalLayoutGroup>();
    vl.spacing = 6;
    vl.childControlWidth = true;
    vl.childControlHeight = true;        // ← 자식 CSF preferredHeight 반영
    vl.childForceExpandWidth = true;
    vl.childForceExpandHeight = false;   // ← 자식이 자체 preferredHeight 유지
    var fitter = content.AddComponent<ContentSizeFitter>();
    fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
}
```

**핵심**: `childControlHeight=true` 없이는 CSF 캐스케이드가 끊긴다. `childForceExpandHeight=false`는 자식이 자기 preferredHeight를 유지하게 함.

참조: `V2HubCanvasSetupEditor.cs::AddVerticalItemContainer` (952줄)

---

## 6. AbilityShop 캐릭터 전환 시 카드 겹침

### 증상
캐릭터를 빅터→사라로 바꾸는 순간, 한 프레임 동안 카드가 6장(빅터 3 + 사라 3) 동시에 보이며 컬럼 폭이 잠깐 두 배로 잡힘.

### 원인
캐릭터 전환 핸들러가 옛 카드 3장을 `Destroy`로 보내고 새 카드 3장을 `Instantiate`. Destroy는 같은 프레임 끝에 처리되지만 layout 계산은 그 사이 한 번 돌아 6장 모두 활성으로 보고 정렬됨.

### 해결
2단계 처리:

1. **SetActive(false) 먼저** — 옛 카드 즉시 숨겨 같은 프레임 layout 계산에서 빠지게:
   ```csharp
   foreach (var old in _cards) { old.SetActive(false); Destroy(old); }
   ```
2. **`RebuildIfCharacterChanged`** — 마지막으로 빌드한 캐릭터 ID와 비교, 같으면 빌드 자체 스킵. 불필요한 churn 차단.

참조: `Assets/_Game/Scripts/OutGame/AbilityShopUI.cs::RebuildIfCharacterChanged`

---

## 7. GemUpgradeSubPanel이 표시되지 않음

### 증상
GemUpgradeUI를 부착하고 데이터 SO도 만들어 뒀는데 보석 채집 패널이 빈 헤더만 나옴.

### 원인
씬에 이미 존재하던 `UpgradeManager` 인스턴스의 `_availableUpgrades` 리스트에 v2 신규 SO(`gem_drop`, `gem_speed`, `mine_target`)가 누락. 메뉴 재실행으로도 자동 갱신되지 않음.

### 해결
에디터 메뉴에 `EnsureUpgradeManagerLinks()` 추가 — `Assets/_Game/Data/Upgrades` 폴더의 모든 `UpgradeData` SO를 `AssetDatabase.FindAssets`로 끌어와 SerializedObject로 강제 동기화. 씬에 매니저가 없으면 새로 생성.

```csharp
const string dir = "Assets/_Game/Data/Upgrades";
string[] guids = AssetDatabase.FindAssets("t:UpgradeData", new[] { dir });
// listProp.arraySize = guids.Length; 후 모두 채워넣기
```

**핵심**: 디자이너가 SO만 추가하면 메뉴 재실행으로 즉시 반영되도록 자동 발견 패턴 적용.

참조: `V2HubCanvasSetupEditor.cs::EnsureUpgradeManagerLinks` (775줄)

---

## 8. 현재 스탯의 HP가 230으로 표시 (130이어야 함)

### 증상
StatDisplayUI의 "최대 체력" 행이 캐릭터 base(100) + 강화 1단계(30) = 130이어야 하는데 230으로 표시.

### 원인
`UpgradeData._baseValue`에 base(100)을 넣어 두고, UI에서도 `캐릭터.MaxHp + UpgradeManager.GetTotalBonus(MaxHealth)` 식으로 계산 → base가 두 번 더해짐.

### 해결
**규약 확립**: SO `_baseValue = 0`, UI가 base 합산 책임.

| SO | _baseValue | valuePerLevel |
|---|---|---|
| `excavator_hp` | 0 | 30 |
| `mine_speed` | 0 | 2 |
| `mine_target` | 0 | 50 |

UI가 `캐릭터.기본값 + UpgradeManager.GetTotalBonus(타입)`로 계산. SO는 "강화로 추가되는 양"만 책임.

참조: `Assets/_Game/Data/Upgrades/*.asset`, `Assets/_Game/Scripts/OutGame/StatDisplayUI.cs`

---

## 9. 서브패널 사이 간격이 이상하게 벌어짐 (childForceExpandHeight 함정)

### 증상
Column_Left의 첫 서브패널(ExcavatorUpgradeSubPanel)이 컬럼 상단에 붙지 않고 **79.5px 아래로 밀려서** 배치됨. Column_Mid의 WeaponShopSubPanel은 정상적으로 상단에 붙음.

결과적으로 같은 화면 높이에 있어야 할 "무기 & 강화" 패널과 "굴착기 강화" 패널의 Y가 달라 **화면상 "무기 & 강화"만 위로 튀어 나와 보임**.

스크린샷: `screenshot/weapon_03.png.png`

### 증상 시각화

```
기대한 배치 (Upper 정렬):         실제 배치:
┌───────────────┐                  ┌───────────────┐
│ Excavator 194 │                  │               │ ← +79.5 빈 공간
├───────────────┤ (spacing 12)     ├───────────────┤
│     Gem 126   │                  │ Excavator 194 │
├───────────────┤                  ├───────────────┤
│               │                  │               │ ← +159 빈 공간
│               │                  │               │   (spacing 12 포함)
│ 남는 공간 318 │                  ├───────────────┤
│               │                  │    Gem 126    │
│               │                  ├───────────────┤
└───────────────┘                  │               │ ← +79.5 빈 공간
                                   └───────────────┘
Column Height 650                  Column Height 650
Total Content 332                  79.5 + 194 + 12 + 159
Surplus 318 → 바닥              + 126 + 79.5 = 650
                                   (surplus 318이 1:2:1로 분배됨)
```

### 원인 — "고무줄 오해"

Unity `HorizontalOrVerticalLayoutGroup.GetChildSizes` 소스:

```csharp
void GetChildSizes(RectTransform child, int axis,
    bool controlSize, bool childForceExpand,
    out float min, out float preferred, out float flexible)
{
    // ... preferred/min 계산 ...
    if (childForceExpand)
        flexible = Mathf.Max(flexible, 1);  // ← 핵심
}
```

**`childForceExpandHeight=true`일 때 자식의 flexibleHeight를 무조건 ≥1로 클램프.** 이는 단순히 "이 VLG 안에서 자식이 고무줄"을 의미하는 게 아니라, **그 VLG 자체가 상위 LayoutGroup에게 "저는 flex가 있어요"라고 리포트**하는 부작용을 만든다.

### 전체 데이터 흐름 (이번 케이스)

```
Column_Left VLG (Upper Center, childControl=T, forceExpand=F)
 │
 │  Column_Left height = 650
 │  Total preferred = 194 + 12 + 126 = 332
 │  Surplus = 318
 │
 │  자식(Excavator, Gem)의 flexibleHeight 쿼리:
 │  ├─ Excavator VLG (forceExpandHeight=T) → 자식 Flex 1로 클램프
 │  │                                        → Excavator.flex = 2 리포트
 │  └─ Gem VLG (forceExpandHeight=T)        → Gem.flex = 2 리포트
 │
 │  Total Flex = 4 > 0
 │  → surplus를 alignment 대신 flex로 분배!
 │  → multiplier = 318 / 4 = 79.5
 │
 ▼
Excavator: preferred(194) + flex(2)*79.5 = 353 크기로 pos=0 배치
   └ anchoredPos.y = -(0 + 353 * 0.5) = -176.5 ✓
   → Excavator 안의 CSF가 "내 컨텐츠는 194면 충분"이라며 sizeDelta=194로 복원
   → 위치는 -176.5 그대로, 크기만 194
   → Top = -176.5 + 97 = -79.5 (컬럼 top에서 79.5 아래)

pos += 353 + 12 = 365

Gem: preferred(126) + flex(2)*79.5 = 285 크기로 pos=365 배치
   └ anchoredPos.y = -(365 + 285 * 0.5) = -507.5 ✓
   → CSF가 sizeDelta=126으로 복원
   → Bottom = -507.5 - 63 = -570.5, 컬럼 바닥(-650)까지 79.5 남음
```

수치가 모두 정확히 맞아떨어지는 것이 이 메커니즘의 결정적 증거.

### 왜 Column_Mid는 정상처럼 보였나

Column_Mid는 WeaponShop(344) + spacing(12) + Stat(≈294) ≈ 650으로 **surplus가 거의 0**. Surplus가 0이면 flex 분배도 0 → 위치 왜곡 없음. 그래서 같은 메커니즘에 노출됐지만 증상이 숨어 있었다.

### 해결 — 한 줄

`CreateSubPanel`에서 VLG 생성 시 **명시적으로 false**:

```csharp
var vl = panel.AddComponent<VerticalLayoutGroup>();
// ...
vl.childForceExpandHeight = false;  // Unity 기본값 true를 덮어씀
```

이 한 줄로:
- 서브패널 VLG가 자식 flex를 부풀리지 않음
- 서브패널 자체의 flexibleHeight = 0
- 상위 Column VLG가 surplus를 alignment로 처리 (UpperCenter → 위쪽 정렬)
- 남는 공간은 컬럼 하단으로 깔끔하게 몰림

### 왜 이걸 빨리 못 찾았나

1. **Unity 공식 문서가 모호함** — `childForceExpand*`를 "자식이 남는 공간을 채우도록 강제"로만 설명. "그 VLG 자체가 상위에 flex를 리포트하게 된다"는 파급 효과는 문서 어디에도 없음.
2. **기본값이 함정** — Unity가 LayoutGroup 생성 시 기본을 `true`로 둬서, `AddComponent` 후 속성 명시 안 하면 켜져 있음. 프리팹 인스펙터로 UI 만드는 사람은 체크박스를 한 번이라도 봤겠지만, 코드 생성에서는 놓치기 쉬움.
3. **증상이 간접적** — **부모 VLG × 자식 VLG × ContentSizeFitter** 3개가 모두 중첩돼야 드러남. 조건을 하나만 빼도 안 나타남. Column_Mid처럼 surplus가 0에 가까운 컬럼에서도 숨어 있음.
4. **숫자가 퍼즐** — 79.5라는 값이 `(650 - 332)/4`에서 나온다는 걸 알아내려면 Unity 소스의 `GetChildSizes` → `Mathf.Max(flexible, 1)` 한 줄과 surplus/flex 분배 공식을 정확히 맞춰봐야 함. 직관적으로 보이지 않음.

### 재사용 규칙

**동적으로 LayoutGroup을 생성할 때는 `childForceExpandWidth/Height`를 항상 명시적으로 `false`로 꺼라.** 자식을 진짜 고무줄로 만들고 싶을 때만 `true`로 켜고, 그때도 상위 LayoutGroup에 미치는 파급을 의식하라.

참조: `V2HubCanvasSetupEditor.cs::CreateSubPanel` (childForceExpandHeight=false 명시 추가), Unity 소스 `HorizontalOrVerticalLayoutGroup.cs::GetChildSizes`

---

## 재사용 가능한 규칙 정리

uGUI 동적 빌드 시 항상 점검할 체크리스트:

| 규칙 | 적용처 | 누락 시 증상 |
|---|---|---|
| **OnEnable에서 leaf-first ForceRebuild** | 3단 이상 CSF 중첩 | 첫 프레임만 배경 작게 |
| **VLG는 `childControlHeight=true` + `childForceExpandHeight=false`** | 빈 Content에 VLG 부착 시 | 패널 자체가 안 보임 |
| **HLG 균등 컬럼은 LE `preferredWidth=0` 강제** | flexibleWidth로 분배할 때 | 한 컬럼만 비대 |
| **이벤트 UI는 BuildOnce + 캐시된 view 패치** | 데이터 변경 핸들러 | 입력마다 정렬·배경 출렁임 |
| **카드 교체 시 SetActive(false) 후 Destroy** | 컬렉션 갱신 | 한 프레임 6장 동시 표시 |
| **SO는 강화량만, base는 캐릭터/UI 책임** | 스탯 합산 | base 이중 가산 |
| **에디터 메뉴는 폴더 자동 스캔으로 SO 동기화** | 매니저 SO 리스트 | 디자이너가 추가해도 안 보임 |
| **`childForceExpandWidth/Height`는 명시적으로 false** | 동적 VLG/HLG 생성 시 | 상위에 flex 부풀려 리포트 → 간격 벌어짐 |

---

## 관련 메모리 (자동 메모리 시스템)

이 트러블슈팅의 핵심 패턴은 다음 메모리에도 저장됨:
- `feedback_ugui_csf_cascade.md`
- `feedback_ugui_patch_pattern.md`
- `feedback_ugui_column_width_leak.md`

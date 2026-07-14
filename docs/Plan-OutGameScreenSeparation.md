# 아웃게임 화면 분리 및 성장 UI 구현 계획

> 상태: 구현 전 작업 계획서
> 최종 확인: 2026-07-14
> 대상 씬: `Assets/_Game/Scenes/Title.unity`
> 기준 해상도: 1920×1080
> 구현 기준: 현재 클라이언트 코드와 ScriptableObject 데이터

## 0. 문서 목적

현재 `Title.unity`의 `HubPanel`에 한꺼번에 배치된 성장 UI를 현재 클라이언트 데이터에 맞게 재설계하고, 승인된 **클라이언트 기준 UI 레이아웃 목업**을 기준으로 실제 화면을 분리한다.

```text
TitleLandingPanel
├── UPGRADES
│   ├── 굴착기 강화
│   ├── 보석 채집 강화
│   └── 무기 해금 및 강화
├── CHARACTER
│   ├── 캐릭터 선택
│   └── 선택 캐릭터의 고유 장비 해금
└── CRAFTING
    └── 향후 아이템 제작용 예약 화면
```

제공된 이미지는 스타일 참고 자료이며 구현 목표 레이아웃이 아니다. 구현 전에 현재 클라이언트의 실제 콘텐츠를 반영한 **UI 레이아웃 목업**을 별도로 제작하고 사용자 승인을 받아야 한다. 이 문서 수정 단계에서는 이미지를 생성하지 않지만, UI 레이아웃 목업 제작은 전체 구현 계획의 필수 단계다.

---

## 1. 이미지 사용 원칙

### 1.1 제공 자료

| 파일 | 역할 |
|---|---|
| `codex-clipboard-09ee3a92-7929-48ce-a90a-8953fa8a594f.png` | UPGRADES 기획 메모와 스타일 참고 |
| `codex-clipboard-31b2d9e7-1bb4-4f06-9a48-ed2751ecb8f5.png` | CHARACTER 기획 메모와 스타일 참고 |
| `타이틀.png` | 타이틀 아트 방향과 금속/SF 스타일 참고 |
| `굴착기.png` | UPGRADES 화면의 시각적 밀도와 스타일 참고 |
| `캐릭터.png` | CHARACTER 화면의 카드/패널 스타일 참고 |

위 파일은 현재 프로젝트 외부의 사용자 로컬 경로에 있다. 스타일을 파악하는 용도로만 사용하며 구현 레이아웃이나 실제 콘텐츠 수를 그대로 복사하지 않는다. 런타임 배경이나 UI 조각으로도 사용하지 않는다. 저장소에 참고 이미지를 보존할 필요가 생기면 라이선스와 용량을 확인한 뒤 별도 작업으로 처리한다.

### 1.2 스타일 참고 이미지에서 가져올 것

- 선택됨, 잠김, 구매 가능, 재화 부족, 최대 레벨 상태의 시각적 우선순위
- `UPGRADES / CHARACTER / CRAFTING` 하단 내비게이션
- 금속 패널, 청록색 선택 강조, 녹색 완료, 주황색 비용, 비활성 잠금 표현
- SF 산업 장비 분위기, 패널 깊이, 테두리, 발광 강도의 방향성

### 1.3 스타일 참고 이미지에서 결정하지 않을 것

- 클라이언트에 없는 세 번째 재화, 크레딧, 재료 종류
- 실제 데이터에 없는 캐릭터 5종, 캐릭터 구매 가격, 캐릭터 잠금 시스템
- 캐릭터 장비 레벨, 패시브 기어 슬롯, 장비 강화 수치
- 클라이언트에 없는 굴착기/무기 스탯과 가상의 트리 선행 조건
- 목업 안의 영어 명칭, 임시 숫자, 임시 설명
- 목업 이미지를 잘라 만든 버튼, 카드, 프리뷰 이미지
- 패널의 정확한 위치, 크기, 개수, 화면별 정보 우선순위

### 1.4 클라이언트 기준 UI 레이아웃 목업 제작 요구사항

UI 레이아웃 목업은 최종 캐릭터·굴착기 아트를 제작하는 작업이 아니다. 현재 클라이언트 구조를 기준으로 실제 버튼, 목록, 패널, 상태 정보가 어디에 배치되는지 보여주는 1920×1080 화면 설계 이미지다. 실제 아트가 없는 영역은 이름이 표시된 사각형 placeholder를 사용한다.

이 목업이 승인된 뒤에 Unity 레이아웃 구현을 시작한다.

| UI 레이아웃 목업 | 반드시 포함할 실제 콘텐츠 |
|---|---|
| `Title` | 현재 타이틀 요소, `UPGRADES / CHARACTER / CRAFTING`, 설정, 종료 |
| `UPGRADES 단일 화면` | 중앙 굴착기 프리뷰, 굴착기 강화 4종, 보석 강화 2종, 무기 5종의 해금/강화 |
| `CHARACTER` | Victor/Sara/Jinus 3종, 선택 정보, 해당 고유 장비 3종 해금 |
| `CRAFTING` | 아이템 제작 `준비 중` 상태 |

제작 시 권장 저장 위치와 파일명:

```text
docs/image/outgame-layout/
├── Layout-Title.png
├── Layout-Upgrades.png
├── Layout-Character.png
└── Layout-Crafting-Placeholder.png
```

이 파일들은 구현 승인을 위한 설계 산출물이며 Unity `Assets` 폴더에 넣지 않는다.

UI 레이아웃 목업에는 다음 상태 변형도 표시한다.

- 선택됨
- 해금/강화 가능
- 재화 부족
- 선행 조건 부족
- 해금 완료
- 최대 레벨
- 프리뷰/초상화 아트 미확정 placeholder

목업 승인 게이트:

1. 실제 클라이언트의 콘텐츠 수, 명칭, 재화, 해금 조건과 일치한다.
2. 화면별 정보 우선순위와 클릭 흐름이 확정된다.
3. 1920×1080 기준 패널 경계와 최소 클릭 영역이 확인된다.
4. 목업에만 존재하고 데이터에는 없는 기능을 명확히 제외한다.
5. 사용자 승인 전에는 Unity 계층과 좌표를 최종 확정하지 않는다.

UI 레이아웃 목업에서 하지 않는 것:

- 신규 캐릭터 일러스트 제작
- 신규 굴착기·무기 아트 제작
- 최종 배경 아트와 VFX 제작
- 목업 이미지를 런타임 UI 에셋으로 사용
- 데이터에 없는 기능이나 수치를 디자인 편의를 위해 추가

---

## 2. 확정된 화면 경계

| 화면 | 포함 기능 | 포함하지 않는 기능 |
|---|---|---|
| `UPGRADES` | 굴착기 강화 4종, 보석 채집 강화 2종, 무기 5종 해금, 무기별 강화 3종 | 캐릭터 선택, 고유 장비, 아이템 제작 |
| `CHARACTER` | 캐릭터 3종 선택, 선택 캐릭터의 고유 장비 3종 해금 | 캐릭터 구매, 고유 장비 반복 강화, 무기 강화 |
| `CRAFTING` | 현재는 `준비 중` 안내 | 무기 해금/강화 및 임의의 제작 기능 |

`CRAFTING`은 무기 강화 화면의 다른 이름이 아니다. 향후 레시피와 재료를 사용하는 아이템 제작 시스템을 위한 예약 화면이다.

---

## 3. 현재 클라이언트 기준점

### 3.1 현재 진입 구조

`TitleLandingSetupEditor`는 하단의 세 버튼을 다음 메서드에 연결한다.

| 버튼 | 현재 호출 | 현재 결과 |
|---|---|---|
| `UpgradesButton` | `TitleUI.ShowUpgradeHubPanel()` | 통합 `HubPanel`을 열고 굴착기 강화 첫 버튼에 포커스 |
| `CharacterButton` | `TitleUI.ShowCharacterHubPanel()` | 같은 `HubPanel`을 열고 캐릭터 첫 버튼에 포커스 |
| `CraftingButton` | `TitleUI.ShowCraftingHubPanel()` | 같은 `HubPanel`을 열고 무기 첫 버튼에 포커스 |

현재 `HubController.Focus()`는 화면을 전환하지 않는다. `CharacterSelectSubPanel`, `ExcavatorUpgradeSubPanel`, `GemUpgradeSubPanel`, `WeaponShopSubPanel`, `AbilityShopSubPanel`, `StatDisplaySubPanel`이 동시에 존재하는 상태에서 EventSystem 포커스만 옮긴다.

### 3.2 현재 화면 계층

```text
HubPanel
├── TopBar
├── CharacterSelectSubPanel
└── BodyScrollArea
    └── Content
        ├── Column_Left
        │   ├── ExcavatorUpgradeSubPanel
        │   └── GemUpgradeSubPanel
        ├── Column_Mid
        │   ├── WeaponShopSubPanel
        │   └── StatDisplaySubPanel
        └── Column_Right
            └── AbilityShopSubPanel
```

이 3열 통합 구조를 유지한 채 일부 패널만 숨기는 방식은 사용하지 않는다. 비활성 레이아웃이 폭이나 높이를 점유하지 않도록 화면 루트를 분리한다.

### 3.3 재사용할 코드

| 파일 | 현재 책임 | 개편 후 원칙 |
|---|---|---|
| `TitleUI.cs` | 타이틀/허브/옵션 패널 전환 | 화면 진입과 옵션 복귀 상태 확장 |
| `HubController.cs` | 재화, 공통 버튼, 단순 포커스 | `HubScreen` 상태와 화면 전환 책임 추가 또는 별도 컨트롤러로 분리 |
| `UpgradeManager.cs` | 굴착기/채집 강화 구매와 레벨 | 그대로 authoritative 유지 |
| `WeaponUpgradeManager.cs` | 무기별 강화 구매와 레벨 | 그대로 authoritative 유지 |
| `DataManager.cs` | 재화, 캐릭터 선택, 무기/고유 장비 해금 | 그대로 authoritative 유지 |
| `ExcavatorUpgradeUI.cs` | 굴착기 강화 행 | 4개 강화 행을 동시에 표시하는 패널로 확장 또는 어댑터 추가 |
| `GemUpgradeUI.cs` | 보석 강화 행 | 2개 강화 행을 동시에 표시하는 패널로 확장 또는 어댑터 추가 |
| `WeaponShopUI.cs` | 5종 카드와 강화 행 | 5개 무기 패널을 동시에 배치하고 기존 구매 로직 재사용 |
| `CharacterSelectUI.cs` | 3캐릭터 선택 카드 | 좌측 목록과 중앙 선택 정보로 재배치 |
| `AbilityShopUI.cs` | 선택 캐릭터별 3개 고유 장비 해금 | CHARACTER 화면의 고유 장비 영역으로 재배치 |
| `StatDisplayUI.cs` | 통합 스탯 요약 | 화면 문맥에 맞춰 굴착기/캐릭터 표시 분리 |

### 3.4 데이터가 실제로 제공하는 범위

#### 굴착기 및 채집 강화

| 영역 | Upgrade ID | 최대 레벨 | 효과 | 재화 |
|---|---|---:|---|---|
| 굴착기 | `excavator_hp` | 5 | 최대 체력 +30/레벨 | 광석 |
| 굴착기 | `excavator_armor` | 3 | 받는 피해 감소 +15%/레벨 | 광석 |
| 굴착기 | `mine_speed` | 5 | 초당 채굴 +2/레벨 | 광석 |
| 굴착기 | `mine_target` | 5 | 목표 채굴량 +50/레벨 | 광석 |
| 보석 채집 | `gem_drop` | 5 | 출현 확률 +2%p/레벨 | 보석 |
| 보석 채집 | `gem_speed` | 5 | 채집 속도 +20%/레벨 | 보석 |

이 6종에는 현재 선행 업그레이드 데이터가 없다. 연결선과 트리는 분류와 진행도를 표현할 수 있지만 실제 잠금 관계를 만들어서는 안 된다.

#### 무기 해금

| 순서 | Weapon ID | 표시명 | 해금 조건 | 비용 |
|---:|---|---|---|---:|
| 1 | `sniper` | 저격총 | 기본 해금 | 0 |
| 2 | `bomb` | 폭탄 | 없음 | 보석 30 |
| 3 | `gun` | 기관총 | 폭탄 해금 | 보석 20 |
| 4 | `laser` | 레이저 | 기관총 해금 | 보석 40 |
| 5 | `saw` | 회전톱날 | 레이저 해금 | 보석 40 |

각 무기는 `WeaponUpgradeData` 3개를 갖는다. 데미지, 쿨타임/연사, 범위/탄창/재장전/슬로우 중 무기별 실제 세 항목만 표시한다. 비용은 광석과 보석을 동시에 사용할 수 있으며 `WeaponUpgradeManager.GetNextCost()` 결과를 그대로 표시한다.

#### 캐릭터와 고유 장비

| 캐릭터 | 선택 가능 여부 | 고유 장비 해금 순서 |
|---|---|---|
| Victor | 항상 선택 가능 | 네이팜 탄 → 화염방사기, 네이팜 탄 → 폭발 지뢰 |
| Sara | 항상 선택 가능 | 블랙홀 → 충격파 → 반중력 메테오 |
| Jinus | 항상 선택 가능 | 드론 포탑 → 채굴 드론 → 드론 거미 |

- 캐릭터 자체의 잠금, 구매 비용, 레벨 데이터는 없다.
- 고유 장비는 각각 보석 30으로 해금한다.
- 고유 장비는 해금 여부만 저장하며 반복 강화 레벨은 없다.
- `AbilityData.Description`은 현재 9개 모두 비어 있어 상세 설명을 표시하려면 데이터 작성이 선행돼야 한다.
- `CharacterData.Portrait`는 현재 3개 모두 비어 있다.
- 현재 프로젝트에서 해석 가능한 `MachineData`는 `Machine_Default.asset` 하나이며 `Prefab`과 `Icon`이 비어 있다.
- Sara/Jinus의 `DefaultMachine` 참조는 현재 저장소에서 대상 GUID를 찾을 수 없으므로 구현 전 정합성 확인이 필요하다.

---

## 4. 목표 화면 상태와 내비게이션

```csharp
public enum HubScreen
{
    Upgrades,
    Character,
    Crafting
}
```

### 4.1 전환 규칙

| 입력 | 결과 |
|---|---|
| 타이틀 `UPGRADES` | 굴착기/보석/무기 성장이 모두 보이는 `HubScreen.Upgrades` 단일 화면 |
| 타이틀 `CHARACTER` | `HubScreen.Character` |
| 타이틀 `CRAFTING` | `HubScreen.Crafting` 준비 중 화면 |
| 허브 하단 내비게이션 | 해당 화면 루트로 전환 |
| 허브 좌측 상단 뒤로가기 | `TitleLandingPanel` 복귀 |
| 옵션 진입 후 닫기 | 옵션 진입 전 `HubScreen` 복원 |
| 세션 결과의 업그레이드 이동 | `HubScreen.Upgrades` |

한 번에 `ScreenContainer`의 자식 하나만 활성화한다. 화면 전환 때 패널을 생성하거나 파괴하지 않는다.

---

## 5. 타이틀 화면 구현 기준

`타이틀.png`는 스타일 참고 자료다. Title UI 레이아웃 목업은 현재 `TitleLandingSetupEditor`와 `Assets/_Game/Sprites/UI/Title/Generated`가 구현한 요소를 기준으로 다시 구성한다. 현재 타이틀의 기능과 사용 가능한 에셋을 버리지 않되, 정확한 배치는 UI 레이아웃 목업 승인 후 확정한다.

### 유지할 요소

- 우주 배경, 로고, 굴착기 중심 이미지
- 우측 `SETTINGS`, `EXIT GAME`
- 중앙 `PRESS ANY KEY TO START`
- 하단 `UPGRADES / CHARACTER / CRAFTING`
- `TitleUI.Update()`의 New Input System 키 입력 처리

### 수정할 요소

- 세 하단 버튼이 같은 허브의 포커스만 바꾸지 않고 각각 실제 화면을 열도록 연결
- `CRAFTING` 클릭 시 무기 상점이 아니라 준비 중 화면 표시
- 허브에서 돌아오면 타이틀 랜딩 상태와 버튼 포커스 복원
- 타이틀 셋업 재실행 시 새 화면 연결을 덮어쓰지 않도록 `TitleLandingSetupEditor` 갱신

---

## 6. UPGRADES 화면 구현 기준

### 6.1 공통 골격

UPGRADES는 내부 탭이나 별도 상세 화면으로 다시 나누지 않는다. 기획 이미지 하단 구현 계획처럼 중앙 굴착기를 기준으로 무기 5종, 굴착기 강화 4종, 보석 채집 2종을 한 화면에 동시에 배치한다. 사용자는 화면 이동 없이 각 패널에서 해금과 강화를 바로 진행한다.

```text
UpgradeScreen
├── CommonHeader
│   ├── ScreenTitle (UPGRADES)
│   └── CurrencyBar (Ore / Gem)
├── MachinePreviewPanel (중앙)
├── WeaponScrollPanel (좌측 독립 ScrollRect)
│   └── WeaponContent
│       ├── SniperAccordion
│       ├── BombAccordion
│       ├── GunAccordion
│       ├── LaserAccordion
│       └── SawAccordion
├── GrowthArea
│   ├── ExcavatorUpgradePanel (4행)
│   └── GemUpgradePanel (2행)
├── BackButton (우측 하단)
└── BottomNavigation (공통 내비게이션을 유지하는 경우 화면 최하단)
```

레이아웃 원칙:

- 1920×1080에서 화면 전체는 고정하고 무기 목록만 독립 세로 스크롤을 사용한다.
- 중앙 굴착기 프리뷰는 시각적 중심이며 구매 패널을 가리지 않는다.
- 무기 패널은 중앙 프리뷰 좌측의 독립 `ScrollRect`에 세로로 배치한다.
- 굴착기 강화와 보석 채집은 우측 하단의 독립 목록으로 동시에 표시한다.
- 재화 보유량은 우측 상단에서 항상 확인할 수 있다.
- 각 강화 행에 레벨, 비용, 상태와 클릭 영역을 함께 제공한다.
- `UPGRADES` 내부 화면 이동, 탭 전환, 팝업 진입을 필수 구매 흐름으로 사용하지 않는다.

### 6.2 굴착기 강화 섹션

- `excavator_hp`, `excavator_armor`, `mine_speed`, `mine_target` 네 항목 표시
- 네 행을 한 패널에 항상 표시
- 각 행에 현재 레벨/최대 레벨, 현재→다음 효과, 광석 비용, 강화 버튼 표시
- 패널 요약 수치는 `MachineData + UpgradeManager.GetTotalBonus()`로 계산
- 실제 선행 조건이 없으므로 행 사이에 잠금 관계를 만들지 않음
- 구매는 `UpgradeManager.TryUpgrade(upgradeId)`만 호출

### 6.3 보석 채집 섹션

- `gem_drop`, `gem_speed` 두 항목 표시
- 두 행을 굴착기 강화 패널과 같은 화면에 항상 표시
- 기본 드랍률과 강화 후 드랍률, 기본 수집 시간과 강화 후 수집 시간을 읽기 쉬운 값으로 표시
- 구매 비용은 보석만 표시
- 구매는 동일한 `UpgradeManager`를 사용하고 별도 레벨 저장소를 만들지 않음

### 6.4 무기 & 강화 섹션

- 실제 무기 5종 패널을 같은 화면에 동시에 표시
- 무기 5종은 좌측 독립 스크롤 영역에 해금 순서대로 세로 배치
- 모든 무기 패널은 최초 진입 시 펼친 상태이며, 헤더 화살표로 각각 접거나 다시 펼칠 수 있음
- 펼친 패널에는 해당 무기의 실제 강화 3종을 모두 표시
- 잠긴 무기 패널은 선행 무기와 보석 해금 비용을 본문에 표시
- 잠긴 무기의 강화 3종도 표시하되 구매 입력은 비활성화하고 잠금 상태로 표현
- 각 강화 행에 현재 레벨, 다음 효과, 광석/보석 비용, 강화 버튼 표시
- 해금은 `DataManager.TryUnlockWeapon()`, 강화는 `WeaponUpgradeManager.TryBuy()` 사용
- 접기/펼치기 상태는 UI 세션 상태이며 성장 저장 데이터와 분리
- 잠금↔해금 전환은 기존 노드를 유지한 채 버튼과 색상만 갱신

### 6.5 프리뷰 정책

현재 `MachineData.Prefab/Icon`과 무기 프리뷰 자산이 준비되지 않았다. 따라서 다음 우선순위를 사용한다.

1. 실제 클라이언트용 Sprite가 제공되면 `Image`에 바인딩
2. 실제 프리팹이 준비되면 별도 Preview Camera와 `RenderTexture`를 검토
3. 둘 다 없으면 공용 금속 패널과 이름/아이콘 placeholder로 레이아웃만 완성

목업 이미지를 잘라 중앙 프리뷰로 사용하지 않는다. Preview Camera 방식을 구현할 때는 Unity 6 최신 API를 context7로 확인한 뒤 사용한다.

### 6.6 표시 상태

| 상태 | 표시 | 해당 행/패널 버튼 |
|---|---|---|
| 구매 가능 | 정상 비용색, 다음 효과 강조 | `강화` 또는 `해금` |
| 재화 부족 | 부족 재화만 경고색 | 비활성 |
| 선행 조건 부족 | 잠금과 요구 대상 표시 | 비활성 |
| 최대 레벨 | 완료 표시, 현재 효과만 강조 | `MAX` 비활성 |

---

## 7. CHARACTER 화면 구현 기준

### 7.1 CHARACTER UI 레이아웃 목업에 필요한 정보 골격

```text
CharacterScreen
├── CommonHeader
│   ├── BackButton
│   ├── ScreenTitle (CHARACTER SELECT)
│   └── CurrencyBar (Ore / Gem)
├── CharacterList (Victor / Sara / Jinus)
├── CharacterPreview
│   ├── PortraitSlot
│   ├── Name
│   ├── Title
│   └── Description
├── UniqueEquipmentPanel
│   └── EquipmentCard × 3
├── CharacterStatPanel
├── SelectButton
└── BottomNavigation
```

### 7.2 캐릭터 선택

- 3캐릭터 모두 항상 선택 가능
- 현재 선택 캐릭터는 `SELECTED`, 나머지는 `SELECT` 상태
- 클릭 시 `DataManager.SelectCharacter(characterId)` 호출
- 선택 후 `OnCharacterSelected` 이벤트로 중앙 정보, 테마색, 고유 장비 3개, 스탯만 패치
- 캐릭터 구매/해금 버튼과 가격은 만들지 않음

### 7.3 고유 장비 해금

- 내부 데이터 타입과 클래스명은 `AbilityData`, `AbilityShopUI`를 유지 가능
- 사용자 노출 명칭은 `고유 장비`로 통일
- 선택 캐릭터의 `CharacterId`와 일치하는 3개만 표시
- 해금됨, 해금 가능, 선행 장비 필요, 보석 부족 상태 구분
- 해금은 `DataManager.TryUnlockAbility()`만 호출
- 고유 장비 레벨, 강화 버튼, 장비 슬롯 교체는 만들지 않음

### 7.4 초상화와 설명 정책

- `CharacterData.Portrait`가 비어 있으면 이름, 테마색, 공용 프레임으로 placeholder 표시
- 캐릭터 목업의 인물 이미지를 잘라 사용하지 않음
- 고유 장비 설명은 `AbilityData.Description`을 채운 뒤 표시
- 설명 데이터가 비어 있으면 임의 문구를 하드코딩하지 않고 숨김 또는 `설명 준비 중` 처리

### 7.5 스탯 패널

스타일 참고 이미지의 이동 속도, 치명타, 장비 레벨을 임의 생성하지 않는다. 현재 계산 가능한 다음 값만 사용한다.

- 선택 캐릭터의 `DefaultMachine` 기본 체력과 채굴 속도
- `UpgradeManager`가 적용된 최대 체력, 방어, 채굴 속도, 목표량
- 보석 드랍률과 채집 속도

Sara/Jinus의 `DefaultMachine` 참조가 복구되기 전에는 `Machine_Default` fallback 여부를 코드에서 명시적으로 결정해야 한다.

---

## 8. CRAFTING 예약 화면

```text
CraftingScreen
├── CommonHeader
├── EmptyStatePanel
│   ├── icon_crafting
│   ├── "아이템 제작"
│   └── "준비 중"
└── BottomNavigation
```

- `WeaponShopUI`와 `WeaponUpgradeManager`를 연결하지 않는다.
- 제작 대상, 레시피, 재료, 결과물이 확정되기 전까지 기능 버튼을 만들지 않는다.
- 타이틀 버튼은 유지해 화면 분리와 내비게이션 구조를 검증한다.

---

## 9. 런타임 구조 설계

### 9.1 권장 계층

```text
HubPanel
├── CommonHeader
├── ScreenContainer
│   ├── UpgradeScreen
│   ├── CharacterScreen
│   └── CraftingScreen
└── BottomNavigation
```

공통 헤더와 하단 내비게이션은 화면 전환마다 재생성하지 않는다. 화면 루트만 `SetActive`로 전환한다.

### 9.2 컨트롤러 책임

| 컴포넌트 | 책임 |
|---|---|
| `HubController` 또는 `HubScreenController` | 현재 화면, 화면 전환, 옵션 복귀 상태, 공통 재화, 포커스 |
| `HubNavigationUI` | 하단 버튼 클릭과 선택 비주얼 |
| `UpgradeScreenUI` | 단일 UPGRADES 화면의 무기·굴착기·보석 패널 연결, 공통 재화 및 구매 결과 갱신 조정 |
| `CharacterScreenUI` | 캐릭터 선택 UI와 고유 장비 UI의 화면 수준 연결 |
| 기존 각 UI | 데이터 표시, 구매/해금 호출, 이벤트 기반 부분 갱신 |

### 9.3 갱신 원칙

- 최초 활성화에 `BuildOnce`
- 이후 이벤트에는 cached view의 텍스트, 색상, 버튼 상태만 patch
- 캐릭터 변경 시 고유 장비 3개만 재구성 가능
- 무기 잠금↔해금 시 해당 무기 패널 본문만 재구성 가능
- 화면 전환만으로 카드 목록을 재생성하지 않음
- 레이아웃 강제 갱신은 다음 프레임에 화면 루트 단위로 한 번만 수행

### 9.4 데이터 소유권

UI는 비용, 보너스, 해금 조건을 별도로 계산하거나 저장하지 않는다.

| 데이터 | authoritative 소유자 |
|---|---|
| 광석/보석, 선택 캐릭터, 무기/고유 장비 해금 | `DataManager` |
| 굴착기/채집 강화 레벨과 비용 | `UpgradeManager`, `UpgradeData` |
| 무기 강화 레벨과 비용 | `WeaponUpgradeManager`, `WeaponUpgradeData` |
| 캐릭터-고유 장비 매핑 | `CharacterData`, `AbilityData.CharacterId` |

---

## 10. 에셋 및 스타일 사용 계획

### 재사용

- `Assets/_Game/Sprites/UI/Common/metal_panel.png`
- `card_frame.png`, `list_row.png`, `resource_slot.png`
- `node_frame*.png`, `lock_overlay.png`, `selected_glow.png`
- `metal_button_*.png`, `button_glow_*.png`
- `Assets/_Game/Sprites/UI/Icons/icon_upgrade.png`
- `icon_character.png`, `icon_crafting.png`, `icon_back.png`
- `icon_ore.png`, `icon_gem.png`, `icon_lock.png`, `icon_check.png`
- 기존 캐릭터별 고유 장비 64/128/256px 아이콘
- `D2Coding-Ver1.3` 및 Bold TMP 폰트

### 별도 아트가 필요한 항목

- 실제 굴착기 프리뷰 Sprite 또는 프리뷰 가능한 프리팹
- 실제 무기 프리뷰 Sprite
- Victor/Sara/Jinus 초상화 Sprite
- 필요한 경우 굴착기/보석 강화 전용 아이콘

실제 프리뷰 아트가 준비되지 않아도 UI 레이아웃 목업에는 placeholder 영역을 명확히 표시한다. 목업 승인 후 Unity 레이아웃과 상태 로직을 placeholder로 먼저 구현하고, 실제 아트가 준비되면 바인딩한다.

---

## 11. 수정 대상 파일

### 반드시 함께 수정

| 파일 | 작업 |
|---|---|
| `Assets/_Game/Scenes/Title.unity` | 세 화면 루트와 공통 셸 연결 |
| `Assets/_Game/Scripts/OutGame/TitleUI.cs` | 화면별 진입과 옵션 복귀 상태 |
| `Assets/_Game/Scripts/OutGame/HubController.cs` | 실제 화면 상태 및 전환 |
| `Assets/_Game/Scripts/Editor/V2HubCanvasSetupEditor.cs` | 새 계층 재생성 및 모든 참조 자동 연결 |
| `Assets/_Game/Scripts/Editor/TitleLandingSetupEditor.cs` | 타이틀 버튼의 새 화면 진입 연결 유지 |
| `ExcavatorUpgradeUI.cs`, `GemUpgradeUI.cs` | 한 화면에 항상 노출되는 강화 행 배치 대응 |
| `WeaponShopUI.cs` | 5개 무기 패널 동시 노출 및 패널 내부 해금/강화 대응 |
| `CharacterSelectUI.cs`, `AbilityShopUI.cs` | CHARACTER 목표 레이아웃 대응 |
| `StatDisplayUI.cs` | 화면별 표시 항목 분리 |

### 신규 컴포넌트 후보

- `HubScreenController.cs`
- `HubNavigationUI.cs`
- `UpgradeScreenUI.cs`
- `UpgradeEntryUI.cs`
- `WeaponUpgradePanelUI.cs`
- `CharacterScreenUI.cs`
- `CraftingPlaceholderUI.cs`

기존 클래스 확장으로 책임이 명확하면 신규 파일을 억지로 만들지 않는다.

---

## 12. 구현 단계

### Phase 0 — 선행 정합성 복구

- Unity 6000.5와 충돌하는 embedded `Packages/com.unity.2d.sprite` 문제 해결
- Console 컴파일 오류 0건 확인
- CharacterData의 Portrait와 DefaultMachine 참조 상태 기록
- 현재 저장 데이터로 광석/보석, 강화, 무기 해금, 고유 장비 해금 회귀 기준 캡처

### Phase 1 — 클라이언트 기준 UI 레이아웃 목업 제작 및 승인

- 스타일 참고 이미지와 현재 클라이언트 상태를 분리해 디자인 입력 정리
- Title, UPGRADES 단일 화면, CHARACTER, CRAFTING UI 레이아웃 목업 제작
- 실제 2재화, 3캐릭터, 5무기, 강화/해금 데이터를 목업에 반영
- 프리뷰와 초상화가 없는 영역은 placeholder로 표시
- 선택/잠금/부족/MAX 상태 시트 작성
- 사용자 피드백을 반영하고 최종 UI 레이아웃 목업 승인

완료 기준:

- 스타일 참고 이미지와 구분되는 현재 클라이언트용 UI 레이아웃 목업이 존재한다.
- 모든 화면과 상태의 정보 구조, 패널 비율, 클릭 흐름이 승인됐다.
- 승인 전에는 Unity 레이아웃 좌표 구현으로 넘어가지 않는다.

### Phase 2 — 화면 상태와 공통 셸

- `HubScreen` 상태 구현
- `UpgradeScreen`, `CharacterScreen`, `CraftingScreen` 루트 생성
- 공통 헤더, 재화, 뒤로가기, 하단 내비게이션 구현
- 옵션 복귀와 EventSystem 포커스 복원

### Phase 3 — 타이틀 연결

- 기존 타이틀 비주얼 유지
- 세 버튼을 실제 화면 전환에 연결
- CRAFTING 준비 중 화면 검증
- 셋업 에디터 재실행 결과 검증

### Phase 4 — UPGRADES 단일 화면 골격

- 별도 `UpgradeSection` 상태나 내부 탭 없이 하나의 `UpgradeScreen` 루트로 구성
- 중앙 굴착기 프리뷰, 좌측 스크롤형 무기 5종, 우측 굴착기 강화 4행과 보석 채집 2행을 1920×1080 안에 배치
- 무기 5종은 기본 펼침 상태로 생성하고 각 헤더에서 개별 접기/펼치기 제공
- 각 무기 패널과 강화 행에 현재 상태, 다음 효과, 비용, 해금/강화 버튼을 직접 배치
- 상단 우측 재화 표시와 우측 하단 뒤로가기 동선을 구성
- 화면 진입 후 추가 화면 전환이나 팝업 없이 모든 굴착기 관련 업그레이드를 실행할 수 있도록 연결

### Phase 5 — 굴착기 및 보석 강화 이관

- 4+2 실제 UpgradeData 연결
- 현재→다음 효과 및 비용 표시
- 구매 후 해당 강화 행, 재화, 요약 스탯만 갱신

### Phase 6 — 무기 해금 및 강화 이관

- 실제 5종 해금 체인 연결
- 각 무기 패널에 해당 무기의 실제 강화 3종 연결
- 해금/구매/최대/재화 부족 상태 구현

### Phase 7 — CHARACTER 이관

- 3캐릭터 목록과 선택 정보 구성
- 캐릭터 선택 이벤트 연결
- 선택 캐릭터의 고유 장비 3개와 선행 해금 상태 연결
- 초상화/설명 부재 fallback 구현

### Phase 8 — 스타일 및 안정화

- 기존 UI Style Kit 적용
- 1920×1080 정렬과 축소 해상도 검증
- 마우스/키보드/게임패드 포커스 검증
- BuildOnce/patch 원칙과 레이아웃 흔들림 검증

---

## 13. QA 체크리스트

### 클라이언트 기준 UI 레이아웃 목업

- [ ] 스타일 참고 이미지와 UI 레이아웃 목업이 구분되어 있다.
- [ ] UI 레이아웃 목업이 현재 2재화, 3캐릭터, 5무기 구조와 일치한다.
- [ ] UPGRADES 단일 화면에 굴착기 강화 4종, 보석 채집 2종, 무기 5종이 동시에 설계되어 있다.
- [ ] 중앙 굴착기 프리뷰를 기준으로 좌측 무기 스크롤과 우측 강화 목록의 위치 관계가 정의되어 있다.
- [ ] 선택, 잠금, 재화 부족, MAX, placeholder 상태가 정의되어 있다.
- [ ] 사용자가 UI 레이아웃 목업을 승인했다.

### 화면 전환

- [ ] 타이틀 세 버튼이 서로 다른 화면을 연다.
- [ ] UPGRADES 진입 후 내부 탭이나 별도 상세 화면 없이 모든 강화 항목에 접근할 수 있다.
- [ ] CRAFTING에 무기 해금/강화가 나타나지 않는다.
- [ ] 뒤로가기와 옵션 복귀가 마지막 화면 상태를 보존한다.
- [ ] 비활성 화면이 레이아웃 공간이나 입력 포커스를 차지하지 않는다.

### UPGRADES

- [ ] 굴착기 4종과 보석 2종의 레벨/비용/효과가 기존 데이터와 일치한다.
- [ ] 무기 5종의 해금 순서와 비용이 기존 코드와 일치한다.
- [ ] 각 무기에 실제 강화 3종만 표시된다.
- [ ] 굴착기 강화 4종과 보석 채집 2종은 고정 표시되고, 무기 5종은 좌측 독립 스크롤로 접근할 수 있다.
- [ ] 무기 5종이 기본 펼침 상태이며 각 패널을 독립적으로 접고 펼칠 수 있다.
- [ ] 잠긴 무기의 강화 3종은 표시되지만 구매할 수 없다.
- [ ] 각 항목의 해금/강화 버튼을 같은 화면에서 직접 실행할 수 있다.
- [ ] 구매 후 전체 화면이 재생성되거나 흔들리지 않는다.
- [ ] 광석/보석 부족, 선행 조건, MAX 상태가 구분된다.
- [ ] 목업의 세 번째 재화나 가상 스탯이 표시되지 않는다.

### CHARACTER

- [ ] Victor/Sara/Jinus 3명만 표시된다.
- [ ] 캐릭터 자체에 잠금이나 구매 비용이 표시되지 않는다.
- [ ] 선택 캐릭터의 고유 장비 3개만 표시된다.
- [ ] Victor 분기와 Sara/Jinus 선형 선행 조건이 정확하다.
- [ ] 고유 장비는 해금만 가능하고 강화 레벨이 표시되지 않는다.
- [ ] Portrait/Description이 없어도 레이아웃이 깨지지 않는다.

### 안정성

- [ ] Console 컴파일 오류가 없다.
- [ ] Missing Script/Sprite/Font 참조가 없다.
- [ ] `V2HubCanvasSetupEditor` 재실행 후 같은 계층과 연결이 재현된다.
- [ ] D2Coding 폰트가 모든 동적 TMP에 적용된다.
- [ ] 릴리즈 빌드에서 치트 버튼이 숨겨진다.
- [ ] 저장 후 재실행해도 선택 캐릭터, 해금, 강화 레벨이 유지된다.

---

## 14. 완료 정의

다음 조건을 모두 만족하면 화면 분리 작업을 완료로 본다.

1. `UPGRADES / CHARACTER / CRAFTING`이 실제 독립 화면으로 동작한다.
2. UPGRADES는 굴착기 강화 4종, 보석 채집 강화 2종, 무기 5종의 해금/강화를 하나의 화면에 동시에 제공한다.
3. UPGRADES에서는 내부 탭, 별도 상세 화면, 필수 팝업 이동 없이 각 항목을 직접 해금하거나 강화할 수 있다.
4. CHARACTER는 캐릭터 선택과 선택 캐릭터의 고유 장비 3개 해금만 제공한다.
5. CRAFTING은 아이템 제작 예약 화면이며 무기 강화와 연결되지 않는다.
6. 모든 수치, 비용, 해금 조건, 저장은 기존 매니저와 ScriptableObject를 사용한다.
7. 스타일 참고 이미지는 시각 방향에만 사용하고 현재 클라이언트용 UI 레이아웃 목업을 별도로 제작해 승인받았다.
8. 승인된 UI 레이아웃 목업과 Unity 구현의 정보 구조, 패널 비율, 상태 표현이 일치한다.
9. 클라이언트에 없는 재화·캐릭터·스탯을 만들지 않는다.
10. 스타일 참고 이미지를 런타임 에셋으로 재사용하지 않는다.
11. 씬과 에디터 셋업 도구가 동일한 결과를 만들며 재실행에도 참조가 유지된다.
12. 1920×1080에서 잘림, 겹침, 레이아웃 흔들림, 입력 포커스 유실이 없다.

# Sys-UI Implementation Plan

> 작성일: 2026-06-26  
> 관련 컨셉 문서: [Sys-UI-ConceptDraft.md](Sys-UI-ConceptDraft.md)

## 1. 작업 원칙

UI는 화면별로 따로 만들지 않고, 공통 UI 키트를 먼저 만든 뒤 모든 화면에서 재사용한다.

기본 흐름:

1. 공통 UI 리소스 생성
2. Unity Import 설정 적용
3. 9-slice 가능한 Sprite로 구성
4. 공통 프리팹 제작
5. 프리뷰 프리팹 또는 실제 씬에서 확인
6. 다음 화면으로 확장

## 2. 1단계: 공통 UI 키트

### 목표

모든 아웃게임 UI가 공유할 금속 HUD 스타일의 최소 리소스와 프리팹을 만든다.

### 에디터 메뉴

```text
Tools/Drill-Corp/3. 게임 초기 설정/UI/1단계 공통 UI 키트 생성
```

구현 파일:

```text
Assets/_Game/Scripts/Editor/UIStyleKitStage1Builder.cs
```

### 생성 리소스

실행 시 다음 경로에 PNG 리소스를 생성한다.

```text
Assets/_Game/Sprites/UI/Common/
```

생성 대상:

| 리소스 | 용도 |
|---|---|
| `metal_panel.png` | 큰 패널 공통 프레임 |
| `metal_panel_small.png` | 작은 패널 공통 프레임 |
| `popup_frame.png` | 모달/확인 팝업 프레임 |
| `metal_button_normal.png` | 버튼 기본 상태 |
| `metal_button_hover.png` | 버튼 hover/selected 상태 |
| `metal_button_pressed.png` | 버튼 pressed 상태 |
| `metal_button_disabled.png` | 버튼 disabled 상태 |
| `button_glow_cyan.png` | 청록 hover/강조 오버레이 |
| `button_glow_amber.png` | 노랑 selected 오버레이 |
| `resource_slot.png` | 자원 표시 슬롯 |
| `list_row.png` | 업그레이드/옵션 리스트 행 |
| `card_frame.png` | 캐릭터 카드 프레임 |
| `node_frame.png` | 기본 노드 프레임 |
| `node_frame_active.png` | 활성 노드 프레임 |
| `node_frame_selected.png` | 선택 노드 프레임 |
| `node_frame_locked.png` | 잠금 노드 프레임 |
| `lock_overlay.png` | 잠금 오버레이 |
| `selected_glow.png` | 카드 선택 글로우 |
| `tooltip_frame.png` | 툴팁 프레임 |
| `divider_line.png` | 구분선 |
| `ui_common_contact_sheet.png` | 생성 리소스 확인용 시트 |

### Import 설정

각 Sprite는 생성 직후 자동으로 다음 설정을 적용한다.

| 항목 | 값 |
|---|---|
| Texture Type | Sprite |
| Sprite Mode | Single |
| Pixels Per Unit | 100 |
| Filter Mode | Point |
| Mipmap | Off |
| Wrap Mode | Clamp |
| Compression | Uncompressed |
| Alpha Is Transparency | true |
| Border | 리소스별 9-slice 여백 |

> 1단계는 개별 PNG를 생성하고, 나중에 Unity Sprite Atlas로 패킹하는 방식이다.  
> 이렇게 하면 각 리소스의 9-slice Border를 안정적으로 관리할 수 있다.

### 생성 프리팹

실행 시 다음 경로에 공통 프리팹을 생성한다.

```text
Assets/_Game/Prefabs/UI/Common/
```

생성 대상:

| 프리팹 | 내용 |
|---|---|
| `MetalPanel.prefab` | `metal_panel`을 사용하는 Sliced Image |
| `MetalButton.prefab` | Sprite Swap 상태를 가진 버튼 + D2Coding TMP 라벨 |
| `UIStyleKitPreview.prefab` | 버튼/패널/노드/카드/자원바 샘플 확인용 Canvas |

## 3. 1단계 Unity 확인 기준

Unity에서 메뉴를 실행한 뒤 다음을 확인한다.

- `Assets/_Game/Sprites/UI/Common/`에 PNG 리소스가 생성된다.
- 버튼/패널류 Sprite의 Border가 들어가 있다.
- `MetalPanel.prefab`의 Image Type이 `Sliced`다.
- `MetalButton.prefab`이 normal/hover/pressed/disabled Sprite Swap을 사용한다.
- `MetalButton.prefab` 라벨에 D2Coding이 적용되어 있다.
- `UIStyleKitPreview.prefab`을 열었을 때 다음 샘플이 한 화면에 보인다.
  - 버튼 5상태
  - 팝업 프레임
  - 리스트 행
  - 툴팁
  - 노드 4상태
  - 캐릭터 카드 프레임
  - 자원바 샘플
- 버튼/패널 크기를 조절해도 모서리가 심하게 찌그러지지 않는다.

## 4. 현재 상태

| 단계 | 상태 | 비고 |
|---|---|---|
| 1단계 스크립트 작성 | 완료 | `UIStyleKitStage1Builder.cs` 추가 |
| 컴파일 검증 | 완료 | `dotnet build Assembly-CSharp-Editor.csproj` 오류 0 |
| Unity batchmode 생성 실행 | 보류 | Unity가 프로젝트 경로 확인 직후 return code 1로 종료, 상세 오류 없음 |
| Unity 에디터 메뉴 실행 확인 | 대기 | 에디터에서 메뉴 실행 필요 |

## 5. 2단계: 공통 아이콘 + ResourceBar

### 목표

메인 메뉴, 자원 표시, 옵션 카테고리, 잠금/선택 상태에서 반복 사용할 공통 아이콘과 자원바 프리팹을 만든다.

### 에디터 메뉴

```text
Tools/Drill-Corp/3. 게임 초기 설정/UI/2단계 공통 아이콘·자원바 생성
```

구현 파일:

```text
Assets/_Game/Scripts/Editor/UIStyleKitStage2Builder.cs
```

### 생성 리소스

실행 시 다음 경로에 32x32 PNG 아이콘을 생성한다.

```text
Assets/_Game/Sprites/UI/Icons/
```

생성 대상:

| 리소스 | 용도 |
|---|---|
| `icon_ore.png` | 광석 |
| `icon_gem.png` | 보석 |
| `icon_credit.png` | 크레딧 |
| `icon_settings.png` | 설정 |
| `icon_exit.png` | 종료 |
| `icon_upgrade.png` | 업그레이드 |
| `icon_character.png` | 캐릭터 |
| `icon_crafting.png` | 제작 |
| `icon_back.png` | 뒤로가기 |
| `icon_lock.png` | 잠금 |
| `icon_check.png` | 선택/완료 |
| `icon_display.png` | 화면 설정 |
| `icon_sound.png` | 사운드 |
| `icon_language.png` | 언어 |
| `icon_accessibility.png` | 접근성 |
| `ui_icon_contact_sheet.png` | 생성 아이콘 확인용 시트 |

### 생성 프리팹

| 프리팹 | 내용 |
|---|---|
| `ResourceBar.prefab` | 광석/보석/크레딧 슬롯 3개가 들어간 공통 자원바 |
| `IconButton.prefab` | 아이콘 + TMP 라벨 + 1단계 `MetalButton` 상태 스프라이트를 쓰는 버튼 |

### 2단계 Unity 확인 기준

- `Assets/_Game/Sprites/UI/Icons/`에 아이콘 PNG와 `.meta`가 생성된다.
- `ui_icon_contact_sheet.png`에서 아이콘이 식별 가능하다.
- `ResourceBar.prefab`에 광석/보석/크레딧 슬롯이 보인다.
- `IconButton.prefab`이 `metal_button_*` 상태 스프라이트를 사용한다.
- 모든 TMP 라벨에 D2Coding이 적용되어 있다.

### 현재 상태

| 단계 | 상태 | 비고 |
|---|---|---|
| 2단계 스크립트 작성 | 완료 | `UIStyleKitStage2Builder.cs` 추가 |
| 컴파일 검증 | 완료 | `dotnet build Assembly-CSharp-Editor.csproj` 오류 0 |
| Unity 에디터 메뉴 실행 확인 | 완료 | 아이콘 방향 보정 후 재생성 완료 |

## 6. 3단계: Title UI 스타일 적용

### 목표

Title 씬의 기존 동작 바인딩은 유지하면서, 1~2단계 공통 UI 키트를 실제 화면에 적용한다.

### 에디터 메뉴

```text
Tools/Drill-Corp/3. 게임 초기 설정/UI/3단계 Title UI 스타일 적용
```

구현 파일:

```text
Assets/_Game/Scripts/Editor/UIStyleKitStage3TitleApplier.cs
```

### 적용 대상

| 대상 | 적용 내용 |
|---|---|
| Canvas | `vertexColorAlwaysGammaSpace` 켜기, 1920x1080 CanvasScaler 보정 |
| HubPanel / MainPanel / UpgradePanel / OptionsPanel | 전체 화면 과장 방지를 위해 플랫 다크 배경 적용 |
| TopBar / 각 SubPanel | `metal_panel_small` Sliced Image 적용 |
| 모든 Button | `metal_button_*` Sprite Swap 상태 적용 |
| 주요 큰 버튼 | `icon_upgrade`, `icon_crafting`, `icon_back` 등 삽입. 작은 상단바 버튼은 텍스트 중심 유지 |
| OreDisplay / GemDisplay | `resource_slot` 배경 + 공통 광석/보석 아이콘 적용 |
| TMP 텍스트 | D2Coding 적용, 큰 글자 외곽선 보정 |

### 3단계 Unity 확인 기준

- Title 씬에서 버튼들이 1단계 금속 버튼 스타일로 보인다.
- Start/Options/Quit/Back/Upgrade 계열 버튼에 아이콘이 보인다.
- TopBar의 광석/보석 표시가 2단계 자원 슬롯 스타일로 보인다.
- 기존 버튼 클릭 동작이 유지된다.
- 텍스트가 D2Coding으로 보이고, 큰 글자에 검은 외곽선이 들어간다.

### 현재 상태

| 단계 | 상태 | 비고 |
|---|---|---|
| 3단계 스크립트 작성 | 완료 | `UIStyleKitStage3TitleApplier.cs` 추가 |
| 컴파일 검증 | 완료 | `dotnet build Assembly-CSharp-Editor.csproj` 경고 0 / 오류 0 |
| Unity 에디터 메뉴 실행 확인 | 대기 | Title 씬에서 메뉴 실행 필요 |

## 7. 다음 단계

3단계가 Unity에서 확인되면 4단계로 넘어간다.

4단계 목표:

- 종료 버튼 클릭 시 즉시 종료 대신 `ConfirmPopup` 표시
- `ConfirmPopup.prefab` 생성
- YES/NO 버튼에 공통 버튼 스타일 적용
- ESC/NO/외부 클릭 닫기 처리

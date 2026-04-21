# 리팩토링 계획 — 에디터 전용 코드 마킹 정리

> 작성일: 2026-04-21
> 상태: **대기 중 (defer)** — Sara/Jinus 어빌리티 개발 완료 후 재개 예정
> 범위: 런타임 폴더에 섞여 있는 `#if UNITY_EDITOR` 블록들의 가시성·검색성 개선

## 1. 배경

라이브 튜닝용 `AbilityData.OnValidated` 이벤트 + `BlackHoleRunner.OnDataValidated` 구독을 추가하면서, "이게 개발 중에만 쓰는 코드"라는 게 파일을 열어보기 전까지는 안 보이는 문제가 드러났다. 나중에 빌드 정리 / 코드 정리 시점에 한 번에 찾고 싶다.

- **`Assets/_Game/Scripts/Editor/` 폴더 안의 파일**은 Unity 관례상 자동으로 에디터 전용 — 본 리팩토링 범위 밖.
- 본 문서는 **런타임 폴더(`Data/`, `Ability/`, `Machine/` 등)에 섞여 있는 `#if UNITY_EDITOR` 블록**만 다룬다.

## 2. 현황 조사 (2026-04-21 기준)

### 2-1. 런타임 폴더에서 `#if UNITY_EDITOR` 쓰는 파일 (총 10개)

| 파일 | 블록 용도 | 분류 |
|---|---|---|
| `Scripts/Data/AbilityData.cs` | `OnValidate` + `OnValidated` 이벤트 (라이브 튜닝 훅) | **Live-tuning 훅** |
| `Scripts/Ability/Runners/BlackHoleRunner.cs` | `OnDataValidated` 구독 + 존 스케일 갱신 | **Live-tuning 훅** |
| `Scripts/Machine/TurretController.cs` | `OnValidate` (인스펙터 값 → 자식 Transform 반영) | OnValidate (기존) |
| `Scripts/Aim/AimWeaponRing.cs` | `OnValidate` (Mesh 재빌드) | OnValidate (기존) |
| `Scripts/UI/Weapon/WeaponSlotUI.cs` | `[ContextMenu]` 슬롯 레이아웃 빌더 | ContextMenu 툴링 |
| `Scripts/Camera/DebugCameraUI.cs` | `using UnityEditor` + 세팅 에셋 저장 | 디버그 툴 |
| `Scripts/OutGame/HubController.cs` | Quit 버튼 `EditorApplication.isPlaying=false`, Cheat 재화 추가 | **빌드 필수 분기** (지우면 안 됨) |
| `Scripts/OutGame/TitleUI.cs` | Quit 버튼 동일 패턴 | **빌드 필수 분기** |
| `Scripts/Bug/Simple/SimpleBugSpawner.cs` | `OnDrawGizmosSelected` | Gizmos |
| `Scripts/Bug/Simple/TunnelEventManager.cs` | `OnDrawGizmosSelected` | Gizmos |

### 2-2. `OnDrawGizmos*` 만 있는 파일 (추가로 5개)

`#if UNITY_EDITOR` 없이 그냥 `OnDrawGizmos*` 만 있는 파일들. 관례상 릴리스 빌드에선 자동으로 strip 됨 — 건드릴 필요 없음.

- `MineInstance.cs`, `AimController.cs`, `DynamicCamera.cs`, `FormationSpawner.cs`, `BugSpawner.cs`

### 2-3. 어셈블리 구조

- **`.asmdef` 없음** — 전체가 단일 `Assembly-CSharp` 어셈블리
- Editor 폴더(`Scripts/Editor/*`) 만 Unity 관례로 별도 에디터 어셈블리로 분리됨

## 3. 옵션 비교

### 옵션 A — 주석 태그만 (최소)

- `#if UNITY_EDITOR` 블록 위에 `// [DEV-TUNING]` 또는 `// [EDITOR-ONLY]` 태그 주석 한 줄
- 대상: 라이브 튜닝/OnValidate 관련 4~6 파일
- 소요: **10분 이내**
- 검색: `Grep "[DEV-TUNING]"`
- 빌드 필수 분기(Quit/Cheat), Gizmos 는 건드리지 않음

### 옵션 B — partial 분리 (중간)

- `AbilityData.cs` 를 `AbilityData.Editor.cs` 로 partial 분리 (가장 명확한 케이스)
  - `OnValidated` 이벤트 + `OnValidate()` 만 Editor partial 쪽으로 이동
- 짧은 `OnValidate` 만 있는 TurretController/AimWeaponRing 은 분리 실익 낮아 주석 태그만
- 소요: **15~20분** (1 파일 생성 + 원본에서 제거 + 네임스페이스/using 맞춤)
- 검색: `Glob "*.Editor.cs"` + 주석 태그 병행

### 옵션 C — `.asmdef` + Editor 어셈블리 (최대)

- `DrillCorp.Runtime` asmdef 생성 + `DrillCorp.Runtime.Editor` 별도 어셈블리
- 기존 코드 전부 asmdef 참조 재정비 필요 — 컴파일 오류 연쇄
- 소요: **1~3시간**
- **이 프로젝트 규모엔 과투자** — 어셈블리 분리 이득(컴파일 속도)보다 유지보수 부담 큼

## 4. 결정 (defer)

**옵션 A + B 조합 예정** (총 ~20분 예상):

1. `Scripts/Data/AbilityData.cs` → `Scripts/Data/AbilityData.Editor.cs` partial 분리
   - `OnValidated` event, `OnValidate()` 메서드 이동
   - 원본 쪽엔 이동 흔적만 주석 (`// Editor-only hooks in AbilityData.Editor.cs`)
2. 나머지 라이브 튜닝 관련 `#if UNITY_EDITOR` 블록에 `// [DEV-TUNING]` 태그
   - `BlackHoleRunner.cs` (OnDataValidated)
   - 후속 Sara/Jinus Runner 추가 시 동일 태그
3. `OnValidate` 짧은 케이스(TurretController, AimWeaponRing)는 태그만
4. **건드리지 않는 것**:
   - Gizmos 메서드 (관례상 이미 에디터 전용 표시)
   - 빌드 필수 분기 (HubController Quit/Cheat, TitleUI Quit — strip 시 런타임 동작 깨짐)
   - Editor 폴더(`Scripts/Editor/*`) 전체

## 5. 재개 조건

- Phase 6 사라 어빌리티 3종 구현 완료 **또는**
- Phase 7 지누스 어빌리티 완료 후
- 라이브 튜닝 훅이 3개 이상 Runner 에 생겨 마커 필요성이 체감될 때 (현재는 BlackHole 1개)

## 6. 참고

- 현재 생성된 라이브 튜닝 훅: `AbilityData.OnValidated` (2026-04-21 추가), `BlackHoleRunner.OnDataValidated`
- Phase 6 진행 중 Shockwave/Meteor Runner 에도 동일 훅이 추가되면 이때 묶어서 리팩토링 후보로 승격

# Drill-Corp 프로젝트 가이드

## 프로젝트 개요
- **게임 타이틀**: Drill-Corp (Galaxy Probe)
- **장르**: 로그라이트 디펜스 슈팅 서바이벌
- **엔진**: Unity 6 (6000.x) - URP
- **개발 환경**: VSCode + Claude Code
- **타겟 플랫폼**: Steam (PC)
- **해상도**: 1920x1080 (16:9)

## 게임 핵심 컨셉
- 화면 중앙의 채굴 머신을 지키면서 벌레를 처치
- 플레이어는 이동 없이 **마우스 커서로만 조준/사격**
- 연료가 소진되면 세션 성공, 머신 HP 0이면 세션 실패
- 세션 성공 시 채굴량만큼 재화 획득

## 폴더 구조
```
Assets/
├── _Game/
│   ├── Scripts/
│   │   ├── Core/          # GameManager, DataManager, GameEvents
│   │   ├── Machine/       # MachineController
│   │   ├── Bug/           # BugBase, BugAI, BugSpawner, 벌레 종류별
│   │   ├── Aim/           # AimController (사격 시스템)
│   │   ├── Wave/          # WaveManager
│   │   ├── UI/            # UIManager, HP바, FUEL바 등
│   │   └── Data/          # ScriptableObject 클래스 정의
│   ├── Prefabs/
│   ├── Data/              # ScriptableObject .asset 파일
│   └── Materials/
├── Scenes/
├── Settings/
└── [서드파티 에셋 폴더들 - 이동 금지]
```

## Unity 환경 설정
- **Unity 버전**: Unity 6 (6000.x)
- **렌더 파이프라인**: URP (Universal Render Pipeline)
- **Input System**: New Input System (UnityEngine.InputSystem) - 레거시 사용 금지
- **카메라**: Orthographic, 탑다운
- **저장 방식**: JSON (PlayerPrefs 최소화)
- **기본 폰트**: D2Coding-Ver1.3 (TextMeshPro 사용 시 이 폰트 적용)

## ⚠️ 탑다운 뷰 좌표계 (모든 코드에 필수 적용)
이 게임은 **탑다운 뷰**이며, 카메라가 위에서 아래(-Y)를 내려다봅니다.
- **X축**: 좌우 (화면 좌 ↔ 우)
- **Y축**: 높이 (위 아래, 화면에 수직) - 일반적으로 고정
- **Z축**: 상하 (화면 위 ↔ 아래)

### 코드 작성 시 규칙
| 상황 | 올바른 예 | 잘못된 예 |
|------|-----------|-----------|
| 위로 이동/떠오름 | `Vector3(0, 0, 1)` 또는 `Vector3.forward` | `Vector3(0, 1, 0)` 또는 `Vector3.up` |
| 아래로 이동 | `Vector3(0, 0, -1)` 또는 `Vector3.back` | `Vector3(0, -1, 0)` |
| XZ 평면 거리 계산 | `Vector3.Distance(new Vector3(a.x, 0, a.z), new Vector3(b.x, 0, b.z))` | `Vector3.Distance(a, b)` (Y 포함됨) |
| 2D 방향 벡터 | `new Vector3(dir.x, 0, dir.z).normalized` | Y값 포함된 방향 |

### 주의 사항
- `Vector3.up`은 Y축(높이)이므로 화면상 "위"가 아님
- UI/이펙트가 "화면 위로" 떠야 한다면 **Z축 양의 방향** 사용
- 이동, 회전, 거리 계산 시 항상 **XZ 평면** 기준으로 작성

### 월드 UI 컴포넌트 체크리스트 (HP바, 라벨, 팝업 등)
월드 공간에 표시되는 UI 컴포넌트를 만들 때 반드시 확인:
1. **위치**: 오프셋의 Z값이 "화면 위쪽"을 의미 (예: `new Vector3(0, 0.1f, 0.8f)`)
2. **회전**: `Quaternion.Euler(90f, 0f, 0f)` 또는 `transform.rotation = Quaternion.identity`로 카메라 향함
3. **부모 회전 무시**: 부모 오브젝트가 회전해도 UI는 고정되어야 함
   - `LateUpdate`에서 월드 좌표로 위치 설정: `transform.position = _target.position + _offset`
   - 회전 고정: `transform.rotation = Quaternion.identity` 또는 `Quaternion.Euler(90f, 0f, 0f)`
4. **참조 패턴**: `BugHpBar.cs` 참고 (올바른 구현 예시)

## TextMeshPro 폰트 규칙
코드에서 TextMeshPro를 생성할 때는 **반드시 D2Coding 폰트**를 적용해야 합니다.

### 런타임 코드에서
```csharp
using DrillCorp.UI;

// TextMeshPro 생성 후 폰트 적용
var tmp = obj.AddComponent<TextMeshPro>();
TMPFontHelper.ApplyDefaultFont(tmp);
```

### 에디터 스크립트에서
```csharp
var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/D2Coding-Ver1.3.asset");
if (font != null) tmp.font = font;
```

### 폰트 에셋 경로
- 기본: `Assets/TextMesh Pro/Fonts/D2Coding-Ver1.3.asset`
- 볼드: `Assets/TextMesh Pro/Fonts/D2CodingBold-Ver1.3.asset`

## 코딩 컨벤션
- 언어: C#
- private 변수: `_camelCase`
- public 변수/프로퍼티: `PascalCase`
- 이벤트: `OnEventName`
- 인터페이스: `IInterfaceName`
- ScriptableObject 파일명: `Bug_Beetle`, `Wave_01` 형식

## 주의사항
- 서드파티 에셋 폴더 절대 이동 금지 (참조 깨짐)
- `Resources` 폴더 사용 최소화 (빌드 사이즈 영향)
- New Input System 사용 (`UnityEngine.Input` 사용 금지)

## 참고 문서
- 아키텍처 패턴: `docs/ARCHITECTURE.md`
- 개발 로드맵: `docs/ROADMAP.md`
- 데이터 구조: `docs/DATA_STRUCTURE.md`

## 개발 로그 (블로깅용)
각 단계 완료 시 상세 내용 정리:
- 1단계 코어 시스템: `docs/DevLog_01_CoreSystem.md`
- 2단계 인게임 세션: `docs/DevLog_02_InGameSession.md`
- 3단계 UI: `docs/DevLog_03_UI.md`
- 4단계 데이터 시트: `docs/DevLog_04_DataSheet.md`
- 5단계 아웃게임: `docs/DevLog_05_OutGame.md`
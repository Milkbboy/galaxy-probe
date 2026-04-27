# 3단계 - UI 개발 로그

## 개요
Drill-Corp 게임의 UI 시스템 구축. 인게임 HUD, 세션 결과 화면 구현.

---

## 타임라인

| 시간 | 작업 내용 |
|------|-----------|
| 2026-03-18 | 3단계 UI 구현 시작 |
| └ | 폴더 구조 생성: `Assets/_Game/Scripts/UI/` |
| └ | UIManager.cs 작성 - UI 패널 관리 |
| └ | MachineStatusUI.cs 작성 - HP/FUEL 바 |
| └ | AimChargeUI.cs 작성 - 에임 충전 게이지 |
| └ | MiningUI.cs 작성 - 채굴량 표시 |
| └ | SessionResultUI.cs 작성 - 세션 성공/실패 화면 |
| └ | **UISetupEditor.cs 작성 - UI 자동 생성 에디터 스크립트** |
| 2026-03-19 | UI 개선 및 버그 수정 |
| └ | UISetupEditor 개선 - 배경+Fill 구조로 HP/Fuel 바 변경 |
| └ | UISetupEditor 개선 - D2Coding 한글 폰트 자동 적용 |
| └ | UISetupEditor 개선 - Canvas additionalShaderChannels 설정 (TMP 경고 해결) |
| └ | MachineStatusUI 수정 - Gradient 색상 변경 로직 제거 (흰색 버그 수정) |
| └ | AimChargeUI 수정 - 충전 방식 → 쿨다운 표시 방식으로 변경 |
| └ | SpriteGenerator.cs 작성 - UI용 단색 스프라이트 생성 도구 |
| └ | BugHealthBar.cs 작성 - Sprite 기반 버그 HP바 (Canvas 대신 최적화) |
| └ | BugBase.cs 수정 - HP바 생성/업데이트/정리 로직 추가 |
| └ | HP바 오프셋 수정 - 탑다운 뷰에 맞게 Z축 오프셋 적용 |

---

## 생성된 파일

| 파일명 | 경로 | 역할 |
|--------|------|------|
| UIManager.cs | `Scripts/UI/` | UI 패널 관리 |
| MachineStatusUI.cs | `Scripts/UI/` | HP/FUEL 바 |
| AimChargeUI.cs | `Scripts/UI/` | 에임 쿨다운 게이지 |
| MiningUI.cs | `Scripts/UI/` | 채굴량 표시 |
| SessionResultUI.cs | `Scripts/UI/` | 세션 결과 화면 |
| UISetupEditor.cs | `Scripts/Editor/` | UI 자동 생성 에디터 도구 |
| SpriteGenerator.cs | `Scripts/Editor/` | UI용 스프라이트 생성 도구 |
| BugHealthBar.cs | `Scripts/Bug/` | Sprite 기반 버그 HP바 |

---

## UIManager.cs

### 역할
- 게임 상태에 따른 UI 패널 전환
- 싱글톤 패턴

### 관리하는 패널
| 패널 | 표시 시점 |
|------|-----------|
| InGame UI | Playing 상태 |
| Session Success UI | 세션 성공 |
| Session Failed UI | 세션 실패 |
| Pause UI | 일시정지 |

### 이벤트 구독
- `OnGameStateChanged` - 상태 변경 시 패널 전환
- `OnSessionSuccess` - 성공 UI 표시
- `OnSessionFailed` - 실패 UI 표시

---

## MachineStatusUI.cs

### 역할
- 머신 HP바 표시
- 머신 FUEL바 표시
- 이벤트 기반 업데이트

### Inspector 설정
```
HP Bar:
- HP Fill Image (Image) - Fill Amount 사용
- HP Text (TMP) - "현재/최대" 형식
- HP Gradient - 체력에 따른 색상 변화

Fuel Bar:
- Fuel Fill Image (Image)
- Fuel Text (TMP) - "00s" 형식

References:
- Machine (MachineController) - 자동 검색 가능
```

### HP Gradient 추천 설정
| 위치 | 색상 |
|------|------|
| 0% | 빨강 (#FF0000) |
| 50% | 노랑 (#FFFF00) |
| 100% | 초록 (#00FF00) |

---

## AimChargeUI.cs

### 역할
- 마우스 조준점 아래 쿨다운 게이지 표시
- 버그가 범위 내에 있을 때만 표시
- 공격 쿨다운 상태 시각화

### Inspector 설정
```
Charge Bar:
- Charge Fill Image (Image)
- Canvas Group - 페이드용

Colors:
- Charging Color: 노랑 (#FFFF00) - 쿨다운 중
- Ready Color: 빨강 (#FF0000) - 공격 가능

Settings:
- Fade Speed: 5
- Offset: (0, -0.5, 0) - 조준점 아래 위치
```

### 동작 방식 (자동 공격 방식으로 변경됨)
1. 버그가 Aim 범위에 진입 → 페이드 인
2. 쿨다운 진행 중 → 노란색 게이지 증가
3. 쿨다운 완료 (공격 가능) → 빨간색으로 변경
4. 버그가 범위에서 벗어남 → 페이드 아웃

---

## MiningUI.cs

### 역할
- 현재 채굴량 표시
- 채굴 획득 시 펀치 애니메이션

### Inspector 설정
```
Mining Display:
- Mining Text (TMP)
- Prefix: "채굴: "

Animation:
- Punch Scale: 1.2
- Punch Duration: 0.1초
```

### 애니메이션
- 채굴 획득 시 텍스트 1.2배 확대 후 복귀
- 시각적 피드백 제공

---

## SessionResultUI.cs

### 역할
- 세션 성공 화면 표시
- 세션 실패 화면 표시
- 버튼 이벤트 처리

### 성공 화면 구성
| 요소 | 설명 |
|------|------|
| 채굴량 텍스트 | 이번 세션 채굴량 |
| 재화 텍스트 | 현재 보유 재화 |
| Continue 버튼 | 다음 세션 시작 |

### 실패 화면 구성
| 요소 | 설명 |
|------|------|
| 채굴량 텍스트 | 이번 세션 채굴량 (획득 불가) |
| Retry 버튼 | 세션 재시작 |
| Quit 버튼 | 타이틀로 이동 |

---

## Unity 설정 가이드

### 1. Canvas 생성
1. Hierarchy > UI > Canvas
2. Canvas Scaler 설정:
   - UI Scale Mode: Scale With Screen Size
   - Reference Resolution: 1920 x 1080
   - Match: 0.5

### 2. InGame UI 패널
```
Canvas
└── InGamePanel
    ├── MachineStatusUI
    │   ├── HPBar (Image - Filled)
    │   │   └── HPText (TMP)
    │   └── FuelBar (Image - Filled)
    │       └── FuelText (TMP)
    ├── MiningUI
    │   └── MiningText (TMP)
    └── AimChargeUI
        └── ChargeBar (Image - Filled)
```

### 3. Result UI 패널
```
Canvas
├── SuccessPanel (기본 비활성)
│   ├── Title (TMP) - "세션 성공!"
│   ├── MiningText (TMP)
│   ├── CurrencyText (TMP)
│   └── ContinueButton
└── FailedPanel (기본 비활성)
    ├── Title (TMP) - "세션 실패..."
    ├── MiningText (TMP)
    ├── RetryButton
    └── QuitButton
```

### 4. UIManager 설정
1. Canvas에 UIManager 컴포넌트 추가
2. 각 패널 연결:
   - InGame UI → InGamePanel
   - Session Success UI → SuccessPanel
   - Session Failed UI → FailedPanel

### 5. 컴포넌트 연결
- MachineStatusUI, MiningUI에 Machine 자동 검색 (또는 수동 연결)
- AimChargeUI에 AimController 자동 검색 (또는 수동 연결)
- SessionResultUI는 이벤트로 자동 동작

---

## 필요 패키지
- TextMeshPro (TMP) - Unity 기본 포함
- 프로젝트에 TMP Essential Resources 임포트 필요

---

## UI 자동 생성 (에디터 스크립트)

### UISetupEditor.cs
**경로**: `Assets/_Game/Scripts/Editor/UISetupEditor.cs`

### 사용법
1. Unity 메뉴: `Tools > Drill-Corp > Setup InGame UI` 클릭
2. Canvas + 모든 UI 요소 자동 생성
3. 스크립트 컴포넌트 자동 연결

### 자동 생성 항목
```
Canvas (Screen Space - Overlay, 1920x1080)
├── InGamePanel
│   ├── MachineStatusUI + 컴포넌트
│   │   ├── HPBar (빨강, Filled)
│   │   │   └── HPText
│   │   └── FuelBar (노랑, Filled)
│   │       └── FuelText
│   └── MiningUI + 컴포넌트
│       └── MiningText
│
├── AimChargeUI + 컴포넌트
│   └── ChargeBar
│
├── SuccessPanel (비활성) + SessionResultUI
│   ├── TitleText - "세션 성공!"
│   ├── MiningText
│   ├── CurrencyText
│   └── ContinueButton
│
└── FailedPanel (비활성)
    ├── TitleText - "세션 실패..."
    ├── MiningText
    ├── RetryButton
    └── QuitButton
```

### 장점
- 수동 UI 구성 불필요
- 컴포넌트 자동 연결
- 일관된 UI 구조 보장

---

## BugHealthBar.cs (추가)

### 역할
- 버그 머리 위에 HP바 표시
- Sprite 기반 (Canvas 대신 최적화)
- 100마리 버그 = 100개 SpriteRenderer (Canvas보다 가벼움)

### 구조
```
BugHealthBar (GameObject)
├── Background (SpriteRenderer) - 어두운 배경
└── Fill (SpriteRenderer) - HP 게이지
```

### 특징
- `BugHealthBar.Create(Transform)` 정적 메서드로 코드에서 생성
- XZ 평면 기준 오프셋 (탑다운 뷰 대응)
- 체력 비율에 따른 색상 변화 (초록 → 빨강)

---

## SpriteGenerator.cs (추가)

### 역할
- UI용 단순 사각형 스프라이트 생성
- `Tools > Drill-Corp > Generate UI Sprites` 메뉴

### 생성 파일
- `Assets/_Game/Sprites/UI/Square_White.png` (4x4 흰색)

---

## 해결한 문제들

### 1. HP바 Fill이 흰색으로 변하는 문제
- **원인**: `_hpGradient`가 빈 상태에서 `Evaluate()` 호출 시 흰색 반환
- **해결**: Gradient 색상 변경 로직 제거, Inspector 설정 색상 유지

### 2. HP바 게이지가 안 줄어드는 문제
- **원인**: Image Type이 Simple (Filled가 아님)
- **해결**: Source Image 설정 후 Image Type = Filled로 변경

### 3. HP바 모서리가 둥글게 보이는 문제
- **원인**: Unity 기본 Background 스프라이트가 둥근 모서리
- **해결**: SpriteGenerator로 단순 사각형 스프라이트 생성

### 4. TMP 관련 Canvas 경고
- **원인**: Canvas에 Additional Shader Channels 미설정
- **해결**: UISetupEditor에서 TexCoord1, Normal, Tangent 채널 추가

### 5. 버그 HP바 위치 문제 (탑다운 뷰)
- **원인**: Y축 오프셋으로 설정 (카메라가 위에서 내려다봄)
- **해결**: Z축 오프셋으로 변경 (화면상 위쪽으로 이동)

---

## 다음 단계
4단계 - 데이터 시트
- BugData ScriptableObject
- WaveData ScriptableObject
- MachineData ScriptableObject
- .asset 파일 생성
- 밸런스 초기값 세팅

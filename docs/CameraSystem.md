# 카메라 시스템

> 최종 갱신: 2026-04-13

## 1. 개요

Drill-Corp의 카메라는 **Nuclear Throne 방식**을 사용합니다.
- Orthographic Size는 **고정**
- 카메라 위치가 **머신과 마우스 사이**로 이동
- 마우스 쪽으로 살짝 따라가면서 전방 시야 확보

## 2. 왜 Nuclear Throne 방식?

### 기존 줌 방식의 문제
| 항목 | 문제 |
|------|------|
| 에임 정확도 | 줌 변경 시 픽셀당 월드 거리 변동 |
| 피로감 | 조준할 때마다 화면이 줌인/줌아웃 |
| 구현 복잡도 | Size 계산 + Dead Zone + 보정 필요 |

### Nuclear Throne 방식의 장점
| 항목 | 개선 |
|------|------|
| 에임 정확도 | Size 고정 → 픽셀당 월드 거리 일정 |
| 피로감 | 줌 변화 없음 → 안정적 시야 |
| 구현 단순 | Position Lerp 한 줄 |
| 연출 | 마우스 쪽 시야 확보로 "떼 몰려오는" 연출 유지 |

## 3. 동작 원리

```
카메라 위치 = Lerp(머신, 마우스월드, MouseWeight)
             ↓ 클램프 (MaxOffset 초과 방지)
             ↓ Y값 고정 (카메라 높이 유지)
             ↓ SmoothSpeed로 부드럽게 보간
             최종 위치
```

### 파라미터

| 파라미터 | 기본값 | 범위 | 설명 |
|---------|--------|------|------|
| **OrthographicSize** | 8 | 3~20 | 카메라 Orthographic Size (고정) |
| **MouseWeight** | 0.3 | 0~1 | 0=머신 고정, 0.5=정중간, 1=마우스 추적 |
| **MaxOffset** | 8 | 1~20 | 머신에서 카메라가 떨어질 수 있는 최대 거리 |
| **SmoothSpeed** | 5 | 1~20 | 카메라 추적 속도 |

## 4. 파일 구조

```
Assets/_Game/
├── Scripts/Camera/
│   ├── CameraSettingsData.cs   # SO 클래스 ([Range] 속성)
│   ├── DynamicCamera.cs        # 카메라 제어 + Gizmo
│   └── DebugCameraUI.cs        # F1 디버그 UI
└── Data/Camera/
    └── CameraSettings.asset    # 실제 SO 에셋
```

## 5. 주요 컴포넌트

### 5.1 CameraSettingsData
- ScriptableObject로 파라미터 관리
- `[Range]` 속성으로 Inspector에서 슬라이더 제공
- 런타임 수정 가능 (Setter 메서드)

### 5.2 DynamicCamera
- Main Camera에 부착
- `LateUpdate()`에서 카메라 위치 갱신
- Mouse Position → Ground Plane Raycast로 월드 좌표 변환
- XZ 평면에서만 이동 (Y 고정)

**씬 설정:**
1. Main Camera에 `DynamicCamera` 컴포넌트 추가
2. `Settings` 필드에 `CameraSettings.asset` 할당
3. `Machine` 필드에 머신 Transform 할당 (또는 "Machine" 태그 설정 시 자동)

### 5.3 DebugCameraUI
- F1 키로 토글
- 런타임 중 슬라이더로 실시간 조정
- "Save to Asset" 버튼으로 현재 값을 SO에 영구 저장 (에디터 전용)

## 6. Gizmo 시각화

씬 뷰에서 카메라 선택 시:

| 색상 | 표시 내용 |
|------|----------|
| 🟠 주황 원 | MaxOffset 범위 (카메라 이동 가능 영역) |
| 🔵 청록 구 | 현재 카메라 목표 위치 (재생 중) |

## 7. 튜닝 가이드

### 카메라가 너무 흔들림
- `SmoothSpeed` 낮추기 (5 → 3)
- `MouseWeight` 낮추기 (0.3 → 0.2)

### 마우스 쪽이 잘 안 보임
- `MouseWeight` 높이기 (0.3 → 0.5)
- `MaxOffset` 늘리기 (8 → 12)

### 반응이 너무 둔함
- `SmoothSpeed` 높이기 (5 → 8)

### 머신이 화면 밖으로 나감
- `MouseWeight` 낮추기
- `MaxOffset` 줄이기

## 8. 좌표계 주의사항

CLAUDE.md 규칙에 따라 **탑다운 뷰**에서:
- X: 좌우 (화면 가로)
- Y: 높이 (카메라와 지면 수직 거리, 고정)
- Z: 상하 (화면 세로)

카메라는 **XZ 평면에서만** 이동합니다. Y값은 `Awake()`에서 현재 높이를 캐싱해 유지.

## 9. 향후 확장 가능 기능

- **웨이브 시작 줌아웃 연출**: 특수 상황에서만 OrthographicSize 변경
- **카메라 흔들림**: 폭발/피격 시 shake
- **보스 포커스**: 보스 등장 시 중간 지점으로 카메라 이동
- **경계 클램프**: 맵 밖으로 카메라가 나가지 않도록

## 10. 참고

- **Nuclear Throne**: 2015년 Vlambeer의 탑다운 로그라이크 슈터
- **참조**: `DRILL-CORP-PLAN.md` 7. 카메라 시스템

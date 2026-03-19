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
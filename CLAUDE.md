# Drill-Corp 프로젝트 가이드

## 개요
- **타이틀**: Drill-Corp (Galaxy Probe) / 로그라이트 디펜스 슈팅 서바이벌
- **엔진**: Unity 6 (6000.x) + URP / 타겟: Steam PC / 해상도: 1920x1080
- **컨셉**: 중앙 채굴 머신 방어, 플레이어는 이동 없이 마우스로만 조준·사격.
  연료 소진 = 세션 성공(채굴량만큼 재화), 머신 HP 0 = 실패.

## 폴더 구조
```
Assets/_Game/
├── Scripts/  (Core, Machine, Bug, Aim, Wave, UI, Weapon, Camera, Data, Editor ...)
├── Prefabs/  Data/  Materials/  Settings/
Assets/[서드파티]   # 이동 금지 (참조 깨짐)
```

## Unity 환경
- Unity 6 / URP / Orthographic 탑다운 카메라
- **New Input System 필수** (`UnityEngine.Input` 레거시 금지)
- 저장: JSON (PlayerPrefs 최소화)
- 기본 폰트: `D2Coding-Ver1.3` (TMP 생성 시 항상 적용)
- `Resources` 폴더 사용 최소화

## 최신 API 확인 (context7 MCP 필수)
Unity / URP / Input System / TMP / 외부 패키지 코드를 작성·수정할 때는
**context7**(`resolve-library-id` → `get-library-docs`)로 현재 시그니처 확인 후 사용.
학습 컷오프 이후 deprecated 된 API 주의 (예: `FindObjectOfType` → `FindAnyObjectByType`).
`Find*`, `SendMessage`, Legacy Input 등 의심 API는 특히 필수.

## ⚠️ 탑다운 좌표계 (모든 코드 필수)
카메라가 -Y로 내려다봄. **X = 좌우, Y = 높이(고정), Z = 화면 상하**.
- 화면상 "위로" = `Vector3.forward` / `(0,0,+)` (❌ `Vector3.up`)
- 화면상 "아래로" = `Vector3.back` / `(0,0,-)`
- 2D 거리·방향은 **XZ 평면**에서 계산 (Y 성분 제거)

### 월드 UI (HP바 / 라벨 / 팝업)
- 오프셋은 Z로 띄우기 (예: `new Vector3(0, 0.1f, 0.8f)`)
- 회전 고정: `Quaternion.Euler(90,0,0)` 또는 `Quaternion.identity` — 부모 회전 무시
- `LateUpdate`에서 `transform.position = _target.position + _offset` 갱신
- 참조 구현: `Assets/_Game/Scripts/UI/Minimap/MinimapIcon.cs` (LateUpdate 월드 추적 + 부모 회전 무시)

## VFX 제작 정책
**새 VFX(파티클·글로우·폭발·트레일 등) 만들기 전에 `Assets/Polygon Arsenal/Prefabs/` 폴더를 먼저 검색**해 재활용 가능한 프리펩이 있는지 확인. Mesh/ParticleSystem 직접 제작은 예외적인 경우에만.
- 주요 카테고리: `Combat/Explosions` (폭발), `Combat/Muzzleflash` (총구), `Combat/Flamethrower` (화염), `Combat/Nova` (원형 폭발), `Interactive/Zone/Glow` (바닥 글로우), `Interactive/Powerups/Orbs` (소형 orb), `Interactive/Sparkle` (반짝임), `Environment/Sparks` (불꽃)
- 색 변형: 대부분 Red/Blue/Green/Yellow/Pink/Purple 6색 존재 — 색 선택만으로 해결되는 경우가 많음
- 프리펩 참조 경로는 에디터 셋업 툴(예: `MinePrefabCreator.cs`)에 상수로 박아두면 재생성 시 자동 바인딩

## TextMeshPro 폰트
코드로 TMP 생성 시 반드시 D2Coding 적용.
- **런타임**: `TMPFontHelper.ApplyDefaultFont(tmp)` (`DrillCorp.UI` 네임스페이스)
- **에디터**: `AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/D2Coding-Ver1.3.asset")`
- 볼드 경로: `.../D2CodingBold-Ver1.3.asset`

## 코딩 컨벤션
- C# / private `_camelCase` / public·프로퍼티 `PascalCase`
- 이벤트 `OnEventName` / 인터페이스 `IInterfaceName`
- ScriptableObject 파일명: `SimpleBug_Normal`, `Wave_01` 형식

## 참고 문서 (`docs/`)
- 인덱스: `README.md`
- 기획/로드맵: `DRILL-CORP-PLAN.md`
- 아키텍처: `Architecture.md` / 데이터: `DataStructure.md` (SimpleBug/Wave/Machine/Upgrade SO 정의 + 런타임 흐름)
- 시트/Import: `GoogleSheetsGuide.md` (기획자 튜닝 워크플로우 포함) / v2 신규 시트: `GoogleSheetsGuide_v2Addendum.md`
- 시스템별: `WeaponUnlockUpgradeSystem.md` (v2 무기), `WeaponSystem.md` (Phase 3 아카이브), `CameraSystem.md`, `MinimapSystem.md`, `SoundSystem.md`, `GemMiningSystem.md`, `CharacterAbilitySystem.md`
- 최적화: `Optimization_Overview.md` / 이력: `Optimization_History.md`
- 개발 로그: `DevLog_01_CoreSystem` ~ `DevLog_05_OutGame`
- 변경 이력: `CHANGELOG.md`
- 과거 자료: `archive/` (프로토타입 분석, 폐기된 BugBehavior/Formation 문서, 완료된 계획서 등)

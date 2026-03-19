# 개발 로드맵

## 전체 진행 상황
- ✅ 0단계 - 프로젝트 세팅
- ✅ 1단계 - 코어 시스템
- ✅ 2단계 - 인게임 세션
- ✅ 3단계 - UI
- 🔲 4단계 - 데이터 시트
- 🔲 5단계 - 아웃게임 (추후)

---

## ✅ 0단계 - 프로젝트 세팅
- [x] Unity 6 URP 프로젝트 생성
- [x] 폴더 구조 세팅
- [x] CLAUDE.md 작성

---

## ✅ 1단계 - 코어 시스템
- [x] `GameManager.cs` (싱글톤, 씬 전환, 게임 상태)
- [x] `DataManager.cs` (재화/강화 저장/로드 - JSON)
- [x] `GameEvents.cs` (이벤트 목록 정의)

---

## ✅ 2단계 - 인게임 세션
- [x] `MachineController.cs` (HP/FUEL 시스템)
- [x] `IDamageable.cs` (인터페이스)
- [x] `BugBase.cs` (벌레 기본 클래스)
- [x] `BeetleBug.cs`, `FlyBug.cs`, `CentipedeBug.cs`
- [x] `BugSpawner.cs` (스폰 처리)
- [x] `AimController.cs` (마우스 커서 에임 + 0.5초 충전 + 사격)
- [x] `WaveManager.cs` (웨이브 순서 제어)

---

## ✅ 3단계 - UI
- [x] `UIManager.cs` (UI 패널 관리)
- [x] `MachineStatusUI.cs` (HP바 / FUEL바)
- [x] `AimChargeUI.cs` (에임 충전 게이지)
- [x] `MiningUI.cs` (채굴량 표시)
- [x] `SessionResultUI.cs` (세션 성공/실패 화면)

---

## 🔲 4단계 - 데이터 시트
- [ ] `BugData.cs` ScriptableObject
- [ ] `WaveData.cs` ScriptableObject
- [ ] `MachineData.cs` ScriptableObject
- [ ] 각 벌레별 .asset 파일 생성
- [ ] 웨이브별 .asset 파일 생성
- [ ] 밸런스 초기값 세팅

---

## 🔲 5단계 - 아웃게임 (프로토타입 이후)
- [ ] 타이틀 화면
- [ ] 강화 시스템 (머신 스탯 업그레이드)
- [ ] 캐릭터 선택/구매
- [ ] 무기 제작 (크래프팅)
- [ ] 옵션 (사운드, 언어 등)

---

## 씬 구성
| 씬 이름 | 설명 |
|---------|------|
| TitleScene | 타이틀 화면 (5단계에서 추가) |
| GameScene | 인게임 세션 |
| ResultScene | 세션 결과 화면 |
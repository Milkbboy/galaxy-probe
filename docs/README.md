# Drill-Corp 문서 인덱스

> 최종 갱신: 2026-04-27 — 30초 요약 + 5분 투어 추가. SimpleBug 전면 교체 + 레거시 문서 archive 이동 + 가이드 재작성 이력은 [CHANGELOG](Overview-Changelog.md).

## 30초 요약

- **장르** — 탑다운 로그라이트 디펜스 슈팅 서바이벌 (Unity 6 URP, Steam PC, 1920×1080)
- **컨셉** — 중앙 **채굴 머신**을 600~1,000 마리 벌레 떼로부터 방어. 플레이어는 이동 없이 마우스로만 조준·사격
- **승리/패배** — 채굴 목표량(`mineTarget`) 달성 = 승리 / 머신 HP 0 = 패배 (1세션 약 1분)
- **캐릭터 × 어빌리티** — 3 캐릭터 (지누스/사라/빅터) × 슬롯 3 = **어빌리티 9종**, 보석 해금 + req 체인
- **무기 5종** — 저격(기본 해금) → 폭탄 → 기관총 → 레이저 → 회전톱날, **동시 발동** (슬롯 전환 없음)
- **이중 재화** — 🪨 광석 (채굴+처치) / 💎 보석 (벌레 드랍 호버 채집)
- **데이터 주도** — Google Sheets 8 탭 → ScriptableObject Import (기획자 직접 튜닝)

## 5분 투어 (처음 본 사람용 읽기 순서)

1. **[Overview-Plan.md](Overview-Plan.md)** — 게임 컨셉·도파민 루프·Phase 진행 상황 (~10분)
2. **[Overview-Architecture.md](Overview-Architecture.md)** — Core / InGame / Data 계층 한눈에 (~5분)
3. **[Overview-DataStructure.md](Overview-DataStructure.md)** — Sheets→SO→Scene 데이터 흐름 + Bug/Wave/Machine/Upgrade 정의 (~10분)
4. **[V2-IntegrationPlan.md §8](V2-IntegrationPlan.md#8-구현-현황-2026-04-19)** — 현재 어디까지 구현됐는지 한 표로 (~5분)
5. **[Overview-Changelog.md](Overview-Changelog.md)** 최근 항목 — 가장 최근에 무엇이 바뀌었는지

> 작업 컨텍스트로 들어가려면 위 5개 후, 손대려는 시스템의 [🧩 시스템별](#-시스템별) 문서로 진입.

## 🎯 시작점

| 목적 | 문서 |
|---|---|
| 프로젝트 전체 기획/로드맵 | [Overview-Plan.md](Overview-Plan.md) |
| 아키텍처 개요 | [Overview-Architecture.md](Overview-Architecture.md) |
| 데이터 계층(Wave→Bug→Behavior) | [Overview-DataStructure.md](Overview-DataStructure.md) |
| 변경 이력 | [Overview-Changelog.md](Overview-Changelog.md) |

## 🚀 v2 아웃게임 통합 (Hub→Game 연결 + 보석 드랍/채집 완료, 어빌리티·승리조건 남음)

`docs/v2.html` 프로토타입의 아웃게임 고도화를 Unity에 이식하기 위한 계획·설계 문서.

**현재 상태**: Hub UI 5개 서브패널 + 데이터 v2 정렬 + **회전톱날·동시 발동 아키텍처** + **Hub→Game 강화/해금/캐릭터 반영** + **보석 드랍/채집 + 인게임 광석·보석 HUD** 완료 (2026-04-20). 어빌리티 9종 런타임·`mineTarget` 승리 조건만 남음. → 상세는 [V2-IntegrationPlan.md §8](V2-IntegrationPlan.md)

| 목적 | 문서 |
|---|---|
| **총론 — 갭 분석 · 씬 구성 판단 · 작업 순서** | [V2-IntegrationPlan.md](V2-IntegrationPlan.md) |
| 캐릭터 3종 + 어빌리티 9종 설계 | [Sys-Character.md](Sys-Character.md) |
| 캐릭터 고유 장비 v2.html 원본 분석 (수치 타이 브레이커) | [V2-CharacterAbilityReference.md](V2-CharacterAbilityReference.md) |
| 무기 해금 체인 · 무기별 강화 · 회전톱날 | [Sys-Weapon.md](Sys-Weapon.md) |
| 보석 드랍·채집 · 이중 재화 · mineTarget 승리 | [Sys-Gem.md](Sys-Gem.md) |
| 시트 스키마 (전체 8 탭) | [Data-SheetsGuide.md](Data-SheetsGuide.md) — 통합 (구 v2Addendum 흡수) |
| Hub UI 구현 트러블슈팅 (CSF 캐스케이드·patch-pattern·컬럼 폭 누수) | [V2-HubUI-Troubleshooting.md](V2-HubUI-Troubleshooting.md) |
| 프로토타입 원본 (HTML) | [v2.html](V2-prototype.html) |

## 🧩 시스템별

| 시스템 | 문서 |
|---|---|
| **Bug/Wave 시스템 (SimpleBug + SimpleWaveManager)** | [Overview-DataStructure.md](Overview-DataStructure.md) §1~4 |
| 무기 시스템 (v2 5종: Sniper/Bomb/Gun/Laser/Saw, self-driven 동시 발동) | [Sys-Weapon.md](Sys-Weapon.md) |
| 무기 시스템 (Phase 3 아카이브 — Shotgun/BurstGun/LockOn) | [archive/WeaponSystem.md](archive/WeaponSystem.md) |
| 보석 드랍/채집 + 이중 재화 + mineTarget 승리 조건 | [Sys-Gem.md](Sys-Gem.md) |
| VFX 3D 전환 (Polygon Arsenal 기반, 1차 완료) | [archive/VFX_3D_MigrationPlan.md](archive/VFX_3D_MigrationPlan.md) |
| 카메라 (Nuclear Throne 방식) | [Sys-Camera.md](Sys-Camera.md) |
| 미니맵 (RenderTexture) | [Sys-Minimap.md](Sys-Minimap.md) |
| 사운드 (AudioManager + 파형 편집 툴) | [Sys-Sound.md](Sys-Sound.md) |
| 최적화 현황 (VFX·DamagePopup 풀링 완료) | [Sys-Optimization.md](Sys-Optimization.md) |
| 최적화 이력 (1~4차 의사결정 연대기) | [Sys-Optimization-History.md](Sys-Optimization-History.md) |

> 구 BugBehaviors/Formation 시스템(폐기): [archive/BugBehaviorSystem.md](archive/BugBehaviorSystem.md) · [archive/BugBehaviorPatterns.md](archive/BugBehaviorPatterns.md) · [archive/FormationSystem.md](archive/FormationSystem.md)

## 🧱 Phase별 구현 기록 (전부 ✅ 머지 완료)

각 Phase 문서는 사전 계획서 + 결정 근거. 결과물은 시스템 문서에 반영됨. 단발성 의사결정 추적용.

| Phase | 문서 | 결과물 |
|---|---|---|
| 2 — 폭탄 무기 | [Phase2-Bomb.md](Phase2-Bomb.md) | [Sys-Weapon.md](Sys-Weapon.md) bomb |
| 3 — 기관총 | [Phase3-MachineGun.md](Phase3-MachineGun.md) | 동상 gun |
| 4 — 레이저 | [Phase4-Laser.md](Phase4-Laser.md) | 동상 laser |
| 5 — 빅터 어빌리티 (네이팜·화염방사기·지뢰) | [Phase5-Victor.md](Phase5-Victor.md) | [Sys-Character.md §5.1~5.3](Sys-Character.md) |
| 6 — 사라 어빌리티 (블랙홀·충격파·메테오) | [Phase6-Sara.md](Phase6-Sara.md) | [Sys-Character.md §5.4~5.6](Sys-Character.md) |
| 7 — 지누스 어빌리티 (드론 포탑·채굴 드론·드론 거미) | [Phase7-Jinus.md](Phase7-Jinus.md) | [Sys-Character.md §5.7~5.9](Sys-Character.md) |

## 🔧 진행 중인 리팩토링

활성 PLAN 문서. 트리거 조건 충족 시 작업 가능.

| 상태 | 문서 | 비고 |
|---|---|---|
| 🟢 재개 가능 | [Refactor-EditorOnlyCode.md](Refactor-EditorOnlyCode.md) | 런타임 폴더 `#if UNITY_EDITOR` 블록 마킹. 트리거 충족 (Live-tuning 훅 3개) |

## 📊 데이터 / 연동

| 목적 | 문서 |
|---|---|
| 시트 컬럼 정의 + Import 규칙 (전체 8 탭) | [Data-SheetsGuide.md](Data-SheetsGuide.md) |
| **기획자 전달용** 워크플로우 + 시나리오별 가이드 | [Data-PlannerGuide.md](Data-PlannerGuide.md) |

스프레드시트: https://docs.google.com/spreadsheets/d/1hwgQ4IF-gQqVSX4xS_uqeKIPWUDy2NR4bC-OWmZQO_E/edit

## 📖 개발 로그 (블로깅 원본, 보존)

| # | 주제 |
|---|---|
| 01 | [코어 시스템](DevLog-01-Core.md) |
| 02 | [인게임 세션](DevLog-02-InGame.md) |
| 03 | [UI](DevLog-03-UI.md) |
| 04 | [데이터 시트](DevLog-04-DataSheet.md) |
| 05 | [아웃게임](DevLog-05-OutGame.md) |

## 🗂️ 아카이브

`archive/` 폴더 — 참고용 과거 문서(프로토타입 분석, 완료된 구현 계획 등).
현재 구현과 분리되어 있으니 최신 정보는 위 시스템 문서를 우선 참조.

주요 자료:
- `_v1prototype.html` / `.png` — v1 웹 프로토타입 원본 (Phase 2~4 무기 이식 근거)
- `AIM_PROTOTYPE.md` / `WEAPON_IMPLEMENTATION_PLAN.md` — v1 프로토 무기 4종 분석/이식 계획 (v2 5종으로 후속됨, → [Sys-Weapon.md](Sys-Weapon.md))
- `REFACTORING_PLAN.md` — 2026-04-16 작성. 다수 항목이 v2 전환으로 자연 해소
- `VFX_3D_MigrationPlan.md` — 1차 구현 완료 (MachineGun/Bomb/Shotgun/Laser 스코치)
- `GoogleSheetsGuide_v2Addendum.md` — 본 가이드에 흡수됨 (SUPERSEDED)
- `_review_initial_sheet_data/` — 8 탭 초기 입력 CSV/TSV (시트 통합 완료 후 보존)

## 🖼️ 리소스

- 다이어그램 원본: `diagrams/BugBehaviorSystem.drawio`
- 이미지: `image/`

---

## 빠른 참조 (SimpleBug 시트 컬럼)

**SimpleBugData**: `BugName, Kind(Normal/Elite/Swift), BaseHp, HpPerWave, BaseSpeed, SpeedPerWave, SpeedRandom, Size, Score, TintHex`
**WaveData**: `WaveNumber, WaveName, KillTarget, NormalSpawnInterval, EliteSpawnInterval, MaxBugs, TunnelEnabled, TunnelEventInterval, SwiftPerTunnel`

`-1`/빈 셀 = SpawnConfig 폴백. `0` = 명시적 값. 예외: `EliteSpawnInterval`·`KillTarget` 의 `-1`/`0` 은 "비활성"·"전환 없음".

전체 스키마: [Data-SheetsGuide.md](Data-SheetsGuide.md) · 런타임 의미: [Overview-DataStructure.md](Overview-DataStructure.md).

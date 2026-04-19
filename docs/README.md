# Drill-Corp 문서 인덱스

> 최종 갱신: 2026-04-20

## 🎯 시작점

| 목적 | 문서 |
|---|---|
| 프로젝트 전체 기획/로드맵 | [DRILL-CORP-PLAN.md](DRILL-CORP-PLAN.md) |
| 아키텍처 개요 | [Architecture.md](Architecture.md) |
| 데이터 계층(Wave→Bug→Behavior) | [DataStructure.md](DataStructure.md) |
| 변경 이력 | [CHANGELOG.md](CHANGELOG.md) |

## 🚀 v2 아웃게임 통합 (Hub→Game 연결 + 보석 드랍/채집 완료, 어빌리티·승리조건 남음)

`docs/v2.html` 프로토타입의 아웃게임 고도화를 Unity에 이식하기 위한 계획·설계 문서.

**현재 상태**: Hub UI 5개 서브패널 + 데이터 v2 정렬 + **회전톱날·동시 발동 아키텍처** + **Hub→Game 강화/해금/캐릭터 반영** + **보석 드랍/채집 + 인게임 광석·보석 HUD** 완료 (2026-04-20). 어빌리티 9종 런타임·`mineTarget` 승리 조건만 남음. → 상세는 [V2_IntegrationPlan.md §8](V2_IntegrationPlan.md)

| 목적 | 문서 |
|---|---|
| **총론 — 갭 분석 · 씬 구성 판단 · 작업 순서** | [V2_IntegrationPlan.md](V2_IntegrationPlan.md) |
| 캐릭터 3종 + 어빌리티 9종 설계 | [CharacterAbilitySystem.md](CharacterAbilitySystem.md) |
| 무기 해금 체인 · 무기별 강화 · 회전톱날 | [WeaponUnlockUpgradeSystem.md](WeaponUnlockUpgradeSystem.md) |
| 보석 드랍·채집 · 이중 재화 · mineTarget 승리 | [GemMiningSystem.md](GemMiningSystem.md) |
| 신규 4개 시트 스키마 · UpgradeData 확장 | [GoogleSheetsGuide_v2Addendum.md](GoogleSheetsGuide_v2Addendum.md) |
| Hub UI 구현 트러블슈팅 (CSF 캐스케이드·patch-pattern·컬럼 폭 누수) | [V2HubUI_Troubleshooting.md](V2HubUI_Troubleshooting.md) |
| 프로토타입 원본 (HTML) | [v2.html](v2.html) |

## 🧩 시스템별

| 시스템 | 문서 |
|---|---|
| Bug 행동 (BugController + Behaviors) | [BugBehaviorSystem.md](BugBehaviorSystem.md) |
| Bug 행동 — 기획자 가이드/예시 | [BugBehaviorPatterns.md](BugBehaviorPatterns.md) |
| 무기 시스템 (v2 5종: Sniper/Bomb/Gun/Laser/Saw, self-driven 동시 발동) | [WeaponUnlockUpgradeSystem.md](WeaponUnlockUpgradeSystem.md) |
| 무기 시스템 (Phase 3 아카이브 — Shotgun/BurstGun/LockOn) | [WeaponSystem.md](WeaponSystem.md) |
| 보석 드랍/채집 + 이중 재화 + mineTarget 승리 조건 | [GemMiningSystem.md](GemMiningSystem.md) |
| **VFX 3D 전환 계획 (Polygon Arsenal 기반)** | [VFX_3D_MigrationPlan.md](VFX_3D_MigrationPlan.md) |
| 군집(Formation) 스폰 | [FormationSystem.md](FormationSystem.md) |
| 카메라 (Nuclear Throne 방식) | [CameraSystem.md](CameraSystem.md) |
| 미니맵 (RenderTexture) | [MinimapSystem.md](MinimapSystem.md) |
| 사운드 (AudioManager + 파형 편집 툴) | [SoundSystem.md](SoundSystem.md) |

## 📊 데이터 / 연동

| 목적 | 문서 |
|---|---|
| 시트 컬럼 정의 + 행동 파싱 문법 + Import 규칙 | [GoogleSheetsGuide.md](GoogleSheetsGuide.md) |
| v2 신규 시트 (Character/Weapon/WeaponUpgrade/Ability) + UpgradeData 확장 | [GoogleSheetsGuide_v2Addendum.md](GoogleSheetsGuide_v2Addendum.md) |

스프레드시트: https://docs.google.com/spreadsheets/d/1hwgQ4IF-gQqVSX4xS_uqeKIPWUDy2NR4bC-OWmZQO_E/edit

## 📖 개발 로그 (블로깅 원본, 보존)

| # | 주제 |
|---|---|
| 01 | [코어 시스템](DevLog_01_CoreSystem.md) |
| 02 | [인게임 세션](DevLog_02_InGameSession.md) |
| 03 | [UI](DevLog_03_UI.md) |
| 04 | [데이터 시트](DevLog_04_DataSheet.md) |
| 05 | [아웃게임](DevLog_05_OutGame.md) |

## 🗂️ 아카이브

`archive/` 폴더 — 참고용 과거 문서(프로토타입 분석, 완료된 구현 계획 등).
현재 구현과 분리되어 있으니 최신 정보는 위 시스템 문서를 우선 참조.

## 🖼️ 리소스

- 다이어그램 원본: `diagrams/BugBehaviorSystem.drawio`
- 이미지: `image/`

---

## 빠른 참조 (Bug 행동 문자열)

```
Passives:  Armor:5 / Shield:20:2 / Dodge:30 / Regen:3 / PoisonAttack:3:5
Skills:    Nova:5:10:3 / BuffAlly:10:50:4 / HealAlly:6:10:4 / Spawn:8:Beetle:2
Triggers:  Enrage:30:50 / ExplodeOnDeath:10:2 / PanicBurrow:50:5
```

전체 문법·타입표는 [GoogleSheetsGuide.md](GoogleSheetsGuide.md#패시브스킬트리거-파싱-문법) 참조.

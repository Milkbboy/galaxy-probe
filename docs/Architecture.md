# 아키텍처 패턴

> 최종 갱신: 2026-04-15

## 전체 구조

```
Core (전역) ── GameManager / DataManager / AudioManager
      │  (GameEvents 이벤트로 통신)
InGame ── MachineController / WaveManager / AimController
       ├─ Bug     : BugController ←→ Behaviors/ (신규 조합형)  |  BugBase 계열 (레거시)
       │           BugManager / BugPool / OffscreenVisibilityTracker
       │           Formation/ (FormationSpawner, FormationGroup, FormationMember)
       │           Simple/   (SimpleBug, SimpleBugSpawner, TunnelEventManager)
       ├─ Weapon  : WeaponBase (self-driven) + WeaponGauge
       │           Sniper / Bomb / MachineGun / Laser / Saw  (v2 5종)
       │           (legacy: Shotgun / BurstGun / LockOn — 비활성)
       ├─ Camera  : DynamicCamera + CameraSettingsData
       └─ UI      : Minimap (MinimapCamera/UI/Icon), HPBar, FuelBar ...
Data (SO)  BugData / BugBehaviorData / WaveData / FormationData
           WeaponData (+ Sniper/Bomb/MachineGun/Laser/Saw Data) + WeaponUpgradeData
           CharacterData / AbilityData (v2 어빌리티 SO)
           MachineData / UpgradeData / CameraSettings / BugPoolConfig
```

---

## 1. 싱글톤 (전역 매니저)
`GameManager`(씬·게임 상태), `DataManager`(재화·강화·저장), `AudioManager`(사운드)는 `Instance`로 접근.
과용 금지 — 씬 로컬 매니저는 DI/참조로 연결.

## 2. 이벤트 (GameEvents)
결합도를 낮추기 위해 정적 `Action` 사용.
```csharp
public static class GameEvents {
    public static Action<int> OnMachineDamaged;
    public static Action OnBugDied, OnSessionSuccess, OnSessionFail;
}
```
흐름 예: 벌레 공격 → `OnMachineDamaged` → MachineController HP↓ / UI 갱신 / GameManager 종료 체크.

## 3. ScriptableObject (데이터 주도)
코드 수정 없이 인스펙터에서 튜닝. 시트 Import로 자동 생성(→ `GoogleSheetsGuide.md`).

## 4. 인터페이스 & 다형성
- `IDamageable.TakeDamage(float)` — 사격/투사체/근접 모두 이 인터페이스만 알면 됨.
- `IBehavior` 계열 5종 (Movement/Attack/Skill/Passive/Trigger) — BugController가 조합해서 실행.

---

## 5. Bug 계층: 공존 중인 두 시스템

| 시스템 | 위치 | 특징 | 상태 |
|---|---|---|---|
| **BugController + Behaviors** | `Bug/BugController.cs`, `Bug/Behaviors/` | 조합형. SO로 이동·공격·패시브·스킬·트리거 주입. Google Sheets Import 지원 | 현재 표준 |
| **BugBase 상속** | `Bug/BugBase.cs`, `BeetleBug`/`FlyBug`/`CentipedeBug` | 클래스별 Override. 구형 프리펩 호환 | Phase 5에서 제거 예정 |
| **SimpleBug** | `Bug/Simple/` | 풀링 최적화 경량 벌레. 대량 스폰(수백~수천) 용도 | 상시 사용 |
| **Formation** | `Bug/Formation/` | 리더(FormationGroup) + 멤버(FormationMember) 오프셋. 600~1000 군집 | 상시 사용 |
| **Pool/Visibility** | `Bug/Pool/`, `OffscreenVisibilityTracker.cs` | 오브젝트 풀 + 오프스크린 숨김으로 드로우콜 절감 | 상시 사용 |

상세: `BugBehaviorSystem.md`, `FormationSystem.md`

## 6. Weapon 시스템
`WeaponBase`(abstract) ← Sniper/Bomb/MachineGun/Laser/Saw 5종 + 각 `*Data` SO. v2 포팅 후 **self-driven** 패턴 — 각 무기가 자체 `Update()`에서 `TryFire(_aimController)` 호출, 해금 무기 전부 병렬 동작 (`WeaponSwitcher`·`AimController.EquipWeapon` 제거됨). `AimController`는 에임 위치·범위·머신 참조만 공급. UI는 `WeaponPanelUI` + `WeaponSlotUI` + 무기별 `*AimRingBinder`.
상세: `WeaponUnlockUpgradeSystem.md` (v2 현행), `WeaponSystem.md` (Phase 3 아카이브)

## 7. Camera 시스템
`DynamicCamera`가 Nuclear Throne 방식(머신-커서 중간점 Lerp)으로 추적. `CameraSettingsData` SO로 파라미터 튜닝, `DebugCameraUI`로 런타임 조정.
상세: `CameraSystem.md`

## 8. Minimap
RenderTexture 기반 2차 카메라(`MinimapCamera`) + `MinimapUI` + 실시간 아이콘(`MinimapIcon`).
상세: `MinimapSystem.md`

---

## 9. 탑다운 좌표계 주의
X=좌우, **Y=높이(고정)**, Z=상하. 화면상 "위"는 `Vector3.forward`(+Z). 상세: `CLAUDE.md` "탑다운 좌표계" 섹션.

## 10. 참고
- 데이터 계층: `DataStructure.md`
- Bug 행동 시스템: `BugBehaviorSystem.md`
- 시스템별: `WeaponSystem.md` / `FormationSystem.md` / `CameraSystem.md` / `MinimapSystem.md`
- 시트/Import: `GoogleSheetsGuide.md`
- 전체 기획/로드맵: `DRILL-CORP-PLAN.md`

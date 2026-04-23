# 최적화 3차 — 현재 Game 씬 실제 핫패스 재조사

> 작성 2026-04-23 · 트리거 Optimization_02 §5 후속 ("735MB 누수의 실체" 규명)
> 조사 범위: Game.unity · Assets/\_Game/Scripts/ 전체 (BugController/BugBase 계열 제외)
> 방법론: Game.unity yaml 의 `m_Script guid` 를 스크립트 .meta guid 와 매칭해 **실제 씬에 배치된 MonoBehaviour 만** 추출 → Start/OnEnable/Update/LateUpdate/Coroutine 전수 리뷰

## 배경

Optimization_02 는 기획자 PC 실측 CSV 에서 **baseline(벌레 0) 세션에 GCUsed 735MB · 스파이크 75→115ms 점진 증가**를 관측하고, §3 에서 BugHpBar / DamagePopup / VFX 풀링을 1차 용의자로 지목했다. 그러나 이들은 전부 **벌레 피격 / 벌레 사망 / 충돌 이벤트**가 트리거 — **벌레 0 baseline 에선 실행되지 않는 코드**다. 즉 Optimization_02 의 Phase A0-1~A0-4 가 실제 누수 경로를 잘못 겨냥했다.

본 문서는 "**벌레 0 · 어빌리티 미사용 · 드론 미소환** 상태에서도 세션 내내 Update 루프를 도는 컴포넌트"만 추려, 그 중 **관리 힙에 지속 누적되는 지점** 을 코드 리뷰로 재추적한다.

## 1. 현재 Game 씬에 실제로 활성된 컴포넌트 전수 목록

Game.unity (11818 줄) 에서 `m_Script guid` → `.cs.meta` 매칭으로 얻은 사용자 스크립트 **35종**. Unity 내장(TMP/UGUI/URP) 제외.

### 코어 / 세션
- `GameManager` — 씬 전환
- `DataManager` — 영구 세이브 (싱글턴, DontDestroyOnLoad)
- `DebugManager` — I/H/K 디버그 키 + OnGUI
- `MachineController` — 머신 HP/채굴/세션 상태
- `TurretController` — 포탑 배럴 회전

### HUD / UI (스크린 공간)
- `TopBarHud` — 상단 6슬롯
- `AbilityHud` + `AbilitySlotUI`×3 — 우상단 어빌리티 슬롯
- `AbilitySlotController` — 어빌리티 실행 + 슬롯 키 입력
- `WeaponPanelUI` + `WeaponSlotUI`×N — 좌측 무기 슬롯
- `GemCounterUI`, `SessionResultUI`, `UIManager`, `TMPFontHolder`

### 에임 / 조준
- `AimController` — 마우스 위치 + `OverlapSphereNonAlloc` 매 프레임
- `AimWeaponRing` × **4** — 무기별 쿨다운 진행 호 (동적 메시)
- `SniperAimRingBinder`, `MachineGunAimRingBinder`, `BombAimRingBinder`, `LaserAimRingBinder`

### 무기 (씬에 상시 활성, 모두 자체 Update 구동)
- `SniperWeapon` — 범위 내 벌레 있을 때만 발사 (baseline: 0회)
- `MachineGunWeapon` — **적 유무 무관 자동 연사** (ShouldFire: ammo 있으면 true)
- `BombWeapon` — 마우스 좌클릭 필요 (baseline: 0회)
- `LaserWeapon` — **적 유무 무관 쿨다운마다 빔 스폰** (ShouldFire: `_activeBeam == null && _laserCD <= 0f`)
- `SawWeapon` — 쿨다운 없음, 상시 블레이드 궤도 + 틱 데미지 OverlapSphere

### 어빌리티
- `AbilitySlotController` + 3개의 `IAbilityRunner` (캐릭터별 자동 생성)
- Runners (빅터 기준): NapalmRunner / FlameRunner / MineRunner — 모두 **사용자 입력(키 1/2/3)** 필요 → baseline: 비활성

### 스포너 / 생태
- `SimpleBugSpawner` — `IsActive=1, Enabled=1`, `_spawnInterval=0.083s`, `_maxBugs=90` — **scene yaml 상 활성 상태**
- `TunnelEventManager` — 30초 후 첫 땅굴 이벤트 (15초 주기)
- `GemDropSpawner` — 이벤트 구독만 (벌레 사망 시)
- `FormationSpawner` — Spawn() 외부 호출 필요, Update 없음 → baseline 에선 inert
- `BugPool` — 싱글턴, Awake 1회 풀 초기화 후 Update 없음

### 카메라 / 미니맵
- `DynamicCamera` (LateUpdate)
- `MinimapCamera` (LateUpdate)
- `MinimapUI`, `DebugCameraUI` (F1 토글)

### Diagnostics (자동 부트스트랩)
- `PerfRecorder` — `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` 로 **씬 배치 없이도 자동 생성**. DontDestroyOnLoad.

## 2. 벌레 0 에서도 돌아가는 Update 루프 확인

PerfMarker 로 이미 등록되어 있고 CSV 상 `0.00 ms` 가 아닌 (실제로 매 프레임 돌고 있는) 후보:

| 컴포넌트 | 루프 | baseline 거동 |
|---|---|---|
| `AimController.Update` (Aim/AimController.cs:245) | Update | 매 프레임 Mouse 위치 레이캐스트 + `OverlapSphereNonAlloc(128)` |
| `AimWeaponRing.LateUpdate` (Aim/AimWeaponRing.cs:172) | LateUpdate × 4 | **매 프레임 `new Vector3[130] + new Vector2[130] + new int[384]` 할당** (캐시 무효 시) |
| `MachineGunWeapon.Update` → Fire (Weapon/MachineGun/MachineGunWeapon.cs:126, 167) | Update | 적 0 이어도 매 `FireDelay` 마다 `Instantiate(BulletPrefab)` |
| `LaserWeapon.Update` → Fire (Weapon/Laser/LaserWeapon.cs:121, 142) | Update | 적 0 이어도 쿨다운 리프레시마다 `Instantiate(BeamPrefab) + Instantiate(ScorchPrefab)` |
| `SawWeapon.Update` (Weapon/Saw/SawWeapon.cs:101) | Update | 매 프레임 블레이드 회전 + tick 간격 `OverlapSphereNonAlloc` |
| `MachineController.Update` (Machine/MachineController.cs:127) | Update | `_miningAccumulator` 누적 + 정수 넘어갈 때마다 GameEvents 이벤트 |
| `TopBarHud.Update` (UI/HUD/TopBarHud.cs:166) | Update | `$"체력 {int}"` 문자열 매 프레임 |
| `AbilityHud.Update` (UI/HUD/AbilityHud.cs:35) | Update | `AbilitySlotUI.Refresh()` × 3 |
| `DynamicCamera.LateUpdate` (Camera/DynamicCamera.cs:50) | LateUpdate | 마우스→타겟 블렌드 |
| `MinimapCamera.LateUpdate` (UI/Minimap/MinimapCamera.cs:43) | LateUpdate | 머신 따라가기 |
| `SimpleBugSpawner.Update` (Bug/Simple/SimpleBugSpawner.cs:50) | Update | **scene yaml 상 활성 — PruneDead + 0.083s 주기 SpawnNormal** |
| `TunnelEventManager.Update` (Bug/Simple/TunnelEventManager.cs:60) | Update | 30초 이후 땅굴 이벤트 |
| `PerfRecorder.Update / SampleFrame / OnGUI` | Update + OnGUI | **녹화 중 매 프레임 `_channels[i].Samples.Add()` + OnGUI 에서 `new GUIStyle()` 매 호출** |
| `DebugManager.Update / OnGUI` | Update + OnGUI | OnGUI 에서 GUILayout 매 호출 |
| `TurretController.Update` (Machine/TurretController.cs:52) | Update | `Quaternion.RotateTowards` |
| `DebugCameraUI.Update` | Update | F1 키 체크만 |
| `AbilitySlotController.Update` (Ability/AbilitySlotController.cs:83) | Update | `runner.Tick(dt)` × 3 + 키 1/2/3 체크 |

## 3. 누수 의심 지점 발견 (우선순위 순)

### 3.1 **`PerfRecorder.SampleFrame` — `Channel.Samples` List 무한 누적** [상]

- **파일**: `Assets/_Game/Scripts/Diagnostics/PerfRecorder.cs:214-225`
- **패턴**: #7 매 프레임 마이크로-할당이지만 long-lived 참조에 매달려 GC 못타는 경우

```csharp
private void SampleFrame()
{
    for (int i = 0; i < _channels.Count; i++)
    {
        var c = _channels[i];
        long raw = c.Rec.LastValue;
        double v = ToDisplayValue(raw, c.Unit);
        c.Sum += v;
        if (v > c.Max) c.Max = v;
        c.Count++;
        c.Samples.Add(v);   // ← 녹화 중 세션 내내 Add-only
    }
    // ... spike 기록 (StringBuilder)
}
```

- **왜 누수인가**: `PerfRecorder` 는 `DontDestroyOnLoad` 싱글턴. `_channels` 는 인스턴스 필드. 녹화(F9) 시작 시 각 `Channel` 이 `Samples = new List<double>(4096)` 으로 초기화되고, 이후 **매 프레임(~144fps 기획자 PC) 마다 모든 채널에 `Samples.Add(double)` 수행**. `_channels.Count` = MarkerSpecs 25개 중 `.Valid` 필터 통과한 수(~20~25). `StopAndSave()` 에서 `_channels.Clear()` 전까지 리스트는 절대 shrink 안 됨.
- **예상 기여도 (baseline 46.8s / 6763 frames)**: 
  - 채널 20 × 6763 프레임 × 8 bytes = **1.03 MB** (samples) + List 헤더/resize 오버헤드 = 실측 약 1.5~2 MB
  - List 용량은 내부적으로 4096 → 8192 → 16384 로 2배씩 증가 — resize 순간 **옛 배열을 LOH 경계 근처까지 남겨둠**
  - 735MB 중 직접 기여: ~0.3% (소규모) — **그러나 녹화 자체가 측정 노이즈**. baseline 측정 동안 녹화기가 돌고 있었다는 의미.
- **해결 방향**:
  1. p50/p95/p99 계산을 위해 전 샘플 보관할 필요 없음 → **온라인 백분위 (P²algorithm / t-digest)** 로 교체
  2. 또는 `Samples` 를 circular buffer `double[1024]` 로 바꿔 용량 고정
  3. 당장은 **baseline 측정 시 녹화 OFF 후 별도 F9 측정** 으로 노이즈 제거 검증 (가장 싸다)

---

### 3.2 **`PerfRecorder.OnGUI` — 프레임당 `new GUIStyle()` 반복** [상·알로케이션]

- **파일**: `Assets/_Game/Scripts/Diagnostics/PerfRecorder.cs:427`
- **패턴**: #3 상시 회전 Instantiate 유사 (임시 객체 생성). 누적은 아니지만 **25KB/frame 베이스라인의 주 기여자**.

```csharp
private void OnGUI()
{
    // ...
    var style = new GUIStyle(GUI.skin.label) { richText = true };   // ← 매 OnGUI 호출
    // ...
    if (!string.IsNullOrEmpty(_bannerText) && ...)
    {
        var bannerStyle = new GUIStyle(GUI.skin.box) { ... };       // ← 추가 할당
    }
}
```

- **왜 문제인가**: OnGUI 는 **프레임당 2회 이상** 호출 (Layout/Repaint 이벤트). `GUIStyle` 은 내부적으로 `GUIStyleState`×7, `Font` ref, margin/padding 등 포함 — 1개당 1KB+. 기획자 PC 144fps 에서 `~288 호출/s × 1KB = 288KB/s` alloc 발생. 46.8s 세션 = **13~14 MB 누적 alloc** (모두 단명 → GC 로 회수). 누수는 아니지만 **GC 스파이크의 직접 연료**.
- **예상 기여도**: 25KB/frame 중 ~2KB/frame (8%) 수준. 735MB 누적에 직접 기여는 X, **스파이크 빈도·크기 증가에는 기여**.
- **해결 방향**: `_labelStyle` / `_bannerStyle` 을 `private static readonly GUIStyle` 로 static-cache. `OnGUI` 진입 시 1회 lazy init.

---

### 3.3 **`AimWeaponRing.RebuildMesh` — 프레임당 verts/uvs/tris 배열 3개 할당** [상·알로케이션]

- **파일**: `Assets/_Game/Scripts/Aim/AimWeaponRing.cs:177-247`
- **패턴**: #7 매 프레임 마이크로-할당 (단명). 누수 아님, 다만 베이스라인 25KB/frame 의 주요 기여자.

```csharp
private void RebuildMesh(bool force)
{
    // ... 캐시 체크 (_fillAmount 변동 시 통과)
    int vertCount = (seg + 1) * 2;
    var verts = new Vector3[vertCount];    // ← ~1.5KB
    var uvs = new Vector2[vertCount];      // ← ~1KB
    var tris = new int[seg * 6];           // ← ~1.5KB
    // ... mesh.vertices = verts; mesh.uv = uvs; mesh.triangles = tris;
}
```

- **왜 문제인가**: 씬에 `AimWeaponRing` 4개. MachineGun/Laser 가 매 프레임 쿨다운 진행 → `_fillAmount` 가 프레임마다 바뀌어 캐시 miss → 리빌드. 4개 × 4KB × ~144fps = **2.3 MB/s alloc**. 46.8s = **108 MB 할당** (단명, 전부 GC 대상). `Mesh.vertices = verts` 대입 시 Unity 가 내부 복사 → verts 는 임시 객체.
- **예상 기여도**: 735MB GCUsed 중 직접 누수는 0 이나, **25KB/frame baseline 의 주된 원인** (프레임당 ~16KB — TMP 재할당보다 큼).
- **해결 방향**:
  1. 클래스 필드로 `Vector3[] _verts; Vector2[] _uvs; int[] _tris;` 캐시해 `_segments` 변화 시에만 재할당
  2. Unity 6 권장 경로: `Mesh.SetVertices(NativeArray<>/Span<>)` + `MeshDataArray` 로 alloc 없는 업데이트
  3. 또는 LineRenderer 로 치환 (AimController 의 링이므로 얇은 선으로 대체 가능)

---

### 3.4 **`MachineGunWeapon` / `LaserWeapon` — 적 0 에서도 매 쿨마다 Instantiate** [중·확인 필요]

- **파일**: 
  - `Assets/_Game/Scripts/Weapon/MachineGun/MachineGunWeapon.cs:146-165` (`ShouldFire` 가 적 유무 무시)
  - `Assets/_Game/Scripts/Weapon/Laser/LaserWeapon.cs:139-140` (`ShouldFire => _activeBeam == null && _laserCD <= 0f`)
- **패턴**: #3 Instantiate 후 Destroy 경로 있으나 프레임당 대량 churn

```csharp
// MachineGunWeapon.ShouldFire (L146)
protected override bool ShouldFire(AimController aim)
{
    if (_isReloading) { ... return false; }
    if (_data == null || _currentAmmo <= 0) return false;
    // 자동 연사 — 적 유무 무관, 에임 방향으로 계속 발사
    return true;
}

// LaserWeapon.ShouldFire (L139)
protected override bool ShouldFire(AimController aim) =>
    _activeBeam == null && _laserCD <= 0f;
```

- **왜 문제인가**: 사용자는 "baseline = 어빌리티 미사용, 벌레 0" 이라고 설명했지만, **무기는 여전히 매 프레임 TryFire 호출**. 스나이퍼/폭탄은 안 쏘지만 **기관총은 40발/5초 재장전 사이클로 계속 발사**, **레이저는 6초 수명 + 5초 쿨다운 사이클**로 계속 스폰. MachineGunBullet / LaserBeam / BombLandingMarker 들은 Destroy(gameObject) 경로로 self-cleanup — 힙 누수 아님. 
- **예상 기여도 (46.8s)**:
  - MG: 40발 / 5.6초(40발 × 0.14s) + 5초 리로드 = 10.6s 주기 → ~4.4 사이클 → 176 발 → 각 발 ~500 bytes(GO + MB + Collider) Instantiate = **88KB + Destroy GC 부담**
  - Laser: 11s 사이클 × 4 = 4 빔 + 4 scorch — 거의 무시할 수준
  - **힙 누수 아님. 단 Instantiate/Destroy 의 GC 압력 증가로 스파이크 기여**
- **해결 방향**: 
  1. **Optimization_02 § 4.1 A0 Phase** 가 제안한 "무기 발사체 풀링" — MachineGunBullet 풀 도입
  2. 또는 **baseline 측정 시 무기 GameObject 모두 disable** 하는 전용 디버그 토글 (조사 정확도↑)

---

### 3.5 **`SimpleBugSpawner` — scene yaml 상 활성 상태 (baseline 전제와 충돌)** [확인 필요]

- **파일**: `Assets/_Game/Scripts/Bug/Simple/SimpleBugSpawner.cs:50-67`
- **패턴**: #3 주기적 Instantiate

```csharp
private void Update()
{
    PruneDead();
    _eliteTimer -= Time.deltaTime;
    if (_eliteTimer <= 0f) { SpawnElite(); _eliteTimer = _eliteInterval; }
    _spawnTimer -= Time.deltaTime;
    if (_spawnTimer <= 0f) { SpawnNormal(); _spawnTimer = _spawnInterval; }
}
```

Game.unity yaml (L6033-6056) 확인:
```yaml
m_IsActive: 1            # GameObject 활성
m_Enabled: 1             # MonoBehaviour 활성
_normalData: {fileID: ..., guid: ed01e0c872c5f01448ce8a785b5ff279}  # bound
_spawnInterval: 0.083    # 12Hz
_eliteInterval: 15
_maxBugs: 90
```

- **왜 누수인가 (조건부)**: 사용자 설명 "벌레 0 baseline" 이 **scene 상태와 충돌**. yaml 에서는 spawner 활성이라 Play 시작 즉시 12Hz 로 벌레를 Instantiate 한다. 첫 스폰은 카메라 외곽 원 — 화면 밖에 나타나 머신으로 행진. 46.8s 안에 최대 90마리(cap)까지 쌓이고, 각 벌레는:
  - `SimpleBug` MB + 자동 `SphereCollider` 추가
  - `MinimapIcon` — 새 GameObject + MeshFilter + MeshRenderer + (캐시된) 메시/매트리얼
- **예상 기여도**: 벌레 90 × ~5KB = ~450KB 관리 힙 + Physics/Render 서브시스템 네이티브 영역. **baseline 이 정말 0 이라면 사용자가 런타임에 spawner GameObject 를 비활성화** 했거나 **scene 수정이 커밋 안 됐거나** 둘 중 하나. 이 전제가 뒤집히면 Optimization_02 의 "735MB 수수께끼" 가 상당 부분 (벌레 90마리 × BugHpBar Texture2D 등) 에서 설명됨.
- **해결 방향**: 
  1. **우선 확인**: 기획자 PC 에서 녹화 시작 전 Hierarchy 에 `SimpleBugSpawner` GameObject 가 비활성 / `_normalData` 가 비어있는지 스크린샷 요청
  2. 코드 자체에는 누수 없음 (PruneDead 로 dead 제거, cap 90)
  3. 만약 "정말 spawner 꺼뒀다" 라면 본 항목 제거

---

### 3.6 **`TopBarHud.Update` / `AbilityHud.Update` — 문자열 포맷 매 프레임** [하·베이스라인 기여]

- **파일**: `UI/HUD/TopBarHud.cs:135-163`, `UI/HUD/AbilityHud.cs:35-47` (+ `AbilitySlotUI.Refresh`)
- **패턴**: #7 매 프레임 문자열 alloc (단명)

```csharp
// TopBarHud.UpdateHealth — 매 프레임 호출
_healthText.text = $"체력 {Mathf.CeilToInt(_machine.CurrentHealth)}";
```

- **왜 문제인가**: 숫자가 바뀌지 않아도 매 프레임 interpolated string → boxing → new string. TMP `.text` setter 가 content 비교 스킵 처리 (built-in), 그래도 **string alloc 자체는 발생**.
- **예상 기여도**: `체력 N` 류 × ~6개 슬롯 × 144fps = ~4.3KB/s string alloc. 46.8s = ~200KB churn. 누적 아님, 역시 25KB/frame baseline 기여.
- **해결 방향**: `private int _lastHealthShown = -1;` 캐시해 변경 시에만 갱신. AbilitySlotUI 의 `OverlayText` / `StateText` 도 동일.

---

## 4. 확인했으나 문제 없는 항목

재조사 시 중복 방지를 위한 whitelist. **벌레 0 baseline 에서 누수 기여 없음**으로 확인.

- **`MachineController`** (Machine/MachineController.cs) — 이벤트 구독/해제 균형. Update 에 Mining 누적만. 누수 없음.
- **`GameEvents` static Action delegates** — 구독자 모두 OnEnable/OnDisable 균형. `grep += == 53 / -= == 53`. 누수 없음.
- **`DataManager`** — Awake 1회 + 세이브 시점만 동작. Update 없음.
- **`GameManager`, `UIManager`** — 씬 전환 이벤트만.
- **`BombWeapon`** — 마우스 클릭 필요, baseline 0회 Fire.
- **`SniperWeapon`** — `base.ShouldFire = aim.HasBugInRange` 기본. baseline 0회 Fire.
- **`NapalmRunner / FlameRunner / MineRunner`** — `TryUse(aim)` 는 키 1/2/3 필요. Tick 만 돌지만 `_cooldown` 감소와 `_zones` 정리만. 활성 zone 0 → O(1).
- **`AudioManager`** — pool 8개 고정. `_sfxPool` 배열 readonly. 누수 없음.
- **`GemDropSpawner`** — OnBugDied 구독. 벌레 0 → 핸들러 0회.
- **`BugPool`** — Awake 에 InitialSize 만큼 Instantiate 후 Update 없음. baseline 에 Get 호출 없음.
- **`MinimapCamera`, `DynamicCamera`, `TurretController`** — 위치/회전 갱신만. 힙 사용 없음.
- **`AimController`** — `_cachedBugs` 는 Clear() 후 OverlapSphere 결과로 refresh. `_overlapBuffer[128]` 재사용 버퍼.
- **`MinimapIcon`** — **static Dictionary<Color, Material>** 캐시는 unbounded 처럼 보이지만 **실제로는 색상 종류 상한이 작음** (벌레 종류 × 엘리트 등 < 20). 누수 아님.
- **`TunnelEventManager`** — baseline 30s 이후 1회 이벤트. `_tunnels`/`_warnings` 은 lifetime 끝나면 제거. 다만 L114 `_markerObjects[i]` 인덱스 동기화 버그 있음 (마커 프리펩 null 이면 두 리스트 길이 불일치) — **기능 버그이지 누수 아님**.
- **`WaveManager`, `MachineStatusUI`, `MiningUI`, `Hp3DBar`, `BugHpBar`, `DamagePopup`** — `.meta guid grep` 결과 Game.unity / 모든 .prefab 에서 참조 0. **현재 씬에 배치되지 않음** — 이전 분석(Optimization_02 §3.1~3.3) 의 타깃 전원 inactive.

## 5. 결론 — 다음 액션 제안

### 5.1 핵심 요약

"735MB Managed Heap" 의 **단일 범인은 이번 조사에서 발견되지 않았다**. 개별 누수 크기는:

| 지점 | 세션당 누적 | 스파이크 기여 |
|---|---|---|
| PerfRecorder Samples | ~2 MB | 약 |
| PerfRecorder OnGUI GUIStyle | 14 MB churn (누적 X) | **강** |
| AimWeaponRing mesh rebuild | 108 MB churn (누적 X) | **강** |
| MG/Laser Instantiate | ~100KB churn (누적 X) | 중 |
| TopBarHud 문자열 | 200KB churn (누적 X) | 약 |
| SimpleBugSpawner (조건부) | 450KB retained + 벌레당 native | ? |

→ **735MB 는 Unity Editor baseline 힙 + Incremental GC 의 resize 여유 예약** 일 가능성이 높다. Editor 플레이 모드는 도메인 리로드 / 에셋 캐시 / 씬 preview 로 쉽게 400~800MB 사용.

**스파이크 75→115ms 점진 증가**의 물리적 원인:
- 위 3.2 / 3.3 으로 매 프레임 **~20KB 단명 alloc** 이 꾸준히 쌓임 → Incremental GC 가 scan 해야 할 살아있는 참조가 많아지면서 마크 단계 비용 ↑
- Editor 의 Mono 힙은 scan-time 이 힙 크기에 비례 — 100MB 스캔보다 700MB 스캔이 7배 느림

### 5.2 Phase A0 재구성안

원래 Optimization_02 A0-1~A0-4 (BugHpBar Texture2D / DamagePopup / SimpleVFX.PlayBugHit / VFX 풀링) 는 **전부 벌레 피격 트리거** → baseline 에선 돌지 않는 경로. 다음으로 교체 제안:

- **A0-1 (신규, 최우선, 검증 쉬움)**: baseline 측정 중 **PerfRecorder 녹화 자체가 노이즈인가** 확인. 해결은 간단 — Samples 를 제거하고 온라인 percentile 로 교체 (**3.1**). 또는 단순히 **baseline vs 같은 씬 아무것도 안 할 때** 의 GCUsed 차이를 다른 계측법(editor 프로파일러 스냅샷)으로 재측정.
- **A0-2 (신규)**: `PerfRecorder.OnGUI` GUIStyle static-cache (**3.2**) — 30초 수정 / 베이스라인 14MB churn 제거
- **A0-3 (신규)**: `AimWeaponRing` verts/uvs/tris 필드 캐시 (**3.3**) — 가장 큰 단일 베이스라인 alloc 제거
- **A0-4 (조건부)**: SimpleBugSpawner 가 정말 활성인지 기획자 PC 재확인 (**3.5**). 활성이면 "벌레 0 baseline" 은 사실상 "벌레 ≤90 상시 공급" 시나리오 — Optimization_02 의 BugHpBar/MinimapIcon 결론이 다시 유효해짐.

### 5.3 가장 먼저 검증할 단일 수정 (추천)

**`AimWeaponRing` verts/uvs/tris 필드 캐시** (§3.3). 근거:
1. 기여도 추정이 가장 크고 (프레임당 16KB)
2. 수정이 국소적 (한 파일, 한 메서드, 기존 동작 변화 없음)
3. 검증이 즉시 가능 — 수정 전/후 동일 세션 F9 녹화 후 `GC Alloc / frame` 감소 폭 측정

### 5.4 런타임 로깅이 필요한 지점 (코드 리뷰만으로 불확실)

- **"벌레 0 baseline" 의 실제 상태**: Play 중 Hierarchy 에서 `SimpleBugSpawner` 의 활성 상태, 그리고 `(SimpleBug)FindObjectsOfType` count 를 OnGUI 로 표시해 스크린샷 회수 — §3.5 확인용
- **기관총/레이저 Fire 빈도**: `Fire()` 에 카운터 추가해 46s 세션 중 총 Instantiate 횟수 측정 — §3.4 기여도 확정
- **Managed Heap delta 분해**: Editor Memory Profiler (com.unity.memoryprofiler) 스냅샷을 세션 시작/종료 시 2회 찍어 `Heap delta` 를 Type 별 분해 — "정말 어디가 retained 인지" 의 최종 증거. 코드 리뷰만으로 retained 를 특정하는 한계는 여기까지.

---

### 부록 — 조사 방법 재현

```bash
# 1) Game.unity 에서 모든 script guid 추출
grep -oP 'guid: [a-f0-9]+' Assets/_Game/Scenes/Game.unity | sort -u > /tmp/scene_guids.txt

# 2) 각 .cs.meta 의 guid 를 스크립트 이름과 매칭
find Assets/_Game/Scripts -name "*.cs.meta" | while read f; do
  g=$(grep -oP 'guid: [a-f0-9]+' "$f" | head -1 | sed 's/guid: //')
  name=$(basename "$f" .cs.meta)
  echo "$g $name"
done | sort > /tmp/script_guids.txt

# 3) join — Game.unity 에 존재하는 user script 만 추출
awk 'NR==FNR{a[$1]=$2; next} ($1 in a){print $1, a[$1]}' \
  /tmp/script_guids.txt /tmp/scene_guids.txt | sort -k2
```

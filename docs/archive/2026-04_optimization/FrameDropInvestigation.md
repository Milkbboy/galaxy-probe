# 프레임 드랍 파악

> 작성 2026-04-22 · 대상 에디터 환경 RTX 3060, Unity 6 / URP / 탑다운 오쏘

## 배경

기획자 컴퓨터(RTX 3060)에서 Unity 에디터로 게임 실행 시, **벌레가 많이 나오지 않은 상태에서도 프레임 드랍이 심함**. 최적화에 앞서 원인 후보를 전수 조사한 결과를 정리한다.

조사 범위는 `Assets/_Game/Scripts/` 와 `Assets/Settings/` (URP). 서드파티(`Polygon Arsenal`, `TextMesh Pro`, `Packages`)는 변경 대상이 아니므로 제외.

---

## 요약

증상("벌레가 적어도 렉")을 설명하는 비용을 우선순위로 나누면:

1. **HUD/UI 매 프레임 TMP 재할당** — 벌레 0마리여도 체력·채굴·어빌리티 쿨다운 텍스트가 매 프레임 포맷팅·setter. 지금 증상의 주범.
2. **URP 오버스펙** — 탑다운 오쏘에 비해 과한 섀도우 카스케이드(4) / 해상도(2048) / SSAO.
3. **드론/거미 수에 비례하는 스케일링 비용** — 각 인스턴스가 매 프레임 `Physics.OverlapSphere` 2~3회. 거미 자동 소환 주기 10s→3s 단축(`9c161f0`) 이후 누적이 빠름.
4. **탄환/VFX 스파이크** — 기관총 발사 중 Instantiate/Destroy 폭주 (초당 30+ GameObject).
5. **`BugController` 매 프레임 `is` 캐스팅** — 벌레 수에 선형.

아래 각 항목은 파일경로:라인까지 특정했다.

---

## 1. 상시 매 프레임 비용 (벌레 0마리여도 돔)

### 1.1 HUD TMP 텍스트 매 프레임 재할당 + setter **[확정 — 최우선]**

| 파일 | 라인 | 매 프레임 실행되는 코드 |
|---|---|---|
| `Assets/_Game/Scripts/UI/HUD/TopBarHud.cs` | 137, 165-169 | `$"체력 {Mathf.CeilToInt(_machine.CurrentHealth)}"` (Update→UpdateHealth) |
| `Assets/_Game/Scripts/UI/MachineStatusUI.cs` | 36-40, 60-62 | `$"{Mathf.CeilToInt(CurrentHp)} / {Mathf.CeilToInt(MaxHp)}"` (Update→UpdateHPBar) |
| `Assets/_Game/Scripts/UI/MiningUI.cs` | 52-60, 71 | `$"{_prefix}{totalMined}"` (Update→UpdateMiningText) |
| `Assets/_Game/Scripts/UI/HUD/AbilitySlotUI.cs` | 84-102 | 3슬롯 전부 매 프레임 `.text = ...`, `.fillAmount = ...` (`AbilityHud.Update` 에서 호출 — `AbilityHud.cs:43`) |

- `.text` / `.fillAmount` 대입은 값이 동일해도 Graphic 을 SetVerticesDirty / SetLayoutDirty 로 마킹 → UI mesh/layout rebuild.
- 문자열 포맷팅은 GC 압박도 누적 (초당 ~수 KB).

### 1.2 월드 UI LateUpdate 러시
- `Assets/_Game/Scripts/UI/Hp3DBar.cs:91-100`
- `Assets/_Game/Scripts/Bug/BugLabel.cs:36-45`
- `Assets/_Game/Scripts/UI/Minimap/MinimapIcon.cs:81-92`

  각 인스턴스마다 개별 `LateUpdate` 에서 `transform.position/rotation` setter. 드론·거미·벌레·미니맵 아이콘이 합산되면 LateUpdate 콜 수가 수십~수백 개.

### 1.3 Find* — 실제는 1회성 (재평가 결과 낮은 우선순위)

재조사 결과, 아래 Find* 는 모두 초기화 시점에만 호출됨. 매 프레임 비용 아님.

| 파일 | 호출 위치 | 실제 빈도 |
|---|---|---|
| `Weapon/{Bomb,Laser,MachineGun,Saw}Weapon.cs` | `Start()` | **1회** |
| `Camera/DynamicCamera.cs:39` | `Awake()` | **1회** |
| `UI/HUD/TopBarHud.cs:47, 66` | `Awake()` + `TryApplyCharacter()` (`_characterApplied` 플래그로 바운드) | 캐릭터 해석까지만 |
| `Bug/BugController.cs:598` | `FindTarget()` — `_target == null` 일 때만 | 타겟 소실 순간만 |
| `UI/MachineStatusUI.cs:22`, `UI/MiningUI.cs:36` | `Start()` | **1회** |

→ Phase A 에서 제거 대상 아님. 필요해지면 Phase C 로.

---

## 2. 드론/거미 스케일링 비용

### 2.1 드론·거미마다 매 프레임 `Physics.OverlapSphereNonAlloc` 2~3회
- `Assets/_Game/Scripts/Ability/Runners/DroneInstance.cs:131, 215`
- `Assets/_Game/Scripts/Ability/Runners/SpiderDroneInstance.cs:199, 260`

  (타겟 탐색 + 근접/멜리 판정). `NonAlloc` 이라 GC 는 없지만 **쿼리 수 자체가 곱셈**.

### 2.2 거미 자동 소환 주기 10s → 3s 단축으로 누적 가속 (`9c161f0`)
- `SpiderDroneRunner.Tick()` (`Assets/_Game/Scripts/Ability/Runners/SpiderDroneRunner.cs:51-77`) — 상한 도달 후에도 매 프레임 조건 검사는 계속 돔.

### 2.3 Bug 공격 타입 instanceof 매 프레임
- `Assets/_Game/Scripts/Bug/BugController.cs:227, 239-277`

  ```csharp
  if (_currentAttack is CleaveAttack cleaveAttack) ...
  if (_currentAttack is BeamAttack beamAttack) ...
  ```

  벌레마다, 공격 타입마다 is-cast 가 반복됨. 공격이 바뀔 때만 캐시하면 없어질 비용.

---

## 3. 탄환 / VFX 스파이크 (풀링 부재)

- 무기 계열 모두 `Instantiate` → `Destroy(vfx, lifetime)` 패턴. 오브젝트 풀 없음. (`BugPool` 은 있음)
- 루핑 파티클을 다수 포함한 VFX 프리팹:
  - `FX_Bullet_Projectile.prefab` (ParticleSystem 3개 looping)
  - `FX_Grenade_Projectile.prefab` (3개)
  - `FX_Laser_Muzzle.prefab` (4개)
  - `FX_Laser_Impact.prefab` (5개)
  - `FX_Grenade_Impact.prefab` (1개)
- **기관총 10 rps × 3세트(머즐+탄환+임팩트) = 초당 30개 GameObject 생성/파괴.** GC 스파이크 주범 후보.

관련 파일:
- `Assets/_Game/Scripts/Weapon/WeaponBase.cs:199-200`
- `Assets/_Game/Scripts/Weapon/Shotgun/ShotgunWeapon.cs:39-40`
- `Assets/_Game/Scripts/VFX/SimpleVFX.cs`
- `Assets/_Game/Scripts/Bug/BugController.cs:1069` (피격/사망 VFX)

---

## 4. URP / 렌더 설정

### 4.1 `Assets/Settings/PC_RPAsset.asset` — 섀도우 오버스펙
- MainLight Shadowmap **2048**
- Shadow Cascades **4**
- AdditionalLights Shadowmap **2048**, perPixel, 섀도우 지원
- Soft Shadow Quality **3**

  씬에 동적 Point/Spot Light 는 **0개**. AdditionalLights 쪽은 사실상 낭비고, **MainLight Cascade 4 + 2048 이 고정 비용의 주범**. 탑다운 오쏘에서 Cascade 4 는 과함.

### 4.2 `PC_Renderer` — SSAO 활성
- AO Method SSAO, Intensity 0.4, Samples 1

  탑다운 오쏘 + 언릿 위주에서 SSAO 의 시각적 이득은 작고, 풀스크린 포스트 1패스 비용은 상시.

### 4.3 Post-processing Volume
- `DefaultVolumeProfile.asset`: Bloom(0.25), LiftGammaGain, SplitToning
- `SampleSceneProfile.asset`: Bloom(0.25, high quality), Tonemapping, Vignette, MotionBlur(off)

  Bloom high-quality 필터링은 저사양에서 체감 비용 있음.

### 4.4 `Mobile_RPAsset.asset` (비교)
- 렌더 스케일 0.8, Cascade 1, Shadowmap 1024, Soft Shadow off

  → 에디터 테스트 시 Mobile 프로필로 스왑해 기준선 측정해볼 수 있음.

---

## 5. 에디터 전용 오버헤드 (Scene 뷰 전용)

`OnDrawGizmos(Selected)` 구현 파일:

| 파일 | 비용 |
|---|---|
| `Camera/DynamicCamera.cs` | 64 segment 원 (중) |
| `Aim/AimController.cs` | sphere 1개 (낮) |
| `TunnelEventManager.cs` | 영역 표시 (중) |
| `BugSpawner.cs`, `SimpleBugSpawner.cs`, `FormationSpawner.cs` | 스폰 영역 (중) |
| `MineInstance.cs` | sphere 2개 (낮) |

Play 모드 Game 뷰 비용은 없음. **Scene 뷰를 띄워둔 채 플레이할 때만** 영향. 기획자 환경에서 Scene+Game 둘 다 띄워두면 체감 가능.

---

## 6. 조사에서 발견된 양호한 부분 (수정 불필요)

- Update 루프 내 LINQ 사용 0건.
- `Debug.Log` 가 Update 핫패스에 없음.
- `Camera.main` 은 `Awake` 에서만 캐시, Update 재접근 없음.
- `Physics.OverlapSphere` 는 대부분 `NonAlloc` 버퍼 버전 사용.
- 월드 UI(`Hp3DBar`, `MiningDroneTimer3D`)는 Unlit + shadow off 로 이미 경량.
- 씬 내 동적 라이트 0개 (Directional 외).

---

## 7. 수정 계획

### Phase A — 퀵윈 (반나절, 위험도 낮음, 벌레 0 상태 체감 개선 목표)

#### A-1. HUD TMP dirty 가드 (UI 텍스트 재할당 제거)

**대상**
- `Assets/_Game/Scripts/UI/HUD/TopBarHud.cs` — `UpdateHealth`, `UpdateMining`, `UpdateKills`, `UpdateOre`, `UpdateGems` 5개.
- `Assets/_Game/Scripts/UI/MachineStatusUI.cs` — `UpdateHPBar`.
- `Assets/_Game/Scripts/UI/MiningUI.cs` — `UpdateMiningText` (+ Update 에서 매 프레임 호출하는 부분 제거 검토).
- `Assets/_Game/Scripts/UI/HUD/AbilitySlotUI.cs` — `Refresh`.

**패턴 (정수 값)**
```csharp
// 필드 추가
private int _lastHp = int.MinValue;

private void UpdateHealth()
{
    if (_healthText == null || _machine == null) return;
    int ceilHp = Mathf.CeilToInt(_machine.CurrentHealth);
    if (ceilHp == _lastHp) return;          // ← 가드
    _lastHp = ceilHp;
    _healthText.text = $"체력 {ceilHp}";
}
```

**패턴 (AbilitySlotUI — fillAmount + color + status)**
```csharp
private int  _lastCdSec  = int.MinValue;
private bool _lastReady;
private float _lastFill = -1f;

public void Refresh()
{
    if (_runner == null || _data == null) return;

    float norm  = _runner.CooldownNormalized;
    float pct   = 1f - norm;
    bool  ready = norm <= 0f;

    if (_cooldownBarFill != null)
    {
        if (!Mathf.Approximately(_lastFill, pct))
        {
            _lastFill = pct;
            _cooldownBarFill.fillAmount = pct;
        }
        Color target = ready ? ReadyGreen : _themeColor;
        if (_cooldownBarFill.color != target) _cooldownBarFill.color = target;
    }

    if (_statusLabel != null)
    {
        if (ready)
        {
            if (!_lastReady) { _lastReady = true; _statusLabel.text = "사용가능"; }
        }
        else
        {
            int sec = Mathf.CeilToInt(norm * _data.CooldownSec);
            if (_lastReady || sec != _lastCdSec)
            {
                _lastReady = false;
                _lastCdSec = sec;
                _statusLabel.text = $"{sec}s";
            }
        }
    }
}
```

**MiningUI 특이 사항**
- `MiningUI.Update()` 에서 매 프레임 `UpdateMiningText(_machine.TotalMined)` 호출. `OnMiningGained` 이벤트가 이미 있으므로 Update 는 `UpdatePunchAnimation()` 만 남기고 텍스트 갱신은 이벤트로 이동.
  ```csharp
  private void Update() { UpdatePunchAnimation(); }
  private void OnMiningGained(int amount)
  {
      _punchTimer = _punchDuration;
      if (_machine != null) UpdateMiningText(_machine.TotalMined);
  }
  ```

**기대 효과**: 에디터에서 벌레 0 상태 초당 3~5 KB GC 할당 감소, UI mesh rebuild 콜 제거.

---

#### A-2. URP 에셋 튜닝

**대상 파일**: `Assets/Settings/PC_RPAsset.asset`, `Assets/Settings/PC_Renderer.asset`, `Assets/Settings/DefaultVolumeProfile.asset`, `Assets/Settings/SampleSceneProfile.asset`

| 설정 | 현재 | 권장 | 비고 |
|---|---|---|---|
| MainLight Shadowmap Resolution | 2048 | **1024** | 탑다운 오쏘에 2048 는 과함 |
| Shadow Cascades | 4 | **2** | 오쏘 카메라에는 2 면 충분 |
| AdditionalLights Rendering | Per-Pixel + shadows | **Vertex** 또는 shadows off | 씬에 동적 Point/Spot 없음 |
| Soft Shadow Quality | High (3) | **Low (1)** | 시각 차 거의 없음 |
| SSAO (PC_Renderer) | On | **Off** | Unlit/저디테일 환경에서 이득 작음 |
| Bloom High Quality Filtering | On | **Off** | 저사양에서 체감 부담 |

**변경 방법**: Unity 에디터에서 해당 `.asset` 선택 → Inspector 수정 후 커밋. 숫자 필드는 `.asset` yaml 로도 수정 가능.

**기대 효과**: Rendering Main thread 1.5~3 ms 감소 (해상도 1920×1080 기준).

---

#### A-3. `BugController` 매 프레임 `is` 캐스팅 제거

**대상**: `Assets/_Game/Scripts/Bug/BugController.cs:236-290`.

**패턴**:
```csharp
// 필드
private CleaveAttack _cleaveCache;
private BeamAttack   _beamCache;
private readonly List<NovaSkill>      _novaCache      = new();
private readonly List<BuffAllySkill>  _buffAllyCache  = new();
private readonly List<HealAllySkill>  _healAllyCache  = new();

// 공격 세팅 시점 (초기화 또는 전환)
private void SetCurrentAttack(BugAttack attack)
{
    _currentAttack = attack;
    _cleaveCache   = attack as CleaveAttack;
    _beamCache     = attack as BeamAttack;
}

// 스킬 세팅 시점 (초기화)
private void CacheSkillSubtypes()
{
    _novaCache.Clear(); _buffAllyCache.Clear(); _healAllyCache.Clear();
    foreach (var s in _skills)
    {
        if (s is NovaSkill n)      _novaCache.Add(n);
        if (s is BuffAllySkill b)  _buffAllyCache.Add(b);
        if (s is HealAllySkill h)  _healAllyCache.Add(h);
    }
}

// Update — is 제거
if (_cleaveCache != null) _cleaveCache.UpdateRangeIndicator(_target);
if (_beamCache   != null) _beamCache.UpdateBeam(deltaTime);

foreach (var s in _skills) s.UpdateCooldown(deltaTime);
foreach (var n in _novaCache)     n.UpdateRangeIndicator();
foreach (var b in _buffAllyCache) b.UpdateAura();
foreach (var h in _healAllyCache) h.UpdateHealAura(deltaTime);
```

**기대 효과**: 벌레 N 마리 × (매 프레임 4회 is-cast 비용) 제거. 50마리 웨이브에서 0.3~0.5 ms.

---

### Phase B — 드론/거미·VFX 규모 대응 (하루, 중간 위험도)

#### B-1. 드론/거미 타겟 탐색 프레임 분산

**대상**
- `Assets/_Game/Scripts/Ability/Runners/DroneInstance.cs:125-166` (`Update`, `FindClosestBug`).
- `Assets/_Game/Scripts/Ability/Runners/SpiderDroneInstance.cs:150-221` (`Update`, `AcquireOrRefreshTarget`, `FindBestBugExcludingClaimed`).

**옵션 1 — 시간 슬라이스 (최소 변경)**
```csharp
[SerializeField] private float _scanInterval = 0.15f;
private float _nextScanTime;
private Collider _cachedTarget;

private void Update()
{
    if (_currentHp <= 0f) return;
    float dt = Time.deltaTime;

    if (Time.time >= _nextScanTime || _cachedTarget == null)
    {
        _nextScanTime = Time.time + _scanInterval;
        _cachedTarget = FindClosestBug(_abilityData?.Range ?? 0f);
    }

    if (_cachedTarget != null) AimBodyAt(_cachedTarget.transform.position);
    ...
}
```
- 프레임당 비용 1/10 로 감소 (60 fps → 0.15s 당 1회).
- 락 타겟 유효성은 매 프레임 가볍게(거리 제곱 비교) 검증.

**옵션 2 — `BugManager` 살아있는 벌레 리스트 기반 (근본 해결)**
- `BugManager` 에 `IReadOnlyList<Transform> ActiveBugs` 노출.
- 드론/거미는 매 프레임 O(N) 거리 비교로 최근접 탐색 → OverlapSphere 자체 제거.
- `_claimedTargets` 는 거미 쪽에만 유지.

**권장**: 먼저 옵션 1 로 체감 개선 후, 벌레 다수 웨이브에서 병목 남으면 옵션 2.

**기대 효과**: 드론 5 + 거미 3 환경에서 OverlapSphere 호출 수 24/frame → 1.6/frame.

---

#### B-2. VFX/탄환 풀링

**대상 (우선순위)**
1. 기관총: `FX_MachineGun_Muzzle`, `MachineGunBullet`, `FX_MachineGun_Impact`.
2. 폭탄: `FX_Grenade_Projectile`, `FX_Grenade_Impact`.
3. 레이저: `FX_Laser_Muzzle`, `FX_Laser_Impact`.

**아키텍처 — `BugPool` 패턴 확장**
```csharp
// Assets/_Game/Scripts/VFX/Pool/VfxPool.cs
public class VfxPool : MonoBehaviour
{
    [SerializeField] private GameObject _prefab;
    [SerializeField] private int _initialCapacity = 16;

    private readonly Stack<GameObject> _stack = new();

    public GameObject Get(Vector3 pos, Quaternion rot)
    {
        GameObject go = _stack.Count > 0 ? _stack.Pop() : Instantiate(_prefab);
        go.transform.SetPositionAndRotation(pos, rot);
        go.SetActive(true);
        return go;
    }

    public void Return(GameObject go)
    {
        go.SetActive(false);   // ParticleSystem 재생은 OnEnable 에서 Play()
        _stack.Push(go);
    }
}

// VFX 프리팹에 붙이는 자동 반납 컴포넌트
public class PooledVfxReturn : MonoBehaviour
{
    private VfxPool _pool;
    private float _returnAt;
    public void Initialize(VfxPool pool, float lifetime)
    { _pool = pool; _returnAt = Time.time + lifetime; }

    private void Update()
    {
        if (Time.time >= _returnAt) _pool.Return(gameObject);
    }
}
```

**통합 지점**
- `WeaponBase.PlayHitVfx` (`Weapon/WeaponBase.cs:199-200`), `ShotgunWeapon.PlayMuzzleVfx` (`Weapon/Shotgun/ShotgunWeapon.cs:39-40`), 기관총 발사 경로를 풀 사용으로 교체.
- VFX 프리팹의 ParticleSystem 은 `Play On Awake = true` → OnEnable 시 자동 재생 유지.

**주의**
- 풀 반납 시 파티클 잔상 방지: `ParticleSystem.Clear()` 호출 또는 `stopAction = None`.
- 트레일 포함 VFX 는 `TrailRenderer.Clear()` 같이 호출 (없으면 이전 궤적이 이어짐).

**기대 효과**: 기관총 10 rps × 3세트 Instantiate/Destroy 제거 → GC 스파이크 제거.

---

### Phase C — 여력 있을 때

#### C-1. 월드 UI 단일 tick Updater
- 현재: 드론/거미/벌레 별 `Hp3DBar`, `BugLabel`, `MinimapIcon` 이 개별 `LateUpdate`.
- 변경: `WorldUiTicker` 싱글톤이 리스트 들고 한 번에 loop.
- `IWorldUiFollower { Transform Target; Vector3 Offset; void Tick(); }` 등록/해제 패턴.
- 효과: MonoBehaviour 콜백 오버헤드 감소 (N → 1).

#### C-2. `DynamicCamera.OnDrawGizmosSelected` 가드
```csharp
#if UNITY_EDITOR
private void OnDrawGizmosSelected() { ... }
#endif
```
- Scene 뷰에서 선택 중일 때만 비용. Play 모드 Game 뷰 영향 없음. 체감보단 위생.

#### C-3. 미니맵 갱신 주기 조정
- `MinimapCamera` 가 매 프레임 렌더 → 격프레임 또는 0.1s 주기로.
- Unity `Camera.enabled = false` 로 두고 필요 시 `camera.Render()` 수동 호출.

---

## 8. 검증 방법

### 8.1 `PerfRecorder` 로거 (권장)

`Assets/_Game/Scripts/Diagnostics/PerfRecorder.cs` — `ProfilerRecorder` 기반 세션 로거. 수정 전/후 같은 시나리오에서 CSV 를 뽑아 수치로 비교.

**셋업**
1. Game 씬의 아무 GameObject 에 `PerfRecorder` 컴포넌트 추가 (씬당 1개면 충분).
2. Inspector 에서 `_sessionLabel` 을 시나리오에 맞게 설정 (예: `baseline`, `phase_a1`).

**녹화**
- 플레이 모드에서 **F9** 로 start/stop 토글. 화면 좌상단에 상태/경과시간 오버레이 표시.
- 정지 시 프로젝트 루트 `PerfLogs/` 폴더에 2개 CSV 저장:
  - `{label}_{timestamp}.csv` — 채널별 avg / max / p50 / p95 / p99
  - `{label}_{timestamp}_spikes.csv` — `_spikeThresholdMs` 초과 프레임 상세

**녹화 채널 (가용한 것만 자동 선별)**
- CPU: `Main Thread`, `CPU Main Thread Frame Time`, `Render Thread`
- Render counters: `Batches`, `SetPass`, `DrawCalls`, `Vertices`, `Triangles`, `ShadowCasters`
- Memory/GC: `GCUsed`, `GCReserved`, `GCAllocInFrame`, `SystemUsed`, `TotalReserved`

### 8.2 기준 시나리오

| 용도 | 시나리오 | 재현 시간 |
|---|---|---|
| Phase A 검증 (UI/TMP dirty 가드 · URP) | 웨이브 시작 직후 **벌레 0마리** 상태 유지 | 30초 |
| Phase B-1 검증 (드론 스캔 분산) | 드론 5 + 거미 3 + 벌레 10 | 60초 |
| Phase B-2 검증 (VFX 풀) | 기관총 장착, 벌레 20 상대 연사 | 60초 |
| URP 튜닝 시각 검증 | Scene 뷰 동일 각도 스크린샷 전후 비교 | — |

### 8.3 최소 수용 기준

- Phase A 후 벌레 0 시나리오 `MainThread.avg` **2~3 ms 감소**, `GCAllocInFrame.avg` **큰 폭 감소** (수 KB → ~0).
- Phase B-2 후 기관총 사격 중 `spikes.csv` 에 기록되는 스파이크 프레임 수 **80% 이상 감소**.
- 모든 시나리오에서 시각적 회귀 없음 (섀도우 단절, VFX 잔상, UI 지연 등).

---

## 9. 작업 순서 제안

```
Phase A (PR 1개로 묶어도 무방)
  ├─ A-1. HUD TMP dirty 가드        [ 4 파일 ]
  ├─ A-2. URP 에셋 튜닝              [ 4 에셋 ]
  └─ A-3. BugController is 캐시     [ 1 파일 ]
  → 에디터에서 벌레 0 시나리오 측정

Phase B (각각 PR 분리 권장)
  ├─ B-1. 드론/거미 스캔 분산        [ 2 파일 ]
  └─ B-2. VFX 풀                    [ 신규 2~3 파일 + 호출부 교체 ]
  → 스트레스 시나리오 측정

Phase C (선택)
  ├─ C-1. WorldUiTicker
  ├─ C-2. Gizmos 가드
  └─ C-3. 미니맵 주기
```

# Drill-Corp 최적화 종합 문서

> 통합일 2026-04-23 · 원본 [Optimization_01.md](Optimization_01.md) + [Optimization_02_PlannerPCAnalysis.md](Optimization_02_PlannerPCAnalysis.md) · 상세 후보 목록 [FrameDropInvestigation.md](FrameDropInvestigation.md)

## 이 문서를 읽기 전에

이 프로젝트는 **"기획자 PC(RTX 3060)에서 에디터로 실행하면 렉이 걸린다"** 라는 한 줄짜리 보고에서 시작됐습니다. 개발자 PC에서는 멀쩡했기에 추측만으로는 해결이 불가능했고, **계측 도구를 만들어 실제 데이터를 뽑고 → 분석하고 → 우선순위를 바로잡는** 과정을 거쳤습니다.

이 문서는 그 전체 과정을 시간 순서대로 정리한 것입니다. 기술 용어가 나올 때마다 "왜 이게 프레임 드랍을 일으키는지" 를 짧게 설명해 뒀습니다.

---

## 한눈에 보는 결론

**진짜 범인은 "쓰레기 누수"였습니다.**

게임이 실행되는 동안 메모리에 안 쓰는 객체가 계속 쌓여서(`Managed Heap 735MB`), Unity가 주기적으로 "대청소(GC)"를 할 때마다 화면이 잠깐 멎는 현상이었습니다. 시간이 지날수록 쌓이는 쓰레기가 많아져 **대청소 시간도 점점 길어지는** (초반 75ms → 후반 115ms) 전형적인 누수 곡선이었습니다.

> 💡 **GC(Garbage Collection) 쉬운 설명**
> C#은 개발자가 메모리를 직접 해제하지 않아도 됩니다. 안 쓰는 객체는 런타임이 알아서 치워주는데, 이걸 **가비지 컬렉션**이라고 합니다. 문제는 이 청소 작업이 일어나는 동안 **게임 로직이 잠깐 멈춘다**는 것. 청소할 쓰레기가 많을수록 멈추는 시간도 길어집니다.

따라서 해결 순서는:

1. **Phase A0 (누수 차단)** — 쓰레기가 계속 쌓이는 원인을 막는다. ← **최우선**
2. **Phase A (HUD/URP/캐싱)** — 매 프레임 발생하는 잔 비용을 제거한다.
3. **Phase B (드론/VFX 풀링)** — 전투 상황에서의 스파이크를 줄인다.
4. **Phase C (세부 정리)** — 여력 있을 때.

---

## 1. 왜 렉이 걸리는가 — 용어 사전

본격 분석 전에, 프레임 드랍을 일으키는 대표적인 패턴 4가지를 알면 나머지가 쉽게 이해됩니다.

### 1.1 GC 스파이크

**현상**: 평소엔 60fps인데, 몇 초에 한 번씩 100ms 짜리 긴 프레임이 뚝 떨어짐.

**원인**: 위에서 설명한 가비지 컬렉션. C#에서 `new` 키워드로 뭔가를 만들 때마다 (심지어 `$"Hello {name}"` 같은 문자열 보간도!) 쓰레기 후보가 쌓임. Unity의 GC는 **Incremental 모드**로 조금씩 치우지만, 힙이 커지면 한 번에 긴 스파이크가 불가피합니다.

**진단 신호**: CSV에서 `MainThread max` 가 특정 프레임에만 유독 큼 + 그 프레임의 `GCAllocInFrame` 은 오히려 작음 (이 프레임이 쓰레기를 만드는 게 아니라 **이전 프레임들의 쓰레기를 치우는** 중).

### 1.2 매 프레임 문자열/UI 재할당

**현상**: 화면에 아무것도 안 일어나도 렉 감지됨.

**원인**: `text.text = $"체력 {hp}"` 같은 코드가 **Update() 에 있으면** 매 프레임마다:
1. `$"..."` 가 **새 문자열 객체 할당** (= 쓰레기 후보 1개)
2. TMP(TextMeshPro)의 `.text` setter는 내부적으로 **메시를 다시 만들라고 플래그** (= UI 리빌드 비용)

**영향**: 값이 **안 바뀌어도** setter 호출만으로 비용 발생. 60fps면 초당 60번의 헛수고.

**해결 패턴**: "값이 달라졌을 때만 set" 하는 **dirty 가드**:
```csharp
if (ceilHp == _lastHp) return;   // 변경 없으면 return
_lastHp = ceilHp;
_healthText.text = $"체력 {ceilHp}";
```

### 1.3 `Physics.OverlapSphere` 매 프레임 호출

**현상**: 드론/거미 수에 비례해서 렉이 심해짐.

**원인**: `Physics.OverlapSphere(원점, 반경, ...)` 는 "이 구 안에 어떤 콜라이더가 있는지" 물리 엔진에 물어보는 것. **매 프레임 × 인스턴스 수** 만큼 물어보면 물리 엔진이 바빠짐.

**해결 패턴**: 드론이 타겟을 "매 프레임" 새로 탐색할 필요는 없음. **0.15초 간격으로 탐색 + 탐색 사이엔 저번 타겟 그대로** 쓰면 호출 수가 1/10.

### 1.4 `Instantiate` / `Destroy` 스파이크

**현상**: 무기 발사·피격마다 순간 프레임 드랍.

**원인**: `Instantiate(prefab)` 은 프리팹을 복제해서 씬에 찍어내는 것. 하부에서 **메모리 할당 + 컴포넌트 초기화 + 물리 등록**이 일어나는 무거운 작업. `Destroy` 는 그 반대라 또 무거움.

**해결 패턴**: **오브젝트 풀링**. 미리 20개쯤 만들어두고 "비활성화 ↔ 활성화" 로 재사용. 이미 `BugPool` 은 이 방식으로 벌레를 재사용 중인데, VFX/탄환은 풀이 없음.

---

## 2. 어떻게 조사했나 — 계측 도구

처음엔 코드 리뷰로 "이게 의심된다" 수준이었지만, 그것만으로는 **어느 놈이 진짜 주범인지** 알 수 없습니다. 그래서 실측 도구 2개를 만들었습니다.

### 2.1 `PerfRecorder` — 세션 로거

**파일**: `Assets/_Game/Scripts/Diagnostics/PerfRecorder.cs`

Unity 6 의 `ProfilerRecorder` API를 감싼 간단한 로거입니다. 씬에 뭘 추가할 필요 없이 **자동으로 부트스트랩** 되고, 플레이 중에 키보드로 녹화를 조작합니다.

**사용법**
| 키 | 동작 |
|---|---|
| `F9` | 녹화 시작/정지 토글 |
| `F10` | 라벨 순환 (`baseline` → `wave_fighting` → `drones_active` → `heavy_combat`) |

**결과물**
- `PerfLogs/{label}_{timestamp}.csv` — 채널별 평균·최대·p50·p95·p99
- `PerfLogs/{label}_{timestamp}_spikes.csv` — 33.3ms 초과 프레임 상세
- 정지 순간 CSV가 **클립보드에 복사** + Explorer가 폴더 열어줌 → 공유 편함

> 💡 **p95, p99 가 뭔가요?**
> 프레임 시간을 정렬했을 때 상위 5%, 상위 1% 지점의 값입니다. 평균은 좋아 보여도 p99가 나쁘면 **"보통은 쾌적한데 가끔 끊김"** 이란 뜻. 체감 렉은 p99가 좌우합니다.

### 2.2 `PerfMarkers` — 의심 구간 직접 계측

**파일**: `Assets/_Game/Scripts/Diagnostics/PerfMarkers.cs`

"이 함수가 얼마나 오래 걸리지?" 를 재려면 그 함수를 `ProfilerMarker` 로 감싸야 합니다. 조사에서 의심했던 11개 구간(BugController.Update, 드론/거미 Update, HUD Update 등)에 마커를 심어서 **PerfRecorder 가 자동으로 캡처**하게 만들었습니다.

| 마커 이름 예시 | 계측 대상 |
|---|---|
| `DrillCorp.BugController.Update` | 벌레 Update 전체 (모든 벌레 합산) |
| `DrillCorp.Drone.OverlapSphere` | 드론 타겟 탐색 물리 쿼리만 |
| `DrillCorp.TopBarHud.Update` | 상단 HUD 업데이트 |

> 💡 **마커의 효용**
> `MainThread = 8ms` 인데 `BugController.Update = 5ms` 가 나오면 "벌레 로직이 절반 이상 먹고 있다" 는 걸 숫자로 확인할 수 있음. 추측이 아닌 증거가 됨.

---

## 3. 실측 결과 — 무엇이 나왔나

### 3.1 개발자 PC (정상 환경)

47초 녹화 / 벌레·드론·무기 비활성:

| 지표 | 값 |
|---|---|
| 평균 FPS | **201.5** |
| MainThread avg | 4.95 ms |
| MainThread p99 | 8.92 ms |
| MainThread max | 399ms (초기 hitch 1회만) |

→ 정상 구간에서 33ms 초과 프레임 **0건**. 개발자 PC는 기준선으로만 쓰고 넘어감.

### 3.2 기획자 PC (문제 환경)

46.8초 녹화 / 벌레 0 / `baseline` 라벨:

| 지표 | 값 | 해석 |
|---|---|---|
| 평균 FPS | 144.5 | 평소엔 쾌적 |
| MainThread p50 | 5.46 ms | 절반의 프레임은 빠름 |
| MainThread p95 | 8.09 ms | 95%까지도 괜찮음 |
| MainThread **p99** | **75.30 ms** | **하위 1%가 13 fps — 여기가 체감 렉** |
| MainThread **max** | **144.39 ms** | 최악 7 fps |
| 스파이크 빈도 | 87회 / 46.8s ≈ **1.9 Hz** | 약 78프레임마다 1번 |
| 평균 스파이크 크기 | 초반 75ms → 후반 **115ms** | **시간 지날수록 악화** |
| **GCUsed** | **735 MB** | **핵심 신호** |
| GC Alloc / frame | 25 KB | 평소 부하 |

**이 표가 말하는 것**:
1. 평상시(p50, p95)는 전혀 문제 없음.
2. 100프레임에 1번 정도 75~140ms 짜리 **긴 프레임**이 끼어드는데, 이게 체감 렉.
3. 스파이크가 **시간이 지날수록 커진다** → 뭔가가 계속 쌓이고 있다.
4. `GCUsed 735MB` — 이게 누수의 결정적 증거.

### 3.3 단순 누적으로는 설명 불가

```
46.8초 × 25 KB/frame × 144 fps ≈ 1.1 MB 
```

정상이면 힙은 1~2 MB 증가해야 하는데, 실제는 **735 MB**. → **어딘가가 정상 속도의 700배로 쓰레기를 만드는 중** 이거나, **만든 쓰레기가 회수 안 되고 쌓이는 중**.

또 하나의 단서: `MainThread max 144ms` 프레임의 `GCAllocInFrame = 34 KB`.

평소(25 KB)와 거의 같음. 즉 **이 프레임이 쓰레기를 많이 만들어서 느린 게 아니라, 다른 프레임들의 쓰레기를 회수하느라 느린 것**. 이건 GC 스파이크의 전형적 패턴입니다.

---

## 4. 어디서 누수가 나는가 — 코드 리뷰

`baseline` 라벨은 벌레 0 상태로 녹화했기 때문에, 벌레 Update/드론 Update 같은 마커들은 모두 `0.00 ms`. 그런데 힙은 735MB. 이 모순을 설명하려면 **벌레와 무관하게 쌓이는 뭔가**를 찾아야 합니다.

코드 리뷰로 4개 지점 발견:

### 4.1 `BugHpBar.CreateSquareSprite` — Texture2D 매 생성 [고위험]

**파일**: `Assets/_Game/Scripts/Bug/BugHpBar.cs:136-142`

```csharp
private static Sprite CreateSquareSprite()
{
    Texture2D texture = new Texture2D(1, 1);   // ← 벌레 1마리당 2번 new
    texture.SetPixel(0, 0, Color.white);
    texture.Apply();
    return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
}
```

벌레 1마리당 **Texture2D 2개 + Sprite 2개** 생성. 똑같은 "흰 점 1x1 텍스처" 를 매번 새로 만들고 있음.

> 💡 **왜 이게 누수인가**
> `BugController.Die` 에서 `Destroy(_hpBar.gameObject)` 로 HP 바 오브젝트는 파괴되지만, Texture2D/Sprite 는 **Unity 네이티브 자원**이라 참조가 끊겨도 즉시 해제 안 됨. `Resources.UnloadUnusedAssets()` 가 호출되는 타이밍(씬 전환 등)에야 회수. 그 사이 계속 쌓임.
>
> **풀링의 의미 상실**: `BugPool` 은 벌레 GameObject는 재사용하는데, 풀에서 꺼낼 때마다 `CreateHpBar` 가 또 호출되어 새 Texture/Sprite 생성.

**해결**: `static Sprite _sharedSprite` 로 1번만 만들고 모든 벌레가 공유.

```csharp
private static Sprite _sharedSprite;
private static Sprite GetSharedSprite()
{
    if (_sharedSprite == null)   // 도메인 리로드/씬 전환 대비 null 체크
        _sharedSprite = CreateSquareSprite();
    return _sharedSprite;
}
```

### 4.2 `DamagePopup.Create` — TMP 매 생성 [고위험]

**파일**: `Assets/_Game/Scripts/UI/DamagePopup.cs`

```csharp
GameObject popupObj = new GameObject("DamagePopup");       // 매 피격마다
_text = gameObject.AddComponent<TextMeshPro>();            // ← TMP 자체가 무거움
TMPFontHelper.ApplyDefaultFont(_text);
// ...
Destroy(gameObject);    // 1초 후
```

> 💡 **TextMeshPro 가 왜 무거운가**
> TMP 컴포넌트 1개 = 메시 + 머티리얼 인스턴스 + 폰트 아틀라스 바인딩. 생성 자체가 수 KB~수십 KB. 피격 이벤트마다 `new GameObject + AddComponent<TMP>` 를 반복하면 치명적.

**해결**: `DamagePopupPool` 싱글톤으로 고정 개수(예: 64) 미리 만들고 순환 재사용.

### 4.3 `SimpleVFX.PlayBugHit` — Gradient/Burst[] 매 생성 [중위험]

**파일**: `Assets/_Game/Scripts/VFX/SimpleVFX.cs`

```csharp
GameObject effectObj = new GameObject("BugHitVFX");
var ps = effectObj.AddComponent<ParticleSystem>();
emission.SetBursts(new ParticleSystem.Burst[] { ... });   // 배열 new
var gradient = new Gradient();                             // Gradient new
gradient.SetKeys(new GradientColorKey[] {...}, new GradientAlphaKey[] {...});  // 또 배열 new
Object.Destroy(effectObj, 1f);
```

호출 경로: 벌레 프리팹에 `_hitVfxPrefab` 이 할당 안 돼있으면 이 폴백 경로가 탐. **미할당 벌레 1종이라도 있으면** 대량 피격 시 터짐.

**해결**: 1안 — 프리팹 할당 강제해서 이 경로 자체 제거. 2안 — VfxPool 패턴 적용 + Gradient/Burst[] 를 `static readonly` 로 캐싱.

### 4.4 `MinimapIcon` — 풀 복귀 시 재생성 [중위험]

**파일**: `Assets/_Game/Scripts/Bug/BugController.cs:158-174`

```csharp
private void OnDestroy()
{
    if (_minimapIcon != null)
        Destroy(_minimapIcon.gameObject);   // ← 벌레 파괴 시 아이콘도 파괴
}
```

같은 벌레 인스턴스 내에서는 `_minimapIcon == null` 체크로 재사용되지만, **풀이 축소되거나 씬 전환 시** 함께 파괴. 다음 스폰에서 `MeshFilter + MeshRenderer` 재생성.

**해결**: `ResetForPool` 에서 `_minimapIcon` 을 유지 (현재는 null 처리).

### 4.5 HUD `$"..."` 매 프레임 [기존 분석, 재확인]

`TopBarHud.Update` · `MachineStatusUI.Update` · `MiningUI.Update` · `AbilityHud.Update` 가 각자 매 프레임 문자열 보간. CSV의 `GCAllocInFrame avg = 25 KB/frame` 의 주 기여자로 추정.

---

## 5. 조사에서 발견한 다른 후보들 (매 프레임 비용)

`baseline` 측정에서는 드러나지 않지만, **전투가 시작되면** 문제가 되는 비용들. [FrameDropInvestigation.md](FrameDropInvestigation.md) 상세.

### 5.1 HUD TMP 매 프레임 재할당 (§1.1 재언급)

| 파일 | 라인 | 비용 |
|---|---|---|
| `UI/HUD/TopBarHud.cs` | 137, 165-169 | `$"체력 {...}"` 매 프레임 |
| `UI/MachineStatusUI.cs` | 36-40, 60-62 | `$"{...} / {...}"` 매 프레임 |
| `UI/MiningUI.cs` | 52-60, 71 | `$"{_prefix}{totalMined}"` 매 프레임 |
| `UI/HUD/AbilitySlotUI.cs` | 84-102 | 3슬롯 × `.text` + `.fillAmount` 매 프레임 |

### 5.2 월드 UI LateUpdate 러시

| 파일 | 라인 |
|---|---|
| `UI/Hp3DBar.cs` | 91-100 |
| `Bug/BugLabel.cs` | 36-45 |
| `UI/Minimap/MinimapIcon.cs` | 81-92 |

각 인스턴스가 개별 `LateUpdate` 에서 position/rotation 동기화. 드론·거미·벌레·미니맵 아이콘 합산하면 수십~수백 개 LateUpdate.

### 5.3 드론/거미 `Physics.OverlapSphere` (스케일링)

| 파일 | 라인 |
|---|---|
| `Ability/Runners/DroneInstance.cs` | 131, 215 |
| `Ability/Runners/SpiderDroneInstance.cs` | 199, 260 |

거미 자동 소환 주기가 `10s → 3s` 로 단축된 후 누적이 가속.

### 5.4 BugController 매 프레임 `is` 캐스팅

**파일**: `Bug/BugController.cs:227, 239-277`

```csharp
if (_currentAttack is CleaveAttack cleaveAttack) ...
if (_currentAttack is BeamAttack beamAttack) ...
```

벌레 N마리 × 공격 타입 체크 수 만큼 is-cast. **공격이 바뀔 때만 캐시하면 0비용**.

### 5.5 VFX/탄환 풀링 부재

- 무기 계열 전부 `Instantiate` → `Destroy(vfx, lifetime)`. 풀 없음.
- 루핑 파티클 다수: `FX_Laser_Impact`(5개), `FX_Laser_Muzzle`(4개), `FX_Bullet_Projectile`(3개) 등.
- **기관총 10rps × 3세트(머즐/탄환/임팩트) = 초당 30개 GameObject 생성/파괴**.

### 5.6 URP 과설정

**`Assets/Settings/PC_RPAsset.asset`** — 탑다운 오쏘에 비해 과함:
- MainLight Shadowmap **2048** (권장 1024)
- Shadow Cascades **4** (권장 2)
- AdditionalLights Shadows on (씬에 동적 Point/Spot **0개** — 낭비)
- Soft Shadow Quality **3 (High)** (권장 1 Low)

**`PC_Renderer`** — SSAO 활성 (Unlit 위주 씬에서 이득 작음 → off)

**Volume profile** — Bloom High Quality filtering on (저사양 부담 → off)

### 5.7 에디터 전용 오버헤드 (Scene 뷰 전용)

| 파일 | 비용 |
|---|---|
| `Camera/DynamicCamera.cs` | 64 segment 원 gizmo |
| `BugSpawner.cs` 등 | 스폰 영역 gizmo |

Play 모드 Game 뷰에는 영향 없음. **Scene + Game 둘 다 띄워둔 환경**에서만 체감. `#if UNITY_EDITOR` 가드로 간단 해결.

---

## 6. 수정 계획 — Phase 별

### Phase A0 — 누수 차단 [최우선 · 이게 없으면 나머지 효과 안 보임]

| 항목 | 대상 | 기대 효과 |
|---|---|---|
| **A0-1** `BugHpBar` 공유 Sprite + 풀 복귀 재활용 | `Bug/BugHpBar.cs`, `Bug/BugController.cs` | Texture2D N*2 → 1 |
| **A0-2** `DamagePopup` 풀 | `UI/DamagePopup.cs` + 신규 `DamagePopupPool.cs` | TMP 생성 피격마다 → 초기 1회 |
| **A0-3** `SimpleVFX` 핫패스 제거 or 풀 | `VFX/SimpleVFX.cs`, `Bug/BugController.cs` | Gradient/Burst[] 정적 캐시 |
| **A0-4** `MinimapIcon` 풀 복귀 재활용 | `Bug/BugController.cs:ResetForPool` | 풀에서 꺼낼 때 재생성 제거 |

> 💡 **왜 A0가 먼저인가**
> HUD dirty 가드(A-1) 를 먼저 해도 **새로 만드는 쓰레기 양**만 줄 뿐, **이미 쌓인 힙 735MB** 는 그대로. 스파이크 빈도는 개선 안 되고 크기만 조금 줄어듦. 누수부터 막아야 힙 증가가 멈추고 그 다음 개선이 효과를 냄.

### Phase A — 매 프레임 상시 비용 제거 (퀵윈)

| 항목 | 패턴 |
|---|---|
| **A-1** HUD TMP dirty 가드 | `if (newVal == _lastVal) return;` 패턴을 5~6개 Update 에 적용 |
| **A-2** URP 에셋 튜닝 | Shadow Cascade 4→2, Shadowmap 2048→1024, SSAO off, Bloom HQ off |
| **A-3** BugController `is` 캐시 | 공격 세팅 시점에 `_cleaveCache = attack as CleaveAttack` |

상세 코드 패턴은 [FrameDropInvestigation.md §7-Phase A](FrameDropInvestigation.md) 에 있음.

**MiningUI 특이사항**: `MiningUI.Update()` 에서 매 프레임 `UpdateMiningText(_machine.TotalMined)` 호출. 이미 `OnMiningGained` 이벤트가 있으므로 Update 는 punch 애니메이션만 남기고 텍스트 갱신은 이벤트로 이동.

### Phase B — 웨이브 중 측정 후 조건부

| 항목 | 패턴 |
|---|---|
| **B-1** 드론/거미 OverlapSphere 프레임 분산 | 0.15초 간격으로만 탐색, 나머지 프레임은 캐시 사용 |
| **B-2** VFX/탄환 풀 (`VfxPool` + `PooledVfxReturn`) | 기관총 머즐·탄환·임팩트 우선 풀링 |

B-1 옵션 비교:
- **옵션 1 (시간 슬라이스)**: `_nextScanTime` 필드 추가, 0.15초마다만 `OverlapSphere`. 최소 변경.
- **옵션 2 (BugManager 리스트)**: 살아있는 벌레 리스트를 매니저에서 노출, 물리 쿼리 자체 제거. 근본 해결.

→ 먼저 옵션 1 로 체감 개선 후 병목 남으면 옵션 2.

**B-2 주의점**: 풀 반납 시 파티클 잔상 방지 (`ParticleSystem.Clear()`), 트레일 포함 VFX 는 `TrailRenderer.Clear()` 도 호출.

### Phase C — 여력 있을 때

| 항목 | 내용 |
|---|---|
| **C-1** WorldUiTicker 단일 tick | 드론/거미/벌레 개별 LateUpdate → 싱글톤 하나가 리스트 루프 |
| **C-2** Gizmos 가드 | `OnDrawGizmosSelected` 를 `#if UNITY_EDITOR` 로 감싸기 (위생) |
| **C-3** 미니맵 갱신 주기 | 매 프레임 렌더 → 격프레임 or 0.1s 주기 수동 `camera.Render()` |

---

## 7. 검증 — 단계별 목표 수치

각 단계 후 동일 시나리오(baseline: 벌레 0, 46~60초) 재녹화. 비교 지표:

| 지표 | 현재 | A0 후 기대 | A 후 기대 | 비고 |
|---|---:|---:|---:|---|
| 평균 FPS | 144 | 145+ | 150+ | 큰 변화 없을 수도 |
| **MainThread p99** | **75 ms** | **< 20 ms** | < 15 ms | **핵심 개선 지표** |
| **MainThread max** | **144 ms** | < 30 ms | < 20 ms | **핵심 개선 지표** |
| **스파이크 빈도** | 87회/47s | **< 10회/47s** | < 5회/47s | **체감 직결** |
| **GCUsed** | 735 MB | **증가 중단** (+10MB/세션 이하) | 동일 | **누수 차단 확인** |
| GC Alloc / frame | 25 KB | 15 KB | **< 3 KB** | A-1 효과 |

> 💡 **"MainThread p99" 가 왜 핵심인가**
> 평균은 좋아도 100 프레임에 1번 드는 긴 프레임이 체감 렉의 본질. p99가 75ms → 20ms 가 되면, "15 fps 짜리 프레임이 50 fps 짜리 프레임으로" 바뀌는 것. 여전히 부드럽진 않지만 **끊김이 사라짐**.

### 시나리오별 검증

| 용도 | 시나리오 | 재현 시간 |
|---|---|---|
| Phase A0 검증 (누수) | 벌레 0 유지하며 1~2분 (GCUsed 증가 추적) | 60~120초 |
| Phase A 검증 (UI/URP) | 벌레 0 상태 유지 | 30초 |
| Phase B-1 검증 (드론 스캔) | 드론 5 + 거미 3 + 벌레 10 | 60초 |
| Phase B-2 검증 (VFX 풀) | 기관총 장착, 벌레 20 상대 연사 | 60초 |
| URP 튜닝 시각 검증 | Scene 뷰 동일 각도 전후 스크린샷 | — |

### 시각적 회귀 체크

- 섀도우 단절 (Cascade 4→2 로 인한 원거리 그림자 품질)
- VFX 잔상 (풀 반납 시 파티클 미청소)
- UI 지연 (dirty 가드가 실제 변화까지 놓치는 경우)
- Bloom 품질 (High → Low)

---

## 8. 보조 진단 도구 (필요 시)

`735 MB` 가 A0 의 4개 지점으로 다 설명 안 되면:

1. **Unity Memory Profiler** 스냅샷 2장 (시작 직후 vs 30초 후) → 어떤 타입이 얼마나 늘었는지 비교.
2. Deep Profile 모드에서 `"GC.Alloc"` 콜스택 샘플링 → 알로케이션 주범 함수 특정.
3. `Resources.FindObjectsOfTypeAll<Texture2D>().Length` 를 30초 간격 로깅 → Texture 누수 숫자 확인.

> 💡 **Managed vs Native 구분 주의**
> `GCUsed` 는 **C# Managed Heap** 수치. Texture2D/Sprite 는 대부분 **Native 자원**이라 `GCUsed` 에 직접 잡히지 않음 (관리 핸들만 수백 바이트). 735MB 가 순수 Managed면 **범인은 문자열 누수 or 이벤트 구독 해제 누락** 같은 Managed 쪽일 가능성 큼.
>
> → **Memory Profiler 스냅샷을 A0 착수 전에 먼저 1회** 찍어 범인을 좁히는 것이 가장 확실.

---

## 9. 작업 순서 제안

```
[진단 확정]
  └ Memory Profiler 스냅샷 2장 → 735MB 실체 확인 → A0 우선순위 확정

Phase A0 (누수 차단, PR 1~2개)
  ├ A0-1 BugHpBar 공유 Sprite + 풀 복귀
  ├ A0-2 DamagePopup 풀
  ├ A0-3 SimpleVFX 핫패스 제거 + static 캐시
  └ A0-4 MinimapIcon 풀 복귀
  → 기획자 PC 재측정 (baseline 60~120초, GCUsed 증가 중단 확인)

Phase A (퀵윈, PR 1개로 묶어도 됨)
  ├ A-1 HUD TMP dirty 가드 (4 파일)
  ├ A-2 URP 에셋 튜닝 (4 에셋)
  └ A-3 BugController is 캐시 (1 파일)
  → 벌레 0 시나리오 재측정

Phase B (각각 PR 분리)
  ├ B-1 드론/거미 스캔 분산 (2 파일)
  └ B-2 VFX 풀 (신규 2~3 파일 + 호출부 교체)
  → 스트레스 시나리오 측정

Phase C (선택)
  ├ C-1 WorldUiTicker
  ├ C-2 Gizmos 가드
  └ C-3 미니맵 주기
```

---

## 10. 체크리스트

### 이미 완료
- [x] 프레임 드랍 원인 후보 조사 (`FrameDropInvestigation.md`)
- [x] `PerfRecorder` 세션 로거 도입
- [x] `PerfMarkers` 11개 의심 구간 계측
- [x] Unity 6.4 `GetInstanceID` → `GetEntityId` 마이그레이션 (`SpiderDroneInstance.cs`)
- [x] 개발자 PC baseline 측정 (평균 201 fps, 병목 없음)
- [x] 기획자 PC 실측 CSV 수령 · 분석
- [x] Managed Heap 누수 의심 지점 코드 리뷰로 식별
- [x] Phase A0 (누수 차단) 신설

### 다음
- [ ] Memory Profiler 스냅샷으로 735MB 실체 확인 (권장)
- [ ] A0-1 ~ A0-4 구현
- [ ] A0 후 기획자 PC 재측정 (GCUsed 증가 중단 확인)
- [ ] Phase A 착수 (A-1 ~ A-3, 스텝당 1 커밋)
- [ ] 각 단계 후 재녹화 비교 → 본 문서 §7 표 갱신
- [ ] Phase B 필요성 재평가 (A 완료 기준)

---

## 11. 커밋 단위 제안

```
[A0-1] BugHpBar: 공유 Sprite + 풀 복귀 재활용
  M  Assets/_Game/Scripts/Bug/BugHpBar.cs
  M  Assets/_Game/Scripts/Bug/BugController.cs

[A0-2] DamagePopup 풀
  +  Assets/_Game/Scripts/UI/DamagePopupPool.cs
  M  Assets/_Game/Scripts/UI/DamagePopup.cs
  M  (호출부: BugController, MachineController 등)

[A0-3] SimpleVFX 핫패스 제거 + static 캐시
  M  Assets/_Game/Scripts/VFX/SimpleVFX.cs
  M  Assets/_Game/Scripts/Bug/BugController.cs (PlayHitVfx 정리)

[A0-4] MinimapIcon 풀 복귀 재활용 (필요 시)
  M  Assets/_Game/Scripts/Bug/BugController.cs

[측정]
  PerfLogs/after_a0_*.csv → gitignore (문서에 수치만 반영)
```

---

## 12. 부록 — 실측 원본 메타

```
session_label=baseline
scene=Game
resolution=1469x723
platform=WindowsEditor
isEditor=True
unityVersion=6000.4.0f1
duration_s=46.80
frames=6763
avg_fps=144.5
vSyncCount=0
targetFrameRate=-1
```

스파이크 구간별 평균 (87건 집계):

| 구간 | 평균 ms | 최대 ms | 횟수 |
|---|---:|---:|---:|
| 0-5s | 76.7 | 83.9 | 10 |
| 5-10s | 75.6 | 85.7 | 9 |
| 10-15s | 72.3 | 87.0 | 10 |
| 15-20s | 75.6 | 93.2 | 8 |
| 20-25s | 96.1 | 111.4 | 11 |
| 25-30s | 91.2 | 106.3 | 10 |
| 30-35s | 102.9 | **144.4** | 8 |
| 35-40s | 101.7 | 122.7 | 8 |
| 40-45s | 115.9 | 122.2 | 9 |
| 45-50s | 101.7 | 119.7 | 4 |

**패턴**: 초반 75ms → 후반 115ms. Incremental GC 주기/힙 크기에 비례하는 전형적 GC 스파이크 곡선.

원본 CSV: `PerfLogs/baseline_20260423_063517.csv` + `_spikes.csv`

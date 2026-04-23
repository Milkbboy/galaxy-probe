# 최적화 2차 — 기획자 PC 실측 분석 및 Phase A 착수

> 작성 2026-04-23 · 후속 [Optimization_01.md](Optimization_01.md) §7 체크리스트 이행
> 데이터: `PerfLogs/baseline_20260423_063517.csv` (+ `_spikes.csv`)
> 환경: RTX 3060 · Unity 6000.4.0f1 · WindowsEditor · 1469×723 · vSync off

## 결과 요약

| 항목 | 값 | 해석 |
|---|---|---|
| 세션 길이 | 46.80s / 6763 frames | — |
| 평균 FPS | **144.5** | 평상시는 쾌적 |
| MainThread p50 | 5.46 ms | ≈ 183 fps 상당 |
| MainThread p95 | 8.09 ms | ≈ 124 fps 상당 |
| MainThread p99 | **75.30 ms** | 하위 1% 프레임이 13 fps |
| MainThread max | **144.39 ms** | 최악 7 fps |
| 스파이크 빈도 | 87회 / 46.8s ≈ **1.9 Hz** | 약 78프레임마다 1회 |
| 평균 스파이크 크기 | 초반 75 ms → 후반 **115 ms** | 시간 지날수록 악화 |
| GCUsed | **735 MB** | 이게 핵심 |
| GCReserved | 808 MB | Managed Heap 거의 포화 |
| GC Alloc / frame (평균) | 25 KB | baseline 부하 |

**한 줄 결론**: 평상시 성능은 문제 없음. **주기적 GC-추정 스파이크**(78프레임에 1번, 75~140ms) 가 "렉"으로 체감되며, **Managed Heap 이 735MB 까지 부풀어 있어** 스파이크가 시간 경과에 따라 커지고 있음.

## 1. 스파이크 구간 분석

시간 구간별 스파이크 평균·최대 (87건 집계):

| 구간 | 평균 ms | 최대 ms | 횟수 |
|------|--------:|--------:|-----:|
| 0-5s   | 76.7 | 83.9  | 10 |
| 5-10s  | 75.6 | 85.7  | 9  |
| 10-15s | 72.3 | 87.0  | 10 |
| 15-20s | 75.6 | 93.2  | 8  |
| 20-25s | 96.1 | 111.4 | 11 |
| 25-30s | 91.2 | 106.3 | 10 |
| 30-35s | 102.9 | **144.4** | 8 |
| 35-40s | 101.7 | 122.7 | 8  |
| 40-45s | 115.9 | 122.2 | 9  |
| 45-50s | 101.7 | 119.7 | 4  |

패턴: **초반 75ms → 후반 115ms 로 점진 증가**. Incremental GC 주기/힙 크기에 비례하는 전형적 GC 스파이크 곡선.

## 2. Optimization_01 조사 결과와의 대조

기존 `FrameDropInvestigation.md` 에서 제시한 후보와 실측 정합성:

| 기존 §1.1 HUD TMP 매 프레임 재할당 | 정합 | 평균 25KB/frame alloc 의 주된 기여자. |
| 기존 §1.2 월드 UI LateUpdate 러시 | 부분 정합 | baseline 이라 벌레 0 — 이번 CSV 로는 확인 안 됨. Bug_Update / Hp3DBar_Late / BugLabel_Late 모두 `0.00 ms` (마커가 거의 호출 안 됨). |
| 기존 §2 드론/거미 OverlapSphere | 미적용 | 세션이 `baseline` 라벨이라 드론/거미 없음. `Drone_Update`, `Spider_Update` 모두 `0.00 ms`. |
| 기존 §3 VFX 풀링 부재 | 강한 정합 | 전투 없는 baseline 인데도 스파이크가 주기적 → 다른 스파이크 원인이 더 있음. |
| 기존 §4 URP 과설정 | 부분 정합 | Vertices p99 = 340K (p50 대비 2배), SetPass p99 = 233 (p50 대비 2배) → **어떤 순간에 렌더링 부하가 급증**. URP 단독 원인보다는 "뭔가 한꺼번에 렌더에 들어오는" 순간이 있음. |

### 2.1 CSV 에서 새로 발견한 것

**MainThread max = 144.39 ms** 단일 프레임이 GC Alloc `34 KB` 밖에 안 됨. 즉 "이 프레임에 쓰레기를 많이 만들어서 느린" 게 아니라 **다른 프레임들이 만든 쓰레기를 GC 가 이 프레임에 회수**하는 중.

**735 MB Managed Heap** 은 단순 누적으로는 설명 불가. 46초 × 25KB = 1.1MB 에 불과. → **어딘가에서 지속 누수 중**이란 뜻.

## 3. 누수 의심 지점 — 코드 리뷰로 추가 발견

`BugPool` 로 벌레 GameObject 는 재사용하는데, **벌레에 붙는 월드 UI 가 풀링 범위 밖**. 이게 누수 벡터로 의심됨.

### 3.1 `BugHpBar.Create` — `Texture2D` · `Sprite` 매 생성 [고위험]

`Assets/_Game/Scripts/Bug/BugHpBar.cs:136-142`

```csharp
private static Sprite CreateSquareSprite()
{
    Texture2D texture = new Texture2D(1, 1);   // 매 호출 신규
    texture.SetPixel(0, 0, Color.white);
    texture.Apply();
    return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
}
```

- 호출 경로: `BugController.CreateHpBar` → `BugHpBar.Create` → bg/fill 2개 `SpriteRenderer` 각각 `CreateSquareSprite()`.
- 벌레 1마리당 **Texture2D 2개 + Sprite 2개** 생성.
- `BugController.Die` 에서 `Destroy(_hpBar.gameObject)` 로 GameObject 는 파괴되지만, 흰 점 `Texture2D`/`Sprite` 는 Unity 네이티브 객체 — 스프라이트 참조 제거 후 GC 가 `Resources.UnloadUnusedAssets` 타이밍에야 회수. 그 사이 Managed + Native 힙에 누적.
- 풀에서 벌레를 다시 꺼낼 때 `CreateHpBar` 가 또 호출됨 (`BugController.Start` 내) → **풀링 의미 상실**.

**왜 기존 조사에서 놓쳤나**: §1.2 에서 `Hp3DBar.LateUpdate` 는 계측했으나 `BugHpBar` (벌레용, 다른 클래스) 의 생성 비용은 별도 항목 없었음.

### 3.2 `DamagePopup.Create` — `GameObject` + `TextMeshPro` 매 생성 [고위험]

`Assets/_Game/Scripts/UI/DamagePopup.cs`

```csharp
GameObject popupObj = new GameObject("DamagePopup");
DamagePopup popup = popupObj.AddComponent<DamagePopup>();
_text = gameObject.AddComponent<TextMeshPro>();           // TMP 신규 = 매우 비쌈
TMPFontHelper.ApplyDefaultFont(_text);
// ...
Destroy(gameObject);                                      // 1초 후
```

- TMP 인스턴스 1개 = 메시 + 머티리얼 인스턴스 + 폰트 바인딩. 생성 자체가 수 KB~수십 KB.
- 피격·힐·방어 이벤트마다 호출 → **피격 수 × TMP 생성/파괴** 가 핫패스.

### 3.3 `SimpleVFX.PlayBugHit` 류 — `ParticleSystem` + `Gradient` 매 생성 [중위험]

`Assets/_Game/Scripts/VFX/SimpleVFX.cs`

```csharp
GameObject effectObj = new GameObject("BugHitVFX");
var ps = effectObj.AddComponent<ParticleSystem>();
emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 6) });  // GC
var gradient = new Gradient();                                                        // GC
gradient.SetKeys(new GradientColorKey[] { ... }, new GradientAlphaKey[] { ... });     // GC
Object.Destroy(effectObj, 1f);
```

- 호출 경로: `BugController.PlayHitVfx` (기본 VFX 프리팹이 없을 때 폴백) → `SimpleVFX.PlayBugHit`.
- 벌레 프리팹에 `_hitVfxPrefab` 이 할당돼 있으면 `SpawnScaledVfx` 경로로 가지만, **미할당 벌레 1종이라도 있으면** 대량 스폰 시 이 경로가 터짐.
- 기존 §3 의 "기관총 VFX 풀링 부재" 와 같은 근본 문제 (Instantiate/Destroy + `new Burst[]` + `new Gradient()`).

### 3.4 `MinimapIcon` — 벌레 풀링 이점 삭감 [중위험]

`Assets/_Game/Scripts/Bug/BugController.cs:158-174`

```csharp
private void OnEnable()
{
    if (_minimapIcon == null)
        _minimapIcon = MinimapIcon.Create(transform, ...);   // 신규
    else
        _minimapIcon.gameObject.SetActive(true);              // 재사용
}

private void OnDestroy()
{
    if (_minimapIcon != null)
        Destroy(_minimapIcon.gameObject);                     // 완전 파괴
}
```

- `MinimapIcon` 은 `_minimapIcon == null` 가드가 있어 **같은 벌레 인스턴스 내에서는 재사용**되지만, 풀의 벌레 GameObject 자체가 `Destroy` 되는 순간 (예: 씬 전환, 풀 축소) 같이 파괴됨.
- 정상 게임플레이에서는 누수 없지만, **풀에서 꺼낸 벌레가 첫 활성화될 때 매번 새 `GameObject + MeshFilter + MeshRenderer`** 가 생김. 초기 웨이브 스폰에서 스파이크 기여.

### 3.5 HUD `$"..."` 매 프레임 [기존 §1.1, 재확인됨]

`TopBarHud.Update` · `MachineStatusUI.Update` · `MiningUI.Update` · `AbilityHud.Update` 가 각자 매 프레임 문자열 interpolation → UI mesh rebuild.

- CSV `GCAllocInFrame.avg = 25 KB/frame` 의 주 기여자로 추정.
- Optimization_01 §A-1 dirty 가드 패턴으로 해결 가능.

## 4. 우선순위 재조정

기존 Phase A/B/C 에 **Phase A0 (누수 차단)** 을 앞단에 삽입. 735MB 힙을 먼저 잡아야 나머지 효과가 보임.

### 4.1 Phase A0 — 누수 차단 [신규 · 최우선]

힙 증가 자체를 멈추는 단계. 이게 없으면 A-1 의 GC alloc 감소 효과가 스파이크 빈도 개선으로 이어지지 않음 (스파이크 크기만 줄고 빈도는 유지).

#### A0-1. `BugHpBar` 공유 `Sprite` / 풀링

- **변경**: `CreateSquareSprite()` 를 `static Sprite _sharedSprite` 로 1회 생성 + 재사용.
- **영향**: Texture2D 0 → 1 (전체), Sprite 생성 N*2 → 2.
- **추가 권장**: HP바 자체를 풀링 — `BugController.Die` 에서 `Destroy` 대신 비활성화, 풀 복귀 시 재연결.
- **대상**: `Assets/_Game/Scripts/Bug/BugHpBar.cs`, `Assets/_Game/Scripts/Bug/BugController.cs:CreateHpBar/Die`.

#### A0-2. `DamagePopup` 풀

- **변경**: `DamagePopupPool` 싱글톤. 고정 개수 (예: 64) 의 GameObject + TMP 미리 생성, 순환 사용.
- **영향**: TMP 생성 빈도를 **피격 수 → 초기 1회** 로.
- **대상**: `Assets/_Game/Scripts/UI/DamagePopup.cs` + 호출 경로.

#### A0-3. `SimpleVFX` 핫패스 제거 또는 풀

- **1안 (간단)**: 벌레 프리팹에 `_hitVfxPrefab` 을 반드시 할당해 `SpawnScaledVfx` 경로만 타도록. `SimpleVFX.PlayBugHit` 호출 자체를 제거.
- **2안 (정석)**: Optimization_01 §B-2 의 `VfxPool` 을 `SimpleVFX` 에도 적용. 단 `SimpleVFX` 의 매번 새 `Gradient`/`Burst[]` 는 static 캐싱으로.
- **대상**: `Assets/_Game/Scripts/VFX/SimpleVFX.cs`, 벌레 프리팹.

#### A0-4. `BugHpBar` / `MinimapIcon` 풀 복귀 시 재활용 보장

- **변경**: `BugController.ResetForPool` 에서 `_hpBar` / `_minimapIcon` 를 파괴하지 않고 비활성 유지. 풀 복귀 후 `OnEnable` 에서 재연결.
- 현재 `ResetForPool` 에 이미 `_hpBar = null;` 이 있어 다음 `Start` 에서 새로 만드는 구조 — 이걸 보존형으로 바꿔야.
- **대상**: `Assets/_Game/Scripts/Bug/BugController.cs:ResetForPool, CreateHpBar, Die`.

### 4.2 Phase A (기존) — HUD / URP / BugController is-cast

기존 `FrameDropInvestigation.md §7` 그대로 유지. A0 완료 후 착수.

- A-1 HUD TMP dirty 가드 (TopBar / MachineStatus / Mining / AbilitySlot)
- A-2 URP 에셋 튜닝 (Shadow Cascade 4→2, Shadowmap 2048→1024, SSAO off, Bloom HQ off)
- A-3 `BugController.Update` 의 `is CleaveAttack` / `is BeamAttack` 캐싱

### 4.3 Phase B (기존) — 웨이브 중 측정 후 조건부

- B-1 드론/거미 OverlapSphere 프레임 분산
- B-2 무기 VFX 풀

### 4.4 Phase C (기존) — 여력 있을 때

- C-1 WorldUiTicker 단일 tick
- C-2 Gizmos 가드
- C-3 미니맵 갱신 주기

## 5. 측정 계획

각 단계 후 동일 시나리오(baseline: 벌레 0, 46~60초 유지)로 PerfRecorder 녹화. 비교 지표:

| 지표 | Pre (현재) | A0 후 기대 | A 후 기대 | 비고 |
|------|-----------:|----------:|---------:|------|
| 평균 FPS | 144 | 145+ | 150+ | 큰 변화 없을 수 있음 |
| MainThread p99 | **75 ms** | **< 20 ms** | < 15 ms | 핵심 개선 지표 |
| MainThread max | **144 ms** | < 30 ms | < 20 ms | 핵심 개선 지표 |
| 스파이크 빈도 | 87회/47s | **< 10회/47s** | < 5회/47s | 체감 직결 |
| GCUsed | 735 MB | **증가 중단** (세션 내 +10 MB 이하) | 동일 | 누수 차단 확인 |
| GC Alloc / frame 평균 | 25 KB | 15 KB | **< 3 KB** | A-1 효과 |

기획자 PC 에서 A0 적용 후 CSV 1건, A 적용 후 CSV 1건 추가 수집 → 본 문서 §5 표 갱신.

## 6. 필요 시 추가 진단

누수 원인이 위 4개로 다 설명 안 되면:

1. **Unity Memory Profiler** 스냅샷 2장 (시작 직후 vs 30초 후) → Managed 증가분 객체 유형 확인.
2. `ProfilerRecorder` 로 `"GC.Alloc"` 콜스택 샘플링 (Deep Profile 필요, 에디터 전용).
3. `Resources.FindObjectsOfTypeAll<Texture2D>().Length` 를 30초 간격으로 로깅 → Texture 누수 확인.

## 7. 커밋 단위 제안

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
  +  PerfLogs/after_a0_*.csv  (ignore 대상, 참조용으로 본 문서에만 요약)
```

## 8. 체크리스트

- [x] 기획자 PC CSV 수령 · 분석
- [x] Managed Heap 누수 의심 지점 코드 리뷰로 식별
- [x] Phase A0 (누수 차단) 신설
- [ ] A0-1 ~ A0-4 구현
- [ ] 기획자 PC 에서 A0 후 재측정
- [ ] Optimization_01 §7 의 Phase A 착수 (기존 계획대로)
- [ ] Phase B 필요성 재평가 (A 완료 기준)

## 9. 부록 — 분석에 사용한 원본 CSV 메타

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
qualityLevel=0
```

스파이크 87건 전체 원본은 `PerfLogs/baseline_20260423_063517_spikes.csv` 참조.

# Drill-Corp 사운드 시스템

> 상위 문서: `DRILL-CORP-PLAN.md`
> 상태: Phase A(게임플레이 핵심) 대부분 구현 완료 / Phase B(UI·이벤트) 및 Phase C(BGM) 대기

## 0. 개요

**외부 WAV/OGG 파일 기반의 경량 SFX 매니저**. 초기에는 프로토(_.html) 합성 방식(SfxSynth) 이식을 시도했으나 Unity 외부 파일 쪽이 더 실용적이라 판단하고 코드 합성 경로는 제거. 현재는 인스펙터에 할당한 AudioClip을 AudioSource Pool 또는 전용 Source로 재생.

**특징**
- 싱글톤 `AudioManager` — 씬 로드 시 자동 생성, `DontDestroyOnLoad`
- AudioSource 풀(8개) + 기관총/레이저 전용 Source 2개 (연사·loop 제어 전담)
- 클립별 볼륨 필드 (0~2 범위, 부스트 가능)
- 디버그 Enable 토글 — 소리를 하나씩 켜며 단독 테스트
- `GameEvents` 구독으로 머신 피격·벌레 사망음 자동 재생

---

## 1. 아키텍처

```
 ┌──────────────── AudioManager (Singleton) ─────────────────┐
 │                                                            │
 │  [AudioClip 필드 × 8]                                      │
 │    _sfxMachineGunFire  _sfxSniperFire                      │
 │    _sfxBombLaunch      _sfxBombExplosion                   │
 │    _sfxLaserBeam       _sfxBugHit                          │
 │    _sfxBugDeath        _sfxMachineDamaged                  │
 │                                                            │
 │  [AudioSource]                                             │
 │    • _sfxPool[8]         (PlayOneShot 라운드로빈)          │
 │    • _machineGunSource   (Stop → Play, 연사 겹침 방지)     │
 │    • _laserSource        (loop=true, 빔 수명 동안 지속)    │
 │                                                            │
 │  [GameEvents 구독]                                         │
 │    OnMachineDamaged → PlayMachineDamaged (150ms 쓰로틀)    │
 │    OnBugKilled      → PlayBugDeath                         │
 │                                                            │
 │  [공개 API]                                                │
 │    PlayMachineGunFire / PlaySniperFire / PlayBombLaunch    │
 │    PlayBombExplosion  / PlayBugHit                         │
 │    StartLaserBeamLoop / StopLaserBeam                      │
 │    SetMasterVolume / SetSfxVolume                          │
 └────────────────────────────────────────────────────────────┘
```

**최종 볼륨 = `_masterVolume × _sfxVolume × _vol<SFX>`** (모두 인스펙터 슬라이더). 기관총은 전용 Source 라 `AudioSource.volume`에 직접 반영, 나머지는 `PlayOneShot(clip, volumeScale)`의 스케일로 전달.

---

## 2. SFX 레지스트리

| # | ID (필드) | 트리거 | 호출 경로 | Vol 기본 | 비고 |
|---|-----------|--------|----------|---------|------|
| 1 | `_sfxMachineGunFire` | 기관총 발사 | `MachineGunWeapon.Fire:145` → `PlayMachineGunFire` | 0.4 | 전용 Source, 피치 ±8% 변주 |
| 2 | `_sfxSniperFire` | 저격 명중 | `SniperWeapon.Fire` (hit>0) → `PlaySniperFire` | 1.0 | PlayOneShot |
| 3 | `_sfxBombLaunch` | 폭탄 발사 | `BombWeapon.Fire` → `PlayBombLaunch` | 1.0 | PlayOneShot |
| 4 | `_sfxBombExplosion` | 폭탄 폭발 | `BombProjectile.Detonate` → `PlayBombExplosion` | 1.2 | PlayOneShot |
| 5 | `_sfxLaserBeam` | 레이저 빔 생존 | `LaserWeapon.Fire:103` → `StartLaserBeamLoop` / `LaserBeam.OnDestroy` → `StopLaserBeam` | 1.0 | 전용 Source, loop=true |
| 6 | `_sfxBugHit` | 벌레 피격 | `BugBase.TakeDamage:251` / `BugController.TakeDamage:709` / `SimpleBug.TakeDamage:95` → `PlayBugHit` | 0.6 | 피치 ±10% 변주 |
| 7 | `_sfxBugDeath` | 벌레 사망 | `GameEvents.OnBugKilled` → `HandleBugKilled` | 1.0 | 자동 구독 |
| 8 | `_sfxMachineDamaged` | 머신 피격 | `GameEvents.OnMachineDamaged` → `HandleMachineDamaged` | 1.0 | 150ms 쓰로틀 (연속 피격 시) |

### 같은 클립 중첩 방지
`PlayOneShot` 경로에는 **`SameClipMinInterval = 30ms`** 가드 — 같은 AudioClip이 연속 프레임에 여러 번 재생되면 한 번만 남김. 벌레 여러 마리가 한 프레임에 죽을 때 소리가 뭉치는 현상 완화.

### 머신 피격 쓰로틀
프로토 `sndHitThrottled`와 동일하게 150ms 간격 보장 (`MachineDamagedMinInterval`). 벌레 떼가 붙어 매 프레임 이벤트가 터지는 상황에서 귀 아픈 소음 방지.

---

## 3. 무기·벌레 통합 지점

### 무기 측 (`Assets/_Game/Scripts/Weapon/`)
- `MachineGun/MachineGunWeapon.cs:145` — 총알 스폰 직후
- `Proto/SniperWeapon.cs:45` — 범위 내 벌레 최소 1마리 피격 시
- `Bomb/BombWeapon.cs` — 투사체 스폰 직후 / `Bomb/BombProjectile.cs` — Detonate
- `Laser/LaserWeapon.cs:103` — 빔 스폰 직후 `StartLaserBeamLoop`
- `Laser/LaserBeam.cs:OnDestroy` — 수명 만료·자파괴 시 `StopLaserBeam` 자동 호출

### 벌레 측 (`Assets/_Game/Scripts/Bug/`)
- `BugBase.TakeDamage:251` (레거시 BeetleBug/FlyBug/CentipedeBug)
- `BugController.TakeDamage:709` (신규 조합형)
- `Simple/SimpleBug.TakeDamage:95` — `PlayBugHit` + `GameEvents.OnBugKilled?.Invoke(GetInstanceID())` 직접 호출 (다른 벌레 계열과 달리 자체 구현이라 수동 훅)

### OptionsUI 연결
`Assets/_Game/Scripts/OutGame/OptionsUI.cs:143` — SFX 슬라이더의 `onValueChanged` → `AudioManager.Instance?.SetSfxVolume(value)`. Master는 현재 `AudioListener.volume` 직접 조작, BGM 슬라이더는 Phase C에서 `BgmManager` 연결 예정.

---

## 4. 에디터 툴: AudioTrimWindow

긴 연사 녹음·tail이 긴 외부 파일을 짧은 1-shot으로 가공하기 위한 편집기.

메뉴: `Tools → Drill-Corp → Audio → Trim AudioClip`
파일: `Assets/_Game/Scripts/Editor/AudioTrimWindow.cs`

**주요 기능**
- 파형 뷰 (peak-per-column, `AudioClip.GetData` + `Texture2D` 직접 렌더)
- 드래그 핸들 4종
  - 🟢 **S** — Start
  - 🔴 **E** — End
  - 🟠 **FI** — Fade In 끝점 (`Start + FadeIn`)
  - 🟠 **FO** — Fade Out 시작점 (`End - FadeOut`)
- Zoom: `View Start/End` 숫자 입력 + `Fit All` / `Fit Sel` 버튼
- 핸들 드래그 ↔ 숫자 필드 양방향 바인딩
- Preview: 원본 에셋을 Start 위치부터 재생, (End-Start)초 후 자동 Stop
  - ※ `AudioUtil.PlayPreviewClip`은 런타임 생성 AudioClip에서 Unity 6 Windows 무음 이슈가 있어 원본 에셋을 재생하는 방식으로 우회
  - ※ Fade/Mono는 Preview에 반영되지 않음 — Save 후 Project view에서 완제품 확인
- Save: 선택 구간 + 선형 페이드 인/아웃 + forceMono 다운믹스 → 16-bit PCM WAV 파일 생성 (원본과 동일 폴더, `_Short` suffix)

**Preset**: `첫 0.3s (MachineGun 1-shot)` — Start=0, End=0.3, FadeOut=0.05, Mono로 일괄 설정 + 파형 뷰 0~0.6s로 확대

---

## 5. 사용자 튜닝 워크플로

**1) 단독 테스트** — 한 번에 한 소리씩
1. Play 모드
2. AudioManager Inspector에서 `Enable <SFX>` 토글 중 테스트할 것만 ON, 나머지 OFF
3. 게임 내에서 해당 SFX가 발생하는 상황 유도
4. 소리 크기·밸런스 확인

**2) 볼륨 조정** — Play 중에도 슬라이더 실시간 반영
1. 튜닝 값 찾으면 컴포넌트 우클릭 → **Copy Component**
2. Play 중지 → **Paste Component Values** (저장됨)
3. 저장 안 하면 Play 종료 시 원복

**3) 소스 파일 가공** — 긴 외부 파일을 짧은 1-shot으로
1. `Tools → Drill-Corp → Audio → Trim AudioClip`
2. Source 드래그 → 핸들 드래그로 구간 지정 → Preview로 확인 → Save
3. 생성된 `*_Short.wav`를 AudioManager 슬롯에 할당

---

## 6. 폴더 구조

```
Assets/_Game/
├── Audio/                      # 외부 음원 파일
│   └── SFX/
│       ├── Weapon/  MachineGun_Fire.wav, MachineGun_Fire_Short.wav, SniperGun_Fire.ogg,
│       │            Bomb_Launch.mp3, Bomb_Explosion.flac, Laser_Beam.wav
│       ├── Bug/     Bug_Hit.wav, Bug_Death.mp3
│       └── Machine/ (머신 피격음 — 추후 추가)
└── Scripts/
    ├── Audio/
    │   └── AudioManager.cs
    └── Editor/
        └── AudioTrimWindow.cs
```

BGM 파일은 Phase C에서 `Assets/_Game/Audio/Bgm/` 디렉토리에 추가 예정.

---

## 7. Phase 진행 상황

### Phase A — 게임플레이 핵심 (구현 완료)
| # | 항목 | 상태 | 파일 |
|---|------|------|------|
| 1 | Shoot (저격 명중) | ✅ | `SniperGun_Fire.ogg` |
| 2 | GunShot (기관총) | ✅ | `MachineGun_Fire.wav` |
| 3 | BombLaunch | ✅ | `Bomb_Launch.mp3` |
| 4 | Explosion | ✅ | `Bomb_Explosion.flac` |
| 5 | LaserOn | ✅ | `Laser_Beam.wav` (loop) |
| 6 | Kill (벌레 사망) | ✅ | `Bug_Death.mp3` |
| 7 | EliteKill | ❌ | 엘리트 분기 미구현 — 추후 `BugController.Die`에 조건 추가 |
| 8 | HitThrottled (머신 피격) | ✅ | (미할당 — 파일 준비 필요) |
| — | BugHit (벌레 피격) | ✅ 추가 | `Bug_Hit.wav` |

### Phase B — UI/이벤트 (대기)
- ButtonClick / ButtonHover / WaveStart / GameOver / SessionWin / Unlock / Growth / TunnelWarning

### Phase C — BGM (대기)
- BgmMenu / BgmInGame / BgmResult
- `BgmManager` 별도 구현 + OptionsUI의 BGM 슬라이더 연결
- BGM 파일은 Pixabay/itch.io/OpenGameArt 등 무료 음원 또는 직접 제작

---

## 8. 튜닝 포인트 / 향후 과제

- **기관총 1-shot 교체 적용**: 원본 `MachineGun_Fire.wav`(11.6초 연사)를 `AudioTrimWindow`로 0~0.3s 구간을 잘라 `MachineGun_Fire_Short.wav` 생성 완료. AudioManager 인스펙터에서 `_sfxMachineGunFire` 슬롯을 Short 버전으로 교체하면 적용됨.
- **EliteKill**: `BugController.Die` 분기에 `_data.IsElite` 체크 추가 + `_sfxEliteKill` 필드 신설.
- **MachineDamaged 클립 할당**: 현재 필드는 있으나 파일 미할당.
- **피치 변주**: 기관총 ±8%, 벌레 피격 ±10%. 단조로움 완화용. 필요 시 인스펙터에서 변주 폭을 슬라이더화 검토.
- **3D 사운드**: 현재 전부 2D (`spatialBlend=0`). 게임 범위가 좁아 효과 미미 — 보류.
- **압축 포맷 통일**: 현재 WAV/MP3/FLAC/OGG 혼재. Unity Import 설정(Vorbis/ADPCM)으로 빌드 크기 최적화 Phase C 전에 한 번 정리.

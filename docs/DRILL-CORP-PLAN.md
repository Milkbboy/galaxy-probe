# Drill-Corp 게임 기획 & 개발 계획

> 개발명: Drill-Corp / 출시명: Galaxy Probe  
> 엔진: Unity 6 (URP) / 장르: 로그라이트 디펜스 슈팅 생존  
> 작성일: 2025

---

## 목차

1. [게임 컨셉](#1-게임-컨셉)
2. [핵심 재미 (도파민 루프)](#2-핵심-재미-도파민-루프)
3. [전체 게임 루프](#3-전체-게임-루프)
4. [머신 시스템](#4-머신-시스템)
5. [오퍼레이터 & 무기 시스템](#5-오퍼레이터--무기-시스템)
6. [벌레 시스템](#6-벌레-시스템)
7. [카메라 시스템](#7-카메라-시스템)
8. [미니맵](#8-미니맵)
9. [강화 시스템](#9-강화-시스템)
10. [최적화 전략](#10-최적화-전략)
11. [개발 계획](#11-개발-계획)
12. [미확정 사항](#12-미확정-사항)

---

## 1. 게임 컨셉

- 행성에 착륙한 **드릴 머신**이 자원을 채굴하는 동안
- 밀려오는 **수백~천 마리의 벌레 떼**로부터 머신을 방어하는 게임
- 1판 = **1분**, 생존하면 자원 획득 → 강화 → 반복

---

## 2. 핵심 재미 (도파민 루프)

```
처음엔 약함 → 벌레 1마리도 버거움
    ↓
자원 채굴 → 장비 강화
    ↓
강해짐 → 수백 마리를 시원하게 쓸어버림
    ↓
도파민 팍! 팍!
```

> **핵심**: "처음엔 힘들다가 → 강화 후 무쌍" 이 반복되는 쾌감

---

## 3. 전체 게임 루프

```
[홈 화면 / 로비]
├── 오퍼레이터 선택 (무기 결정)
├── 장비 / 드릴(머신) 강화 (자원 소비)
└── 게임 시작
        ↓
[인게임 - 1분]
├── 드릴이 행성 자원 자동 채굴
├── 벌레 떼가 웨이브로 공격
├── 오퍼레이터 + 머신포로 방어
└── 1분 생존 → 클리어 / HP 0 or 연료 고갈 → 게임오버
        ↓
[클리어 후]
└── 자원 획득 → 로비 복귀 → 강화 → 재도전
```

---

## 4. 머신 시스템

### 기본
- 맵 **중앙에 고정**
- 드릴로 행성 자원 **자동 채굴**
- 머신 HP = 0 → 게임오버
- 연료 고갈 → 게임오버

### 머신 자동포
- **마우스 방향**으로 일정 시간마다 자동 발사
- 오퍼레이터 무기와 **독립적으로 동작**
- 오퍼레이터 특성과 **조합 시 다른 효과** 발생 (스킬 트리)

---

## 5. 오퍼레이터 & 무기 시스템

### 오퍼레이터
- 머신 주변에 **고정** (이동 없음)
- **마우스 커서 위치**로 주무기 발사
- 오퍼레이터마다 **주무기 + 머신 영향 특성** 보유

### 주무기 종류

| 무기 | 발사 스타일 | 재장전 | 재미 포인트 |
|------|------------|--------|------------|
| 샷건 | 탕! 탕! 탕! (넓게 퍼짐) | 수동 재장전 → 철컥 | 리듬감, 근접 쾌감 |
| 다발총 | 다다다다다 (고속 연사) | 탄창 소진 후 자동 → 철컥 | 속도감, 무쌍 |
| 레이저 | 커서를 느리게 따라오는 스윙 빔 | 과열(Heat) 방식 | 줄기로 쓸어버리는 맛 |
| 락온 | 에임으로 30~50마리 틱!틱! 타게팅 후 클릭 → 머신에서 미사일 팡!팡!팡! | 길다 (위~잉) | 타게팅 쾌감 + 무더기 폭발 |

### 머신 무기 / 스킬 종류

| 무기/스킬 | 동작 방식 |
|----------|---------|
| 기본 자동포 | 마우스 방향 자동 발사 |
| 블랙홀 런처 | 특정 위치에 블랙홀 생성 → 벌레 흡입 → 뭉침 |
| 체인 라이트닝 | 한 마리 → 주변으로 전기 연쇄 전달 |
| 클러스터 봄 | 공중 폭발 → 소형 폭탄 산개 → 융단폭격 |
| 소닉 웨이브 | 머신 360도 충격파 → 넉백 + 데미지 |
| 드론 스웜 | 공격 드론 배치 → 자동으로 벌레 추적 공격 |
| 플레임 스로워 | 불꽃 분사 → 지나간 자리에 불길 잔류 |

### 오퍼레이터 × 머신 조합 효과 (스킬 트리)

> 같은 머신 무기라도 오퍼레이터에 따라 완전히 다르게 동작

| 머신 무기 | 오퍼레이터 특성 | 조합 효과 |
|----------|--------------|---------|
| 기본 자동포 | 샷건 | 자동포가 산탄으로 변함 |
| 기본 자동포 | 레이저 | 자동포 관통 추가 |
| 기본 자동포 | 락온 | 자동포가 가장 가까운 적 자동 추적 |
| 블랙홀 | 체인 라이트닝 | 뭉친 벌레에 전기 연쇄 폭발 |
| 소닉 웨이브 | 플레임 | 밀려난 벌레 경로에 불길 생성 |
| 블랙홀 | 드론 | 블랙홀 위치에 드론 자동 집결 |

### 클래스 구조

```
WeaponBase (추상 클래스)
├── Shotgun
├── BurstGun
├── LaserBeam
└── LockOn

MachineWeaponBase (추상 클래스)
├── MachineGun (기본 자동포)
├── BlackHoleLauncher
├── ChainLightning
├── ClusterBomb
├── SonicWave
├── DroneSwarm
└── FlametHrower
```

---

## 6. 벌레 시스템

### 목표 동시 등장 수: 600 ~ 1,000마리

### 스폰: 웨이브 방식

| 웨이브 | 구성 예시 | 총 마리 수 |
|--------|---------|----------|
| Wave 1 | 소형 × 3 | ~75마리 |
| Wave 2 | 중형 × 3 | ~200마리 |
| Wave 3 | 중형 × 2 + 대형 × 1 | ~300마리 |
| Wave 4 | 대형 × 3 | ~500마리 |
| Wave N | 거대 진형, 다방향 동시 등장 | 600~1000마리 |

### 군집(Formation) 크기

| 크기 | 마리 수 (랜덤) | 느낌 |
|------|-------------|------|
| 소형 | 15~35마리 | 정찰대, 빠름 |
| 중형 | 40~90마리 | 기본 군집 |
| 대형 | 120~200마리 | 압도적인 떼 |

### 진형 종류

| 진형 | 특징 |
|------|------|
| Cluster | 원형 뭉텅이 (기본) |
| Line | 일렬 종대 (돌파형) |
| Swarm | 느슨한 군집 (살짝 흔들림) |

- `FormationLeader`가 머신을 향해 이동
- 소속 벌레들은 리더 주변 **오프셋 위치 유지**

### 머신 근접 시 3단계 행동

```
Phase 1 : 진형 유지한 채 공격 (외곽 벌레 돌진)
Phase 2 : 점점 흐트러지면서 개별 행동
Phase 3 : 완전 해체 → 머신에 달라붙어 공격
```

### 벌레 종류 (비헤이비어로 처리)

```
BugBase (추상)
├── BeetleBug    - 기본형
├── FlyBug       - 비행형
└── CentipedeBug - 대형 / 돌진형
```

---

## 7. 카메라 시스템

### 구현 방식: Nuclear Throne 스타일 ✅ 구현 완료

- **Orthographic Size는 고정**
- 카메라 위치가 **머신과 마우스 사이**로 블렌드 이동
- 마우스 쪽으로 살짝 따라가 전방 시야 확보
- 줌 변화 없음 → 에임 정확도 안정적

```
카메라 위치 = Lerp(머신, 마우스월드, MouseWeight=0.3)
             + MaxOffset 클램프
             + SmoothSpeed로 보간
```

### 왜 줌 방식이 아닌가?
- 줌 변경 시 픽셀당 월드 거리 변동 → 조준 어려움
- 마우스 움직일 때마다 화면 흔들림 → 피로감

### 상세 문서
`docs/CameraSystem.md` 참조 (파라미터, 튜닝 가이드, 확장 기능)

---

## 8. 미니맵

- 별도 **Orthographic 카메라** → RenderTexture → UI RawImage
- 머신: 흰 점 / 벌레: 빨간 점 / 오퍼레이터: 초록 점
- 벌레 전용 레이어로 미니맵에만 표시

---

## 9. 강화 시스템

| 강화 대상 | 효과 예시 |
|----------|---------|
| 주무기 | 샷건 탄수 증가 / 레이저 폭 넓어짐 / 연사속도 UP |
| 드릴(머신) | 채굴 속도 UP / 머신 HP 증가 |
| 머신 자동포 | 발사 속도 UP / 데미지 UP |
| 오퍼레이터 | 재장전 속도 / 탄 퍼짐 감소 |

- 강화 재화: 인게임에서 드릴로 채굴한 **자원**
- 강화 위치: **홈 화면(로비)**

---

## 10. 최적화 전략

600~1000마리 목표를 위한 필수 적용 항목

| 기법 | 내용 | 예상 효과 |
|------|------|---------|
| Object Pooling | Instantiate/Destroy 제거, 미리 생성 후 재사용 | GC 스파이크 제거 |
| GPU Instancing | 머티리얼 Enable GPU Instancing 체크 | 드로우콜 대폭 감소 |
| Update 분산 | BugManager가 매 프레임 N마리씩 순서대로 처리 | CPU 부하 분산 |
| 카메라 밖 비활성화 | OnBecameInvisible → enabled = false | 불필요한 연산 제거 |
| 직접 벡터 이동 | NavMesh 대신 Vector 직접 계산 | 수백 마리 한계 없음 |

---

## 11. 개발 계획

### 개발 순서 근거

```
의존성:  카메라 → 벌레 등장 → 무기 → 미니맵
체감:    카메라 → 벌레 떼   → 쏘기 → 미니맵
```
두 기준이 같은 순서 → 이 순서로 진행

---

### 전체 진행 상황 (2026-04-13 기준)

| Phase | 내용 | 상태 |
|-------|------|------|
| **Phase 0 (추가)** | Bug Behavior 시스템 | ✅ 완료 |
| **Phase 1** | 카메라 (Nuclear Throne 방식) | ✅ 완료 |
| **Phase 2** | 벌레 군집 (Formation) | ✅ 완료 |
| **Phase 3** | 무기 시스템 (주무기 4종) | ✅ 완료 |
| **Phase 4** | 미니맵 | ✅ 완료 (기본 표시) |
| **Phase 5 (추가)** | 기존 BugBase 코드 제거 | ⏳ 대기 |

> **참고**: 기획서 Phase 2 (군집)와 별개로 **Bug Behavior 시스템**이 먼저 구축됨.
> 개별 행동(Movement/Attack/Passive/Skill/Trigger) 조합 기반.
> 상세: `docs/BugBehaviorSystemAnalysis.md`

---

### Phase 0 (추가) — Bug Behavior 시스템 ✅

**목표**: 데이터 기반 행동 조합으로 다양한 적 유형 생성

- [x] 5가지 행동 인터페이스 (Movement/Attack/Passive/Skill/Trigger)
- [x] 각 카테고리별 구현체 (총 26종)
- [x] BugBehaviorData SO (행동 조합)
- [x] 조건부 행동 전환 시스템
- [x] Google Sheets Import 연동
- [x] BugController (통합 관리자)

**관련 문서**: `BugBehaviorSystemAnalysis.md`, `BugBehaviorImportGuide.md`

---

### Phase 1 — 카메라 (Nuclear Throne 방식) ✅

**목표**: 마우스 쪽으로 카메라가 따라가 전방 시야 확보

- [x] 머신 ↔ 마우스 블렌드 위치 계산
- [x] Position Lerp 기반 부드러운 이동
- [x] MaxOffset 클램프 (안전장치)
- [x] CameraSettingsData SO ([Range] 슬라이더)
- [x] DebugCameraUI (F1 런타임 조정)
- [x] Gizmo 시각화

**구현 완료**: 2026-04-13
**관련 문서**: `CameraSystem.md`

> 최초 기획은 "줌인/줌아웃" 방식이었으나, 에임 정확도와 피로감 문제로 Nuclear Throne 방식으로 변경.

---

### Phase 2 — 벌레 등장 (군집) ⏳

**목표**: 600마리 이상 군집으로 등장, 60fps 유지

> **주의**: 현재 Bug Behavior 시스템과 **통합 설계 필요**.
> Formation(군집)은 개별 Bug를 묶는 상위 개념이어야 함.

#### 2-1. BugPool (Object Pooling)
- [ ] BugPool 싱글톤 구현
- [ ] 풀 사이즈 설정 (ScriptableObject)
- [ ] Get / Return 메서드

#### 2-2. FormationLeader 이동
- [ ] FormationLeader 머신 방향 이동
- [x] 소속 버그 오프셋 위치 추적 (기존 BugController와 연동)
- [x] Phase 1/2/3 전환 로직 (머신 거리 기반)

#### 2-3. FormationGroup
- [x] 진형 종류 (Cluster / Line / Swarm)
- [x] 진형 내 벌레 수 랜덤 (소/중/대형)
- [x] 맵 외곽 스폰 위치 계산

#### 2-4. WaveManager 확장
- [x] 웨이브 데이터 ScriptableObject (기존)
- [x] FormationSpawnEntry 추가 + 병렬 스폰 코루틴
- [ ] 1분 타이머 연동 (향후)

#### 2-5. 최적화 적용
- [x] Update 분산 구조 (BugManager 준비)
- [x] OffscreenVisibilityTracker
- [ ] GPU Instancing 머티리얼 설정 (에디터 작업)
- [ ] 투사체/VFX Object Pool (별도 작업)

**완료 기준**: ✅ 600마리 군집이 외곽에서 등장해 머신으로 밀려오는 구조 완성
**미해결 이슈**: 리더 사망 처리, 투사체 풀, GPU Instancing

---

### Phase 3 — 무기 시스템

**목표**: 오퍼레이터 주무기 + 머신 자동포 동작

#### 3-1. WeaponBase 추상 클래스
- [ ] Fire() / Reload() 인터페이스
- [ ] 탄약 / 재장전 상태 관리
- [ ] BulletPool 연동

#### 3-2. 주무기 구현 (순서대로)
- [ ] Shotgun (산탄, 수동 재장전)
- [ ] BurstGun (연사, 자동 재장전)
- [ ] LaserBeam (스윙 빔, Heat 시스템)
- [ ] LockOn (타게팅 UI + 미사일 발사)

#### 3-3. 머신 자동포
- [ ] MachineGun 마우스 방향 자동 발사
- [ ] 발사 간격 설정 (ScriptableObject)

#### 3-4. 조합 효과 (기초)
- [ ] 오퍼레이터 특성 데이터 구조 설계
- [ ] 머신 무기 + 오퍼레이터 특성 결합 로직

**완료 기준**: 샷건/다발총/레이저/락온 모두 동작, 머신 자동포 발사

---

### Phase 4 — 미니맵 ✅ (기본 표시 완료)

**목표**: 벌레/머신/오퍼레이터 위치 실시간 표시

- [x] 미니맵 전용 Orthographic 카메라 세팅 (`MinimapCamera.cs`)
- [x] RenderTexture 생성 및 UI RawImage 연결 (`MinimapUI.cs`)
- [x] 벌레 전용 레이어 분리 (`Minimap` 레이어)
- [x] 머신(파랑 사각형), 벌레(빨강 원) 아이콘 — 오퍼레이터는 향후 추가
- [x] 메시/머티리얼 캐싱으로 수백 마리 대응

**완료 기준**: 미니맵에서 벌레 떼가 밀려오는 게 실시간으로 보임 ✅

**남은 개선**: 배경 프레임, 원형 마스크, 벌레 종류별 색상 분기, 조준 방향 표시 등 — `docs/MinimapSystem.md` Phase 2~4 참고

---

### 클래스 구조 전체

```
Systems/                    ⏳ Phase 2에서 구현
├── WaveManager.cs          - 웨이브 진행, 1분 타이머 (일부 구현)
├── FormationSpawner.cs     - 외곽 스폰 위치 계산
├── FormationGroup.cs       - 군집 리더 + 벌레 오프셋
├── BugPool.cs              - Object Pool
├── BugManager.cs           - Update 분산 관리
└── MiniMapController.cs    - 미니맵 (Phase 4)

Bugs/                       ✅ Bug Behavior 시스템 완성
├── BugController.cs        - 통합 관리자 (BugBase 대체)
├── Behaviors/
│   ├── Movement/           - 8종 (Linear, Hover, Orbit, ...)
│   ├── Attack/             - 5종 (Melee, Projectile, ...)
│   ├── Passive/            - 6종 (Armor, Shield, ...)
│   ├── Skill/              - 4종 (Nova, Spawn, ...)
│   └── Trigger/            - 3종 (Enrage, ExplodeOnDeath, ...)
└── (BugBase/BeetleBug/... ─ Phase 5에서 제거 예정)

Weapons/                    ⏳ Phase 3에서 구현
├── WeaponBase.cs
├── Shotgun.cs / BurstGun.cs / LaserBeam.cs / LockOn.cs
└── Machine/                - 7종 (자동포, 블랙홀, ...)

Camera/                     ✅ Phase 1 완료
├── DynamicCamera.cs        - Nuclear Throne 방식
├── CameraSettingsData.cs   - SO
└── DebugCameraUI.cs        - F1 런타임 디버그

ScriptableObjects/
├── WaveData.cs             ✅ 구현
├── BugData.cs              ✅ 구현
├── BugBehaviorData.cs      ✅ 구현
├── CameraSettingsData.cs   ✅ 구현
├── MachineData.cs          ✅ 구현
├── UpgradeData.cs          ✅ 구현
├── FormationData.cs        ⏳ Phase 2
├── WeaponData.cs           ⏳ Phase 3
└── OperatorData.cs         ⏳ Phase 3
```

---

## 12. 미확정 사항

- 로그라이트 강화 방식 (런마다 랜덤 선택? 고정 트리?)
- 오퍼레이터 종류 / 스탯 / 몇 종?
- 연료 소비 / 충전 메커니즘 상세
- 스테이지 / 행성 종류
- 머신 무기 슬롯 수 (몇 개까지?)
- 스킬 트리 깊이 (얼마나 복잡하게?)
- **Phase 2 통합 설계**: Formation(군집)과 기존 Bug Behavior 시스템을 어떻게 결합할지
  - 안 1) Formation이 Bug들을 그룹핑하고, 개별 Bug는 기존 Behavior로 동작
  - 안 2) FormationMovement를 Movement Behavior 중 하나로 추가

# 무기 / 무기 강화 → 구글 시트 마이그레이션 제안서

> 목적: 무기 5종 + 무기 강화 15종을 구글 시트로 빼서 **밸런스 튜닝을 시트에서 즉시 반영**하도록.
>
> 이미 적용된 동일 패턴: `SimpleBugData`, `WaveData`, `MachineData`, `UpgradeData` (4개 탭이 시트로 운영 중)

---

## 1. 현재 상태 — Unity 안에서만 존재

```
Assets/_Game/Data/
├── Weapons/                       ← 5개 SO (각자 다른 C# 클래스)
│   ├── Weapon_Sniper.asset        SniperWeaponData
│   ├── Weapon_Bomb.asset          BombData
│   ├── Weapon_MachineGun.asset    MachineGunData
│   ├── Weapon_LaserBeam.asset     LaserWeaponData
│   └── Weapon_Saw.asset           SawWeaponData
│
└── WeaponUpgrades/                ← 15개 SO (모두 같은 클래스)
    ├── WeaponUpgrade_Gun_Damage.asset      \
    ├── WeaponUpgrade_Gun_Ammo.asset         |  Gun 강화 3종
    ├── WeaponUpgrade_Gun_Reload.asset      /
    ├── WeaponUpgrade_Sniper_Damage.asset    \
    ├── WeaponUpgrade_Sniper_Range.asset      |  Sniper 강화 3종
    ├── WeaponUpgrade_Sniper_Cooldown.asset  /
    ├── WeaponUpgrade_Bomb_Damage.asset      \
    ├── WeaponUpgrade_Bomb_Radius.asset       |  Bomb 강화 3종
    ├── WeaponUpgrade_Bomb_Cooldown.asset    /
    ├── WeaponUpgrade_Laser_Damage.asset     \
    ├── WeaponUpgrade_Laser_Range.asset       |  Laser 강화 3종
    ├── WeaponUpgrade_Laser_Cooldown.asset   /
    ├── WeaponUpgrade_Saw_Damage.asset       \
    ├── WeaponUpgrade_Saw_Radius.asset        |  Saw 강화 3종
    └── WeaponUpgrade_Saw_Slow.asset         /
```

**튜닝하려면?** Unity 에디터에서 SO 하나하나 더블클릭 → Inspector 에서 값 수정 → `Ctrl+S`. 20개 파일을 일일이 열어야 함.

**시트 적용 후?** 구글 시트 탭에서 셀 수정 → "Import All Data" 버튼 → 끝.

---

## 2. 시트 마이그레이션의 어려운 점

### 2-1. 무기 SO 는 5개가 **각자 다른 클래스**

`WeaponData` 라는 추상 베이스 클래스를 모든 무기가 상속하지만, 무기별로 **고유 필드** 가 있음:

```
WeaponData (베이스, 모든 무기 공통)
├── WeaponId, DisplayName, ThemeColor
├── UnlockedByDefault, UnlockGemCost, RequiredWeapon
├── FireDelay, Damage, HitVfxLifetime
└── Icon (sprite), HitVfxPrefab     ← 시트로 못 옮김 (Unity 에셋 참조)

   ↓ 상속

SniperWeaponData          BombData                    MachineGunData              LaserWeaponData            SawWeaponData
├── UseAimRadius          ├── ExplosionRadius         ├── MaxAmmo                 ├── Cooldown               ├── OrbitRadius
└── CustomRange           ├── Instant                 ├── ReloadDuration          ├── BeamDuration           ├── BladeRadius
                          ├── ProjectileSpeed/Life    ├── LowAmmoThreshold        ├── BeamSpeed              ├── SpinSpeed
                          ├── ProjectilePrefab        ├── BulletSpeed/Life        ├── StopDistance           ├── DamageTickInterval
                          ├── ExplosionVfxPrefab      ├── BulletHitRadius         ├── BeamRadius             ├── SlowFactor
                          └── LandingMarkerPrefab     ├── SpreadAngle             ├── TickInterval           ├── SlowDuration
                                                      └── BulletPrefab            └── BeamPrefab/...         └── BladeVisualPrefab
```

→ **무기별로 시트 컬럼이 다 다름.** 한 시트로 합치려면 **빈 칸이 많은 wide 시트** 가 됨.

### 2-2. 무기 강화 SO 15개는 **전부 같은 클래스**

`WeaponUpgradeData` 단일 클래스. **균일한 14컬럼 시트로 깔끔하게 표현 가능.**

---

## 3. 옵션 비교

세 옵션을 실제 값 채운 미리보기로 비교합니다.

### 옵션 A — 무기 + 강화 둘 다 시트

#### A-1. `WeaponData` 시트 (5행 × ~22열, sparse)

> 무기별 고유 컬럼은 해당 무기 행만 채우고 나머지는 빈 칸. (실제 현재 값 기준)

| WeaponId | DisplayName | ThemeColorHex | UnlockedByDefault | UnlockGemCost | RequiredWeaponId | FireDelay | Damage | HitVfxLifetime | Sniper_CustomRange | Bomb_ExplosionRadius | Gun_MaxAmmo | Gun_ReloadDuration | Gun_SpreadAngle | Laser_Cooldown | Laser_BeamDuration | Laser_BeamRadius | Laser_TickInterval | Saw_OrbitRadius | Saw_BladeRadius | Saw_SpinSpeed | Saw_SlowFactor | Saw_SlowDuration |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| sniper | 저격총 | #E040FA | TRUE | 0 | | 0.5 | 1 | 1.5 | **0.4** | | | | | | | | | | | | | |
| bomb | 폭탄 | #F4A422 | FALSE | ? | sniper | 5 | 3 | 1.5 | | **3** | | | | | | | | | | | | |
| gun | 기관총 | #4CC3F7 | FALSE | ? | sniper | 0.14 | 0.5 | 1.5 | | | **40** | **5** | **0.06** | | | | | | | | | |
| laser | 레이저 | #FF1744 | FALSE | ? | gun | 0 | 0.8 | 0.5 | | | | | | **5** | **10** | **1** | **0.1** | | | | | |
| saw | 회전톱날 | #E040FA | FALSE | 40 | gun | 0 | 0.15 | 1.5 | | | | | | | | | | **7.2** | **1.8** | **4.8** | **0.3** | **2** |

✅ **단일 시트, 단일 진실 소스**
❌ **빈 칸이 많아 보기 불편**, 컬럼 22개라 가로 스크롤 필요

#### A-2. `WeaponUpgradeData` 시트 (15행 × 14열, 균일)

> 모든 행이 동일 스키마. 깔끔.

| UpgradeId | WeaponId | DisplayName | TargetStat | MaxLevel | ValuePerLevel | IsPercentage | Operation | BaseCostOre | BaseCostGem | OreCostMultiplier | GemCostMultiplier | ManualCostsOre | ManualCostsGem |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| gun_dmg | gun | 기관총 데미지 | Damage | 5 | 0.25 | TRUE | Multiply | 70 | 5 | 2 | 2 | | |
| gun_ammo | gun | 탄창 확장 | AmmoBonus | 5 | 10 | FALSE | Add | 50 | 3 | 2 | 2 | | |
| gun_reload | gun | 재장전 단축 | ReloadTime | 5 | -0.10 | TRUE | Multiply | 60 | 4 | 2 | 2 | | |
| sniper_dmg | sniper | 저격총 데미지 | Damage | 5 | 0.30 | TRUE | Multiply | 80 | 5 | 2 | 2 | | |
| sniper_range | sniper | 사거리 확장 | Range | 5 | 0.20 | TRUE | Multiply | 60 | 4 | 2 | 2 | | |
| sniper_cd | sniper | 발사 속도 | Cooldown | 5 | -0.15 | TRUE | Multiply | 70 | 4 | 2 | 2 | | |
| bomb_dmg | bomb | 폭탄 데미지 | Damage | 5 | 0.30 | TRUE | Multiply | 80 | 5 | 2 | 2 | | |
| bomb_radius | bomb | 폭발 반경 | Radius | 5 | 0.20 | TRUE | Multiply | 70 | 4 | 2 | 2 | | |
| bomb_cd | bomb | 쿨다운 단축 | Cooldown | 5 | -0.15 | TRUE | Multiply | 60 | 4 | 2 | 2 | | |
| ... | ... | ... | ... | ... | ... | ... | ... | ... | ... | ... | ... | ... | ... |

> **수동 비용 예시** — `ManualCostsOre = "40\|90\|180\|360\|720"`, `ManualCostsGem = "2\|5\|10\|18\|30"` 이면 5 레벨 비용 명시. 빈 칸이면 BaseCost × Multiplier^level 공식 사용.

✅ **15행 × 14열, 균일, 정렬 깔끔**
✅ **튜닝 효용이 가장 큼** (수치 종류·다양성이 무기 본체보다 훨씬 큼)

---

### 옵션 B — 무기는 시트당 1개씩 (5+1 = 6 시트)

```
WeaponData_Common  ← 5행 (공통 필드만)
WeaponData_Sniper  ← 1행 (Sniper 고유)
WeaponData_Bomb    ← 1행
WeaponData_Gun     ← 1행
WeaponData_Laser   ← 1행
WeaponData_Saw     ← 1행
WeaponUpgradeData  ← 15행
```

✅ **각 시트가 컴팩트** (무기 5종이라도 각 시트는 고유 컬럼만)
❌ **시트 7개로 폭증**, 1행짜리 시트 5개 — 오버엔지니어링

---

### 옵션 C — 강화만 시트, 무기는 Unity 그대로

```
WeaponUpgradeData  ← 15행 (가장 가치 큼)
```
무기 본체 5종은 자주 안 만지므로 Unity 에서 직편집.

✅ **가장 단순** — 시트 1개만 추가, 임포터 1개 함수만 작성
✅ **튜닝 효용의 90%** 를 가져감 (강화 = 게임 진행 곡선의 핵심)
❌ 무기 자체의 `FireDelay`/`Damage`/`UnlockGemCost` 변경은 여전히 Unity 에서

---

## 4. 워크플로우 비교 — "기관총 데미지 강화 비용 너무 비싸. 70→50 으로"

### 시트 적용 전 (현재)
1. Unity 켠다
2. `Assets/_Game/Data/WeaponUpgrades/WeaponUpgrade_Gun_Damage.asset` 더블클릭
3. Inspector 에서 `_baseCostOre` 70 → 50 수정
4. Ctrl+S → git commit → push
5. 다른 사람이 pull 해야 적용

### 시트 적용 후 (옵션 A·C 둘 다)
1. 구글 시트 → `WeaponUpgradeData` 탭 → `gun_dmg` 행 → `BaseCostOre` 셀 → `50`
2. Unity 메뉴 "Drill-Corp / Import All Data" 클릭
3. SO 자동 갱신 + git commit (asset 변경)

→ **Unity 안 켜고 기획자도 수정 가능**, **여러 항목 동시 비교** 가능.

---

## 5. 추천

### 추천 순위
1. **옵션 C (강화만)** — 가장 가성비. 무기 5종은 Unity 에서 충분히 관리됨.
2. **옵션 A (무기+강화)** — 완전성 원하면. wide 시트 단점 감수.
3. ~~옵션 B~~ — 시트 분할 비추.

### 권장 진행 (옵션 C 가정)

```
Phase 1. (제가) docs/_review/WeaponUpgradeData.csv 작성 — 현재 15개 SO 값 dump
Phase 2. (제가) GoogleSheetsImporter 에 ImportWeaponUpgradeDataAsync() 추가
              - UpgradeId 필드 기반 lookup (UpgradeData 임포터와 동일 패턴)
              - TargetStat / Operation enum 파싱
              - ManualCostsOre/Gem 파이프 배열 → List<WeaponUpgradeCostTuple> 주입
Phase 3. (사용자) 구글 시트에 "WeaponUpgradeData" 탭 만들고 CSV 붙여넣기
Phase 4. (사용자) "Import All Data" 실행 → Title 씬에서 무기 강화 패널 동작 확인
Phase 5. (선택, 나중에) 무기 본체도 시트화하고 싶으면 옵션 A 의 WeaponData 시트 추가
```

---

## 6. 시각적 요약

```
            ┌─────────────────────┐
            │  Google Sheets      │
            │  (기획자/디자이너)   │
            └──────────┬──────────┘
                       │ 셀 수정
                       ▼
   ┌──────────────────────────────────────┐
   │  Sheets API (서비스 계정)             │
   └────────────────────┬─────────────────┘
                        │ Import All Data 클릭
                        ▼
   ┌──────────────────────────────────────┐
   │  GoogleSheetsImporter (Unity Editor) │
   │  ─────────────────────────────────── │
   │  • SimpleBugData     ← 기존          │
   │  • WaveData          ← 기존          │
   │  • MachineData       ← 기존          │
   │  • UpgradeData       ← 기존          │
   │  • WeaponUpgradeData ← 추가 ⭐       │
   │  • WeaponData        ← (옵션 A 시)    │
   └────────────────────┬─────────────────┘
                        │ SO 값 update-in-place
                        ▼
   ┌──────────────────────────────────────┐
   │  Assets/_Game/Data/*.asset           │
   │  (Title.unity 가 GUID 로 참조)        │
   └──────────────────────────────────────┘
                        │ Play
                        ▼
   ┌──────────────────────────────────────┐
   │  WeaponUpgradeManager 가 SO 읽어 적용 │
   └──────────────────────────────────────┘
```

**선택 부탁:** 옵션 A / B / C 중 어느 쪽으로 진행할지 알려주세요.

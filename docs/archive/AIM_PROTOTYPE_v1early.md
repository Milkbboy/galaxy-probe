# 에임 & 무기 시스템 — 프로토타입 분석

> 출처: `docs/_.html` (굴착기 디펜스 웹 프로토타입)
> 목적: Unity Drill-Corp 프로젝트에 이식하기 위한 에임/무기 사양 정리
> 선행 문서: `docs/SPAWN_PROTOTYPE.md`

---

## 1. 에임의 역할 — "무기 컨트롤 허브"

프로토타입의 에임은 단순 조준점이 아니다. **4종 무기가 전부 마우스 좌표를 중심으로 동작**하며, 플레이어는 **에임 이동 + 클릭(폭탄 전용)만** 수행한다.

| 무기 | 에임 좌표 역할 | 입력 | 발사 방식 |
|---|---|---|---|
| 저격총 | 타격 중심 (범위 원 내 자동 피격) | 자동 | 쿨다운 + 타겟 존재 시 |
| 폭탄 | 착탄 지점 | **클릭** | 수동 |
| 기관총 | 발사 방향 계산 (중앙→에임) | 자동 | 쿨다운 기반 연사 |
| 레이저 | 빔 스폰 + 추적 목표 | 자동 | 쿨다운 완료 시 자동 |

---

## 2. 마우스 → 월드 좌표 변환

### 2-1. 프로토타입 코드 (L162-164)
```js
let mouseX = CX, mouseY = CY;  // 초기값: 캔버스 중앙

function getPos(e){
  const r = canvas.getBoundingClientRect();
  return {
    x: (e.clientX - r.left) * (W / r.width),
    y: (e.clientY - r.top) * (H / r.height)
  };
}
canvas.addEventListener('mousemove', e => {
  const p = getPos(e);
  mouseX = p.x; mouseY = p.y;
});
```

**핵심 포인트**
- CSS 리사이즈 무시하고 **내부 해상도(700×560)로 스케일 변환** → 캔버스가 작아져도 조준 정확도 유지
- `cursor:none` (L53) — 기본 커서 숨김, 크로스헤어만 그림

### 2-2. Unity 매핑 (XZ 평면)
```csharp
Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
if (groundPlane.Raycast(ray, out float enter)) {
    Vector3 aimPos = ray.GetPoint(enter);  // XZ 평면 교점
    aimPos.y = 0f;
}
```

---

## 3. 각 무기의 에임 사용 방식

### 3-1. 저격총 — 범위 원 내 일제 타격 (L265)

```js
function fireSniper(){
  let fired = false;
  for (let i = bugs.length - 1; i >= 0; i--) {
    const g = bugs[i];
    if (Math.hypot(g.x - mouseX, g.y - mouseY) < ws.sniper.range + g.sz) {
      g.hp -= ws.sniper.dmg;
      if (g.hp <= 0) killBug(i);
      fired = true;
    }
  }
  return fired;
}
```

**특성**
- 조준원 안의 **모든 벌레 동시 피격** (AoE 관통형)
- 쿨다운은 "타겟 있을 때만" 소비 (L290: `if(f) sniperCD = ws.sniper.cd`)
- 타겟 없으면 쿨다운 안 돌아감 → 낭비 방지

**Unity 매핑**
```csharp
var hits = Physics.OverlapSphere(aimPos, ws.sniper.range, bugLayer);
foreach (var col in hits) {
    if (col.TryGetComponent<IDamageable>(out var dmg))
        dmg.TakeDamage(ws.sniper.dmg);
}
```

### 3-2. 폭탄 — 중앙에서 에임으로 투사체 (L266)

```js
function fireBomb(){
  if (bombCD > 0 || !unlocked.bomb) return;
  const a = Math.atan2(mouseY - CY, mouseX - CX);
  bombs.push({
    x: CX, y: CY,
    vx: Math.cos(a) * ws.bomb.speed,
    vy: Math.sin(a) * ws.bomb.speed,
    tx: mouseX, ty: mouseY,  // 목표 지점 기억
    life: 220, trail: []
  });
  bombCD = ws.bomb.cd;
}
```

**특성**
- 굴착기 중심 → 에임 방향으로 발사
- 이동 중 **목표점 도달 or 수명 끝**에서 폭발 (L295)
- 폭발 시 반경 내 모든 벌레 피격 (`explode()` L267)
- **유일한 수동 입력 무기** (마우스 클릭)

**Unity 매핑**
```csharp
canvas.addEventListener('click', () => fireBomb());
// →
void OnFirePressed() {
    Vector3 dir = (aimPos - machinePos).normalized;
    dir.y = 0;
    var bomb = Instantiate(bombPrefab, machinePos, Quaternion.identity);
    bomb.GetComponent<Rigidbody>().linearVelocity = dir * ws.bomb.speed;
}
```

### 3-3. 기관총 — 산포 발사 (L268)

```js
function fireGun(){
  if (!unlocked.gun || gunReloadCD > 0 || gunAmmo <= 0) return;
  const a = Math.atan2(mouseY - CY, mouseX - CX) + (Math.random() - 0.5) * 0.12;
  bullets.push({
    x: CX, y: CY,
    vx: Math.cos(a) * ws.gun.speed,
    vy: Math.sin(a) * ws.gun.speed,
    life: 85
  });
  gunAmmo--;
  if (gunAmmo <= 0) gunReloadCD = ws.gun.reload;
}
```

**특성**
- 에임 방향 기준 **±0.06 rad (약 ±3.4°) 랜덤 오프셋** → 탄착 벌어짐
- 탄창 소진 시 자동 리로딩 (L292)
- 탄환은 투사체로 날아가 벌레와 충돌 판정

**Unity 매핑**
```csharp
float baseAngle = Mathf.Atan2(aimPos.z - machinePos.z, aimPos.x - machinePos.x);
float spread = Random.Range(-0.06f, 0.06f);
Vector3 dir = new Vector3(Mathf.Cos(baseAngle + spread), 0, Mathf.Sin(baseAngle + spread));
```

### 3-4. 레이저 — 빔 스폰 + 느린 추적 (L269, L294)

```js
// 스폰: 에임 위치에 빔 생성
function fireLaser(){
  if (!unlocked.laser) return;
  laserBeams.push({
    x: mouseX, y: mouseY,
    r: ws.laser.range, life: ws.laser.dur,
    maxLife: ws.laser.dur, dmgTick: 0
  });
  laserCD = ws.laser.cd;
}

// 매 프레임: 빔이 에임 쪽으로 천천히 이동 (L294)
const dx = mouseX - lb.x, dy = mouseY - lb.y, d = Math.hypot(dx, dy);
if (d > 2) {
  lb.x += dx/d * ws.laser.speed * dt;  // 속도 1.725 → 느림
  lb.y += dy/d * ws.laser.speed * dt;
}
```

**특성**
- 빔이 마우스를 **추격**하되 속도가 느려 유저가 끌고 다니는 느낌
- 체류 중인 빔 안 벌레는 **6프레임마다 지속 피해** (`dmgTick`)
- 빔 수명(`dur`) 끝나면 자동 소멸

**Unity 매핑**
```csharp
// Update
Vector3 toAim = aimPos - beam.position;
if (toAim.magnitude > 0.03f) {
    beam.position += toAim.normalized * ws.laser.speed * Time.deltaTime;
}
```

---

## 4. 굴착기 포탑 회전 (L308)

```js
const ba = Math.atan2(mouseY - CY, mouseX - CX);
ctx.rotate(ba);
// 무기 배럴 3종을 회전된 로컬좌표에서 그림
```

각 무기 배럴이 **에임 방향을 향해 회전**:
- 저격 배럴 (보라) — 항상 표시
- 기관총 배럴 (하늘) — `unlocked.gun` 시
- 레이저 배럴 (빨강) — `unlocked.laser` 시

**Unity 매핑**
```csharp
Vector3 toAim = aimPos - turret.position;
toAim.y = 0;
if (toAim.sqrMagnitude > 0.001f)
    turret.rotation = Quaternion.LookRotation(toAim);
```

---

## 5. 크로스헤어 HUD — 동심원 레이어 (L313)

에임 좌표 `(x, y)` 주변에 **4겹 동심원**으로 전 무기 상태를 한눈에 표시:

```
          ┌──── R+27: 레이저 쿨다운 호 ────┐
          │  ┌── R+20: 기관총 탄창 호 ──┐  │
          │  │  ┌── R+13: 폭탄 쿨다운 ──┐│  │
          │  │  │  ┌── R+6: 저격 쿨 ──┐ ││  │
          │  │  │  │  ● R: 저격 범위원 │ ││  │
          │  │  │  │  + 십자선(gap 7, len 11)
          │  │  │  └──────────────────┘ ││  │
          │  │  └───────────────────────┘│  │
          │  └────────────────────────────┘  │
          └──────────────────────────────────┘
```

### 5-1. 레이어별 상세

| 반경 | 요소 | 표시 방식 | 색상 |
|---|---|---|---|
| `R` | 저격 사거리 원 | 항상 표시 | 타겟 있음 보라 / 없음 시안 |
| `R+6` | 저격 쿨다운 호 | 12시부터 시계방향 채움 | 보라 `#e040fb` |
| `R+13` | 폭탄 쿨다운 호 | 동일 | 주황 `#ff9632` |
| `R+20` | 기관총 탄창/리로딩 호 | 탄 수 비례 | 리로딩 시 빨강, 아니면 하늘 |
| `R+27` | 레이저 쿨다운 호 | 동일 | 빨강 `#ff1744` |

### 5-2. 중앙 십자선
- 4방향 막대 (gap 7px, len 11px)
- 중앙 점 2px
- **타겟 유무로 색 전환**: 시안 `#00e5ff` ↔ 보라 `#e040fb`

### 5-3. 입력 프롬프트
- 폭탄 준비 시 십자선 하단에 **"클릭→폭탄"** 텍스트 표시

### 5-4. Unity 구현 권장
- **World Space Canvas** (에임 위치 자식 UI)
- 각 호: `Image` (type: Filled, method: Radial360)
- `fillAmount = 1 - (cooldown / maxCooldown)`

---

## 6. 전체 발사 루프 (update 함수, L283-302)

```js
function update(dt) {
  // 1. 저격: 쿨 감소 + 타겟 있을 때만 발사
  if (sniperCD > 0) sniperCD -= dt;
  if (sniperCD <= 0) {
    const fired = fireSniper();
    if (fired) sniperCD = ws.sniper.cd;
  }

  // 2. 폭탄: 쿨 감소만 (발사는 click 이벤트)
  if (unlocked.bomb && bombCD > 0) bombCD -= dt;

  // 3. 기관총: 리로딩 우선, 아니면 자동 연사
  if (unlocked.gun) {
    if (gunReloadCD > 0) {
      gunReloadCD -= dt;
      if (gunReloadCD <= 0) { gunAmmo = ws.gun.maxAmmo; }
    } else {
      if (gunFireCD > 0) gunFireCD -= dt;
      if (gunFireCD <= 0) { fireGun(); gunFireCD = ws.gun.fireCD; }
    }
  }

  // 4. 레이저: 빔 없고 쿨 끝나면 스폰
  if (unlocked.laser && laserBeams.length === 0) {
    if (laserCD > 0) laserCD -= dt;
    if (laserCD <= 0) fireLaser();
  }

  // 5. 활성 빔 추적 + 지속 피해
  for (const lb of laserBeams) {
    /* aimPos 추적 이동 + dmgTick 마다 OverlapSphere */
  }

  // 6. 투사체 (bomb, bullet) 이동/충돌
  // 7. 폭발, 파티클 갱신
}
```

---

## 7. 프로토타입 무기 기본 스탯 (L166)

```js
const BASE = {
  sniper: { cd: 24,  dmg: 1,   range: 24 },
  bomb:   { cd: 360, radius: 110, speed: 5, dmg: 3 },
  gun:    { fireCD: 8.4, maxAmmo: 40, reload: 300, dmg: 0.5, speed: 9 },
  laser:  { cd: 300, dur: 360, speed: 1.725, range: 28.8, dmg: 0.8 }
};
```

**프레임 → 초 환산 (60fps)**
| 무기 | 쿨다운 | 비고 |
|---|---|---|
| sniper | 0.4s | 연사 빠름 |
| bomb | 6s | 수동 발사 |
| gun | 0.14s (fireCD) | 리로딩 5s |
| laser | 5s | 빔 수명 6s |

---

## 8. 이 에임 구조의 강점

1. **학습 불필요** — 에임 주변만 봐도 "무기가 뭐 있고, 언제 쓸 수 있고, 얼마 남았는지" 전부 보임
2. **조작 최소화** — 마우스 이동 + 클릭 1개만. WASD/단축키 없음
3. **자동과 수동 공존** — 저격/기관총/레이저는 자동, 폭탄만 수동
4. **사거리 시각화** — 저격 원이 "들어오면 죽는 범위"를 명확히 전달
5. **무기 선택 없음** — 4종 동시 가동 (로그라이트 빌드업 감성)

---

## 9. Unity 이식 컴포넌트 구조

### 9-1. 핵심 컴포넌트
| 컴포넌트 | 역할 | 의존 |
|---|---|---|
| `AimController` | 마우스 → 월드좌표, `AimPos` 프로퍼티 | New Input System |
| `CrosshairHUD` | 에임 따라가는 World UI, 동심원 레이어 | AimController |
| `TurretController` | 배럴 Transform을 aimPos 향해 회전 | AimController |
| `WeaponBase` (추상) | `Tick(dt)`, `TryFire()`, `CurrentCooldown` | — |
| `SniperWeapon : WeaponBase` | OverlapSphere 타격 | AimController |
| `BombWeapon : WeaponBase` | 클릭 입력 수신, 투사체 생성 | AimController, Input |
| `GunWeapon : WeaponBase` | 자동 발사 + 탄창 | AimController |
| `LaserWeapon : WeaponBase` | 빔 GameObject 생성 + 추적 | AimController |
| `WeaponHUDBinding` | 각 무기 상태를 호 Image에 바인딩 | CrosshairHUD, Weapons |

### 9-2. SO 데이터 (각 무기별)
```csharp
[CreateAssetMenu(menuName = "Drill-Corp/Weapon Data")]
public class WeaponData : ScriptableObject {
    public string DisplayName;
    public Color ThemeColor;
    public Sprite Icon;
    public float Cooldown;
    public float Damage;
    public float Range;
    // 무기별 추가 필드는 상속 SO로 분리
}
```

### 9-3. 씬 배치
```
[Scene]
├── AimController (빈 GameObject)
├── Turret (굴착기 자식)
│   ├── SniperBarrel
│   ├── GunBarrel
│   └── LaserBarrel
├── WeaponSystem
│   ├── SniperWeapon
│   ├── BombWeapon
│   ├── GunWeapon
│   └── LaserWeapon
└── UI
    └── CrosshairHUD (World Space Canvas)
        ├── RangeCircle
        ├── SniperArc
        ├── BombArc
        ├── GunArc
        ├── LaserArc
        └── Crosshair+
```

---

## 10. 무기 UI 표시 시스템 (상시 UI)

무기 UI는 **3개 레이어**로 구성된다:
1. **좌측 무기 패널** — 상시 표시, 4개 슬롯
2. **크로스헤어 동심원** — 에임 주변 실시간 상태 (섹션 5 참조)
3. **오버레이 카드** — 해금/성장 선택 화면 (섹션 12)

### 10-1. 좌측 무기 패널 DOM 구조 (L94-100)
```
#weaponPanel (88px 고정 너비)
├── .wp-title "무기"
├── .weapon-slot #wSlot0 (저격총 — 항상 활성)
├── .weapon-slot.locked #wSlot1 (폭탄)
├── .weapon-slot.locked #wSlot2 (기관총 + ammo-row)
└── .weapon-slot.locked #wSlot3 (레이저)
```

### 10-2. 슬롯 내부 구성 (세로 배치)
```
┌─────────────┐
│  [Icon 32x32]  │ ← canvas (절차 생성)
│   저격총     │ ← .w-name (10px)
│   Lv.1      │ ← .w-lvl (9px)
│   발사!     │ ← .w-cool (10px, 상태 텍스트)
│  ▓▓▓▓░░░░   │ ← .cool-bar-bg + .cool-bar (3px 높이)
│  ●●●●○○○    │ ← .ammo-row (기관총만)
│   [overlay] │ ← .cool-overlay (쿨 중 검은 덮개)
└─────────────┘
```

### 10-3. 잠김 상태 처리
- `.weapon-slot.locked` → `opacity: 0.35`
- 레벨 → "잠김", 쿨 텍스트 → "-"
- 쿨바 → 회색 (`locked-bar`)

### 10-4. 쿨다운 바 색상 규칙 (CSS L45-47)
| 클래스 | 색 | 의미 |
|---|---|---|
| `.ready` | `#51cf66` 초록 | 모두 준비 완료 |
| `.sniper` | `#e040fb` 보라 | 저격 쿨다운 중 |
| `.bomb` | `#f4a423` 주황 | 폭탄 쿨다운 중 |
| `.gun` | `#4fc3f7` 하늘 | 기관총 탄창 남음 |
| `.reload` | `#ff6b6b` 빨강 | 기관총 리로딩 |
| `.laser` | `#ff1744` 진빨강 | 레이저 쿨다운 |
| `.laser-active` | `#ff6090` 핑크 | 레이저 활성 중 |
| `.locked-bar` | `#333` 회색 | 잠김 |

---

## 11. 무기별 슬롯 업데이트 로직 (L270-282, `updateWeaponUI`)

매 프레임 호출되며 무기별로 다른 규칙 적용한다.

### 11-1. 저격총 (L272-275)
```js
if (!hasTarget)    { 바="ready" 100%, cool="대기",  테두리=기본 }
else if (cd<=0)    { 바="ready" 100%, cool="발사!", 테두리=보라 }
else               { 바="sniper" 진행%, cool="1.2s", 테두리=기본 }
```
**핵심**: 타겟 유무로 "대기" vs "발사!" 분기 → 플레이어가 범위 진입 즉시 피드백

### 11-2. 폭탄 (L276-277)
```js
if (cd<=0) { 바=100%, cool="[클릭]", 테두리=초록, 오버레이 숨김 }
else       { 바=진행%, cool="3.5s", 테두리=기본, 오버레이 표시 }
```
**"[클릭]"** 텍스트로 수동 입력 프롬프트 역할

### 11-3. 기관총 (L278-279)
```js
if (reloadCD>0) { 바="reload" 진행%, cool="리로딩", 오버레이="리로딩 X.Xs", 테두리=빨강 }
else            { 바="gun" ammo/max%, cool="32발", 테두리=(탄>8?하늘:빨강) }
```
- 탄 8발 이하 → 테두리 빨강으로 **탄 부족 경고**
- 탄알 점(pip)도 함께 갱신: `ammo-pip.empty` 토글

### 11-4. 레이저 (L280-281)
```js
if (활성중)     { 바="laser-active" 수명%, cool="5.2s", 테두리=빨강 }
else if (cd>0)  { 바="laser" 진행%, cool="3.0s", 오버레이 표시 }
else            { 바="ready" 100%, cool="자동발사", 테두리=빨강 }
```
3단계 상태 각각 다른 색

### 11-5. 쿨다운 오버레이 (L51-52)
```css
.cool-overlay {
  position: absolute; inset: 0;
  background: rgba(0,0,0,0.45);  ← 45% 검은 반투명
  font-size: 11px;
  color: rgba(255,255,255,0.9);
}
```
- 쿨 중일 때만 표시 → 남은 시간을 **큰 글씨로 강조**
- 폭탄: `3.5s` 한 줄
- 기관총 리로딩: `리로딩` (ov-label) + `2.1s` 두 줄

### 11-6. 무기 아이콘 (L135-144)
**런타임에 canvas로 절차 생성** — 이미지 파일 없음.
Unity에선 **Sprite 에셋 4개** (`Icon_Sniper/Bomb/Gun/Laser.png`)로 대체 권장.

---

## 12. 오버레이 카드 (해금/성장)

### 12-1. 공통 구조 (L55-65)
```css
#overlay {
  position: absolute; inset: 0;
  background: rgba(10,10,30,0.92);  ← 92% 어두운 오버레이
  z-index: 10;
}
.upg-card {
  width: 128px;
  background: rgba(255,255,255,0.06);
  border: 1.5px solid rgba(255,255,255,0.18);
  cursor: pointer;
}
```
- **게임 전체 pause** → `state='pause'` (update 루프 차단)

### 12-2. 해금 오버레이 (L216-232, `showWeaponUnlock`)

10/30/50 처치 시 발동, 잠긴 무기 전체를 카드로 표시:
```
┌────── 무기 해금! ──────┐
│  추가할 무기를 선택하세요  │
│  ┌────┐ ┌────┐ ┌────┐ │
│  │[🎯]│ │[💣]│ │[⚡]│ │
│  │폭탄│ │기관│ │레이│ │
│  │설명│ │설명│ │설명│ │
│  └────┘ └────┘ └────┘ │
└───────────────────────┘
```

카드 내용 (`DESCS` 딕셔너리, L221):
- 폭탄: "폭발 범위 데미지 / 6초 쿨다운"
- 기관총: "자동발사 40발 / 5초 리로딩"
- 레이저: "쿨다운 자동발사 / 마우스 추적 6초"

**호버 효과**: 테두리 `+'66'`(반투명) → 진한 색

### 12-3. 성장 오버레이 (L233-249, `showGrowthScreen`)

30마리마다 **14종 업그레이드 중 랜덤 3개** 제시:
```
┌────── 무기 성장! ──────┐
│ ┌────┐ ┌────┐ ┌────┐ │
│ │[🎯]│ │[💣]│ │[⚡]│ │
│ │저격│ │폭탄│ │레이│ │
│ │Lv.3│ │Lv.2│ │Lv.4│ │ ← 현재 레벨 추가
│ │범위│ │범위│ │속도│ │
│ │+20%│ │+20%│ │+30%│ │ ← opt.label 한 줄
│ └────┘ └────┘ └────┘ │
└───────────────────────┘
```

### 12-4. 아이콘 재사용
두 오버레이 모두 **좌측 패널의 아이콘 canvas를 drawImage로 복사** (L224, L240):
```js
ic.getContext('2d').drawImage(
  document.getElementById('wIcon'+idx), 0,0,32,32, 4,4,32,32
);
```

---

## 13. 주변 UI 요소

### 13-1. 상단 스탯바 (L73-78)
```
┌─────────────────────────────────────────┐
│ 체력 100   웨이브 1   처치 0   [▶ 시작] │
└─────────────────────────────────────────┘
```

### 13-2. 캔버스 내부 진행도 바 (L322-326)
화면 하단 중앙 175×8px:
- **해금 전**: "1차 해금까지 X마리" (노란색)
- **해금 완료**: "성장까지 X마리" (초록색)

### 13-3. 캔버스 내부 타이머 바 (L327-329)
- **우측 하단**: 엘리트 등장까지 `★ Xs` (노랑)
- **좌측 하단**: 땅굴 이벤트까지 `◈ Xs` (보라, 30초 후 활성)

### 13-4. 경고 배너 (L306, `drawWarnings`)
땅굴 이벤트 시 상단 중앙:
- 반투명 보라 배경 + fade-in/out
- "◈ 땅굴 침공!" + 서브텍스트
- **방향 화살표** (땅굴이 화면 밖이면 중심에서 60px 거리)

### 13-5. 게임오버 (L110, L303)
```html
<div id="goPanel">
  <h3>게임 오버</h3>
  <p>웨이브 5 도달 · 처치 42마리</p>
  <button>↺ 다시 시작</button>
</div>
```

---

## 14. 좌표 스케일 기준 (Unity 환산)

프로토타입 px → Unity 유닛 변환 (`SPAWN_PROTOTYPE.md`와 동일 기준):

| 프로토타입 값 | Unity 유닛 | 비고 |
|---|---|---|
| sniper range 24 | 약 0.4 | 스폰 margin과 동일 스케일 |
| bomb radius 110 | 약 1.8 | 폭발 범위 |
| laser range 28.8 | 약 0.48 | 빔 반경 |
| 화면 중심 → 벌레 접촉 44 | 0.73 | `_contactRange`로 반영됨 |

**주의**: 실제 튜닝은 Unity에서 플레이하며 조정. 이 값들은 **시작점**일 뿐.

---

## 15. 관련 문서

- **구현 로드맵**: `docs/WEAPON_IMPLEMENTATION_PLAN.md` — 수직 슬라이스 4단계 작업 계획
- **벌레 스폰**: `docs/SPAWN_PROTOTYPE.md` — 적 스폰 시스템 (이미 구현됨)

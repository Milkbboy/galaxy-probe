# Drill-Corp 자원 아이콘 v2 (로우폴리 + 글로시)

**배경 없는 투명 PNG**로 제작된 자원 아이콘 12종 (64/128/256/**512**px)

## v1과의 차이

| 항목 | v1 (drillcorp_icons) | v2 (이번 팩) |
|------|----|----|
| 스타일 | 픽셀아트 (도트) | 로우폴리 + 글로시 |
| 배경 | 둥근 사각형 프레임 | **투명** |
| 해상도 | 64/128/256 | 64/128/**256/512** |
| 용도 | UI 슬롯/카드 | 인게임 드랍 + UI 양쪽 |

## 구성

### 💎 보석 4종 (글로시 스타일)
| 파일 | 이름 | 스타일 특징 |
|------|------|--------|
| `01_diamond.png` | 다이아몬드 | 브릴리언트 컷 + 흰 하이라이트 + 파란 글로우 |
| `02_ruby.png` | 루비 | 원형 커팅 + 진홍 그라디언트 + 분홍 글로우 |
| `03_emerald.png` | 에메랄드 | 에메랄드 컷 (사각 스텝) + 녹색 글로우 |
| `04_sapphire.png` | 사파이어 | 팔각 커팅 + 깊은 파랑 + 블루 글로우 |

### ⛏️ 광석 4종 (로우폴리 스타일)
| 파일 | 이름 | 스타일 특징 |
|------|------|--------|
| `05_iron.png` | 철광석 | 회색 바위 + 주황 광맥 |
| `06_gold.png` | 금광석 | 어두운 갈색 바위 + 크고 밝은 금 광맥 |
| `07_copper.png` | 구리광석 | 붉은 갈색 바위 + 선명한 오렌지 구리맥 |
| `08_crystal.png` | 크리스탈 | 3개 크리스탈 클러스터 + 보라 글로우 |

### ⚡ 특수 자원 4종 (혼합)
| 파일 | 이름 | 스타일 특징 |
|------|------|--------|
| `09_fuel.png` | 연료 | 실린더 탱크 + 청록 액체 + 버블 + 글로우 |
| `10_core.png` | 에너지 코어 | 육각 하우징 + 빨간 구 + 중심 화이트핫 |
| `11_circuit.png` | 회로판 | 녹색 PCB + 금색 트레이스 + 빨강/파랑 LED |
| `12_exotic.png` | 우주 금속 | 3D 잉곳 + 홀로그래픽 3색 스트라이프 |

## Unity 임포트 설정

### 기본 (권장)
```
Texture Type       : Sprite (2D and UI)
Sprite Mode        : Single
Filter Mode        : Bilinear  ← v1과 달리 부드럽게!
Compression        : Normal Quality
Generate Mip Maps  : 체크 해제
Wrap Mode          : Clamp
Pixels Per Unit    : 100
```

**v1 팩과의 중요한 차이**: v1은 픽셀아트라 `Filter Mode: Point` 필수였지만, v2는 벡터 기반이라 **Bilinear**가 더 예뻐요.

### 인게임 드랍용 (월드 공간)
```
Pixels Per Unit    : 64
Mesh Type          : Tight  ← 투명 배경 최적화
```

## 스타일 가이드

### 자원 등급 표현 (레어도 확장 가능)
```csharp
// 광석 = 일반 등급 (로우폴리 거친 느낌)
// 보석 = 고급 등급 (글로시 + 글로우)
// 특수 자원 = 레어 등급 (복잡한 구조 + 이펙트)
// 홀로그래픽 잉곳 = 레전더리 (희귀 재료)
```

### 인게임 배치 팁
- **땅에 떨어진 보석/광석**: 투명 배경이라 바닥 텍스처 위에 자연스럽게 놓임
- **UI 슬롯 안에 넣을 때**: Unity Image 컴포넌트에서 Preserve Aspect 체크
- **반짝임 연출**: 기본 반짝이 외에 Unity에서 `DOTween`으로 Scale pulse 추가하면 더 살아있음

## ScriptableObject 연동

```csharp
[CreateAssetMenu(menuName = "Drill-Corp/Resource Data")]
public class ResourceData : ScriptableObject
{
    public string resourceId;       // "diamond", "iron" 등
    public string displayName;
    public Sprite icon;             // ← 512px 드래그 권장
    public ResourceType type;       // Gem / Ore / Special
    public RarityTier rarity;
    public int baseValue;
    public Color glowColor;         // 인게임 글로우 이펙트용
}

public enum ResourceType { Gem, Ore, Special }
public enum RarityTier { Common, Rare, Epic, Legendary }
```

## 권장 저장 경로

```
Assets/Art/Resources/
├── Gems/        (01~04)
├── Ores/        (05~08)
└── Special/     (09~12)
```

## 전체 Drill-Corp 아이콘 팩 현황

| 팩 | 종류 | 용도 |
|---|---|---|
| `drillcorp_icons` | 자원 12종 (픽셀아트) | v1 - UI 카드용 |
| `drillcorp_ui_icons` | UI 14종 | 재화/강화/무기/장비 |
| `drillcorp_emoji_icons` | 이모지 대체 12종 | 보석 시스템/캐릭터 스킬 |
| `drillcorp_game_assets` | 캐릭터 + 오브젝트 20종 | HTML 분석 기반 |
| `drillcorp_resources_v2` | **자원 12종 (로우폴리+글로시)** | **이번 팩 - 인게임용** |

## 라이선스

자유롭게 사용/수정 가능합니다.

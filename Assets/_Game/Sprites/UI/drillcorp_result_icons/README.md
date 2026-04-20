# Drill-Corp 결과 화면 아이콘

게임 결과 화면(#resultPanel)에서 사용할 아이콘 2종 (128/256/512px)

## 구성

| 파일 | 이름 | 용도 |
|------|------|------|
| `01_mining_success.png` | 채굴 완료 (성공) | "⛏ 채굴 완료!" 타이틀 옆 |
| `02_mining_failure.png` | 채굴 실패 | "💥 채굴 실패" 타이틀 옆 |

## 디자인 디테일

### 01 채굴 완료 (성공)
- **메인**: 황금 트로피 (로우폴리 컷)
- **중앙**: 은색 드릴 아이콘 (채굴 성공 상징)
- **우상단**: 초록색 체크 마크 배지
- **이펙트**: 황금 글로우, 별 반짝이 3개, 빛줄기 4방향
- **컬러**: `#ffd040` (황금) / `#40c068` (체크 배지)

### 02 채굴 실패
- **메인**: 부서진 굴착기 (-15도 기울어짐, 본체에 금 감)
- **상단**: 폭발/화염 + 연기 구름
- **우상단**: 빨간 X 마크 배지
- **이펙트**: 빨간 글로우, 균열 라인, 튀어나온 파편
- **컬러**: `#a06008` (갈색 굴착기) / `#e02040` (X 배지)

## Unity 사용 팁

### 기본 임포트 설정
```
Texture Type       : Sprite (2D and UI)
Sprite Mode        : Single
Filter Mode        : Bilinear
Compression        : Normal Quality
Generate Mip Maps  : 체크 해제
Pixels Per Unit    : 100
```

### 결과 화면 UI 배치
- **256px 또는 512px 권장** (결과 화면에서 크게 표시되므로)
- Canvas Image 컴포넌트 → Preserve Aspect 체크
- 권장 크기: 화면의 20~30% 차지
- 등장 애니메이션: `DOTween.DOScale(1f, 0.5f).SetEase(Ease.OutBack)` 같은 팝업 효과 추천

### 간단한 연출 예시
```csharp
public Image resultIcon;
public Sprite successSprite;
public Sprite failureSprite;

public void ShowResult(bool isSuccess)
{
    resultIcon.sprite = isSuccess ? successSprite : failureSprite;
    
    // 팝업 등장 효과
    resultIcon.transform.localScale = Vector3.zero;
    resultIcon.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
    
    // 성공시 살짝 회전 (반짝이는 느낌)
    if (isSuccess)
    {
        resultIcon.transform.DORotate(new Vector3(0, 0, 10f), 0.3f)
            .SetLoops(2, LoopType.Yoyo);
    }
    // 실패시 흔들림
    else
    {
        resultIcon.transform.DOShakePosition(0.5f, 10f);
    }
}
```

## HTML 버전 대체 가이드

현재 HTML의 `result-title`:
```html
<!-- 기존 이모지 방식 -->
<h2>⛏ 채굴 완료!</h2>
<h2>💥 채굴 실패</h2>

<!-- 아이콘 이미지로 교체 (Unity UI 기준) -->
[01_mining_success.png] 채굴 완료!
[02_mining_failure.png] 채굴 실패
```

## 권장 저장 경로

```
Assets/Art/UI/Result/
├── 01_mining_success.png
└── 02_mining_failure.png
```

## 전체 Drill-Corp 아이콘 팩 현황

| 팩 | 종류 | 용도 |
|---|---|---|
| `drillcorp_icons` | 자원 12종 v1 (픽셀아트) | UI 카드용 |
| `drillcorp_ui_icons` | UI 14종 | 재화/강화/무기/장비 |
| `drillcorp_emoji_icons` | 이모지 대체 12종 | 보석 시스템/캐릭터 스킬 |
| `drillcorp_game_assets` | 캐릭터 + 오브젝트 20종 | HTML 분석 기반 |
| `drillcorp_resources_v2` | 자원 12종 v2 (로우폴리+글로시) | 인게임용 |
| `drillcorp_result_icons` | **결과 화면 2종** | **이번 팩 - 결과 패널용** |

## 라이선스

자유롭게 사용/수정 가능합니다.

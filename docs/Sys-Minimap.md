# 미니맵 시스템

## 목적
화면 좌상단에 미니맵을 배치하여 머신과 벌레의 위치를 한눈에 파악하게 한다.

## 구현 방식
**세컨드 카메라 + RenderTexture** 방식. 별도 Orthographic 카메라로 월드를 찍어 UI RawImage에 표시.

---

## 구조

### 스크립트
| 파일 | 역할 |
|------|------|
| `Assets/_Game/Scripts/UI/Minimap/MinimapCamera.cs` | 머신 상공(+Y)에서 -Y를 내려다보는 Orthographic 카메라. RenderTexture 출력. |
| `Assets/_Game/Scripts/UI/Minimap/MinimapUI.cs` | RawImage에 RenderTexture 바인딩. |
| `Assets/_Game/Scripts/UI/Minimap/MinimapIcon.cs` | 월드 루트에 생성되어 target을 따라다니는 아이콘. Layer="Minimap". |

### 데이터 흐름
```
월드 (머신/벌레 + 추적용 MinimapIcon)
   ↓ MinimapCamera (Culling Mask: Minimap만)
RenderTexture (RT_Minimap)
   ↓
RawImage (UI Canvas 좌상단)
```

### 아이콘 생성 지점
- `MachineController.Start()` → 파란 Square (`(0.3, 0.8, 1)`, size 2)
- `BugController.OnEnable()` → 빨간 Circle (`(1, 0.3, 0.3)`, size 1)
- `BugBase.Start()` → 빨간 Circle (레거시 경로, BugBase 상속 클래스가 실제 사용 시 동작)

### Unity 에디터 설정
- Layer 8: `Minimap` 추가
- Main Camera Culling Mask에서 `Minimap` 해제
- MinimapCamera 오브젝트 배치, `RT_Minimap` 할당
- MinimapCamera Culling Mask: `Minimap`만 (Default 해제)
- UI Canvas 좌상단에 RawImage + `MinimapUI.cs`
- 디버그 UI(`DebugManager`, `DebugCameraUI`)는 우상단으로 이동

---

## MinimapIcon 설계 핵심

### 1. 부모의 자식이 아니라 월드 루트에 생성
부모(벌레)가 이동 중 Y축 회전(`Quaternion.Euler(0, angle, 0)`)을 하기 때문에,
아이콘을 자식으로 두면 월드 회전 강제(`transform.rotation = ...`)를 적용해도
부모 회전과 자식 강제 회전이 결합되어 **localRotation Z축에 부산물(예: Z=128°)**이 박힌다.

해결: `BugHpBar` 패턴과 동일하게, 아이콘을 **월드 루트 GameObject**로 만들고
`LateUpdate`에서 target의 position만 추적한다.

```csharp
transform.position = _target.position + new Vector3(0f, _heightOffset, 0f);
transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
transform.localScale = Vector3.one * _size;
```

### 2. 회전 `(-90, 0, 0)`인 이유
- MinimapCamera는 `+Y`에서 `-Y`를 내려다봄
- 메시 노멀이 카메라를 향해야(즉 +Y 방향) 보임
- 정상 winding(반시계)인 메시의 노멀은 +Z
- X축 -90도 회전 → 노멀 +Z가 +Y로 향함 → 카메라가 앞면을 봄

### 3. 메시/머티리얼 캐싱 (수백 마리 대응)
벌레가 화면을 가득 채우는 시나리오를 위해:

| 리소스 | 캐싱 키 | 효과 |
|--------|---------|------|
| `_quadMesh` / `_circleMesh` | static (1개씩) | 모든 아이콘이 동일 메시 공유 |
| `_materialCache` | Color | 같은 색은 같은 머티리얼 → SRP Batcher가 묶음 |
| `_cachedShader` | static | `Shader.Find` 1회만 호출 |

`MeshRenderer`도 `lightProbeUsage`/`reflectionProbeUsage` Off로 설정하여 SRP Batcher 호환성을 높임.

### 4. Winding order 주의
- `BuildQuadMesh`: `{0, 1, 2, 0, 2, 3}` (반시계)
- `BuildCircleMesh`: 인덱스 순서 그대로(반시계)

둘 다 노멀 +Z. 이걸 임의로 바꾸면 카메라에 안 보이거나 뒷면이 보임.

### 5. 풀링 대응
`BugController`:
- `OnEnable`: 아이콘 없으면 생성, 있으면 `SetActive(true)`로 재활성화
- `OnDisable`: 아이콘 `SetActive(false)`
- `OnDestroy`: 아이콘 `Destroy` (자식이 아니므로 수동 정리 필요)

`MinimapIcon` 자체도 `LateUpdate`에서 `_target == null`이면 `Destroy(gameObject)`로 자동 정리.

---

## 해결 이력 (디버깅 히스토리)

### 1. 머신 아이콘이 하얗게 보이던 문제 (해결됨)
- 원인: `Create()`에서 `AddComponent` 직후 기본값(white)으로 `Awake`가 먼저 빌드되고 색상 세팅된 2번째 빌드가 겹침
- 해결: `Create()`에서 `SetActive(false)` → 값 세팅 → `SetActive(true)` 순으로 변경하여 Awake 1회만 실행

### 2. 미니맵에 월드 머신 메쉬까지 찍히던 문제 (해결됨)
- 원인: MinimapCamera Culling Mask에 `Default` 포함
- 해결: Culling Mask를 `Minimap`만으로 제한

### 3. 벌레 아이콘이 뒤집힌 회전(Z=128°)으로 박히던 문제 (해결됨)
- 원인: 벌레가 Y축 회전을 하는데 자식 아이콘에 월드 회전을 강제하니 Z축에 부산물이 박힘
- 해결: 아이콘을 자식이 아닌 **월드 루트 오브젝트**로 변경, `LateUpdate`에서 position 추적

### 4. Winding/회전 정합성 (해결됨)
- 원인: Quad의 winding이 시계방향(`{0,2,1, 0,3,2}`)이라 노멀이 -Z. 회전 `(90,0,0)`로 우연히 카메라 향했던 상태
- 해결: Quad winding을 반시계로 정상화(`{0,1,2, 0,2,3}`), 회전을 `(-90,0,0)`로 변경. Circle은 원본이 이미 정상이라 유지.

---

## 남은 작업 (선택)

### Phase 2: 시각 품질 개선
- [ ] 미니맵 배경에 어두운 반투명 프레임(UI Image) 추가
- [ ] 원형 마스크 옵션 (RawImage Material)
- [ ] 벌레 종류별 색상 분기 (Beetle/Fly/Centipede 구분)
- [ ] 조준 방향 표시 (삼각형 아이콘)

### Phase 3: 확장 기능
- [ ] 줌 인/아웃 (OrthographicSize 토글)
- [ ] 웨이브 경고 표시 (곧 스폰될 벌레 위치)
- [ ] 위험 지역 오버레이 (HP 낮은 방향 강조)

### Phase 4: 정리
- [ ] `BugBase`의 레거시 MinimapIcon 생성 경로 제거 여부 결정 (실제 미사용 확인 시 삭제)
- [ ] MinimapCamera/UI 인스펙터 값을 ScriptableObject로 분리 고려

---

## 탑뷰 좌표계 주의사항
- 카메라 회전: `Quaternion.Euler(90, 0, 0)` — Y 높이 방향에서 -Y를 향함
- 아이콘 회전: `Quaternion.Euler(-90, 0, 0)` — 메시 노멀(+Z)이 +Y(카메라 방향)로 향하게
- 아이콘 위치 오프셋: `new Vector3(0, heightOffset, 0)` — **Y가 높이**
- 거리/이동은 XZ 평면 기준이지만 미니맵 카메라는 Y 높이에 무관하게 XZ를 투영

## 참고 커밋
- `d13d2ef` — 디버그 UI 우상단 이동 (미니맵 자리 확보)

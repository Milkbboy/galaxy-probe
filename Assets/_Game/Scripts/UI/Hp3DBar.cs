using UnityEngine;
using DrillCorp.Diagnostics;

namespace DrillCorp.UI
{
    /// <summary>
    /// 3D primitive Cube 2개로 구성된 월드공간 HP 바.
    /// Canvas/SpriteRenderer 가 아닌 실제 3D 메시라 조명·깊이 처리가 일관적.
    ///
    /// 구조:
    ///   Root (이 컴포넌트 + LateUpdate로 target 위치 추적)
    ///   ├─ Bg   (어두운 배경 Cube, 고정 크기)
    ///   └─ Fill (밝은 Cube, localScale.x 가 HP 비율에 비례, 왼쪽 정렬)
    ///
    /// 사용:
    ///   var bar = Hp3DBar.Create(drone.transform, new Vector3(0, 0.8f, 0), new Vector3(2f, 0.2f, 0.3f));
    ///   bar.SetHealth(_currentHp / _maxHp);
    ///
    /// 탑뷰(카메라 -Y) 기준 offset.z 가 "화면 위쪽". 드론보다 살짝 위/뒤에 띄우기 권장.
    /// 회전은 LateUpdate 에서 identity 로 고정 → 부모 회전 무시.
    /// </summary>
    public class Hp3DBar : MonoBehaviour
    {
        private Transform _target;
        private Vector3 _offset;
        private Transform _fill;
        private Renderer _fillRenderer;
        private Material _fillMaterial;
        private Material _bgMaterial;
        private Vector3 _baseFillScale;
        private float _currentRatio = 1f;

        // 색 — 지누스 테마 기본. Create 후 SetColors 로 교체 가능.
        private Color _colorFull = new Color(0.31f, 0.81f, 0.4f, 1f);
        private Color _colorLow  = new Color(0.95f, 0.28f, 0.22f, 1f);
        private float _lowThreshold = 0.3f;

        /// <summary>
        /// HP 바 월드오브젝트 생성. target 은 따라갈 대상 (보통 드론 root Transform).
        /// size 는 (가로, 높이, 깊이) 유닛 — 가로가 HP 비율에 따라 축소.
        /// </summary>
        public static Hp3DBar Create(Transform target, Vector3 offset, Vector3 size)
        {
            var root = new GameObject("Hp3DBar");
            var bar = root.AddComponent<Hp3DBar>();

            // Background — 어두운 고정 크기 Cube
            var bg = BuildCube("Bg", root.transform, Vector3.zero, size, new Color(0.06f, 0.06f, 0.08f, 1f), out bar._bgMaterial);

            // Fill — 밝은 Cube, z-fight 방지용 살짝 앞/위로 오프셋
            float zFrontEps = 0.002f;
            var fillPos = new Vector3(0f, size.y * 0.05f, -zFrontEps);
            BuildCube("Fill", root.transform, fillPos, size, bar._colorFull, out bar._fillMaterial);
            var fillGo = root.transform.Find("Fill").gameObject;
            bar._fill = fillGo.transform;
            bar._fillRenderer = fillGo.GetComponent<Renderer>();
            bar._baseFillScale = size;

            bar._target = target;
            bar._offset = offset;
            bar.SetHealth(1f);
            return bar;
        }

        /// <summary>색 튜닝 — Create 이후 호출 (드론 포탑 테마색 강조용).</summary>
        public void SetColors(Color full, Color low, float lowThreshold = 0.3f)
        {
            _colorFull = full;
            _colorLow = low;
            _lowThreshold = Mathf.Clamp01(lowThreshold);
            SetHealth(_currentRatio); // 즉시 재반영
        }

        public void SetHealth(float ratio)
        {
            _currentRatio = Mathf.Clamp01(ratio);
            if (_fill == null) return;

            // 가로 축소 + 왼쪽 정렬.
            var s = _baseFillScale;
            s.x = _baseFillScale.x * _currentRatio;
            _fill.localScale = s;

            var p = _fill.localPosition;
            p.x = -(_baseFillScale.x - s.x) * 0.5f;
            _fill.localPosition = p;

            if (_fillMaterial != null)
                _fillMaterial.color = _currentRatio <= _lowThreshold ? _colorLow : _colorFull;
        }

        private void LateUpdate()
        {
            using var _perf = PerfMarkers.Hp3DBar_LateUpdate.Auto();

            if (_target == null)
            {
                Destroy(gameObject);
                return;
            }
            transform.position = _target.position + _offset;
            transform.rotation = Quaternion.identity; // 부모 회전 무시 (탑뷰 고정)
        }

        private void OnDestroy()
        {
            // 런타임 Instance 머티리얼 정리 — 누수 방지.
            if (_fillMaterial != null) Destroy(_fillMaterial);
            if (_bgMaterial != null) Destroy(_bgMaterial);
        }

        // ─── 내부: Cube 빌더 ─────────────────────────────────────────
        private static GameObject BuildCube(string name, Transform parent, Vector3 localPos, Vector3 localScale, Color color, out Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            // 물리 간섭 제거 — HP 바가 Physics 에 잡히면 안 됨.
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = localScale;

            // Unlit 재질로 교체 — 조명 영향 없이 또렷하게 보이도록.
            var r = go.GetComponent<Renderer>();
            mat = CreateUnlitMaterial(color);
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }

        private static Material CreateUnlitMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader) { name = "Hp3DBar_Runtime" };
            // URP Unlit 은 _BaseColor, 빌트인 Unlit/Color 는 _Color 사용 — 둘 다 시도.
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            mat.color = color; // 대부분 셰이더에서 동작하는 일반 경로
            return mat;
        }
    }
}

using TMPro;
using UnityEngine;

namespace DrillCorp.UI
{
    /// <summary>
    /// 채굴 드론 수명 타이머 시각화 — 월드공간 3D.
    ///
    /// v2.html:1643~1646 원본 포팅:
    ///   · 외곽 원호(12시 시작, 시계방향 수축) — 남은 수명 비율 0~1
    ///   · "Ns" 라벨 — ceil(남은 초) 표시
    ///
    /// 구현:
    ///   · 원호 = LineRenderer (반경 _arcRadius, N+1 points).
    ///     활성 점 개수를 progress 에 따라 바꿔 수축 애니메이션.
    ///   · 라벨 = TextMeshPro (비-UI 3D) — Ns 포맷.
    ///
    /// CLAUDE.md 탑뷰 규칙 준수 — XZ 평면에 누워서 카메라(-Y) 쪽으로 렌더.
    /// LineRenderer 는 useWorldSpace=false 로 로컬 좌표 사용 → target.position + offset 만으로 위치 갱신.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class MiningDroneTimer3D : MonoBehaviour
    {
        private Transform _target;
        private Vector3 _offset;
        private float _arcRadius = 1.4f;
        private float _lineWidth = 0.15f;
        private int _segments = 48; // 원호 전체 분할 수

        private LineRenderer _line;
        private TextMeshPro _label;

        private float _progress = 1f; // 0~1, 1 = 전체 원, 0 = 없음

        // 원호는 12시(탑뷰 월드 +Z) 에서 시작, 시계방향으로 수축.
        // XZ 평면에서: angle=0 → (0,0,1) (+Z), 시계방향 증가 → +X 방향으로.
        // canvas 원본은 -π/2 시작 시계방향 — 월드에선 (0, +Z) 시작 (x=sin, z=cos) 로 매핑.

        public static MiningDroneTimer3D Create(
            Transform target,
            Vector3 offset,
            float arcRadius,
            float lineWidth,
            Color color,
            int segments = 48)
        {
            var root = new GameObject("MiningDroneTimer3D");
            var timer = root.AddComponent<MiningDroneTimer3D>();
            timer._target = target;
            timer._offset = offset;
            timer._arcRadius = arcRadius;
            timer._lineWidth = lineWidth;
            timer._segments = Mathf.Max(8, segments);

            timer.SetupLineRenderer(color);
            timer.SetupLabel(color, arcRadius);

            timer.SetProgress(1f);
            timer.SetSeconds(0);
            return timer;
        }

        private void SetupLineRenderer(Color color)
        {
            _line = GetComponent<LineRenderer>();
            _line.useWorldSpace = false;
            _line.loop = false;
            _line.numCornerVertices = 4;
            _line.numCapVertices = 2;
            // View 모드 = quad normal 이 항상 카메라 방향.
            // 탑뷰(카메라 -Y) 에서 선이 XZ 평면에 납작하게 그려져 너비가 올바르게 보임.
            // (TransformZ 는 quad normal 을 transform.Z 에 고정해 vertical plane 으로 서 버림.)
            _line.alignment = LineAlignment.View;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader) { name = "MiningDroneTimer_Line" };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            mat.color = color;
            _line.sharedMaterial = mat;

            _line.startWidth = _lineWidth;
            _line.endWidth = _lineWidth;
            _line.startColor = color;
            _line.endColor = color;
            _line.positionCount = 0;
        }

        private void SetupLabel(Color color, float radius)
        {
            var labelGo = new GameObject("SecondsLabel");
            labelGo.transform.SetParent(transform, false);
            // 원호 중심 바로 위(Z+) — 탑뷰에서 원 '12시 방향' 바깥.
            labelGo.transform.localPosition = new Vector3(0f, 0.05f, radius + 0.6f);
            labelGo.transform.localRotation = Quaternion.identity;
            labelGo.transform.localScale = Vector3.one;

            _label = labelGo.AddComponent<TextMeshPro>();
            TMPFontHelper.ApplyDefaultFont(_label);

            var rect = _label.rectTransform;
            rect.sizeDelta = new Vector2(3f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            _label.alignment = TextAlignmentOptions.Center;
            _label.fontSize = 4f;
            _label.color = color;
            _label.fontStyle = FontStyles.Bold;
            _label.text = "0s";
            _label.textWrappingMode = TextWrappingModes.NoWrap;
            _label.overflowMode = TextOverflowModes.Overflow;
            _label.enableAutoSizing = false;
        }

        /// <summary>남은 수명 비율 0~1 로 원호 길이 갱신.</summary>
        public void SetProgress(float progress)
        {
            _progress = Mathf.Clamp01(progress);
            RebuildArc();
        }

        /// <summary>초 단위 텍스트 갱신 (예: 7 → "7s").</summary>
        public void SetSeconds(int seconds)
        {
            if (_label == null) return;
            if (seconds < 0) seconds = 0;
            _label.text = seconds + "s";
        }

        private void RebuildArc()
        {
            if (_line == null) return;

            // segments 개의 포인트로 _progress 비율만큼 원호 렌더.
            // +Z(12시) 에서 시작, 시계방향 = +X 쪽으로 회전.
            int activeSegs = Mathf.CeilToInt(_segments * _progress);
            int pointCount = activeSegs + 1; // segs 개 구간 = N+1 점
            if (pointCount < 2) pointCount = 0;

            _line.positionCount = pointCount;
            if (pointCount == 0) return;

            float sweepRad = Mathf.PI * 2f * _progress;
            for (int i = 0; i < pointCount; i++)
            {
                float t = (activeSegs > 0) ? (float)i / activeSegs : 0f;
                float a = t * sweepRad;           // 0 → sweepRad
                // 탑뷰 매핑: angle 0 → (0,0,+R), 시계방향(clockwise, 위에서 볼 때) = +X 쪽 이동
                // 따라서 x = sin(a) * R, z = cos(a) * R
                float x = Mathf.Sin(a) * _arcRadius;
                float z = Mathf.Cos(a) * _arcRadius;
                _line.SetPosition(i, new Vector3(x, 0f, z));
            }
        }

        private void LateUpdate()
        {
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
            if (_line != null && _line.sharedMaterial != null)
            {
                // 런타임 Instance 머티리얼 정리.
                Destroy(_line.sharedMaterial);
            }
        }
    }
}

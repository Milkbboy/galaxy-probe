using UnityEngine;

namespace DrillCorp.Aim
{
    /// <summary>
    /// 에임 주변에 표시되는 무기별 쿨다운 진행 호 (동적 메시 기반).
    /// _.html 프로토타입의 drawCrosshair에서 각 무기마다 반경 r+6/r+13/r+20/r+27에
    /// 쿨다운 진행도를 시계방향 호로 그리는 효과를 Unity MeshRenderer로 재현한다.
    ///
    /// 사용법:
    /// 1. AimController 자식에 빈 GameObject 생성
    /// 2. 이 컴포넌트 추가 (MeshFilter/MeshRenderer 자동 추가)
    /// 3. RadiusOffset/Thickness/Color 설정, 외부 Binder가 FillAmount 주입
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class AimWeaponRing : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("AimController (비우면 부모에서 자동 탐색)")]
        [SerializeField] private AimController _aim;

        [Header("Ring Shape")]
        [Tooltip("에임 기본 반경(AimRadius)에 더해지는 외곽 오프셋. 프로토타입 r+6/r+13/r+20/r+27 매핑용.")]
        [SerializeField] private float _radiusOffset = 0.08f;

        [Tooltip("링 굵기 (월드 유닛, 외부 반경에서 내부 반경까지)")]
        [SerializeField] private float _thickness = 0.04f;

        [Tooltip("원 둘레를 몇 조각으로 나눌지 (부드러움 vs 버텍스 수)")]
        [Range(12, 128)]
        [SerializeField] private int _segments = 64;

        [Header("Colors")]
        [Tooltip("쿨다운 중 색 (이 색으로 진행 호가 차오름)")]
        [SerializeField] private Color _cooldownColor = new Color(0.88f, 0.25f, 0.98f, 0.9f);

        [Tooltip("준비 완료 + 타겟 있음 색")]
        [SerializeField] private Color _readyHitColor = new Color(0.88f, 0.25f, 0.98f, 0.9f);

        [Tooltip("준비 완료 + 타겟 없음 색")]
        [SerializeField] private Color _readyIdleColor = new Color(0f, 0.9f, 1f, 0.5f);

        [Header("Debug")]
        [Range(0f, 1f)]
        [Tooltip("편집기 미리보기용. 런타임에는 Binder가 덮어씀")]
        [SerializeField] private float _previewFill = 1f;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Mesh _mesh;
        private Material _material;

        private float _fillAmount = 1f;
        private Color _currentColor;
        private float _cachedOuterRadius = -1f;
        private float _cachedFill = -1f;
        private int _cachedSegments = -1;

        private static readonly int ColorProp = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorPropLegacy = Shader.PropertyToID("_Color");

        public AimController Aim => _aim;
        public float RadiusOffset { get => _radiusOffset; set => _radiusOffset = value; }

        public float FillAmount
        {
            get => _fillAmount;
            set { _fillAmount = Mathf.Clamp01(value); }
        }

        public void SetColor(Color c)
        {
            _currentColor = c;
            ApplyMaterialColor();
        }

        public void SetState(bool isReady, bool isHitting)
        {
            Color target = !isReady ? _cooldownColor : (isHitting ? _readyHitColor : _readyIdleColor);
            if (target != _currentColor)
            {
                _currentColor = target;
                ApplyMaterialColor();
            }
        }

        private void Awake()
        {
            EnsureRefs();
        }

        private void OnEnable()
        {
            EnsureRefs();
            RebuildMesh(force: true);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureRefs();
            _fillAmount = _previewFill;
            RebuildMesh(force: true);
        }
#endif

        private void OnDestroy()
        {
            if (_mesh != null)
            {
                if (Application.isPlaying) Destroy(_mesh);
                else DestroyImmediate(_mesh);
            }
            if (_material != null)
            {
                if (Application.isPlaying) Destroy(_material);
                else DestroyImmediate(_material);
            }
        }

        private void EnsureRefs()
        {
            if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
            if (_meshRenderer == null) _meshRenderer = GetComponent<MeshRenderer>();
            if (_aim == null) _aim = GetComponentInParent<AimController>();

            if (_mesh == null)
            {
                _mesh = new Mesh { name = "AimWeaponRing_Dynamic" };
                _mesh.MarkDynamic();
            }
            if (_meshFilter.sharedMesh != _mesh)
            {
                _meshFilter.sharedMesh = _mesh;
            }

            if (_material == null)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
                if (sh == null) sh = Shader.Find("Unlit/Color");
                if (sh == null) sh = Shader.Find("Sprites/Default");
                _material = new Material(sh) { name = "AimWeaponRing_Mat" };
                // 양면 렌더링 + 알파 투명
                if (_material.HasProperty("_Cull")) _material.SetFloat("_Cull", 0f); // Off
                if (_material.HasProperty("_Surface")) _material.SetFloat("_Surface", 1f); // Transparent
                if (_material.HasProperty("_Blend")) _material.SetFloat("_Blend", 0f); // Alpha
                if (_material.HasProperty("_SrcBlend")) _material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                if (_material.HasProperty("_DstBlend")) _material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                if (_material.HasProperty("_ZWrite")) _material.SetFloat("_ZWrite", 0f);
                _material.renderQueue = 3000;
            }
            _meshRenderer.sharedMaterial = _material;
            _meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _meshRenderer.receiveShadows = false;

            if (_currentColor.a < 0.01f && _currentColor.r + _currentColor.g + _currentColor.b < 0.01f)
            {
                _currentColor = _cooldownColor;
                ApplyMaterialColor();
            }
        }

        private void ApplyMaterialColor()
        {
            if (_material == null) return;
            _material.color = _currentColor;
            if (_material.HasProperty(ColorProp)) _material.SetColor(ColorProp, _currentColor);
            if (_material.HasProperty(ColorPropLegacy)) _material.SetColor(ColorPropLegacy, _currentColor);
        }

        private void LateUpdate()
        {
            RebuildMesh(force: false);
        }

        private void RebuildMesh(bool force)
        {
            if (_mesh == null || _aim == null) return;

            float outer = _aim.AimRadius + _radiusOffset;
            float inner = Mathf.Max(0.001f, outer - _thickness);

            if (!force
                && Mathf.Approximately(outer, _cachedOuterRadius)
                && Mathf.Approximately(_fillAmount, _cachedFill)
                && _segments == _cachedSegments)
            {
                return;
            }

            _cachedOuterRadius = outer;
            _cachedFill = _fillAmount;
            _cachedSegments = _segments;

            _mesh.Clear();

            if (_fillAmount <= 0.0001f)
            {
                return;
            }

            int seg = Mathf.Max(1, Mathf.RoundToInt(_segments * _fillAmount));
            float totalAngle = Mathf.PI * 2f * _fillAmount;
            float startAngle = -Mathf.PI / 2f; // 12시 방향

            int vertCount = (seg + 1) * 2;
            var verts = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];
            var tris = new int[seg * 6];

            for (int i = 0; i <= seg; i++)
            {
                float t = (float)i / seg;
                float a = startAngle + totalAngle * t; // 시계방향으로 증가
                float c = Mathf.Cos(a);
                float s = Mathf.Sin(a);

                // 에임 오브젝트의 로컬 좌표계: X/Y 평면에 그림
                // (Aim GameObject가 이미 X축 90° 회전돼 있어서 월드에선 XZ 평면 표시됨)
                int vi = i * 2;
                verts[vi] = new Vector3(c * outer, s * outer, 0f);
                verts[vi + 1] = new Vector3(c * inner, s * inner, 0f);
                uvs[vi] = new Vector2(t, 0f);
                uvs[vi + 1] = new Vector2(t, 1f);
            }

            for (int i = 0; i < seg; i++)
            {
                int o = i * 2;
                int t = i * 6;
                // Aim 오브젝트가 X축 90°로 눕혀져 있어 카메라가 메시의 -Z(로컬) 면을 본다.
                // → 반시계방향으로 감아야 앞면이 카메라를 향한다 (회전 없이 바로 보이도록)
                tris[t + 0] = o + 0;
                tris[t + 1] = o + 1;
                tris[t + 2] = o + 2;
                tris[t + 3] = o + 1;
                tris[t + 4] = o + 3;
                tris[t + 5] = o + 2;
            }

            _mesh.vertices = verts;
            _mesh.uv = uvs;
            _mesh.triangles = tris;
            _mesh.RecalculateBounds();
        }
    }
}

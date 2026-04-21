using UnityEngine;

namespace DrillCorp.Ability
{
    /// <summary>
    /// 어빌리티 바닥 범위 데칼 — 탑뷰(XZ 평면) Sprite/Mesh 기반.
    ///
    /// 공통 기능:
    ///   · 탑뷰 회전 고정 (90,0,0) — 부모 회전 무시하고 로컬 회전으로 Yaw만 반영
    ///   · 알파 페이드인 → 유지 → 페이드아웃 (BombLandingMarker 펄스 확장)
    ///   · 원형/직사각형/부채꼴에 공통 적용 — 모양 제어는 Renderer/Mesh 쪽에서 담당
    ///
    /// 사용 시나리오:
    ///   A. 프리펩 + SpriteRenderer — 네이팜(사각 스프라이트), 지뢰(링 스프라이트)
    ///   B. 런타임 Mesh — 화염방사기 부채꼴 (MeshRenderer 사용)
    ///
    /// 색/알파는 Renderer.material.color (또는 SpriteRenderer.color) 인스턴스가 잡으므로
    /// 여러 데칼이 동시에 존재해도 서로 간섭 없음.
    /// </summary>
    public class AbilityRangeDecal : MonoBehaviour
    {
        [Header("Visual")]
        [Tooltip("기본 색. 알파는 _baseAlpha × 페이드 계수 × 펄스 계수로 매 프레임 재계산.")]
        [SerializeField] private Color _tint = new Color(1f, 0.42f, 0.21f, 1f);

        [Range(0f, 1f)]
        [Tooltip("펄스/페이드 기준 알파. 실제 표시 알파는 이 값 × 펄스 × 페이드.")]
        [SerializeField] private float _baseAlpha = 0.25f;

        [Header("Pulse")]
        [Tooltip("펄스 진폭 (baseAlpha 위로 더해지는 최대값).")]
        [Range(0f, 1f)]
        [SerializeField] private float _pulseAmplitude = 0.15f;

        [Tooltip("펄스 속도 (1초당 PingPong 사이클).")]
        [Range(0.1f, 5f)]
        [SerializeField] private float _pulseSpeed = 1.5f;

        [Header("Fade")]
        [Tooltip("페이드인 시간(초). 0이면 즉시 최대 알파.")]
        [Min(0f)]
        [SerializeField] private float _fadeInSec = 0.25f;

        [Tooltip("페이드아웃 시간(초). Dispose() 호출 시 시작.")]
        [Min(0f)]
        [SerializeField] private float _fadeOutSec = 0.5f;

        private SpriteRenderer _sprite;
        private MeshRenderer _mesh;
        private Material _runtimeMaterial; // Mesh 경로 전용 — Runner가 SetupMesh()로 생성.

        private float _age;
        private float _fadeOutRemaining = -1f; // 음수 = 페이드아웃 중 아님

        private void Awake()
        {
            _sprite = GetComponentInChildren<SpriteRenderer>();
            _mesh = GetComponentInChildren<MeshRenderer>();
            ApplyColor(0f);
        }

        /// <summary>
        /// 런타임 Mesh 데칼 초기화 — MeshFilter + MeshRenderer + 투명 Unlit 머티리얼을 같은 GO에 구성.
        /// Runner가 <see cref="AbilityDecalMeshBuilder"/>로 만든 Mesh를 넘긴다.
        /// </summary>
        public void SetupMesh(Mesh mesh)
        {
            var mf = GetComponent<MeshFilter>();
            if (mf == null) mf = gameObject.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            _mesh = GetComponent<MeshRenderer>();
            if (_mesh == null) _mesh = gameObject.AddComponent<MeshRenderer>();
            _mesh.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _mesh.receiveShadows = false;
            _mesh.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            _mesh.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            if (_runtimeMaterial == null)
                _runtimeMaterial = CreateTransparentUnlitMaterial();
            _mesh.sharedMaterial = _runtimeMaterial;
        }

        private static Material CreateTransparentUnlitMaterial()
        {
            // Sprites/Default 셰이더 — 빌트인/URP 둘 다 존재. SpriteRenderer용이지만 MeshRenderer에서도
            // Vertex Color × _Color 로 알파 블렌딩이 정상 동작하고 라이팅 영향 없음. URP Unlit Transparent 보다
            // 키워드 세팅 실수 여지가 없어 안전.
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");

            var mat = new Material(shader) { name = "AbilityRangeDecal_Runtime" };
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            // URP Unlit fallback 시 필요한 키워드도 세팅해 둠.
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            return mat;
        }

        private void OnDestroy()
        {
            if (_runtimeMaterial != null) Destroy(_runtimeMaterial);
        }

        private void OnEnable()
        {
            _age = 0f;
            _fadeOutRemaining = -1f;
            ApplyColor(0f);
        }

        /// <summary>색 설정 — 런타임 튜닝용. 알파는 별도 펄스/페이드 계산.</summary>
        public void SetTint(Color color)
        {
            _tint = new Color(color.r, color.g, color.b, 1f);
            ApplyColor(CurrentAlphaFactor());
        }

        /// <summary>baseAlpha 설정 — 기본 밝기 조절.</summary>
        public void SetBaseAlpha(float a) => _baseAlpha = Mathf.Clamp01(a);

        /// <summary>펄스 진폭 0으로 두면 PingPong 없이 단색 유지. 페이드인/아웃은 그대로 작동.</summary>
        public void SetPulseAmplitude(float a) => _pulseAmplitude = Mathf.Clamp01(a);

        /// <summary>페이드아웃 시작. 이후 _fadeOutSec 경과하면 GameObject Destroy.</summary>
        public void Dispose()
        {
            if (_fadeOutRemaining < 0f)
                _fadeOutRemaining = Mathf.Max(0.01f, _fadeOutSec);
        }

        private void Update()
        {
            _age += Time.deltaTime;

            if (_fadeOutRemaining >= 0f)
            {
                _fadeOutRemaining -= Time.deltaTime;
                if (_fadeOutRemaining <= 0f)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            ApplyColor(CurrentAlphaFactor());
        }

        // 페이드인(0→1) × 펄스(기본→진폭) × 페이드아웃(1→0) 곱으로 계산.
        private float CurrentAlphaFactor()
        {
            float fadeIn = _fadeInSec <= 0f ? 1f : Mathf.Clamp01(_age / _fadeInSec);
            float fadeOut = _fadeOutRemaining < 0f ? 1f : Mathf.Clamp01(_fadeOutRemaining / Mathf.Max(0.0001f, _fadeOutSec));
            float pulse = _baseAlpha + Mathf.PingPong(Time.time * _pulseSpeed, _pulseAmplitude);
            return pulse * fadeIn * fadeOut;
        }

        private void ApplyColor(float alpha)
        {
            var c = new Color(_tint.r, _tint.g, _tint.b, alpha);

            if (_sprite != null) _sprite.color = c;

            if (_runtimeMaterial != null)
            {
                // URP Unlit은 _BaseColor, Built-in/Legacy Unlit은 _Color. 둘 다 설정.
                if (_runtimeMaterial.HasProperty("_BaseColor")) _runtimeMaterial.SetColor("_BaseColor", c);
                if (_runtimeMaterial.HasProperty("_Color")) _runtimeMaterial.SetColor("_Color", c);
            }
        }
    }
}

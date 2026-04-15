using System.Collections.Generic;
using UnityEngine;

namespace DrillCorp.UI.Minimap
{
    /// <summary>
    /// 월드 오브젝트를 따라다니며 미니맵 카메라에만 보이는 아이콘.
    /// 부모 회전/스케일 영향을 피하기 위해 자식이 아닌 월드 루트 오브젝트로 둔다.
    /// 메시/머티리얼은 shape·color 단위로 캐싱하여 SRP Batcher 친화적으로 동작한다.
    /// </summary>
    public class MinimapIcon : MonoBehaviour
    {
        public enum IconShape { Square, Circle }

        [SerializeField] private IconShape _shape = IconShape.Square;
        [SerializeField] private Color _color = Color.white;
        [SerializeField] private float _size = 1f;
        [SerializeField] private float _heightOffset = 0.5f;

        private Transform _target;
        private MeshRenderer _renderer;
        private bool _built;

        private static Mesh _quadMesh;
        private static Mesh _circleMesh;
        private static Shader _cachedShader;
        private static readonly Dictionary<Color, Material> _materialCache = new Dictionary<Color, Material>();
        private static readonly MaterialPropertyBlock _propBlock = new MaterialPropertyBlock();

        public static MinimapIcon Create(Transform target, Color color, float size = 1f, IconShape shape = IconShape.Square)
        {
            int minimapLayer = LayerMask.NameToLayer("Minimap");
            if (minimapLayer == -1)
            {
                Debug.LogWarning("[MinimapIcon] 'Minimap' 레이어가 존재하지 않습니다. Tags and Layers 설정 확인 필요.");
                return null;
            }

            var go = new GameObject($"MinimapIcon_{target.name}");
            go.layer = minimapLayer;

            go.SetActive(false);
            var icon = go.AddComponent<MinimapIcon>();
            icon._color = color;
            icon._size = size;
            icon._shape = shape;
            icon._target = target;
            go.SetActive(true);
            return icon;
        }

        private void Awake()
        {
            if (!_built)
                BuildMesh();
        }

        private void BuildMesh()
        {
            if (_built) return;
            _built = true;

            int minimapLayer = LayerMask.NameToLayer("Minimap");
            if (minimapLayer == -1) return;

            var child = new GameObject("Quad");
            child.transform.SetParent(transform, false);
            child.layer = minimapLayer;

            var mf = child.AddComponent<MeshFilter>();
            _renderer = child.AddComponent<MeshRenderer>();
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            _renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            mf.sharedMesh = _shape == IconShape.Circle ? GetCircleMesh() : GetQuadMesh();
            _renderer.sharedMaterial = GetMaterial(_color);
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                Destroy(gameObject);
                return;
            }

            transform.position = _target.position + new Vector3(0f, _heightOffset, 0f);
            transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            transform.localScale = Vector3.one * _size;
        }

        private static Mesh GetQuadMesh()
        {
            if (_quadMesh != null) return _quadMesh;

            _quadMesh = new Mesh { name = "MinimapQuad" };
            _quadMesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3( 0.5f, -0.5f, 0f),
                new Vector3( 0.5f,  0.5f, 0f),
                new Vector3(-0.5f,  0.5f, 0f)
            };
            _quadMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            _quadMesh.RecalculateNormals();
            _quadMesh.RecalculateBounds();
            return _quadMesh;
        }

        private static Mesh GetCircleMesh()
        {
            if (_circleMesh != null) return _circleMesh;

            const int segments = 32;
            _circleMesh = new Mesh { name = "MinimapCircle" };
            var verts = new Vector3[segments + 1];
            var tris = new int[segments * 3];
            verts[0] = Vector3.zero;
            for (int i = 0; i < segments; i++)
            {
                float a = (float)i / segments * Mathf.PI * 2f;
                verts[i + 1] = new Vector3(Mathf.Cos(a) * 0.5f, Mathf.Sin(a) * 0.5f, 0f);
                tris[i * 3 + 0] = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = (i + 1) % segments + 1;
            }
            _circleMesh.vertices = verts;
            _circleMesh.triangles = tris;
            _circleMesh.RecalculateNormals();
            _circleMesh.RecalculateBounds();
            return _circleMesh;
        }

        private static Material GetMaterial(Color color)
        {
            if (_materialCache.TryGetValue(color, out var cached) && cached != null)
                return cached;

            if (_cachedShader == null)
            {
                _cachedShader = Shader.Find("Unlit/Color");
                if (_cachedShader == null) _cachedShader = Shader.Find("Universal Render Pipeline/Unlit");
                if (_cachedShader == null) _cachedShader = Shader.Find("Sprites/Default");
            }

            var mat = new Material(_cachedShader) { name = $"MinimapIcon_{ColorUtility.ToHtmlStringRGBA(color)}" };
            mat.color = color;
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_Color", color);
            _materialCache[color] = mat;
            return mat;
        }
    }
}

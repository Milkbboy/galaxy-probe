using UnityEngine;
using DrillCorp.Machine;

namespace DrillCorp.Bug.Behaviors.Skill
{
    /// <summary>
    /// 전방향 폭발 스킬 - 주변 모든 대상에게 데미지
    /// param1 = 쿨다운
    /// param2 = 폭발 반경 (기본 3)
    /// </summary>
    public class NovaSkill : SkillBehaviorBase
    {
        private float _explosionRadius;
        private float _damageMultiplier = 1.5f;
        private GameObject _effectPrefab;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private GameObject _indicatorObj;
        private const int CIRCLE_SEGMENTS = 32;

        public NovaSkill(float cooldown = 8f, float radius = 3f, GameObject effectPrefab = null)
            : base(cooldown, radius + 1f) // 사거리는 폭발 반경보다 약간 크게
        {
            _explosionRadius = radius > 0f ? radius : 3f;
            _effectPrefab = effectPrefab;
        }

        public override void Initialize(BugController bug)
        {
            base.Initialize(bug);
            CreateRangeIndicator();
        }

        public override void Cleanup()
        {
            if (_indicatorObj != null)
            {
                Object.Destroy(_indicatorObj);
                _indicatorObj = null;
            }
            base.Cleanup();
        }

        private void CreateRangeIndicator()
        {
            if (_bug == null) return;

            _indicatorObj = new GameObject("NovaRangeIndicator");
            _indicatorObj.transform.SetParent(_bug.transform);
            _indicatorObj.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            _indicatorObj.transform.localRotation = Quaternion.identity;

            _meshFilter = _indicatorObj.AddComponent<MeshFilter>();
            _meshRenderer = _indicatorObj.AddComponent<MeshRenderer>();

            // 원형 메시 생성
            _meshFilter.mesh = CreateCircleMesh(_explosionRadius);

            // 머티리얼 설정 (반투명 주황색)
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(1f, 0.5f, 0.1f, 0.3f);
            _meshRenderer.material = mat;
        }

        private Mesh CreateCircleMesh(float radius)
        {
            Mesh mesh = new Mesh();

            // 정점: 중심 + 원둘레
            Vector3[] vertices = new Vector3[CIRCLE_SEGMENTS + 1];
            vertices[0] = Vector3.zero; // 중심

            for (int i = 0; i < CIRCLE_SEGMENTS; i++)
            {
                float angle = (360f / CIRCLE_SEGMENTS) * i * Mathf.Deg2Rad;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            }

            // 삼각형 인덱스
            int[] triangles = new int[CIRCLE_SEGMENTS * 3];
            for (int i = 0; i < CIRCLE_SEGMENTS; i++)
            {
                triangles[i * 3] = 0; // 중심
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = (i + 1) % CIRCLE_SEGMENTS + 1;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            return mesh;
        }

        /// <summary>
        /// 매 프레임 범위 표시 업데이트
        /// </summary>
        public void UpdateRangeIndicator()
        {
            if (_indicatorObj == null || _bug == null) return;

            // 위치만 업데이트 (메시는 로컬 좌표라 부모 따라감)
            _indicatorObj.transform.position = new Vector3(
                _bug.transform.position.x,
                0.05f,
                _bug.transform.position.z
            );
        }

        protected override void UseSkill(Transform target)
        {
            if (_bug == null) return;

            Vector3 center = _bug.transform.position;
            float damage = _bug.AttackDamage * _damageMultiplier;

            // 범위 내 모든 타겟 검색
            Collider[] hits = Physics.OverlapSphere(center, _explosionRadius);

            foreach (var hit in hits)
            {
                // 자기 자신 및 다른 버그 제외
                if (hit.transform == _bug.transform) continue;
                if (hit.transform.IsChildOf(_bug.transform)) continue;
                if (hit.GetComponent<BugController>() != null) continue;
                if (hit.GetComponent<BugBase>() != null) continue;

                // IDamageable이 있는 대상만
                var damageable = hit.GetComponent<IDamageable>();
                if (damageable == null) continue;

                damageable.TakeDamage(damage);
            }

            // 폭발 이펙트
            PlayNovaEffect(center);
        }

        private void PlayNovaEffect(Vector3 position)
        {
            if (_effectPrefab != null)
            {
                // 커스텀 이펙트 사용
                var effect = Object.Instantiate(_effectPrefab, position, Quaternion.identity);
                Object.Destroy(effect, 3f);
            }
            else
            {
                // 폴백: SimpleVFX 폭발 이펙트
                VFX.SimpleVFX.PlayExplosion(position, _explosionRadius);
            }
        }
    }
}

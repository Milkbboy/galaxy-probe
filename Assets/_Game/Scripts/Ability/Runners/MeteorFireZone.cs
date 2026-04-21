using UnityEngine;
using DrillCorp.Machine;

namespace DrillCorp.Ability.Runners
{
    /// <summary>
    /// 반중력 메테오 착지 시 생성되는 원형 지속 화염 지대.
    /// MeteorInstance 가 착지 순간 new GameObject 로 스폰 후 Initialize 호출.
    ///
    /// v2.html:1013~1019 + 1177 (isMeteor 원형 판정 + napalmZones push) 포팅.
    ///
    /// 수명 _duration 동안 _tickInterval 주기로 _radius 내 SimpleBug 에 _damagePerTick 데미지.
    /// 바닥 데칼 + 프리펩 VFX(FloorTrapMolten) 은 자식으로 붙여 같이 정리.
    /// </summary>
    public class MeteorFireZone : MonoBehaviour
    {
        private float _radius;
        private float _life;
        private float _tickTimer;
        private float _tickInterval;
        private float _damagePerTick;
        private LayerMask _bugLayer;

        private AbilityRangeDecal _decal;
        private GameObject _vfxWrapper;

        private readonly Collider[] _buffer = new Collider[64];

        // 탑뷰 평면화 — FloorTrapMolten 은 원형 authoring 이라 기본 false 도 무방.
        // 다만 wrapper 로 감싸두면 다른 프리펩(원형 아닌 것) 교체 시 유연.
        private const float DecalYOffset = 0.02f;

        /// <param name="center">지면 평면(Y=0) 기준 중심 좌표</param>
        /// <param name="radius">화염 지대 반경 (AbilityData.Range)</param>
        /// <param name="duration">지속 시간(초) (AbilityData.DurationSec)</param>
        /// <param name="damagePerTick">0.1s 틱당 데미지 (AbilityData.Damage)</param>
        /// <param name="tickInterval">틱 주기(초), 일반적으로 0.1f</param>
        /// <param name="bugLayer">대상 레이어</param>
        /// <param name="fireVfxPrefab">FloorTrapMolten 등 지속 화염 프리펩. null 허용</param>
        public void Initialize(
            Vector3 center, float radius, float duration,
            float damagePerTick, float tickInterval,
            LayerMask bugLayer, GameObject fireVfxPrefab)
        {
            _radius = Mathf.Max(0.1f, radius);
            _life = duration;
            _damagePerTick = damagePerTick;
            _tickInterval = Mathf.Max(0.01f, tickInterval);
            _tickTimer = 0f;
            _bugLayer = bugLayer;

            transform.position = center;
            transform.rotation = Quaternion.identity;

            BuildDecal();
            BuildFireVfx(fireVfxPrefab);
        }

        private void BuildDecal()
        {
            var decalGo = new GameObject("MeteorFireZoneDecal");
            decalGo.transform.SetParent(transform, false);
            decalGo.transform.localPosition = new Vector3(0f, DecalYOffset, 0f);

            _decal = decalGo.AddComponent<AbilityRangeDecal>();
            _decal.SetupMesh(AbilityDecalMeshBuilder.BuildCircle(1f, 48));
            decalGo.transform.localScale = new Vector3(_radius, 1f, _radius);
            _decal.SetTint(new Color(1f, 0.4f, 0.15f, 1f)); // 주황
        }

        private void BuildFireVfx(GameObject prefab)
        {
            if (prefab == null) return;

            _vfxWrapper = new GameObject("MeteorFireVfx");
            _vfxWrapper.transform.SetParent(transform, false);
            _vfxWrapper.transform.localPosition = Vector3.zero;
            _vfxWrapper.transform.localRotation = Quaternion.identity;

            var instance = Instantiate(prefab, _vfxWrapper.transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;

            // 반경에 맞춰 스케일 — FloorTrapMolten 은 원형 authoring (기준 반경 약 1 유닛).
            const float FireVfxReferenceRadius = 1f;
            float ratio = Mathf.Max(0.01f, _radius / FireVfxReferenceRadius);
            instance.transform.localScale *= ratio;

            // 네이팜 타일링 방식과 유사 — 자식 PS 전부 loop 강제 (FloorTrapMolten 은 보통 non-loop 3초).
            var systems = instance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                var main = systems[i].main;
                main.loop = true;
                if (!systems[i].isPlaying) systems[i].Play();
            }
        }

        private void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f)
            {
                Dispose();
                return;
            }

            _tickTimer -= Time.deltaTime;
            if (_tickTimer <= 0f)
            {
                _tickTimer += _tickInterval;
                ApplyTickDamage();
            }
        }

        private void ApplyTickDamage()
        {
            int hits = Physics.OverlapSphereNonAlloc(transform.position, _radius, _buffer, _bugLayer);
            for (int i = 0; i < hits; i++)
            {
                var col = _buffer[i];
                if (col == null) continue;
                if (col.TryGetComponent<IDamageable>(out var d))
                    d.TakeDamage(_damagePerTick);
            }
        }

        private void Dispose()
        {
            if (_decal != null) _decal.Dispose();   // 페이드아웃 후 자체 Destroy
            _decal = null;
            if (_vfxWrapper != null) Destroy(_vfxWrapper);
            _vfxWrapper = null;
            Destroy(gameObject);
        }
    }
}

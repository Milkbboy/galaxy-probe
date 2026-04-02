using UnityEngine;
using System.Collections.Generic;

namespace DrillCorp.Bug.Behaviors.Skill
{
    /// <summary>
    /// 아군 회복 스킬 (Aura 방식)
    /// 범위 내 아군 버그에게 주기적으로 HP 회복
    ///
    /// param1 = 범위
    /// param2 = 회복량 (회복 주기마다)
    /// cooldown = 회복 주기 (몇 초마다 회복)
    /// </summary>
    public class HealAllySkill : SkillBehaviorBase
    {
        private float _healAmount;
        private float _healInterval;
        private float _healTimer;
        private GameObject _effectPrefab;

        // Aura 표시용
        private GameObject _auraIndicator;
        private MeshRenderer _auraRenderer;

        // Physics 체크용
        private static LayerMask _bugLayerMask = -1;
        private Collider[] _overlapResults = new Collider[32];

        public HealAllySkill(float range = 5f, float healAmount = 10f, float healInterval = 1f, GameObject effectPrefab = null)
            : base(0f, range) // 쿨다운 0 (Aura는 쿨다운 사용 안 함)
        {
            _healAmount = healAmount > 0f ? healAmount : 10f;
            _healInterval = healInterval > 0f ? healInterval : 1f;
            _effectPrefab = effectPrefab;
            _healTimer = 0f;
        }

        public override void Initialize(BugController bug)
        {
            base.Initialize(bug);
            _healTimer = 0f;
            InitializeLayerMask();
            CreateAuraIndicator();
        }

        private void InitializeLayerMask()
        {
            if (_bugLayerMask == -1)
            {
                int bugLayer = LayerMask.NameToLayer("Bug");
                if (bugLayer != -1)
                {
                    _bugLayerMask = 1 << bugLayer;
                }
                else
                {
                    _bugLayerMask = Physics.DefaultRaycastLayers;
                }
            }
        }

        public override void Cleanup()
        {
            // Aura 인디케이터 제거
            if (_auraIndicator != null)
            {
                Object.Destroy(_auraIndicator);
            }

            base.Cleanup();
        }

        /// <summary>
        /// 매 프레임 Aura 범위 체크 및 힐 적용
        /// </summary>
        public void UpdateHealAura(float deltaTime)
        {
            if (_bug == null) return;

            // 인디케이터 위치 동기화
            UpdateAuraPosition();

            _healTimer += deltaTime;

            // 회복 주기마다 범위 내 버그 회복
            if (_healTimer >= _healInterval)
            {
                _healTimer = 0f;
                HealBugsInRange();
            }
        }

        /// <summary>
        /// 범위 내 아군 버그 회복 (Physics.OverlapSphereNonAlloc 사용)
        /// </summary>
        private void HealBugsInRange()
        {
            Vector3 center = _bug.transform.position;
            int count = Physics.OverlapSphereNonAlloc(center, _range, _overlapResults, _bugLayerMask);

            for (int i = 0; i < count; i++)
            {
                var bugController = _overlapResults[i].GetComponent<BugController>();
                if (bugController == null) continue;
                if (bugController == _bug) continue; // 자신 제외
                if (bugController.IsDead) continue;
                if (bugController.CurrentHp >= bugController.MaxHp) continue; // 풀피면 스킵

                bugController.Heal(_healAmount);

                // 이펙트 재생
                if (_effectPrefab != null)
                {
                    var effect = Object.Instantiate(_effectPrefab, bugController.transform.position, Quaternion.identity);
                    Object.Destroy(effect, 2f);
                }
            }
        }

        /// <summary>
        /// Aura 범위 표시 인디케이터 생성 (Cylinder로 XZ 평면 원형)
        /// </summary>
        private void CreateAuraIndicator()
        {
            _auraIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _auraIndicator.name = "HealAuraIndicator";
            // 부모에 붙이지 않고 독립적으로 관리 (회전/스케일 영향 안 받음)

            // 콜라이더 제거
            var collider = _auraIndicator.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            // 스케일: 지름 = range * 2, 높이 = 0.05 (납작하게)
            // Cylinder 기본 높이는 2, 반경은 0.5이므로:
            // X, Z = range * 2 (지름), Y = 0.025 (높이 0.05)
            _auraIndicator.transform.localScale = new Vector3(_range * 2f, 0.025f, _range * 2f);

            // 머티리얼 설정 (녹색 반투명 - 회복 표시)
            _auraRenderer = _auraIndicator.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(0.2f, 1f, 0.4f, 0.02f); // 녹색 반투명
            _auraRenderer.material = mat;
            _auraRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _auraRenderer.receiveShadows = false;
        }

        /// <summary>
        /// Aura 인디케이터 위치 동기화
        /// </summary>
        private void UpdateAuraPosition()
        {
            if (_auraIndicator != null && _bug != null)
            {
                // Y = 0.05로 약간 띄움
                _auraIndicator.transform.position = _bug.transform.position + new Vector3(0f, 0.05f, 0f);
            }
        }

        // SkillBehaviorBase의 TryUse는 사용하지 않음 (Aura 방식)
        protected override void UseSkill(Transform target)
        {
            // Aura는 매 프레임 UpdateHealAura()로 동작
        }
    }
}

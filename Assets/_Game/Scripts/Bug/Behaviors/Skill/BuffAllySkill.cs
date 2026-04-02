using UnityEngine;
using System.Collections.Generic;

namespace DrillCorp.Bug.Behaviors.Skill
{
    /// <summary>
    /// 아군 강화 스킬 (Aura 방식)
    /// 범위 내 아군 버그에게 공격력/이속 버프 적용
    /// 범위를 벗어나면 버프 해제
    ///
    /// param1 = 범위
    /// param2 = 공격력 배율
    /// cooldown = 이속 배율로 사용 (기존 구조 활용)
    /// </summary>
    public class BuffAllySkill : SkillBehaviorBase
    {
        private float _damageMultiplier;
        private float _speedMultiplier;
        private HashSet<BugController> _buffedBugs = new HashSet<BugController>();
        private GameObject _effectPrefab;

        // Aura 표시용
        private GameObject _auraIndicator;
        private MeshRenderer _auraRenderer;

        // Physics 체크용
        private static LayerMask _bugLayerMask = -1;
        private Collider[] _overlapResults = new Collider[32];

        public BuffAllySkill(float range = 5f, float damageMultiplier = 1.3f, float speedMultiplier = 1.2f, GameObject effectPrefab = null)
            : base(0f, range) // 쿨다운 0 (Aura는 쿨다운 사용 안 함)
        {
            _damageMultiplier = damageMultiplier > 0f ? damageMultiplier : 1.3f;
            _speedMultiplier = speedMultiplier > 0f ? speedMultiplier : 1.2f;
            _effectPrefab = effectPrefab;
        }

        public override void Initialize(BugController bug)
        {
            base.Initialize(bug);
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
            // 모든 버프 해제
            foreach (var buffedBug in _buffedBugs)
            {
                if (buffedBug != null)
                {
                    buffedBug.RemoveBuff(this);
                }
            }
            _buffedBugs.Clear();

            // Aura 인디케이터 제거
            if (_auraIndicator != null)
            {
                Object.Destroy(_auraIndicator);
            }

            base.Cleanup();
        }

        /// <summary>
        /// 매 프레임 Aura 범위 체크 (BugController.Update에서 호출)
        /// </summary>
        public void UpdateAura()
        {
            if (_bug == null) return;

            // 인디케이터 위치 동기화
            UpdateAuraPosition();

            // 범위 내 버그 탐색 (Physics.OverlapSphere 사용)
            var bugsInRange = FindBugsInRange();

            // 범위 밖으로 나간 버그 → 버프 해제
            var toRemove = new List<BugController>();
            foreach (var buffedBug in _buffedBugs)
            {
                if (buffedBug == null || !bugsInRange.Contains(buffedBug))
                {
                    toRemove.Add(buffedBug);
                }
            }
            foreach (var bug in toRemove)
            {
                if (bug != null)
                {
                    bug.RemoveBuff(this);
                }
                _buffedBugs.Remove(bug);
            }

            // 범위 안으로 들어온 버그 → 버프 적용
            foreach (var bug in bugsInRange)
            {
                if (!_buffedBugs.Contains(bug))
                {
                    bug.ApplyBuff(this, _damageMultiplier, _speedMultiplier);
                    _buffedBugs.Add(bug);
                }
            }
        }

        /// <summary>
        /// 범위 내 아군 버그 탐색 (Physics.OverlapSphereNonAlloc 사용)
        /// </summary>
        private HashSet<BugController> FindBugsInRange()
        {
            var result = new HashSet<BugController>();
            Vector3 center = _bug.transform.position;

            int count = Physics.OverlapSphereNonAlloc(center, _range, _overlapResults, _bugLayerMask);

            for (int i = 0; i < count; i++)
            {
                var bugController = _overlapResults[i].GetComponent<BugController>();
                if (bugController == null) continue;
                if (bugController == _bug) continue; // 자신 제외
                if (bugController.IsDead) continue;

                result.Add(bugController);
            }

            return result;
        }

        /// <summary>
        /// Aura 범위 표시 인디케이터 생성 (Cylinder로 XZ 평면 원형)
        /// </summary>
        private void CreateAuraIndicator()
        {
            _auraIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _auraIndicator.name = "BuffAuraIndicator";
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

            // 머티리얼 설정 (황금색 반투명)
            _auraRenderer = _auraIndicator.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(1f, 0.85f, 0.2f, 0.02f); // 황금색 반투명
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
            // Aura는 매 프레임 UpdateAura()로 동작
        }
    }
}

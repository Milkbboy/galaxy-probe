using UnityEngine;
using DrillCorp.Core;

namespace DrillCorp.Bug.Behaviors.Attack
{
    /// <summary>
    /// 지속 레이저 공격
    /// param1 = 지속시간 (초, 기본 2초)
    /// param2 = 틱간격 (초, 기본 0.5초)
    /// </summary>
    public class BeamAttack : AttackBehaviorBase
    {
        private float _duration;
        private float _tickInterval;
        private GameObject _beamVfxPrefab;

        // 활성 빔 상태
        private GameObject _activeBeam;
        private LineRenderer _lineRenderer;
        private Transform _currentTarget;
        private float _beamTimer;
        private float _tickTimer;
        private bool _isBeamActive;

        // 폴백 빔 설정
        private const float BEAM_WIDTH = 0.15f;
        private static readonly Color BEAM_COLOR_START = new Color(1f, 0.2f, 0.1f, 1f);
        private static readonly Color BEAM_COLOR_END = new Color(1f, 0.5f, 0.2f, 0.8f);

        public bool IsBeamActive => _isBeamActive;

        public BeamAttack(float duration = 2f, float tickInterval = 0.5f, GameObject beamVfxPrefab = null)
        {
            _duration = duration > 0f ? duration : 2f;
            _tickInterval = tickInterval > 0f ? tickInterval : 0.5f;
            _beamVfxPrefab = beamVfxPrefab;
        }

        public override void Initialize(BugController bug)
        {
            base.Initialize(bug);
            // BugData.AttackRange 그대로 사용
        }

        public override void Cleanup()
        {
            StopBeam();
            base.Cleanup();
        }

        protected override void PerformAttack(Transform target)
        {
            if (_isBeamActive) return;

            _currentTarget = target;
            StartBeam();
        }

        /// <summary>
        /// 빔 시작
        /// </summary>
        private void StartBeam()
        {
            _isBeamActive = true;
            _beamTimer = _duration;
            _tickTimer = 0f; // 즉시 첫 데미지

            if (_beamVfxPrefab != null)
            {
                _activeBeam = Object.Instantiate(_beamVfxPrefab, _bug.transform.position, Quaternion.identity);
                _lineRenderer = _activeBeam.GetComponent<LineRenderer>();
            }
            else
            {
                CreateFallbackBeam();
            }

            UpdateBeamPositions();
        }

        /// <summary>
        /// 폴백 빔 생성 (LineRenderer)
        /// </summary>
        private void CreateFallbackBeam()
        {
            _activeBeam = new GameObject("BeamVFX");
            _activeBeam.transform.SetParent(_bug.transform);
            _activeBeam.transform.localPosition = Vector3.zero;

            _lineRenderer = _activeBeam.AddComponent<LineRenderer>();
            _lineRenderer.positionCount = 2;
            _lineRenderer.startWidth = BEAM_WIDTH;
            _lineRenderer.endWidth = BEAM_WIDTH * 0.7f;

            // 머티리얼 설정
            var mat = new Material(Shader.Find("Sprites/Default"));
            _lineRenderer.material = mat;
            _lineRenderer.startColor = BEAM_COLOR_START;
            _lineRenderer.endColor = BEAM_COLOR_END;

            // 정렬
            _lineRenderer.sortingOrder = 100;
        }

        /// <summary>
        /// 빔 종료
        /// </summary>
        private void StopBeam()
        {
            _isBeamActive = false;
            _currentTarget = null;

            if (_activeBeam != null)
            {
                Object.Destroy(_activeBeam);
                _activeBeam = null;
                _lineRenderer = null;
            }
        }

        /// <summary>
        /// 빔 위치 갱신
        /// </summary>
        private void UpdateBeamPositions()
        {
            if (_lineRenderer == null || _bug == null || _currentTarget == null) return;

            Vector3 startPos = GetBeamOrigin();
            Vector3 endPos = GetTargetHitPoint(startPos);

            _lineRenderer.SetPosition(0, startPos);
            _lineRenderer.SetPosition(1, endPos);
        }

        /// <summary>
        /// 빔 시작점 (FX_Socket 또는 버그 위치)
        /// </summary>
        private Vector3 GetBeamOrigin()
        {
            Transform fxSocket = _bug.transform.Find("FX_Socket");
            if (fxSocket != null)
            {
                return fxSocket.position;
            }
            // 폴백: 버그 위치 + 높이 보정
            return _bug.transform.position + new Vector3(0f, 0.5f, 0f);
        }

        /// <summary>
        /// 타겟 콜라이더의 가장 가까운 점 계산
        /// </summary>
        private Vector3 GetTargetHitPoint(Vector3 fromPos)
        {
            var collider = _currentTarget.GetComponent<Collider>();
            if (collider == null) return _currentTarget.position;

            // 버그에서 타겟 방향으로 Raycast (트리거 콜라이더도 감지)
            Vector3 direction = (_currentTarget.position - fromPos).normalized;
            float maxDistance = Vector3.Distance(fromPos, _currentTarget.position) + 10f;

            if (Physics.Raycast(fromPos, direction, out RaycastHit hit, maxDistance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            {
                if (hit.transform == _currentTarget || hit.transform.IsChildOf(_currentTarget))
                {
                    return hit.point;
                }
            }

            // 폴백: Bounds의 가장 가까운 점
            return collider.bounds.ClosestPoint(fromPos);
        }

        /// <summary>
        /// 매 프레임 업데이트 (BugController에서 호출)
        /// </summary>
        public void UpdateBeam(float deltaTime)
        {
            if (!_isBeamActive) return;

            // 타이머 감소
            _beamTimer -= deltaTime;
            _tickTimer -= deltaTime;

            // 빔 종료 체크
            if (_beamTimer <= 0f)
            {
                StopBeam();
                return;
            }

            // 타겟 유효성 체크
            if (_currentTarget == null)
            {
                StopBeam();
                return;
            }

            // 빔 위치 갱신
            UpdateBeamPositions();

            // 틱 데미지
            if (_tickTimer <= 0f)
            {
                ApplyTickDamage();
                _tickTimer = _tickInterval;
            }

            // 빔 펄스 효과 (크기 변동)
            if (_lineRenderer != null)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 15f) * 0.2f;
                _lineRenderer.startWidth = BEAM_WIDTH * pulse;
                _lineRenderer.endWidth = BEAM_WIDTH * 0.7f * pulse;
            }
        }

        /// <summary>
        /// 틱 데미지 적용
        /// </summary>
        private void ApplyTickDamage()
        {
            if (_currentTarget == null) return;

            float damage = GetDamage();
            DealDamage(_currentTarget, damage);

            // Hit VFX (빔 끝점 - 콜라이더 표면)
            Vector3 hitPos = GetTargetHitPoint(_bug.transform.position);
            hitPos.y += 0.5f; // 높이 보정
            PlayHitVfx(hitPos);
        }
    }
}

using UnityEngine;

namespace DrillCorp.VFX
{
    /// <summary>
    /// 레이저 스코치 페이드 — 루프형 파티클(PA DamageAura 등)에 부착해 시간 제한 부여.
    ///
    /// 동작: stopAfter 초에 자식 ParticleSystem 전부 StopEmitting
    ///       (기존 파티클은 자연 소멸, 새 방출 없음)
    ///       → totalLifetime 초에 GameObject 파괴
    ///
    /// 즉각 Destroy로 자르면 파티클이 뚝 끊겨 보임 → 방출만 끊고 기존 입자 fade 보전.
    /// </summary>
    public class LaserScorchDecay : MonoBehaviour
    {
        [Tooltip("이 시간 후 파티클 방출만 정지 (기존 파티클은 계속 자연 소멸)")]
        [SerializeField] private float _stopAfter = 6f;

        [Tooltip("이 시간 후 GameObject 완전 파괴")]
        [SerializeField] private float _totalLifetime = 8f;

        // 추적 대상 — 빔 Transform. null이 되거나 파괴되면 자동으로 현재 위치에서 멈춤.
        // 자식으로 붙이지 않는 이유: 빔이 Destroy되면 자식도 같이 사라져 잔상 fade가 끊김.
        private Transform _followTarget;

        public void Initialize(float stopAfter, float totalLifetime)
        {
            _stopAfter = Mathf.Max(0.01f, stopAfter);
            _totalLifetime = Mathf.Max(_stopAfter + 0.1f, totalLifetime);
        }

        /// <summary>
        /// 빔 Transform을 설정하면 LateUpdate에서 위치를 계속 따라감.
        /// 빔이 파괴되면 마지막 위치에서 정지해 잔상 fade 완료.
        /// </summary>
        public void SetFollowTarget(Transform target)
        {
            _followTarget = target;
        }

        private void Start()
        {
            Invoke(nameof(StopEmission), _stopAfter);
            Destroy(gameObject, _totalLifetime);
        }

        private void LateUpdate()
        {
            // Unity 객체 비교 — 파괴된 Transform은 != null 에서 false로 평가됨
            if (_followTarget != null)
            {
                transform.position = _followTarget.position;
            }
        }

        private void StopEmission()
        {
            var systems = GetComponentsInChildren<ParticleSystem>(includeInactive: true);
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] != null)
                    systems[i].Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }
}

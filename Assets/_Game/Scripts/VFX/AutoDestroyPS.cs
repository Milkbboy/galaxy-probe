using UnityEngine;

namespace DrillCorp.VFX
{
    /// <summary>
    /// ParticleSystem 이 완전히 종료되면 GameObject 파괴.
    /// Polygon Arsenal 등 외부 파티클 프리펩 Variant 에 부착해 수명 자동 관리.
    /// AutoDestroy(타이머) 와 달리 파티클 실제 종료 시점 기준 — Lifetime 하드코딩 불필요.
    ///
    /// 누수 방지 처리:
    ///   1) 루트 + 자식 PS 모두 `stopAction = Callback` 으로 설정 → 자식이 더 오래 살아있을 때도 안전
    ///   2) 루트 PS 의 `cullingMode = AlwaysSimulate` 로 강제 — 화면 밖이어도 시뮬레이션 계속
    ///      (기본값 PauseAndCatchup 은 frustum 밖에서 시뮬레이션 정지 → 종료 안 됨 → GameObject 누적)
    ///   3) 폴백 타이머 — `lengthInSec + 최대 startLifetime + 1s` 경과 후에도 파괴 안 됐으면 강제 Destroy
    ///      (서브 이미터/트레일/느슨한 종료 조건 회피)
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class AutoDestroyPS : MonoBehaviour
    {
        [Tooltip("안전 폴백 타이머 (초). 이 시간 안에 파티클이 정상 종료 안 되면 강제 파괴. " +
                 "0 이면 폴백 없음. 기본 10s — 루프 VFX 는 이 컴포넌트 부착 금지.")]
        [SerializeField] private float _maxLifetimeSec = 10f;

        private bool _destroyed;

        private void Awake()
        {
            var systems = GetComponentsInChildren<ParticleSystem>(true);
            float longestLife = 0f;

            // 서브 이미터로 지정된 PS 수집 — stopAction 적용 금지
            // ("Sub-emitters may not use stop actions" 경고 회피)
            var subEmitters = new System.Collections.Generic.HashSet<ParticleSystem>();
            foreach (var ps in systems)
            {
                var sub = ps.subEmitters;
                if (!sub.enabled) continue;
                int count = sub.subEmittersCount;
                for (int i = 0; i < count; i++)
                {
                    var child = sub.GetSubEmitterSystem(i);
                    if (child != null) subEmitters.Add(child);
                }
            }

            foreach (var ps in systems)
            {
                var main = ps.main;
                if (!subEmitters.Contains(ps))
                    main.stopAction = ParticleSystemStopAction.Callback;
                // 화면 밖에서도 시뮬레이션 계속 → 정상 종료 보장
                main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

                float life = main.duration
                    + (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants
                        ? main.startLifetime.constantMax
                        : main.startLifetime.constant);
                if (life > longestLife) longestLife = life;
            }

            // 폴백 타이머 — startLifetime 기준 + 여유 1s. 사용자가 지정한 상한보다 작을 때만.
            if (_maxLifetimeSec > 0f)
            {
                float fallback = Mathf.Min(_maxLifetimeSec, longestLife + 1f);
                if (fallback > 0f) Invoke(nameof(ForceDestroy), fallback);
            }
        }

        private void OnParticleSystemStopped()
        {
            if (_destroyed) return;
            _destroyed = true;
            Destroy(gameObject);
        }

        private void ForceDestroy()
        {
            if (_destroyed) return;
            if (this == null) return;
            _destroyed = true;
            Destroy(gameObject);
        }
    }
}

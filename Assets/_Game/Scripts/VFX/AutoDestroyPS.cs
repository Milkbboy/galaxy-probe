using UnityEngine;

namespace DrillCorp.VFX
{
    /// <summary>
    /// ParticleSystem이 완전히 종료되면 GameObject 파괴.
    /// Polygon Arsenal 등 외부 파티클 프리펩 Variant에 부착해 수명 자동 관리.
    /// AutoDestroy(타이머) 와 달리 파티클 실제 종료 시점 기준 — Lifetime 하드코딩 불필요.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class AutoDestroyPS : MonoBehaviour
    {
        private void Awake()
        {
            var ps = GetComponent<ParticleSystem>();
            var main = ps.main;
            main.stopAction = ParticleSystemStopAction.Callback;
        }

        private void OnParticleSystemStopped()
        {
            Destroy(gameObject);
        }
    }
}

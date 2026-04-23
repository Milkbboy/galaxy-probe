using UnityEngine;

namespace DrillCorp.VFX.Pool
{
    /// <summary>
    /// 풀링되는 VFX 프리팹 루트에 부착하는 컴포넌트.
    ///
    /// 책임:
    ///   1) Awake 1회 — 루트+자식 PS 전부 `cullingMode=AlwaysSimulate` + `stopAction=Callback` 강제
    ///      → 화면 밖에서도 시뮬레이션 계속 → `OnParticleSystemStopped` 정상 호출
    ///   2) OnEnable — 풀에서 꺼낼 때마다 파티클 재시작 (Clear + Play)
    ///   3) OnParticleSystemStopped — 파티클 완전 종료 시 VfxPool 로 반환
    ///   4) 폴백 타이머 — 콜백 누락 시 10s 후 강제 반환 (루프 VFX 방어)
    ///
    /// Bind(pool, prefabKey) 로 VfxPool 이 소유권/프리팹 키를 주입. 프리팹 키는 반환 시
    /// 어떤 풀에 넣을지 역추적하는 데 필요.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class PooledVfx : MonoBehaviour
    {
        [Tooltip("콜백 누락 대비 폴백 타이머(초). 이 시간 안에 OnParticleSystemStopped 가 안 오면 강제 반환.")]
        [SerializeField] private float _fallbackSeconds = 10f;

        private VfxPool _pool;
        private GameObject _prefabKey;
        private bool _returned;
        private bool _setupDone;

        internal void Bind(VfxPool pool, GameObject prefabKey)
        {
            _pool = pool;
            _prefabKey = prefabKey;
        }

        private void Awake()
        {
            EnsureSetup();
        }

        // cullingMode/stopAction 세팅은 첫 Awake 1회면 충분하지만,
        // 에디터에서 부착 없이 프리팹만 교체한 경우 Bind 전에 Awake 가 호출될 수 있어 idempotent 하게 보호.
        private void EnsureSetup()
        {
            if (_setupDone) return;
            var systems = GetComponentsInChildren<ParticleSystem>(true);

            // 서브 이미터로 지정된 PS 수집 — stopAction 적용 금지 대상
            // (서브 이미터는 부모 이벤트로 트리거돼 독립 Stop 불가. "Sub-emitters may not use stop actions" 경고 원인)
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
                main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
                // stopAction 은 서브 이미터 아닌 PS 에만 — Callback 여러 번 와도 ReturnToPool 가드(_returned)로 안전.
                if (!subEmitters.Contains(ps))
                    main.stopAction = ParticleSystemStopAction.Callback;
                // 풀링 패턴에서는 loop 를 쓰면 안 됨 (자동 반환 타이밍 불명확).
                // 의도적인 looping VFX 는 풀링 대상에서 제외해야 한다.
            }
            _setupDone = true;
        }

        private void OnEnable()
        {
            _returned = false;
            EnsureSetup();

            var systems = GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in systems)
            {
                ps.Clear(true);
                ps.Play(true);
            }

            if (_fallbackSeconds > 0f)
            {
                CancelInvoke(nameof(ForceReturn));
                Invoke(nameof(ForceReturn), _fallbackSeconds);
            }
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(ForceReturn));
        }

        private void OnParticleSystemStopped()
        {
            ReturnToPool();
        }

        private void ForceReturn()
        {
            ReturnToPool();
        }

        private void ReturnToPool()
        {
            if (_returned) return;
            if (!gameObject.activeSelf) return;   // 이미 반환된 상태
            _returned = true;

            if (_pool != null && _prefabKey != null)
            {
                _pool.ReturnInternal(gameObject, _prefabKey);
            }
            else
            {
                // Bind 되지 않은 경로(예외적) — 누수 방지 위해 일반 Destroy 로 폴백
                Destroy(gameObject);
            }
        }
    }
}

using System;
using UnityEngine;
using DrillCorp.Core;
using DrillCorp.Data;
using DrillCorp.Machine;
using DrillCorp.UI;

namespace DrillCorp.Ability.Runners
{
    /// <summary>
    /// 지누스 채굴 드론 본체 — 수명 10초짜리 채굴 버프 유닛.
    /// MiningDroneRunner 가 Instantiate 직후 <see cref="Initialize"/> 로 의존성을 주입한다.
    ///
    /// v2.html:1296~1303 포팅:
    ///   1. _life(= data.DurationSec) 카운트다운 — 0 이하면 Destroy
    ///   2. 매 프레임 machine.AddBonusMining(_miningRatePerSec * dt) — v2 `mineAmt += 5/60 * dt`
    ///   3. _gemRollInterval(기본 1초) 주기로 Random &lt; _gemChance 검사 → 성공 시
    ///      GameEvents.OnGemCollected?.Invoke(1) 발행 (MachineController 가 세션 보석 누적)
    ///
    /// 공격·HP 없음. 시각 표시는 프리펩 Body(CrystalGrowthGreen) VFX 가 담당.
    /// </summary>
    public class MiningDroneInstance : MonoBehaviour
    {
        [Header("Visual")]
        [Tooltip("몸체 VFX 자식 Transform (CrystalGrowthGreen 등).")]
        [SerializeField] private Transform _bodyTransform;

        [Header("Mining (Runner 기본값 — v2 원본값)")]
        [Tooltip("초당 채굴량. v2 원본 5/60 × 60 = 5/sec.")]
        [SerializeField] private float _miningRatePerSec = 5f;

        [Tooltip("보석 롤 주기(초). v2 원본 60프레임 = 1초.")]
        [SerializeField] private float _gemRollInterval = 1f;

        [Tooltip("보석 롤 성공 확률(0~1). v2 원본 0.1.")]
        [Range(0f, 1f)]
        [SerializeField] private float _gemChance = 0.1f;

        [Header("Timer Visual (v2.html:1643~1646 포팅)")]
        [Tooltip("원호 타이머 표시 여부. off 시 Body VFX 만 뜸.")]
        [SerializeField] private bool _showTimer = true;

        [Tooltip("원호 반경(유닛). 본체보다 살짝 크게.")]
        [SerializeField] private float _timerArcRadius = 1.4f;

        [Tooltip("원호 선 두께(유닛). 탑뷰 가시성 기준.")]
        [SerializeField] private float _timerLineWidth = 0.15f;

        [Tooltip("타이머 전체 오프셋. Z+ = 탑뷰 화면 위쪽, Y+ = 지면 위로 약간 띄움.")]
        [SerializeField] private Vector3 _timerOffset = new Vector3(0f, 0.05f, 0f);

        [Tooltip("원호·라벨 색상 (지누스 초록).")]
        [SerializeField] private Color _timerColor = new Color(0.32f, 0.81f, 0.4f, 1f);

        [Header("Runtime Body (실체감용)")]
        [Tooltip("런타임에 지면 디스크 본체를 생성할지. 프리펩 Body(CrystalGrowthGreen) 만으로는 지속 가시성이 없어 기본 ON.")]
        [SerializeField] private bool _buildRuntimeBody = true;

        [Tooltip("디스크 반경(유닛).")]
        [SerializeField] private float _bodyDiscRadius = 0.8f;

        [Tooltip("디스크 두께(유닛).")]
        [SerializeField] private float _bodyDiscHeight = 0.12f;

        [Tooltip("디스크 색 (밝은 초록).")]
        [SerializeField] private Color _bodyDiscColor = new Color(0.32f, 0.81f, 0.4f, 1f);

        // ─── runtime state ───
        private AbilityData _abilityData;
        private MachineController _machine;
        private float _lifeRemaining;
        private float _totalDuration;
        private float _gemRollTimer;
        private MiningDroneTimer3D _timer;
        private int _lastDisplayedSeconds = -1;

        /// <summary>수명 종료/Destroy 시 발행. Runner 가 리스트에서 제거.</summary>
        public event Action OnDestroyed;

        public void Initialize(AbilityData data, MachineController machine)
        {
            _abilityData = data;
            _machine = machine;

            // Duration 이 0 이면 기본 10초 (v2 원본) 로 fallback — SO 값 누락 방어.
            _totalDuration = (data != null && data.DurationSec > 0f) ? data.DurationSec : 10f;
            _lifeRemaining = _totalDuration;
            _gemRollTimer = _gemRollInterval;

            if (_buildRuntimeBody) BuildRuntimeBody();
            if (_showTimer) BuildTimer();
        }

        private void BuildRuntimeBody()
        {
            // 바닥 디스크 — 얇은 Cylinder. Cylinder primitive 는 기본 지름 1, 높이 2이므로 scale 로 조정.
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "RuntimeBase";
            if (disc.TryGetComponent<Collider>(out var discCol)) Destroy(discCol);
            disc.transform.SetParent(transform, false);
            disc.transform.localPosition = new Vector3(0f, _bodyDiscHeight * 0.5f, 0f);
            // Cylinder primitive 높이축 = 로컬 Y, scale(X,Y,Z) 중 X·Z 가 반지름 영향 (둘 다 같게).
            float discD = _bodyDiscRadius * 2f;
            disc.transform.localScale = new Vector3(discD, _bodyDiscHeight * 0.5f, discD);
            ApplyUnlit(disc.GetComponent<Renderer>(), _bodyDiscColor);
        }

        private static void ApplyUnlit(Renderer r, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            var mat = new Material(shader) { name = "MiningDroneBody_Runtime" };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            mat.color = color;
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        private void BuildTimer()
        {
            _timer = MiningDroneTimer3D.Create(
                target: transform,
                offset: _timerOffset,
                arcRadius: _timerArcRadius,
                lineWidth: _timerLineWidth,
                color: _timerColor);
            _timer.SetProgress(1f);
            _timer.SetSeconds(Mathf.CeilToInt(_lifeRemaining));
            _lastDisplayedSeconds = Mathf.CeilToInt(_lifeRemaining);
        }

        private void Update()
        {
            if (_lifeRemaining <= 0f) return;

            float dt = Time.deltaTime;

            // 수명 감소
            _lifeRemaining -= dt;
            if (_lifeRemaining <= 0f)
            {
                DestroySelf();
                return;
            }

            // 타이머 시각 갱신 — 원호는 매 프레임, 초 텍스트는 정수 바뀔 때만.
            if (_timer != null && _totalDuration > 0f)
            {
                _timer.SetProgress(_lifeRemaining / _totalDuration);
                int sec = Mathf.CeilToInt(_lifeRemaining);
                if (sec != _lastDisplayedSeconds)
                {
                    _timer.SetSeconds(sec);
                    _lastDisplayedSeconds = sec;
                }
            }

            // 채굴 훅 — MachineController._miningAccumulator 에 누적 (정수화는 Machine.Update에서)
            if (_machine != null) _machine.AddBonusMining(_miningRatePerSec * dt);

            // 보석 롤 — 주기마다 1회 Random 검사
            _gemRollTimer -= dt;
            if (_gemRollTimer <= 0f)
            {
                _gemRollTimer += _gemRollInterval;
                if (_gemChance > 0f && UnityEngine.Random.value < _gemChance)
                {
                    // MachineController.OnGemCollected 구독이 _sessionGemsCollected 누적 + HUD 갱신 담당.
                    GameEvents.OnGemCollected?.Invoke(1);
                }
            }
        }

        private void DestroySelf()
        {
            if (_timer != null)
            {
                // 타이머는 target=drone transform 을 LateUpdate 추적 중이라 드론이 사라지면 자체 Destroy 되지만,
                // 같은 프레임에 정리하려면 명시적 Destroy 가 안전.
                Destroy(_timer.gameObject);
                _timer = null;
            }
            OnDestroyed?.Invoke();
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            // 외부 요인(씬 종료 등)으로 파괴된 경우에도 Runner 가 리스트 정리하도록 신호.
            // DestroySelf 경로와 중복 발행될 수 있지만 Runner 의 Remove 호출은 idempotent.
            OnDestroyed?.Invoke();
        }
    }
}

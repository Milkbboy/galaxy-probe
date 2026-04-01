using UnityEngine;
using DrillCorp.Machine;
using DrillCorp.UI;

namespace DrillCorp.Bug.Behaviors.Passive
{
    /// <summary>
    /// 독 공격 패시브 - 공격 시 대상에게 독 데미지 적용
    /// param1 = 독 지속시간 (기본 3초)
    /// param2 = 초당 독 데미지 (기본 5)
    /// </summary>
    public class PoisonAttackPassive : PassiveBehaviorBase
    {
        private float _poisonDuration;
        private float _poisonDamagePerSecond;

        public PoisonAttackPassive(float duration = 3f, float damagePerSecond = 5f)
        {
            _poisonDuration = duration > 0f ? duration : 3f;
            _poisonDamagePerSecond = damagePerSecond > 0f ? damagePerSecond : 5f;
        }

        public override void ProcessOutgoingDamage(float damage, Transform target)
        {
            // Debug.Log($"[PoisonAttackPassive] ProcessOutgoingDamage called! target={target?.name}");

            if (target == null) return;

            // 타겟에 PoisonEffect 컴포넌트 추가/갱신
            var poisonEffect = target.GetComponent<PoisonEffect>();
            if (poisonEffect == null)
            {
                Debug.Log($"[PoisonAttackPassive] Adding PoisonEffect to {target.name}");
                poisonEffect = target.gameObject.AddComponent<PoisonEffect>();
            }

            poisonEffect.ApplyPoison(_poisonDuration, _poisonDamagePerSecond);
        }
    }

    /// <summary>
    /// 독 효과 컴포넌트 - 대상에 부착되어 지속 데미지
    /// </summary>
    public class PoisonEffect : MonoBehaviour
    {
        private float _duration;
        private float _damagePerSecond;
        private float _tickInterval = 0.5f;
        private float _tickTimer;
        private IDamageable _damageable;

        // 시각 효과 (MaterialPropertyBlock 사용 - 원본 머티리얼 보존)
        private Renderer[] _renderers;
        private Color[] _originalColors;
        private MaterialPropertyBlock _propBlock;
        private static readonly Color PoisonColor = new Color(0.4f, 0.8f, 0.2f); // 독 녹색
        private float _flashTimer;

        private void Start()
        {
            _damageable = GetComponent<IDamageable>();
            _propBlock = new MaterialPropertyBlock();
            CacheRenderers();
        }

        private void CacheRenderers()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            _originalColors = new Color[_renderers.Length];

            Debug.Log($"[PoisonEffect] Found {_renderers.Length} renderers on {gameObject.name}");

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;

                var mat = _renderers[i].sharedMaterial;
                Debug.Log($"[PoisonEffect] Renderer[{i}]: {_renderers[i].name}, Material: {mat?.name}, HasBaseColor: {mat?.HasProperty("_BaseColor")}, HasColor: {mat?.HasProperty("_Color")}");

                // 현재 PropertyBlock에서 색상 가져오기 (없으면 머티리얼에서)
                _renderers[i].GetPropertyBlock(_propBlock);

                if (mat != null && mat.HasProperty("_BaseColor"))
                {
                    _originalColors[i] = mat.GetColor("_BaseColor");
                }
                else if (mat != null && mat.HasProperty("_Color"))
                {
                    _originalColors[i] = mat.color;
                }
                else
                {
                    _originalColors[i] = Color.white;
                }
            }
        }

        public void ApplyPoison(float duration, float damagePerSecond)
        {
            // 새 독이 더 강하거나 지속시간이 길면 갱신
            if (duration > _duration || damagePerSecond > _damagePerSecond)
            {
                _duration = duration;
                _damagePerSecond = damagePerSecond;
            }
            else
            {
                // 지속시간만 갱신 (스택되지 않음)
                _duration = Mathf.Max(_duration, duration);
            }
        }

        private void Update()
        {
            if (_duration <= 0f)
            {
                RestoreColors();
                Destroy(this);
                return;
            }

            _duration -= Time.deltaTime;
            _tickTimer += Time.deltaTime;

            // 틱당 데미지
            if (_tickTimer >= _tickInterval)
            {
                _tickTimer = 0f;
                DealPoisonDamage();
            }

            // 깜빡임 효과
            UpdateFlash();
        }

        private void UpdateFlash()
        {
            _flashTimer += Time.deltaTime * 4f; // 깜빡임 속도
            float t = (Mathf.Sin(_flashTimer) + 1f) * 0.5f; // 0~1 사이 값

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;

                Color lerpedColor = Color.Lerp(_originalColors[i], PoisonColor, t * 0.6f);

                _renderers[i].GetPropertyBlock(_propBlock);

                // URP는 _BaseColor, Built-in은 _Color
                if (_renderers[i].sharedMaterial.HasProperty("_BaseColor"))
                {
                    _propBlock.SetColor("_BaseColor", lerpedColor);
                }
                else
                {
                    _propBlock.SetColor("_Color", lerpedColor);
                }

                _renderers[i].SetPropertyBlock(_propBlock);
            }
        }

        private void RestoreColors()
        {
            if (_renderers == null) return;

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;

                // PropertyBlock 초기화 (원본 머티리얼로 복구)
                _renderers[i].SetPropertyBlock(null);
            }
        }

        private void DealPoisonDamage()
        {
            if (_damageable == null)
            {
                _damageable = GetComponent<IDamageable>();
            }

            if (_damageable != null)
            {
                float tickDamage = _damagePerSecond * _tickInterval;
                _damageable.TakeDamage(tickDamage);

                // 독 데미지 팝업 (보라색)
                DamagePopup.CreateText(transform.position, $"{tickDamage:F0}", new Color(0.6f, 0.2f, 0.8f));
            }
        }

        private void OnDestroy()
        {
            RestoreColors();
        }
    }
}

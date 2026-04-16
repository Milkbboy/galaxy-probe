using UnityEngine;

namespace DrillCorp.Weapon.Bomb
{
    /// <summary>
    /// 폭탄 폭발 VFX (스프라이트 기반 radial burst)
    /// - 시작 작게 → 빠르게 커지면서(ease-out) 알파 페이드 → 자동 파괴
    /// - 완전 자립형: BombProjectile.Detonate가 Instantiate하고 잊으면 됨
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class BombExplosionFx : MonoBehaviour
    {
        [Tooltip("총 지속 시간 (초)")]
        [Range(0.1f, 3f)]
        [SerializeField] private float _duration = 0.6f;

        [Tooltip("시작 스케일 (작은 점)")]
        [Range(0.01f, 5f)]
        [SerializeField] private float _startScale = 0.5f;

        [Tooltip("끝 스케일 (최대 확장 크기)")]
        [Range(0.5f, 20f)]
        [SerializeField] private float _endScale = 4f;

        private SpriteRenderer _renderer;
        private float _t;
        private Color _baseColor;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer != null) _baseColor = _renderer.color;
        }

        private void Update()
        {
            _t += Time.deltaTime;
            float n = Mathf.Clamp01(_t / _duration);

            // ease-out (1-(1-n)^2): 시작 빠르게 → 끝으로 갈수록 느려짐
            float scaleT = 1f - (1f - n) * (1f - n);
            float scale = Mathf.Lerp(_startScale, _endScale, scaleT);
            transform.localScale = new Vector3(scale, scale, scale);

            if (_renderer != null)
            {
                Color c = _baseColor;
                c.a = _baseColor.a * (1f - n); // 선형 페이드
                _renderer.color = c;
            }

            if (_t >= _duration) Destroy(gameObject);
        }
    }
}

using UnityEngine;

namespace DrillCorp.Weapon.Bomb
{
    /// <summary>
    /// 폭탄 착탄 예정 위치 표시기 (순수 비주얼)
    /// BombProjectile.Initialize에서 스폰, Explode에서 파괴됨
    /// 데미지/판정 일체 없음 — SpriteRenderer 알파 펄스만 처리
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class BombLandingMarker : MonoBehaviour
    {
        [Tooltip("기본 알파 (펄스 최소값)")]
        [Range(0f, 1f)]
        [SerializeField] private float _baseAlpha = 0.25f;

        [Tooltip("펄스 진폭 (기본 알파 위로 더해지는 크기)")]
        [Range(0f, 1f)]
        [SerializeField] private float _pulseAmplitude = 0.15f;

        [Tooltip("펄스 속도 (1초당 PingPong 사이클)")]
        [Range(0.1f, 5f)]
        [SerializeField] private float _pulseSpeed = 1.5f;

        private SpriteRenderer _renderer;
        private Color _baseColor;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            if (_renderer != null)
            {
                // SpriteRenderer.color는 인스턴스별로 별도 보관 → 다른 마커에 영향 없음
                _baseColor = _renderer.color;
            }
        }

        private void Update()
        {
            if (_renderer == null) return;

            float a = _baseAlpha + Mathf.PingPong(Time.time * _pulseSpeed, _pulseAmplitude);
            _renderer.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, a);
        }
    }
}

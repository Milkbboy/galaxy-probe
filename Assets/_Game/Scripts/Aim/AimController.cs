using UnityEngine;
using UnityEngine.InputSystem;
using DrillCorp.Machine;

namespace DrillCorp.Aim
{
    public class AimController : MonoBehaviour
    {
        [Header("Aim Settings")]
        [SerializeField] private float _aimRadius = 0.5f;
        [SerializeField] private bool _autoCalculateRadius = true;
        [SerializeField] private LayerMask _bugLayer;
        [SerializeField] private LayerMask _groundLayer;

        [Header("Auto Attack Settings")]
        [SerializeField] private float _attackCooldown = 0.5f;
        [SerializeField] private float _damage = 10f;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer _crosshairRenderer;
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _readyColor = Color.red;

        private Camera _mainCamera;
        private Vector3 _aimPosition;
        private float _lastAttackTime;
        private bool _hasBugInRange;

        public bool HasBugInRange => _hasBugInRange;
        public Vector3 AimPosition => _aimPosition;
        public float CooldownProgress => Mathf.Clamp01((Time.time - _lastAttackTime) / _attackCooldown);
        public bool IsReady => Time.time >= _lastAttackTime + _attackCooldown;

        private void Awake()
        {
            _mainCamera = Camera.main;
            CalculateAimRadius();
            EnsureBugLayer();
        }

        private void EnsureBugLayer()
        {
            // _bugLayer가 설정 안 되어 있으면 "Bug" 레이어 자동 설정
            if (_bugLayer == 0)
            {
                int bugLayerIndex = LayerMask.NameToLayer("Bug");
                if (bugLayerIndex != -1)
                {
                    _bugLayer = 1 << bugLayerIndex;
                }
            }
        }

        private void CalculateAimRadius()
        {
            if (_autoCalculateRadius && _crosshairRenderer != null && _crosshairRenderer.sprite != null)
            {
                // 스프라이트의 월드 크기에서 반지름 계산
                Vector3 spriteSize = _crosshairRenderer.bounds.size;
                _aimRadius = Mathf.Max(spriteSize.x, spriteSize.z) / 2f;
            }
        }

        private void Update()
        {
            UpdateAimPosition();
            UpdateCrosshairPosition();
            TryAutoAttack();
            UpdateCrosshairColor();
        }

        private void UpdateAimPosition()
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = _mainCamera.ScreenPointToRay(mousePos);

            // 바닥 평면(Y=0)과의 교차점 계산
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float distance))
            {
                _aimPosition = ray.GetPoint(distance);
            }
        }

        private void UpdateCrosshairPosition()
        {
            transform.position = _aimPosition + Vector3.up * 0.1f; // 살짝 위로
        }

        private void TryAutoAttack()
        {
            // 에임 범위 내 버그 확인
            Collider[] hits = Physics.OverlapSphere(_aimPosition, _aimRadius, _bugLayer);
            _hasBugInRange = hits.Length > 0;

            // 쿨다운 체크 후 자동 공격
            if (_hasBugInRange && Time.time >= _lastAttackTime + _attackCooldown)
            {
                Fire(hits);
                _lastAttackTime = Time.time;
            }
        }

        private void Fire(Collider[] hits)
        {
            foreach (var hit in hits)
            {
                var damageable = hit.GetComponent<IDamageable>();
                damageable?.TakeDamage(_damage);
            }
        }

        private void UpdateCrosshairColor()
        {
            if (_crosshairRenderer == null) return;

            // 범위 내 버그가 있으면 공격 색상, 없으면 일반 색상
            Color targetColor = _hasBugInRange ? _readyColor : _normalColor;
            _crosshairRenderer.color = targetColor;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _aimRadius);
        }
    }
}

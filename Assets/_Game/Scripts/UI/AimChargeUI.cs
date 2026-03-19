using UnityEngine;
using UnityEngine.UI;
using DrillCorp.Aim;

namespace DrillCorp.UI
{
    public class AimChargeUI : MonoBehaviour
    {
        [Header("Charge Bar")]
        [SerializeField] private Image _chargeFillImage;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Colors")]
        [SerializeField] private Color _chargingColor = Color.yellow;
        [SerializeField] private Color _readyColor = Color.red;

        [Header("Settings")]
        [SerializeField] private float _fadeSpeed = 5f;
        [SerializeField] private Vector3 _offset = new Vector3(0f, -0.5f, 0f);

        [Header("References")]
        [SerializeField] private AimController _aimController;

        private Camera _mainCamera;
        private RectTransform _rectTransform;
        private float _targetAlpha;

        private void Awake()
        {
            _mainCamera = Camera.main;
            _rectTransform = GetComponent<RectTransform>();

            if (_aimController == null)
            {
                _aimController = FindFirstObjectByType<AimController>();
            }
        }

        private void Update()
        {
            if (_aimController == null) return;

            UpdatePosition();
            UpdateChargeBar();
            UpdateVisibility();
        }

        private void UpdatePosition()
        {
            Vector3 worldPos = _aimController.AimPosition + _offset;
            Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPos);
            _rectTransform.position = screenPos;
        }

        private void UpdateChargeBar()
        {
            float progress = _aimController.CooldownProgress;

            if (_chargeFillImage != null)
            {
                _chargeFillImage.fillAmount = progress;
                _chargeFillImage.color = _aimController.IsReady ? _readyColor : _chargingColor;
            }
        }

        private void UpdateVisibility()
        {
            if (_canvasGroup == null) return;

            // 버그가 범위 내에 있을 때만 쿨다운 바 표시
            _targetAlpha = _aimController.HasBugInRange ? 1f : 0f;
            _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, _targetAlpha, _fadeSpeed * Time.deltaTime);
        }
    }
}

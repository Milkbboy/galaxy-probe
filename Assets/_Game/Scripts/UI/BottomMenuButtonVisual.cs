using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DrillCorp.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class BottomMenuButtonVisual : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        ISelectHandler,
        IDeselectHandler
    {
        [Header("References")]
        [SerializeField] private RectTransform _visualRoot;
        [SerializeField] private GameObject _hoverGlow;
        [SerializeField] private CanvasGroup _visualGroup;

        [Header("Pressed")]
        [SerializeField, Range(0.9f, 1f)] private float _pressedScale = 0.98f;
        [SerializeField, Range(0f, 1f)] private float _disabledAlpha = 0.55f;

        private Button _button;
        private bool _pointerInside;
        private bool _selected;

        private void Awake()
        {
            _button = GetComponent<Button>();
            ApplyNormalState();
        }

        private void OnDisable()
        {
            _pointerInside = false;
            _selected = false;
            ApplyNormalState();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_button == null || !_button.interactable)
                return;

            _pointerInside = true;
            UpdateGlow();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _pointerInside = false;
            ResetScale();
            UpdateGlow();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_button == null || !_button.interactable || eventData.button != PointerEventData.InputButton.Left)
                return;

            if (_hoverGlow != null)
                _hoverGlow.SetActive(true);

            if (_visualRoot != null)
                _visualRoot.localScale = Vector3.one * _pressedScale;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ResetScale();
            UpdateGlow();
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (_button == null || !_button.interactable)
                return;

            _selected = true;
            UpdateGlow();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _selected = false;
            ResetScale();
            UpdateGlow();
        }

        public void RefreshInteractableState()
        {
            if (_button == null)
                _button = GetComponent<Button>();

            if (_button == null || !_button.interactable)
            {
                _pointerInside = false;
                _selected = false;
                ApplyNormalState();
                return;
            }

            UpdateGlow();
            ApplyInteractableAlpha();
        }

        private void UpdateGlow()
        {
            if (_button == null)
                _button = GetComponent<Button>();

            var showGlow = _button != null && _button.interactable && (_pointerInside || _selected);
            if (_hoverGlow != null)
                _hoverGlow.SetActive(showGlow);

            ApplyInteractableAlpha();
        }

        private void ApplyNormalState()
        {
            ResetScale();

            if (_hoverGlow != null)
                _hoverGlow.SetActive(false);

            ApplyInteractableAlpha();
        }

        private void ApplyInteractableAlpha()
        {
            if (_visualGroup == null)
                return;

            if (_button == null)
                _button = GetComponent<Button>();

            _visualGroup.alpha = _button != null && !_button.interactable ? _disabledAlpha : 1f;
        }

        private void ResetScale()
        {
            if (_visualRoot != null)
                _visualRoot.localScale = Vector3.one;
        }
    }
}

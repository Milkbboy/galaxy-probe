using UnityEngine;
using TMPro;
using DrillCorp.Core;
using DrillCorp.Machine;

namespace DrillCorp.UI
{
    public class MiningUI : MonoBehaviour
    {
        [Header("Mining Display")]
        [SerializeField] private TextMeshProUGUI _miningText;
        [SerializeField] private string _prefix = "채굴: ";

        [Header("Animation")]
        [SerializeField] private float _punchScale = 1.2f;
        [SerializeField] private float _punchDuration = 0.1f;

        [Header("References")]
        [SerializeField] private MachineController _machine;

        private Vector3 _originalScale;
        private float _punchTimer;

        private void Awake()
        {
            if (_miningText != null)
            {
                _originalScale = _miningText.transform.localScale;
            }
        }

        private void Start()
        {
            if (_machine == null)
            {
                _machine = FindAnyObjectByType<MachineController>();
            }

            UpdateMiningText(0);
        }

        private void OnEnable()
        {
            GameEvents.OnMiningGained += OnMiningGained;
        }

        private void OnDisable()
        {
            GameEvents.OnMiningGained -= OnMiningGained;
        }

        private void Update()
        {
            if (_machine != null)
            {
                UpdateMiningText(_machine.TotalMined);
            }

            UpdatePunchAnimation();
        }

        private void OnMiningGained(int amount)
        {
            _punchTimer = _punchDuration;
        }

        private void UpdateMiningText(int totalMined)
        {
            if (_miningText != null)
            {
                _miningText.text = $"{_prefix}{totalMined}";
            }
        }

        private void UpdatePunchAnimation()
        {
            if (_miningText == null) return;

            if (_punchTimer > 0f)
            {
                _punchTimer -= Time.deltaTime;
                float t = _punchTimer / _punchDuration;
                float scale = Mathf.Lerp(1f, _punchScale, t);
                _miningText.transform.localScale = _originalScale * scale;
            }
            else
            {
                _miningText.transform.localScale = _originalScale;
            }
        }
    }
}

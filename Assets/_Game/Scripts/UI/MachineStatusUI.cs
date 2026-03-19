using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrillCorp.Core;
using DrillCorp.Machine;

namespace DrillCorp.UI
{
    public class MachineStatusUI : MonoBehaviour
    {
        [Header("HP Bar")]
        [SerializeField] private Image _hpFillImage;
        [SerializeField] private TextMeshProUGUI _hpText;

        [Header("Fuel Bar")]
        [SerializeField] private Image _fuelFillImage;
        [SerializeField] private TextMeshProUGUI _fuelText;

        [Header("References")]
        [SerializeField] private MachineController _machine;

        private void Start()
        {
            if (_machine == null)
            {
                _machine = FindFirstObjectByType<MachineController>();
            }
        }

        private void OnEnable()
        {
            GameEvents.OnMachineDamaged += OnMachineDamaged;
            GameEvents.OnFuelChanged += UpdateFuelBar;
        }

        private void OnDisable()
        {
            GameEvents.OnMachineDamaged -= OnMachineDamaged;
            GameEvents.OnFuelChanged -= UpdateFuelBar;
        }

        private void Update()
        {
            // 매 프레임 HP 업데이트 (초기화 타이밍 문제 해결)
            UpdateHPBar();
        }

        private void OnMachineDamaged(float damage)
        {
            UpdateHPBar();
        }

        private void UpdateHPBar()
        {
            if (_machine == null) return;

            float ratio = _machine.CurrentHealth / _machine.MaxHealth;

            if (_hpFillImage != null)
            {
                _hpFillImage.fillAmount = ratio;

                // Gradient는 사용하지 않음 - 색상은 Inspector에서 설정한 값 유지
            }

            if (_hpText != null)
            {
                _hpText.text = $"{Mathf.CeilToInt(_machine.CurrentHealth)} / {Mathf.CeilToInt(_machine.MaxHealth)}";
            }
        }

        private void UpdateFuelBar(float currentFuel)
        {
            if (_machine == null) return;

            float ratio = currentFuel / _machine.MaxFuel;

            if (_fuelFillImage != null)
            {
                _fuelFillImage.fillAmount = ratio;
            }

            if (_fuelText != null)
            {
                _fuelText.text = $"{Mathf.CeilToInt(currentFuel)}s";
            }
        }
    }
}

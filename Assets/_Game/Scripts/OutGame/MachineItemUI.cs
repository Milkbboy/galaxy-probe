using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrillCorp.Data;

namespace DrillCorp.OutGame
{
    public class MachineItemUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _briefStatsText;
        [SerializeField] private Button _selectButton;
        [SerializeField] private GameObject _selectedIndicator;

        [Header("Colors")]
        [SerializeField] private Color _selectedColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _normalColor = new Color(0.8f, 0.8f, 0.8f);

        private MachineData _machineData;
        private MachineSelectUI _selectUI;

        private void Awake()
        {
            if (_selectButton != null)
                _selectButton.onClick.AddListener(OnSelectClicked);
        }

        public void Setup(MachineData data, MachineSelectUI selectUI)
        {
            _machineData = data;
            _selectUI = selectUI;

            if (_iconImage != null && data.Icon != null)
                _iconImage.sprite = data.Icon;

            if (_nameText != null)
                _nameText.text = data.MachineName;

            if (_briefStatsText != null)
            {
                _briefStatsText.text = $"HP:{data.MaxHealth} DMG:{data.AttackDamage}";
            }
        }

        public void Refresh(int selectedMachineId)
        {
            if (_machineData == null) return;

            bool isSelected = _machineData.MachineId == selectedMachineId;

            if (_selectedIndicator != null)
                _selectedIndicator.SetActive(isSelected);

            if (_selectButton != null)
            {
                var colors = _selectButton.colors;
                colors.normalColor = isSelected ? _selectedColor : _normalColor;
                _selectButton.colors = colors;
            }
        }

        private void OnSelectClicked()
        {
            if (_machineData != null && _selectUI != null)
            {
                _selectUI.SelectMachine(_machineData);
            }
        }
    }
}

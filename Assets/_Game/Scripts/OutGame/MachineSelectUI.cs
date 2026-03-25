using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DrillCorp.Data;
using DrillCorp.Core;

namespace DrillCorp.OutGame
{
    public class MachineSelectUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _machineListContainer;
        [SerializeField] private GameObject _machineItemPrefab;
        [SerializeField] private Button _backButton;
        [SerializeField] private TitleUI _titleUI;

        [Header("Machine Data")]
        [SerializeField] private List<MachineData> _availableMachines = new List<MachineData>();

        [Header("Selected Info")]
        [SerializeField] private TextMeshProUGUI _selectedNameText;
        [SerializeField] private TextMeshProUGUI _selectedDescText;
        [SerializeField] private TextMeshProUGUI _selectedStatsText;

        private List<MachineItemUI> _machineItems = new List<MachineItemUI>();
        private int _selectedMachineId = 1;

        private void Start()
        {
            if (_backButton != null)
                _backButton.onClick.AddListener(OnBackClicked);

            LoadSelectedMachine();
            CreateMachineItems();
        }

        private void OnEnable()
        {
            RefreshUI();
        }

        private void CreateMachineItems()
        {
            if (_machineItemPrefab == null || _machineListContainer == null) return;

            foreach (var machineData in _availableMachines)
            {
                if (machineData == null) continue;

                var itemObj = Instantiate(_machineItemPrefab, _machineListContainer);
                var itemUI = itemObj.GetComponent<MachineItemUI>();
                if (itemUI != null)
                {
                    itemUI.Setup(machineData, this);
                    _machineItems.Add(itemUI);
                }
            }
        }

        public void RefreshUI()
        {
            foreach (var item in _machineItems)
            {
                item.Refresh(_selectedMachineId);
            }
            UpdateSelectedInfo();
        }

        public void SelectMachine(MachineData machineData)
        {
            if (machineData == null) return;

            _selectedMachineId = machineData.MachineId;
            SaveSelectedMachine();
            RefreshUI();

            GameEvents.OnMachineSelected?.Invoke(_selectedMachineId);
        }

        private void UpdateSelectedInfo()
        {
            var selectedMachine = _availableMachines.Find(m => m != null && m.MachineId == _selectedMachineId);
            if (selectedMachine == null && _availableMachines.Count > 0)
            {
                selectedMachine = _availableMachines[0];
            }

            if (selectedMachine == null) return;

            if (_selectedNameText != null)
                _selectedNameText.text = selectedMachine.MachineName;

            if (_selectedDescText != null)
                _selectedDescText.text = selectedMachine.Description;

            if (_selectedStatsText != null)
            {
                _selectedStatsText.text =
                    $"HP: {selectedMachine.MaxHealth}\n" +
                    $"Armor: {selectedMachine.Armor}\n" +
                    $"Fuel: {selectedMachine.MaxFuel}s\n" +
                    $"Mining: {selectedMachine.MiningRate}/s\n" +
                    $"Damage: {selectedMachine.AttackDamage}\n" +
                    $"Attack Speed: {1f / selectedMachine.AttackCooldown:F1}/s";
            }
        }

        public int GetSelectedMachineId()
        {
            return _selectedMachineId;
        }

        public MachineData GetSelectedMachineData()
        {
            return _availableMachines.Find(m => m != null && m.MachineId == _selectedMachineId);
        }

        private void OnBackClicked()
        {
            if (_titleUI != null)
                _titleUI.ShowMainPanel();
        }

        private void SaveSelectedMachine()
        {
            PlayerPrefs.SetInt("SelectedMachine", _selectedMachineId);
            PlayerPrefs.Save();
        }

        private void LoadSelectedMachine()
        {
            _selectedMachineId = PlayerPrefs.GetInt("SelectedMachine", 1);
        }
    }
}

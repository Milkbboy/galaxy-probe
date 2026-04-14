using UnityEngine;
using UnityEngine.InputSystem;
using DrillCorp.Aim;

namespace DrillCorp.Weapon
{
    /// <summary>
    /// 디버그용 무기 교체 (숫자키 1~4)
    /// </summary>
    public class WeaponSwitcher : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AimController _aimController;

        [Header("Weapons (1~4번 키)")]
        [SerializeField] private WeaponBase _slot1;
        [SerializeField] private WeaponBase _slot2;
        [SerializeField] private WeaponBase _slot3;
        [SerializeField] private WeaponBase _slot4;

        [Header("UI (선택)")]
        [SerializeField] private bool _showOnScreenLabel = true;

        private int _currentSlot = 1;
        private string _currentWeaponName = "";

        private void Start()
        {
            if (_aimController == null)
                _aimController = FindAnyObjectByType<AimController>();

            if (_slot1 != null)
                Equip(1);
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.digit1Key.wasPressedThisFrame) Equip(1);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) Equip(2);
            if (Keyboard.current.digit3Key.wasPressedThisFrame) Equip(3);
            if (Keyboard.current.digit4Key.wasPressedThisFrame) Equip(4);
        }

        private void Equip(int slot)
        {
            if (_aimController == null) return;

            WeaponBase weapon = slot switch
            {
                1 => _slot1,
                2 => _slot2,
                3 => _slot3,
                4 => _slot4,
                _ => null
            };

            if (weapon == null)
            {
                Debug.LogWarning($"[WeaponSwitcher] Slot {slot} 무기가 비어 있음");
                return;
            }

            _aimController.EquipWeapon(weapon);
            _currentSlot = slot;
            _currentWeaponName = weapon.BaseData != null ? weapon.BaseData.DisplayName : weapon.name;
        }

        private void OnGUI()
        {
            if (!_showOnScreenLabel) return;

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                normal = { textColor = Color.white }
            };

            string text = $"[Weapon {_currentSlot}] {_currentWeaponName}\n1/2/3/4 키로 교체";
            GUI.Label(new Rect(10, Screen.height - 60, 400, 60), text, style);
        }
    }
}

using UnityEngine;
using DrillCorp.Weapon;

namespace DrillCorp.UI.Weapon
{
    /// <summary>
    /// 좌측 무기 패널 (저격총/폭탄/기관총/레이저 4개 슬롯).
    /// 인스펙터에서 슬롯 4개와 무기 4개를 1:1로 매핑해주면
    /// Start 시점에 각 슬롯에 WeaponBase를 바인딩한다.
    ///
    /// 해금/언락 시스템은 현재 범위 밖 — 무기 Array에 null을 넣어두면
    /// 해당 슬롯은 자동으로 "잠김" 상태로 표시된다.
    /// </summary>
    public class WeaponPanelUI : MonoBehaviour
    {
        [Header("Slots (순서 = 무기 배열과 1:1)")]
        [SerializeField] private WeaponSlotUI[] _slots;

        [Header("Weapons (Slots 배열과 같은 순서로 지정)")]
        [Tooltip("비어있는(null) 항목은 슬롯이 '잠김' 상태로 표시됨")]
        [SerializeField] private WeaponBase[] _weapons;

        private void Start()
        {
            BindAll();
        }

        /// <summary>런타임에 재바인딩 필요 시 외부에서 호출.</summary>
        public void BindAll()
        {
            if (_slots == null) return;

            int n = _slots.Length;
            for (int i = 0; i < n; i++)
            {
                if (_slots[i] == null) continue;
                WeaponBase weapon = (_weapons != null && i < _weapons.Length) ? _weapons[i] : null;
                _slots[i].SetWeapon(weapon);
            }
        }
    }
}

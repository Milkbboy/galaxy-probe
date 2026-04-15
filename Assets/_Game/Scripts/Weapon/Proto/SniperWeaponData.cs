using UnityEngine;

namespace DrillCorp.Weapon.Proto
{
    /// <summary>
    /// 저격총 데이터 (_.html 프로토타입 호환)
    /// AimController의 Range 대신 무기 자체 Range를 쓰고 싶으면 UseCustomRange=true
    /// </summary>
    [CreateAssetMenu(fileName = "Weapon_Sniper", menuName = "Drill-Corp/Weapons/Proto/Sniper", order = 100)]
    public class SniperWeaponData : WeaponData
    {
        [Header("Sniper")]
        [Tooltip("AimController의 Aim Radius를 그대로 쓸지 여부. false면 CustomRange 사용.")]
        [SerializeField] private bool _useAimRadius = true;

        [Tooltip("무기 자체 사거리 (UseAimRadius=false일 때만 적용)")]
        [Range(0.1f, 10f)]
        [SerializeField] private float _customRange = 0.4f;

        public bool UseAimRadius => _useAimRadius;
        public float CustomRange => _customRange;
    }
}

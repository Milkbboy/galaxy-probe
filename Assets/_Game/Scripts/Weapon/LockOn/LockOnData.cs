using UnityEngine;

namespace DrillCorp.Weapon.LockOn
{
    [CreateAssetMenu(fileName = "Weapon_LockOn", menuName = "Drill-Corp/Weapons/Lock On", order = 23)]
    public class LockOnData : WeaponData
    {
        [Header("Lock On")]
        [Tooltip(
            "한 번에 타게팅할 최대 Bug 수\n" +
            "• 에임 범위 내 적이 더 많아도 이 수까지만 락온\n" +
            "• 업그레이드로 증가 가능"
        )]
        [Range(1, 100)]
        [SerializeField] private int _maxTargets = 20;

        [Tooltip(
            "마커가 순차적으로 찍히는 간격 (초)\n" +
            "• 0: 동시에 모든 마커 생성\n" +
            "• 0.05: 타닥타닥 순차 (추천)\n" +
            "• 0.1~0.2: 느긋하게 찍힘 (연출 강조)"
        )]
        [Range(0f, 0.5f)]
        [SerializeField] private float _markerSpawnInterval = 0.05f;

        [Tooltip(
            "타겟 간 데미지 적용 간격 (초)\n" +
            "• 0: 전원 동시 피격 (팡!)\n" +
            "• 0.05: 다다다 순차 (추천)"
        )]
        [Range(0f, 0.2f)]
        [SerializeField] private float _hitInterval = 0.05f;

        public int MaxTargets => _maxTargets;
        public float MarkerSpawnInterval => _markerSpawnInterval;
        public float HitInterval => _hitInterval;
    }
}

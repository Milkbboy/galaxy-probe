using UnityEngine;

namespace DrillCorp.Weapon.Saw
{
    /// <summary>
    /// 회전톱날 데이터. 머신 궤도 위 고정 반경에 떠있으며 자체 스핀.
    /// v2 가이드: docs/WeaponUnlockUpgradeSystem.md §7
    /// </summary>
    [CreateAssetMenu(fileName = "Weapon_Saw", menuName = "Drill-Corp/Weapons/Saw", order = 25)]
    public class SawWeaponData : WeaponData
    {
        [Header("Saw — Orbit")]
        [Tooltip("머신 중심에서 톱날까지 궤도 반경 (월드 유닛). 마우스 방향으로 이 거리만큼 떨어짐.")]
        [Range(1f, 20f)]
        [SerializeField] private float _orbitRadius = 7.2f;

        [Tooltip("블레이드 충돌 반경 (OverlapSphere 기준).")]
        [Range(0.3f, 10f)]
        [SerializeField] private float _bladeRadius = 1.8f;

        [Tooltip("블레이드 자체 스핀 속도 (rad/sec). 시각 전용.")]
        [Range(0f, 30f)]
        [SerializeField] private float _spinSpeed = 4.8f;

        [Header("Saw — Damage")]
        [Tooltip("데미지 tick 주기 (초). WeaponData.Damage 값이 tick당 데미지.")]
        [Range(0.02f, 1f)]
        [SerializeField] private float _damageTickInterval = 0.1f;

        [Header("Saw — Slow")]
        [Tooltip("접촉 시 벌레에게 걸리는 슬로우 강도 (0~0.9). 0.3 = 30% 감속.")]
        [Range(0f, 0.9f)]
        [SerializeField] private float _slowFactor = 0.3f;

        [Tooltip("슬로우 지속 시간 (초). 반복 접촉 시 타이머 갱신.")]
        [Range(0.1f, 10f)]
        [SerializeField] private float _slowDuration = 2f;

        [Header("Saw — Visual")]
        [Tooltip("톱날 비주얼 프리펩. null이면 시각 없이 동작. 프리펩 회전은 보존됨 (CLAUDE.md 좌표계 규칙).")]
        [SerializeField] private GameObject _bladeVisualPrefab;

        public float OrbitRadius => _orbitRadius;
        public float BladeRadius => _bladeRadius;
        public float SpinSpeed => _spinSpeed;
        public float DamageTickInterval => _damageTickInterval;
        public float SlowFactor => _slowFactor;
        public float SlowDuration => _slowDuration;
        public GameObject BladeVisualPrefab => _bladeVisualPrefab;
    }
}

using UnityEngine;

namespace DrillCorp.Weapon.Laser
{
    [CreateAssetMenu(fileName = "Weapon_Laser", menuName = "Drill-Corp/Weapons/Laser Beam", order = 22)]
    public class LaserBeamData : WeaponData
    {
        [Header("Laser")]
        [Tooltip(
            "데미지 적용 주기 (초)\n" +
            "• 이 간격마다 Damage 적용\n" +
            "• 0.05 → 초당 20회\n" +
            "• DPS = Damage × (1 / DamageTickInterval)"
        )]
        [Range(0.01f, 0.2f)]
        [SerializeField] private float _damageTickInterval = 0.05f;

        [Header("Fire Mode")]
        [Tooltip(
            "상시 발사 모드\n" +
            "• true: 에임에 Bug 없어도 항상 필드 활성화 (Heat/Overheat는 여전히 작동)\n" +
            "• false: 에임 범위에 Bug 있을 때만 활성화"
        )]
        [SerializeField] private bool _alwaysOn = true;

        [Header("Heat System")]
        [Tooltip(
            "최대 Heat (이 값 도달 시 과열 상태)\n" +
            "• 지속 시간 = MaxHeat / HeatPerSecond"
        )]
        [Range(10f, 500f)]
        [SerializeField] private float _maxHeat = 100f;

        [Tooltip("발사 중 초당 Heat 증가량")]
        [Range(1f, 200f)]
        [SerializeField] private float _heatPerSecond = 30f;

        [Tooltip("미발사 시 초당 Heat 감소량")]
        [Range(1f, 200f)]
        [SerializeField] private float _coolPerSecond = 20f;

        [Tooltip("과열 후 강제 쿨다운 시간 (초)")]
        [Range(0.5f, 5f)]
        [SerializeField] private float _overheatLockTime = 1.5f;

        [Header("Field")]
        [Tooltip(
            "필드 반경 (월드 단위)\n" +
            "• 0: AimController.AimRadius 사용 (에임 범위와 동일)\n" +
            "• 양수: 고정 반경"
        )]
        [Range(0f, 10f)]
        [SerializeField] private float _fieldRadius = 0f;

        [Tooltip(
            "필드가 에임을 따라오는 속도\n" +
            "• 0: 즉시 따라옴 (순간이동)\n" +
            "• 5~10: 살짝 지연 (자연스러움)\n" +
            "• 2~4: 크게 지연 (잔상 강조)"
        )]
        [Range(0f, 30f)]
        [SerializeField] private float _followSpeed = 6f;

        [Tooltip(
            "이동 방향 스트레치 강도\n" +
            "• 0: 스트레치 없음 (원형 유지)\n" +
            "• 0.2~0.4: 살짝 타원 (자연스러움)\n" +
            "• 0.5+: 강한 늘어짐"
        )]
        [Range(0f, 1f)]
        [SerializeField] private float _stretchAmount = 0.25f;

        public bool AlwaysOn => _alwaysOn;
        public float DamageTickInterval => _damageTickInterval;
        public float MaxHeat => _maxHeat;
        public float HeatPerSecond => _heatPerSecond;
        public float CoolPerSecond => _coolPerSecond;
        public float OverheatLockTime => _overheatLockTime;
        public float FieldRadius => _fieldRadius;
        public float FollowSpeed => _followSpeed;
        public float StretchAmount => _stretchAmount;
    }
}

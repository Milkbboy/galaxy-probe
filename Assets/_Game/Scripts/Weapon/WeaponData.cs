using UnityEngine;

namespace DrillCorp.Weapon
{
    /// <summary>
    /// 무기 공통 데이터 (ScriptableObject 베이스)
    /// 투사체 없이 에임 범위 내 영역 데미지 방식
    /// </summary>
    public abstract class WeaponData : ScriptableObject
    {
        [Header("Info")]
        [Tooltip("UI/디버그 표시용 이름 (예: '샷건')")]
        [SerializeField] private string _displayName = "Weapon";

        [Tooltip("무기 설명 (UI 툴팁용, 선택)")]
        [TextArea(2, 4)]
        [SerializeField] private string _description;

        [Header("Firing")]
        [Tooltip(
            "발사 간 딜레이 (초)\n" +
            "• 값이 작을수록 빠른 발사\n" +
            "• 업그레이드로 단축되는 핵심 스탯\n" +
            "• 예: Shotgun 1.0 / BurstGun 0.08 / Laser 0 / LockOn 2.0"
        )]
        [Range(0f, 5f)]
        [SerializeField] private float _fireDelay = 0.5f;

        [Tooltip(
            "데미지 (무기별 의미가 다름)\n" +
            "• Shotgun/LockOn: 1회 발사당 각 타겟에 가하는 데미지\n" +
            "• BurstGun: 1회 발사당 단일 타겟 데미지\n" +
            "• Laser: Tick당 데미지 (DamageTickInterval마다 적용)"
        )]
        [Range(0.1f, 500f)]
        [SerializeField] private float _damage = 10f;

        [Header("Visual")]
        [Tooltip(
            "피격 시 Bug 위치에 스폰되는 VFX 프리펩\n" +
            "• 탄 흔적, 폭발, 스파크 등\n" +
            "• null 허용 (효과 없이 데미지만)\n" +
            "• 자동 파괴 시간: HitVfxLifetime"
        )]
        [SerializeField] private GameObject _hitVfxPrefab;

        [Tooltip("Hit VFX 자동 파괴 시간 (초)")]
        [Range(0.1f, 5f)]
        [SerializeField] private float _hitVfxLifetime = 1.5f;

        public string DisplayName => _displayName;
        public string Description => _description;
        public float FireDelay => _fireDelay;
        public float Damage => _damage;
        public GameObject HitVfxPrefab => _hitVfxPrefab;
        public float HitVfxLifetime => _hitVfxLifetime;
    }
}

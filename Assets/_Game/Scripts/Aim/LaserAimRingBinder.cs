using UnityEngine;
using DrillCorp.Weapon.Laser;

namespace DrillCorp.Aim
{
    /// <summary>
    /// LaserWeapon의 크로스헤어 쿨 호를 AimWeaponRing에 바인딩.
    ///
    /// 슬롯 바와는 분리된 표시(§1.5 ①):
    /// - 슬롯 바(WeaponSlotUI): Active 시 빔 수명% / Cooling 시 쿨% / Ready 시 1
    /// - 크로스헤어 호(여기): Active 시 0(호 숨김, 프로토 L313 lp=0) / Cooling 시 쿨% / Ready 시 1
    /// - 색: 프로토 L313 `rgba(255,23,68,...)` #ff1744 진홍 고정 (슬롯 바의 3상태 색과 달리)
    ///
    /// BombAimRingBinder / MachineGunAimRingBinder 패턴 동일 — 씬에서 AimController 자식에 부착.
    /// </summary>
    [RequireComponent(typeof(AimWeaponRing))]
    public class LaserAimRingBinder : MonoBehaviour
    {
        [SerializeField] private LaserWeapon _weapon;

        private AimWeaponRing _ring;

        // 프로토 #ff1744 (laser 테마색, L313 drawCrosshair)
        private static readonly Color LaserRed = new Color(1f, 0.09f, 0.267f, 1f);

        private void Awake()
        {
            _ring = GetComponent<AimWeaponRing>();
            if (_weapon == null)
                _weapon = FindAnyObjectByType<LaserWeapon>();
        }

        private void Update()
        {
            if (_ring == null) return;

            // v2 — 미해금 무기는 SetActive(false)되어 호도 영구 숨김
            if (_weapon == null || !_weapon.gameObject.activeInHierarchy)
            {
                gameObject.SetActive(false);
                return;
            }

            // 슬롯 바(BarFillAmount)와 분리된 CrosshairRingFill 사용 —
            // Active 중엔 0 반환 → 호 숨김 (프로토 L313 laserCD 고정 → lp=0 재현)
            _ring.FillAmount = _weapon.CrosshairRingFill;
            _ring.SetColor(LaserRed);
        }
    }
}

using UnityEngine;
using DrillCorp.Weapon.MachineGun;

namespace DrillCorp.Aim
{
    /// <summary>
    /// MachineGunWeapon의 상태를 AimWeaponRing에 바인딩.
    /// - FillAmount는 _weapon.BarFillAmount (탄 잔량 또는 리로딩 진행)
    /// - 색은 _weapon.BarColor (파랑 #4fc3f7 / 리로딩 중 빨강) 푸시
    ///   → 인스펙터의 _cooldownColor 기본값(핑크) 무시하고 무기 상태에 따라 자동 변경
    /// </summary>
    [RequireComponent(typeof(AimWeaponRing))]
    public class MachineGunAimRingBinder : MonoBehaviour
    {
        [SerializeField] private MachineGunWeapon _weapon;

        private AimWeaponRing _ring;

        private void Awake()
        {
            _ring = GetComponent<AimWeaponRing>();
            if (_weapon == null)
                _weapon = FindAnyObjectByType<MachineGunWeapon>();
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

            // BarFillAmount: 평상시 탄 잔량(감소) / 리로딩 중 진행도(차오름)
            _ring.FillAmount = _weapon.BarFillAmount;

            // 슬롯 바와 동일 색상을 푸시 (평상시 파랑 / 리로딩 중 빨강)
            _ring.SetColor(_weapon.BarColor);
        }
    }
}

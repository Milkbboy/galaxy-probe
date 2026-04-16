using UnityEngine;
using DrillCorp.Weapon.Bomb;

namespace DrillCorp.Aim
{
    /// <summary>
    /// BombWeapon의 쿨다운 진행도를 AimWeaponRing에 바인딩한다.
    /// SniperAimRingBinder 패턴 — 폭탄용 호 (반경 R+0.2, 주황 #f4a423)
    /// 폭탄은 수동 발사이므로 준비 상태면 항상 "hit" 색으로 표시.
    /// </summary>
    [RequireComponent(typeof(AimWeaponRing))]
    public class BombAimRingBinder : MonoBehaviour
    {
        [SerializeField] private BombWeapon _weapon;

        private AimWeaponRing _ring;

        private void Awake()
        {
            _ring = GetComponent<AimWeaponRing>();
            if (_weapon == null)
                _weapon = FindAnyObjectByType<BombWeapon>();
        }

        private void Update()
        {
            if (_ring == null) return;

            if (_weapon == null)
            {
                _ring.FillAmount = 0f;
                return;
            }

            // CooldownProgress: 0(방금 발사) → 1(준비 완료)
            _ring.FillAmount = _weapon.CooldownProgress;

            // 슬롯 바와 동일 색상을 푸시 (쿨중=주황 #f4a423, 준비=초록 ReadyBarColor)
            // → 인스펙터의 _cooldownColor/_readyHitColor 기본값(핑크) 무시하고 SO ThemeColor 기반으로 표시
            _ring.SetColor(_weapon.BarColor);
        }
    }
}

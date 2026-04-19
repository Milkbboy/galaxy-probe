using UnityEngine;
using DrillCorp.Weapon.Proto;

namespace DrillCorp.Aim
{
    /// <summary>
    /// SniperWeapon의 쿨다운 진행도를 AimWeaponRing에 바인딩한다.
    /// _.html 프로토타입의 sp=sniperCD<=0?1:(1-sniperCD/ws.sniper.cd) 로직 재현.
    /// </summary>
    [RequireComponent(typeof(AimWeaponRing))]
    public class SniperAimRingBinder : MonoBehaviour
    {
        [SerializeField] private SniperWeapon _weapon;

        private AimWeaponRing _ring;

        private void Awake()
        {
            _ring = GetComponent<AimWeaponRing>();
            if (_weapon == null)
                _weapon = FindAnyObjectByType<SniperWeapon>();
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

            // CooldownProgress: 0(방금 발사) → 1(준비 완료)
            _ring.FillAmount = _weapon.CooldownProgress;

            bool isReady = _weapon.CanFire;
            bool isHitting = _ring.Aim != null && _ring.Aim.HasBugInRange;
            _ring.SetState(isReady, isHitting);
        }
    }
}

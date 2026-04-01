using UnityEngine;
using DrillCorp.UI;

namespace DrillCorp.Bug.Behaviors.Passive
{
    /// <summary>
    /// 쉴드 패시브 - 데미지 흡수, 시간이 지나면 재생
    /// param1 = 쉴드량 (기본 50)
    /// param2 = 재생 대기시간 (기본 5초, 피격 후 대기)
    /// </summary>
    public class ShieldPassive : PassiveBehaviorBase
    {
        private float _maxShield;
        private float _currentShield;
        private float _regenDelay;
        private float _regenRate;
        private float _timeSinceLastHit;

        public float CurrentShield => _currentShield;
        public float MaxShield => _maxShield;

        public ShieldPassive(float shieldAmount = 50f, float regenDelay = 5f)
        {
            _maxShield = shieldAmount > 0f ? shieldAmount : 50f;
            _currentShield = _maxShield;
            _regenDelay = regenDelay > 0f ? regenDelay : 5f;
            _regenRate = _maxShield / 3f; // 3초에 완충
            _timeSinceLastHit = _regenDelay; // 시작 시 재생 가능
        }

        public override void UpdatePassive(float deltaTime)
        {
            _timeSinceLastHit += deltaTime;

            // 재생 대기시간이 지났고, 쉴드가 최대가 아니면 재생
            if (_timeSinceLastHit >= _regenDelay && _currentShield < _maxShield)
            {
                _currentShield += _regenRate * deltaTime;
                _currentShield = Mathf.Min(_currentShield, _maxShield);
            }
        }

        public override float ProcessIncomingDamage(float damage)
        {
            if (_currentShield <= 0f)
            {
                return damage;
            }

            _timeSinceLastHit = 0f; // 피격 시 재생 타이머 리셋

            // 쉴드로 흡수
            if (_currentShield >= damage)
            {
                _currentShield -= damage;
                ShowShieldAbsorb(damage);
                return 0f; // 완전 흡수
            }
            else
            {
                // 부분 흡수
                float absorbed = _currentShield;
                float remaining = damage - _currentShield;
                _currentShield = 0f;
                ShowShieldAbsorb(absorbed);
                ShowShieldBreak();
                return remaining;
            }
        }

        private void ShowShieldAbsorb(float absorbed)
        {
            if (_bug != null)
            {
                DamagePopup.CreateText(_bug.transform.position, $"Shield -{absorbed:F0}", new Color(0.3f, 0.7f, 1f));
            }
        }

        private void ShowShieldBreak()
        {
            if (_bug != null)
            {
                DamagePopup.CreateText(_bug.transform.position, "SHIELD BREAK!", new Color(1f, 0.3f, 0.3f));
            }
        }
    }
}

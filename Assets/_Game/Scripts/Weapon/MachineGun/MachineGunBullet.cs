using UnityEngine;
using DrillCorp.Machine;
using DrillCorp.VFX.Pool;

namespace DrillCorp.Weapon.MachineGun
{
    /// <summary>
    /// 기관총 탄환 (자립형 — 무기 참조 없이 데이터만 보관)
    /// 발사 시 방향/속도/수명 캡처 후 매 프레임 직진 비행 + OverlapSphere 충돌 검사
    /// 첫 명중 1마리에게만 데미지를 가하고 자기 자신 파괴 (관통 없음, 프로토타입과 동일)
    /// </summary>
    public class MachineGunBullet : MonoBehaviour
    {
        private Vector3 _velocity;        // 단위: 유닛/초 (XZ 평면, Y=0)
        private MachineGunData _data;
        private LayerMask _bugLayer;
        private float _spawnTime;
        private bool _consumed;           // 명중/수명만료 후 중복 처리 방지
        private float _effectiveDamage;   // v2 — WeaponUpgrade 반영

        // 충돌 검사용 공용 버퍼 (한 프레임에 다발이 동시 처리돼도 안전)
        private static readonly Collider[] _overlapBuffer = new Collider[16];

        /// <summary>
        /// MachineGunWeapon.Fire에서 스폰 직후 호출.
        /// direction은 XZ 평면 정규화 벡터 (산포가 적용된 최종 방향).
        /// </summary>
        public void Initialize(Vector3 direction, MachineGunData data, LayerMask bugLayer)
            => Initialize(direction, data, bugLayer, data != null ? data.Damage : 0f);

        /// <summary>WeaponUpgrade 보정된 damage를 받는 오버로드.</summary>
        public void Initialize(Vector3 direction, MachineGunData data, LayerMask bugLayer, float effectiveDamage)
        {
            _data = data;
            _bugLayer = bugLayer;
            _spawnTime = Time.time;
            _effectiveDamage = effectiveDamage;

            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) direction = Vector3.forward;
            else direction.Normalize();

            _velocity = direction * data.BulletSpeed;

            // 지면 평면 고정
            var pos = transform.position;
            pos.y = 0f;
            transform.position = pos;
        }

        private void Update()
        {
            if (_consumed || _data == null) return;

            // 수명 만료 → 그냥 사라짐 (폭발 없음)
            if (Time.time - _spawnTime >= _data.BulletLifetime)
            {
                Despawn();
                return;
            }

            // XZ 평면 직진 이동
            transform.position += _velocity * Time.deltaTime;

            // 명중 검사 — 작은 반경 OverlapSphere
            int count = Physics.OverlapSphereNonAlloc(
                transform.position, _data.BulletHitRadius, _overlapBuffer, _bugLayer);

            if (count <= 0) return;

            // 가장 가까운 대상 1마리에게만 데미지 (관통 없음)
            Collider closest = null;
            float bestDistSqr = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var c = _overlapBuffer[i];
                if (c == null) continue;
                float d = (c.transform.position - transform.position).sqrMagnitude;
                if (d < bestDistSqr)
                {
                    bestDistSqr = d;
                    closest = c;
                }
            }

            if (closest != null) DealDamageAndDespawn(closest);
        }

        private void DealDamageAndDespawn(Collider target)
        {
            var damageable = target.GetComponent<IDamageable>()
                             ?? target.GetComponentInParent<IDamageable>();
            damageable?.TakeDamage(_effectiveDamage);

            if (_data.HitVfxPrefab != null)
            {
                // 프리펩 회전 보존 — Quaternion.identity 금지 (탑뷰 스프라이트가 카메라 쪽으로 서버림)
                VfxPool.Get(_data.HitVfxPrefab, target.transform.position, _data.HitVfxPrefab.transform.rotation);
            }

            Despawn();
        }

        private void Despawn()
        {
            if (_consumed) return;
            _consumed = true;
            Destroy(gameObject);
        }
    }
}

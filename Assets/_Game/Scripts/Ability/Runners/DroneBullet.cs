using UnityEngine;
using DrillCorp.Machine;

namespace DrillCorp.Ability.Runners
{
    /// <summary>
    /// 지누스 드론 탄 — 드론 포탑·드론 거미가 공유하는 경량 투사체.
    ///
    /// v2.html:1048/1201 의 `bullets.push({x,y,vx,vy,life,fromDrone:true})` 포팅.
    /// v2는 기관총 탄 풀을 재사용하고 `fromDrone` 플래그로 데미지만 0.8 고정시켰지만,
    /// Unity에선 MachineGunData 의존을 끌어올 이유가 없어 별도 MonoBehaviour로 분리.
    ///
    /// 동작:
    ///   · Initialize 시 방향/속도/데미지/수명 캡처 (호출자가 산포 적용 후 넘김)
    ///   · 매 프레임 XZ 평면 직진 이동
    ///   · OverlapSphereNonAlloc 으로 벌레 첫 1마리 명중 → TakeDamage → Destroy
    ///   · 수명 만료 시 그냥 소멸 (폭발 VFX 없음 — v2 원본과 동일)
    /// </summary>
    public class DroneBullet : MonoBehaviour
    {
        private Vector3 _velocity;     // 단위: 유닛/초 (XZ 평면, Y=0)
        private float _damage;
        private float _lifetime;
        private float _spawnTime;
        private LayerMask _bugLayer;
        private float _hitRadius = 0.3f;
        private bool _consumed;

        // 한 프레임에 다발이 동시 처리돼도 안전하도록 static 버퍼 공유.
        private static readonly Collider[] _overlapBuffer = new Collider[8];

        /// <summary>
        /// 스폰 직후 1회 호출. direction 은 XZ 평면 정규화 벡터(산포 적용 완료).
        /// </summary>
        public void Initialize(Vector3 direction, float speed, float damage, float lifetime, LayerMask bugLayer)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) direction = Vector3.forward;
            else direction.Normalize();

            _velocity = direction * speed;
            _damage = damage;
            _lifetime = lifetime;
            _bugLayer = bugLayer;
            _spawnTime = Time.time;

            // 지면 평면 고정
            var pos = transform.position;
            pos.y = 0f;
            transform.position = pos;
        }

        private void Update()
        {
            if (_consumed) return;

            // 수명 만료 → 그냥 소멸
            if (Time.time - _spawnTime >= _lifetime)
            {
                Despawn();
                return;
            }

            // XZ 평면 직진
            transform.position += _velocity * Time.deltaTime;

            // 명중 검사 — 작은 반경 OverlapSphere
            int count = Physics.OverlapSphereNonAlloc(
                transform.position, _hitRadius, _overlapBuffer, _bugLayer);
            if (count <= 0) return;

            // 가장 가까운 대상 1마리에게만 데미지 (관통 없음, v2 동일)
            Collider closest = null;
            float bestDistSqr = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var c = _overlapBuffer[i];
                if (c == null) continue;
                float d = (c.transform.position - transform.position).sqrMagnitude;
                if (d < bestDistSqr) { bestDistSqr = d; closest = c; }
            }

            if (closest != null) DealDamageAndDespawn(closest);
        }

        private void DealDamageAndDespawn(Collider target)
        {
            var damageable = target.GetComponent<IDamageable>()
                             ?? target.GetComponentInParent<IDamageable>();
            damageable?.TakeDamage(_damage);
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

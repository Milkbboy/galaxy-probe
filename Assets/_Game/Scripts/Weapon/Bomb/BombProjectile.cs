using UnityEngine;
using DrillCorp.Machine;
using DrillCorp.Audio;

namespace DrillCorp.Weapon.Bomb
{
    /// <summary>
    /// 폭탄 투사체 (자립형 — 무기 참조 없이 데이터만 보관)
    /// 머신에서 스폰되어 발사 시점 타겟 좌표로 비행 → 도달 또는 수명 만료 시 폭발 AoE
    /// 무기 전환 후에도 정상 동작하도록 BombWeapon에 의존하지 않음
    /// </summary>
    public class BombProjectile : MonoBehaviour
    {
        private Vector3 _targetPos;
        private BombData _data;
        private LayerMask _bugLayer;
        private float _spawnTime;
        private bool _exploded;

        private GameObject _marker;

        // 폭발 시 OverlapSphere 결과를 담는 공용 버퍼 (한 프레임에 여러 폭탄이 터져도 안전)
        private static readonly Collider[] _overlapBuffer = new Collider[64];

        /// <summary>
        /// BombWeapon.Fire에서 스폰 직후 호출. 타겟·데이터·레이어 캡처 + 착탄 마커 스폰.
        /// </summary>
        public void Initialize(Vector3 targetPos, BombData data, LayerMask bugLayer)
        {
            _data = data;
            _bugLayer = bugLayer;
            _spawnTime = Time.time;

            // 지면 평면(Y=0)에서 비행 — 타겟의 Y 성분 무시
            targetPos.y = 0f;
            _targetPos = targetPos;

            var pos = transform.position;
            pos.y = 0f;
            transform.position = pos;

            SpawnLandingMarker();
        }

        private void SpawnLandingMarker()
        {
            if (_data == null || _data.LandingMarkerPrefab == null) return;

            // 프리펩에 베이크된 회전(탑뷰용 90,0,0) 보존 — Quaternion.identity 금지
            // Y는 살짝 띄워서 지면 z-fighting 방지
            Vector3 markerPos = new Vector3(_targetPos.x, 0.02f, _targetPos.z);
            _marker = Instantiate(_data.LandingMarkerPrefab, markerPos, _data.LandingMarkerPrefab.transform.rotation);

            // 폭발 반경에 정확히 맞춰 스케일 (지름 = radius × 2)
            // 업그레이드로 반경 늘어나도 마커가 자동으로 정확한 크기로 표시됨
            float diameter = _data.ExplosionRadius * 2f;
            _marker.transform.localScale = new Vector3(diameter, diameter, 1f);
        }

        private void Update()
        {
            if (_exploded || _data == null) return;

            // 수명 만료 — 도달 못해도 강제 폭발
            if (Time.time - _spawnTime >= _data.ProjectileLifetime)
            {
                Explode();
                return;
            }

            // XZ 평면 이동 (Y는 0 유지)
            Vector3 pos = transform.position;
            Vector3 dir = _targetPos - pos;
            dir.y = 0f;
            float dist = dir.magnitude;
            float step = _data.ProjectileSpeed * Time.deltaTime;

            if (dist <= step)
            {
                // 이번 프레임 안에 도달 — 정확히 타겟 위치에 스냅 후 폭발
                transform.position = _targetPos;
                Explode();
                return;
            }

            transform.position = pos + (dir / dist) * step;
        }

        private void Explode()
        {
            if (_exploded) return;
            _exploded = true;

            Detonate(transform.position, _data, _bugLayer);

            DestroyMarker();
            Destroy(gameObject);
        }

        /// <summary>
        /// 위치 + 데이터 + 레이어를 받아 폭발 처리 (AoE 데미지 + VFX).
        /// 투사체 도달 폭발과 BombWeapon의 즉시(instant) 폭발 모드 양쪽에서 호출됨.
        /// </summary>
        public static void Detonate(Vector3 pos, BombData data, LayerMask bugLayer)
        {
            if (data == null) return;

            AudioManager.Instance?.PlayBombExplosion();

            int count = Physics.OverlapSphereNonAlloc(
                pos, data.ExplosionRadius, _overlapBuffer, bugLayer);

            for (int i = 0; i < count; i++)
            {
                var col = _overlapBuffer[i];
                if (col == null) continue;

                var damageable = col.GetComponent<IDamageable>()
                                 ?? col.GetComponentInParent<IDamageable>();
                damageable?.TakeDamage(data.Damage);

                if (data.HitVfxPrefab != null)
                {
                    // 프리펩 회전 보존 (탑뷰 스프라이트는 90,0,0 — identity 쓰면 카메라 쪽으로 서버림)
                    var hitVfx = Instantiate(data.HitVfxPrefab, col.transform.position, data.HitVfxPrefab.transform.rotation);
                    Destroy(hitVfx, data.HitVfxLifetime);
                }
            }

            if (data.ExplosionVfxPrefab != null)
            {
                // 프리펩 회전 보존 (탑뷰 스프라이트는 90,0,0 — identity 쓰면 카메라 쪽으로 서버림)
                var vfx = Instantiate(data.ExplosionVfxPrefab, pos, data.ExplosionVfxPrefab.transform.rotation);
                Destroy(vfx, data.ExplosionVfxLifetime);
            }
        }

        private void DestroyMarker()
        {
            if (_marker != null)
            {
                Destroy(_marker);
                _marker = null;
            }
        }

        private void OnDestroy()
        {
            // 씬 전환·강제 제거 등으로 Explode를 거치지 않은 경우 마커 잔존 방지
            DestroyMarker();
        }
    }
}

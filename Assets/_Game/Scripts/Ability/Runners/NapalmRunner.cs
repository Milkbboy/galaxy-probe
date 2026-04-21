using System.Collections.Generic;
using UnityEngine;
using DrillCorp.Data;
using DrillCorp.Machine;

namespace DrillCorp.Ability.Runners
{

    /// <summary>
    /// 빅터 1번 어빌리티 — 네이팜.
    /// 머신 원점에서 마우스 방향으로 회전된 직사각형(OBB) 지속 장판 생성.
    /// 20초간 0.1초 주기로 내부 bug에 틱 데미지.
    ///
    /// v2.html:1056 / 1005~1041 / 1001~1004 (pointInRect) 포팅.
    ///
    /// 데미지 해석:
    ///   · AbilityData.Damage = 틱당 데미지 (v2 원본 0.5)
    ///   · AbilityData.Range  = 직사각형 반폭 halfW (v2 원본 42 픽셀)
    ///   · AbilityData.DurationSec / CooldownSec = 초 단위
    /// </summary>
    public class NapalmRunner : IAbilityRunner
    {
        public AbilityType Type => AbilityType.Napalm;

        // ─── 튜닝 상수 ───
        // v2 원본은 len=화면대각선, halfW=42 → 비율 약 10:1 (길쭉한 불바다).
        // SO Range(=halfW)만 튜닝하면 length가 따라 변함.
        private const float LengthToHalfWidthRatio = 10f;
        // v2 6프레임(60fps) = 0.1초 주기 데미지 틱.
        private const float DamageTickInterval = 0.1f;
        // OverlapBox Y 반폭 — 지면 Y=0 기준 위아래로 크게 둬서 Y 놓침 방지.
        private const float ZoneHalfHeight = 4f;
        // 타일 간격 = halfW × 이 값. 작을수록 촘촘(무거움), 클수록 성김(틈새 보일 수 있음).
        private const float TileSpacingMultiplier = 1f;
        // 각 타일 스케일 배율 = halfW × 이 값. OilFireRed 한 타일이 halfW 를 덮도록.
        private const float TileScaleMultiplier = 1f;
        // 바닥 데칼 살짝 띄움 — Z-fighting 방지.
        private const float DecalYOffset = 0.02f;

        private AbilityData _data;
        private AbilityContext _ctx;
        private float _cooldown;

        private readonly List<NapalmZone> _zones = new List<NapalmZone>();
        private readonly Collider[] _overlapBuffer = new Collider[64];

        public float CooldownNormalized =>
            (_data == null || _data.CooldownSec <= 0f)
                ? 0f
                : Mathf.Clamp01(_cooldown / _data.CooldownSec);

        public void Initialize(AbilityData data, AbilityContext ctx)
        {
            _data = data;
            _ctx = ctx;
            _cooldown = 0f;
        }

        public void Tick(float dt)
        {
            if (_cooldown > 0f) _cooldown = Mathf.Max(0f, _cooldown - dt);

            for (int i = _zones.Count - 1; i >= 0; i--)
            {
                var zone = _zones[i];
                if (!zone.Tick(dt, _overlapBuffer, _ctx.BugLayer))
                {
                    zone.Dispose();
                    _zones.RemoveAt(i);
                }
            }
        }

        public bool TryUse(Vector3 aimPoint)
        {
            if (_cooldown > 0f) return false;
            if (_data == null || _ctx == null || _ctx.MachineTransform == null) return false;

            Vector3 origin = _ctx.MachineTransform.position;
            origin.y = 0f;

            Vector3 dir = aimPoint - origin;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
            else dir.Normalize();

            float halfW = Mathf.Max(0.1f, _data.Range);
            float length = halfW * LengthToHalfWidthRatio;

            var zone = new NapalmZone(
                origin: origin,
                dir: dir,
                halfWidth: halfW,
                length: length,
                life: _data.DurationSec,
                damagePerTick: _data.Damage,
                tickInterval: DamageTickInterval,
                halfHeight: ZoneHalfHeight,
                vfxPrefab: _data.VfxPrefab,
                vfxParent: _ctx.VfxParent
            );
            _zones.Add(zone);

            _cooldown = _data.CooldownSec;
            return true;
        }

        // ───────────────────────────────────────────────────────────
        // 단일 장판 인스턴스 — 데미지 판정 + VFX 수명 관리.
        // ───────────────────────────────────────────────────────────
        private class NapalmZone
        {
            private readonly Vector3 _center;
            private readonly Quaternion _rotation;
            private readonly Vector3 _halfExtents;
            private readonly float _damagePerTick;
            private readonly float _tickInterval;

            private float _tickTimer;
            private float _life;
            private GameObject _vfxRoot;
            private AbilityRangeDecal _decal;

            public NapalmZone(
                Vector3 origin, Vector3 dir,
                float halfWidth, float length,
                float life, float damagePerTick, float tickInterval, float halfHeight,
                GameObject vfxPrefab, Transform vfxParent)
            {
                // 원점에서 dir 방향으로 length 만큼 뻗는 직사각형의 "중심".
                _center = origin + dir * (length * 0.5f);
                _rotation = Quaternion.LookRotation(dir, Vector3.up);
                // 로컬 기준: X=좌우 반폭, Y=높이 여유, Z=앞뒤 반길이 (+Z = forward)
                _halfExtents = new Vector3(halfWidth, halfHeight, length * 0.5f);

                _damagePerTick = damagePerTick;
                _tickInterval = Mathf.Max(0.01f, tickInterval);
                _tickTimer = 0f;
                _life = life;

                if (vfxPrefab != null)
                    BuildTiledVfx(origin, halfWidth, length, vfxPrefab, vfxParent);

                BuildRangeDecal(origin, dir, halfWidth, length, vfxParent);
            }

            // 바닥 범위 데칼 — 직사각형 Mesh. wrapper와 독립된 루트로 붙여 페이드아웃을 따로 제어.
            private void BuildRangeDecal(Vector3 origin, Vector3 dir, float halfWidth, float length, Transform vfxParent)
            {
                var decalGo = new GameObject("NapalmRangeDecal");
                decalGo.transform.SetParent(vfxParent, false);
                decalGo.transform.position = origin + new Vector3(0f, DecalYOffset, 0f);
                decalGo.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

                var decal = decalGo.AddComponent<AbilityRangeDecal>();
                decal.SetupMesh(AbilityDecalMeshBuilder.BuildRectangle(halfWidth, length));
                decal.SetTint(new Color(1f, 0.4f, 0.15f, 1f));
                _decal = decal;
            }

            // 타일링 — 머신 원점에서 dir 방향으로 N개 복제 배치.
            // v2 원본의 "길쭉한 불바다" 느낌을 내려면 OilFireRed 같은 작은 불꽃 프리펩을
            // 길이축으로 촘촘히 배치해야 함 (단일 FloorTrapMolten 확대는 원형/정사각형).
            private void BuildTiledVfx(Vector3 origin, float halfWidth, float length,
                                       GameObject tilePrefab, Transform vfxParent)
            {
                // wrapper — 모든 타일의 부모. origin 에 배치하고 dir 방향으로 회전.
                // LookRotation 기준 wrapper 의 로컬 +Z = dir. 타일은 로컬 Z=[0, length] 분포.
                var wrapper = new GameObject("NapalmZoneVfx");
                wrapper.transform.SetParent(vfxParent, false);
                wrapper.transform.position = origin;
                wrapper.transform.rotation = _rotation;
                _vfxRoot = wrapper;

                float spacing = Mathf.Max(0.5f, halfWidth * TileSpacingMultiplier);
                int tileCount = Mathf.Max(1, Mathf.CeilToInt(length / spacing));
                float step = length / tileCount;
                float tileScale = Mathf.Max(0.1f, halfWidth * TileScaleMultiplier);

                for (int t = 0; t < tileCount; t++)
                {
                    var tile = Object.Instantiate(tilePrefab, wrapper.transform);
                    // 타일 중심을 각 세그먼트 중앙에 배치 (원점 ~ origin+dir*length 사이 균등 분포).
                    tile.transform.localPosition = new Vector3(0f, 0f, step * (t + 0.5f));
                    tile.transform.localRotation = Quaternion.identity;
                    tile.transform.localScale *= tileScale;

                    ForceLoopAllParticleSystems(tile);
                }
            }

            private static void ForceLoopAllParticleSystems(GameObject go)
            {
                var particleSystems = go.GetComponentsInChildren<ParticleSystem>(true);
                for (int p = 0; p < particleSystems.Length; p++)
                {
                    var ps = particleSystems[p];
                    var main = ps.main;
                    main.loop = true;
                    if (!ps.isPlaying) ps.Play();
                }
            }

            /// <summary>수명이 남아있으면 true 반환. false면 Runner가 리스트에서 제거.</summary>
            public bool Tick(float dt, Collider[] buffer, LayerMask bugLayer)
            {
                _life -= dt;
                if (_life <= 0f) return false;

                _tickTimer -= dt;
                if (_tickTimer <= 0f)
                {
                    _tickTimer += _tickInterval;
                    ApplyDamage(buffer, bugLayer);
                }
                return true;
            }

            private void ApplyDamage(Collider[] buffer, LayerMask bugLayer)
            {
                int hits = Physics.OverlapBoxNonAlloc(_center, _halfExtents, buffer, _rotation, bugLayer);
                for (int i = 0; i < hits; i++)
                {
                    var col = buffer[i];
                    if (col == null) continue;
                    if (col.TryGetComponent<IDamageable>(out var dmg))
                        dmg.TakeDamage(_damagePerTick);
                }
            }

            public void Dispose()
            {
                if (_vfxRoot != null) Object.Destroy(_vfxRoot);
                _vfxRoot = null;

                // 데칼은 자체 페이드아웃 후 자기 자신을 Destroy.
                if (_decal != null) _decal.Dispose();
                _decal = null;
            }
        }
    }
}

using UnityEngine;
using DrillCorp.Data;
using DrillCorp.Machine;

namespace DrillCorp.Ability.Runners
{
    /// <summary>
    /// 빅터 2번 어빌리티 — 화염방사기.
    /// 활성화 후 5초간 매 프레임 마우스 방향 부채꼴에 dps 데미지.
    /// 마우스가 움직이면 부채꼴도 실시간 회전 (v2.html:1220~1231 포팅).
    ///
    /// 데미지 해석:
    ///   · AbilityData.Damage = 초당 데미지(dps).  v2: 0.18/frame × 60 = 10.8 dps.
    ///   · AbilityData.Range  = 부채꼴 길이 (Unity 월드 유닛).
    ///   · AbilityData.Angle  = 부채꼴 반각 (라디안).  v2: 0.35 rad ≈ ±20°.
    ///   · AbilityData.DurationSec / CooldownSec = 초 단위.
    ///
    /// 한 번에 1회만 활성 (_maxInstances=1). 활성 중 재발동 불가 — 어차피 쿨다운이 더 김.
    /// </summary>
    public class FlameRunner : IAbilityRunner
    {
        public AbilityType Type => AbilityType.Flame;

        private AbilityData _data;
        private AbilityContext _ctx;
        private float _cooldown;
        private float _remainingDuration;
        private GameObject _vfxRoot;

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
            _remainingDuration = 0f;
        }

        public void Tick(float dt)
        {
            if (_cooldown > 0f) _cooldown = Mathf.Max(0f, _cooldown - dt);

            if (_remainingDuration <= 0f) return;

            _remainingDuration -= dt;

            Vector3 dir = ComputeAimDir();
            UpdateVfx(dir);
            ApplyDamage(dir, dt);

            if (_remainingDuration <= 0f)
                StopFlame();
        }

        public bool TryUse(Vector3 aimPoint)
        {
            if (_cooldown > 0f) return false;
            if (_remainingDuration > 0f) return false; // 이미 활성 중
            if (_data == null || _ctx == null || _ctx.MachineTransform == null) return false;

            _remainingDuration = _data.DurationSec;
            _cooldown = _data.CooldownSec;
            SpawnVfx();
            return true;
        }

        // ─── 내부 ───

        private Vector3 ComputeAimDir()
        {
            if (_ctx.Aim == null) return _ctx.MachineTransform.forward;

            Vector3 dir = _ctx.Aim.AimPosition - _ctx.MachineTransform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return _ctx.MachineTransform.forward;
            return dir.normalized;
        }

        private void SpawnVfx()
        {
            if (_data.VfxPrefab == null) return;

            // 머신 자식으로 붙여서 머신과 함께 이동 (현재 머신은 고정이지만 유연성 확보).
            _vfxRoot = Object.Instantiate(
                _data.VfxPrefab,
                _ctx.MachineTransform.position,
                Quaternion.identity,
                _ctx.MachineTransform);
        }

        private void UpdateVfx(Vector3 dir)
        {
            if (_vfxRoot == null) return;

            // +Z forward 모델 가정. Polygon Arsenal Flamethrower 프리펩이 다른 축을
            // 쓴다면 프리펩 내부 자식을 회전시켜 맞추는 편이 Runner 수정보다 깔끔.
            _vfxRoot.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

        private void StopFlame()
        {
            _remainingDuration = 0f;
            if (_vfxRoot != null) Object.Destroy(_vfxRoot);
            _vfxRoot = null;
        }

        private void ApplyDamage(Vector3 dir, float dt)
        {
            Vector3 origin = _ctx.MachineTransform.position;
            origin.y = 0f;

            float range = Mathf.Max(0.1f, _data.Range);
            float halfAngleDeg = _data.Angle * Mathf.Rad2Deg;
            float damageThisFrame = _data.Damage * dt; // dps × dt = 이번 프레임 데미지

            int hits = Physics.OverlapSphereNonAlloc(origin, range, _overlapBuffer, _ctx.BugLayer);
            for (int i = 0; i < hits; i++)
            {
                var col = _overlapBuffer[i];
                if (col == null) continue;

                Vector3 toBug = col.transform.position - origin;
                toBug.y = 0f;
                if (toBug.sqrMagnitude < 0.0001f) continue; // 머신 위 겹침 예외

                // 부채꼴 판정: 발사 방향과 벌레 방향 사이 각도가 반각 이내일 것.
                float angle = Vector3.Angle(dir, toBug);
                if (angle > halfAngleDeg) continue;

                if (col.TryGetComponent<IDamageable>(out var dmg))
                    dmg.TakeDamage(damageThisFrame);
            }
        }
    }
}

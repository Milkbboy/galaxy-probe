using System.Collections.Generic;
using UnityEngine;
using DrillCorp.Data;

namespace DrillCorp.Ability.Runners
{
    /// <summary>
    /// 지누스 2번 어빌리티 — 채굴 드론.
    /// 마우스 위치에 <see cref="MiningDroneInstance"/> 프리펩을 배치. 수명(10초) 내내 채굴량 증가 + 10%/s 보석.
    ///
    /// v2.html:1081~1086 (useItem 'miningdrone') 포팅. tick 로직은 MiningDroneInstance 안에 있음.
    ///
    /// 쿨다운: TryUse 성공 시에만 시작. MaxInstances(기본 1) 를 넘기면 발동 불가.
    /// </summary>
    public class MiningDroneRunner : IAbilityRunner
    {
        public AbilityType Type => AbilityType.MiningDrone;

        private AbilityData _data;
        private AbilityContext _ctx;
        private float _cooldown;

        private readonly List<MiningDroneInstance> _drones = new List<MiningDroneInstance>();

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

            // 수명 만료/Destroy 로 null 된 항목 정리
            for (int i = _drones.Count - 1; i >= 0; i--)
            {
                if (_drones[i] == null) _drones.RemoveAt(i);
            }
        }

        public bool TryUse(Vector3 aimPoint)
        {
            if (_cooldown > 0f) return false;
            if (_data == null || _ctx == null) return false;
            if (_data.VfxPrefab == null)
            {
                Debug.LogWarning("[MiningDroneRunner] AbilityData.VfxPrefab 이 비어있습니다. " +
                                 "Ability_Jinus_MiningDrone.asset 의 _vfxPrefab 슬롯에 MiningDroneInstance 프리펩 바인딩 필요. " +
                                 "Tools/Drill-Corp/3. 게임 초기 설정/10. 지누스 드론 프리펩 생성 메뉴 실행 권장.");
                return false;
            }
            if (_drones.Count >= Mathf.Max(1, _data.MaxInstances)) return false;

            Vector3 pos = aimPoint;
            pos.y = 0f;

            var go = Object.Instantiate(_data.VfxPrefab, pos, Quaternion.identity, _ctx.VfxParent);
            var drone = go.GetComponent<MiningDroneInstance>();
            if (drone == null)
            {
                Debug.LogError("[MiningDroneRunner] VfxPrefab 에 MiningDroneInstance 컴포넌트가 없습니다. 프리펩 바인딩 확인.");
                Object.Destroy(go);
                return false;
            }

            // ctx.Machine 은 null 일 수 있음 (MachineController 없는 씬) — Instance 쪽에서 null-safe.
            drone.Initialize(_data, _ctx.Machine);
            drone.OnDestroyed += () => _drones.Remove(drone);
            _drones.Add(drone);

            _cooldown = _data.CooldownSec;
            return true;
        }
    }
}
